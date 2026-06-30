@tool
class_name CharacterMovement extends Node

@export var state_machine:StateMachine

@export_group("Settings")
@export var jump_force:float = 15.0
@export var speed_multiplier:float = 1.0

@export_subgroup("Keys", "key_")
@export var key_forward:StringName = "move_forward"
@export var key_backward:StringName = "move_backward"
@export var key_left:StringName = "move_left"
@export var key_right:StringName = "move_right"
@export var key_crouch:StringName = "crouch"
@export var key_sprint:StringName = "sprint"

@export_group("Custom", "custom_")
@export var custom_character: CharacterBody3D:
	set(value):
		custom_character = value
		_bind_character()

var character:CharacterBody3D
var input_enabled:bool = true

func _ready() -> void:
	_bind_character()
	
	var enabled:bool = is_multiplayer_authority() and !(Engine.is_editor_hint())
	set_process(enabled)
	set_physics_process(enabled)
	set_process_input(enabled)
	
	#SimusDev.ui.interface_opened_or_closed.connect(_update_input_status)

func _update_input_status(_node:Node, _status:bool) -> void:
	#input_enabled = !SimusDev.ui.has_active_interface()
	pass

func _bind_character() -> void:
	if !custom_character:
		var target = get_parent()
		if !target is CharacterBody3D:
			push_error("[BaseCharacterMovement] '_bind_character' target is not CharacterBody3D")
			return
			
		character = get_parent()
	else:
		character = custom_character
	
	update_configuration_warnings()

func _physics_process(delta: float) -> void:
	if not character or not state_machine: return
	
	
	var current_state = state_machine.current_state as StateMachineMovementState
	if not current_state: return
	
	if not character.is_on_floor():
		character.velocity.y -= (ProjectSettings.get_setting("physics/3d/default_gravity") * 2.24) * delta
	elif Input.is_action_just_pressed(&"jump") and input_enabled:
		character.velocity.y = jump_force
		
	var input_dir = Input.get_vector(key_left, key_right, key_forward, key_backward)
	var direction:Vector3
	if input_enabled:
		direction = (character.global_transform.basis * Vector3(input_dir.x, 0, input_dir.y)).normalized()
	else:
		direction = Vector3.ZERO
	
	if character.is_on_floor():
		var target_vel = direction * current_state.state_speed * speed_multiplier
		var weight = current_state.acceleration if direction.length() > 0 else current_state.friction
		character.velocity.x = lerp(character.velocity.x, target_vel.x, weight * delta)
		character.velocity.z = lerp(character.velocity.z, target_vel.z, weight * delta)
	else:
		if direction.length() > 0:
			var air_accel = current_state.acceleration * 0.15
			var accel_force = direction * air_accel * delta
			
			var horizontal_vel = Vector2(character.velocity.x, character.velocity.z)
			if horizontal_vel.dot(Vector2(direction.x, direction.z)) < current_state.state_speed:
				character.velocity.x += accel_force.x
				character.velocity.z += accel_force.z
		
		var air_resistance = 0.998
		character.velocity.x *= air_resistance
		character.velocity.z *= air_resistance

	character.move_and_slide()

func _process(_delta: float) -> void:
	_handle_state_transitions()

func _handle_state_transitions() -> void:
	var input_dir = Input.get_vector(key_left, key_right, key_forward, key_backward)
	var is_moving = input_dir.length() > 0.1
	var wants_sprint = Input.is_action_pressed(key_sprint) and -(character.velocity.normalized() * character.transform.basis).z >= -0.1
	var wants_crouch = Input.is_action_pressed(key_crouch)
	
	
	var target_state_name: StringName = &"Idle"
	
	if !character.is_on_floor():
		target_state_name = &"Floating"
	elif wants_crouch:
		target_state_name = &"CrouchedWalking" if is_moving else &"Crouched"
	elif is_moving:
		target_state_name = &"Running" if wants_sprint else &"Walking"
	else:
		target_state_name = &"Idle"
	
	if state_machine:
		state_machine.switch_by_name(target_state_name)

func _get_configuration_warnings() -> PackedStringArray:
	var warnings = []
	var target = custom_character if custom_character else get_parent()
	
	if not target is CharacterBody3D:
		warnings.append("Parent must be a 'CharacterBody3D'")
	
	return warnings

func _notification(what):
	if what == NOTIFICATION_PARENTED or what == NOTIFICATION_UNPARENTED:
		update_configuration_warnings()
