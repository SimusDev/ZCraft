using Godot;
using System;

using ZCraft.Source.Networking;
using ZCraft.Source.Networking.Rpc;
using ZCraft.Source.Module;

public partial class NetworkedNodeTest : Node, INetworkObject
{
    public override void _EnterTree()
    {
        NetworkReplicator.AddObject(this, GetPath());
    }

    public override void _ExitTree()
    {
        NetworkReplicator.RemoveObject(this);
    }

    public override void _Ready()
    {
        NetworkReplicator.RpcInvoke(this, RpcTest);
    }

    [NetworkRpc(SendTo.Server)]
    protected void RpcTest()
    {
    }

}
