using System;
using Godot;
using Godot.Collections;
using ZCraft.Source.World.Interaction;

namespace ZCraft.Source.World.Environment.Explosion;

public partial class ServerExplosionScene : Area3D
{
	public void Setup(float strength)
	{
		Scale = new Vector3(strength, strength, strength);
	}

	public override void _PhysicsProcess(double delta)
	{
		Array<Node3D> bodies = GetOverlappingBodies();
		Array<Area3D> areas = GetOverlappingAreas();

		float damagePoints = 100;

		DamageContext damage = new(this, damagePoints);

		for (int i = 0; i < bodies.Count; i++)
		{
			if (bodies[i] is IDamageReceiver receiver)
				receiver.ApplyDamage(damage);
		}

		for (int i = 0; i < areas.Count; i++)
		{
			if (areas[i] is IDamageReceiver receiver)
				receiver.ApplyDamage(damage);
		}

		SetPhysicsProcess(false);
		SetPhysicsProcessInternal(false);
		ProcessMode = ProcessModeEnum.Disabled;
		QueueFree();
	}
}
