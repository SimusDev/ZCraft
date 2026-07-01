@tool
extends EditorPlugin

func _enable_plugin() -> void:
	add_autoload_singleton("NetGame", "res://addons/NetGame/singletons/NetGame.tscn")
	add_autoload_singleton("NetGameRpc", "res://addons/NetGame/scripts/NetGameRpc.gd")

func _disable_plugin() -> void:
	remove_autoload_singleton("NetGame")
	remove_autoload_singleton("NetGameRpc")

func _enter_tree() -> void:
	# Initialization of the plugin goes here.
	pass

func _exit_tree() -> void:
	# Clean-up of the plugin goes here.
	pass
