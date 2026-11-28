using System;
using Godot;
using ZCraft.Source.Serialization;

namespace ZCraft.Source.World.Item;

[GlobalClass]
public partial class ItemComponent : Resource
{
    public virtual void NetworkSerialize(ItemStack source, BytePacker packer) {}
    public virtual void NetworkDeserialize(ItemStack source, BytePacker packer) {}
    public virtual void WorldSerialize(ItemStack source, BytePacker packer) {}
    public virtual void WorldDeserialize(ItemStack source, BytePacker packer) {}

}
