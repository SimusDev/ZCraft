using System;
using System.Threading;
using Godot;

namespace ZCraft.Source.Networking;

/// <summary>
/// SendPacketToPeer/FlushRawPackets accept calls from any thread. Setup, Tick, Shutdown,
/// SceneMultiplayer and all events belong to the thread that called Setup (Godot's main thread).
/// Add this node to the tree for automatic ticking, or call Tick yourself when it is detached.
/// </summary>
public partial class NetworkTransport : Node
{
    public const int MTUBytes = PacketBatchPipeline.Mtu;
    public const int MaxRawPacketBytes = PacketBatchPipeline.MaxPacketSize;

    public delegate void PacketSpanHandler(int peer, ReadOnlySpan<byte> packet);

    /// <summary>The span is valid ONLY during the callback; copy it if it needs to outlive the call.</summary>
    public event PacketSpanHandler PacketReceivedSpan;

    /// <summary>Compatibility API: allocates an owned byte[] per raw packet ONLY when subscribed.</summary>
    public event Action<int, byte[]> PacketReceived;

    /// <summary>Malformed input, codec/queue failures. If unsubscribed, errors go to Godot's log.</summary>
    public event Action<int, Exception> TransportError;
    public event Action<int, Error> SendFailed;

    public bool IsServer { get; private set; }
    public long BufferedBytes => Volatile.Read(ref _pipeline)?.BufferedBytes ?? 0;
    public int QueuedBatches => Volatile.Read(ref _pipeline)?.QueuedBatches ?? 0;
    public int MaxBatchesPerTick { get; private set; } = 4096;

    private SceneMultiplayer _api;
    private PacketBatchPipeline _pipeline;
    private PacketBatchPipeline.SendHandler _send;
    private PacketBatchPipeline.ReceiveHandler _receive;
    private Action<int, Exception> _fault;
    private int _ownerThread;
    private double _flushInterval;
    private double _elapsed;

    public void Setup(bool isServer, SceneMultiplayer multiplayer, double flushTickRate = 60,
        int compressThreshold = 512, long maxBufferedBytes = 32 * 1024 * 1024, int maxQueuedBatches = 4096,
        int maxBatchesPerTick = 4096)
    {
        ArgumentNullException.ThrowIfNull(multiplayer);
        if (!double.IsFinite(flushTickRate) || flushTickRate <= 0 || !double.IsFinite(1.0 / flushTickRate))
            throw new ArgumentOutOfRangeException(nameof(flushTickRate));
        if (maxBatchesPerTick < 1) throw new ArgumentOutOfRangeException(nameof(maxBatchesPerTick));
        EnsureOwnerThread();
        var pipeline = new PacketBatchPipeline(compressThreshold, maxBufferedBytes, maxQueuedBatches);
        Shutdown();
        _ownerThread = System.Environment.CurrentManagedThreadId;
        IsServer = isServer;
        MaxBatchesPerTick = maxBatchesPerTick;
        _flushInterval = 1.0 / flushTickRate;
        _elapsed = 0;
        _send ??= SendRawAPI;
        _receive ??= DeliverPacket;
        _fault ??= ReportError;
        _api = multiplayer;
        Volatile.Write(ref _pipeline, pipeline);
        _api.PeerPacket += OnApiPeerPacket;
        _api.PeerDisconnected += OnPeerDisconnected;
    }

    private void OnApiPeerPacket(long id, byte[] packet)
    {
        EnsureOwnerThread();
        // Фоновые задачи распаковывают параллельно; Tick сохраняет FIFO внутри каждого маршрута.
        try { Volatile.Read(ref _pipeline)?.Receive(checked((int)id), packet); }
        catch (Exception exception) { ReportError((int)id, exception); }
    }

    /// <summary>
    /// Copies into the (peer, mode, channel) batch. Full batches and sendImmediate bypass the flush
    /// timer for that route, but not its earlier batches. Other routes proceed independently.
    /// Oversized raw packets get a dedicated batch; the peer handles fragmentation.
    /// Throws before accepting this packet on invalid arguments or a full queue (no silent drop).
    /// </summary>
    public void SendPacketToPeer(int peer, ReadOnlySpan<byte> data, MultiplayerPeer.TransferModeEnum mode,
        int channel, bool sendImmediate = false)
    {
        if (channel < 0) throw new ArgumentOutOfRangeException(nameof(channel));
        if ((uint)mode > (uint)MultiplayerPeer.TransferModeEnum.Reliable)
            throw new ArgumentOutOfRangeException(nameof(mode));
        GetPipeline().Send(peer, data, (int)mode, channel, sendImmediate);
    }

    /// <summary>Seals pending data on all routes for asynchronous encoding. Does not wait for workers.</summary>
    public void FlushRawPackets()
    {
        GetPipeline().Flush();
    }

    public override void _Process(double delta) => Tick(delta);

    public void Tick(double delta)
    {
        EnsureOwnerThread();
        if (!double.IsFinite(delta) || delta < 0) throw new ArgumentOutOfRangeException(nameof(delta));
        PacketBatchPipeline pipeline = Volatile.Read(ref _pipeline);
        if (pipeline == null) return;
        _elapsed += delta;
        if (_elapsed >= _flushInterval)
        {
            _elapsed %= _flushInterval;
            pipeline.Flush();
        }
        // Pump every frame, not only on flush ticks: full/immediate batches must not wait for the timer.
        pipeline.Pump(_send, _receive, _fault, MaxBatchesPerTick);
    }

    /// <summary>Unsubscribes and discards queued packets. In-flight workers release their own buffers.</summary>
    public void Shutdown()
    {
        EnsureOwnerThread();
        if (_api != null)
        {
            _api.PeerPacket -= OnApiPeerPacket;
            _api.PeerDisconnected -= OnPeerDisconnected;
            _api = null;
        }
        Interlocked.Exchange(ref _pipeline, null)?.Dispose();
        _elapsed = 0;
    }

    public override void _ExitTree() => Shutdown();

    private void OnPeerDisconnected(long peer) => Volatile.Read(ref _pipeline)?.DropPeer((int)peer);

    private PacketBatchPipeline GetPipeline() => Volatile.Read(ref _pipeline) ??
        throw new InvalidOperationException("Call NetworkTransport.Setup before sending packets.");

    private void SendRawAPI(int peer, ReadOnlySpan<byte> data, int mode, int channel)
    {
        Error error = _api.SendBytes(data, peer, (MultiplayerPeer.TransferModeEnum)mode, channel);
        if (error == Error.Ok) return;
        if (SendFailed != null) SendFailed(peer, error);
        else GD.PushError($"NetworkTransport.SendBytes to {peer} failed: {error}");
    }

    private void DeliverPacket(int peer, ReadOnlySpan<byte> packet)
    {
        try
        {
            PacketReceivedSpan?.Invoke(peer, packet);
            PacketReceived?.Invoke(peer, packet.ToArray());
        }
        catch (Exception exception) { ReportError(peer, exception); }
    }

    private void ReportError(int peer, Exception exception)
    {
        if (TransportError != null) TransportError(peer, exception);
        else GD.PushError($"NetworkTransport peer {peer}: {exception.Message}");
    }

    private void EnsureOwnerThread()
    {
        if (_ownerThread != 0 && System.Environment.CurrentManagedThreadId != _ownerThread)
            throw new InvalidOperationException("This operation must run on the thread that called Setup.");
    }
}
