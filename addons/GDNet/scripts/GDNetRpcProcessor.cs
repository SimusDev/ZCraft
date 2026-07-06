using Godot;
using System;

public partial class GDNetRpcProcessor : Node
{
	static GDNetRpcProcessor Instance;

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
			_RpcRequest(bytes, peerId);
		}

		else if (type == GDNet.PacketType.RpcReceive)
		{
			_RpcReceive(bytes, peerId);
        }
    }

	public void _RpcRequest(byte[] bytes, long peerId)
	{

	}

    public void _RpcReceive(byte[] bytes, long peerId)
    {

    }

    public void Invoke()
	{

	}

}
