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
    
	private GDNetRpc _rpc = new();

	[Export] private Godot.Collections.Dictionary<long, Connection.User> _users = new();

	public override void _Ready()
	{
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
