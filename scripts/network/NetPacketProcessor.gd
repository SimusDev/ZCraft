extends Node
class_name NetPacketProcessor

enum HEADER {
	NONE,
	COMMUNICATOR_MESSAGE,
}

signal on_received(header: HEADER, bytes: PackedByteArray)

var _buffer: StreamPeerBuffer = StreamPeerBuffer.new()

func _ready() -> void:
	(multiplayer as SceneMultiplayer).peer_packet.connect(_recieve_raw)

func send(bytes: PackedByteArray,
peer: int,
mode: MultiplayerPeer.TransferMode = MultiplayerPeer.TransferMode.TRANSFER_MODE_RELIABLE,
channel: int = 0) -> void:
	(multiplayer as SceneMultiplayer).send_bytes(
		bytes,
		peer,
		mode,
		channel
		)

func _recieve_raw(peer: int, bytes: PackedByteArray) -> void:
	WorkerThreadPool.add_task(_proccess_threaded.bind(peer, bytes), true)

func _proccess_threaded(peer: int, bytes: PackedByteArray) -> void:
	pass
