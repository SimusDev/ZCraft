using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net.NetworkInformation;
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
	public readonly List<int> PeerVisibility = new();
	public List<Player.PlayerLevelTracker> PlayerTrackers => _playerTrackers;

	private List<Player.PlayerLevelTracker> _playerTrackers = new();

	private string _savePath;

	private static List<Level> _all = new();

	public static List<Level> All => _all;

	private readonly Dictionary<string, long> _sceneFilePathCache = new();
	private readonly Dictionary<long, PackedScene> _packedSceneCache = new();
	private readonly Dictionary<ulong, Node> _replicatedNodes = new();
	private readonly Dictionary<Node, ulong> _rootUniqueIdenteriers = new();
	private readonly Dictionary<ulong, WorldRegionNetwork> _rootUniqueNetworks = new();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public long CacheSceneFileAndGetUID(string sceneFilePath)
	{
		if (_sceneFilePathCache.TryGetValue(sceneFilePath, out long id))
			return id;
		
		id = ResourceUid.TextToId(ResourceUid.PathToUid(sceneFilePath));
		_sceneFilePathCache[sceneFilePath] = id;
		return id;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public PackedScene CacheUIDAndGetPackedScene(long sceneUID)
	{
		if (_packedSceneCache.TryGetValue(sceneUID, out PackedScene packedScene))
			return packedScene;
		
		packedScene = GD.Load<PackedScene>(ResourceUid.GetIdPath(sceneUID));
		_packedSceneCache[sceneUID] = packedScene;
		return packedScene;
	}

	private enum PacketHeader : byte
	{
		ChunkReceive,
		ChunkRemove,
		ObjectsRequest,
		ObjectRecieve,
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

    public override void _EnterTree()
	{
		NetworkReplicator.AddObject(this, new NetworkId(GetPath()));
	}

    public override void _ExitTree()
	{
		NetworkReplicator.RemoveObject(this);
	}

	public override void _Ready()
	{
		_all.Add(this);
		
		if (NetworkManager.IsServer)
		{
			_savePath = GenerateRuntimeSavePath();
			ChildEnteredTree += OnChildEnteredTree;
			ChildExitingTree += OnChildExitingTree;
			GenerateMapServer();
		}

		else
		{
			BytePacker packer = BytePackerPool.Rent();
			packer.WriteByte((byte)PacketHeader.ObjectsRequest);
			NetworkReplicator.SendMessageToServer(this, packer);
		}
	}

    private void GenerateMapServer()
	{
		var sw = System.Diagnostics.Stopwatch.StartNew();

		for (int i = 0; i < GetChildCount(); i++)
		{
			Node child = GetChild(i);
			if (IsRootTreeSerializable(child))
				TryGenerateUniqueNodeTree(child);
		}

		sw.Stop();
	}
    private void OnChildExitingTree(Node node)
	{
		if (!IsRootTreeSerializable(node))
			return;
		
		if (IsNodeTreeQueuedForDeletion(node))
		{
			if (_rootUniqueIdenteriers.TryGetValue(node, out ulong id))
			{
				ZCraftWorldDebug.UniqueNodeRoots--;
				_rootUniqueIdenteriers.Remove(node);
				_rootUniqueNetworks.Remove(id);
			}
		}

		if (!NetworkReplicator.IsPackedSceneCanBeReplicated(node.SceneFilePath))
			return;
	}

	private void OnChildEnteredTree(Node node)
	{
		if (!IsRootTreeSerializable(node))
			return;
		
		TryGenerateUniqueNodeTree(node);

		if (!NetworkReplicator.IsPackedSceneCanBeReplicated(node.SceneFilePath))
			return;
	}

	private void TryGenerateUniqueNodeTree(Node node)
	{
		if (!_rootUniqueIdenteriers.TryGetValue(node, out ulong uniqueId))
		{
			ZCraftWorldDebug.UniqueNodeRoots++;
			uniqueId = ZCraftWorld.GenerateUniqueId();
			node.Name = uniqueId.ToString();
			_rootUniqueIdenteriers[node] = uniqueId;
			
			var network = new WorldRegionNetwork();
			network.SetPeers(NetworkManager.Peers);
			_rootUniqueNetworks[uniqueId] = network;
		}
	}

	private void CollectObjectsToSend()
	{
		
	}

	private void SendAllObjectsToPeer(int toPeer)
	{
		Stopwatch stopwatch = new();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsDynamicObject(Node node)
	{
		return node is RigidBody3D || node is CharacterBody3D;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsRootTreeSerializable(Node rootTree)
	{
		return rootTree.SceneFilePath != string.Empty;
	}

    public void OnNetworkMessage(int fromPeer, BytePacker packer)
    {
        PacketHeader header = (PacketHeader)packer.ReadByte();
		
		switch(header)
		{
			case PacketHeader.ObjectsRequest:
				SendAllObjectsToPeer(fromPeer);
				break;
			case PacketHeader.ChunkReceive:

				break;
			case PacketHeader.ChunkRemove:
				
				break;
		}
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

	public static bool IsNodeTreeQueuedForDeletion(Node node)
	{
		while (node != null)
		{
			if (node.IsQueuedForDeletion())
				return true;
			
			node = node.GetParent();
		}

		return false;
	}

}
