class_name NetCorePlayerSpawner extends Node3D

@export var default_prefab: PackedScene
@export var players_node: Node
@export var spawn_points: Array[Node3D]

func _ready():
	if multiplayer.is_server():
		multiplayer.peer_connected.connect(_on_server_player_connected)
		multiplayer.peer_disconnected.connect(_on_server_player_disconnected)
		_spawn(1)
	else:
		_request.rpc_id(1)

func _on_server_player_connected(id: int) -> void:
	_spawn.rpc(id)

func _on_server_player_disconnected(id: int) -> void:
	_despawn.rpc(id)

@rpc("any_peer", "reliable")
func _request() -> void:
	for i in players_node.get_children():
		_spawn.rpc_id(multiplayer.get_remote_sender_id(), int(i.name))

func _pick_spawn_point() -> Node3D:
	if spawn_points.is_empty():
		return players_node
	
	return spawn_points.pick_random()

func _init_spawned_instance(instance: Node) -> void:
	if !is_instance_valid(instance):
		return

	print("%s : %s" % [get_multiplayer_authority(), instance.name])

	if instance is Node3D:
		var sp = _pick_spawn_point()
		var target_pos: Vector3 

		if is_instance_valid(sp) and sp.is_inside_tree():
			target_pos = sp.position
		else:
			target_pos = self.position

		instance.position = target_pos  


@rpc("authority", "call_local", "reliable")
func _spawn(id: int) -> Node:
	if !is_instance_valid(players_node):
		return null
	
	if players_node.has_node(str(id)):
		return

	var instance: Node = default_prefab.instantiate()
	instance.set_multiplayer_authority(id)
	
	instance.name = str(id)

	players_node.add_child(instance)
	
	var remote_sender_id: int = multiplayer.get_remote_sender_id()

	if remote_sender_id != 0:
		if id == remote_sender_id:
			instance.tree_entered.connect(
				_init_spawned_instance.bind(instance)
			)
	else:
		pass
	
	return instance

@rpc("authority", "call_local", "reliable")
func _despawn(id: int) -> void:
	var player: Node = players_node.get_node_or_null(str(id))
	if player:
		player.queue_free()
