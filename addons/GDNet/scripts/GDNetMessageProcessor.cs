using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

[GlobalClass]
public partial class GDNetMessageProcessor : Node
{
	public static GDNetMessageProcessor Instance;

	private GDNetUtils.ChunkedList<PendingCommunication> _pendingCommunicator = new(512);

	private ConcurrentDictionary<CommunicationBatchKey, CommunicationBatch> _batchProcess = new();

	private Mutex _mutex = new();

	const int MTU = 1400;

	public enum CommunicationType : byte
	{

	}

	private struct PendingCommunication
	{
		public ulong NetworkID;
		public long Peer;
		public MultiplayerPeer.TransferModeEnum Mode;
		public int Channel;
		public byte[] Data;

		public PendingCommunication(ulong networkId, long peer, MultiplayerPeer.TransferModeEnum mode, int channel, byte[] data)
		{
			NetworkID = networkId;
			Peer = peer;
			Mode = mode;
			Channel = channel;
			Data = data;
		}
	}

	private struct CommunicationBatch : IDisposable
	{
		public long Peer;
		public MultiplayerPeer.TransferModeEnum Mode;
		public int Channel;

		public GDNetBuffer Buffer;

		public CommunicationBatch(long peer, MultiplayerPeer.TransferModeEnum mode, int channel)
		{
			Peer = peer;
			Mode = mode;
			Channel = channel;
			Buffer = new GDNetBuffer();
		}

		public void Dispose()
		{

		}
	}

	private struct CommunicationBatchKey
	{
		public readonly long Peer;
		public readonly MultiplayerPeer.TransferModeEnum Mode;
		public readonly int Channel;

		public CommunicationBatchKey(long peer, MultiplayerPeer.TransferModeEnum mode, int channel)
		{
			Peer = peer;
			Mode = mode;
			Channel = channel;
		}
	}

	public override void _EnterTree()
	{
		Instance = this;
	}

	public void ProcessAll()
	{
		ProcessCommunication();
	}

    private void ProcessCommunication()
    {
        List<PendingCommunication[]> pending = _pendingCommunicator.TakeOwnership();
        if (pending.Count == 0) return;

        // Массив результатов: каждый чанк -> список батчей
        var results = new List<CommunicationBatch>[pending.Count];
        for (int i = 0; i < pending.Count; i++)
        {
            results[i] = new List<CommunicationBatch>();
        }

        Parallel.For(0, pending.Count, (chunkIndex) =>
        {
            PendingCommunication[] chunk = pending[chunkIndex];
            var localBatches = new Dictionary<CommunicationBatchKey, CommunicationBatch>();

            for (int j = 0; j < chunk.Length; j++)
            {
				PendingCommunication data = chunk[j];

                var key = new CommunicationBatchKey(data.Peer, data.Mode, data.Channel);

                if (!localBatches.TryGetValue(key, out var batch))
                {
                    batch = new CommunicationBatch(key.Peer, key.Mode, key.Channel);
                    localBatches[key] = batch;
                }

                batch.Buffer.WriteInt((long)data.NetworkID);
                batch.Buffer.WriteBytes(data.Data);

                if (batch.Buffer.Size >= MTU)
                {
                    lock (results)
                    {
                        results[chunkIndex].Add(batch);
                    }

                    localBatches[key] = new CommunicationBatch(key.Peer, key.Mode, key.Channel);
                }
            }

            foreach (var kvp in localBatches)
            {
                lock (results)
                {
                    results[chunkIndex].Add(kvp.Value);
                }
            }
        });

        // Отправляем строго по порядку чанков
        for (int i = 0; i < results.Length; i++)
        {
            foreach (var batch in results[i])
            {
                GDNet.Instance.SendPacket(GDNet.PacketType.CommunicationMessage, batch.Buffer.GetBytes(), (int)batch.Peer, batch.Mode, batch.Channel);
                batch.Dispose();
            }
        }

        _batchProcess.Clear();
    }

    public void ___QueueCommunicator(ulong networkID, GDNetAoI AoI, byte[] data, MultiplayerPeer.TransferModeEnum mode, int channel)
	{
		if (GDNet.Instance.IsServer)
		{
			if (AoI.IsPublicVisible())
			{
				var peers = Multiplayer.GetPeers();
				for (int i = 0; i < peers.Length; i++)
				{
					_pendingCommunicator.Add(
						new PendingCommunication(networkID, peers[i], mode, channel, data)
						);
				}
			}

			else
			{
				foreach(int peer in AoI.Peers)
				{
					_pendingCommunicator.Add(
						new PendingCommunication(networkID, peer, mode, channel, data)
						);
				}
			}

			return;
		}

		_pendingCommunicator.Add(
			new PendingCommunication(networkID, GDNet.ServerID, mode, channel, data)
			);

	}

}
