using Godot;

[GlobalClass]
public partial class SlotsContainer : Resource, IGDNetSerializable
{
	private GDNetRpc _rpc = new();

	private long _netId = 0;

	[Export] private Godot.Collections.Array<ContainerSlot> _slots = new();

	public SlotsContainer()
	{
		_netId = GDNet.GenerateUniqueID();
		_rpc.SynchronizeNetworkIDByUniqueID(_netId);
		_rpc.BindAll(this);

	}

	public void Deserialize(GDNetBuffer buffer)
	{
		_slots.Clear();
		_rpc.SynchronizeNetworkIDByUniqueID(buffer.ReadLongVar());

		var slotCount = buffer.ReadIntVar();

		for (int i = 0; i < slotCount; i++)
			_slots.Add(buffer.ReadSerializable<ContainerSlot>());
	}

	public void Serialize(GDNetBuffer buffer)
	{
		buffer.WriteLongVar(_netId);
		buffer.WriteIntVar(_slots.Count);

		foreach (var slot in _slots)
			buffer.WriteSerializable(slot);

	}
}
