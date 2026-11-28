using Godot;

namespace ZCraft.Source.Content.Survival.Projectiles;

/// <summary>
/// Контакт снаряда с поверхностью за тик: результат рейкаста, резолюция и данные для
/// события <see cref="ProjectileSystem.OnImpact"/> (урон, FX, репликация клиенту).
/// Плотная структура без аллокаций.
/// </summary>
public readonly struct ProjectileContact
{
	/// <summary>Сетевой id снаряда (0 если снаряд локальный/серверный ещё не проанонсирован).</summary>
	public readonly ulong NetId;

	public readonly int AmmoIndex;
	public readonly int OwnerPeer;

	/// <summary>Точка контакта в мире.</summary>
	public readonly Vector3 Point;

	/// <summary>Нормаль поверхности в точке контакта.</summary>
	public readonly Vector3 Normal;

	/// <summary>Скорость снаряда до контакта, м/с.</summary>
	public readonly Vector3 Velocity;

	/// <summary>Скорость по нормали (|v·n|), м/с — для энергии пенетрации и FX.</summary>
	public readonly float NormalSpeed;

	/// <summary>Базовый урон патрона.</summary>
	public readonly float Damage;

	public readonly ProjectileMaterial Material;

	/// <summary>Коллайдер (может быть null, если физика уже удалила тело).</summary>
	public readonly Node Collider;

	public readonly ProjectileImpactKind Kind;

	/// <summary>Скорость после контакта (для продолжения полёта при пробитии/рикошете).</summary>
	public readonly Vector3 VelocityAfter;

	public ProjectileContact(
		ulong netId, int ammoIndex, int ownerPeer,
		Vector3 point, Vector3 normal, Vector3 velocity, float normalSpeed,
		float damage, in ProjectileMaterial material, Node collider,
		ProjectileImpactKind kind, Vector3 velocityAfter)
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
		VelocityAfter = velocityAfter;
	}

	/// <summary>Преобразует контакт в информационную структуру события Impact.</summary>
	public ProjectileHitInfo ToHitInfo() => new(
		NetId, AmmoIndex, OwnerPeer,
		Point, Normal, Velocity, NormalSpeed,
		Damage, Material, Collider, Kind);
}
