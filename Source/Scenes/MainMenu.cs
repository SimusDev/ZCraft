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

	public override void _Ready()
	{
		Multiplayer.ConnectedToServer += OnConnectedToServer;

		_buttonServer.Pressed += CreateServer;
		_buttonClient.Pressed += CreateClient;

	}

	public override void _Process(double delta)
	{

	}

	private void OnConnectedToServer()
	{
		ChangeSceneToWorld();
	}

	private void ChangeSceneToWorld()
	{
		GetTree().CallDeferred("change_scene_to_file", "res://Scenes/SetupWorldAndContent.tscn");
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
