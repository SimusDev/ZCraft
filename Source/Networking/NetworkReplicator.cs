using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;
using ZCraft.Source.Serialization;

namespace ZCraft.Source.Networking;

public static class NetworkReplicator
{
    #nullable enable
    public static Dictionary<NetworkId, INetworkReplicationObject> _objectsById = new();
    public static Dictionary<INetworkReplicationObject, NetworkId> _idByObjects = new();

    public static int ActiveObjectCount => _objectsById.Count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NetworkId GetObjectNetworkId(INetworkReplicationObject @object)
    {
        if (_idByObjects.TryGetValue(@object, out var netId))
        {
            return netId;
        }

        throw new ArgumentException($"INetworkReplicationObject Id was not found {@object}, use NetworkReplicator.AddObject to register with Id");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static INetworkReplicationObject? TryGetObjectByNetworkId(NetworkId id)
    {
        if (_objectsById.TryGetValue(id, out var @object))
        {
            return @object;
        }

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddObject(INetworkReplicationObject @object, NetworkId networkId)
    {
        _objectsById[networkId] = @object;
        _idByObjects[@object] = networkId;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RemoveObject(INetworkReplicationObject @object)
    {
        if (_idByObjects.TryGetValue(@object, out var networkId))
        {
            _idByObjects.Remove(@object);
            _objectsById.Remove(networkId);
        }
    }

    internal static void OnObjectMessagePacket(int fromPeer, BytePacker packer)
    {
        NetworkId.TypeEnum netIdType = (NetworkId.TypeEnum)packer.ReadByte();
        ulong Id = packer.ReadVarULong();
        int bytesLength = packer.ReadVarInt();

        NetworkId netId = new(netIdType, Id);

        if (bytesLength <= 0 || bytesLength > packer.Remaining)
            throw new InvalidOperationException(
                $"Invalid payload length: {bytesLength}, remaining: {packer.Remaining}");

        
        INetworkReplicationObject? @object = TryGetObjectByNetworkId(netId);

        if (@object != null)
        {
            @object.OnNetworkMessage(fromPeer, packer);
        }

        #if DEBUG
        else
        {
            GD.PushError($"Failed to deliver message. INetworkReplicationObject by (ID: {netId}) was not found on your registry.");
        }
        #endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SendMessageToServer(INetworkReplicationObject @object, BytePacker bytePacker, NetworkManager.NetChannel channel = NetworkManager.NetChannel.Reliable)
    {
        if (NetworkManager.IsServer)
        {
            return;
        }
        
        BytePacker packer = BytePackerPool.Rent();

        try
        {
            SerializeMessage(@object, packer, bytePacker);

            NetworkManager.Instance.SendPacketToPeer(
            NetworkManager.ServerId,
            packer.WrittenSpan,
            NetworkManager.NetChannelToMultiplayerTransferMode(channel),
            (int)channel
            );
        }

        catch(Exception e)
        {
            GD.PushError($"SendMessageToServer failed: {e.Message}");
        }

        finally
        {
            if (bytePacker.IsFromPool)
                BytePackerPool.Return(bytePacker);
            
            BytePackerPool.Return(packer);
        }

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SendMessageToPeers(HashSet<int> peers, INetworkReplicationObject @object, BytePacker bytePacker, NetworkManager.NetChannel channel = NetworkManager.NetChannel.Reliable)
    {
        if (peers.Count < 1)
            return;
        
        if (!NetworkManager.IsServer)
        {
            throw new Exception("Only server can messages to peers");
        }

        BytePacker packer = BytePackerPool.Rent();

        try
        {
            SerializeMessage(@object, packer, bytePacker);

            foreach(int peer in peers)
            {
                NetworkManager.Instance.SendPacketToPeer(
                peer,
                packer.WrittenSpan,
                NetworkManager.NetChannelToMultiplayerTransferMode(channel),
                (int)channel
                );
            }
        }

        catch(Exception e)
        {
            GD.PushError($"SendMessageToPeers failed: {e.Message}");
        }

        finally
        {
            if (bytePacker.IsFromPool)
                BytePackerPool.Return(bytePacker);
            
            BytePackerPool.Return(packer);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SendMessageToPeer(int peer, INetworkReplicationObject @object, BytePacker bytePacker, NetworkManager.NetChannel channel = NetworkManager.NetChannel.Reliable)
    {
        if (!NetworkManager.IsServer)
        {
            throw new Exception("Only server can messages to peers");
        }

        BytePacker packer = BytePackerPool.Rent();

        try
        {
            SerializeMessage(@object, packer, bytePacker);

            NetworkManager.Instance.SendPacketToPeer(
            peer,
            packer.WrittenSpan,
            NetworkManager.NetChannelToMultiplayerTransferMode(channel),
            (int)channel
            );
        }

        catch(Exception e)
        {
            GD.PushError($"SendMessageToPeers failed: {e.Message}");
        }

        finally
        {
            if (bytePacker.IsFromPool)
                BytePackerPool.Return(bytePacker);
            
            BytePackerPool.Return(packer);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SerializeMessage(INetworkReplicationObject @object, BytePacker packer, BytePacker message)
    {
        NetworkId netId = GetObjectNetworkId(@object);

        packer.WriteByte((byte)NetworkManager.PacketHeader.NetworkObjectMessage);
        packer.WriteByte((byte)netId.Type);
        packer.WriteVarULong(netId.Value);
        packer.WriteVarInt(message.Length);
        packer.WriteBytes(message.WrittenSpan);
    }


}
