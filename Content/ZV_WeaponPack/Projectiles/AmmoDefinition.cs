using Godot;

namespace ZCraft.Source.Content.Survival.Projectiles;

/// <summary>
/// Баллистические данные патрона. Плотная структура без ссылок — лежит в реестре
/// <see cref="AmmoRegistry"/> и копируется в слот снаряда при выстреле.
/// </summary>
public struct AmmoDefinition
{
	/// <summary>Строковый идентификатор («7.62x39», «5.56x45»...). Только для регистрации/отладки.</summary>
	public string Id;

	/// <summary>Наружный диаметр пули, м (для площади лобового сечения).</summary>
	public float Caliber;

	/// <summary>Масса пули, кг.</summary>
	public float Mass;

	/// <summary>Дульная скорость, м/с.</summary>
	public float MuzzleVelocity;

	/// <summary>Баллистический коэффициент G1 (безразмерный, типично 0.1–0.6). Больше = меньше снос ветром и замедление.</summary>
	public float BallisticCoefficient;

	/// <summary>Энергия пенетрации при вылете из ствола, Дж (порог пробития материалов).</summary>
	public float PenetrationEnergy;

	/// <summary>Угол рикошета от поверхности, град (меньше = рикошетить легче). От поверхности (глазурь), не от нормали.</summary>
	public float RicochetAngle;

	/// <summary>Вероятность рикошета при касательном угле 0..1 (масштабируется углом).</summary>
	public float RicochetChance;

	/// <summary>Максимальное время жизни снаряда, с (после — детерминированное исчезновение).</summary>
	public float MaxLifetime;

	/// <summary>Максимальная дистанция полёта, м (0 = бесконечно).</summary>
	public float MaxRange;

	/// <summary>Флаг: пуля оставляет видимый трассер (рендерится ярче).</summary>
	public bool IsTracer;

	/// <summary>Базовый урон (для будущей системы здоровья; пока informational).</summary>
	public float Damage;

	/// <summary>Лобовая площадь пули, м² (вычисляется из калибра).</summary>
	public float FrontalArea => Caliber <= 0f ? 0f : Mathf.Pi * (Caliber * 0.5f) * (Caliber * 0.5f);
}
