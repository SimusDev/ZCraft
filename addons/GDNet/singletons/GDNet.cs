using Godot;
using System;

public partial class GDNet : Node
{
	public static GDNet Instance = null;

	private StreamPeerBuffer _buffer = new();

	public const int ServerID = 1;

	[Signal] public delegate void OnNetworkPeerConnectionStatusChangedEventHandler(MultiplayerPeer.ConnectionStatus status);
	[Signal] public delegate void OnNetworkReadyEventHandler();
	[Signal] public delegate void OnNetworkConnectingEventHandler();
	[Signal] public delegate void OnNetworkDisconnectedEventHandler();

	[Export] private Timer _tickTimer;

	public event Action<PacketType, byte[], long> OnNetworkPacket;

	private MultiplayerPeer.ConnectionStatus _connectionStatus = MultiplayerPeer.ConnectionStatus.Disconnected;
	public bool IsConnectedToServer = false;
	public bool IsServer = true;
	public int UniqueID = ServerID;

	[Export] private GDNetOptimizedSend _optimizedSend;

	public enum PacketType
	{
		RpcRequest,
		RpcReceive,
	}

	public override void _EnterTree()
	{
		Instance = this;
	}

	public override void _Ready()
	{
		_tickTimer.Timeout += UpdateNetworkStateTick;
		_tickTimer.Start();
	}

	private void UpdateNetworkStateTick()
	{
		MultiplayerPeer peer = Multiplayer.MultiplayerPeer;
		if (peer == null)
			return;

		if (peer is OfflineMultiplayerPeer)
		{
			return;
		}

		if (peer.GetConnectionStatus() != _connectionStatus)
		{
			_connectionStatus = peer.GetConnectionStatus();
			ConnectionStatusChanged();
			EmitSignal(SignalName.OnNetworkPeerConnectionStatusChanged, ((int)_connectionStatus));
		}
	}

	private void ConnectionStatusChanged()
	{
		IsServer = Multiplayer.IsServer();
		IsConnectedToServer = _connectionStatus == MultiplayerPeer.ConnectionStatus.Connected;
		UniqueID = Multiplayer.GetUniqueId();

        switch (_connectionStatus)
		{
			case MultiplayerPeer.ConnectionStatus.Disconnected:
				EmitSignal(SignalName.OnNetworkDisconnected);
				break;
			case MultiplayerPeer.ConnectionStatus.Connecting:
				EmitSignal(SignalName.OnNetworkConnecting);
				break;
			case MultiplayerPeer.ConnectionStatus.Connected:
				EmitSignal(SignalName.OnNetworkReady);
				break;
		}
	}

	public void Setup(SceneMultiplayer api)
	{
		GetTree().SetMultiplayer(api);
		_optimizedSend.Setup(api);
		_optimizedSend.MultiplayerPeerPacket += OnOptimizedPeerPacket;
	}
    public void Setup()
    {
        Setup(new());
    }

    private void OnOptimizedPeerPacket(long id, byte[] bytes)
    {
		_buffer.DataArray = bytes;
		_buffer.Seek(0);
		
		var type = (PacketType)_buffer.GetU8();
		OnNetworkPacket?.Invoke(type, (byte[])_buffer.GetData(_buffer.GetAvailableBytes())[1], id);
    }

	public void SendPacket(PacketType type, byte[] bytes, int peer, MultiplayerPeer.TransferModeEnum mode, int channel)
	{
		_buffer.Clear();
		_buffer.Seek(0);
		_buffer.PutU8((byte)type);
		_buffer.PutData(bytes);
		_optimizedSend.MultiplayerSendBytes(_buffer.DataArray, peer, mode, channel);

	}



}
