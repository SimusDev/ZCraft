using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace ZCraft.Source.Networking;

/// <summary>
/// Managed batching/codec pipeline. Godot is accessed only by the consumer of Pump.
/// Both directions are isolated by (peer, mode, channel).
/// </summary>
internal sealed class PacketBatchPipeline : IDisposable
{
    internal const int Mtu = 1350;
    internal const int HeaderSize = 12;
    internal const int LengthSize = 4;
    internal const int MaxPacketSize = 1024 * 1024;

    private const ushort Magic = 0x435A;
    private const byte Version = 2;
    private const int MaxRetainedJobs = 128;

    internal delegate void SendHandler(int peer, ReadOnlySpan<byte> data, int mode, int channel);
    internal delegate void ReceiveHandler(int peer, ReadOnlySpan<byte> data);

    private readonly object _gate = new();
    private readonly Dictionary<RouteKey, RouteState> _outgoingRoutes = new();
    private readonly Dictionary<RouteKey, RouteState> _incomingRoutes = new();
    private readonly Queue<RouteState> _readyOutgoingRoutes = new();
    private readonly Queue<RouteState> _readyIncomingRoutes = new();
    private readonly Stack<Job> _pool = new(MaxRetainedJobs);
    private readonly int _compressThreshold;
    private readonly long _maxBufferedBytes;
    private readonly int _maxQueuedBatches;
    private readonly Action<IThreadPoolWorkItem> _schedule;
    private readonly List<RouteKey> _removedRoutes = new();

    private long _bufferedBytes;
    private int _jobCount;
    private int _pumping;
    private bool _disposed;
    private Job _dispatchingJob;

    internal PacketBatchPipeline(
        int compressThreshold,
        long maxBufferedBytes = 32 * 1024 * 1024,
        int maxQueuedBatches = 4096,
        Action<IThreadPoolWorkItem> schedule = null)
    {
        if (compressThreshold < 0)
            throw new ArgumentOutOfRangeException(nameof(compressThreshold));
        if (maxBufferedBytes < Mtu * 2)
            throw new ArgumentOutOfRangeException(nameof(maxBufferedBytes));
        if (maxQueuedBatches < 1)
            throw new ArgumentOutOfRangeException(nameof(maxQueuedBatches));

        _compressThreshold = compressThreshold;
        _maxBufferedBytes = maxBufferedBytes;
        _maxQueuedBatches = maxQueuedBatches;
        _schedule = schedule; // Tests can control completion order; production uses the shared ThreadPool.
    }

    internal long BufferedBytes
    {
        get
        {
            lock (_gate)
                return _bufferedBytes;
        }
    }

    internal int QueuedBatches
    {
        get
        {
            lock (_gate)
                return _jobCount;
        }
    }

    internal int RouteCount
    {
        get { lock (_gate) return _outgoingRoutes.Count + _incomingRoutes.Count; }
    }

    /// <summary>
    /// Copies one raw packet into the route-specific current batch.
    /// The caller may reuse the source span as soon as this method returns.
    /// </summary>
    internal void Send(int peer, ReadOnlySpan<byte> data, int mode, int channel, bool immediate)
    {
        if (data.Length > MaxPacketSize)
            throw new ArgumentOutOfRangeException(nameof(data));
        if ((uint)mode > 2) throw new ArgumentOutOfRangeException(nameof(mode));
        if (channel < 0) throw new ArgumentOutOfRangeException(nameof(channel));

        int recordSize = LengthSize + data.Length;

        lock (_gate)
        {
            ThrowIfDisposed();

            RouteKey key = new(peer, mode, channel);
            if (!_outgoingRoutes.TryGetValue(key, out RouteState route))
            {
                CheckBudget(OutgoingReservation(recordSize));
                route = GetRoute(_outgoingRoutes, key, incoming: false);
            }

            if (route.Pending != null &&
                recordSize > Mtu - route.Pending.Length)
            {
                SealPending(route);
            }

            if (route.Pending == null)
                route.Pending = CreateOutgoingJob(route, peer, mode, channel, recordSize);

            BinaryPrimitives.WriteInt32LittleEndian(
                route.Pending.Input.AsSpan(route.Pending.Length),
                data.Length);
            data.CopyTo(route.Pending.Input.AsSpan(route.Pending.Length + LengthSize));
            route.Pending.Length += recordSize;

            if (immediate || route.Pending.Length >= Mtu)
                SealPending(route);
        }
    }

