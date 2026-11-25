using System;
using Godot;
using ZCraft.Source.World.Item;

namespace ZCraft.Content.Base.Items.Components;

[GlobalClass]
public partial class IUIIconLayerItemComponent: ClientItemComponent
{
    [Export] public Texture2D Texture = null;
    [Export] public Color Color = new(1, 1, 1, 1);
}
