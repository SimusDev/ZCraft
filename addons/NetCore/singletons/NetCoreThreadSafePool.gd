extends RefCounted
class_name ThreadSafePool

var _pool: Array = []
var _max_size: int = 64
var _mutex: Mutex = Mutex.new()
var _factory: Callable
var _cleanup: Callable

# -------- КОНСТРУКТОР --------
func _init(factory: Callable, cleanup: Callable = func(obj): pass, initial_size: int = 16, max_size: int = 64) -> void:
	_factory = factory
	_cleanup = cleanup
	_max_size = max_size
	
	for i in range(initial_size):
		_pool.append(_factory.call())

# -------- ПУБЛИЧНЫЙ API --------
func get_var() -> Variant:
	_mutex.lock()
	var obj = _pool.pop_back() if not _pool.is_empty() else null
	_mutex.unlock()
	return obj if obj != null else _factory.call()

func return_object(obj: Variant) -> void:
	if obj == null:
		return
	_cleanup.call(obj)
	_mutex.lock()
	if _pool.size() < _max_size:
		_pool.append(obj)
	_mutex.unlock()

func clear() -> void:
	_mutex.lock()
	_pool.clear()
	_mutex.unlock()

func size() -> int:
	_mutex.lock()
	var s = _pool.size()
	_mutex.unlock()
	return s
