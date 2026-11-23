using Godot;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Xml;
using ZCraft.Source.Serialization;

namespace ZCraft.Source.Networking
{
    public partial class NetworkManager : Node
    {
        public const int ServerId = 1;
        public const int MaxClients = 100;

        public static bool IsServer => _isServer;
        public static int UniqueId = _uniqueId;
        public static NetworkManager Instance => _instance;

        private static SceneMultiplayer _api;

        private static bool _isServer = true;
        private static int _uniqueId = ServerId;

        private static NetworkManager _instance;

        public NetworkTransport Transport { get; private set; }

        public static readonly List<int> Peers = new();

        public enum PacketHeader : byte
        {
            Default = 0,
            NetworkObjectMessage = 1,
        }

        public enum NetChannel : byte
        {
            Default = 0,
            Reliable,
            Unreliable,
            UnreliableOrdered,
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]    
        public static MultiplayerPeer.TransferModeEnum NetChannelToMultiplayerTransferMode(NetChannel channel)
        {
            switch(channel)
            {
                case NetChannel.Reliable:
                    return MultiplayerPeer.TransferModeEnum.Reliable;
                case NetChannel.Unreliable:
                    return MultiplayerPeer.TransferModeEnum.Unreliable;
                case NetChannel.UnreliableOrdered:
                    return MultiplayerPeer.TransferModeEnum.UnreliableOrdered;
            }

            return MultiplayerPeer.TransferModeEnum.Reliable;
        }

        public override void _EnterTree()
        {
            _instance = this;
            _api = new();
            _api.ServerRelay = false;
            GetTree().SetMultiplayer(_api);

            Transport = new NetworkTransport { Name = "NetworkTransport" };
            Transport.PacketReceivedSpan += InternalApiPeerPacket;
            AddChild(Transport);

            Multiplayer.ConnectedToServer += OnConnectedToServer;
            Multiplayer.ServerDisconnected += OnServerDisconnected;

            Multiplayer.PeerConnected += OnPeerConnected;
            Multiplayer.PeerDisconnected += OnPeerDisconnected;
        }

        private void OnPeerConnected(long id)
        {
            Peers.Add((int)id);
        }

        private void OnPeerDisconnected(long id)
        {
            Peers.Remove((int)id);
        }

        private void OnServerDisconnected()
        {
            Shutdown();
        }

        private void OnConnectedToServer()
        {
            Start(Multiplayer.GetUniqueId(), Multiplayer.IsServer());
        }

        public void Shutdown()
        {
            Transport?.Shutdown();
            _isServer = true;
            _uniqueId = ServerId;
            GD.Print("NetworkManager shutdown");
            //Engine.MaxFps = 0;
        }

        private void Start(int uniqueId, bool asServer)
        {
            _isServer = asServer;
            _uniqueId = uniqueId;
            Transport.Setup(asServer, _api);
            GD.Print($"NetworkManager started. asServer = {asServer}, uniqueId = {uniqueId}");
            
            // if (asServer)
            // {
            //     Engine.MaxFps = 48;
            // }
        }

        internal void QueuePacket(int peer, Span<byte> data, int channel, bool sendImmediate)
        {
            Transport.SendPacketToPeer(peer, data,
                NetChannelToMultiplayerTransferMode((NetChannel)channel), channel, sendImmediate);
        }

        private void InternalApiPeerPacket(int id, ReadOnlySpan<byte> packet)
        {
            BytePacker packer = BytePackerPool.Rent();

            try
            {
                packer.Wrap(packet);
                PacketHeader header = (PacketHeader)packer.ReadByte();

                switch (header)
                {
                    case PacketHeader.NetworkObjectMessage:
                        NetworkReplicator.OnObjectMessagePacket(id, packer);
                        break;
                }
            }

            catch(Exception e)
            {
                GD.Print($"Receiving Packet Error: {e}");
            }

            finally
            {
                BytePackerPool.Return(packer);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SendPacketToPeer(int peer, ReadOnlySpan<byte> data, MultiplayerPeer.TransferModeEnum mode, int channel)
        {
            Transport.SendPacketToPeer(peer, data, mode, channel);
        }

        public Error StartServer(int port)
        {
            ENetMultiplayerPeer peer = new();
            Error error = peer.CreateServer(port, MaxClients);
            
            if (error == Error.Ok)
            {
                Transport.Setup(true, _api);
                Multiplayer.MultiplayerPeer = peer;
                Start(Multiplayer.GetUniqueId(), Multiplayer.IsServer());
            }

            return error;
        }

        public Error StartClient(string address, int port)
        {
            ENetMultiplayerPeer peer = new();
            Error error = peer.CreateClient(address, port);
            if (error == Error.Ok)
            {
                Transport.Setup(false, _api);
                Multiplayer.MultiplayerPeer = peer;
            }
            return error;
        }

    }
}
