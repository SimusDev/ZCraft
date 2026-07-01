extends Node
# V2

enum CompressionHeader {
	NONE,
	DEFLATE,
	ZSTD,
}

enum PacketKey {
	Bytes,
	Peer,
	Mode,
	Channel,
}

enum BatchKey {
	BytesArray,
	TotalBytes,
	Peer,
	Mode,
	Channel,
	ID,
}

enum ReceivedPacketKey {
	PacketArray,
	Peer,
}

var _packet_sequence_id: int = 0

const COMPRESSION_DELFATE: int = FileAccess.COMPRESSION_DEFLATE
const COMPRESSION_ZSTD: int = FileAccess.COMPRESSION_ZSTD

const MAX_PACKET_SIZE: int = 1350

var compression_enabled: bool = true
var compression_threshold_deflate: int = 256
var compression_threshold_zstd: int = 1024

var batch_flush_tickrate: float = 60.0
var _batch_flush_time: float = 0.0

signal multiplayer_peer_packet(id: int, bytes: PackedByteArray)

var API: SceneMultiplayer
var profiler: NetCoreProfiler = NetCoreProfiler.new()

#var _pending_packets_e: NetCoreElementBatcher = NetCoreElementBatcher.new()
var _pending_packets: Array[Dictionary] = []
var _pending_packets_mutex: Mutex = Mutex.new()

var _packet_batches: Dictionary[String, Dictionary] = {}
var _packet_batches_mutex: Mutex = Mutex.new()
var _packet_batch_collect_mutex: Mutex = Mutex.new()

var _processed_batches: Array[Dictionary] = []
var _processed_batches_mutex: Mutex = Mutex.new()

var _received_api_packets: Array[Dictionary] = []
var _received_api_packets_mutex: Mutex = Mutex.new()

func _ready() -> void:
	profiler._ready()

func _process(delta: float) -> void:
	_batch_flush_time += delta
	if _batch_flush_time >= 1.0 / batch_flush_tickrate:
		_flush_main_thread()
		_batch_flush_time = 0

func _flush_main_thread() -> void:
	if !_pending_packets.is_empty():
		var task: int = WorkerThreadPool.add_task(_collect_packets_into_batches_threaded, true)
		WorkerThreadPool.wait_for_task_completion(task)
	if !_received_api_packets.is_empty():
		var task: int = WorkerThreadPool.add_task(_process_all_received_packets_threaded, true)
		WorkerThreadPool.wait_for_task_completion(task)

func _collect_packets_into_batches_threaded() -> void:
	_pending_packets_mutex.lock()
	var pending: Array[Dictionary] = _pending_packets
	_pending_packets = []
	_pending_packets_mutex.unlock()
	
	var total: int = pending.size()
	
	if total == 0:
		return
	
	var task_batch: int = WorkerThreadPool.add_task(_collect_packets_into_batch.bind(pending),
	true)
	
	WorkerThreadPool.wait_for_task_completion(task_batch)
	
	var task_process: int = WorkerThreadPool.add_task(_process_packet_batches_threaded, true)
	WorkerThreadPool.wait_for_task_completion(task_process)
	
	_processed_batches_mutex.lock()
	var processed: Array[Dictionary] = _processed_batches
	_processed_batches = []
	_processed_batches_mutex.unlock()
	
	var single_batch_result: Array[Dictionary] = []
	single_batch_result.resize(processed.size())
	
	var task_single_batch: int = WorkerThreadPool.add_group_task(
		_process_single_batch.bind(processed, single_batch_result),
		processed.size(),
		-1,
		true
	)
	
	WorkerThreadPool.wait_for_group_task_completion(task_single_batch)
	
	for result: Dictionary[PacketKey, Variant] in single_batch_result:
		_send_bytes_main_thread.call_deferred(
			result[PacketKey.Bytes],
			result[PacketKey.Peer],
			result[PacketKey.Mode],
			result[PacketKey.Channel]
		)

func _send_bytes_main_thread(bytes: PackedByteArray, peer: int, mode: MultiplayerPeer.TransferMode, channel: int) -> void:
	if multiplayer.get_unique_id() == peer:
		_on_api_peer_packet(peer, bytes)
		return
	
	API.send_bytes(bytes, peer, mode, channel)

