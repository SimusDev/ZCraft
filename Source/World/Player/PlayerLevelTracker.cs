using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ZCraft.Source.World.Player;

[GlobalClass]
public partial class PlayerLevelTracker : Node
{
	[Export] private Node3D _trackNode;

	private Level _level;
	public string NickName = "";

	public Node3D TrackNode => _trackNode;
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vector3 GetGlobalPosition()
	{
		return _trackNode.GlobalPosition;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Level GetLevel()
	{
		return _level;
	}

	public override void _EnterTree()
	{
		_level = Level.FindAbove(this);
		_level.AddPlayerTracker(this);
	}

	public override void _ExitTree()
	{
		_level.RemovePlayerTracker(this);
	}

	public void ResetClient(int newPeerId)
	{
		SetMultiplayerAuthorityTracker(newPeerId);
	}

	public void SetMultiplayerAuthorityTracker(int id)
	{
		SetMultiplayerAuthority(id, false);
		_trackNode.SetMultiplayerAuthority(id, true);
	}

}
