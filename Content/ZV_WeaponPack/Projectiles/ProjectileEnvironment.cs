using Godot;

namespace ZCraft.Source.Content.Survival.Projectiles;

/// <summary>
/// Глобальное окружение симуляции снарядов: ветер, гравитация, плотность воздуха.
/// Сервер — источник истины; клиент получает ветер через снапшоты <see cref="ProjectileNetwork"/>.
/// </summary>
public static class ProjectileEnvironment
{
	/// <summary>Ветер, м/с (+X восток, +Y вверх, +Z юг). Действует на пулю через относительную скорость.</summary>
	public static Vector3 Wind = Vector3.Zero;

	/// <summary>Ускорение свободного падения, м/с². В Godot +Y вверх — гравитация идёт в −Y.</summary>
	public static float Gravity = 9.80665f;

	/// <summary>Плотность воздуха у уровня моря, кг/м³.</summary>
	public static float SeaLevelDensity = 1.225f;

	/// <summary>Масштабная высота атмосферы, м (экспоненциальное затухание плотности с высотой).</summary>
	public static float ScaleHeight = 8500f;

	/// <summary>Скорость звука у уровня моря при 15 °C, м/с (нужна для числа Маха в модели сопротивления).</summary>
	public static float SpeedOfSound = 340.3f;

	/// <summary>Плотность воздуха на высоте worldY (уровень моря = 0).</summary>
	public static float AirDensity(float worldY)
	{
		if (worldY <= 0f)
			return SeaLevelDensity;

		return SeaLevelDensity * Mathf.Exp(-worldY / ScaleHeight);
	}
}
