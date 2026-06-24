extends Node

var API: SceneMultiplayer = SceneMultiplayer.new()

@export var connection: NetConnection

func _ready() -> void:
	get_tree().set_multiplayer(API)
