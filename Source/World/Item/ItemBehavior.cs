using System;
using Godot;

namespace ZCraft.Source.World.Item;

[GlobalClass]
public partial class ItemBehaivor : Resource
{
    public virtual void OnUse(ItemBehaviorContext context) {}
    public virtual void OnUseReleased(ItemBehaviorContext context) {}
    public virtual void OnAltUse(ItemBehaviorContext context) {}
    public virtual void OnAltUseReleased(ItemBehaviorContext context) {}
}
