class_name StateMachineState extends Node


func _init(p_name:StringName = "") -> void:
	if !p_name.is_empty():
		name = p_name

func on_enter(_sm:StateMachine) -> void:
	pass

func on_exit(_sm:StateMachine) -> void:
	pass
