extends Node3D


@export var player_scene:PackedScene
@export var players_node:Node3D
var players = {}

func _ready():
	# Только сервер (хост) управляет спавном
	if not multiplayer.is_server():
		return

	# Подключаем сигналы для обработки подключений/отключений
	multiplayer.peer_connected.connect(_on_player_connected)
	multiplayer.peer_disconnected.connect(_on_player_disconnected)

	# Спавним игрока для самого хоста (peer_id = 1)
	_spawn_player(1)

func _on_player_connected(id: int):
	_spawn_player(id)

func _on_player_disconnected(id: int):
	# Удаляем игрока, если он все еще есть в дереве
	var player = players_node.get_node_or_null(str(id))
	if player:
		player.queue_free()

func _spawn_player(id: int):
	# Создаем экземпляр сцены игрока
	var player = player_scene.instantiate()
	player.name = str(id)
	player.set_multiplayer_authority(id, true)
	# Добавляем в контейнер. Спавнер автоматически реплицирует его на всех клиентов
	players_node.add_child(player, true) # 'true' для force_readable_name
