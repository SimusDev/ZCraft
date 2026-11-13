using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ZCraft.Source.Networking;
using ZCraft.Source.Serialization;

namespace ZCraft.Source.World;

public partial class Level : Node3D, INetworkReplicationObject
{
	
	public Level() {}

	public const int ChunkSize = 32;
	
	[Export] public bool Debug = true;
	[Export] public int RenderDistance = 4;   

	public List<Player.PlayerLevelTracker> PlayerTrackers => _playerTrackers;

	private List<Player.PlayerLevelTracker> _playerTrackers = new();

	private HashSet<Node3D> _trackableNodes = new();

	private HashSet<Node3D> _childEnteredQueue = new();

	private List<ITickable> _tickables = new();

	private Dictionary<ChunkPos, ChunkData> _chunksData = new();

	public Dictionary<ChunkPos, ChunkData> ChunksData => _chunksData;

	private Dictionary<IWorldObject, WorldObjectData> _worldObjects = new();

	public int ActiveChunksCount => _chunksData.Count;

	private string _savePath;

	private static List<Level> _all = new();

	public static List<Level> All => _all;

	public static readonly StringName GodotMetaCurrentChunkName = new("CurrentChunk");

	private enum PacketHeader : byte
	{
		ChunkReceive,
		ChunkRemove,
	}

	public string GenerateRuntimeSavePath()
	{
		string executablePath;
		string levelPath;

		if (OS.HasFeature("editor"))
		{
			executablePath = $"res://.world";
		}

		else
		{
			executablePath = OS.GetExecutablePath().PathJoin(".world");
		}

		levelPath = executablePath.PathJoin(Name);

		DirAccess.MakeDirRecursiveAbsolute(levelPath);

		return levelPath;
	}

	public override void _Ready()
	{
		InitializeNetworkReplication(new NetworkId(this.GetPath()));
		_all.Add(this);
		
		_savePath = GenerateRuntimeSavePath();
		
		if (NetworkManager.IsServer)
		{
			ChildEnteredTree += OnChildEnteredTree;
			ChildExitingTree += OnChildExitingTree;
			GenerateMapServer();
		}
	}

	public void InitializeNetworkReplication(NetworkId networkId)
	{
		NetworkReplicator.AddObject(this, networkId);
	}

	public void DeinitializeNetworkReplication()
	{
		NetworkReplicator.RemoveObject(this);
	}

    private void GenerateMapServer()
	{
		var sw = System.Diagnostics.Stopwatch.StartNew();

		for (int i = 0; i < GetChildCount(); i++)
		{
			Node node = GetChild(i);

			if (node.SceneFilePath == string.Empty)
				continue;

			if (node is Node3D node3d)
			{
				ChunkPos chunkPos = new ChunkPos(node3d.GlobalPosition);
				CreateEntityNode(node3d, chunkPos);

				if (IsDynamicObject(node3d))
					RegisterTrackableNode(node3d);
			}
		}

		InitializeWorldObjectNodeTree(this);

		sw.Stop();
		GD.Print($"({Name}) Server MapGen time: {sw.Elapsed.TotalMilliseconds} miliseconds");
	}

	public override void _PhysicsProcess(double delta)
	{
		for (int i = 0; i < _tickables.Count; i++)
		{
			_tickables[i].PhysicsTick(delta);
		}

		if (!NetworkManager.IsServer)
			return;

		for (int p = 0; p < _playerTrackers.Count; p++)
		{
			UpdatePlayerTracker(_playerTrackers[p]);
		}

		foreach(Node3D q in _childEnteredQueue)
		{
			CreateEntityNode(q, new ChunkPos(q.GlobalPosition));
			_childEnteredQueue.Remove(q);
		}

		foreach(Node3D t in _trackableNodes)
		{
			UpdateTrackableNode(t);
		}

	}

    public override void _ExitTree()
	{
		DeinitializeNetworkReplication();
	}

	public void RegisterTrackableNode(Node3D node)
	{
		_trackableNodes.Add(node);
		ZCraftWorldDebug.ActiveTrackableNodes++;
	}

	public void UnregisterTrackableNode(Node3D node)
	{
		_trackableNodes.Remove(node);
		ZCraftWorldDebug.ActiveTrackableNodes--;
	}

