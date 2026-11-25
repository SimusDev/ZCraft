using Godot;

namespace ZCraft.Content.ZVWeaponPack;

[GlobalClass]
public partial class ZVProjectileData : Resource
{
    [Export] public float Damage = 1.0f;
    [Export] public float MuzzleVelocity = 1.0f;
    [Export] public float AirFriction = 1.0f;
    [Export] public Mesh CasingMesh = null;
}