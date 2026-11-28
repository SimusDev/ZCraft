using Godot;
using System.Collections.Generic;

namespace ZCraft.Source.Content.Survival.Projectiles;

/// <summary>
/// Детерминированная симуляция одного тика: суб-шаги интегрирования, сегментные рейкасты,
/// резолюция контакта (пробитие/рикошет/остановка). Вызывается только на сервере
/// (авторитарная симуляция), клиент рисует интерполированные снапшоты.
/// </summary>
public static class ProjectileSimulator
{
	/// <summary>Максимальная длина одного суб-шага, м (пуля ~900 м/с при 40 тик/с = 22 м/тик).</summary>
	public const float MaxSubStepLength = 2.0f;

	/// <summary>Жёсткий потолок суб-шагов на тик (защита от «взрыва» численности).</summary>
	private const int MaxSubSteps = 16;

	/// <summary>Маска коллизий по умолчанию — слои 1 (мир) и 2 (сущности).</summary>
	public const uint DefaultCollisionMask = 0b11;

	private static readonly List<Rid> _excludeScratch = new(4);
	private static PhysicsRayQueryParameters3D _query;

	/// <summary>
	/// Шаг симуляции всех живых снарядов на dt секунд. Контакты складываются в
	/// <paramref name="hits"/>, снаряды умершие без контакта (дистанция/время) —
	/// в <paramref name="despawned"/> (оба списка очищаются в начале).
	/// Уничтоженные снаряды удаляются swap-remove'ом; итерация идёт с конца, поэтому это безопасно.
	/// </summary>
	public static void Step(
		ProjectileStore store,
		PhysicsDirectSpaceState3D space,
		float dt,
		List<ProjectileContact> hits,
		List<ulong> despawned)
	{
		hits.Clear();
		despawned.Clear();

		if (store.Count == 0 || space == null)
			return;

		_query ??= new PhysicsRayQueryParameters3D();
		_query.CollisionMask = DefaultCollisionMask;
		_query.HitFromInside = true;
		_query.CollideWithAreas = false;

		for (int i = store.Count - 1; i >= 0; i--)
		{
			int ammoIndex = store.AmmoIndex(i);
			ref readonly AmmoDefinition ammo = ref AmmoRegistry.Get(ammoIndex);

			Vector3 position = store.Position(i);
			Vector3 velocity = store.Velocity(i);
			Vector3 startPos = position;

			float speed = velocity.Length();
			int subSteps = Mathf.Clamp(
				Mathf.CeilToInt(speed * dt / MaxSubStepLength), 1, MaxSubSteps);
			float subDt = dt / subSteps;

			bool destroyed = false;
			int contactKilledIndex = -1; // индекс, убитый контактом (его despawn не дублируем)

			for (int s = 0; s < subSteps && !destroyed; s++)
			{
				Vector3 from = position;

				ProjectileBallistics.Integrate(in ammo, ref position, ref velocity, subDt);

				Vector3 segment = position - from;
				float segmentLength = segment.Length();
				if (segmentLength < 0.0001f)
					continue;

				// Сегментный рейкаст от старой к новой позиции — тонкие стены не проскакиваются.
				_excludeScratch.Clear();
				Rid ownerBody = store.OwnerBody(i);
				if (ownerBody.IsValid)
					_excludeScratch.Add(ownerBody);

				_query.From = from;
				_query.To = position;
				_query.Exclude = _excludeScratch;

				Dictionary result = space.IntersectRay(_query);
				if (result.Count == 0)
					continue;

				Vector3 point = (Vector3)result["position"];
				Vector3 normal = (Vector3)result["normal"];
				Node collider = result["collider"] as Node;

				float normalSpeed = Mathf.Abs(velocity.Dot(normal));
				ProjectileMaterial material = ResolveMaterial(collider);
				ProjectileImpactKind kind = ResolveImpact(
					in ammo, material, velocity, normal, normalSpeed, out Vector3 newVelocity);

				ulong netId = store.NetId(i);
				int ownerPeer = store.OwnerPeer(i);

				hits.Add(new ProjectileContact(
					netId, ammoIndex, ownerPeer,
					point, normal, velocity, normalSpeed,
					ammo.Damage, material, collider, kind, newVelocity));

				if (kind == ProjectileImpactKind.Ricocheted || kind == ProjectileImpactKind.Penetrated)
				{
					// Полёт продолжается: скорость скорректирована, точку слегка отодвигаем от поверхности.
					velocity = newVelocity;
					position = point + velocity.Normalized() * 0.01f;
				}

				else
				{
					destroyed = true;
					contactKilledIndex = i;
				}
			}

			store.Position(i) = position;
			store.Velocity(i) = velocity;
			store.DistanceFlown(i) += (position - startPos).Length();

			if (!destroyed)
			{
				store.Age(i) += dt;

				if (ammo.MaxRange > 0f && store.DistanceFlown(i) >= ammo.MaxRange)
					destroyed = true;
				else if (ammo.MaxLifetime > 0f && store.Age(i) >= ammo.MaxLifetime)
					destroyed = true;
			}

			if (destroyed)
			{
				// Контакт уже попал в hits — этот case только для «тихих» смертей
				// (дистанция/время). Снаряды, убитые контактом, помечены через break выше.
				if (contactKilledIndex != i)
					despawned.Add(store.NetId(i));

				store.Remove(i);
			}
		}
	}

