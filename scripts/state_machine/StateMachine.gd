class_name StateMachine extends Node

signal state_enter(state_name: String)
signal state_exit(state_name: String)

var current_state: StateMachineState

func _init(state_list: Array[StateMachineState] = []) -> void:	
	for state in state_list:
		add_child(state, true)

@rpc("any_peer", "call_local")
func local_switch_by_name(state_name: StringName) -> void:
	for state in get_children():
		if !state is StateMachineState:
			continue
		
		if state.name == state_name:
			local_switch(state)
			break

func local_switch(state:StateMachineState) -> void:
	if !state:
		return
	
	if current_state == state:
		return
	
	if current_state:
		current_state.on_exit(self)
		state_exit.emit(current_state.name)
	
	current_state = state
	current_state.on_enter(self)
	state_enter.emit(current_state.name)


func switch(state:StateMachineState) -> void:
	local_switch.rpc(state)

func switch_by_name(state_name:StringName) -> void:
	local_switch_by_name.rpc(state_name)

func current_state_is(match_value:Variant) -> bool:
	if !current_state:
		return false
	
	if match_value is StringName:
		return current_state.name == match_value
	elif match_value is String:
		return current_state.name == StringName(match_value)
	elif match_value is Array:
		return match_value.has(current_state.name)
	elif match_value is Object:
		return current_state == match_value
	
	
	return false
