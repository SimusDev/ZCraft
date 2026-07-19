using Godot;
using System;

namespace Singletons
{
    public partial class UIHandler : Node
    {
        private static UIHandler _instance;
        public UIHandler Instance => _instance;

        private System.Collections.Generic.List<AudioStreamPlayer> _playPool = new();

        const int PlayPoolSize = 32;

        public override void _Ready()
        {
            _instance = this;

            for (int i = 0;  i < PlayPoolSize; i++)
            {
                var player = new AudioStreamPlayer();
                player.Finished += () => OnPlayerFinished(player);
                _playPool.Add(player);
            }
        }

        private void OnPlayerFinished(AudioStreamPlayer player)
        {

        }
    }

}