	/// <summary>Материал коллайдера: интерфейс на объекте или на родителе, иначе дефолт.</summary>
	public static ProjectileMaterial ResolveMaterial(Node collider)
	{
		Node node = collider;
		for (int depth = 0; node != null && depth < 4; depth++)
		{
			if (node is IProjectileMaterialProvider provider)
				return provider.GetProjectileMaterial();

			node = node.GetParent();
		}

		return ProjectileMaterial.Default;
	}

	/// <summary>
	/// Резолюция контакта: а) рикошет при касательном угле; б) пробитие при достаточной
	/// кинетической энергии нормальной составляющей; в) иначе остановка.
	/// </summary>
	public static ProjectileImpactKind ResolveImpact(
		in AmmoDefinition ammo,
		in ProjectileMaterial material,
		Vector3 velocity,
		Vector3 normal,
		float normalSpeed,
		out Vector3 newVelocity)
	{
		newVelocity = velocity;
		float speed = velocity.Length();

		if (speed < 0.01f)
			return ProjectileImpactKind.Hit;

		// Угол касания к ПОВЕРХНОСТИ, град: 0 = летит вдоль поверхности, 90 = лобовой.
		// Угол к нормали = acos(|v·n|/|v|); к поверхности = 90° − угол_к_нормали.
		float cosToNormal = Mathf.Clamp(Mathf.Abs(velocity.Dot(normal)) / speed, 0f, 1f);
		float grazingAngle = 90f - Mathf.RadToDeg * Mathf.Acos(cosToNormal);

		// ---- Рикошет ----
		float criticalRicochet = Mathf.Min(material.RicochetAngle, ammo.RicochetAngle);
		if (grazingAngle < criticalRicochet && speed > 30f)
		{
			// Вероятность падает с ростом угла к критическому (к критическому → 0).
			float angleFactor = 1f - grazingAngle / Mathf.Max(criticalRicochet, 0.01f);
			float chance = material.RicochetChance * ammo.RicochetChance * angleFactor;

			if (RandomFloat() < chance)
			{
				Vector3 reflected = velocity - 2f * velocity.Dot(normal) * normal;
				newVelocity = reflected * material.Restitution;

				if (newVelocity.Length() < 15f)
					return ProjectileImpactKind.RicochetStopped;

				return ProjectileImpactKind.Ricocheted;
			}
		}

		// ---- Пробитие ----
		float kineticEnergy = 0.5f * ammo.Mass * normalSpeed * normalSpeed;
		float resistance = material.PenetrationResistance / AngleResistanceFactor(grazingAngle);

		if (kineticEnergy >= resistance)
		{
			// Остаточная энергия → остаточная нормальная скорость.
			float residual = Mathf.Sqrt(
				Mathf.Max(kineticEnergy - resistance, 0f) * 2f / Mathf.Max(ammo.Mass, 0.0001f));

			// Тангенциальная составляющая сохраняется почти полностью (×0.9),
			// нормальная — с редукцией до остаточной по величине, внутрь поверхности.
			Vector3 tangential = (velocity - velocity.Dot(normal) * normal) * 0.9f;
			Vector3 inward = normal * -residual;
			newVelocity = tangential + inward;

			if (newVelocity.Length() < 10f)
				return ProjectileImpactKind.Hit;

			return ProjectileImpactKind.Penetrated;
		}

		return ProjectileImpactKind.Hit;
	}

	/// <summary>
	/// Множитель сопротивления по углу: лобовое (90° к поверхности) → 1.0,
	/// касательное — до ×6.6 (пуля проходит большой косой путь внутри материала).
	/// </summary>
	private static float AngleResistanceFactor(float grazingAngle)
	{
		float sin = Mathf.Sin(Mathf.DegToRad * Mathf.Max(grazingAngle, 0f));
		return Mathf.Max(sin, 0.15f);
	}

	/// <summary>Сид ГПСЧ (детерминизм рикошетов между сервером и клиентом не требуется — клиент не резолвит).</summary>
	private static uint _rngState = 0x9E3779B9u;

	public static void Seed(ulong seed) => _rngState = (uint)(seed | 1u);

	/// <summary>xorshift32 без аллокаций.</summary>
	private static float RandomFloat()
	{
		_rngState ^= _rngState << 13;
		_rngState ^= _rngState >> 17;
		_rngState ^= _rngState << 5;
		return (_rngState & 0xFFFFFF) / 16777216f;
	}
}
