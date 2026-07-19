
using Godot;
using System;

namespace Connection
{
    public partial class User : RefCounted
    {
        private GDNetRpc _rpc;

        private double _time = 0;

        private int _authority = 1;
        private long _netID = 0;

        public void Init()
        {
            _rpc = new GDNetRpc();
            _rpc.SynchronizeNetworkIDByUniqueID(_netID);
            _rpc.Authority = _authority;

            _rpc.BindAll(this);
        }

        public void PhysicsTick(double delta)
        {
            _time += delta;
            if (_time >= 1)
            {
                Synchronize();
                _time = 0;
            }
        }

        private void Synchronize()
        {
            _rpc.InvokeOnServer(_ServerReceiveData, Time.GetTicksMsec());
        }

        [GDNetRpc(permission: Permission.ServerOrAuth, Channel = 0, Mode = Mode.Reliable)]
        private void _ServerReceiveData(ulong time)
        {

        }


    }

}