    /// <summary>Seals all route-specific partial batches without waiting for worker completion.</summary>
    internal void Flush()
    {
        lock (_gate)
        {
            ThrowIfDisposed();

            foreach (RouteState route in _outgoingRoutes.Values)
                SealPending(route);
        }
    }

    /// <summary>
    /// Enqueues one incoming SceneMultiplayer packet. The packet array is retained as supplied by Godot
    /// and released by dropping the reference after processing.
    /// </summary>
    internal void Receive(int peer, byte[] packet)
    {
        ArgumentNullException.ThrowIfNull(packet);

        int decodedLength = ReadHeader(packet, out _, out int mode, out int channel);
        long reservation = packet.Length +
            (long)System.Numerics.BitOperations.RoundUpToPowerOf2((uint)decodedLength);

        lock (_gate)
        {
            ThrowIfDisposed();
            CheckBudget(reservation);

            RouteState route = GetRoute(_incomingRoutes, new RouteKey(peer, mode, channel), incoming: true);
            Job job = RentJob(route);
            job.Incoming = true;
            job.Input = packet;
            job.Length = packet.Length;
            job.Peer = peer;
            job.Mode = mode;
            job.Channel = channel;
            job.Reservation = reservation;

            route.Jobs.Enqueue(job);
            _bufferedBytes += reservation;
            Schedule(job);
        }
    }

    /// <summary>
    /// Runs on the SceneMultiplayer owner thread. At most maxBatches are emitted per direction.
    /// Routes do not block each other; each route remains FIFO.
    /// </summary>
    internal void Pump(
        SendHandler send,
        ReceiveHandler receive,
        Action<int, Exception> fault,
        int maxBatches = 4096)
    {
        if (maxBatches < 1)
            throw new ArgumentOutOfRangeException(nameof(maxBatches));
        if (Interlocked.Exchange(ref _pumping, 1) != 0)
            return;

        try
        {
            Drain(_readyOutgoingRoutes, send, receive, fault, maxBatches, incoming: false);
            Drain(_readyIncomingRoutes, send, receive, fault, maxBatches, incoming: true);
        }
        finally
        {
            Volatile.Write(ref _pumping, 0);
        }
    }

    /// <summary>Marks queued and currently dispatching packets for a disconnected peer as cancelled.</summary>
    internal void DropPeer(int peer)
    {
        lock (_gate)
        {
            if (_dispatchingJob != null && _dispatchingJob.Peer == peer)
                _dispatchingJob.Cancelled = true;

            DropRoutes(_outgoingRoutes, peer);
            DropRoutes(_incomingRoutes, peer);
            RemoveDetachedReadyRoutes(_readyOutgoingRoutes);
            RemoveDetachedReadyRoutes(_readyIncomingRoutes);
        }
    }

    private void DropRoutes(Dictionary<RouteKey, RouteState> routes, int peer)
    {
        foreach (var pair in routes)
        {
            if (pair.Key.Peer != peer) continue;
            CancelAndReleaseCompleted(pair.Value);
            _removedRoutes.Add(pair.Key);
        }
        foreach (RouteKey key in _removedRoutes) routes.Remove(key);
        _removedRoutes.Clear();
    }

    private static void RemoveDetachedReadyRoutes(Queue<RouteState> readyRoutes)
    {
        int count = readyRoutes.Count;
        for (int i = 0; i < count; i++)
        {
            RouteState route = readyRoutes.Dequeue();
            if (!route.Detached) readyRoutes.Enqueue(route);
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
                return;

            _disposed = true;

            foreach (RouteState route in _outgoingRoutes.Values)
                CancelAndReleaseCompleted(route);

            foreach (RouteState route in _incomingRoutes.Values)
                CancelAndReleaseCompleted(route);

            _readyOutgoingRoutes.Clear();
            _readyIncomingRoutes.Clear();
            _outgoingRoutes.Clear();
            _incomingRoutes.Clear();
            _pool.Clear();
        }
    }

