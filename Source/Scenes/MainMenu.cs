using Godot;
using System;

namespace ZCraft.Source.Scenes;

public partial class MainMenu : Control
{
	[Export] private Button _buttonServer;
	[Export] private Button _buttonClient;
	[Export] private LineEdit _ipLine;

	public const int Port = 8080;

	public override void _Ready()
	{
		SceneMultiplayer multiplayer = new();
		multiplayer.ServerRelay = false;
		GetTree().SetMultiplayer(multiplayer);

		Multiplayer.ConnectedToServer += OnConnectedToServer;

		_buttonServer.Pressed += CreateServer;
		_buttonClient.Pressed += CreateClient;
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
		ENetMultiplayerPeer peer = new ENetMultiplayerPeer();
		Error err = peer.CreateServer(Port);
		if (err == Error.Ok)
		{
			Multiplayer.MultiplayerPeer = peer;
			ChangeSceneToWorld();
		}

	}

	public void CreateClient()
	{
		ENetMultiplayerPeer peer = new ENetMultiplayerPeer();
		Error err = peer.CreateClient(_ipLine.Text, Port);
		if (err == Error.Ok)
		{
			Multiplayer.MultiplayerPeer = peer;
		}
	}
}
