using Godot;
using System;

public partial class GameWorld : Node3D
{
    [Export] private Button _testButtonSend;

    GDNetRpc _rpc = new();

    private Random _random = new Random(Guid.NewGuid().GetHashCode());

    public override void _Ready()
    {
        _rpc.BindOwnerAsNode(this);
        _rpc.BindAll(this);

        _testButtonSend.Pressed += OnTestSendPressed;
    }

    public override void _Process(double delta)
    {
        return;
        if (Multiplayer.IsServer())
        {
            for (int i = 0; i < 2000; i++)
            {
                _rpc.Invoke(_RpcTest, new Vector3(), new Vector3());
            }
        }
    }

    private void OnTestSendPressed()
    {
        _rpc.InvokeOn(1, _RpcTest, new Vector3(), new Vector3());
    }

    [GDNetRpc(permission: Permission.Any, Channel = 1, Mode = Mode.Reliable)]
    private void _RpcTest(Vector3 v1, Vector3 v2)
    {
        GD.Print($"Hello From {_rpc.GetRemoteSender()}");
    }
}
