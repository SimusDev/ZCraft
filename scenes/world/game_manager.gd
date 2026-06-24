extends Node3D

@export var player_scene:PackedScene
@export var players_node:Node3D

func _ready():
	if multiplayer.is_server():
		multiplayer.peer_connected.connect(_on_server_player_connected)
		multiplayer.peer_disconnected.connect(_on_server_player_disconnected)
		_spawn_player(1)
	else:
		_request.rpc_id(1)

@rpc("any_peer", "reliable")
func _request() -> void:
	for i in players_node.get_children():
		_spawn_player.rpc_id(multiplayer.get_remote_sender_id(), int(i.name))
		

@rpc("authority", "reliable")
func _receive(peers: PackedInt32Array) -> void:
	for pid: int in peers:
		_spawn_player(pid)

func _on_server_player_connected(peer: int) -> void:
	_spawn_player.rpc(peer)

func _on_server_player_disconnected(peer: int) -> void:
	_despawn_player.rpc(peer)

@rpc("authority", "call_local", "reliable")
func _spawn_player(peer: int) -> void:
	var instance: Node = player_scene.instantiate()
	instance.set_multiplayer_authority(peer)
	instance.name = str(peer)
	players_node.add_child.call_deferred(instance)

@rpc("authority", "call_local", "reliable")
func _despawn_player(peer: int) -> void:
	var player: Node = players_node.get_node_or_null(str(peer))
	if player:
		player.queue_free()
