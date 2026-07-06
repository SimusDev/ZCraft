using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

[GlobalClass]
public partial class GDNetOptimizedSend : Node
{
	private SceneMultiplayer _api;

	private Mutex _pendingPacketMutex = new();
	private ConcurrentQueue<QueuedPacket> _pendingPacketsQueue = new();

	private ConcurrentDictionary<BatchKey, BatchData> _pendingBatches = new();

	private ConcurrentQueue<BatchData> _pendingBatchesToCompress = new();

	private GDNetBuffer _mainThreadBuffer = new();

	public const int MTU = 1350;

	[Signal] public delegate void MultiplayerPeerPacketEventHandler(long id, byte[] bytes);

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
		byte[] decompressed = TryDecompressBytes(packet);
		GD.Print(decompressed);
	}

	public void ProcessAll()
	{
		CollectAndBatchPendingPackets();
		CompressPendingBatches();
	}

	private void CollectAndBatchPendingPackets()
	{
		// 1. Забираем все пакеты из очереди
		var packets = new List<QueuedPacket>();
		while (_pendingPacketsQueue.TryDequeue(out var packet))
		{
			packets.Add(packet);
		}


		if (packets.Count == 0) return;

		// 2. Разбиваем на чанки (по 200 пакетов)
		const int chunkSize = 200;
		var chunks = new List<List<QueuedPacket>>();
		for (int i = 0; i < packets.Count; i += chunkSize)
		{
			chunks.Add(packets.GetRange(i, Math.Min(chunkSize, packets.Count - i)));
		}

		// 3. Параллельная обработка чанков
		var results = new ConcurrentBag<KeyValuePair<BatchKey, BatchData>>();

		Parallel.ForEach(chunks, (chunk) =>
		{
			// Локальный словарь для этого чанка
			var localBatches = new Dictionary<BatchKey, BatchData>();

			foreach (var packet in chunk)
			{
				var key = new BatchKey(packet.TargetPeer, packet.Mode, packet.Channel);

				if (!localBatches.TryGetValue(key, out var data))
				{
					data = new BatchData(key.TargetPeer, key.Mode, key.Channel);
					localBatches[key] = data;
				}

				data.Buffer.WriteBytes(packet.Data);

				// Если батч переполнен — отправляем в очередь на сжатие
				if (data.Buffer.Size >= MTU)
				{
					// Сохраняем результат
					results.Add(new KeyValuePair<BatchKey, BatchData>(key, data));
					// Создаём новый буфер для этого ключа
					localBatches[key] = new BatchData(key.TargetPeer, key.Mode, key.Channel);
				}
			}

			// Сохраняем оставшиеся батчи
			foreach (var kvp in localBatches)
			{
				if (kvp.Value.Buffer.Size > 0)
				{
					results.Add(kvp);
				}
			}
		});

		
		foreach (var result in results)
		{
			_pendingBatchesToCompress.Enqueue(result.Value);
		}

	}

	private void CompressPendingBatches()
	{
		// 1. Забираем все батчи из очереди
		var datas = new List<BatchData>();
		while (_pendingBatchesToCompress.TryDequeue(out var data))
		{
			datas.Add(data);
		}

		if (datas.Count == 0) return;

		// 2. Создаём массив для хранения сжатых данных с индексами
		var compressedData = new (int Index, BatchData Data)[datas.Count];

		// 3. Параллельно сжимаем, сохраняя индекс
		Parallel.For(0, datas.Count, (i) =>
		{
			var pData = datas[i];
			var compressed = TryCompressBytes(pData.Buffer.GetBytes());
			pData.Buffer.SetBytes(compressed);
			compressedData[i] = (i, pData);
		});

		// 4. Восстанавливаем порядок по индексу и отправляем
		for (int i = 0; i < compressedData.Length; i++)
		{
			var data = compressedData[i].Data;
			// Отправляем данные в правильном порядке
			SendBytesInternal(data.Buffer.GetBytes(), (int)data.TargetPeer, data.Mode, data.Channel);
		}
	}

	private byte[] TryCompressBytes(byte[] bytes)
	{
		int length = bytes.Length;

		if (length < CompressionThresholdDeflate)
		{
			return bytes;
		}

		using var stream = new MemoryStream();
		using var writer = new BinaryWriter(stream);

		if (length >= CompressionThresholdZstd)
		{
			writer.Write((byte)CompressHeader.Zstd);
			writer.Write(length);
			var compressed = bytes.Compress(Godot.FileAccess.CompressionMode.Zstd);
			writer.Write(compressed);
		}
		else if (length >= CompressionThresholdDeflate)
		{
			writer.Write((byte)CompressHeader.Deflate);
			writer.Write(length);
			var compressed = bytes.Compress(Godot.FileAccess.CompressionMode.Deflate);
			writer.Write(compressed);
		}

		return stream.ToArray();
	}

	private byte[] TryDecompressBytes(byte[] bytes)
	{
		using var stream = new MemoryStream();
		using var reader = new BinaryReader(stream);

		var header = (CompressHeader)reader.ReadByte();

		if (header == CompressHeader.None)
		{
			return bytes.Skip(1).ToArray();
		}

		int originalSize = reader.ReadInt32();

		int compressedSize = bytes.Length - 1 - 4;
		byte[] compressedData = reader.ReadBytes(compressedSize);

		switch (header)
		{
			case CompressHeader.Deflate:
				return compressedData.Decompress(originalSize, Godot.FileAccess.CompressionMode.Deflate);
			case CompressHeader.Zstd:
				return compressedData.Decompress(originalSize, Godot.FileAccess.CompressionMode.Zstd);
			default:
				return compressedData;
		}
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

	private struct BatchData
	{
		public BatchData(long targetPeer, MultiplayerPeer.TransferModeEnum mode, int channel)
		{
			TargetPeer = targetPeer;
			Mode = mode;
			Channel = channel;
			Buffer = new();
		}

		public long TargetPeer;
		public MultiplayerPeer.TransferModeEnum Mode;
		public int Channel;
		public GDNetBuffer Buffer;
	}

	public void MultiplayerSendBytes(byte[] data, int id, MultiplayerPeer.TransferModeEnum mode, int channel)
	{
		_pendingPacketsQueue.Enqueue(
			new QueuedPacket(
				data,
				id,
				mode,
				channel
				)
			);
	}

	private void SendBytesInternal(byte[] data, int id, MultiplayerPeer.TransferModeEnum mode, int channel)
	{
		if (_api.GetUniqueId() == id)
		{
			OnApiPeerPacket(id, data);
			return;
		}

		_api.SendBytes(data, id, mode, channel);
	}
}
