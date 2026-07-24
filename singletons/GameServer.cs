using GDNetExtensions;
using Godot;
using System;
using System.Security.Cryptography;
using System.Threading.Tasks;

public partial class GameServer : Node
{
	public enum Channel : int
	{
		Inventory = 1,
		States,
		BigData,
		Users,
	}

	public int Port = 8080;
    
	private GDNetRpc _rpc = new();

	public static GameServer Instance { get; private set; }

	[Export] private Godot.Collections.Dictionary<long, Connection.User> _users = new();

	[Signal] public delegate void OnKickedEventHandler(string reason);
	[Signal] public delegate void OnLocalUserReadyEventHandler(Connection.User user);

	private GDNetBuffer _buffer = new();

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

		GDNet.Instance.OnNetworkReady += OnLocalNetworkReady;
    }

    private void OnLocalNetworkReady()
    {
		_rpc.InvokeOnServer(SendUserDataToClient, "Player");
    }

	[GDNetRpc(Channel = (int)Channel.Users, Permission = Permission.Any)]
	private void SendUserDataToClient(string username)
	{
		_buffer.Clear();
		_buffer.WriteIntVar(_users.Count);
		foreach (var user in _users)
		{

		}

		_rpc.InvokeOn(_rpc.GetRemoteSender(), ReceiveDataFromServer, _buffer.GetBytes());
	}

    [GDNetRpc(Channel = (int)Channel.Users, Permission = Permission.ServerOrAuth)]
    private void ReceiveDataFromServer(byte[] data)
    {
		_buffer.Clear();
		_buffer.SetBytes(data);
    }

    public void KickPeer(int pid, string reason = "")
	{
		if (GDNet.isServer)
			KickPeerInternal(pid, reason);
	}

	private async Task KickPeerInternal(int pid, string reason)
	{
        if (!GDNet.isServer)
            return;

		if (!Multiplayer.GetPeers().Has(pid))
			return;

        _rpc.InvokeOn(pid, ReceiveKick, reason);
		await ToSignal(GetTree().CreateTimer(2), SceneTreeTimer.SignalName.Timeout);
       
		if (!Multiplayer.GetPeers().Has(pid))
            return;

        Multiplayer.MultiplayerPeer.DisconnectPeer(pid);

    }

    [GDNetRpc(Channel = (int)Channel.Users, Permission = Permission.ServerOrAuth)]
	private void ReceiveKick(string reason)
	{
		Multiplayer.MultiplayerPeer.Close();
		EmitSignal(SignalName.OnKicked, reason);
	}

	

}
