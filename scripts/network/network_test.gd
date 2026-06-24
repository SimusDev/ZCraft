extends Node

func _ready() -> void:
	NetPacketProcessor.get_instance().on_received_threaded.connect(_on_received_threaded)

func _on_received_threaded(peer: int, bytes: PackedByteArray) -> void:
	print('hello from %s' % bytes)

func _on_server_pressed() -> void:
	NetworkManager.connection.create_server(8080)

func _on_client_pressed() -> void:
	NetworkManager.connection.create_client("localhost", 8080)

func _on_button_pressed() -> void:
	print(multiplayer.get_peers())
	for id in multiplayer.get_peers():
		NetPacketProcessor.get_instance().send(
			NetPacketProcessor.HEADER.NONE, 
			PackedByteArray(),
			id
		)
	
