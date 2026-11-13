using System;
using Godot;

namespace ZCraft.Source.World;

public readonly struct ChunkPos : IEquatable<ChunkPos>
{
    public readonly int X, Y, Z;

    public ChunkPos(int x, int y, int z) { X = x; Y = y; Z = z; }

    public ChunkPos(float worldX, float worldY, float worldZ)
    {
        X = (int)MathF.Floor(worldX / Level.ChunkSize);
        Y = (int)MathF.Floor(worldY / Level.ChunkSize);
        Z = (int)MathF.Floor(worldZ / Level.ChunkSize);
    }

    public ChunkPos(Vector3 position) : this(position.X, position.Y, position.Z) { }

    public override int GetHashCode()
        => unchecked((X * 73856093) ^ (Y * 19349663) ^ (Z * 83492791));

    public bool Equals(ChunkPos other) => X == other.X && Y == other.Y && Z == other.Z;
    public override bool Equals(object obj) => obj is ChunkPos other && Equals(other);

    public static bool operator ==(ChunkPos a, ChunkPos b) => a.Equals(b);
    public static bool operator !=(ChunkPos a, ChunkPos b) => !a.Equals(b);

    public override string ToString() => $"({X}, {Y}, {Z})";
}