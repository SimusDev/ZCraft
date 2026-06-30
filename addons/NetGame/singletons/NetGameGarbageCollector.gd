extends Node
class_name NetGameGarbageCollector

static var instance: NetGameGarbageCollector

@export var wait_time: float = 30.0 : set = set_wait_time, get = get_wait_time

var _timer: Timer

signal on_try_collect()

func set_wait_time(value: float) -> void:
	wait_time = value
	if is_instance_valid(_timer):
		_timer.wait_time = value

func get_wait_time() -> float:
	return wait_time

func _enter_tree() -> void:
	instance = self

func _ready() -> void:
	_timer = Timer.new()
	_timer.wait_time = wait_time
	_timer.timeout.connect(_on_timer_timeout)
	add_child(_timer)
	_timer.start()

func _on_timer_timeout() -> void:
	_timer.wait_time = wait_time
	try_collect()

func try_collect() -> void:
	on_try_collect.emit()
