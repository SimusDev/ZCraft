using ZCraft.Source.Networking;
using ZCraft.Source.Serialization;

namespace ZCraft.Source.World;

public interface ILevelObject
{
    void LevelObjectCreate(Level level, NetworkId id, NetworkRegion network);
    void LevelObjectDestroy(Level level);
    void SnapshotNetworkWrite(BytePacker packer);
    void SnapshotNetworkRead(BytePacker packer);
    void SnapshotSave(TagCompound tagCompound);
    void SnapshotLoad(TagCompound tagCompound);
}
