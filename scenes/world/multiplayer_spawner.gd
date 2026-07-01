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
		#player.set_multiplayer_authority(id)
		
		
		player.tree_entered.connect(
			func():
				_init_spawned_player.rpc(id)
		)
		
		spawn_node.add_child.call_deferred(player)

@rpc("authority", "call_local")
func _init_spawned_player(id: int) -> Error:
	await get_tree().process_frame # так делают pro))
	
	if spawn_points.is_empty():
		return FAILED
	
	var spawn_node: Node = get_node_or_null(spawn_path)
	if !spawn_node: return FAILED
	
	var player: Node = spawn_node.get_node_or_null(str(id)) as Node3D
	if !player: return FAILED
	
	player.global_position = spawn_points.pick_random().global_position
	return OK

func _on_peer_disconnected(id: int) -> void:
	var spawn_node = get_node_or_null(spawn_path)
	if spawn_node:
		var player = spawn_node.get_node_or_null(str(id))
		if is_instance_valid(player):
			player.queue_free()
