using Godot;

namespace ZCraft.Source.World.Player
{
    [GlobalClass]
    public partial class PlayerMovementControl : Node, ITickable
    {
        [Export] private CharacterBody3D _body;
        [Export] private float _gravity = 9.8f;
        [Export] private float _speed = 5.0f;

        public void PhysicsTick(double delta)
        {
            if (!_body.IsOnFloor())
            {
                _body.Velocity = new Vector3(
                    _body.Velocity.X,
                    _body.Velocity.Y - _gravity * (float)delta,
                    _body.Velocity.Z
                    );
            }

            Vector2 inputDir = Input.GetVector("move_left", "move_right", 
                "move_forward", "move_backward");

            Vector3 direction = (_body.Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

            if (inputDir != Vector2.Zero)
            {
                _body.Velocity = new Vector3(
                    direction.X * _speed,
                    _body.Velocity.Y,
                    direction.Y * _speed
                    );
            }

            else
            {
                _body.Velocity = new Vector3(
                    0,
                    _body.Velocity.Y,
                    0
                    );
            }

            _body.MoveAndSlide();
        }

        public void Tick(double delta)
        {

        }

        public override void _EnterTree()
        {
            Level.FindAbove(this).AddTickable(this);
        }

        public override void _ExitTree()
        {
            Level.FindAbove(this).RemoveTickable(this);
        }



    }
}
