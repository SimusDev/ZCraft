using Connection;
using Godot;
using System;

public partial class MainMenu : Control
{
	private GDNetBuffer _buffer = new();
	public override void _Ready()
	{
		_buffer.Write(this);

	}
}
