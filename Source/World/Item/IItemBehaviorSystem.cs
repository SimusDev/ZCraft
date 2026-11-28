using Godot;

namespace ZCraft.Source.World.Item;

public interface IItemBehaviorSystem
{
    void OnReady(ItemBehaviorContext context);
    void OnDestroy(ItemBehaviorContext context);
    void OnInput(ItemBehaviorContext context, InputEvent @event);
    void Tick(ItemBehaviorContext context, double delta);
    void FixedTick(ItemBehaviorContext context, double delta);

}
