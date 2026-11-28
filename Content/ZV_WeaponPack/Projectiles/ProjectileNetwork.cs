using Godot;
using System.Collections.Generic;
using ZCraft.Source.Networking;
using ZCraft.Source.Serialization;

namespace ZCraft.Source.Content.Survival.Projectiles;

/// <summary>
/// Типы сообщений пакета снарядов. Весь трафик идёт через один
/// <see cref="INetworkReplicationObject"/> с NetworkId «ZVProjectiles».
/// </summary>
public enum ProjectileMessageType : byte
{
	/// <summary>Reliable: появление снарядов (сервер → клиенты).</summary>
	Spawn = 0,

	/// <summary>Reliable: точка/вид контакта (сервер → клиенты, для FX).</summary>
	Impact = 1,

	/// <summary>UnreliableOrdered: периодический снапшот позиций (сервер → клиенты).</summary>
	Snapshot = 2,

	/// <summary>Reliable: удаление снаряда без контакта (конец жизни/дистанции).</summary>
	Despawn = 3,
}

/// <summary>
/// Сетевой мост системы снарядов: сервер рассылает Spawn/Impact/Despawn по Reliable
/// и Snapshot по UnreliableOrdered; клиент применяет снапшоты к локальному хранилищу
/// (позиции интерполируются рендерером). Клиентские сообщения вхлоп игнорируются —
/// симуляция сервер-авторитетная.
/// </summary>
public sealed partial class ProjectileNetwork : Node, INetworkReplicationObject
{
	/// <summary>Стабильный сетевой id объекта-моста (одинаков на сервере и клиенте).</summary>
	public static readonly NetworkId ObjectId = new("ZVProjectiles");

	/// <summary>Частота отправки снапшотов, Гц.</summary>
	public const double SnapshotRate = 20.0;

	/// <summary>Порог «рваной» коррекции: если ошибка позиции больше — телепортируем сразу.</summary>
	public const float SnapThreshold = 8.0f;

	private ProjectileSystem _system;
	private double _snapshotTimer;

	/// <summary>Очередь контактов, ожидающих отправки (наполняется симуляцией).</summary>
	private readonly List<ProjectileContact> _pendingImpacts = new(32);

	/// <summary>Id снарядов, удалённых без контакта, ожидающие отправки.</summary>
	private readonly List<ulong> _pendingDespawns = new(16);

	public void Init(ProjectileSystem system) => _system = system;

	public override void _EnterTree()
	{
		NetworkReplicator.AddObject(this, ObjectId);
	}

	public override void _ExitTree()
	{
		NetworkReplicator.RemoveObject(this);
	}

	// ------------------------------------------------------------------
	//  Сервер → клиенты
	// ------------------------------------------------------------------

	/// <summary>Reliable-анонс нового снаряда всем клиентам.</summary>
	public void BroadcastSpawn(ulong netId, int ammoIndex, int ownerPeer, Vector3 position, Vector3 velocity)
	{
		if (!NetworkManager.IsServer || NetworkManager.Peers.Count == 0)
			return;

		BytePacker packer = BytePackerPool.Rent();
		try
		{
			packer.WriteByte((byte)ProjectileMessageType.Spawn);
			packer.WriteVarULong(netId);
			packer.WriteVarInt(ammoIndex);
			packer.WriteVarInt(ownerPeer);
			packer.WriteVector3(position);
			packer.WriteVector3(velocity);

			SendToAll(packer, NetworkManager.NetChannel.Reliable);
		}
		finally
		{
			BytePackerPool.Return(packer);
		}
	}

	/// <summary>Reliable-анонс контакта (FX на клиентах).</summary>
	public void BroadcastImpact(in ProjectileContact contact)
	{
		if (!NetworkManager.IsServer || NetworkManager.Peers.Count == 0)
			return;

		BytePacker packer = BytePackerPool.Rent();
		try
		{
			packer.WriteByte((byte)ProjectileMessageType.Impact);
			packer.WriteVarULong(contact.NetId);
			packer.WriteByte((byte)contact.Kind);
			packer.WriteVector3(contact.Point);
			packer.WriteVector3(contact.Normal);
			packer.WriteFloat(contact.Velocity.Length());

			SendToAll(packer, NetworkManager.NetChannel.Reliable);
		}
		finally
		{
			BytePackerPool.Return(packer);
		}
	}

	/// <summary>Reliable-удаление снаряда без контакта.</summary>
	public void BroadcastDespawn(ulong netId)
	{
		if (!NetworkManager.IsServer || NetworkManager.Peers.Count == 0)
			return;

		BytePacker packer = BytePackerPool.Rent();
		try
		{
			packer.WriteByte((byte)ProjectileMessageType.Despawn);
			packer.WriteVarULong(netId);
			SendToAll(packer, NetworkManager.NetChannel.Reliable);
		}
		finally
		{
			BytePackerPool.Return(packer);
		}
	}

