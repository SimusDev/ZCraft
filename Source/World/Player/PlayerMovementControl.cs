using Godot;

namespace ZCraft.Source.World.Player
{
    [GlobalClass]
    public partial class PlayerMovementControl : Node, IFixedTickable
    {
        [Export] private CharacterBody3D _body;
        [Export] private float _gravity = 9.8f;
        [Export] private float _speed = 5.0f;
        [Export] private float _jumpForce = 4.5f;

        [Export] private Node3D _head;

        private bool _isNetworkAuthority = false;

        public void OnFixedTick(double delta)
        {
            if (_isNetworkAuthority)
            {
                if (_body.IsOnFloor())
                {
                    if (Input.IsActionJustPressed("jump"))
                        _body.Velocity = new Vector3(
                            _body.Velocity.X,
                            _jumpForce,
                            _body.Velocity.Z
                        );
                }

                else
                {
                    _body.Velocity = new Vector3(
                        _body.Velocity.X,
                        _body.Velocity.Y - _gravity * (float)delta,
                        _body.Velocity.Z
                    );
                }

                Vector2 inputDir = Input.GetVector("move_left", "move_right",
                    "move_forward", "move_backward");

                Vector3 direction = Vector3.Zero;
                if (inputDir != Vector2.Zero)
                {
                    direction = (_head.Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
                }

                if (inputDir != Vector2.Zero)
                {
                    _body.Velocity = new Vector3(
                        direction.X * _speed,
                        _body.Velocity.Y,
                        direction.Z * _speed
                    );
                }
                else
                {
                    var horiz = new Vector3(_body.Velocity.X, 0, _body.Velocity.Z);
                    horiz = horiz.Lerp(Vector3.Zero, 10f * (float)delta);
                    _body.Velocity = new Vector3(horiz.X, _body.Velocity.Y, horiz.Z);
                }
            }


            _body.MoveAndSlide();
        }

        public override void _Ready()
        {
            _isNetworkAuthority = IsMultiplayerAuthority();
        }

        public override void _EnterTree()
        {
            ZCraftWorld.FixedTickables.Add(this);
        }

        public override void _ExitTree()
        {
            ZCraftWorld.FixedTickables.Remove(this);
        }

    }
}
