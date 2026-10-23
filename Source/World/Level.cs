using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ZCraft.Source.Serialization;

namespace ZCraft.Source.World;

public partial class Level : Node3D
{
	[ExportGroup("Map")]
	[Export] public int ChunkSize = 64;
	[Export] public int RenderDistance = 8;
	[Export] public int MapSizeMeters = 2500;

	public List<Player.Player> Players => _players;

	private List<Player.Player> _players = new();

	private List<ITickable> _tickables = new();

	public override void _Ready()
	{
		GenerateMap();
	}

	private void GenerateMap()
	{
		GD.Print($"Level {Name}: Generating map...");
		var stopwatch = System.Diagnostics.Stopwatch.StartNew();

		stopwatch.Stop();
		GD.Print($"Level {Name}: Map generated in {stopwatch.Elapsed.TotalMilliseconds} ms");
	}

	public override void _PhysicsProcess(double delta)
	{
		for (int i = 0; i < _tickables.Count; i++)
		{
			_tickables[i].PhysicsTick(delta);
		}

		for (int i = 0; i < _players.Count; i++)
		{
			Player.Player player = _players[i];
		}
	}

	public override void _Process(double delta)
	{
		for (int i = 0; i < _tickables.Count; i++)
		{
			_tickables[i].Tick(delta);
		}
	}

	public void AddTickable(ITickable tickable)
	{
		_tickables.Add(tickable);
	}

	public void RemoveTickable(ITickable tickable)
	{
		_tickables.Remove(tickable);
	}

	internal void AddPlayer(Player.Player player)
	{
		_players.Add(player);
	}

	internal void RemovePlayer(Player.Player player)
	{
		_players.Remove(player);
	}

	public static Level FindAbove(Node node)
	{
		while (node != null)
		{
			if (node is Level level)
			{
				return level;
			}

			node = node.GetParent();
		}

		return null;
	}
	
}
