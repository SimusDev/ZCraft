using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using ZCraft.Source.Networking;
using ZCraft.Source.Serialization;

namespace ZCraft.Source.World;

public class LevelUniqueTree
{
    private Level _level = null;
    private ulong _rootUniqueId;
    public ulong RootUniqueId => _rootUniqueId;
    public NetworkRegion NetworkRegion = null;
    public Dictionary<ILevelObject, ulong> LevelObjectsId = null;
    public List<ILevelObject> LevelObjects = null;
    public LevelUniqueTree(Level level)
    {
        _level = level;
        LevelObjectsId = new();
        LevelObjects = new();
    }

    public LevelUniqueTree(Level level, ulong rootUniqueId)
    {
        _level = level;
        _rootUniqueId = rootUniqueId;
        LevelObjectsId = new();
        LevelObjects = new();
    }

    public void CreateServer(Node tree, NetworkRegion networkRegion)
    {
        NetworkRegion = networkRegion;

        for (int i = 0; i < tree.GetChildCount(); i++)
        {
            CreateServer(tree.GetChild(i), networkRegion);
        }

        if (tree is ILevelObject @object)
        {
            ulong uid = ZCraftWorld.GenerateUniqueId();
            @object.LevelObjectCreate(_level, new NetworkId(uid), networkRegion);
            LevelObjectsId[@object] = uid;
            LevelObjects.Add(@object);
        }
    }

    public void CreateClient(Node tree, NetworkRegion networkRegion)
    {
        NetworkRegion = networkRegion;

        for (int i = 0; i < tree.GetChildCount(); i++)
        {
            CreateClient(tree.GetChild(i), networkRegion);
        }

        if (tree is ILevelObject @object)
        {
            LevelObjects.Add(@object);
        }
    }

    public void Destroy()
    {
        foreach(ILevelObject obj in LevelObjects)
        {
            obj.LevelObjectDestroy(_level);
        }
        
        _level = null;
        NetworkRegion = null;
        LevelObjectsId = null;
    }

    public void SnapshotSerialize(BytePacker packer)
    {
        packer.WriteVarInt(LevelObjects.Count);

        foreach(ILevelObject obj in LevelObjects)
        {
            packer.WriteULong(LevelObjectsId[obj]);
            obj.SnapshotNetworkWrite(packer);
        }
    }

    public void SnapshotDeserialize(Node tree, BytePacker packer)
    {
        int count = packer.ReadVarInt();

        for(int i = 0; i < count; i++)
        {
            ulong uid = packer.ReadULong();

            ILevelObject obj = LevelObjects[i];

            obj.LevelObjectCreate(_level, new NetworkId(uid), NetworkRegion);
            obj.SnapshotNetworkRead(packer);
        }
    }

}
