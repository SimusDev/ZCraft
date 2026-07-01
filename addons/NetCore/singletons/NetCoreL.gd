extends Node
#class_name OptimizedSend

enum BATCH_HEADER {
	UNBATCHED,
	BATCHED,
}

enum COMPRESS_HEADER {
	NONE,
	DEFLATE,
	ZSTD,
}

const COMPRESSION_DELFATE: int = FileAccess.COMPRESSION_DEFLATE
const COMPRESSION_ZSTD: int = FileAccess.COMPRESSION_ZSTD

const MAX_PACKET_SIZE: int = 1350

signal multiplayer_peer_packet(id: int, bytes: PackedByteArray)

var API: SceneMultiplayer

var _buffer_threaded: StreamPeerBuffer = StreamPeerBuffer.new()
var _buffer_threaded_receive: StreamPeerBuffer = StreamPeerBuffer.new()
var _buffer_threaded_immediate: StreamPeerBuffer = StreamPeerBuffer.new()

var _buffer_batch: NetCoreBuffer = NetCoreBuffer.new()
var _buffer_unbatch: NetCoreBuffer = NetCoreBuffer.new()

var _pending_immediate: Array[Dictionary] = []
var _pending_batches: Array[Dictionary] = []

var _processed_batches: Array[Dictionary] = []

var compression_enabled: bool = true
var compression_threshold_deflate: int = 256
var compression_threshold_zstd: int = 1024

var batching_enabled: bool = true
var batch_flush_tickrate: float = 60.0
var _batch_flush_time: float = 0.0

var _received_packets: Array[Dictionary] = []

var _mutex_immediate_packets: Mutex = Mutex.new()
var _mutex_batch_packets: Mutex = Mutex.new()
var _mutex_batch_task: Mutex = Mutex.new()

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
}


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
	
	_received_packets.append(packet)

func _on_api_peer_packet_worker_thread(id: int, bytes: PackedByteArray) -> void:
	var packet: Dictionary[PacketKey, Variant] = {}
	packet[PacketKey.Bytes] = bytes
	packet[PacketKey.Peer] = id
	
	_received_packets.append(packet)

func _create_packet(bytes: PackedByteArray, peer: int, 
mode: MultiplayerPeer.TransferMode,
channel: int
) -> Dictionary[PacketKey, Variant]:
	return {
		PacketKey.Bytes: bytes,
		PacketKey.Peer: peer,
		PacketKey.Mode: mode,
		PacketKey.Channel: channel,
	}

func _send_packet(packet: Dictionary[PacketKey, Variant]) -> void:
	var peer: int = packet[PacketKey.Peer]
	var bytes: PackedByteArray = packet[PacketKey.Bytes]
	
	if peer == multiplayer.get_unique_id():
		_on_api_peer_packet(peer, bytes)
		return
	
	API.send_bytes(
		bytes, 
		peer, 
		packet[PacketKey.Mode], 
		packet[PacketKey.Channel])

func _queue_immediate_packet(packet: Dictionary[PacketKey, Variant]) -> void:
	_pending_immediate.append(packet)

#var _processed_immediate_packets: 

func _process_immediate_packets() -> void:
	_mutex_immediate_packets.lock()
	
	var local_pending: Array[Dictionary] = _pending_immediate
	_pending_immediate = []
	
	var task: int = WorkerThreadPool.add_group_task(
		_send_immediate_threaded.bind(local_pending),
		local_pending.size(),
		true,
	)
	
	WorkerThreadPool.wait_for_group_task_completion(task)
	
	_mutex_immediate_packets.unlock()

func _send_immediate_threaded(index: int, pending: Array[Dictionary]) -> void:
	var packet: Dictionary[PacketKey, Variant] = pending[index]
	_send_immediate(packet)

