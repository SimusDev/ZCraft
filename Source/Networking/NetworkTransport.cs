using Godot;
using System;
using System.Runtime.CompilerServices;

namespace ZCraft.Source.Networking
{
    public partial class NetworkTransport : Node
    {
        [Export] public bool Debug = true;
        [Export] public double TickRate = 48;

        private double _tickTime = 0;

        public delegate void OnRawPacketReceivedHandler(NetworkStream stream);
        public event OnRawPacketReceivedHandler RawPacketReceivedEvent;

        private NetworkStream _streamReliable = new(4096);
        private NetworkStream _streamUnreliable = new(4096);
        private NetworkStream _streamUnreliableOrdered = new(4096);

        private NetworkStream _streamReceivePackets = new();

        public const int MTU = 1350;

        public enum SendMode : byte
        {
            Reliable = 0,
            Unreliable = 1,
            UnreliableOrdered = 2,
        }

        public override void _Process(double delta)
        {
            _tickTime += delta;
            if (_tickTime >= 1.0 / TickRate)
            {
                FlushPackets();
                _tickTime = 0;
            }
        }

        public override void _PhysicsProcess(double delta)
        {
            if (Multiplayer.IsServer())
            {
                var test = new Godot.Collections.Dictionary();
                byte[] b = GD.VarToBytes(test);
                QueuePacket(b, SendMode.Unreliable);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void QueuePacket(Span<byte> data, SendMode sendMode)
        {
            switch (sendMode)
            {
                case SendMode.Reliable:
                    _streamReliable.WriteVarInt(data.Length);
                    _streamReliable.WriteBytes(data);
                    break;
                case SendMode.Unreliable:
                    _streamUnreliable.WriteVarInt(data.Length);
                    _streamUnreliable.WriteBytes(data);
                    break;
                case SendMode.UnreliableOrdered:
                    _streamUnreliableOrdered.WriteVarInt(data.Length);
                    _streamUnreliableOrdered.WriteBytes(data);
                    break;
            }

            TryFlushIfReachesMTU();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TryFlushIfReachesMTU()
        {
            if (_streamReliable.Length >= MTU)
                Flush(_streamReliable, MethodName.ReceivePacketReliable);

            if (_streamUnreliable.Length >= MTU)
                Flush(_streamUnreliable, MethodName.ReceivePacketUnreliable);

            if (_streamUnreliableOrdered.Length >= MTU)
                Flush(_streamUnreliableOrdered, MethodName.ReceivePacketUnreliableOrdered);
        }

        private void Flush(NetworkStream stream, Godot.StringName rpcMethodName)
        {
            if (stream.Length < 1)
                return;

            Span<byte> buffer = stream.AsSpan();

            NetworkProfiler.PutOutcomingPacket(buffer.Length);
            Rpc(rpcMethodName, buffer);

            stream.Reset();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FlushPackets()
        {
            Flush(_streamReliable, MethodName.ReceivePacketReliable);
            Flush(_streamUnreliable, MethodName.ReceivePacketUnreliable);
            Flush(_streamUnreliableOrdered, MethodName.ReceivePacketUnreliableOrdered);
        }

        [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable, TransferChannel = 1)]
        private void ReceivePacketReliable(byte[] data)
        {
            ProcessReceivedPacket(data);
        }

        [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable, TransferChannel = 2)]
        private void ReceivePacketUnreliable(byte[] data)
        {
            ProcessReceivedPacket(data);
        }

        [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered, TransferChannel = 3)]
        private void ReceivePacketUnreliableOrdered(byte[] data)
        {
            ProcessReceivedPacket(data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessReceivedPacket(byte[] data)
        {
            NetworkProfiler.PutIncomingPacket(data.Length);

            _streamReceivePackets.SetBuffer(data);
            _streamReceivePackets.Seek(0);

            while (_streamReceivePackets.Remaining > 0)
            {
                int bytesSize = _streamReceivePackets.ReadVarInt();
                RawPacketReceivedEvent?.Invoke(_streamReceivePackets);
            }
        }
    }
}
