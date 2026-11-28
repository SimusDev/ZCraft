using Godot;

namespace ZCraft.Source.Content.Survival.Projectiles;

/// <summary>
/// Рендер всех снарядов одним <see cref="MultiMeshInstance3D"/>: один вызов отрисовки
/// на любой размер армии. Ориентация инстанса — по вектору скорости, длина штриха
/// пропорциональна скорости (имитация трассера), цвет — по флагу трассера патрона.
/// Позиции интерполируются между фикс-тиками (предыдущая → текущая).
/// </summary>
public sealed partial class ProjectileRenderer : Node3D
{
	/// <summary>Базовый размер штриха, м (масштабируется по скорости и калибру).</summary>
	private const float BaseStrokeWidth = 0.045f;

	/// <summary>Множитель длины штриха от скорости, с (0.03 с «длины трассера»).</summary>
	private const float StrokeLengthPerSpeed = 0.03f;

	private MultiMesh _multiMesh;
	private MultiMeshInstance3D _instance;
	private Material _material;

	/// <summary>Число инстансов, под которые выделена память MultiMesh (рост без реаллокаций в кадр).</summary>
	private int _allocated;

	/// <summary>Цвет обычной пули / трассера.</summary>
	private static readonly Color NormalColor = new(1.0f, 0.78f, 0.45f, 1.0f);
	private static readonly Color TracerColor = new(1.0f, 0.35f, 0.12f, 1.0f);

	public override void _Ready()
	{
		_multiMesh = new MultiMesh
		{
			TransformFormat = MultiMesh.TransformFormat.Transform3D,
			UseColors = true,
		};

		// Простой яркий материал без освещения — пуля всегда видима.
		_material = new StandardMaterial3D
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			VertexColorUseAsAlbedo = true,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			BillboardMode = BaseMaterial3D.BillboardModeEnum.Disabled,
		};

		QuadMesh mesh = new()
		{
			Size = new Vector2(1f, 1f),
			Material = _material,
		};
		_multiMesh.Mesh = mesh;

		_instance = new MultiMeshInstance3D
		{
			MultiMesh = _multiMesh,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		};
		AddChild(_instance);
	}

	/// <summary>
	/// Перезапись всех инстансов по состоянию хранилища. Вызывается каждый кадр
	/// (только рендер-данные; симуляция в фикс-тиках отсюда не зависит).
	/// </summary>
	public void Render(ProjectileStore store)
	{
		if (_multiMesh == null)
			return;

		int count = store.Count;

		if (count > _allocated)
		{
			// Растим степенями двойки, чтобы не реаллоковать каждый выстрел.
			int newAlloc = _allocated == 0 ? 256 : _allocated;
			while (newAlloc < count)
				newAlloc *= 2;

			_multiMesh.InstanceCount = newAlloc;
			_allocated = newAlloc;
		}

		// Интерполяция между фикс-тиками: alpha ∈ [0,1), где 1 = конец текущего тика.
		float alpha = (float)Engine.GetPhysicsInterpolationFraction();

		for (int i = 0; i < count; i++)
		{
			Vector3 prev = store.PreviousPosition(i);
			Vector3 curr = store.Position(i);
			Vector3 position = prev.Lerp(curr, alpha);

			Vector3 velocity = store.Velocity(i);
			float speed = velocity.Length();

			// Штрих: QuadMesh в плоскости, повёрнутой по скорости; длина ∝ скорости.
			float length = Mathf.Max(speed * StrokeLengthPerSpeed, 0.12f);
			float width = BaseStrokeWidth;

			Transform3D transform = BuildStrokeTransform(position, velocity, width, length);
			_multiMesh.SetInstanceTransform(i, transform);

			bool tracer = store.Tracer(i);
			Color color = tracer ? TracerColor : NormalColor;
			// Плавное угасание по краям жизни не делаем — дешевле гасить по дистанции.
			_multiMesh.SetInstanceColor(i, color);
		}

		// Скрываем неиспользуемые инстансы нулевым масштабом, не трогая InstanceCount.
		for (int i = count; i < _allocated; i++)
		{
			_multiMesh.SetInstanceTransform(i, new Transform3D(Basis.Identity, new Vector3(0, -99999f, 0)));
		}
	}

	/// <summary>
	/// Трансформация штриха: локальные оси X (ширина), Y (длина вдоль скорости), Z (толщина).
	/// QuadMesh лежит в плоскости XY — длина по Y, взгляд камеры не учитываем (не billboard,
	/// но штрих виден с большинства углов; для трассера это приемлемо).
	/// </summary>
	private static Transform3D BuildStrokeTransform(Vector3 position, Vector3 velocity, float width, float length)
	{
		Vector3 forward = velocity.LengthSquared() > 0.0001f ? velocity.Normalized() : Vector3.Forward;

		// Ось Y инстанса совпадает с направлением полёта.
		Vector3 yAxis = forward;
		Vector3 upHint = Mathf.Abs(yAxis.Dot(Vector3.Up)) > 0.98f ? Vector3.Forward : Vector3.Up;
		Vector3 xAxis = upHint.Cross(yAxis).Normalized();
		Vector3 zAxis = xAxis.Cross(yAxis).Normalized();

		Basis basis = new(xAxis, yAxis, zAxis);
		basis = basis.Scaled(new Vector3(width, length, width));

		return new Transform3D(basis, position);
	}
}
