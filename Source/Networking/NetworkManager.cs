using Godot;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Xml;

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

        private static Dictionary<int, Serialization.BufferStream> _peerBuffers = new();

        public override void _EnterTree()
        {
            _instance = this;
            _api = new();
            _api.ServerRelay = false;
            GetTree().SetMultiplayer(_api);

            _api.PeerPacket += InternalApiPeerPacket;

            Multiplayer.ConnectedToServer += OnConnectedToServer;
            Multiplayer.ServerDisconnected += OnServerDisconnected;

            Multiplayer.PeerConnected += OnPeerConnected;
            Multiplayer.PeerDisconnected += OnPeerDisconnected;
        }

        private void OnPeerConnected(long id)
        {
            _peerBuffers[(int)id] = new Serialization.BufferStream(2048);
        }

        private void OnPeerDisconnected(long id)
        {
            _peerBuffers.Remove((int)id);
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
            _isServer = true;
            _uniqueId = ServerId;
            GD.Print("NetworkManager shutdown");
        }

        private void Start(int uniqueId, bool asServer)
        {
            _isServer = asServer;
            _uniqueId = uniqueId;
            GD.Print($"NetworkManager started. asServer = {asServer}, uniqueId = {uniqueId}");
        }

        private void InternalApiPeerPacket(long id, byte[] packet)
        {
            
        }

        internal void QueuePacket(int peer, Span<byte> data, int channel, bool sendImmediate)
        {

        }

        public Error StartServer(int port)
        {
            ENetMultiplayerPeer peer = new();
            Error error = peer.CreateServer(port, MaxClients);
            if (error == Error.Ok)
            {
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
                Multiplayer.MultiplayerPeer = peer;
            }
            return error;
        }


    }
}
