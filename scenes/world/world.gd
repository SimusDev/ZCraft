extends Node3D

func _ready() -> void:
	var ss = Node.new()
	if multiplayer.is_server():
		ss.name = "SERVER"
	else:
		ss.name = "CLIENT"
	
	add_child(ss)
