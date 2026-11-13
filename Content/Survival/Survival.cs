using Godot;
using System;
using ZCraft.Source.World.Item;

namespace ZCraft.Source.Content.Survival;
public partial class Survival : Content
{
    public override void OnEnable()
    {
        Registry.AddItem(this, "stone", new Item());
        Registry.AddItem(this, "wood", new Item());
        Registry.AddItem(this, "woodplanks", new Item());
        Registry.AddItem(this, "stick", new Item());
    }

}
