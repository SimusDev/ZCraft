using Godot;
using System;
using System.Collections.Generic;

[GlobalClass]
public partial class GDNetCommunicator : Resource, IDisposable
{

	[Export] private ulong _networkID = 0;
	[Export] public MultiplayerPeer.TransferModeEnum Mode = MultiplayerPeer.TransferModeEnum.Reliable;
	[Export] public int Channel = 0;

	private static Dictionary<ulong, ulong> _registry = new();

	public static GDNetCommunicator FindByNetworkID(ulong id)
	{
		GodotObject obj = InstanceFromId(_registry.GetValueOrDefault(id));
		if (obj != null)
		{
			return (GDNetCommunicator)obj;
		}

		return null;
	}

	public GDNetCommunicator()
	{
		SetNetworkID(_networkID);
	}

	public void SetNetworkID(ulong id)
	{
		_registry.Remove(_networkID);
		_networkID = id;
		_registry[id] = this.GetInstanceId();
	}

	public ulong GetNetworkID()
	{
		return _networkID;
	}

	public void Send(GDNetAoI AoI, byte[] data)
	{
		GDNetMessageProcessor.Instance.___QueueCommunicator(_networkID, AoI, data, Mode, Channel);
	}

	public void ReceivedBytes(long peerId, byte[] data)
	{

	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_registry.Remove(_networkID);
		}

		base.Dispose(disposing);
	}


}
