using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ZCraft.Source.Networking;
using ZCraft.Source.Serialization;

namespace ZCraft.Source.World;

public class LevelObjectWrapper
{
    private List<int> _peers;
    private Level _level;
    public List<int> Peers => _peers;
    public Level Level => _level;

    public LevelObjectWrapper(Level level)
    {
        _level = level;
    }

    public void SetPeers(List<int> peers)
    {
        _peers = peers;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClientSend(INetworkReplicationObject @object, BytePacker packer, NetworkManager.NetChannel channel)
    {
        NetworkReplicator.SendMessageToServer(@object, packer, channel);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ServerBroadcast(INetworkReplicationObject @object, BytePacker packer, NetworkManager.NetChannel channel)
    {
        NetworkReplicator.SendMessageToPeers(Peers, @object, packer, channel);
    }
}
