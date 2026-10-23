using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ZCraft.Source.Networking
{
    public class NetworkReplicationObject
    {
        private ulong _networkId = 0;
        private INetworkObject _owner;

        public ulong NetworkID => _networkId;
        public INetworkObject Owner => _owner;

        private Dictionary<Delegate, ushort> _rpcRegistryDelegateToId = new();
        private Dictionary<ushort, Delegate> _rpcRegistryIdToDelegate = new();

        private Dictionary<ushort, Rpc.RpcConfig> _rpcConfigRegistryId = new();

        private ushort _nextRpcId = 0;

        public void Destroy()
        {
            _owner = null;
            _rpcConfigRegistryId = null;
            _rpcRegistryDelegateToId = null;
            _rpcConfigRegistryId = null;
        }

        public NetworkReplicationObject(ulong networkId, INetworkObject owner)
        {
            _networkId = networkId;
            _owner = owner;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetRpcId(Delegate @delegate, out ushort id)
        {
            return _rpcRegistryDelegateToId.TryGetValue(@delegate, out id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetDelegateByRpcId(ushort id, out Delegate @delegate)
        {
            return _rpcRegistryIdToDelegate.TryGetValue(id, out @delegate);
        }

        public void BindDelegate(Delegate method, Rpc.RpcConfig config)
        {
            _rpcConfigRegistryId[_nextRpcId] = config;
            _rpcRegistryDelegateToId[method] = _nextRpcId;
            _rpcRegistryIdToDelegate[_nextRpcId] = method;
            _nextRpcId++;
        }

        public void TryBindInternalDelegates()
        {

        }
    }
}
