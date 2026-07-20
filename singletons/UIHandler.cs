using Godot;
using System;

namespace Singletons
{
    public partial class UIHandler : Node
    {
        private static UIHandler _instance;
        public static UIHandler Instance => _instance;

        private int _activity = 0;

        [Signal] public delegate void OnGlobalActivityChangedEventHandler();

        private void _AddGlobalActivity()
        {
            _activity++;
            EmitSignal(SignalName.OnGlobalActivityChanged);
        }

        private void _RemoveGlobalActivity()
        {
            _activity--;
            EmitSignal(SignalName.OnGlobalActivityChanged);
        }

        public static bool HasGlobalActivity()
        {
            return Instance._activity > 0;
        }

        public static void AddGlobalActivity()
        {
            Instance._AddGlobalActivity();
        }

        public static void RemoveGlobalActivity()
        {
            Instance._RemoveGlobalActivity();
        }

    }

}
