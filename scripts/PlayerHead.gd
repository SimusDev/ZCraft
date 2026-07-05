class_name PlayerHead extends EntityHead

@export var sensitivity = 1.0
const SENSITIVITY_NORMALIZE_VALUE = 0.1

func _ready():
	var enabled = is_multiplayer_authority()
	set_process(enabled)
	set_process_input(enabled)
	set_process_unhandled_input(enabled)
	if !enabled:
		#camera.current = false
		return
	
	camera.make_current()
	
	InterfaceStack.on_active_changed.connect(
		_interface_stack_changed
	)
	
	_set_capture_mode()

func _interface_stack_changed() -> void:
	_set_capture_mode()

func _set_capture_mode() -> void:
	if InterfaceStack.has_active():
		Input.mouse_mode = Input.MOUSE_MODE_VISIBLE
	else:
		Input.mouse_mode = Input.MOUSE_MODE_CAPTURED

func _unhandled_input(event):
	if InterfaceStack.has_active():
		return
	
	if event is InputEventMouseMotion:
		var relative:Vector2 = event.relative * (sensitivity * SENSITIVITY_NORMALIZE_VALUE)
		
		var x: float = deg_to_rad(-relative.y)
		var y: float = deg_to_rad(-relative.x)

		if entity:
			entity.rotate_y(y)
		rotate_x(x)
		rotation.x = clamp(rotation.x, deg_to_rad(-90), deg_to_rad(90))
