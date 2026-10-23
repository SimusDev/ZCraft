using Godot;

namespace ZCraft.Source.World.Player
{
    [GlobalClass]
    public partial class PlayerMovementControl : Node, ITickable
    {
        [Export] private CharacterBody3D _body;

        public void PhysicsTick(double delta)
        {

        }

        public void Tick(double delta)
        {

        }

        public override void _Ready()
        {
            if (GetMultiplayerAuthority() == Multiplayer.GetUniqueId())
            {

            }

            else
            {

            }

        }

        public override void _EnterTree()
        {
            if (GetMultiplayerAuthority() == Multiplayer.GetUniqueId())
                Level.FindAbove(this).AddTickable(this);
        }

        public override void _ExitTree()
        {
            if (GetMultiplayerAuthority() == Multiplayer.GetUniqueId())
                Level.FindAbove(this).RemoveTickable(this);
        }

    }
}
