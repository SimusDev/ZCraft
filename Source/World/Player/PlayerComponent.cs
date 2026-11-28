using System;
using Godot;
using ZCraft.Source.Networking;
using ZCraft.Source.Scenes;
using ZCraft.Source.Serialization;
using ZCraft.Source.UI;
using ZCraft.Source.World.Environment.Explosion;

namespace ZCraft.Source.World.Player;

[GlobalClass]
public partial class PlayerComponent : Node, ILevelObject, INetworkReplicationObject
{
	[Export] public Node RootNode;

    enum PacketType
    {
        Test,
    }

    public Level Level;
	
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
	public void LevelObjectCreate(Level level, NetworkId id, NetworkRegion network)
    {
        Level = level;
        NetworkReplicator.AddObject(this, id);
    }

	public void LevelObjectDestroy(Level level)
    {
        NetworkReplicator.RemoveObject(this);
    }

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
        if (Input.IsActionJustPressed("pipi"))
        {
            BytePacker packer = BytePackerPool.Rent();
            packer.WriteByte((byte)PacketType.Test);
            NetworkReplicator.SendMessageToServer(this, packer);
            BytePackerPool.Return(packer);
        }
    }

    public void OnNetworkMessage(int fromPeer, BytePacker packer)
    {
        PacketType packet = (PacketType)packer.ReadByte();
        switch(packet)
        {
            case PacketType.Test:
                new Explosion().SetStrength(5).Explode(Level, new Vector3(30, 0, 0));
                break;
        }
    }
}