func _process_single_batch(index: int, processed: Array[Dictionary], result: Array[Dictionary]) -> void:
	var batch: Dictionary[BatchKey, Variant] = processed[index]
	profiler._test_add.call_deferred(batch[BatchKey.BytesArray].size())
	var bytes: PackedByteArray = _batch_packets(batch[BatchKey.BytesArray])
	bytes = _try_compress(bytes)
	
	var batch_result: Dictionary[PacketKey, Variant] = {
		PacketKey.Peer: batch[BatchKey.Peer],
		PacketKey.Mode: batch[BatchKey.Mode],
		PacketKey.Channel: batch[BatchKey.Channel],
		PacketKey.Bytes: bytes
	}
	
	result[index] = batch_result

func _process_packet_batches_threaded() -> void:
	_packet_batches_mutex.lock()
	
	if _packet_batches.is_empty():
		_packet_batches_mutex.unlock()
		return
	
	for key: String in _packet_batches:
		var batch: Dictionary[BatchKey, Variant] = _packet_batches[key]
		_packet_batches.erase(key)
		_processed_batches_mutex.lock()
		_processed_batches.append(batch)
		_processed_batches_mutex.unlock()
	
	_processed_batches_mutex.lock()
	
	var idx: int = 0
	for processed: Dictionary[BatchKey, Variant] in _processed_batches:
		processed[BatchKey.ID] = idx
		idx += 1
	
	_processed_batches_mutex.unlock()
	
	_packet_batches_mutex.unlock()

func _collect_packets_into_batch(packets: Array[Dictionary]) -> void:
	for packet: Dictionary[PacketKey, Variant] in packets:
		var bytes: PackedByteArray = packet[PacketKey.Bytes]
		var peer: int = packet[PacketKey.Peer]
		var mode: MultiplayerPeer.TransferMode = packet[PacketKey.Mode]
		var channel: int = packet[PacketKey.Channel]
		
		var key: String = "%d_%d_%d" % [peer, mode, channel]
		
		if _packet_batches_mutex:
			_packet_batches_mutex.lock()
		
		var data: Dictionary[BatchKey, Variant] = _packet_batches.get_or_add(
			key, {} as Dictionary[BatchKey, Variant]
		)
		
		var raw_packets: Array[PackedByteArray] = data.get_or_add(
			BatchKey.BytesArray, [] as Array[PackedByteArray]
		)
		
		var total_bytes: int = data.get_or_add(
			BatchKey.TotalBytes, 0
		)
		

		
		data[BatchKey.Peer] = peer
		data[BatchKey.Mode] = mode
		data[BatchKey.Channel] = channel
		
		raw_packets.append(bytes)
		
		data[BatchKey.TotalBytes] += bytes.size()
		
		if total_bytes >= MAX_PACKET_SIZE:
			_processed_batches_mutex.lock()
			_processed_batches.append(data)
			_processed_batches_mutex.unlock()
			
			_packet_batches.erase(key)
			
			_packet_batches_mutex.unlock()
			
			continue
		
		_packet_batches_mutex.unlock()

func _batch_packets(data: Array[PackedByteArray]) -> PackedByteArray:
	var buffer: NetCoreBuffer = NetCoreBuffer.new()
	buffer.write_int(data.size())
	
	for packet: PackedByteArray in data:
		buffer.write_bytes(packet)
	
	return buffer.get_data()

func _unbatch_packets(data: PackedByteArray) -> Array[PackedByteArray]:
	var buffer: NetCoreBuffer = NetCoreBuffer.new()
	buffer.set_data(data)
	
	var result: Array[PackedByteArray] = []
	var size: int = buffer.read_int()
	
	for i in size:
		result.append(buffer.read_bytes())
	
	return result

func _try_decompress(bytes: PackedByteArray) -> PackedByteArray:
	var buffer: StreamPeerBuffer = StreamPeerBuffer.new()
	buffer.data_array = bytes
	
	var header: CompressionHeader = buffer.get_u8()
	var raw_size: int = 0
	if header == CompressionHeader.NONE:
		return bytes.slice(1)
	
	raw_size = buffer.get_u32()
	
	var compressed: PackedByteArray = buffer.get_data(buffer.get_available_bytes())[1]
	
	if header == CompressionHeader.DEFLATE:
		return compressed.decompress(raw_size, COMPRESSION_DELFATE)
	elif header == CompressionHeader.ZSTD:
		return compressed.decompress(raw_size, COMPRESSION_ZSTD)
	
	return compressed
	

