using Godot;
using System;

[GlobalClass]
public partial class RGameResource : Resource
{
	[Export] private Godot.Collections.Array<RGameResource> _children = new();

#nullable enable
	public T? FindChildOrThis<T>() where T : RGameResource
	{
		if (this is T)
			return (T)this;

		foreach(var child in _children)
		{
			if (child is T)
				return child as T;
		}

		return null;
	}
#nullable disable



}
