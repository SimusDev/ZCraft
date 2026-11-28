using System;
using System.Diagnostics;
using ZCraft.Source.World.Item;
using ZCraft.Source.World.Player;

namespace ZCraft.Source.World.Interaction;

public class DamageContext
{
    private object _source;
    private ItemStack _itemStack;
    private float _points;
    public object Source => _source;
    public float Points => _points;

    public DamageContext(object source, float points)
    {
        _source = source;
        _points = points;
    }

    public DamageContext WithItemStack(ItemStack itemStack)
    {
        _itemStack = itemStack;
        return this;
    }
}
