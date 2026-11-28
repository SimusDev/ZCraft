using System;

namespace ZCraft.Source.World.Environment;

public static class Environment
{
    public static Explosion.ExplosionSystem ExplosionSystem = new();

    public static void Initialize()
    {
        ExplosionSystem.Initialize();
    }
}
