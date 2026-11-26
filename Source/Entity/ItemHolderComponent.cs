using System;
using Godot;
using ZCraft.Source.Content;
using ZCraft.Source.Networking;
using ZCraft.Source.Serialization;
using ZCraft.Source.World;
using ZCraft.Source.World.Item;

namespace ZCraft.Source.Entity;

[GlobalClass]
public partial class ItemHolderComponent : Node3D, INetworkReplicationObject, ILevelObject
{
    public const string InputActionInteract = "hands.interact";
    public const string InputActionInteractAlt = "hands.interact_alt";
    [Export] private string _initialItemName;

    private ItemStack _itemStackRef = null;

    private NetworkRegion _net;

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
        Item item = Registry.Items.GetByName(_initialItemName);
        ItemStack itemStack = new ItemStack(item);
        _itemStackRef = itemStack;
        SetProcessInput(false);
    }

    public void ClientReady()
    {
        SetProcessInput(IsMultiplayerAuthority());
    }

    public void SetItemStack()
    {
        
    }

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionJustPressed(InputActionInteract))
        {
            GD.Print(_itemStackRef.ResolveItem().ResourcePath);
        }

        else if (Input.IsActionJustReleased(InputActionInteract))
        {
            
        }

        if (Input.IsActionJustPressed(InputActionInteractAlt))
        {
            
        }

        else if (Input.IsActionJustReleased(InputActionInteractAlt))
        {
            
        }
    }

    public void LevelObjectCreate(Level level, NetworkId id, NetworkRegion network)
    {
        _net = network;
        NetworkReplicator.AddObject(this, id);
    }

    public void LevelObjectDestroy(Level level)
    {
        _net = null;
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
        _itemStackRef = ItemStack.NetworkDeserialize(packer);
    }

    public void SnapshotNetworkWrite(BytePacker packer)
    {
        ItemStack.NetworkSerialize(_itemStackRef, packer);
    }

    public void SnapshotSave(TagCompound tagCompound)
    {
        
    }
}
