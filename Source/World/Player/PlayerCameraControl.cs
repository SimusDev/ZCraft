using Godot;
using ZCraft.Source.Networking;
using ZCraft.Source.UI;

namespace ZCraft.Source.World.Player
{
    [GlobalClass]
    public partial class PlayerCameraControl : Node3D
    {
        [Export] private Node3D _body;
        [Export] private Camera3D _camera;
        [Export] private float _sensitivity = 1.0f;
        [Export] private float _minPitchDegrees { get; set; } = -89f;
        [Export] private float _maxPitchDegrees { get; set; } = 89f;

        private bool _captured = false;

        public override void _Ready()
        {
            if (NetworkManager.IsServer)
            {
                SetProcessInput(false);
                return;
            }
            
            
            bool isAuth = IsMultiplayerAuthority();
            
            SetProcessInput(isAuth);

            if (!isAuth)
                return;
            
            _camera.MakeCurrent();

            UpdateCapturing();
            InterfaceStack.Get().ActiveChanged += UpdateCapturing;
        }

        public override void _ExitTree()
        {
            if (NetworkManager.IsServer)
                return;
            
            if (IsNodeReady() && IsMultiplayerAuthority())
            {
                InterfaceStack.Get().ActiveChanged -= UpdateCapturing;
            }
        }

        public void UpdateCapturing()
        {
            bool anyInterfaceActive = InterfaceStack.Get().IsAnyActive;
            SetProcessInput(!anyInterfaceActive);

            if (anyInterfaceActive)
            {
                Input.MouseMode = Input.MouseModeEnum.Visible;
            }
            
            else
            {
                Input.MouseMode = Input.MouseModeEnum.Captured;
            }
        }

        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventMouseMotion mouse)
            {
                float actualSens = _sensitivity * 0.0045f;
                RotateY(-mouse.Relative.X * actualSens);

                _camera.RotateX(-mouse.Relative.Y * actualSens);

                _camera.Rotation = new Vector3(
                    Mathf.Clamp(_camera.Rotation.X, Mathf.DegToRad(_minPitchDegrees), Mathf.DegToRad(_maxPitchDegrees)),
                    _camera.Rotation.Y,
                    _camera.Rotation.Z
                    );
            }
        }

    }
}
