using System;
using System.Collections.Generic;
using Godot;
using ZCraft.Source.Entity;
using ZCraft.Source.Networking;
using ZCraft.Source.Serialization;

namespace ZCraft.Source.World.Synchronizers;

[GlobalClass]
public partial class NetworkTransform : Node, ILevelObject, INetworkReplicationObject, ITickable
{
    [Export] private Godot.Collections.Array<Node3D> _nodes = new(); 
    [Export] private double _tickRate = 0;
    [Export] private double _tickTime = 0;

    private NetworkRegion _network;

    public override void _EnterTree()
    {
        if (_tickRate <= 0)
        {
            _tickRate = 32;
        }

        Ticker.Registry.Add(this);
    }

    public override void _ExitTree()
    {
        Ticker.Registry.Remove(this);
    }

    void ITickable.OnTick(double delta)
    {
        if (_network == null)
            return;

        if (IsMultiplayerAuthority())
        {
            _tickTime += delta;

            if (_tickTime >= 1.0 / _tickRate)
            {
                BytePacker packer = BytePackerPool.Rent();

                packer.WriteByte(0);
                packer.WriteVarInt(_nodes.Count);

                for(int i = 0; i < _nodes.Count; i++)
                {
                    packer.WriteVector3(_nodes[i].Position);
                    packer.WriteVector3(_nodes[i].Rotation);
                }

                if (NetworkManager.IsServer)
                    _network.ClientSend(this, packer, NetworkManager.NetChannel.Unreliable);
                else
                    _network.ServerBroadcast(this, packer, NetworkManager.NetChannel.Unreliable);

                BytePackerPool.Return(packer);
                
                _tickTime = 0;
            }
        }

        else
        {
            
        }
    }

    void INetworkReplicationObject.OnNetworkMessage(int fromPeer, BytePacker packer)
    {
        byte type = packer.ReadByte();
        switch (type)
        {
            case 0:

                int count = packer.ReadVarInt();

                for (int i = 0; i < count; i++)
                {
                    _nodes[i].Position = packer.ReadVector3();
                    _nodes[i].Rotation = packer.ReadVector3();
                }

                break;
        }
    }

    public void SnapshotNetworkWrite(BytePacker packer) {}

    public void SnapshotNetworkRead(BytePacker packer) {}

    public void SnapshotSave(BytePacker packer) {}

    public void SnapshotLoad(BytePacker packer) {}

    public void LevelObjectCreate(Level level, NetworkId id, NetworkRegion network)
    {
        _network = network;
        NetworkReplicator.AddObject(this, id);
    }

    public void LevelObjectDestroy(Level level)
    {
        _network = null;
        NetworkReplicator.RemoveObject(this);
    }
}
