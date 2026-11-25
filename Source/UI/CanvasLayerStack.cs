using System;
using Godot;

namespace ZCraft.Source.UI;

public partial class CanvasLayerStack : CanvasLayer
{
    private InterfaceStack _interfaceStack = InterfaceStack.Get();
    public override void _Ready()
    {
        if (Visible)
        {
            _interfaceStack.AddActive();
        }

        VisibilityChanged += OnVisibilityChanged;
    }

    private void OnVisibilityChanged()
    {
        if (Visible)
            _interfaceStack.AddActive();
        else
            _interfaceStack.RemoveActive();
    }

    public override void _ExitTree()
    {
        if (Visible)
            _interfaceStack.RemoveActive();
    }
}
