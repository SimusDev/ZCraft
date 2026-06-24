extends Node3D

func _ready() -> void:
	if multiplayer.is_server():
		var ss = Node3D.new()
		ss.name = "SERVER"
		add_child(ss)
