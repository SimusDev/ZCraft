#nullable enable

using Godot;

namespace ZCraft.AIGenerated;

public partial class ExplosionPreview : Node3D
{
	private const string Panel = "Interface/Panel/Margins/Controls/";
	private ExplosionVfx _explosion = null!;
	private Camera3D _camera = null!;
	private MeshInstance3D _stage = null!;
	private HSlider _strength = null!;
	private Label _number = null!;
	private Label _result = null!;

	public override void _Ready()
	{
		_explosion = GetNode<ExplosionVfx>("Explosion");
		_camera = GetNode<Camera3D>("Camera");
		_stage = GetNode<MeshInstance3D>("Stage");
		_strength = GetNode<HSlider>(Panel + "Strength");
		_number = GetNode<Label>(Panel + "StrengthHeader/Number");
		_result = GetNode<Label>(Panel + "Status");
		_strength.ValueChanged += value => _number.Text = value.ToString("0");
		_strength.DragEnded += changed => { if (changed) Replay(); };
		_explosion.Finished += () => _result.Text = "Эффект завершён";
		SelectStrength(12);
	}

	public void SelectStrength(float value)
	{
		_strength.Value = value;
		Replay();
	}

	public void Replay()
	{
		_explosion.Play((float)_strength.Value);
		float radius = _explosion.VisualRadius;
		float height = _explosion.VisualHeight;
		float frame = Mathf.Max(2.3f, Mathf.Max(height * 1.25f, radius * 3.64f));
		float pad = Mathf.Max(1.25f, radius * 3.64f);
		_stage.Scale = new Vector3(pad, 1, pad);

		Vector3 towardCamera = new Vector3(4.6f, 3.75f, 8.7f).Normalized();
		Vector3 right = Vector3.Up.Cross(towardCamera).Normalized();
		Vector3 target = Vector3.Up * (height * .40f) + right * (frame * .25f);
		_camera.Size = frame;
		_camera.LookAtFromPosition(target + towardCamera * (5 + frame * 1.7f), target);
		_number.Text = _strength.Value.ToString("0");
		_result.Text = $"Длительность {_explosion.Duration:0.0} с";
	}
}