func _send_immediate(packet: Dictionary[PacketKey, Variant]) -> void:
	var buffer: StreamPeerBuffer = _buffer_threaded_immediate
	buffer.clear()
	buffer.seek(0)
	
	buffer.put_u8(BATCH_HEADER.UNBATCHED)
	buffer.put_data(packet[PacketKey.Bytes])
	
	packet[PacketKey.Bytes] = _try_compress(buffer.data_array)
	_send_packet.call_deferred(packet)

func _process(delta: float) -> void:
	_process_immediate_packets()
	
	_batch_flush_time += delta
	if _batch_flush_time >= 1.0 / batch_flush_tickrate:
		_flush_batches()
		_batch_flush_time = 0
	
	for received_packet: Dictionary[PacketKey, Variant] in _received_packets:
		_process_received_packet(received_packet)
	
	_received_packets.clear()

func _process_received_packet(packet: Dictionary[PacketKey, Variant]) -> void:
	var peer: int = packet[PacketKey.Peer]
	var bytes: PackedByteArray = _try_decompress(packet[PacketKey.Bytes])
	
	_buffer_threaded_receive.data_array = bytes
	_buffer_threaded_receive.seek(0)
	
	var batch_header: BATCH_HEADER = _buffer_threaded_receive.get_u8()
	var data: PackedByteArray = _buffer_threaded_receive.get_data(_buffer_threaded_receive.get_available_bytes())[1]
	if batch_header == BATCH_HEADER.UNBATCHED:
		multiplayer_peer_packet.emit.call_deferred(peer, data)
		return
	
	var packets: Array[PackedByteArray] = _unbatch_packets(data)
	for i in packets:
		multiplayer_peer_packet.emit.call_deferred(peer, i)
	

func _batch_packets(data: Array[PackedByteArray]) -> PackedByteArray:
	_buffer_batch.clear()
	_buffer_batch.write_int(data.size())
	
	for packet: PackedByteArray in data:
		_buffer_batch.write_bytes(packet)
	
	return _buffer_batch.get_data()

func _unbatch_packets(data: PackedByteArray) -> Array[PackedByteArray]:
	_buffer_unbatch.set_data(data)
	_buffer_unbatch.seek(0)
	
	var result: Array[PackedByteArray] = []
	var size: int = _buffer_unbatch.read_int()
	
	for i in size:
		result.append(_buffer_unbatch.read_bytes())
	
	return result

var _processing_batches: Dictionary[String, Dictionary] = {}

func _flush_batch(data: Dictionary[BatchKey, Variant]) -> void:
	var buffer: StreamPeerBuffer = StreamPeerBuffer.new()
	var packets: Array[PackedByteArray] = data[BatchKey.BytesArray]
	var batched: PackedByteArray = _batch_packets(packets)
	
	buffer.clear()
	buffer.seek(0)
	buffer.put_u8(BATCH_HEADER.BATCHED)
	buffer.put_data(batched)
	
	var compressed: PackedByteArray = _try_compress(buffer.data_array)
	
	var api_packet: Dictionary[PacketKey, Variant] = _create_packet(
		compressed,
		data[BatchKey.Peer],
		data[BatchKey.Mode],
		data[BatchKey.Channel]
	)
	_send_packet.call_deferred(api_packet)


func _flush_batch_task(index: int, pending_batches: Array[Dictionary]) -> void:
	var packet: Dictionary[PacketKey, Variant] = pending_batches[index]
	var bytes: PackedByteArray = packet[PacketKey.Bytes]
	var peer: int = packet[PacketKey.Peer]
	var mode: MultiplayerPeer.TransferMode = packet[PacketKey.Mode]
	var channel: int = packet[PacketKey.Channel]
	
	var key: String = "%d_%d_%d" % [peer, mode, channel]
	
	_mutex_batch_task.lock()
	var batch_data: Dictionary[BatchKey, Variant] = _processing_batches.get_or_add(
		key,
		{} as Dictionary[BatchKey, Variant]
	)
	
	
	batch_data[BatchKey.Peer] = peer
	batch_data[BatchKey.Mode] = mode
	batch_data[BatchKey.Channel] = channel
	
	var bytes_array: Array[PackedByteArray] = batch_data.get_or_add(BatchKey.BytesArray, [] as Array[PackedByteArray])
	var total_bytes: int = batch_data.get_or_add(BatchKey.TotalBytes, 0)
	
	_mutex_batch_task.unlock()
	
	if total_bytes >= MAX_PACKET_SIZE:
		_flush_batch(batch_data)
		_pending_batches.erase(batch_data)
	
	bytes_array.append(bytes)
	total_bytes += bytes.size()
	
	batch_data.set(BatchKey.TotalBytes, total_bytes)

