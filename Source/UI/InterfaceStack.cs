using Godot;
using System.Collections.Generic;

namespace ZCraft.Source.UI
{
    public partial class InterfaceStack : Node
    {
        [Signal] public delegate void ItemActivatedEventHandler(CanvasItem item);
        [Signal] public delegate void ItemDeactivatedEventHandler(CanvasItem item);
        [Signal] public delegate void ItemsChangedEventHandler();
        public int ActiveCount => _activeItems.Count;
        public bool IsAnyActive => ActiveCount > 0;
        public InterfaceStack Instance => _instance;

        private InterfaceStack _instance;
        private List<CanvasItem> _activeItems = new();

        public override void _EnterTree()
        {
            _instance = this;
        }

        public void Activate(CanvasItem item)
        {
            _activeItems.Add(item);
            EmitSignal(SignalName.ItemActivated, item);
            EmitSignal(SignalName.ItemsChanged);
        }

        public void Deactivate(CanvasItem item)
        {
            _activeItems.Remove(item);
            EmitSignal(SignalName.ItemDeactivated, item);
            EmitSignal(SignalName.ItemsChanged);
        }

    }


}
