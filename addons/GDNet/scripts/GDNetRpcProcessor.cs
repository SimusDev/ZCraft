using Godot;
using System;

[GlobalClass]
public partial class GDNetRpcProcessor : Node
{
	public static bool Validate(long peer, long authority, Godot.Collections.Dictionary<string, Variant> config)
	{
		string permission = config["permission"].AsString();

		return permission switch
		{
			"authority" => peer == authority,
			"server" => peer == 1,
			_ => true
		};
	}



}
