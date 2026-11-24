using System;
using Godot;
using ZCraft.Source.Content;

namespace ZCraft.Source.World.Item;

public class ItemStack
{
    private int _itemId;

    public Item ResolveItem() => Registry.Items.GetById(_itemId);

    public ItemStack(Item item)
    {
        _itemId = Registry.Items.GetId(item);
    }

    public ItemStack(string itemId)
    {
        _itemId = Registry.Items.GetId(Registry.Items.GetByName(itemId));
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


}
