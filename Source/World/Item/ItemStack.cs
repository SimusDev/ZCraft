using System;
using Godot;
using ZCraft.Source.Content;
using ZCraft.Source.Serialization;

namespace ZCraft.Source.World.Item;

public class ItemStack
{
    private int _itemId;

    public Item ResolveItem() => Registry.Items.GetById(_itemId);

    public ItemStack(Item item)
    {
        _itemId = Registry.Items.GetId(item);
        item.OnCreate(this);
    }

    public ItemStack(string itemId)
    {
        Item item = Registry.Items.GetByName(itemId);
        _itemId = Registry.Items.GetId(item);
        item.OnCreate(this);
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

    public void NetworkSerialize(BytePacker packer)
    {
        Item item = ResolveItem();

        if (item.Components == null)
            return;
        
        for (int i = 0; i < item.Components.Count; i++)
        {
            item.Components[i].NetworkSerialize(this, packer);
        }
    }

    public void NetworkDeserialize(BytePacker packer)
    {
        Item item = ResolveItem();

        if (item.Components == null)
            return;
        
        for (int i = 0; i < item.Components.Count; i++)
        {
            item.Components[i].NetworkDeserialize(this, packer);
        }
    }

    public void WorldSerialize(BytePacker packer)
    {
        Item item = ResolveItem();

        if (item.Components == null)
            return;
        
        for (int i = 0; i < item.Components.Count; i++)
        {
            item.Components[i].WorldSerialize(this, packer);
        }
    }

    public void WorldDeserialize(BytePacker packer)
    {
        Item item = ResolveItem();

        if (item.Components == null)
            return;
        
        for (int i = 0; i < item.Components.Count; i++)
        {
            item.Components[i].WorldDeserialize(this, packer);
        }
    }


}
