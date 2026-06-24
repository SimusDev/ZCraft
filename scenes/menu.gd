extends Control


func _ready() -> void:
	multiplayer.connected_to_server.connect(_no_connected_to_server)

#😡😡😡🤬🤬🤬🤬🤬💢💢
func _no_connected_to_server() -> void:
	get_tree().change_scene_to_file("res://scenes/world/World.tscn")

func _on_button_pressed() -> void:
	NetworkManager.connection.create_server(5678)
	get_tree().change_scene_to_file("res://scenes/world/World.tscn")


func _on_button_2_pressed() -> void:
	NetworkManager.connection.create_client($LineEditsex.text, int($LineEditsex2.text))
