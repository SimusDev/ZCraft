using System;
using Godot;

namespace ZCraft.Source.World.Item;

public class ItemBehaviorContext
{
    private static ItemBehaviorContext _instance = new();
    private object _source;
    public Item _item;
    public ItemStack _itemStack;

    public object Source => _source;
    public Item Item => _item;
    public ItemStack ItemStack => _itemStack;

    public static ItemBehaviorContext Build(object source, Item item)
    {
        _instance._source = source;
        _instance._item = item;
        return _instance;
    }

    public ItemBehaviorContext WithItemStack(ItemStack itemStack)
    {
        _itemStack = itemStack;
        return this;
    }

}
