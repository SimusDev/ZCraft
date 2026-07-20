using Connection;
using Godot;
using System;

public partial class MainMenu : Control
{
	GDNetBuffer _buffer = new();

	[Export] private LineEdit _connectionLineEdit;
	[Export] private Godot.Collections.Array<Button> _hookButtons = new();
	public override void _Ready()
	{
		int maxValue = int.MaxValue;
        _buffer.WriteLongVar(long.MinValue);
		_buffer.Seek(0);
		GD.Print(_buffer.ReadLongVar());

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
