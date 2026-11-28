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
using ZCraft.Source.World.Item;

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
	public List<int> OnlinePlayerPeers => _onlinePlayersPeers;

	private readonly List<Node> _childEnteredTreeUniqueRootsQueue = new();
	private readonly Dictionary<string, long> _sceneFilePathCache = new();
	private readonly Dictionary<long, PackedScene> _packedSceneCache = new();
	private readonly Dictionary<Node, LevelUniqueTree> _rootUniqueIdenteriers = new();
	private readonly Dictionary<ulong, Node> _clientRootUniqueNodes = new();
	private List<int> _onlinePlayersPeers = new();
	private readonly NetworkRegion _clientNetworkRegion = new();

	public static Level ClientLocalLevel;


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
		ObjectsRequest,
		ObjectRecieve,
		ObjectRemove,
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
		_all.Add(this);
		NetworkReplicator.AddObject(this, new NetworkId(GetPath()));
	}

    public override void _ExitTree()
	{
		_all.Remove(this);
		NetworkReplicator.RemoveObject(this);
	}

	public override void _Ready()
	{
		if (NetworkManager.IsServer)
		{
			_savePath = GenerateRuntimeSavePath();
			ChildEnteredTree += OnChildEnteredTree;
			ChildExitingTree += OnChildExitingTree;
			GenerateMapServer();

			Multiplayer.PeerDisconnected += OnPeerDisconnected;
		}

		else
		{
			ClientLocalLevel = this;
			BytePacker packer = BytePackerPool.Rent();
			packer.WriteByte((byte)PacketHeader.ObjectsRequest);
			NetworkReplicator.SendMessageToServer(this, packer);
		}
	}

    private void OnPeerDisconnected(long id)
    {
        _onlinePlayersPeers.Remove((int)id);
    }

    private void GenerateMapServer()
	{
		var sw = System.Diagnostics.Stopwatch.StartNew();

		for (int i = 0; i < GetChildCount(); i++)
		{
			Node child = GetChild(i);
			if (IsRootTreeSerializable(child))
				TryGenerateUniqueNodeTreeServer(child);
		}

		sw.Stop();
	}
    private void OnChildExitingTree(Node node)
	{
		if (!IsRootTreeSerializable(node))
			return;

		bool queuedForDeletion = IsNodeTreeQueuedForDeletion(node);

		if (_rootUniqueIdenteriers.TryGetValue(node, out LevelUniqueTree tree))
		{
			SerializeUniqueRootDeletionAndSend(tree, NetworkManager.Peers);

			if (queuedForDeletion)
			{
				ZCraftWorldDebug.UniqueNodeRoots--;
				_rootUniqueIdenteriers.Remove(node);
				tree.Destroy();
			}


		}
	}

	private void OnChildEnteredTree(Node node)
	{
		if (!IsRootTreeSerializable(node))
			return;
		
		TryGenerateUniqueNodeTreeServer(node);

		_childEnteredTreeUniqueRootsQueue.Add(node);

		// if (!NetworkReplicator.IsPackedSceneCanBeReplicated(node.SceneFilePath))
		// 	return;
		
	}

	private void TryGenerateUniqueNodeTreeServer(Node node)
	{
		if (!_rootUniqueIdenteriers.TryGetValue(node, out LevelUniqueTree tree))
		{
			ZCraftWorldDebug.UniqueNodeRoots++;
			tree = new LevelUniqueTree(this, ZCraftWorld.GenerateUniqueId());
			tree.CreateServer(node, new(NetworkManager.Peers));

			node.Name = tree.RootUniqueId.ToString();
			_rootUniqueIdenteriers[node] = tree;
		}
	}

	private void CollectObjectsToSend()
	{
		
	}

	private void SendAllObjectsToPeer(int toPeer)
	{
		Stopwatch stopwatch = new();

		List<int> peers = new(1);
		peers.Add(toPeer);

		foreach(var pair in _rootUniqueIdenteriers)
		{
			SerializeUniqueRootAndSend(pair.Key, pair.Value, peers);
		}
	}

    public override void _Process(double delta)
	{
		int count = _childEnteredTreeUniqueRootsQueue.Count;
		for (int i = 0; i < count; i++)
		{
			Node node = _childEnteredTreeUniqueRootsQueue[i];

			if (!IsInstanceValid(node))
				continue;
			
			LevelUniqueTree tree = _rootUniqueIdenteriers[node];
			SerializeUniqueRootAndSend(node, tree, NetworkManager.Peers);
		}

		_childEnteredTreeUniqueRootsQueue.RemoveRange(0, count);

	}

	private void SerializeUniqueRootAndSend(Node node, LevelUniqueTree tree, List<int> peers)
	{
		BytePacker bytePacker = BytePackerPool.Rent();
		bytePacker.WriteByte((byte)PacketHeader.ObjectRecieve);

		long sceneId = CacheSceneFileAndGetUID(node.SceneFilePath);
		bytePacker.WriteLong(sceneId);

		bytePacker.WriteULong(tree.RootUniqueId);

		if (node is Node3D node3d)
		{
			bytePacker.WriteBool(true);
			bytePacker.WriteVector3(node3d.Position);
			bytePacker.WriteVector3(node3d.Rotation);
		}

		else
		{
			bytePacker.WriteBool(false);
		}

		tree.SnapshotSerialize(bytePacker);

		NetworkReplicator.SendMessageToPeers(peers, this, bytePacker);

		BytePackerPool.Return(bytePacker);
	}

	private void SerializeUniqueRootDeletionAndSend(LevelUniqueTree tree, List<int> peers)
	{
		BytePacker bytePacker = BytePackerPool.Rent();
		bytePacker.WriteByte((byte)PacketHeader.ObjectRemove);
		bytePacker.WriteULong(tree.RootUniqueId);

		NetworkReplicator.SendMessageToPeers(peers, this, bytePacker);

		BytePackerPool.Return(bytePacker);
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

			case PacketHeader.ObjectRecieve:
				
				long sceneId = packer.ReadLong();
				PackedScene packedScene = CacheUIDAndGetPackedScene(sceneId);

				ulong rootUniqueId = packer.ReadULong();

				if (_clientRootUniqueNodes.ContainsKey(rootUniqueId))
					return;
				
				bool isNode3D = packer.ReadBool();

				Node nodeTree = packedScene.Instantiate();
				nodeTree.Name = rootUniqueId.ToString();

				if (nodeTree is Node3D node3d && isNode3D == true)
				{
					node3d.Position = packer.ReadVector3();
					node3d.Rotation = packer.ReadVector3();
				}


				LevelUniqueTree tree = new(this, rootUniqueId);
				tree.CreateClient(nodeTree, _clientNetworkRegion);
				tree.SnapshotDeserialize(nodeTree, packer);

				_rootUniqueIdenteriers[nodeTree] = tree;
				_clientRootUniqueNodes[rootUniqueId] = nodeTree; 
				
				AddChild(nodeTree);
				ZCraftWorldDebug.UniqueNodeRoots++;

				break;
			case PacketHeader.ObjectRemove:
				
				ulong uid = packer.ReadULong();

				if (_clientRootUniqueNodes.TryGetValue(uid, out Node node))
				{
					ZCraftWorldDebug.UniqueNodeRoots--;
					LevelUniqueTree clientTree = _rootUniqueIdenteriers[node];
					clientTree.Destroy();
					_clientRootUniqueNodes.Remove(uid);
					_rootUniqueIdenteriers.Remove(node);
					node?.QueueFree();
				}
				
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

    public override void _Input(InputEvent @event)
	{
		if (!NetworkManager.IsServer)
			return;
		
		if (Input.IsActionJustPressed("pipi"))
		{
			var item = Item.Item.ByName("CopperIngot");
			var stack = new ItemStack(item);

			ItemStack.DropAtPosition(stack, this, new Vector3(0, 10, 0));
		}
	}


}
