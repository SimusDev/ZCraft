using System;
using Godot;

namespace ZCraft.Source.World.Item;

[GlobalClass]
public partial class ItemBehavior : Resource
{
    public virtual IItemBehaviorSystem Build() => null;
}
