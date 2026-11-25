using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ZCraft.Source.Networking;
using ZCraft.Source.Serialization;

namespace ZCraft.Source.UI
{
    public partial class InterfaceStack : Node, INetworkReplicationObject
    {
        public int _activeCount = 0;
        public int ActiveCount => _activeCount;
        public bool IsAnyActive => _activeCount > 0;
        public static InterfaceStack Instance => _instance;
        private static InterfaceStack _instance;

        public event Action ActiveChanged; 

        public static InterfaceStack Get()
        {
            return _instance;
        }
        
        public override void _EnterTree()
        {
            _instance = this;
            NetworkReplicator.AddObject(this, new NetworkId(GetPath()));
        }

        public override void _ExitTree()
        {
            NetworkReplicator.RemoveObject(this);
        }

        public void AddActive()
        {
            _activeCount++;
            ActiveChanged?.Invoke();
        }

        public void RemoveActive()
        {
            _activeCount--;
            ActiveChanged?.Invoke();
        }

        public void OnNetworkMessage(int fromPeer, BytePacker packer)
        {
            throw new System.NotImplementedException();
        }
    }


}
