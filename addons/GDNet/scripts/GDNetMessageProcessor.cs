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
		public byte[] Bytes;

		public MemoryStream Stream;
		public BinaryWriter Writer;

		public CommunicationBatch(long peer, MultiplayerPeer.TransferModeEnum mode, int channel)
		{
			Peer = peer;
			Mode = mode;
			Channel = channel;

			Stream = new MemoryStream();
			Writer = new BinaryWriter(Stream);
		}

		public void Dispose()
		{
			Stream?.Dispose();
			Writer?.Dispose();
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

		if (pending.Count == 0)
		{
			return;
		}

		

		ConcurrentQueue<CommunicationBatch> readyToSend = new();

		Parallel.For(0, pending.Count, (i) =>
		{
			PendingCommunication[] batch = pending[i];

			for (i = 0; i < batch.Length; i++)
			{
				PendingCommunication data = batch[i];
				CommunicationBatchKey key = new CommunicationBatchKey(data.Peer, data.Mode, data.Channel);

				if (!_batchProcess.TryGetValue(key, out CommunicationBatch communicationBatch))
				{
                    _batchProcess[key] = new CommunicationBatch(key.Peer, key.Mode, key.Channel);
				}

				communicationBatch.Writer.Write(data.NetworkID);
				communicationBatch.Writer.Write(data.Data.Length);
				communicationBatch.Writer.Write(data.Data);

				if (communicationBatch.Stream.Length >= MTU)
				{
					readyToSend.Enqueue(communicationBatch);
                    _batchProcess.TryRemove(key, out var deleted);
				}

			}

		});

		foreach (var pair in _batchProcess)
		{
			readyToSend.Enqueue(pair.Value);
		}

        _batchProcess.Clear();

		while (readyToSend.TryDequeue(out var batch))
		{
			GDNet.Instance.SendPacket(GDNet.PacketType.CommunicationMessage, batch.Stream.ToArray(), (int)batch.Peer, batch.Mode, batch.Channel);
			batch.Dispose();
		}
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
