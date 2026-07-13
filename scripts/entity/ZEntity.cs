using Godot;
using System;

public partial class ZEntity : Node
{
	[Export] Godot.Collections.Array<ZEntityComponent> _components = new();

}
