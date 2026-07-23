using Godot;
using System;

public partial class GameServer : Node
{
	public enum Channel : int
	{
		Inventory = 1,
		BigData,
		Users,
	}

	public int Port = 8080;
    
	private GDNetRpc _rpc = new();

	public static GameServer Instance { get; private set; }

	[Export] private Godot.Collections.Dictionary<long, Connection.User> _users = new();

	public Error CreateServer()
	{
		ENetMultiplayerPeer peer = new();
		var err = peer.CreateServer(Port, 1000);
		Multiplayer.MultiplayerPeer = peer;
		return err;
	}

	public Error CreateClient(string address)
	{
        ENetMultiplayerPeer peer = new();
		var err = peer.CreateClient(address, Port);
        Multiplayer.MultiplayerPeer = peer;
        return err;
    }

	public override void _Ready()
	{
		Instance = this;
		var Api = new SceneMultiplayer();
		Api.ServerRelay = false;
		GDNet.Instance.Setup(Api);

		_rpc.BindOwnerAsNode(this);
		_rpc.BindAll(this);

		Multiplayer.PeerConnected += OnPeerConnected;
		Multiplayer.PeerDisconnected += OnPeerDisconnected;
    }

    private void OnPeerDisconnected(long id)
    {
		_users.Remove(id);
    }

    private void OnPeerConnected(long id)
    {

    }

}
