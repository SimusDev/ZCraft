using System;
using Godot;

namespace ZCraft.Source.World.Item;

[GlobalClass]
public partial class ItemComponent : Resource
{
    public virtual bool IsClientSide() => false;
}
