using System;
using Godot;
using ZCraft.Source.Networking;
using ZCraft.Source.Scenes;
using ZCraft.Source.Serialization;
using ZCraft.Source.UI;

namespace ZCraft.Source.World.Player;

[GlobalClass]
public partial class PlayerComponent : Node, ILevelObject
{
	[Export] public Node RootNode;
	
	public override void _Ready()
    {
        if (NetworkManager.IsServer)
        {
            SetProcessInput(false);
            return;
        }
        bool isAuth = IsMultiplayerAuthority();
        SetProcessInput(isAuth);
        SetProcess(isAuth);
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("mainmenu_hotkey"))
        {
            MainMenu menu = MainMenu.Get();

            if (menu != null)
            {
                CanvasLayer canvas = menu.GetParent<CanvasLayer>();

                if (canvas.Visible)
                {
                    canvas.Hide();
                }
                else
                {
                    canvas.Show();
                }
            }
        }
    }
	public void LevelObjectCreate(Level level, NetworkId id, NetworkRegion network)  {}

	public void LevelObjectDestroy(Level level) {}

	public void SnapshotLoad(TagCompound tagCompound) {}
	public void SnapshotSave(TagCompound tagCompound) {}

	public void SnapshotNetworkRead(BytePacker packer)
	{
		int auth = packer.ReadInt();
        SetMultiplayerAuthority(auth, false);
		RootNode.SetMultiplayerAuthority(auth);
	}

	public void SnapshotNetworkWrite(BytePacker packer)
	{
		packer.WriteInt(RootNode.GetMultiplayerAuthority());
	}

    public override void _Input(InputEvent @event)
    {

    }

}
