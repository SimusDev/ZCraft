using Connection;
using Godot;
using System;

public partial class MainMenu : Control
{
	[Export] private LineEdit _connectionLineEdit;
	[Export] private Godot.Collections.Array<Button> _hookButtons = new();
	public override void _Ready()
	{
		foreach (var button in _hookButtons)
		{
			button.Pressed += () => OnButtonPressed(button);
		}

	}

	private void OnButtonPressed(Button button)
	{
		switch (button.Name)
		{
			case "Quit":
				GetTree().Quit();
				break;
			case "Connect":
				GameServer.Instance.CreateClient(_connectionLineEdit.Text);
				break;
			case "Run Server":
				GameServer.Instance.CreateServer();
				break;

		}
	}
}
