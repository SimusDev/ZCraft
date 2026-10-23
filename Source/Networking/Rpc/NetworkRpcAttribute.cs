using Godot;
using System;

namespace ZCraft.Source.Networking.Rpc;

[AttributeUsage(AttributeTargets.Method)]
public class NetworkRpc : Attribute
{
    public SendTo Type { get; set; }
    public SendMode SendMode { get; init; } = SendMode.Reliable;

    public bool RunLocally { get; set; } = false;
    public bool RequireAuthority { get; set; } = true;

    public NetworkRpc(SendTo type = SendTo.Observer)
    {
        Type = type;
    }
}

public enum SendTo : byte
{
    Server,
    Observer,
}

public enum SendMode : byte
{
    Reliable = 0,
    Unreliable,
    UnreliableOrdered
}



