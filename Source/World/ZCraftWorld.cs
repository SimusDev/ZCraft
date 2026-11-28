using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Threading.Channels;
using Godot;
using ZCraft.Source.Content;
using ZCraft.Source.Entity;
using ZCraft.Source.Networking;
using ZCraft.Source.Serialization;
using ZCraft.Source.World;
using ZCraft.Source.World.Player;
using ZCraft.Source.World.Synchronizers;
using ZCraft.Source.World.View;

namespace ZCraft.Source;

public partial class ZCraftWorld : Node
{
	[Export] private PackedScene _playerScene;
	public event Action<PlayerProfile> PlayerConnected;
	public event Action<PlayerProfile> PlayerDisconnected;
	private static ulong _nextUniqueId = 0;

	public static ulong GenerateUniqueId()
	{
		_nextUniqueId++;
		return _nextUniqueId;
	}
	public override void _Ready()
	{
		if (NetworkManager.IsServer)
		{
			ZCraftWorldDebug.ServerStartTimeStopWatch.Stop();
			GD.Print($"Server started in {ZCraftWorldDebug.ServerStartTimeStopWatch.Elapsed.Milliseconds} ms");
			
			FreeCamera camera = new();
			camera.Position = new Vector3(camera.Position.X, camera.Position.Y + 20, camera.Position.Z);
			AddChild(camera);

			Multiplayer.PeerDisconnected += OnPeerDisconnected;
		}

		else
		{
			RpcId(NetworkManager.ServerId, MethodName.SendAuthDataToServer, PlayerProfile.LocalConnectionNickName);
		}


	}

	public override void _PhysicsProcess(double delta)
	{
		Entity.Ticker.FixedRegistry.Tick(delta);
	}

	public override void _Process(double delta)
	{
		Entity.Ticker.Registry.Tick(delta);
	}

	public Level GetPlayerStartLevel()
	{
		return Level.All[0];
	}

	private void OnPeerDisconnected(long id)
	{
		if (!NetworkManager.IsServer)
			return;
		
		var profile = PlayerProfile.GetProfileByPeerId((int)id);
		if (profile != null)
		{
			PlayerDisconnected?.Invoke(profile);
			GD.Print($"Player {profile.NickName} disconnected");
			Rpc(MethodName.BroadcastPlayerProfile, profile.PeerId);
			PlayerProfile.RemoveProfileFromBase(profile);
		}
	}
	
	[Rpc(MultiplayerApi.RpcMode.Authority, TransferChannel = (int)NetworkManager.NetChannel.Reliable, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void BroadcastPlayerDisconnect(int peerId)
	{
		var profile = PlayerProfile.GetProfileByPeerId((int)peerId);
		if (profile != null)
		{
			PlayerDisconnected?.Invoke(profile);
			GD.Print($"Player {profile.NickName} disconnected");
			PlayerProfile.RemoveProfileFromBase(profile);
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferChannel = (int)NetworkManager.NetChannel.Reliable, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void SendAuthDataToServer(string nickname)
	{
		foreach (var profile in PlayerProfile.All)
		{
			if (profile.NickName == nickname)
			{
				nickname = nickname + "_";
			}
		}
		
		int remoteSender = Multiplayer.GetRemoteSenderId();

		PlayerProfile player = new(remoteSender, nickname);
		PlayerProfile.AddProfileToBase(player);
		PlayerConnected?.Invoke(player);

		PlayerLevelTracker pickedPlayerTracker = null;
		Level pickedPlayerLevel = null;

		foreach(Level level in Level.All)
		{
			foreach (PlayerLevelTracker tracker in level.PlayerTrackers)
			{
				if (tracker.NickName == nickname)
				{
					pickedPlayerTracker = tracker;
					pickedPlayerLevel = tracker.GetLevel();
				} 
			}
		}

		if (pickedPlayerLevel == null)
			pickedPlayerLevel = GetPlayerStartLevel();

		if (pickedPlayerTracker == null)
		{
			Node playerNode = _playerScene.Instantiate();
			playerNode.Name = remoteSender.ToString();
			PlayerLevelTracker tracker = (PlayerLevelTracker)playerNode.GetNode("PlayerLevelTracker");
			tracker.NickName = nickname;
			playerNode.SetMultiplayerAuthority(remoteSender);
			pickedPlayerLevel.AddChild(playerNode);
		}

		else
		{
			pickedPlayerTracker.TrackNode.Name = remoteSender.ToString();
			pickedPlayerTracker.ResetClient(remoteSender);
		}

		pickedPlayerLevel.OnlinePlayerPeers.Add(remoteSender);

		BytePacker packer = BytePackerPool.Rent();
		player.Serialize(packer);
		packer.WriteString(pickedPlayerLevel.Name);
		packer.WriteNetworkId(NetworkReplicator.GetObjectNetworkId(pickedPlayerLevel));
		Rpc(MethodName.BroadcastPlayerProfile, packer.WrittenSpan.ToArray());
		BytePackerPool.Return(packer);
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, TransferChannel = (int)NetworkManager.NetChannel.Reliable, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void BroadcastPlayerProfile(byte[] data)
	{
		BytePacker packer = BytePackerPool.Rent();
		packer.Wrap(data);
		PlayerProfile player = new();
		player.Deserialize(packer);

		PlayerProfile.AddProfileToBase(player);
		PlayerConnected?.Invoke(player);
		GD.Print($"Player {player.NickName} connected");

		string levelName = packer.ReadString();

		if (player.PeerId == Multiplayer.GetUniqueId())
		{
			// Node playerNode = _playerScene.Instantiate();
			// playerNode.Name = player.PeerId.ToString();
			// PlayerLevelTracker tracker = (PlayerLevelTracker)playerNode.GetNode("PlayerLevelTracker");
			// tracker.NickName = player.NickName;
			// playerNode.SetMultiplayerAuthority(player.PeerId);

			Level level = new();
			level.Name = levelName;

			AddChild(level);
			//level.AddChild(playerNode);
		}

		BytePackerPool.Return(packer);
	}

	

}
