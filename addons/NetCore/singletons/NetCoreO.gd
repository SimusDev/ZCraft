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

var _pending_batches: Array[Dictionary] = []
var _pending_batches_mutex: Mutex = Mutex.new()

var _processing_batches: Dictionary[String, Dictionary] = {}
var _processed_batches: Array[Dictionary] = []
var _processing_batches_mutex: Mutex = Mutex.new()

var compression_enabled: bool = true
var compression_threshold_deflate: int = 256
var compression_threshold_zstd: int = 1024

var batching_enabled: bool = true
var batch_flush_tickrate: float = 60.0
var _batch_flush_time: float = 0.0

var _is_batch_flush_processing: bool = false

var _received_packets: Array[Dictionary] = []

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

func _process(delta: float) -> void:
	
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
	
	_processing_batches_mutex.lock()
	var batch_data: Dictionary[BatchKey, Variant] = _processing_batches.get_or_add(
		key,
		{} as Dictionary[BatchKey, Variant]
	)
	
	batch_data[BatchKey.Peer] = peer
	batch_data[BatchKey.Mode] = mode
	batch_data[BatchKey.Channel] = channel
	
	var bytes_array: Array[PackedByteArray] = batch_data.get_or_add(BatchKey.BytesArray, [] as Array[PackedByteArray])
	var total_bytes: int = batch_data.get_or_add(BatchKey.TotalBytes, 0)
	
	_processing_batches_mutex.unlock()
	
	if total_bytes >= MAX_PACKET_SIZE:
		_processing_batches_mutex.lock()
		_flush_batch(batch_data)
		_processing_batches.erase(key)
		_processing_batches_mutex.unlock()
		_pending_batches_mutex.lock()
		_pending_batches.erase(batch_data)
		_pending_batches_mutex.unlock()
		return
	
	_processing_batches_mutex.lock()
	bytes_array.append(bytes)
	batch_data[BatchKey.TotalBytes] += bytes.size()
	_processing_batches_mutex.unlock()

func _flush_batches() -> void:
	WorkerThreadPool.add_task(_flush_batches_threaded, true)

func _flush_batches_threaded() -> void:
	_pending_batches_mutex.lock()
	var pending_batches: Array[Dictionary] = _pending_batches
	_pending_batches = []
	_pending_batches_mutex.unlock()
	
	var task: int = WorkerThreadPool.add_group_task(
		_flush_batch_task.bind(pending_batches),
		pending_batches.size(),
		-1,
		true
	)
	
	_processing_batches_mutex.lock()
	for processing in _processing_batches:
		_flush_batch(_processing_batches[processing])
	
	_processing_batches.clear()
	_processing_batches_mutex.unlock()

func _queue_batch_packet(packet: Dictionary[PacketKey, Variant]) -> void:
	_pending_batches_mutex.lock()
	_pending_batches.append(packet)
	_pending_batches_mutex.unlock()

func _try_decompress(bytes: PackedByteArray) -> PackedByteArray:
	var buffer: StreamPeerBuffer = StreamPeerBuffer.new()
	buffer.data_array = bytes
	
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
	
