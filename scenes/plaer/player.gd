extends CharacterBody3D

@export var speed:float = 5.0
@export var mouse_sensitivity: float = 0.1

@export var _is_pipi:bool = false :
	set(val):
		_is_pipi = val
		$"314314".visible = val
		
		$"314314/GPUParticles3D".emitting = val
		
		if val:
			$"314314/AnimationPlayer".stop()
			$"314314/AnimationPlayer".play("new_animation")

func _enter_tree() -> void:
	set_multiplayer_authority(int(name))

func _ready() -> void:
	var enabled = is_multiplayer_authority()
	set_process(enabled)
	set_physics_process(enabled)
	set_process_input(enabled)

	if enabled:
		$Camera3D.make_current()
		Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED)
	else:
		$Camera3D.current = false
func _input(event: InputEvent) -> void:
	if Input.is_action_just_pressed(&"pipi"):
		_is_pipi = !_is_pipi

	if event is InputEventMouseMotion and Input.get_mouse_mode() == Input.MOUSE_MODE_CAPTURED:
		rotate_y(-event.relative.x * mouse_sensitivity * 0.01)
		$Camera3D.rotate_x(-event.relative.y * mouse_sensitivity * 0.01)
		$Camera3D.rotation.x = clamp($Camera3D.rotation.x, -PI/2, PI/2)


func _physics_process(delta: float) -> void:
	var input_dir = Input.get_vector(&"move_left", &"move_right", &"move_forward", &"move_backward")
	var direction = (transform.basis * Vector3(input_dir.x, 0, input_dir.y)).normalized()
	
	if Input.is_action_pressed(&"move_up"):
		direction.y += 1.0
	if Input.is_action_pressed(&"move_down"):
		direction.y -= 1.0
	
	velocity = direction * speed
	
	move_and_slide()
