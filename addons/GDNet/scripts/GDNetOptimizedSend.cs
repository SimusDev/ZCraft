using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.IO.Compression;
using System.IO;

[GlobalClass]
public partial class GDNetOptimizedSend : Node
{
	private SceneMultiplayer _api;

	private Mutex _pendingPacketMutex = new();
	private List<QueuedPacket> _pendingPacketsQueue = new(512000);

	private ConcurrentDictionary<BatchKey, GDNetBuffer> _pendingBatches = new();

	private ConcurrentQueue<GDNetBuffer> _pendingBatchesToCompress = new();

	private GDNetBuffer _mainThreadBuffer = new();

	public const int MTU = 1350;

	public delegate void MultiplayerPeerPacket(long id, byte[] bytes);

	const int CompressionThresholdDeflate = 256;
	const int CompressionThresholdZstd = 1024;

	enum CompressHeader: byte
	{
		None,
		Deflate,
		Zstd,
	}

	public void Setup(SceneMultiplayer api)
	{
		_api = api;
		_api.PeerPacket += OnApiPeerPacket;
	}

	private void OnApiPeerPacket(long id, byte[] packet)
	{
		throw new NotImplementedException();
	}

	public void ProcessAll()
	{
		CollectAndBatchPendingPackets();
		CompressPendingBatches();
	}

	private void CollectAndBatchPendingPackets()
	{
		for (int i = 0; i < _pendingPacketsQueue.Count; i++)
		{
			QueuedPacket pendingPacket = _pendingPacketsQueue[i];

			BatchKey key = new BatchKey(pendingPacket.TargetPeer, pendingPacket.Mode, pendingPacket.Channel);
			GDNetBuffer data = _pendingBatches.GetOrAdd(key, new GDNetBuffer());

			data.WriteBytes(pendingPacket.Data);

			if (data.Size >= MTU)
			{
				_pendingBatchesToCompress.Enqueue(data);
				_pendingBatches.Remove(key, out var value);
			}

		}

		_pendingPacketsQueue.Clear();


	}

	private void CompressPendingBatches()
	{
		List<GDNetBuffer> buffers = new();

		while (_pendingBatchesToCompress.TryDequeue(out var buffer))
		{

		}

		if (buffers.Count == 0)
			return;


	}

	private byte[] TryCompressBytes(byte[] bytes)
	{
		using var stream = new MemoryStream();
		if (bytes.Length >= CompressionThresholdZstd)
		{
			return stream.ToArray();
		}

		return stream.ToArray();
	}

	private byte[] TryDecompressBytes(byte[] bytes)
	{
		return bytes;
	}

	private struct QueuedPacket
	{
		public QueuedPacket(byte[] data, int targetPeer, MultiplayerPeer.TransferModeEnum mode, int channel)
		{
			Data = data;
			TargetPeer = targetPeer;
			Mode = mode;
			Channel = channel;
		}

		public byte[] Data;
		public int TargetPeer;
		public MultiplayerPeer.TransferModeEnum Mode;
		public int Channel;
	}

	private struct ReceivedPacket
	{
		public long SenderId;
		public byte[] Data;
	}
	private struct BatchKey
	{
		public BatchKey(long targetPeer, MultiplayerPeer.TransferModeEnum mode, int channel)
		{
			TargetPeer = targetPeer;
			Mode = mode;
			Channel = channel;
		}

		public long TargetPeer;
		public MultiplayerPeer.TransferModeEnum Mode;
		public int Channel;
	}

	public void MultiplayerSendBytes(byte[] data, int id, MultiplayerPeer.TransferModeEnum mode, int channel)
	{
		_pendingPacketsQueue.Add(
			new QueuedPacket(
				data,
				id,
				mode,
				channel
				)
			);
	}
}
