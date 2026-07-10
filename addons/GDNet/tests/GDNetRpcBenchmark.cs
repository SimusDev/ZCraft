using Godot;
using System;

[GlobalClass]
public partial class GDNetRpcBenchmark : Node
{
	[Export] private int _SendRate = 32;
	private double _SendTime = 0;

	[Export] private int Count = 1;
	[Export] private Godot.Collections.Array Args = new();

	public GDNetRpc _rpcTest;

	public override void _Ready()
	{
		_rpcTest = GDNetRpc.Config(new Callable(this, "_RpcTest"));
	}

	public override void _Process(double delta)
	{
		if (Multiplayer.GetUniqueId() != GetMultiplayerAuthority())
			return;

		_SendTime += delta;

		if (_SendTime >= 1.0 / _SendRate)
		{
			_SendTime = 0;

			for (int i = 0; i < Count; i++)
			{
				_rpcTest.Invoke(Args);
			}
		}
	}

	public void _RpcTest(Variant value)
	{

	}
}
