using Godot;
using System;
using System.Collections.Generic;
using ZCraft.Source.Networking;
using ZCraft.Source.UI;
using ZCraft.Source.World;
using ZCraft.Source.World.Player;

namespace ZCraft.Source.Scenes;

public partial class MainMenu : Control
{
	[Export] private bool InGame = false;
	[Export] private Control _screensRoot;
	[Export] private Button _buttonMainMultiplayer;
	[Export] private Button _buttonMainSettings;
	[Export] private Button _buttonMainQuit;
	[Export] private Button _buttonMainContinue;
	[Export] private Button _buttonMainDisconnect;
	[Export] private LineEdit _multiplayerAddressLineEdit;
	[Export] private LineEdit _multiplayerNicknameLineEdit;
	[Export] private Button _buttonMultiplayerConnect;

	[Export] private CanvasLayer _continueButtonPressHide;

	public static MainMenu _instance;

	#nullable enable
	public static MainMenu? Get() => _instance;
	#nullable disable

	public void SwitchScreen(string name)
	{
		foreach(var node in _screensRoot.GetChildren())
		{
			if (node is CanvasItem item)
			{
				item.Visible = item.Name == name;
			}
		}
	}

	public override void _EnterTree()
	{
		_instance = this;
	}

	public override void _ExitTree()
	{
		_instance = null;
	}

	public override void _Ready()
	{
		_buttonMainMultiplayer.Pressed += OnMainMultiplayerPressed;
		_buttonMainSettings.Pressed += OnMainSettingsPressed;
		_buttonMainQuit.Pressed += OnMainQuitPressed;
		_buttonMainContinue.Pressed += OnMainContinuePressed;
		_buttonMainDisconnect.Pressed += OnMainDisconnectPressed;

		_screensRoot.GetNode<Button>("Settings/Back").Pressed += ReturnToMainScreen;
		_screensRoot.GetNode<Button>("Multiplayer/Back").Pressed += ReturnToMainScreen;

		_buttonMultiplayerConnect.Pressed += OnMultiplayerConnectPressed;

		if (InGame)
		{
			_buttonMainContinue.Show();
			_buttonMainDisconnect.Show();
			_buttonMainMultiplayer.Hide();
			_buttonMainQuit.Hide();
		}

		else
		{
			Multiplayer.ConnectedToServer += OnConnectedToServer;
		}

		SwitchScreen("Main");
	}

	private void OnMultiplayerConnectPressed()
	{
		PlayerProfile.LocalConnectionNickName = _multiplayerNicknameLineEdit.Text;

		string[] splitted = _multiplayerAddressLineEdit.Text.Split(":");
		if (splitted.Length != 2)
		{
			GD.PrintErr("Invalid Address");
			return;
		}

		Error error = NetworkManager.Instance.StartClient(splitted[0], splitted[1].ToInt());
		if (error != Error.Ok)
		{
			GD.PrintErr(error);
		}
	}

	private void ReturnToMainScreen()
	{
		SwitchScreen("Main");
	}

	private void OnMainDisconnectPressed()
	{
		throw new NotImplementedException();
	}


	private void OnMainContinuePressed()
	{
		_continueButtonPressHide.Hide();
	}


	private void OnMainQuitPressed()
	{
		GetTree().Quit();
	}

	private void OnMainSettingsPressed()
	{
		SwitchScreen("Settings");
	}


	private void OnMainMultiplayerPressed()
	{
		SwitchScreen("Multiplayer");
	}

	private void OnConnectedToServer()
	{
		ChangeSceneToWorld();
	}

	private void ChangeSceneToWorld()
	{
		GetTree().CallDeferred("change_scene_to_file", "res://Scenes/SetupWorldAndContent.tscn");
	}

}
