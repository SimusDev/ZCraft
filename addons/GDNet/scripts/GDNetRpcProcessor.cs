using Godot;
using Godot.NativeInterop;
using System;
using System.IO;

public partial class GDNetRpcProcessor : Node
{
	public static GDNetRpcProcessor Instance;

    private readonly MemoryStream _stream = new();
    private readonly BinaryWriter _writer;
    private readonly BinaryReader _reader;

	private GDNetBuffer _buffer = new();

    public GDNetRpcProcessor()
    {
        _writer = new BinaryWriter(_stream);
        _reader = new BinaryReader(_stream);
    }

    public enum RpcType: byte
	{
		All,
		Target,
		Async,
	}

	public static bool Validate(long peer, long authority, Godot.Collections.Dictionary<string, Variant> config)
	{
		string permission = config["permission"].AsString();

		return permission switch
		{
			"authority" => peer == authority,
			"server" => peer == 1,
			_ => true
		};
	}

	public byte[] SerializeArgs(Godot.Collections.Array args)
	{
		_buffer.Clear();
		_buffer.WriteInt(args.Count);
		foreach (var arg in args)
		{
			_buffer.Write(arg);
		}
		return _buffer.GetBytes();
	}

	public Godot.Collections.Array DeserializeArgs(byte[] bytes)
	{
		_buffer.SetBytes(bytes);
		_buffer.Seek(0);
		long size = _buffer.ReadInt();

		Godot.Collections.Array result = new();

		for (int i = 0; i < size; i++)
		{
			result.Add(_buffer.Read());
		}

		return result;
	}


    public override void _EnterTree()
    {
		Instance = this;
    }

	public void SingletonReady()
	{
		GDNet.Instance.OnNetworkPacket += OnGDNetNetworkPacket;
	}

    private void OnGDNetNetworkPacket(GDNet.PacketType type, byte[] bytes, long peerId)
    {
		if (type == GDNet.PacketType.RpcRequest)
		{
			_RpcRequestPacket(bytes, peerId);
		}

		else if (type == GDNet.PacketType.RpcReceive)
		{
			_RpcReceivePacket(bytes, peerId);
        }
    }

	public void _RpcRequestPacket(byte[] bytes, long peerId)
	{

	}

    public void _RpcReceivePacket(byte[] bytes, long peerId)
    {

    }

	public void _RpcRequestServer(long peerIdFrom, RpcType type, GDNetRpc rpc, Godot.Collections.Array args)
	{
        bool valid = Validate(peerIdFrom, GDNet.GetObjectAuthority(rpc.Owner), rpc.Cfg);
		if (!valid)
		{
			return;
		}

		byte[] serializedArgs = SerializeArgs(args);

    }

	public void _InvokeByType(GDNetRpc rpc, RpcType type, Godot.Collections.Array args)
	{
		if (GDNet.Instance.IsServer)
		{
			_RpcRequestServer(GDNet.ServerID, type, rpc, args);
			return;
        }

        bool valid = Validate(GDNet.Instance.UniqueID, GDNet.GetObjectAuthority(rpc.Owner), rpc.Cfg);
		if (!valid)
			return;

	}

    public void Invoke(GDNetRpc rpc, Godot.Collections.Array args)
	{
		_InvokeByType(rpc, RpcType.All, args);
    }

    public void InvokeFor(GDNetRpc rpc, Godot.Collections.Array args)
    {
        _InvokeByType(rpc, RpcType.Target, args);
    }

}
