extends RefCounted
class_name NetCoreAllocatedArray

var _data: Array = []
var _size: int = 0

func _init(initial_capacity: int = 0) -> void:
	if initial_capacity > 0:
		_data.resize(initial_capacity)
	_size = 0

func append(value: Variant) -> void:
	if _size >= _data.size():
		var new_capacity: int = _data.size() * 2
		if new_capacity < 8:
			new_capacity = 8
		_data.resize(new_capacity)
	
	_data[_size] = value
	_size += 1

func clear() -> void:
	_size = 0

func resize(new_size: int) -> void:
	if new_size < 0:
		push_error("Size cannot be negative: ", new_size)
		return
	
	if new_size > _data.size():
		_data.resize(new_size)
	_size = new_size

func get_value(index: int) -> Variant:
	if index < 0 or index >= _size:
		push_error("Index out of bounds: ", index)
		return null
	return _data[index]

func set_value(index: int, value: Variant) -> void:
	if index < 0 or index >= _size:
		push_error("Index out of bounds: ", index)
		return
	_data[index] = value

func size() -> int:
	return _size

func is_empty() -> bool:
	return _size == 0

func to_array() -> Array:
	if _size == 0:
		return []
	return _data.slice(0, _size)

func get_data() -> Array:
	return to_array()

func reserve(capacity: int) -> void:
	if capacity > _data.size():
		_data.resize(capacity)

func shrink() -> void:
	if _size < _data.size():
		_data.resize(_size)

func swap_and_clear() -> Array:
	var old = _data
	_data = []
	_size = 0
	return old

func append_array(arr: Array) -> void:
	var count: int = arr.size()
	if count == 0:
		return
	
	var needed: int = _size + count
	if needed > _data.size():
		var new_capacity: int = _data.size() * 2
		while new_capacity < needed:
			new_capacity *= 2
		_data.resize(new_capacity)
	
	for i in range(count):
		_data[_size + i] = arr[i]
	_size += count

func _to_string() -> String:
	var result: String = "["
	for i in range(_size):
		if i > 0:
			result += ", "
		result += str(_data[i])
	result += "]"
	return result
