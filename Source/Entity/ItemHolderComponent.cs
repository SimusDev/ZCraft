using System;
using System.Collections.Generic;
using Godot;
using ZCraft.Source.Content;
using ZCraft.Source.Networking;
using ZCraft.Source.Serialization;
using ZCraft.Source.World;
using ZCraft.Source.World.Item;

namespace ZCraft.Source.Entity;

[GlobalClass]
public partial class ItemHolderComponent : Node, INetworkReplicationObject, ILevelObject
{
    [Export] private string _initialItemName;

    private ItemStack _itemStackRef = null;

    private NetworkRegion _net;
    private NetworkId _systemNetworkId;
    private List<IItemBehaviorSystem> _behaviorSystems = new();

    private ItemBehaviorContext _behaviorContext = null;

    public override void _Ready()
    {
        if (NetworkManager.IsServer)
        {
            ServerReady();
        }

        else
        {
            ClientReady();
        }
    }

    public void ServerReady()
    {
        _systemNetworkId = new();
        Item item = Registry.Items.GetByName(_initialItemName);
        ItemStack itemStack = new ItemStack(item);
        _itemStackRef = itemStack;
        SetProcessInput(false);
    }

    public void ClientReady()
    {
        Item item = _itemStackRef.ResolveItem();

        if (item.Behaviors == null)
            return;
        
        foreach(ItemBehavior behavior in item.Behaviors)
        {
            IItemBehaviorSystem system = behavior.Build();
            if (system == null)
                continue;
            


        }

        SetProcessInput(IsMultiplayerAuthority());
    }

    public void SetItemStack()
    {
        
    }

    public void SerializeFullState(BytePacker packer)
    {
        ItemStack.NetworkSerialize(_itemStackRef, packer);
    }

    public void DeserializeFullState(BytePacker packer)
    {
        _itemStackRef = ItemStack.NetworkDeserialize(packer);
    }

    public void CleanUpBehaviors()
    {
        foreach(IItemBehaviorSystem system in _behaviorSystems)
        {
            system.OnDestroy(_behaviorContext);
        }

        _behaviorSystems.Clear();

        _behaviorContext = null;
    }

    public override void _Input(InputEvent @event)
    {
        
    }

    public void LevelObjectCreate(Level level, NetworkId id, NetworkRegion network)
    {
        _net = network;
        NetworkReplicator.AddObject(this, id);
    }

    public void LevelObjectDestroy(Level level)
    {
        _net = null;
        _itemStackRef = null;
        NetworkReplicator.RemoveObject(this);
    }

    public void OnNetworkMessage(int fromPeer, BytePacker packer)
    {
    }

    public void SnapshotLoad(TagCompound tagCompound)
    {
        
    }

    public void SnapshotNetworkRead(BytePacker packer)
    {
        _systemNetworkId = packer.ReadNetworkId();
        DeserializeFullState(packer);
    }

    public void SnapshotNetworkWrite(BytePacker packer)
    {
        packer.WriteNetworkId(_systemNetworkId);
        SerializeFullState(packer);
    }

    public void SnapshotSave(TagCompound tagCompound)
    {
        
    }
}
