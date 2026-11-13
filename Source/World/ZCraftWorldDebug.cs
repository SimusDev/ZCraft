using System;
using System.Diagnostics;

namespace ZCraft.Source.World;

public static class ZCraftWorldDebug
{
    public static int ActiveChunks = 0;
    public static int ActiveEntityData = 0;
    public static int ActiveTrackableNodes = 0;
    public static Stopwatch ServerStartTimeStopWatch = new();

}
