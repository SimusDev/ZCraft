
namespace ZCraft.Source.World.Environment.Explosion;

public class Explosion
{
    public Explosion() {}

    private float _strength = 1;
    private float _baseDamage = 10;
    public Explosion SetStrength(float strength)
    {
        _strength = strength;
        return this;
    }

    public float GetStrength() => _strength;

    public Explosion SetBaseDamage(float damage)
    {
        _baseDamage = damage;
        return this;
    }

    public float GetBaseDamage() => _baseDamage;

    public void Explode(Level level, Godot.Vector3 globalPosition)
    {
        Environment.ExplosionSystem.Explode(level, globalPosition, this);
    }

}
