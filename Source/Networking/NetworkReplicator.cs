using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Xml;

namespace ZCraft.Source.Networking
{
    public static class NetworkReplicator
    {
        private static Dictionary<INetworkObject, NetworkReplicationObject> _registry = new();
        private static Dictionary<ulong, NetworkReplicationObject> _registryId = new();

        public static bool EnableLog = true;

        [ThreadStatic] private static Serialization.BufferStream _RpcBuffer = new(512);

        public static ulong HashStringDetermenistic(string str)
        {
            if (str == null)
                return 0;

            str += "ZCraftReplication";

            byte[] data = System.Text.Encoding.UTF8.GetBytes(str);

            ulong hash = 0x9e3779b97f4a7c15UL;

            for (int i = 0; i < data.Length; i++)
            {
                hash ^= data[i];
                hash *= 0xbf58476d1ce4e5b9UL;
                hash = (hash << 27) | (hash >> 37);
            }

            return hash;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddObjectInternal(INetworkObject @object, string hash)
        {
            if (HasObject(@object))
            {
                GD.PushError($"Network Object {@object} already in replication!");
                return;
            }

            ulong networkId = HashStringDetermenistic(hash);
            var replicationObject = new NetworkReplicationObject(networkId, @object);
            _registry[@object] = replicationObject;
            _registryId[networkId] = replicationObject;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddObject(INetworkObject @object, ulong id)
        {
            AddObjectInternal(@object, $"ByUlong_{id})");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddObject(INetworkObject @object, string id)
        {
            AddObjectInternal(@object, $"ByString_{id}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddObject(INetworkObject @object, NodePath id)
        {
            AddObjectInternal(@object, $"ByNodePath_{id}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RemoveObject(INetworkObject @object)
        {
            if (_registry.TryGetValue(@object, out NetworkReplicationObject replicationObject))
            {
                _registry.Remove(@object);
                _registryId.Remove(replicationObject.NetworkID);
                replicationObject.Destroy();
                replicationObject = null;
            }

            else
            {
                if (EnableLog)
                {
                    GD.PushError($"Network Object {@object} was not found, use NetworkReplicator.AddObject");
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasObject(INetworkObject @object)
        {
            return _registry.ContainsKey(@object);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetReplicationObject(INetworkObject @object, out NetworkReplicationObject replicationObject)
        {
            if (_registry.TryGetValue(@object, out replicationObject))
                return true;

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetReplicationObjectByNetworkId(ulong networkId, out NetworkReplicationObject replicationObject)
        {
            return _registryId.TryGetValue(networkId, out replicationObject);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SerializePacketHeader(Serialization.BufferStream stream)
        {
            stream.WriteByte(0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SerializeRpc(Serialization.BufferStream stream, ulong networkId, ushort rpcId)
        {
            stream.WriteUInt64(networkId);
            stream.WriteUInt16(rpcId);
        }

        public static void RpcInvoke(INetworkObject @object, Delegate method, params object[] args)
        {
            if (!TryGetReplicationObject(@object, out NetworkReplicationObject replicationObject))
            {
                if (EnableLog)
                    GD.PushError($"Can't invoke Rpc on unregistered object, {@object}:{method.Method.Name}, use NetworkReplicator.AddObject");

                return;
            }

            if (!replicationObject.TryGetRpcId(method, out ushort rpcId))
            {
                if (EnableLog)
                    GD.PushError($"Can't invoke Rpc on unregistered Method {@object}:{method.Method.Name}, rpcId was not found!");
               
                return;
            }

            if (NetworkManager.IsServer)
            {
                return;
            }

            SerializePacketHeader(_RpcBuffer);
            SerializeRpc(_RpcBuffer, replicationObject.NetworkID, rpcId);
            for (byte i  = 0; i < args.Length;  i++)
            {
                
            }

        }

        public static void RpcInvoke(INetworkObject @object, Delegate method, ref object arg1)
        {
            if (!TryGetReplicationObject(@object, out NetworkReplicationObject replicationObject))
                return;

        }

        public static void RpcInvoke(INetworkObject @object, Delegate method, ref object arg1, ref object arg2)
        {
            if (!TryGetReplicationObject(@object, out NetworkReplicationObject replicationObject))
                return;
        }

        public static void RpcInvoke(INetworkObject @object, Delegate method, ref object arg1, ref object arg2, ref object arg3)
        {
            if (!TryGetReplicationObject(@object, out NetworkReplicationObject replicationObject))
                return;
        }

        public static void RpcInvoke(INetworkObject @object, Delegate method, ref object arg1, ref object arg2, ref object arg3, ref object arg4)
        {
            if (!TryGetReplicationObject(@object, out NetworkReplicationObject replicationObject))
                return;
        }

    }
}
