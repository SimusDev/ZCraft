using System;

namespace ZCraft.Source.World.View;

using Godot;

public partial class FreeCamera : Camera3D
{
	[Export] public float MoveSpeed = 10f;
	[Export] public float SprintMultiplier = 3f;
	[Export] public float MouseSensitivity = 0.002f;
	[Export] public float MaxPitch = 89f;

	private float _yaw;
	private float _pitch;

	public override void _Ready()
	{
		var rot = Rotation;
		_pitch = rot.X;
		_yaw = rot.Y;

		if (IsMultiplayerAuthority())
			Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!IsMultiplayerAuthority()) {return;}

		if (@event.IsActionPressed("ui_cancel"))
		{
			Input.MouseMode = Input.MouseMode == Input.MouseModeEnum.Captured
				? Input.MouseModeEnum.Visible
				: Input.MouseModeEnum.Captured;
			return;
		}

		if (@event is InputEventMouseMotion motion && Input.MouseMode == Input.MouseModeEnum.Captured)
		{
			_yaw -= motion.Relative.X * MouseSensitivity;
			_pitch -= motion.Relative.Y * MouseSensitivity;

			float maxPitchRad = Mathf.DegToRad(MaxPitch);
			_pitch = Mathf.Clamp(_pitch, -maxPitchRad, maxPitchRad);

			Rotation = new Vector3(_pitch, _yaw, 0f);
		}
	}

	public override void _Process(double delta)
	{
		if (!IsMultiplayerAuthority()) {return;}

		if (Input.MouseMode != Input.MouseModeEnum.Captured)
			return;

		Vector3 input = Vector3.Zero;
		if (Input.IsActionPressed("move_forward")) input.Z -= 1f;
		if (Input.IsActionPressed("move_backward"))    input.Z += 1f;
		if (Input.IsActionPressed("move_left"))    input.X -= 1f;
		if (Input.IsActionPressed("move_right"))   input.X += 1f;
		if (Input.IsActionPressed("move_up"))      input.Y += 1f;
		if (Input.IsActionPressed("move_down"))    input.Y -= 1f;

		if (input == Vector3.Zero) return;

		input = input.Normalized();

		float speed = MoveSpeed;
		if (Input.IsActionPressed("sprint"))
			speed *= SprintMultiplier;

		Vector3 worldDir = (GlobalTransform.Basis * input).Normalized();
		GlobalPosition += worldDir * speed * (float)delta;
	}
}
