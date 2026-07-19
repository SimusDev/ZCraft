using Godot;
using System;

public partial class GameServer : Node
{
	public override void _Ready()
	{
		GDNet.Instance.Setup();
	}

	public override void _Process(double delta)
	{
	}
}
