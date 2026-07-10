using Godot;
using System;


public partial class GDNetRpc : RefCounted
{
	public static Godot.Collections.Dictionary<string, Variant> DefaultConfig = new(){

		["permission"] = "authority",
		["transfer_mode"] = Variant.From(MultiplayerPeer.TransferModeEnum.Reliable),
		["channel"] = 0,

	};

	public Godot.Collections.Dictionary<string, Variant> Cfg = new();

	public int RemoteSenderID = 0;

	private WeakRef _ownerRef;

	public GodotObject Owner => (GodotObject)_ownerRef.GetRef();

	public static GDNetRpc Config(Callable callable, Godot.Collections.Dictionary<string, Variant> config)
	{
		GDNetRpc rpc = new();
		rpc.Init(callable, config);
		return rpc;
	}

	public static GDNetRpc Config(Callable callable)
	{
		return Config(callable, new());
	}

	private void Init(Callable callable, Godot.Collections.Dictionary<string, Variant> config)
	{
		GodotObject obj = callable.Target;
		if (obj == null)
		{
			GD.PushError($"Failed to initialize Rpc, object is null: {callable.ToString()}:{obj}");
			return;
		}

		_ownerRef = WeakRef(obj);

		Cfg = DefaultConfig.Duplicate();

		foreach (var pair in config)
		{
			Cfg[pair.Key] = pair.Value;
		}

		GDNetAutoHashID.Create(obj);
	}

	public void _GDInvoke(Godot.Collections.Array args)
	{
		GDNetRpcProcessor.Instance.Invoke(this, args);
	}

	public void _GDInvokeFor(long id, Godot.Collections.Array args)
	{
		GDNetRpcProcessor.Instance.InvokeFor(this, args);
	}

	public void Invoke(params Variant[] args)
	{
		Godot.Collections.Array gdArgs = new();
		foreach(var v in args)
		{
			gdArgs.Add(v);
		}

		_GDInvoke(gdArgs);
	}

	public void InvokeFor(long id, params Variant[] args)
	{
		Godot.Collections.Array gdArgs = new();
		foreach (var v in args)
		{
			gdArgs.Add(v);
		}

		_GDInvokeFor(id, gdArgs);
	}

}
