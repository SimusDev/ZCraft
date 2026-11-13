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
		_info.Text = $"Active Chunks: {ZCraftWorldDebug.ActiveChunks}\nNetwork Objects: {NetworkReplicator.ActiveObjectCount}\nFps: {Engine.GetFramesPerSecond()}" +
		$"\nLevelEntityData Count: {ZCraftWorldDebug.ActiveEntityData}\nActive Trackable Nodes: {ZCraftWorldDebug.ActiveTrackableNodes}";
	}
}
