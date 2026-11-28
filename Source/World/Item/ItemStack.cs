using System;
using System.Runtime.CompilerServices;
using Godot;
using ZCraft.Source.Content;
using ZCraft.Source.Serialization;

namespace ZCraft.Source.World.Item;

public sealed class ItemStack
{
    private int _itemId;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Item ResolveItem() => Registry.Items.GetById(_itemId);

    public ItemStack(Item item)
    {
        _itemId = Registry.Items.GetId(item);
    }

    public ItemStack(string itemId)
    {
        Item item = Registry.Items.GetByName(itemId);
        _itemId = Registry.Items.GetId(item);
    }

    public static DroppedItem Drop(ItemStack item, Level level)
    {
        DroppedItem droppedItem = (DroppedItem)GD.Load<PackedScene>(DroppedItem.DroppedItemScenePath).Instantiate();
        droppedItem.SetItemStackRef(item);
        level.AddChild(droppedItem);
        return droppedItem;
    }

    public static DroppedItem DropAtPosition(ItemStack item, Level level, Vector3 pos)
    {
        DroppedItem droppedItem = Drop(item, level);
        droppedItem.Position = pos;
        return droppedItem;
    }

    public static DroppedItem DropAtGlobalPosition(ItemStack item, Level level, Vector3 pos)
    {
        DroppedItem droppedItem = Drop(item, level);
        droppedItem.GlobalPosition = pos;
        return droppedItem;
    }

    #nullable enable
    public static void NetworkSerialize(ItemStack? itemStack, BytePacker packer)
    {
        packer.WriteBool(itemStack == null);

        if (itemStack == null)
            return;
        
        Item item = itemStack.ResolveItem();
        item.Serialize(packer);

        if (item.Components == null)
            return;
        
        packer.WriteVarInt(itemStack._itemId);

        for (int i = 0; i < item.Components.Count; i++)
        {
            item.Components[i].NetworkSerialize(itemStack, packer);
        }
    }

    #nullable disable

    #nullable enable
    public static ItemStack? NetworkDeserialize(BytePacker packer)
    {
        bool isNull = packer.ReadBool();
        if (isNull)
            return null;
        
        Item item = Item.Deserialize(packer);
        ItemStack itemStack = new ItemStack(item);

        if (item.Components == null)
            return itemStack;
        
        for (int i = 0; i < item.Components.Count; i++)
        {
            item.Components[i].NetworkDeserialize(itemStack, packer);
        }

        return itemStack;
    }
    #nullable disable

    public void WorldSerialize(TagCompound tagCompound)
    {
        Item item = ResolveItem();

        if (item.Components == null)
            return;
        
        //TODO: WORLD SERIALIZATION

    }

    public void WorldDeserialize(TagCompound tagCompound)
    {
        Item item = ResolveItem();

        if (item.Components == null)
            return;
        
        //TODO: WORLD DESERIALIZATION
        

    }


}
