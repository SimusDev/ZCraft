using Godot;
using System;
using ZCraft.Source.Networking;
using ZCraft.Source.World;

namespace ZCraft.Resources.Interface;

public partial class LevelDebug : CanvasLayer
{
	[Export] private Label _info;

	public override void _Ready() {}
	
	public override void _PhysicsProcess(double delta)
	{
		_info.Text = $"Fps: {Engine.GetFramesPerSecond()}\nNetworkReplicator objects: {NetworkReplicator.ActiveObjectCount}\nReplicated objects: {ZCraftWorldDebug.ReplicatedObjects}" +
		$"\nTickable objects: {ZCraftWorldDebug.TickableObjects}\nUnique Node Roots: {ZCraftWorldDebug.UniqueNodeRoots}";
	}
}
