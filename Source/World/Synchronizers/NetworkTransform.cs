using System;
using System.Collections.Generic;
using Godot;
using ZCraft.Source.Entity;
using ZCraft.Source.Networking;
using ZCraft.Source.Serialization;

namespace ZCraft.Source.World.Synchronizers;

[GlobalClass]
public partial class NetworkTransform : Node, ILevelObject, INetworkReplicationObject, ITickable
{
	[Export] private Godot.Collections.Array<Node3D> _nodes = new();
	[Export] private double _tickRate = 0;
	// Насколько клиент отстаёт от сервера (в тиках). Больше = плавнее, но больше лаг.
	[Export] private double _interpolationDelayTicks = 1.5;

	private double _tickInterval;
	private double _tickTime;

	// Интерполяция (только на клиенте)
	private Vector3[] _fromPos;
	private Vector3[] _fromRot;
	private Vector3[] _toPos;
	private Vector3[] _toRot;
	private double _interpTime;        // сколько прошло с момента получения последнего снапшота
	private double _interpDuration;    // за сколько нужно дойти до _to
	private bool _hasSnapshot;

	private NetworkRegion _network;

	public override void _EnterTree()
	{
		if (_tickRate <= 0) _tickRate = 32;
		_tickInterval = 1.0 / _tickRate;

		int n = _nodes.Count;
		_fromPos = new Vector3[n];
		_fromRot = new Vector3[n];
		_toPos = new Vector3[n];
		_toRot = new Vector3[n];
		for (int i = 0; i < n; i++)
		{
			if (_nodes[i] != null)
			{
				_fromPos[i] = _toPos[i] = _nodes[i].Position;
				_fromRot[i] = _toRot[i] = _nodes[i].Rotation;
			}
		}

		Ticker.Registry.Add(this);
	}

	public override void _ExitTree()
	{
		Ticker.Registry.Remove(this);
	}

	// ---------------------------------------------------------------
	//  Отправка (сервер всегда, клиент только если authority)
	// ---------------------------------------------------------------

	void ITickable.OnTick(double delta)
	{
		if (_network == null) return;
		
		Interpolate(delta);

		bool isServer = NetworkManager.IsServer;
		bool isAuthority = IsMultiplayerAuthority();
		if (!isServer && !isAuthority) return;

		_tickTime += delta;
		if (_tickTime < _tickInterval) return;
		_tickTime -= _tickInterval;

		BytePacker packer = BytePackerPool.Rent();
		try
		{
			packer.WriteByte(0);
			packer.WriteVarInt(_nodes.Count);
			for (int i = 0; i < _nodes.Count; i++)
			{
				packer.WriteVector3(_nodes[i].Position);
				packer.WriteVector3(_nodes[i].Rotation);
			}

			if (isServer)
				_network.ServerBroadcast(this, packer, NetworkManager.NetChannel.Unreliable);
			else
				_network.ClientSend(this, packer, NetworkManager.NetChannel.Unreliable);
		}
		finally
		{
			BytePackerPool.Return(packer);
		}
	}

	// ---------------------------------------------------------------
	//  Приём (клиент): не применяем сразу, а кладём в буфер
	// ---------------------------------------------------------------

	void INetworkReplicationObject.OnNetworkMessage(int fromPeer, BytePacker packer)
	{
		byte type = packer.ReadByte();
		switch (type)
		{
			case 0:
				int count = packer.ReadVarInt();
				int toApply = Math.Min(count, _nodes.Count);

				// Сдвигаем текущее _to в _from, новое пишем в _to
				for (int i = 0; i < toApply; i++)
				{
					_fromPos[i] = _nodes[i].Position;
					_fromRot[i] = _nodes[i].Rotation;
				}

				for (int i = 0; i < count; i++)
				{
					Vector3 pos = packer.ReadVector3();
					Vector3 rot = packer.ReadVector3();
					if (i < toApply)
					{
						_toPos[i] = pos;
						_toRot[i] = rot;
					}
				}

				_interpTime = 0;
				_interpDuration = _tickInterval * _interpolationDelayTicks;
				_hasSnapshot = true;

				if (count != _nodes.Count)
					GD.PushWarning($"NetworkTransform: count mismatch {count} vs {_nodes.Count}");
				break;
		}
	}

	// ---------------------------------------------------------------
	//  Интерполяция (каждый кадр, только на не-authority)
	// ---------------------------------------------------------------

	private void Interpolate(double delta)
	{
		if (!_hasSnapshot) return;
		if (IsMultiplayerAuthority()) return; // authority применяет свои позиции сам

		_interpTime += delta;
		float t = _interpDuration > 0
			? Mathf.Clamp((float)(_interpTime / _interpDuration), 0f, 1f)
			: 1f;

		for (int i = 0; i < _nodes.Count; i++)
		{
			Node3D node = _nodes[i];
			if (node == null) continue;

			node.Position = _fromPos[i].Lerp(_toPos[i], t);
			node.Rotation = _fromRot[i].Lerp(_toRot[i], t);
		}

		// Если дошли до _to — закрепляем, чтобы не дёргалось между кадрами
		if (t >= 1f)
		{
			for (int i = 0; i < _nodes.Count; i++)
			{
				_fromPos[i] = _toPos[i];
				_fromRot[i] = _toRot[i];
			}
		}
	}

	// ---------------------------------------------------------------
	//  ILevelObject / снапшоты — пустые, transform идёт отдельно
	// ---------------------------------------------------------------

	public void SnapshotNetworkWrite(BytePacker packer) {}
	public void SnapshotNetworkRead(BytePacker packer) {}
	public void SnapshotSave(BytePacker packer) {}
	public void SnapshotLoad(BytePacker packer) {}

	public void LevelObjectCreate(Level level, NetworkId id, NetworkRegion network)
	{
		_network = network;
		NetworkReplicator.AddObject(this, id);
	}

	public void LevelObjectDestroy(Level level)
	{
		_network = null;
		NetworkReplicator.RemoveObject(this);
	}
}
