extends Node

var API: SceneMultiplayer = SceneMultiplayer.new()

func _ready() -> void:
	get_tree().set_multiplayer(API)
