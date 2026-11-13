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

		public override void _EnterTree()
		{

		}

		public override void _ExitTree()
		{
			
		}

	}
}
