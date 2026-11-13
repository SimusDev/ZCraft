using System;
using System.Collections.Generic;
using Godot;
using ZCraft.Source.Networking;
using ZCraft.Source.Serialization;

namespace ZCraft.Source.World.Synchronizers;

[GlobalClass]
public partial class NetworkTransform : Node, IWorldObject, INetworkReplicationObject
{
    [Export] private Godot.Collections.Array<Node3D> _nodes = new(); 
    [Export] private double _tickRate = 0;
    [Export] private double _tickTime = 0;
    public static readonly List<NetworkTransform> All = new();
    private WorldObjectData _data;

    public override void _EnterTree()
    {
        if (_tickRate <= 0)
        {
            _tickRate = 32;
        }

        All.Add(this);
    }

    public override void _ExitTree()
    {
        All.Remove(this);
    }

    public void Tick(double delta)
    {
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
                    NetworkReplicator.SendMessageToServer(this, packer, NetworkManager.NetChannel.Unreliable);
                else
                    NetworkReplicator.SendMessageToPeers(_data.Peers, this, packer, NetworkManager.NetChannel.Unreliable);
                
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

    void IWorldObject.Create(WorldObjectData data)
    {
        _data = data;
        //NetworkReplicator.AddObject(this, data.UniqueId);
    }

    void IWorldObject.Delete(WorldObjectData data)
    {
        NetworkReplicator.RemoveObject(this);
    }

    void IWorldObject.SnapshotNetworkWrite(BytePacker packer)
    {

    }

    void IWorldObject.SnapshotNetworkRead(BytePacker packer)
    {
  
    }

    void IWorldObject.SnapshotSave(BytePacker packer)
    {
       
    }

    void IWorldObject.SnapshotLoad(BytePacker packer)
    {
        
    }


}
