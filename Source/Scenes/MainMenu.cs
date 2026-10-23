using Godot;
using System;
using System.Collections.Generic;
using ZCraft.Source.Networking;
using ZCraft.Source.World;

namespace ZCraft.Source.Scenes;

public partial class MainMenu : Control
{
	[Export] private Button _buttonServer;
	[Export] private Button _buttonClient;
	[Export] private LineEdit _ipLine;

	public const int Port = 8080;

	private List<ITickable> tickables = new();

	public override void _Ready()
	{
		Multiplayer.ConnectedToServer += OnConnectedToServer;

		_buttonServer.Pressed += CreateServer;
		_buttonClient.Pressed += CreateClient;

		for (int i = 0; i < 1000000; i++)
		{
			var tickableNode = new ITickableTest();
			AddChild(tickableNode);
			tickables.Add(tickableNode);
		}

	}

	public override void _Process(double delta)
	{
		for (int i = 0; i < tickables.Count; i++)
		{
			tickables[i].Tick(delta);
		}
	}

	private void OnConnectedToServer()
	{
		ChangeSceneToWorld();
	}

	private void ChangeSceneToWorld()
	{
		GetTree().CallDeferred("change_scene_to_file", "res://Scenes/World.tscn");
	}

	public void CreateServer()
	{
		Error err = NetworkManager.Instance.StartServer(Port);
		if (err == Error.Ok)
		{
			ChangeSceneToWorld();
		}
	}

	public void CreateClient()
	{
		NetworkManager.Instance.StartClient("localhost", Port);
	}
}
