
using Godot;
using Godot.Collections;
using System;

namespace Connection
{
    public partial class User : RefCounted
    {
        private GDNetRpc _rpc = new();

        private double _time = 0;

        private long _netID = 0;

        private static GDNetBuffer _buffer = new GDNetBuffer();

        [Export] private int _peerId = 0;
        [Export] private Dictionary<string, Variant> _synchronizedData = new();

        public void SetSyncValue(string key, Variant value)
        {
            if (GDNet.isServer)
            {
                SetSyncValueRpc(key, value);
                _rpc.Invoke(SetSyncValueRpc, key, value);
            }
        }

        [GDNetRpc(Channel = (int)GameServer.Channel.Users)]
        private void SetSyncValueRpc(string key, Variant value)
        {
            _synchronizedData[key] = value;
        }
        public bool TryGetSyncValue(string key, out Variant value)
        {
            return _synchronizedData.TryGetValue(key, out value);
        }

        public User()
        {
            _netID = GDNet.GenerateUniqueID();
            _rpc.SynchronizeNetworkIDByUniqueID(_netID);
            _rpc.BindAll(this);
        }

        public User(long netId)
        {
            _netID = netId;
            _rpc.SynchronizeNetworkIDByUniqueID(_netID);
            _rpc.BindAll(this);
        }

        public User Deserialize(byte[] data)
        {
            _buffer.Clear();
            _buffer.SetBytes(data);
            
            var result = new User(_buffer.ReadLongVar());
            result._peerId = _buffer.ReadIntVar();
            result._synchronizedData = (Dictionary<string, Variant>)_buffer.ReadVarToBytes();

            return result;
        }

        public byte[] Serialize()
        {
            _buffer.Clear();
            _buffer.WriteLongVar(_netID);
            _buffer.WriteIntVar(_peerId);
            _buffer.WriteVarToBytes(_synchronizedData);
            return _buffer.GetBytes();
        }
    }

}
