extends Node

static var flush_tickrate: float = 60.0

var _flush_time: float = 0.0

var _unprocessed_rpc_queue: Array[Dictionary] = []
var _processed_rpc_queue: Array[Dictionary] = []

var _rpc_unprocessed_queue_mutex: Mutex = Mutex.new()
var _rpc_processed_queue_mutex: Mutex = Mutex.new()
var _rpc_flush_mutex: Mutex = Mutex.new()

var _is_flush_rpc_busy: bool = false
var _flush_rpc_target: int = 0

var _rpc_batcher: NetCoreElementBatcher = NetCoreElementBatcher.new()

enum RpcInfoKey
{
	SendTo,
	Args,
	RpcObject,
	NetID,
}

func _process(delta: float) -> void:
	_flush_time += delta
	if _flush_time >= 1.0 / flush_tickrate:
		_flush()
		_flush_time = 0

func _flush() -> void:
	if !_unprocessed_rpc_queue.is_empty():
		var task: int = WorkerThreadPool.add_task(_flush_rpc_threaded, true)
		WorkerThreadPool.wait_for_task_completion(task)

#region RPC

func invoke(callable: Callable, ...args: Array) -> void:
	_rpc_unprocessed_queue_mutex.lock()
	_rpc_batcher.put(
		{
			
		} as Dictionary[RpcInfoKey, Variant]
	)
	
	_rpc_unprocessed_queue_mutex.unlock()

func invoke_on(callable: Callable, ...args: Array) -> void:
	pass

func invoke_async(callable: Callable, ...args: Array) -> void:
	pass

func _flush_rpc_threaded() -> void:
	
	_rpc_unprocessed_queue_mutex.lock()
	var unprocessed: Array = _unprocessed_rpc_queue
	_unprocessed_rpc_queue = []
	_rpc_unprocessed_queue_mutex.unlock()
	
	if unprocessed.is_empty():
		return
	
	WorkerThreadPool.add_group_task(
		_flush_rpc_task.bind(unprocessed),
		unprocessed.size(),
		-1,
		true
	)

func _flush_rpc_task(index: int, unprocessed: Array) -> void:
	return
