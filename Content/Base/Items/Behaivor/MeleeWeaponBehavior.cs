using System;
using ZCraft.Source.World.Item;

namespace ZCraft.Content.Base.Items.Behaivor;

public partial class MeleeWeaponBehavior : ItemBehavior
{
    public override IItemBehaviorSystem Build()
    {
        return new MeleeWeaponBehaviorSystem();
    }
}
