using Godot;
using System;

public partial class NetCore : Node
{
	[Signal]
	public delegate void MultiplayerPeerPacketEventHandler(long id, byte[] bytes);

	public SceneMultiplayer API = null;

	public enum CompressionHeader: byte
	{
		Deflate,
		Zstd,
	}

    public override void _Ready()
    {

    }

    public void Setup(SceneMultiplayer api)
	{
		GetTree().SetMultiplayer(api);
		api.PeerPacket += OnApiPeerPacket;
		API = api;
	}

    private void OnApiPeerPacket(long id, byte[] packet)
    {
        throw new NotImplementedException();
    }

    public void MultiplayerSendBytes(byte[] bytes, int id, MultiplayerPeer.TransferModeEnum mode, int channel)
	{
		API.SendBytes(bytes, id, mode, channel);
	}

}
