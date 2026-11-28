using System;
using System.Collections.Generic;
using Godot;

namespace ZCraft.Source.Content.Survival.Projectiles;

/// <summary>
/// Data-oriented хранилище снарядов (SoA): один плотный массив на поле, никаких узлов.
/// Живые снаряды лежат вплотную в [0, Count), удаление — swap-remove.
/// Индекс слота не стабилен; сквозной идентификатор — NetId (ulong, серверный).
/// </summary>
public sealed class ProjectileStore
{
	private const int InitialCapacity = 256;

	private int _count;
	private int _capacity = InitialCapacity;

	// --- SoA-поля ---
	private Vector3[] _position = new Vector3[InitialCapacity];
	private Vector3[] _previousPosition = new Vector3[InitialCapacity]; // позиция на прошлом тике (для интерполяции рендера)
	private Vector3[] _velocity = new Vector3[InitialCapacity];
	private int[] _ammoIndex = new int[InitialCapacity];
	private int[] _ownerPeer = new int[InitialCapacity];
	private ulong[] _netId = new ulong[InitialCapacity];
	private float[] _age = new float[InitialCapacity];
	private float[] _distanceFlown = new float[InitialCapacity];
	private bool[] _tracer = new bool[InitialCapacity];
	private Rid[] _ownerBody = new Rid[InitialCapacity]; // коллайдер стрелка — исключается из рейкастов

	/// <summary>NetId → индекс слота (нужен клиенту для применения снапшотов).</summary>
	private readonly Dictionary<ulong, int> _indexByNetId = new(256);

	/// <summary>Количество живых снарядов. Валидные индексы: [0, Count).</summary>
	public int Count => _count;

	public int Capacity => _capacity;

	// --- Доступ к полям (ref, без копирования) ---

	public ref Vector3 Position(int i) => ref _position[i];
	public ref Vector3 PreviousPosition(int i) => ref _previousPosition[i];
	public ref Vector3 Velocity(int i) => ref _velocity[i];
	public ref int AmmoIndex(int i) => ref _ammoIndex[i];
	public ref int OwnerPeer(int i) => ref _ownerPeer[i];
	public ref ulong NetId(int i) => ref _netId[i];
	public ref float Age(int i) => ref _age[i];
	public ref float DistanceFlown(int i) => ref _distanceFlown[i];
	public ref bool Tracer(int i) => ref _tracer[i];
	public ref Rid OwnerBody(int i) => ref _ownerBody[i];

	/// <summary>Слот по сетевому id; −1 если не найден.</summary>
	public int FindByNetId(ulong netId) => _indexByNetId.TryGetValue(netId, out int i) ? i : -1;

	/// <summary>Создаёт слот, возвращает его индекс. prevPos = pos, возраст = 0.</summary>
	public int Spawn(
		ulong netId, int ammoIndex, int ownerPeer,
		Vector3 position, Vector3 velocity, bool tracer,
		Rid ownerBody = default)
	{
		if (_count == _capacity)
			Grow(_capacity * 2);

		int i = _count++;
		_position[i] = position;
		_previousPosition[i] = position;
		_velocity[i] = velocity;
		_ammoIndex[i] = ammoIndex;
		_ownerPeer[i] = ownerPeer;
		_netId[i] = netId;
		_age[i] = 0f;
		_distanceFlown[i] = 0f;
		_tracer[i] = tracer;
		_ownerBody[i] = ownerBody;
		_indexByNetId[netId] = i;
		return i;
	}

	/// <summary>Удаляет слот swap-remove'ом. Индексы последнего элемента и NetId-мапа переставляются.</summary>
	public void Remove(int i)
	{
		if ((uint)i >= (uint)_count)
			return;

		_indexByNetId.Remove(_netId[i]);

		int last = _count - 1;
		if (i != last)
		{
			_position[i] = _position[last];
			_previousPosition[i] = _previousPosition[last];
			_velocity[i] = _velocity[last];
			_ammoIndex[i] = _ammoIndex[last];
			_ownerPeer[i] = _ownerPeer[last];
			_netId[i] = _netId[last];
			_age[i] = _age[last];
			_distanceFlown[i] = _distanceFlown[last];
			_tracer[i] = _tracer[last];
			_ownerBody[i] = _ownerBody[last];
			_indexByNetId[_netId[i]] = i;
		}

		_count--;
	}

	/// <summary>Сброс всех снарядов (смена мира).</summary>
	public void Clear()
	{
		_count = 0;
		_indexByNetId.Clear();
	}

	private void Grow(int newCapacity)
	{
		Array.Resize(ref _position, newCapacity);
		Array.Resize(ref _previousPosition, newCapacity);
		Array.Resize(ref _velocity, newCapacity);
		Array.Resize(ref _ammoIndex, newCapacity);
		Array.Resize(ref _ownerPeer, newCapacity);
		Array.Resize(ref _netId, newCapacity);
		Array.Resize(ref _age, newCapacity);
		Array.Resize(ref _distanceFlown, newCapacity);
		Array.Resize(ref _tracer, newCapacity);
		Array.Resize(ref _ownerBody, newCapacity);
		_capacity = newCapacity;
	}

	/// <summary>Прямой доступ к плотному массиву позиций (для рендера/сериализации).</summary>
	public Span<Vector3> Positions => _position.AsSpan(0, _count);

	/// <summary>Прямой доступ к плотному массиву скоростей.</summary>
	public Span<Vector3> Velocities => _velocity.AsSpan(0, _count);
}
