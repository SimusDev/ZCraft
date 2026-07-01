extends RefCounted
class_name NetCoreElementBatcher

var _data: Dictionary[int, Array] = {}

var _batch_count: int = 256
var _position: int = 0
var _index: int = 0

func get_data() -> Dictionary[int, Array]:
	return _data

func swap_and_clear() -> Dictionary[int, Array]:
	var local: Dictionary[int, Array] = _data
	_data = {}
	_position = 0
	return local

func _init(batch_count: int = 256) -> void:
	_batch_count = batch_count

func put(element: Variant) -> void:
	_data.get_or_add(_index, []).append(element)
	_position += 1
	if _position >= _batch_count:
		_position = 0
		_index += 1
