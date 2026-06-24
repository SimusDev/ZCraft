extends Node

func _ready() -> void:
	NetPacketProcessor.get_instance().on_received.connect(_on_received)

func _on_received(peer: int, header, bytes: PackedByteArray) -> void:
	print('hello from %s %s' % [peer, bytes_to_var(bytes)])

func _on_server_pressed() -> void:
	NetworkManager.connection.create_server(8080)

func _on_client_pressed() -> void:
	NetworkManager.connection.create_client("localhost", 8080)

func _on_button_pressed() -> void:
	for id in multiplayer.get_peers():
		NetPacketProcessor.get_instance().send(
			NetPacketProcessor.HEADER.NONE, 
			var_to_bytes("osas!"),
			id
		)
	
