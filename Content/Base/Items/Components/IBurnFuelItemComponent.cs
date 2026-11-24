using System;
using Godot;
using ZCraft.Source.World.Item;

namespace ZCraft.Content.Base.Items.Components;

[GlobalClass]
public partial class IBurnFuelItemComponent: ItemComponent
{
    [Export] int BurnTimeSeconds = 1;
}
