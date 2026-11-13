using System;
using System.Collections.Generic;
using Godot;

namespace ZCraft.Source.World;

public readonly struct LevelEntityData : IEquatable<LevelEntityData>
{
    public readonly ulong UniqueId;
    
    public readonly bool HasPackedScene = false;
    public readonly long PackedSceneUId;

    private static readonly Dictionary<string, long> _uidCache = new();

    public LevelEntityData(ulong uniqueId)
    {
        UniqueId = uniqueId;
    }

    public LevelEntityData(ulong uniqueId, string packedScenePath)
    {
        UniqueId = uniqueId;
        HasPackedScene = true;
        PackedSceneUId = GetOrCacheUid(packedScenePath);
    }

    private static long GetOrCacheUid(string path)
    {
        if (_uidCache.TryGetValue(path, out long uid))
            return uid;

        uid = ResourceUid.TextToId(ResourceUid.PathToUid(path));
        _uidCache[path] = uid;
        return uid;
    }

    public override int GetHashCode() => UniqueId.GetHashCode();
    public bool Equals(LevelEntityData other) => UniqueId == other.UniqueId;
    
    #nullable enable
    public override bool Equals(object? obj) => obj is LevelEntityData o && Equals(o);
    #nullable disable

    public static bool operator ==(LevelEntityData a, LevelEntityData b) => a.Equals(b);
    public static bool operator !=(LevelEntityData a, LevelEntityData b) => !a.Equals(b);
}