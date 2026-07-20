using Godot;
using System;

namespace Interface
{
    [GlobalClass]
    public partial class SwitchScreenComponent: Node
    {
        [Export] private CanvasItem _initialScreen;
        [Export] private Godot.Collections.Array<CanvasItem> _screens = new();

        [Export] private Godot.Collections.Dictionary<Button, CanvasItem> _buttonHooks = new();

        public void Switch(CanvasItem screen)
        {
            if (!_screens.Contains(screen))
            {
                return;
            }

            foreach (var toHide in _screens)
                toHide.Hide();

            screen.Show();
        }

        public override void _Ready()
        {
            foreach (var screen in _screens)
                screen.Hide();

            if (_initialScreen != null)
                _initialScreen.Show();

            foreach (var pair in _buttonHooks)
                pair.Key.Pressed += () => Switch(pair.Value);
        }
    }
}