func _flush_batches() -> void:
	WorkerThreadPool.add_task(_flush_batches_threaded, true)

func _flush_batches_threaded() -> void:
	_mutex_batch_packets.lock()
	
	var pending_batches: Array[Dictionary] = _pending_batches
	_pending_batches = []
	
	_mutex_batch_packets.unlock()
	
	WorkerThreadPool.add_group_task(
		_flush_batch_task.bind(pending_batches),
		pending_batches.size(),
		-1,
		true
	)
	
	for processing in _processing_batches:
		_flush_batch(_processing_batches[processing])
	
	_processing_batches.clear()

func _queue_batch_packet(packet: Dictionary[PacketKey, Variant]) -> void:
	_mutex_batch_packets.lock()
	_pending_batches.append(packet)
	_mutex_batch_packets.unlock()

func _try_decompress(bytes: PackedByteArray) -> PackedByteArray:
	var buffer: StreamPeerBuffer = StreamPeerBuffer.new()
	buffer.data_array = bytes
	buffer.seek(0)
	
	var header: COMPRESS_HEADER = buffer.get_u8()
	var raw_size: int = 0
	if header == COMPRESS_HEADER.NONE:
		return bytes.slice(1)
	
	raw_size = buffer.get_u32()
	
	var compressed: PackedByteArray = buffer.get_data(buffer.get_available_bytes())[1]
	
	if header == COMPRESS_HEADER.DEFLATE:
		return compressed.decompress(raw_size, COMPRESSION_DELFATE)
	elif header == COMPRESS_HEADER.ZSTD:
		return compressed.decompress(raw_size, COMPRESSION_ZSTD)
	
	return compressed
	

func _try_compress(bytes: PackedByteArray) -> PackedByteArray:
	var buffer: StreamPeerBuffer = StreamPeerBuffer.new()
	var raw_size: int = bytes.size()
	buffer.clear()
	buffer.seek(0)
	
	if compression_enabled:
		if raw_size >= compression_threshold_zstd:
			buffer.put_u8(COMPRESS_HEADER.ZSTD)
			buffer.put_u32(raw_size)
			buffer.put_data(bytes.compress(COMPRESSION_ZSTD))
			return buffer.data_array
		
		if raw_size >= compression_threshold_deflate:
			buffer.put_u8(COMPRESS_HEADER.DEFLATE)
			buffer.put_u32(raw_size)
			buffer.put_data(bytes.compress(COMPRESSION_DELFATE))
			return buffer.data_array
	
	buffer.put_u8(COMPRESS_HEADER.NONE)
	buffer.put_data(bytes)
	return buffer.data_array

func multiplayer_send_bytes(bytes: PackedByteArray, peer: int = 0, 
mode: MultiplayerPeer.TransferMode = MultiplayerPeer.TransferMode.TRANSFER_MODE_RELIABLE, 
channel: int = 0, immediate: bool = false) -> void:
	var packet: Dictionary[PacketKey, Variant] = _create_packet(
			bytes,
			peer,
			mode,
			channel
		)
	
	#if bytes.size() >= MAX_PACKET_SIZE or !batching_enabled or immediate:
		#_queue_immediate_packet(packet)
		#return
	
	_queue_batch_packet(packet)
	
