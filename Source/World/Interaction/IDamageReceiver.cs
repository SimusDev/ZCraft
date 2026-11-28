using System;

namespace ZCraft.Source.World.Interaction;

public interface IDamageReceiver
{
    public void ApplyDamage(DamageContext context);
}
