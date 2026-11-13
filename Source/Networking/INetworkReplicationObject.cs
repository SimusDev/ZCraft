using System;
using ZCraft.Source.Serialization;

namespace ZCraft.Source.Networking;

public interface INetworkReplicationObject
{
    void OnNetworkMessage(int fromPeer, BytePacker packer);
}
