using System;
using Godot;

namespace ZCraft.Source.World;

public static class WorldPhysicsCollisions
{
    public const int MaxLayers = 32;

    public enum Layers : int
    {
        World = 0,
        Interaction
    }

    public static void ClearBodyCollisions(CollisionObject3D body)
    {
        int i = MaxLayers;

        while (i > 0)
        {
            body.SetCollisionLayerValue(i, false);
            body.SetCollisionLayerValue(i, false);
        }
    }
}
