using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;
using ZCraft.Source.Serialization;

namespace ZCraft.Source.World
{
    public sealed class ChunkData
    {
        public HashSet<int> ServerPeers = new();
        public Dictionary<Node, LevelEntityData> Nodes = new();
        public void Clear()
        {
            ServerPeers.Clear();
            Nodes.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsEmpty()
        {
            return Nodes.Count == 0;
        }
    }
}