	private void CreateEntityNode(Node node, ChunkPos startPosition)
	{
		if (!_chunksData.TryGetValue(startPosition, out ChunkData chunkData))
		{
			chunkData = ChunkDataPool.Rent();
			_chunksData[startPosition] = chunkData;
			ZCraftWorldDebug.ActiveChunks++;
		}

		ulong uniqueId = ZCraftWorld.GenerateUniqueId();
		node.Name = uniqueId.ToString();
		chunkData.Nodes[node] = new LevelEntityData(uniqueId, node.SceneFilePath);
		ZCraftWorldDebug.ActiveEntityData++;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void UpdateNodeChunk(Node3D node)
	{
		
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void UpdateObjectChunk(Node @object, ChunkPos fromPosition, ChunkPos toPosition)
	{
		if (fromPosition == toPosition)
			return;
		
		_chunksData.TryGetValue(fromPosition, out ChunkData fromChunk);
		_chunksData.TryGetValue(toPosition, out ChunkData toChunk);

		//if (fromChunk == toChunk)
		//	return;
		
		//fromChunk?.SubtractObjectCount();
		//toChunk.AddObjectCount();

		//if (Debug)
		//	@object.SetMeta(GodotMetaCurrentChunkName, toChunk.ChunkPos.ToString());
	}

    private void OnChildExitingTree(Node node)
	{
		if (node.SceneFilePath == string.Empty)
			return;
		
		if (node is Node3D node3D)
		{
			if (IsDynamicObject(node3D))
				UnregisterTrackableNode(node3D);

			ChunkPos chunkPos = new(node3D.GlobalPosition);

			if (_chunksData.TryGetValue(chunkPos, out ChunkData chunkData))
			{
				if (chunkData.Nodes.TryGetValue(node3D, out LevelEntityData entityData))
				{
					chunkData.Nodes.Remove(node3D);
					ZCraftWorldDebug.ActiveEntityData--;
				}

				if (chunkData.IsEmpty())
				{
					_chunksData.Remove(chunkPos);
					ChunkDataPool.Return(chunkData);
					ZCraftWorldDebug.ActiveChunks--;
				}
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsDynamicObject(Node node)
	{
		return node is RigidBody3D || node is CharacterBody3D;
	}

    private void OnChildEnteredTree(Node node)
	{
		if (node.SceneFilePath == string.Empty)
			return;
		
		if (!node.IsNodeReady())
		{
			InitializeWorldObjectNodeTree(node);
		}
		
		if (node is Node3D node3D)
		{
			_childEnteredQueue.Add(node3D);
			
			if (IsDynamicObject(node3D))
				RegisterTrackableNode(node3D);
			
		}
	}

	private void InitializeWorldObjectNodeTree(Node node)
	{
		for (int i = 0; i < node.GetChildCount(); i++)
		{
			Node child = node.GetChild(i);
			InitializeWorldObjectNodeTree(child);
		}

		InitializeWorldObjectNode(node);
	}

	private void InitializeWorldObjectNode(Node node)
	{
		if (node is IWorldObject worldObject)
		{
			WorldObjectData data = RegisterWorldObject(worldObject);
			node.Name = "WO_" + data.UniqueId.ToString();
			node.TreeExited += () => OnNodeWorldObjectTreeExited(node);
		}
	}

	private void OnNodeWorldObjectTreeExited(Node node)
	{
		if (node.IsQueuedForDeletion())
			UnregisterWorldObject((IWorldObject)node);
	}

	public WorldObjectData GetWorldObjectData(IWorldObject @object)
	{
		return _worldObjects[@object];
	}

	public WorldObjectData RegisterWorldObject(IWorldObject @object)
	{
		return RegisterWorldObject(@object, ZCraftWorld.GenerateUniqueId());
	}

	public WorldObjectData RegisterWorldObject(IWorldObject @object, ulong uniqueId)
	{
		WorldObjectData data = new(ZCraftWorld.GenerateUniqueId());
		_worldObjects[@object] = data;
		return data;
	}

	public void UnregisterWorldObject(IWorldObject @object)
	{
		_worldObjects.Remove(@object);
	}

	public override void _Process(double delta)
	{
		for (int i = 0; i < _tickables.Count; i++)
		{
			_tickables[i].Tick(delta);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void UpdatePlayerTracker(Player.PlayerLevelTracker tracker)
	{
		int peerId = tracker.GetMultiplayerAuthority();
		
		if (!Multiplayer.GetPeers().Contains(peerId))
			return;
		
		ChunkPos center = tracker.GetChunkPosition();

		HashSet<ChunkPos> peerVisibleChunks = tracker.VisibleChunks;
		HashSet<ChunkPos> peerVisibleChunksOld = tracker.VisibleChunksOld;
		peerVisibleChunks.Clear();

		for (int dx = -RenderDistance; dx <= RenderDistance; dx++)
			for (int dy = -RenderDistance; dy <= RenderDistance; dy++)
				for (int dz = -RenderDistance; dz <= RenderDistance; dz++)
				{
					ChunkPos visible = new ChunkPos(center.X + dx, center.Y + dy, center.Z + dz);
					if (_chunksData.ContainsKey(visible))
						peerVisibleChunks.Add(visible);
				}
		
		

	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void UpdateTrackableNode(Node3D node3D)
	{
		
	}

    public void OnNetworkMessage(int fromPeer, BytePacker packer)
    {
        PacketHeader header = (PacketHeader)packer.ReadByte();
		
		switch(header)
		{
			case PacketHeader.ChunkReceive:

				break;
			case PacketHeader.ChunkRemove:
				
				break;
		}
    }

	public void AddTickable(ITickable tickable)
	{
		_tickables.Add(tickable);
	}

	public void RemoveTickable(ITickable tickable)
	{
		_tickables.Remove(tickable);
	}

	internal void AddPlayerTracker(Player.PlayerLevelTracker tracker)
	{
		_playerTrackers.Add(tracker);
	}

	internal void RemovePlayerTracker(Player.PlayerLevelTracker tracker)
	{
		_playerTrackers.Remove(tracker);
	}

	public static Level FindAbove(Node node)
	{
		while (node != null)
		{
			if (node is Level level)
			{
				return level;
			}

			node = node.GetParent();
		}

		return null;
	}

	public void Save()
	{
		using var file = FileAccess.Open(_savePath, FileAccess.ModeFlags.WriteRead);
	}

	public void Load()
	{
		using var file = FileAccess.Open(_savePath, FileAccess.ModeFlags.WriteRead);
	}

    public override void _Input(InputEvent @event)
	{
		if (Input.IsActionJustPressed("pipi"))
		{
			foreach(var child in GetChildren())
				child.QueueFree();
		}
	}

}
