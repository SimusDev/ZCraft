extends Node
class_name NetPacketProcessor

enum HEADER {
	NONE,
	COMMUNICATOR_MESSAGE,
}

signal on_received(peer: int, header: HEADER, bytes: PackedByteArray)

static var _instance: NetPacketProcessor

var _buffer: StreamPeerBuffer = StreamPeerBuffer.new()
var _buffer_threaded: StreamPeerBuffer = StreamPeerBuffer.new()

func _ready() -> void:
	_instance = self
	NetworkManager.API.peer_packet.connect(_recieve_raw)

static func get_instance() -> NetPacketProcessor:
	return _instance

func send(header: HEADER, bytes: PackedByteArray,
peer: int,
mode: MultiplayerPeer.TransferMode = MultiplayerPeer.TransferMode.TRANSFER_MODE_RELIABLE,
channel: int = 0) -> void:
	_buffer.clear()
	_buffer.put_u8(header)
	_buffer.put_data(bytes)
	NetworkManager.API.send_bytes(
		bytes,
		peer,
		mode,
		channel
		)

func _recieve_raw(peer: int, bytes: PackedByteArray) -> void:
	_proccess_threaded(peer, bytes)

func _proccess_threaded(peer: int, bytes: PackedByteArray) -> void:
	_buffer_threaded.seek(0)
	_buffer_threaded.data_array = bytes
	var header: HEADER = _buffer.get_u8()
	var data: PackedByteArray = _buffer.get_data(_buffer.get_available_bytes())
	on_received.emit(peer, header, data)