    private void Drain(
        Queue<RouteState> readyRoutes,
        SendHandler send,
        ReceiveHandler receive,
        Action<int, Exception> fault,
        int maxBatches,
        bool incoming)
    {
        for (int i = 0; i < maxBatches; i++)
        {
            RouteState route;
            Job job;

            lock (_gate)
            {
                if (_disposed || !readyRoutes.TryDequeue(out route))
                    return;

                route.ReadyQueued = false;

                if (route.Detached || route.Jobs.Count == 0)
                    continue;

                job = route.Jobs.Peek();
                if (!job.Ready)
                    continue;

                route.Jobs.Dequeue();
                route.Dispatching = true;
                _dispatchingJob = job;
            }

            try
            {
                if (job.Cancelled)
                    continue;

                if (job.Failure != null)
                {
                    fault(job.Peer, job.Failure);
                    continue;
                }

                byte[] data = job.Output ?? job.Input;

                if (!incoming)
                {
                    send(job.Peer, data.AsSpan(0, job.ResultLength), job.Mode, job.Channel);
                    continue;
                }

                int offset = job.Output == null ? HeaderSize : 0;
                int end = offset + job.ResultLength;

                while (offset < end)
                {
                    lock (_gate)
                    {
                        if (_disposed || job.Cancelled)
                            break;
                    }

                    int length = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(offset));
                    offset += LengthSize;
                    receive(job.Peer, data.AsSpan(offset, length));
                    offset += length;
                }
            }
            finally
            {
                lock (_gate)
                {
                    if (ReferenceEquals(_dispatchingJob, job))
                    {
                        _dispatchingJob = null;
                    }

                    route.Dispatching = false;
                    Release(job);
                    EnqueueReadyHead(route);
                }
            }
        }
    }

    private RouteState GetRoute(Dictionary<RouteKey, RouteState> routes, RouteKey key, bool incoming)
    {
        if (routes.TryGetValue(key, out RouteState route))
            return route;

        // Retain idle routes to avoid per-tick allocations, but bound metadata growth too.
        if (routes.Count >= _maxQueuedBatches)
            throw new InvalidOperationException("Network transport route limit exceeded.");
        route = new RouteState(incoming);
        routes.Add(key, route);
        return route;
    }

    private static long OutgoingReservation(int recordSize)
    {
        int capacity = Math.Max(Mtu, HeaderSize + recordSize);
        return 2L * System.Numerics.BitOperations.RoundUpToPowerOf2((uint)capacity);
    }

    private Job CreateOutgoingJob(
        RouteState route,
        int peer,
        int mode,
        int channel,
        int recordSize)
    {
        int capacity = Math.Max(Mtu, HeaderSize + recordSize);
        long reservation = OutgoingReservation(recordSize);
        CheckBudget(reservation);

        byte[] buffer = ArrayPool<byte>.Shared.Rent(capacity);
        Job job = RentJob(route);
        job.Input = buffer;
        job.InputRented = true;
        job.Length = HeaderSize;
        job.Peer = peer;
        job.Mode = mode;
        job.Channel = channel;
        job.Reservation = reservation;
        _bufferedBytes += reservation;
        return job;
    }

    private void SealPending(RouteState route)
    {
        if (route.Pending == null)
            return;

        Job job = route.Pending;
        route.Pending = null;
        route.Jobs.Enqueue(job);
        Schedule(job);
    }

    private void Schedule(Job job)
    {
        try
        {
            if (_schedule != null) { _schedule(job); return; }
            if (ThreadPool.UnsafeQueueUserWorkItem(job, preferLocal: false)) return;
            job.Failure = new InvalidOperationException("Could not schedule a network batch.");
        }
        catch (Exception exception) { job.Failure = exception; }

        MarkReady(job);
    }

    private void Complete(Job job)
    {
        lock (_gate)
        {
            if (_disposed || job.Route.Detached)
            {
                Release(job);
                return;
            }

            MarkReady(job);
        }
    }

    private void MarkReady(Job job)
    {
        job.Ready = true;
        EnqueueReadyHead(job.Route);
    }

    private void EnqueueReadyHead(RouteState route)
    {
        if (_disposed ||
            route.Detached ||
            route.Dispatching ||
            route.ReadyQueued ||
            route.Jobs.Count == 0 ||
            !route.Jobs.Peek().Ready)
        {
            return;
        }

        route.ReadyQueued = true;
        if (route.Incoming)
            _readyIncomingRoutes.Enqueue(route);
        else
            _readyOutgoingRoutes.Enqueue(route);
    }

    private Job RentJob(RouteState route)
    {
        Job job = _pool.TryPop(out Job pooled) ? pooled : new Job(this);
        _jobCount++;
        job.Route = route;
        return job;
    }

    private void CancelAndReleaseCompleted(RouteState route)
    {
        route.Detached = true;
        if (route.Pending != null)
        {
            Release(route.Pending);
            route.Pending = null;
        }
        while (route.Jobs.TryDequeue(out Job job))
        {
            job.Cancelled = true;
            if (job.Ready)
                Release(job);
        }
        // In-flight jobs are no longer in queues; Complete releases them after their worker exits.
    }

    private void Release(Job job)
    {
        if (job.InputRented)
            ArrayPool<byte>.Shared.Return(job.Input);
        if (job.Output != null)
            ArrayPool<byte>.Shared.Return(job.Output);

        _bufferedBytes -= job.Reservation;
        _jobCount--;
        job.Clear();

        if (!_disposed && _pool.Count < MaxRetainedJobs)
            _pool.Push(job);
    }

    private void CheckBudget(long reservation)
    {
        if (_jobCount >= _maxQueuedBatches ||
            reservation > _maxBufferedBytes - _bufferedBytes)
        {
            throw new InvalidOperationException(
                "Network transport backlog limit exceeded. Slow down producers or increase the budget.");
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private static void WriteHeader(Span<byte> target, int length, bool compressed, int mode, int channel)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(target, Magic);
        target[2] = Version;
        target[3] = (byte)((mode << 1) | (compressed ? 1 : 0));
        BinaryPrimitives.WriteInt32LittleEndian(target.Slice(4), length);
        BinaryPrimitives.WriteInt32LittleEndian(target.Slice(8), channel);
    }

    private static int ReadHeader(ReadOnlySpan<byte> packet, out bool compressed, out int mode, out int channel)
    {
        if (packet.Length < HeaderSize + 1 ||
            packet.Length > MaxPacketSize + HeaderSize + LengthSize ||
            BinaryPrimitives.ReadUInt16LittleEndian(packet) != Magic ||
            packet[2] != Version ||
            packet[3] > 5)
        {
            throw new InvalidDataException("Invalid transport header or protocol version.");
        }

        compressed = (packet[3] & 1) != 0;
        mode = packet[3] >> 1;
        channel = BinaryPrimitives.ReadInt32LittleEndian(packet.Slice(8));
        int length = BinaryPrimitives.ReadInt32LittleEndian(packet.Slice(4));

        if (channel < 0 || length < LengthSize ||
            length > MaxPacketSize + LengthSize ||
            (!compressed && length != packet.Length - HeaderSize))
        {
            throw new InvalidDataException("Invalid transport payload length.");
        }

        return length;
    }

    private static void ValidateRecords(ReadOnlySpan<byte> payload)
    {
        if (payload.Length > Mtu - HeaderSize &&
            BinaryPrimitives.ReadInt32LittleEndian(payload) != payload.Length - LengthSize)
        {
            throw new InvalidDataException("An oversized batch must contain exactly one raw packet.");
        }

        int offset = 0;
        while (offset < payload.Length)
        {
            if (payload.Length - offset < LengthSize)
                throw new InvalidDataException("Truncated packet length.");

            int length = BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(offset));
            offset += LengthSize;

            if ((uint)length > (uint)(payload.Length - offset))
                throw new InvalidDataException("Invalid raw packet length.");

            offset += length;
        }
    }

    private readonly struct RouteKey : IEquatable<RouteKey>
    {
        internal readonly int Peer;
        internal readonly int Mode;
        internal readonly int Channel;

        internal RouteKey(int peer, int mode, int channel)
        {
            Peer = peer;
            Mode = mode;
            Channel = channel;
        }

        public bool Equals(RouteKey other)
        {
            return Peer == other.Peer &&
                Mode == other.Mode &&
                Channel == other.Channel;
        }

        public override bool Equals(object obj)
        {
            return obj is RouteKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Peer, Mode, Channel);
        }
    }

    private sealed class RouteState
    {
        internal readonly bool Incoming;
        internal readonly Queue<Job> Jobs = new();
        internal Job Pending;
        internal bool ReadyQueued;
        internal bool Dispatching;
        internal bool Detached;

        internal RouteState(bool incoming)
        {
            Incoming = incoming;
        }
    }

    // Reused ThreadPool work items avoid Task, closure, stream, and per-packet wrapper allocations.
    private sealed class Job : IThreadPoolWorkItem
    {
        private readonly PacketBatchPipeline _owner;

        internal RouteState Route;
        internal byte[] Input;
        internal byte[] Output;
        internal int Length;
        internal int ResultLength;
        internal int Peer;
        internal int Mode;
        internal int Channel;
        internal long Reservation;
        internal bool InputRented;
        internal bool Incoming;
        internal bool Ready;
        internal bool Cancelled;
        internal Exception Failure;

        internal Job(PacketBatchPipeline owner)
        {
            _owner = owner;
        }

        public void Execute()
        {
            try
            {
                if (Incoming)
                    Decode();
                else
                    Encode();
            }
            catch (Exception exception)
            {
                Failure = exception;
            }
            finally
            {
                _owner.Complete(this);
            }
        }

        private void Encode()
        {
            int payloadLength = Length - HeaderSize;
            WriteHeader(Input, payloadLength, false, Mode, Channel);
            ResultLength = Length;

            if (payloadLength < _owner._compressThreshold)
                return;

            Output = ArrayPool<byte>.Shared.Rent(Length);

            if (BrotliEncoder.TryCompress(
                Input.AsSpan(HeaderSize, payloadLength),
                Output.AsSpan(HeaderSize, payloadLength - 1),
                out int written,
                quality: 1,
                window: 22))
            {
                WriteHeader(Output, payloadLength, true, Mode, Channel);
                ResultLength = HeaderSize + written;
            }
            else
            {
                ArrayPool<byte>.Shared.Return(Output);
                Output = null;
            }
        }

        private void Decode()
        {
            ResultLength = ReadHeader(Input.AsSpan(0, Length), out bool compressed, out _, out _);

            if (!compressed)
            {
                ValidateRecords(Input.AsSpan(HeaderSize, ResultLength));
                return;
            }

            Output = ArrayPool<byte>.Shared.Rent(ResultLength);
            using BrotliDecoder decoder = new();
            OperationStatus status = decoder.Decompress(
                Input.AsSpan(HeaderSize, Length - HeaderSize),
                Output.AsSpan(0, ResultLength),
                out int consumed,
                out int written);

            if (status != OperationStatus.Done ||
                written != ResultLength ||
                consumed != Length - HeaderSize)
            {
                throw new InvalidDataException("Invalid or truncated compressed batch.");
            }

            ValidateRecords(Output.AsSpan(0, ResultLength));
        }

        internal void Clear()
        {
            Route = null;
            Input = null;
            Output = null;
            Length = 0;
            ResultLength = 0;
            Peer = 0;
            Mode = 0;
            Channel = 0;
            Reservation = 0;
            InputRented = false;
            Incoming = false;
            Ready = false;
            Cancelled = false;
            Failure = null;
        }
    }
}
