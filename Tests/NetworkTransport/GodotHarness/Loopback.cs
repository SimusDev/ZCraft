using System;
using System.Buffers.Binary;
using System.Threading.Tasks;
using Godot;
using ZCraft.Source.Networking;

public partial class Loopback : Node
{
    private const int Count = 300;
    private SceneMultiplayer _server, _client;
    private ENetMultiplayerPeer _serverPeer, _clientPeer;
    private NetworkTransport _serverTransport, _clientTransport;
    private byte[][] _packets;
    private Task _producer;
    private int _serverReceived, _clientReceived, _compatibilityReceived, _ownerThread;
    private bool _started, _finished;
    private double _time;

    public override void _Ready()
    {
        try
        {
            _ownerThread = System.Environment.CurrentManagedThreadId;
            _server = new SceneMultiplayer { RootPath = GetPath() };
            _client = new SceneMultiplayer { RootPath = GetPath() };
            _serverPeer = new ENetMultiplayerPeer();
            _clientPeer = new ENetMultiplayerPeer();
            // Get an OS-assigned free UDP port, then let ENet bind it.
            using var socket = new System.Net.Sockets.UdpClient(0);
            int port = ((System.Net.IPEndPoint)socket.Client.LocalEndPoint).Port;
            socket.Close();
            Require(_serverPeer.CreateServer(port, 4, 4) == Error.Ok, "ENet server failed");
            Require(_clientPeer.CreateClient("127.0.0.1", port, 4) == Error.Ok, "ENet client failed");
            _server.MultiplayerPeer = _serverPeer;
            _client.MultiplayerPeer = _clientPeer;
            _serverTransport = MakeTransport(_server, true);
            _clientTransport = MakeTransport(_client, false);
            _serverTransport.PacketReceivedSpan += (_, data) => CheckPacket(data, ref _serverReceived);
            _clientTransport.PacketReceivedSpan += (_, data) => CheckPacket(data, ref _clientReceived);
            _clientTransport.PacketReceived += (_, data) => CheckPacket(data, ref _compatibilityReceived);
            _packets = new byte[Count][];
            var random = new Random(123);
            for (int i = 0; i < Count; i++)
            {
                int size = i == 50 ? 32768 : i == 100 ? 128 * 1024 : i % 7 == 0 ? 0 : 32;
                _packets[i] = new byte[size];
                if (i != 100) random.NextBytes(_packets[i]);
                if (size >= 4) BinaryPrimitives.WriteInt32LittleEndian(_packets[i], i);
            }
        }
        catch (Exception e) { Fail(e); }
    }

    private NetworkTransport MakeTransport(SceneMultiplayer api, bool server)
    {
        var transport = new NetworkTransport();
        AddChild(transport);
        transport.Setup(server, api, flushTickRate: 20);
        transport.TransportError += (_, e) => Fail(e);
        transport.SendFailed += (_, error) => Fail(new Exception($"SendBytes: {error}"));
        return transport;
    }

    public override void _Process(double delta)
    {
        if (_finished) return;
        try
        {
            _time += delta;
            Require(_time < 15, $"Timeout: {_serverReceived}/{_clientReceived}");
            _server.Poll();
            _client.Poll();
            if (!_started && _clientPeer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Connected &&
                _server.GetPeers().Length == 1)
            {
                _started = true;
                int clientId = _client.GetUniqueId();
                _producer = Task.Run(() =>
                {
                    foreach (byte[] packet in _packets)
                    {
                        _clientTransport.SendPacketToPeer(1, packet, MultiplayerPeer.TransferModeEnum.Reliable, 1, false);
                        _serverTransport.SendPacketToPeer(clientId, packet, MultiplayerPeer.TransferModeEnum.Reliable, 1, false);
                    }
                });
            }
            if (_producer?.IsFaulted == true) throw _producer.Exception;
            if (_serverReceived != Count || _clientReceived != Count || _compatibilityReceived != Count) return;
            Require(_producer.IsCompletedSuccessfully, "Producer failed");
            _serverTransport.Shutdown();
            _clientTransport.Shutdown();
            _serverPeer.Close();
            _clientPeer.Close();
            _finished = true;
            GD.Print("PASS Godot SceneMultiplayer/ENet loopback: 300 packets each way, background producer, span + byte[] events, ordered delivery, jumbo fragmentation, compression, automatic Tick.");
            GetTree().Quit(0);
        }
        catch (Exception e) { Fail(e); }
    }

    private void CheckPacket(ReadOnlySpan<byte> data, ref int next)
    {
        Require(System.Environment.CurrentManagedThreadId == _ownerThread, "Callback thread changed");
        Require(next < Count && data.SequenceEqual(_packets[next]), $"Corrupted or reordered packet {next}");
        next++;
    }

    private static void Require(bool value, string message)
    {
        if (!value) throw new Exception(message);
    }

    private void Fail(Exception exception)
    {
        if (_finished) return;
        _finished = true;
        GD.PushError(exception.ToString());
        GetTree().Quit(1);
    }
}
