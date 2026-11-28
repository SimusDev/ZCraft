using Godot;

namespace ZCraft.Source.Content.Survival.Projectiles;

/// <summary>
/// Итог контакта снаряда с поверхностью. Передаётся в событие
/// <see cref="ProjectileSystem.OnImpact"/> и в <see cref="IProjectileDamageable"/>.
/// </summary>
public readonly struct ProjectileHitInfo
{
	public readonly ulong NetId;
	public readonly int AmmoIndex;
	public readonly int OwnerPeer;
	public readonly Vector3 Point;
	public readonly Vector3 Normal;
	/// <summary>Скорость пули в момент контакта, м/с.</summary>
	public readonly Vector3 Velocity;
	/// <summary>Скорость, направленная внутрь поверхности (|v·n|), м/с.</summary>
	public readonly float NormalSpeed;
	public readonly float Damage;
	public readonly ProjectileMaterial Material;
	public readonly Node Collider;

	/// <summary>Тип результата контакта (для FX и логики).</summary>
	public readonly ProjectileImpactKind Kind;

	public ProjectileHitInfo(
		ulong netId, int ammoIndex, int ownerPeer,
		Vector3 point, Vector3 normal, Vector3 velocity, float normalSpeed,
		float damage, in ProjectileMaterial material, Node collider, ProjectileImpactKind kind)
	{
		NetId = netId;
		AmmoIndex = ammoIndex;
		OwnerPeer = ownerPeer;
		Point = point;
		Normal = normal;
		Velocity = velocity;
		NormalSpeed = normalSpeed;
		Damage = damage;
		Material = material;
		Collider = collider;
		Kind = kind;
	}
}

/// <summary>Вид события контакта (для FX).</summary>
public enum ProjectileImpactKind : byte
{
	/// <summary>Пуля остановлена материалом (убита).</summary>
	Hit = 0,
	/// <summary>Пуля пробила материал и продолжает полёт.</summary>
	Penetrated = 1,
	/// <summary>Пуля отрикошетила и продолжает полёт.</summary>
	Ricocheted = 2,
	/// <summary>Пуля рикошетом остановлена (полный отказ).</summary>
	RicochetStopped = 3,
}

/// <summary>Материал поверхности для баллистики: сопротивление пробитию и рикошет.</summary>
public struct ProjectileMaterial
{
	public string Id;

	/// <summary>Энергия, Дж,needed для полного прохождения при ударе строго по нормали (0 = прозрачно).</summary>
	public float PenetrationResistance;

	/// <summary>Критический угол рикошета, град от ПОВЕРХНОСТИ (0 = никогда, 90 = почти всегда).</summary>
	public float RicochetAngle;

	/// <summary>Базовая вероятность рикошета при угле меньше критического, 0..1.</summary>
	public float RicochetChance;

	/// <summary>Доля скорости, сохраняемая при рикошете, 0..1 (потери на деформацию).</summary>
	public float Restitution;

	/// <summary>Дефолт, если коллайдер не сообщил материал: бетон-средний.</summary>
	public static ProjectileMaterial Default => new()
	{
		Id = "default",
		PenetrationResistance = 800f,
		RicochetAngle = 18f,
		RicochetChance = 0.5f,
		Restitution = 0.55f,
	};
}

/// <summary>Коллайдер может сообщить свой материал для симуляции (реализуй на игроке/стене).</summary>
public interface IProjectileMaterialProvider
{
	ProjectileMaterial GetProjectileMaterial();
}

/// <summary>Коллайдер получает урон от пули (только серверная симуляция вызывает).</summary>
public interface IProjectileDamageable
{
	void OnProjectileHit(in ProjectileHitInfo hit);
}
