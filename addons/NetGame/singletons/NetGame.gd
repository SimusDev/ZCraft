extends Node

signal on_connected_to_server()
signal on_disconnected_from_server()

@export var game_garbage_collector: NetGameGarbageCollector

signal on_packet_received(type: PacketType, bytes: PackedByteArray, peer: int)

enum PacketType
{
	RpcRequest,
	RpcReceive,
}

const SERVER_ID: int = 1

var _main_thread_buffer: StreamPeerBuffer = StreamPeerBuffer.new()

static var is_server: bool = false
static var unique_id: int = SERVER_ID

var _metadata_store: Dictionary[int, Dictionary] = {}
var _metadata_store_mutex: Mutex = Mutex.new()

func get_or_add_object_meta(object: Object, meta: StringName, default: Variant = null) -> Variant:
	_metadata_store_mutex.lock()
	if is_instance_valid(object):
		var meta_dict: Dictionary[StringName, Variant] = _metadata_store.get_or_add(
			object.get_instance_id(), {} as Dictionary[StringName, Variant])
		_metadata_store_mutex.unlock()
		return meta_dict.get_or_add(meta, default)
	_metadata_store_mutex.unlock()
	return default

func get_object_meta(object: Object, meta: StringName, default: Variant = null) -> Variant:
	_metadata_store_mutex.lock()
	if is_instance_valid(object):
		var meta_dict: Dictionary[StringName, Variant] = _metadata_store.get_or_add(
			object.get_instance_id(), {} as Dictionary[StringName, Variant])
		_metadata_store_mutex.unlock()
		return meta_dict.get(meta, default)
	_metadata_store_mutex.unlock()
	return default

func set_object_meta(object: Object, meta: StringName, value: Variant) -> void:
	_metadata_store_mutex.lock()
	if is_instance_valid(object):
		var meta_dict: Dictionary[StringName, Variant] = _metadata_store.get_or_add(
			object.get_instance_id(), {} as Dictionary[StringName, Variant])
		meta_dict.set(meta, value)
	_metadata_store_mutex.unlock()

func _collect_metadata_garbage() -> void:
	for instance_id: int in _metadata_store:
		_metadata_store_mutex.lock()
		if !is_instance_id_valid(instance_id):
			_metadata_store.erase(instance_id)
		_metadata_store_mutex.unlock()

func _on_garbage_collector_try_collect() -> void:
	if !_metadata_store.is_empty():
		WorkerThreadPool.add_task(_collect_metadata_garbage)

func _ready() -> void:
	game_garbage_collector.on_try_collect.connect(_on_garbage_collector_try_collect)

func setup(api: SceneMultiplayer = null) -> void:
	if !is_instance_valid(api):
		api = SceneMultiplayer.new()
	
	NetCore.setup(api)
	NetCore.multiplayer_peer_packet.connect(_on_multiplayer_peer_packet)

func _on_multiplayer_peer_packet(peer: int, bytes: PackedByteArray) -> void:
	_main_thread_buffer.data_array = bytes
	_main_thread_buffer.seek(0)
	
	var type: PacketType = _main_thread_buffer.get_u8()
	bytes = _main_thread_buffer.get_data(_main_thread_buffer.get_available_bytes())[1]
	on_packet_received.emit(type, bytes, peer)

func send_packet(type: PacketType, bytes: PackedByteArray, peer: int = 0, 
mode: MultiplayerPeer.TransferMode = MultiplayerPeer.TransferMode.TRANSFER_MODE_RELIABLE, 
channel: int = 0) -> void:
	
	_main_thread_buffer.clear()
	_main_thread_buffer.seek(0)
	_main_thread_buffer.put_u8(type)
	_main_thread_buffer.put_data(bytes)
	NetCore.multiplayer_send_bytes(_main_thread_buffer.data_array, peer, mode, channel)

func _on_tick_timeout() -> void:
	is_server = multiplayer.is_server()
	unique_id = multiplayer.get_unique_id()

func get_object_authority(object: Object) -> int:
	if object.has_method("get_multiplayer_authority"):
		return object.get_multiplayer_authority() == unique_id
	return unique_id == SERVER_ID
