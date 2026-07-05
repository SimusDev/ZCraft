using Godot;
using System;

[GlobalClass]
public partial class GDNetRpc : RefCounted
{

	public static Godot.Collections.Dictionary<string, Variant> DefaultConfig = new(){

		["permission"] = "authority",
		["transfer_mode"] = Variant.From(MultiplayerPeer.TransferModeEnum.Reliable),
		["channel"] = 0,

	};

	private static bool Validate(long peer, long authority, Godot.Collections.Dictionary<string, Variant> config)
	{
		string permission = config["permission"].AsString();

		return permission switch
		{
			"authority" => peer == authority,
			"server" => peer == 1,
			_ => true
		};
	}

	public static GDNetRpc Config(Callable callable, Godot.Collections.Dictionary<string, Variant> config)
	{
		GDNetRpc rpc = new();
		return rpc;
	}

	private void Init(Godot.Collections.Dictionary<string, Variant> config)
	{

	}

	public void Invoke(Godot.Collections.Array args)
	{

	}

	public void InvokeFor(long id, Godot.Collections.Array args)
	{

	}


}
