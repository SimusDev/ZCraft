using Godot;
using System;
using System.Collections.Concurrent;

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

	private MultiplayerPeer.ConnectionStatus _connectionStatus = MultiplayerPeer.ConnectionStatus.Disconnected;
	public bool IsConnectedToServer = false;
	public bool IsServer = true;
	public int UniqueID = ServerID;

	public enum PacketHeader
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

		if (peer.GetConnectionStatus() != _connectionStatus)
		{
			_connectionStatus = peer.GetConnectionStatus();
			ConnectionStatusChanged();
			EmitSignal(SignalName.OnNetworkPeerConnectionStatusChanged, ((int)_connectionStatus));
		}
	}

	private void ConnectionStatusChanged()
	{
		GD.Print(_connectionStatus.ToString());

		switch(_connectionStatus)
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
		api.PeerPacket += OnApiPeerPacket;

	}

	private void OnApiPeerPacket(long id, byte[] packet)
	{

	}

	public void Setup()
	{
		Setup(new());
	}

	public void RPCConfig(GodotObject @object, Godot.Collections.Dictionary<string, Variant> config)
	{

	}


}
