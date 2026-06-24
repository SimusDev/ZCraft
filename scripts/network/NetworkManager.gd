extends Node

@export var connection:NetConnection

var API: SceneMultiplayer = SceneMultiplayer.new()

func _ready() -> void:
	get_tree().set_multiplayer(API)