	/// <summary>
	/// Периодический снапшот всех живых снарядов (UnreliableOrdered: старые пакеты
	/// отбрасываются, позиция не «дёргается» назад при потере).
	/// </summary>
	public void SendSnapshot(double delta)
	{
		if (!NetworkManager.IsServer || NetworkManager.Peers.Count == 0)
			return;

		_snapshotTimer += delta;
		if (_snapshotTimer < 1.0 / SnapshotRate)
			return;

		_snapshotTimer = 0.0;

		ProjectileStore store = _system.Store;
		int count = store.Count;

		BytePacker packer = BytePackerPool.Rent();
		try
		{
			packer.WriteByte((byte)ProjectileMessageType.Snapshot);
			packer.WriteVarInt(count);

			for (int i = 0; i < count; i++)
			{
				packer.WriteVarULong(store.NetId(i));
				packer.WriteVector3(store.Position(i));
				packer.WriteVector3(store.Velocity(i));
			}

			SendToAll(packer, NetworkManager.NetChannel.UnreliableOrdered);
		}
		finally
		{
			BytePackerPool.Return(packer);
		}
	}

	/// <summary>Ставит контакты/удаления в очередь (зовёт симуляция), отправка — в конце тика.</summary>
	public void QueueImpact(in ProjectileContact contact) => _pendingImpacts.Add(contact);

	public void QueueDespawn(ulong netId) => _pendingDespawns.Add(netId);

	/// <summary>Отправить накопленные Impact/Despawn (вызывается один раз в конце фикс-тика).</summary>
	public void FlushQueues()
	{
		if (_pendingImpacts.Count > 0)
		{
			foreach (ProjectileContact contact in _pendingImpacts)
				BroadcastImpact(in contact);
			_pendingImpacts.Clear();
		}

		if (_pendingDespawns.Count > 0)
		{
			foreach (ulong netId in _pendingDespawns)
				BroadcastDespawn(netId);
			_pendingDespawns.Clear();
		}
	}

	// ------------------------------------------------------------------
	//  Приём
	// ------------------------------------------------------------------

	public void OnNetworkMessage(int fromPeer, BytePacker packer)
	{
		// Сервер-авторитет: клиентские сообщения не обрабатываются.
		if (NetworkManager.IsServer)
			return;

		ProjectileMessageType type = (ProjectileMessageType)packer.ReadByte();

		switch (type)
		{
			case ProjectileMessageType.Spawn:
				HandleSpawn(packer);
				break;

			case ProjectileMessageType.Snapshot:
				HandleSnapshot(packer);
				break;

			case ProjectileMessageType.Impact:
				HandleImpact(packer);
				break;

			case ProjectileMessageType.Despawn:
				HandleDespawn(packer);
				break;
		}
	}

	private void HandleSpawn(BytePacker packer)
	{
		ulong netId = packer.ReadVarULong();
		int ammoIndex = packer.ReadVarInt();
		int ownerPeer = packer.ReadVarInt();
		Vector3 position = packer.ReadVector3();
		Vector3 velocity = packer.ReadVector3();

		_system.SpawnFromNetwork(netId, ammoIndex, ownerPeer, position, velocity);
	}

	private void HandleSnapshot(BytePacker packer)
	{
		int count = packer.ReadVarInt();
		ProjectileStore store = _system.Store;

		for (int n = 0; n < count; n++)
		{
			ulong netId = packer.ReadVarULong();
			Vector3 position = packer.ReadVector3();
			Vector3 velocity = packer.ReadVector3();

			int i = store.FindByNetId(netId);
			if (i < 0)
				continue; // Снапшот пришёл раньше Spawn (Reliable) — пропускаем, догонит следующий.

			Vector3 current = store.Position(i);
			float error = current.DistanceTo(position);

			if (error > SnapThreshold)
			{
				// Резкая коррекция (потеря Spawn/Contact или большой лаг): без интерполяции.
				store.PreviousPosition(i) = position;
				store.Position(i) = position;
			}

			else
			{
				// Мягкая коррекция: подтягиваемся к серверной позиции за один кадр.
				store.PreviousPosition(i) = store.Position(i).Lerp(position, 0.5f);
				store.Position(i) = position;
			}

			store.Velocity(i) = velocity;
		}
	}

	private void HandleImpact(BytePacker packer)
	{
		ulong netId = packer.ReadVarULong();
		ProjectileImpactKind kind = (ProjectileImpactKind)packer.ReadByte();
		Vector3 point = packer.ReadVector3();
		Vector3 normal = packer.ReadVector3();
		float speed = packer.ReadFloat();

		_system.RaiseNetworkImpact(netId, kind, point, normal, speed);
	}

	private void HandleDespawn(BytePacker packer)
	{
		ulong netId = packer.ReadVarULong();
		_system.DespawnFromNetwork(netId);
	}

	// ------------------------------------------------------------------
	//  Утилиты отправки
	// ------------------------------------------------------------------

	private void SendToAll(BytePacker packer, NetworkManager.NetChannel channel)
	{
		NetworkReplicator.SendMessageToPeers(NetworkManager.Peers, this, packer, channel);
	}
}
