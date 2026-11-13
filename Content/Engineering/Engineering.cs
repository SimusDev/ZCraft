using Godot;
using ZCraft.Source.Content;
using ZCraft.Source.World.Item;

namespace ZCraft.Source.Content.Engineering;
public partial class Engineering : Content
{
	public override void OnEnable()
	{
		Registry.AddItem(this, "test_engineer_item", new Item());
	}
}
