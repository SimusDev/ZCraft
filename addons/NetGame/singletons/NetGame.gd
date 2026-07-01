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

func _ready() -> void:
	pass

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
