extends Node

var _active: Array[Node] = []

var _canvas_registry: Array[CanvasItem] = []

signal on_activate(interface: Node)
signal on_deactivate(interface: Node)
signal on_active_changed()

func _ready() -> void:
	Panku.interactive_shell_visibility_changed.connect(_on_panku_interactive_shell_visibility_changed)

func _on_panku_interactive_shell_visibility_changed(visible: bool) -> void:
	if visible:
		open(Panku)
	else:
		close(Panku)

func register_canvas(interface: CanvasItem) -> void:
	if interface in _canvas_registry:
		return
	
	interface.tree_exited.connect(_on_canvas_tree_exited.bind(interface))
	interface.draw.connect(_on_canvas_draw.bind(interface))
	interface.hidden.connect(_on_canvas_hidden.bind(interface))
	if interface.is_inside_tree():
		if interface.is_visible_in_tree():
			open(interface)

func _on_canvas_tree_exited(interface: CanvasItem) -> void:
	var queued_for_free: bool = _is_interface_queued_for_deletion(interface)
	if queued_for_free:
		_canvas_registry.erase(interface)

func _on_canvas_hidden(interface: CanvasItem) -> void:
	close(interface)

func _on_canvas_draw(interface: CanvasItem) -> void:
	open(interface)

func open(interface: Node) -> void:
	if _active.has(interface):
		return
	
	_active.append(interface)
	on_active_changed.emit()
	on_activate.emit(interface)

func close(interface: Node) -> void:
	if !_active.has(interface):
		return
	
	_active.erase(interface)
	on_active_changed.emit()
	on_deactivate.emit(interface)

func _is_interface_queued_for_deletion(interface: Node) -> bool:
	if interface.get_parent() == null:
		return false
	if interface.is_queued_for_deletion():
		return true
	return _is_interface_queued_for_deletion(interface.get_parent())

func get_active() -> Array[Node]:
	return _active

func get_active_size() -> int:
	return _active.size()

func has_active() -> bool:
	return _active.size() > 0
