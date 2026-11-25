using System;
using System.Diagnostics;
using ZCraft.Source.World.Item;
using ZCraft.Source.World.Player;

namespace ZCraft.Source.World.Interaction;

public class InteractionContext
{
    private static InteractionContext _instance = new();
    private object _source;
    private PlayerComponent _player;
    private ItemStack _itemStack;
    public object Source => _source;

    public static InteractionContext Create(object source)
    {
        _instance._source = source;
        _instance._player = null;
        _instance._itemStack = null;
        return _instance;
    }

    public bool IsPlayer => _player != null;

    #nullable enable
    public int? GetPeerId()
    {
        if (_player != null)
            return _player.GetMultiplayerAuthority();
        return null;
    }

    #nullable disable

    public InteractionContext WithPlayer(PlayerComponent player)
    {
        _instance._player = player;
        return _instance;
    }

    public InteractionContext WithItemStack(ItemStack itemStack)
    {
        _instance._itemStack = itemStack;
        return _instance;
    }





}
