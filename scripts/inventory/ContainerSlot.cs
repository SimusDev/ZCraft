using Godot;
using System;

[GlobalClass]
public partial class ContainerSlot : Resource, IGDNetSerializable
{

    [Export] ItemStack _itemStack = null;

    public void Serialize(GDNetBuffer buffer)
    {
        buffer.WriteSerializableOrNull(_itemStack);
    }

    public void Deserialize(GDNetBuffer buffer)
    {
        _itemStack = buffer.ReadSerializableOrNull<ItemStack>();
    }
}
