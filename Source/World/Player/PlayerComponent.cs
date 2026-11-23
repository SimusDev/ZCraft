using System;
using Godot;
using ZCraft.Source.Networking;
using ZCraft.Source.Serialization;

namespace ZCraft.Source.World.Player;

[GlobalClass]
public partial class PlayerComponent : Node, ILevelObject
{
	[Export] public Node RootNode;
	
	public override void _Ready() {}

	public void LevelObjectCreate(Level level, NetworkId id, NetworkRegion network)  {}

	public void LevelObjectDestroy(Level level) {}

	public void SnapshotLoad(BytePacker packer) {}
	public void SnapshotSave(BytePacker packer) {}

	public void SnapshotNetworkRead(BytePacker packer)
	{
		int auth = packer.ReadInt();
		RootNode.SetMultiplayerAuthority(auth);
	}

	public void SnapshotNetworkWrite(BytePacker packer)
	{
		packer.WriteInt(RootNode.GetMultiplayerAuthority());
	}
}
