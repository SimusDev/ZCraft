extends Node3D

func _ready() -> void:
	var ss = Node.new()
	if multiplayer.is_server():
		ss.name = "SERVER"
	else:
		ss.name = "CLIENT"
	
	add_child(ss)

var communicator: GDNetCommunicator = GDNetCommunicator.new()
var aoi: GDNetAoI = GDNetAoI.new()
func _process(delta: float) -> void:
	for i in 1000:
		communicator.Send(aoi, [])
