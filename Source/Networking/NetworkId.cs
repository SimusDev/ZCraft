using System;
using ZCraft.Source.Serialization;

namespace ZCraft.Source.Networking;

public readonly struct NetworkId : IEquatable<NetworkId>
{
    public enum TypeEnum : byte
    {
        Unique,
        ULong,
        StringHash,
        NodePathHash,
    }

    public readonly TypeEnum Type { get; init; }
    public readonly ulong Value { get; init; }

    private static ulong _nextId = 0;

    public NetworkId()
    {
        Type = TypeEnum.Unique;
        Value = _nextId;
        _nextId++;
    }

    public NetworkId(ulong id)
    {
        Type = TypeEnum.ULong;
        Value = id;
    }

    public NetworkId(string name)
    {
        Type = TypeEnum.StringHash;
        Value = StringHash.Fnv1a64(name);
    }

    public NetworkId(Godot.NodePath nodePath)
    {
        Type = TypeEnum.NodePathHash;
        Value = StringHash.Fnv1a64(nodePath.ToString());
    }

    public NetworkId(TypeEnum type, ulong value)
    {
        Type = type;
        Value = value;
    }

    public bool Equals(NetworkId other) => Type == other.Type && Value == other.Value;
    public override bool Equals(object? obj) => obj is NetworkId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public static bool operator ==(NetworkId a, NetworkId b) => a.Equals(b);
    public static bool operator !=(NetworkId a, NetworkId b) => !a.Equals(b);
}