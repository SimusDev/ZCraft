using Godot;
using System.Collections.Generic;
using ZCraft.Source.Networking;

namespace ZCraft.Source.World.Player
{
	[GlobalClass]
	public partial class Player : Node3D
	{
		[Export] public Node3D Root;

		[Export] public PlayerCameraControl CameraControl;
		[Export] public PlayerMovementControl MovementControl;

		public Level Level => _level;
		public List<Player> All => _all;

		private Level _level;
		private List<Player> _all = new();

		public override void _EnterTree()
		{
			_level = Level.FindAbove(this);
			_all.Add(this);
		}

		public override void _ExitTree()
		{
			_all.Remove(this);
		}

	}
}
