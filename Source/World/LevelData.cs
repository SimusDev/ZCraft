using System;
using Godot;

namespace ZCraft.Source.World;

public class LevelData
{
    public string Id;
    private PackedScene _scene;
    public PackedScene Scene => _scene;

    public LevelData(PackedScene scene)
    {
        _scene = scene;
    }
}
