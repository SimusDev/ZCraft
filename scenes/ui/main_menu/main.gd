extends Control

@onready var container: VBoxContainer = $VBoxContainer

func _ready() -> void:
	for c in container.get_children():
		if c is BaseButton:
			c.pressed.connect(_on_button_pressed.bind(c.name))

func _on_button_pressed(btn_name: StringName) -> void:
	match btn_name:
		"ButtonStartServer":
			var port: int = 8080
			
			var server_adress = container.get_node_or_null("ServerAdress") as LineEdit
			if server_adress:
				var parsed: PackedStringArray = server_adress.text.split(":")
				if !parsed.is_empty():
					port = int(parsed.get(parsed.size()-1))

			var current_peer = multiplayer.multiplayer_peer as ENetMultiplayerPeer
			if is_instance_valid(current_peer):
				current_peer.close()
			current_peer = ENetMultiplayerPeer.new()
			var error = current_peer.create_server(port, 32)
			if error == OK:
				multiplayer.multiplayer_peer = current_peer
				get_tree().change_scene_to_file("res://scenes/world/World.tscn")
			
		"ButtonConnect":
			var ip: String = "localhost"
			var port: int = 8080

			var server_adress = container.get_node_or_null("ServerAdress") as LineEdit
			if server_adress:
				var parsed: PackedStringArray = server_adress.text.split(":")
				if parsed.size() == 2:
					ip = parsed.get(0)
					port = int(parsed.get(1))

			var current_peer = multiplayer.multiplayer_peer as ENetMultiplayerPeer
			if is_instance_valid(current_peer):
				current_peer.close()
			
			current_peer = ENetMultiplayerPeer.new()

			var error = current_peer.create_client(ip, port)
			if error == OK:
				multiplayer.multiplayer_peer = current_peer
		_:
			pass
