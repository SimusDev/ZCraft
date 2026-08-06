using Godot;
using System;
using System.Runtime.CompilerServices;

namespace ZCraft.Source.World;

public partial class Level : Node3D
{
    [Export] public Networking.NetworkTransport NetworkTransport;
    
    public override void _Ready()
    {
        
    }

    public static Level FindAbove(Node node)
    {
        while (node != null)
        {
            if (node is Level level)
            {
                return level;
            }

            node = node.GetParent();
        }

        return null;
    }
    
}
