using System;
using System.Runtime.CompilerServices;
using ZCraft.Source.World;

namespace ZCraft.Source.Entity;

public static class Ticker
{
    public static TickablesRegistry Registry = new();
    public static FixedTickablesRegistry FixedRegistry = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Tick(double dt)
    {
        Registry.Tick(dt);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FixedTick(double dt)
    {
        FixedRegistry.Tick(dt);
    }

}
