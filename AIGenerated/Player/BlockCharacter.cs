#nullable enable

using Godot;

namespace ZCraft.AIGenerated;

[Tool]
public partial class BlockCharacter : Node3D
{
	public enum ArmStyle
	{
		Classic,
		Slim
	}

	private Texture2D? _skinTexture;
	private ArmStyle _arms;
	private MeshInstance3D? _body;
	private StandardMaterial3D? _skinMaterial;
	private StandardMaterial3D? _overlayMaterial;

	[Export]
	public Texture2D? SkinTexture
	{
		get => _skinTexture;
		set => ApplySkin(value, _arms);
	}

	[Export]
	public ArmStyle Arms
	{
		get => _arms;
		set => ApplySkin(_skinTexture, value);
	}

	[ExportGroup("Rig Resources")]
	[Export] private ArrayMesh? ClassicMesh { get; set; }
	[Export] private ArrayMesh? SlimMesh { get; set; }
	[Export] private ArrayMesh? LegacyMesh { get; set; }
	[Export] private Texture2D? DefaultClassicSkin { get; set; }
	[Export] private Texture2D? DefaultSlimSkin { get; set; }

	public override void _Ready()
	{
		_body = GetNode<MeshInstance3D>("Skeleton3D/Body");
		_skinMaterial = CreateSkinMaterial(BaseMaterial3D.TransparencyEnum.Disabled);
		_overlayMaterial = CreateSkinMaterial(BaseMaterial3D.TransparencyEnum.AlphaScissor);
		UpdateAppearance();
	}

	public bool ApplySkin(Texture2D? texture, ArmStyle arms)
	{
		if (texture != null && !SupportsSkin(texture))
		{
			GD.PushWarning("BlockCharacter поддерживает скины 64×64, 128×128 и 64×32.");
			return false;
		}

		_skinTexture = texture;
		_arms = arms;
		UpdateAppearance();
		return true;
	}

	private void UpdateAppearance()
	{
		if (_body == null || _skinMaterial == null || _overlayMaterial == null)
			return;

		bool slim = _arms == ArmStyle.Slim;
		Texture2D? texture = _skinTexture ?? (slim ? DefaultSlimSkin : DefaultClassicSkin);
		bool legacy = texture != null && texture.GetWidth() == 64 && texture.GetHeight() == 32;
		ArrayMesh? mesh = legacy ? LegacyMesh : slim ? SlimMesh : ClassicMesh;
		if (mesh == null)
			return;

		_body.Mesh = mesh;
		_skinMaterial.AlbedoTexture = texture;
		_overlayMaterial.AlbedoTexture = texture;
		_overlayMaterial.AlbedoColor = legacy && texture != null && HasOpaqueLegacyHat(texture)
			? new Color(1, 1, 1, 0)
			: Colors.White;
		_body.SetSurfaceOverrideMaterial(0, _skinMaterial);
		_body.SetSurfaceOverrideMaterial(1, _overlayMaterial);
	}

	private static bool SupportsSkin(Texture2D texture)
	{
		int width = texture.GetWidth();
		int height = texture.GetHeight();
		return (width == 64 && (height == 64 || height == 32)) || (width == 128 && height == 128);
	}

	private static bool HasOpaqueLegacyHat(Texture2D texture)
	{
		using Image? image = texture.GetImage();
		if (image == null || image.IsEmpty() || (image.IsCompressed() && image.Decompress() != Error.Ok))
			return false;

		// Fully opaque textures from the old format have no hat cutout.
		for (int y = 0; y < 16; y++)
		for (int x = 32; x < 64; x++)
		{
			if (image.GetPixel(x, y).A < 0.5f)
				return false;
		}

		return true;
	}

	private static StandardMaterial3D CreateSkinMaterial(BaseMaterial3D.TransparencyEnum transparency)
	{
		return new StandardMaterial3D
		{
			Transparency = transparency,
			AlphaScissorThreshold = 0.5f,
			TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
			Roughness = 0.94f,
			MetallicSpecular = 0.18f
		};
	}
}
