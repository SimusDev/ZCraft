using Godot;

namespace ZCraft.Source.Content.Survival.Projectiles;

/// <summary>
/// Баллистическое ядро: сила сопротивления (приближение кривой G1 по числу Маха),
/// гравитация, ветер, плотность воздуха по высоте. Чистая статика без аллокаций —
/// вызывается из симулятора на каждый суб-шаг каждого снаряда.
/// </summary>
public static class ProjectileBallistics
{
	/// <summary>Коэффициент сопротивления при нуле скорости (дозвуковая зона G1).</summary>
	private const float CdSubsonic = 0.200f;

	/// <summary>Пик сопротивления у скорости звука (трансзвуковой «горб»).</summary>
	private const float CdTransonicPeak = 0.390f;

	/// <summary>Сопротивление на сверхзвуковых скоростях (спад с ростом М).</summary>
	private const float CdHypersonic = 0.260f;

	/// <summary>
	/// Коэффициент сопротивления Cd по числу Маха — кусочно-линейное приближение кривой G1:
	/// плоское дозвуковое плато, пик ~0.39 у M≈1.0–1.1, затухание на сверхзвуке.
	/// </summary>
	public static float DragCoefficient(float mach)
	{
		float m = Mathf.Abs(mach);

		if (m < 0.8f)
			return CdSubsonic;

		if (m < 1.1f)
		{
			// Резкий трансзвуковой подъём к пику.
			float t = (m - 0.8f) / 0.3f;
			return Mathf.Lerp(CdSubsonic, CdTransonicPeak, t);
		}

		if (m < 2.0f)
		{
			// Спад после пика.
			float t = (m - 1.1f) / 0.9f;
			return Mathf.Lerp(CdTransonicPeak, 0.310f, t);
		}

		// Дальний сверхзвук — медленное затухание к асимптоте.
		float t2 = Mathf.Min((m - 2.0f) / 3.0f, 1f);
		return Mathf.Lerp(0.310f, CdHypersonic, t2);
	}

	/// <summary>
	/// Ускорение сопротивления воздуха (м/с²), направленное против вектора
	/// скорости относительно воздуха (velocity − wind). Плотность по высоте.
	/// a = 0.5 · ρ · Cd(M) · A · v² / m
	/// </summary>
	public static Vector3 DragAcceleration(in AmmoDefinition ammo, Vector3 velocity, float worldY)
	{
		Vector3 airVelocity = velocity - ProjectileEnvironment.Wind;
		float speed = airVelocity.Length();

		if (speed < 0.0001f)
			return Vector3.Zero;

		float rho = ProjectileEnvironment.AirDensity(worldY);
		float mach = speed / ProjectileEnvironment.SpeedOfSound;
		float cd = DragCoefficient(mach);
		float area = ammo.FrontalArea;
		float mass = ammo.Mass > 0f ? ammo.Mass : 0.001f;

		// Баллистический коэффициент масштабирует сопротивление: больше BC = меньше замедление.
		float bcScale = ammo.BallisticCoefficient > 0f ? 0.35f / ammo.BallisticCoefficient : 1f;

		float deceleration = 0.5f * rho * cd * area * speed * speed / mass * bcScale;
		return -airVelocity * (deceleration / speed);
	}

	/// <summary>
	/// Полный шаг интегрирования (полунеявный Эйлер): гравитация + сопротивление.
	/// Вызывается на каждый суб-шаг;dt — длина суб-шага в секундах.
	/// </summary>
	public static void Integrate(in AmmoDefinition ammo, ref Vector3 position, ref Vector3 velocity, float dt)
	{
		Vector3 acceleration = DragAcceleration(in ammo, velocity, position.Y);
		acceleration.Y -= ProjectileEnvironment.Gravity;

		velocity += acceleration * dt;
		position += velocity * dt;
	}
}
