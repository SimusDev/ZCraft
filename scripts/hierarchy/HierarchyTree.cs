using Godot;
using System;

public partial class HierarchyTree : Node
{

	private System.Collections.Generic.List<Resource> _tickingObjects = new();
	private System.Collections.Generic.List<Resource> _physicsTickingObjects = new();
	private System.Collections.Generic.List<Resource> _inputTickObjects = new();

	public override void _Ready()
	{
		SetProcess(false);
		SetPhysicsProcess(false);
		SetProcessInput(false);
		SetProcessUnhandledInput(false);
		SetProcessShortcutInput(false);
		SetProcessUnhandledKeyInput(false);

	}

	public override void _Process(double delta)
	{

	}

	public virtual void Tick(double delta)
	{

	}

	public virtual void PhysicsTick(double delta)
	{

	}
}
