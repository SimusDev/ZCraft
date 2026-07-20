using Godot;
using System;


namespace Models.Visuals
{
	[Tool]
	public partial class HumanVisual : Node3D
	{
		[Export] private Godot.Collections.Array<MeshInstance3D> _meshes = new();

		private StandardMaterial3D _visualMaterial = null;

		[Export] public StandardMaterial3D VisualMaterial
		{
			set
			{
				_visualMaterial = value;
				UpdateRender();
			}

			get { return _visualMaterial; }
		}

		public StandardMaterial3D CreateMaterial(Texture2D humanTexture)
		{
			var material = new StandardMaterial3D();

			material.AlbedoTexture = humanTexture;
			material.TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest;

			return material;
		}

		private void UpdateRender()
		{
			foreach (var mesh in _meshes)
			{
				mesh.MaterialOverride = VisualMaterial;
			}
		}

	}

}
