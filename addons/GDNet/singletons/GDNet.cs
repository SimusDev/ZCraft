using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;

public partial class GDNet : Node
{
	public static GDNet Instance = null;

	private StreamPeerBuffer _buffer = new();

	public const int ServerID = 1;

	[Signal] public delegate void OnNetworkPeerConnectionStatusChangedEventHandler(MultiplayerPeer.ConnectionStatus status);
	[Signal] public delegate void OnNetworkReadyEventHandler();
	[Signal] public delegate void OnNetworkConnectingEventHandler();
	[Signal] public delegate void OnNetworkDisconnectedEventHandler();

	[Export] private Timer _tickTimer;

	public event Action<PacketType, byte[], long> OnNetworkPacket;

	private readonly MemoryStream _stream = new();
	private readonly BinaryWriter _writer;
	private readonly BinaryReader _reader;

	public GDNet()
	{
		_writer = new BinaryWriter(_stream);
		_reader = new BinaryReader(_stream);
	}
	public enum PacketType
	{
		RpcRequest,
		RpcReceive,

		CommunicationMessage,
	}

	private MultiplayerPeer.ConnectionStatus _connectionStatus = MultiplayerPeer.ConnectionStatus.Disconnected;
	public bool IsConnectedToServer = false;
	public bool IsServer = true;
	public int UniqueID = ServerID;

	[Export] private GDNetGarbageCollector _garbageCollector;
	[Export] private GDNetMeta _meta;
	[Export] private GDNetOptimizedSend _optimizedSend;
	[Export] private GDNetMessageProcessor _messageProcessor;
	[Export] private GDNetRpcProcessor _rpcProcessor;

	public const string MetaHashID = "GDNetID";
	public const string HashIDSalt = "GDNetHash";
	public const string HashIDSaltResource = "GDNetHashResource";

	private ulong _NextNetworkID = 0;

	private ConcurrentDictionary<ulong, ulong> _ObjectsByHashID = new();
	private ConcurrentDictionary<ulong, ulong> _HashIDByObjects = new();

	public void SetObjectHashID(GodotObject obj, ulong id)
	{
		_ObjectsByHashID[obj.GetInstanceId()] = id;
		_HashIDByObjects[id] = obj.GetInstanceId();
	}

	public ulong GetObjectHashID(GodotObject obj)
	{
		return _ObjectsByHashID.GetValueOrDefault<ulong, ulong>(obj.GetInstanceId(), 0);
	}

	public GodotObject GetObjectByHashID(ulong id)
	{
		return InstanceFromId(_ObjectsByHashID.GetValueOrDefault<ulong, ulong>(id, 0));
	}

	public void AssignNetworkID(GodotObject obj)
	{
		_NextNetworkID++;

	}

	static public ulong GenerateObjectHashID(GodotObject obj)
	{
		if (obj is Node)
		{
			Node node = (Node)obj;
			string path = node.GetPath().ToString();
			string hashStr = $"{HashIDSalt}_{path}";
			return (ulong)hashStr.Hash();
		}

		else if (obj is Resource)
		{
			Resource resource = (Resource)obj;
			if (resource.ResourcePath != "")
			{
				string hashStr = $"{HashIDSaltResource}_{resource.ResourcePath}";
				return (ulong)hashStr.Hash();
			}
		}

		return 0;
	}



	public override void _EnterTree()
	{
		Instance = this;
	}

	public override void _Ready()
	{
		_tickTimer.Timeout += UpdateNetworkStateTick;
		_tickTimer.Start();

		_meta.SingletonReady();
		_rpcProcessor.SingletonReady();

		_garbageCollector.TryCollect += OnTryCollectGarbage;
	}

	public override void _PhysicsProcess(double delta)
	{
		_optimizedSend.ProcessAll();
		_messageProcessor.ProcessAll();
	}

	private void OnTryCollectGarbage()
	{

	}

	private void UpdateNetworkStateTick()
	{
		MultiplayerPeer peer = Multiplayer.MultiplayerPeer;
		if (peer == null)
			return;

		if (peer is OfflineMultiplayerPeer)
		{
			return;
		}

		if (peer.GetConnectionStatus() != _connectionStatus)
		{
			_connectionStatus = peer.GetConnectionStatus();
			ConnectionStatusChanged();
			EmitSignal(SignalName.OnNetworkPeerConnectionStatusChanged, ((int)_connectionStatus));
		}
	}

	private void ConnectionStatusChanged()
	{
		IsServer = Multiplayer.IsServer();
		IsConnectedToServer = _connectionStatus == MultiplayerPeer.ConnectionStatus.Connected;
		UniqueID = Multiplayer.GetUniqueId();

		switch (_connectionStatus)
		{
			case MultiplayerPeer.ConnectionStatus.Disconnected:
				EmitSignal(SignalName.OnNetworkDisconnected);
				break;
			case MultiplayerPeer.ConnectionStatus.Connecting:
				EmitSignal(SignalName.OnNetworkConnecting);
				break;
			case MultiplayerPeer.ConnectionStatus.Connected:
				EmitSignal(SignalName.OnNetworkReady);
				break;
		}
	}

	public void Setup(SceneMultiplayer api)
	{
		GetTree().SetMultiplayer(api);
		_optimizedSend.Setup(api);
		_optimizedSend.MultiplayerPeerPacket += OnOptimizedPeerPacket;
	}
	public void Setup()
	{
		Setup(new());
	}

	public static int GetObjectAuthority(GodotObject obj)
	{
		if (IsInstanceValid(obj))
		{
			if (obj.HasMethod("get_multiplayer_authority"))
			{
				return obj.Call("get_multiplayer_authority").As<int>();
			}
		}

		return ServerID;
	}


	public void SendPacket(PacketType type, byte[] bytes, int peer, MultiplayerPeer.TransferModeEnum mode, int channel)
	{
		_stream.Position = 0;
		_stream.SetLength(0);

		_writer.Write((byte)type);
		_writer.Write(bytes);

		_optimizedSend.MultiplayerSendBytes(_stream.ToArray(), peer, mode, channel);
	}

	private void OnOptimizedPeerPacket(long id, byte[] bytes)
	{
		_stream.Position = 0;
		_stream.SetLength(0);
		_stream.Write(bytes, 0, bytes.Length);
		_stream.Position = 0;

		var type = (PacketType)_reader.ReadByte();
		var data = _reader.ReadBytes((int)(_stream.Length - 1));

		OnNetworkPacket?.Invoke(type, data, id);
	}



}
