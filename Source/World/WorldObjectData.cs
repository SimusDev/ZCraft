using System;
using System.Collections.Generic;

namespace ZCraft.Source.World;

public class WorldObjectData
{
    private HashSet<int> _peers;
    public HashSet<int> Peers => _peers;
    public readonly ulong UniqueId;

    public WorldObjectData(ulong id)
    {
        UniqueId = id;
    }

    public void SetPeers(HashSet<int> peers)
    {
        _peers = peers;
    }
}
