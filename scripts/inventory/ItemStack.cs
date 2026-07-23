using Godot;
using System;

[GlobalClass]
public partial class ItemStack : Resource, IGDNetSerializable
{
    [Export] private RGameResource _resource = null;
    private int _quantity = 1;

    private long _netId = GDNet.GenerateUniqueID();

    [Export] private GDNetRpc _rpc = new();

    [Export] public int Quantity
    {
        set
        {
            SetQuantity(value);
        }

        get { return _quantity; }
    }

    public ItemStack()
    {
        _rpc.SynchronizeNetworkIDByUniqueID(_netId);
        _rpc.BindAll(this);
    }

    public int GetQuantity()
    {
        return _quantity;
    }

    public void SetQuantity(int value)
    {
        _quantity = value;
        _rpc.Invoke(SetQuantityNet, value);
    }

    [GDNetRpc(Channel = (int)GameServer.Channel.Inventory)]
    private void SetQuantityNet(int value)
    {
        _quantity = value;
        GD.Print($"Set Quantity Net {_quantity}");
    }

    public void Deserialize(GDNetBuffer buffer)
    {
        _rpc.SynchronizeNetworkIDByUniqueID(buffer.ReadLongVar());
        _resource = buffer.ReadResource<RGameResource>();
    }

    public void Serialize(GDNetBuffer buffer)
    {
        buffer.WriteLongVar(_netId);
        buffer.WriteResource(_resource);
    }

    
}
