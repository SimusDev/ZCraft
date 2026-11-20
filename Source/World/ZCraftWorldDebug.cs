using System;
using System.Diagnostics;

namespace ZCraft.Source.World;

public static class ZCraftWorldDebug
{
    public static int ActiveChunks = 0;
    public static int ActiveTrackableNodes = 0;
    public static int ActiveWorldObjectData = 0;
    public static int ReplicatedObjects = 0;
    public static int TickableObjects = 0;
    public static int UniqueNodeRoots = 0;
    public static Stopwatch ServerStartTimeStopWatch = new();

}
