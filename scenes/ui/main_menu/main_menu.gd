extends Control

func _ready() -> void:
	multiplayer.connected_to_server.connect(_on_connected_to_server)

func _on_connected_to_server() -> void:
	get_tree().change_scene_to_file("res://scenes/world/World.tscn")
