extends Node3D


@export var player_scene:PackedScene
@export var spawnpointmanagerhandlerprocessor:Node3D
var players = {}



func _on_player_connected(peer_id: int):
	var spawn_pos = Vector3(randf_range(-5, 5), 2, randf_range(-5, 5))
	spawn_player.rpc(peer_id, spawn_pos)

@rpc("authority", "call_local", "reliable")
func spawn_player(peer_id: int, spawn_pos: Vector3):
	var player = player_scene.instantiate()
	player.name = str(peer_id)
	player.set_multiplayer_authority(peer_id)
	add_child(player, true)
	player.global_position = spawnpointmanagerhandlerprocessor.global_position + spawn_pos
	players[peer_id] = player
