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
	private List<ChunkPos> _visibleChunks = new();
	private List<ChunkPos> _visibleChunksOld = new();
	private List<ChunkPos> _removedChunks = new();
	private List<ChunkPos> _addedChunks = new();

	public List<ChunkPos> VisibleChunks => _visibleChunks;
	public List<ChunkPos> VisibleChunksOld => _visibleChunksOld;
	public List<ChunkPos> RemovedChunks => _removedChunks;
	public List<ChunkPos> AddedChunks => _addedChunks;

	public string NickName = "";

	public Node3D TrackNode => _trackNode;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ChunkPos GetChunkPosition()
	{
		return new ChunkPos(_trackNode.GlobalPosition);
	}
	
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
		_visibleChunks.Clear();
		_visibleChunksOld.Clear();
		_removedChunks.Clear();
		_addedChunks.Clear();

		SetMultiplayerAuthorityTracker(newPeerId);
	}

	public void SetMultiplayerAuthorityTracker(int id)
	{
		SetMultiplayerAuthority(id, false);
		_trackNode.SetMultiplayerAuthority(id, true);
	}

}
