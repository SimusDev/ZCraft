using Godot;
using System;

[GlobalClass]
public partial class Inventory : Node
{
	GDNetRpc _rpc = new();

	[Export] private SlotsContainer _container;

	public override void _Ready()
	{
		_rpc.BindOwnerAsNode(this);
		_rpc.BindAll(this);

		_rpc.InvokeOnServer(ServerSend);
	}

	[GDNetRpc(Permission = Permission.Any, Channel = (int)GameServer.Channel.Inventory)]
	private void ServerSend()
	{
		_rpc.InvokeOn(_rpc.GetRemoteSender(), Receive, _container);
	}

	[GDNetRpc(Channel = (int)GameServer.Channel.Inventory)]
	private void Receive(SlotsContainer container)
	{
		_container = container;
	}
}