func _try_compress(bytes: PackedByteArray) -> PackedByteArray:
	var buffer: StreamPeerBuffer = StreamPeerBuffer.new()
	var raw_size: int = bytes.size()
	
	if compression_enabled:
		if raw_size >= compression_threshold_zstd:
			buffer.put_u8(CompressionHeader.ZSTD)
			buffer.put_u32(raw_size)
			buffer.put_data(bytes.compress(COMPRESSION_ZSTD))
			return buffer.data_array
		
		if raw_size >= compression_threshold_deflate:
			buffer.put_u8(CompressionHeader.DEFLATE)
			buffer.put_u32(raw_size)
			buffer.put_data(bytes.compress(COMPRESSION_DELFATE))
			return buffer.data_array
	
	buffer.put_u8(CompressionHeader.NONE)
	buffer.put_data(bytes)
	return buffer.data_array


func multiplayer_send_bytes(bytes: PackedByteArray, peer: int = 0, 
mode: MultiplayerPeer.TransferMode = MultiplayerPeer.TransferMode.TRANSFER_MODE_RELIABLE, 
channel: int = 0) -> void:
	var packet: Dictionary[PacketKey, Variant] = {
		PacketKey.Bytes: bytes,
		PacketKey.Peer: peer,
		PacketKey.Mode: mode,
		PacketKey.Channel: channel,
	}
	
	if _packet_sequence_id >= 4294967295:
		_packet_sequence_id = 0
	_packet_sequence_id += 1
	
	_pending_packets_mutex.lock()
	
	_pending_packets.append(packet)
	_pending_packets_mutex.unlock()

func setup(api: SceneMultiplayer = null) -> void:
	if !is_instance_valid(api):
		api = SceneMultiplayer.new()
	
	API = api
	
	get_tree().set_multiplayer(API)
	API.peer_packet.connect(_on_api_peer_packet)

func _on_api_peer_packet(id: int, bytes: PackedByteArray) -> void:
	var packet: Dictionary[PacketKey, Variant] = {}
	packet[PacketKey.Bytes] = bytes
	packet[PacketKey.Peer] = id
	
	_received_api_packets_mutex.lock()
	_received_api_packets.append(packet)
	_received_api_packets_mutex.unlock()

func _process_all_received_packets_threaded() -> void:
	_received_api_packets_mutex.lock()
	var packets: Array[Dictionary] = _received_api_packets
	_received_api_packets = []
	_received_api_packets_mutex.unlock()
	
	if packets.is_empty():
		return
	
	var result: Array[Dictionary] = []
	result.resize(packets.size())
	
	var task: int = WorkerThreadPool.add_group_task(
		_process_received_packet_task.bind(packets, result),
		packets.size(),
		-1,
		true
	)
	
	WorkerThreadPool.wait_for_group_task_completion(task)
	
	for received: Dictionary[ReceivedPacketKey, Variant] in result:
		var raw: Array[PackedByteArray] = received[ReceivedPacketKey.PacketArray]
		var pid: int = received[ReceivedPacketKey.Peer]
		for raw_packet: PackedByteArray in raw:
			multiplayer_peer_packet.emit.call_deferred(pid, raw_packet)
	

func _process_received_packet_task(index: int, packets: Array[Dictionary], result: Array[Dictionary]) -> void:
	var packet: Dictionary[PacketKey, Variant] = packets[index]
	var bytes: PackedByteArray = packet[PacketKey.Bytes]
	var peer: int = packet[PacketKey.Peer]
	
	bytes = _try_decompress(bytes)
	var raw: Array[PackedByteArray] = _unbatch_packets(bytes)
	
	var received_result: Dictionary[ReceivedPacketKey, Variant] = {
		ReceivedPacketKey.PacketArray: raw,
		ReceivedPacketKey.Peer: peer,
	}
	
	result[index] = received_result
	
	
	
