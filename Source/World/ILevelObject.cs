using System;
using Godot;
using ZCraft.Source.Networking;
using ZCraft.Source.Serialization;

namespace ZCraft.Source.World;

public interface ILevelObject
{
    //void NetworkReplicationRegister(NetworkId id, WorldRegionNetwork network);
    //void NetworkReplicationUnregister(NetworkId id);
    void SnapshotNetworkWrite(BytePacker packer);
    void SnapshotNetworkRead(BytePacker packer);
    void SnapshotSave(BytePacker packer);
    void SnapshotLoad(BytePacker packer);
}
