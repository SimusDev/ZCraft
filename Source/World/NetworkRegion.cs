using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ZCraft.Source.Networking;
using ZCraft.Source.Serialization;

namespace ZCraft.Source.World;

public class NetworkRegion
{
    public NetworkRegion(List<int> peers)
    {
        _peers = peers;
    }

    public NetworkRegion() {}

    public List<int> Peers => _peers;
    private List<int> _peers;

    public void SetPeers(List<int> peers)
    {
        _peers = peers;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClientSend(INetworkReplicationObject @object, BytePacker bytePacker, NetworkManager.NetChannel channel = NetworkManager.NetChannel.Reliable)
    {
        NetworkReplicator.SendMessageToServer(@object, bytePacker, channel);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ServerBroadcast(INetworkReplicationObject @object, BytePacker bytePacker, NetworkManager.NetChannel channel = NetworkManager.NetChannel.Reliable)
    {
        NetworkReplicator.SendMessageToPeers(Peers, @object, bytePacker, channel);
    }

}
