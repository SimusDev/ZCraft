using System;
using ZCraft.Source.Serialization;

namespace ZCraft.Source.World;

public interface IWorldObject
{
    void Create(WorldObjectData data);
    void Delete(WorldObjectData data);
    void SnapshotNetworkWrite(BytePacker packer);
    void SnapshotNetworkRead(BytePacker packer);
    void SnapshotSave(BytePacker packer);
    void SnapshotLoad(BytePacker packer);
}
