using ZCraft.Source.World.Item;

namespace ZCraft.Source.World.Container;

public interface IContainer
{
    int Size { get; }
    ItemStack GetSlot(int index);
    void SetSlot(int index, ItemStack stack);
    bool IsEmpty { get; }
}