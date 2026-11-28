using ZCraft.Source.Entity;
using ZCraft.Source.Networking;

namespace ZCraft.Source.World.Item;

public class ItemBehaviorContext
{
    public ItemHolderComponent ItemHolder => _itemHolder;
    public ItemStack ItemStack => _itemStack;
    public Level Level => _level;
    public NetworkId NetworkId => _networkId;
    public NetworkRegion Network => _network;

    private ItemHolderComponent _itemHolder;
    private ItemStack _itemStack;
    private Level _level;
    private NetworkId _networkId;
    private NetworkRegion _network;

    public ItemBehaviorContext(ItemHolderComponent itemHolder, ItemStack itemStack, Level level, NetworkId networkId, NetworkRegion network)
    {
        _itemHolder = itemHolder;
        _itemStack = itemStack;
        _level = level;
        _networkId = networkId;
        _network = network;
    }

}
