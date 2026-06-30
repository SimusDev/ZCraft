extends MultiplayerSpawner

@export var player_prefab: PackedScene
@export var spawn_points: Array[Node3D]

func _ready() -> void:
	if multiplayer.is_server():
		multiplayer.peer_connected.connect(_on_peer_connected)
		_spawn_player(1)
	
	multiplayer.peer_disconnected.connect(_on_peer_disconnected)

func _on_peer_connected(id: int) -> void:
	_spawn_player(id)

func _spawn_player(id: int) -> void:
	var spawn_node: Node = get_node_or_null(spawn_path)
	if spawn_node:
		var player: Node = player_prefab.instantiate()
		player.name = str(id)
		
		
		player.tree_entered.connect(
			func():
				var tp: Vector3
				if !spawn_points.is_empty():
					tp = spawn_points.pick_random().global_position
				else:
					tp = spawn_node.global_position
				player.global_position = tp
				player.set_multiplayer_authority(id)
		)
		
		spawn_node.add_child.call_deferred(player)

func _on_peer_disconnected(id: int) -> void:
	var spawn_node = get_node_or_null(spawn_path)
	if spawn_node:
		var player = spawn_node.get_node_or_null(str(id))
		if is_instance_valid(player):
			player.queue_free()
