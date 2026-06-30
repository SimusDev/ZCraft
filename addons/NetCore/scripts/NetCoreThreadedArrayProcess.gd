extends RefCounted
class_name NetCoreThreadedArrayProcess

var _incoming: Array[Variant] = []
var _processed: Array[Variant] = []
var _processed_count: int = 0

signal processed(result: Array[Variant])

var _mutex: Mutex = Mutex.new()
var _count_mutex: Mutex = Mutex.new()

var is_busy: bool = false

func add_incoming(data: Variant) -> void:
	_mutex.lock()
	_incoming.append(data)
	_mutex.unlock()

func try_process_all() -> void:
	if is_busy:
		#print("Is Busy... Wait...")
		return
	
	WorkerThreadPool.add_task(
		_process_all_threaded,
		true
	)
	
	is_busy = true

func _process_all_threaded() -> void:
	_mutex.lock()
	
	var local_incoming: Array[Variant] = _incoming
	#print("Start process...")
	#print(local_incoming)
	_incoming = []
	
	_processed.clear()
	_processed.resize(local_incoming.size())
	_processed_count = 0
	
	_mutex.unlock()
	
	var task_id: int = WorkerThreadPool.add_group_task(
		_process_one_threaded.bind(local_incoming),
		local_incoming.size(),
		-1,
		true
	)

func _process_one_threaded(index: int, incoming: Array[Variant]) -> void:
	var result: Variant = _process_array_variant_threaded(incoming[index])
	
	_count_mutex.lock() 
	
	_processed.set(index, result)
	_processed_count += 1
	
	var is_done: bool = _processed_count == _processed.size()
	_count_mutex.unlock()
	
	if is_done:
		_done.call_deferred()

func _done() -> void:
	_mutex.lock()
	
	is_busy = false
	processed.emit(_processed)
	
	_mutex.unlock()

func _process_array_variant_threaded(variant) -> Variant:
	return null
