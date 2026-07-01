extends Node

static var flush_tickrate: float = 60.0

var _flush_time: float = 0.0

var _rpc_batcher: NetCoreElementBatcher = NetCoreElementBatcher.new(256)
var _rpc_batcher_mutex: Mutex = Mutex.new()

enum RpcInfoKey
{
	Callable,
	Args,
	Type,
	TargetID,
}

func _process(delta: float) -> void:
	_flush_time += delta
	if _flush_time >= 1.0 / flush_tickrate:
		_flush()
		_flush_time = 0

func _flush() -> void:
	if !_rpc_batcher.get_data().is_empty():
		var task: int = WorkerThreadPool.add_task(_flush_rpc_threaded, true)
		WorkerThreadPool.wait_for_task_completion(task)

#region RPC

func invoke(callable: Callable, ...args: Array) -> void:
	_rpc_batcher_mutex.lock()
	
	_rpc_batcher.put(
		{
			RpcInfoKey.Callable: callable,
			RpcInfoKey.Args: args,
			RpcInfoKey.Type: NetGameRpcConfig.Type.All
		} as Dictionary[RpcInfoKey, Variant]
	)
	
	_rpc_batcher_mutex.unlock()

func invoke_on(id: int, callable: Callable, ...args: Array) -> void:
	_rpc_batcher_mutex.lock()
	
	_rpc_batcher.put(
		{
			RpcInfoKey.Callable: callable,
			RpcInfoKey.Args: args,
			RpcInfoKey.Type: NetGameRpcConfig.Type.Target,
			RpcInfoKey.TargetID: id
		} as Dictionary[RpcInfoKey, Variant]
	)
	
	_rpc_batcher_mutex.unlock()

func invoke_on_server(callable: Callable, ...args: Array) -> void:
	_rpc_batcher_mutex.lock()
	
	_rpc_batcher.put(
		{
			RpcInfoKey.Callable: callable,
			RpcInfoKey.Args: args,
			RpcInfoKey.Type: NetGameRpcConfig.Type.OnServer
		} as Dictionary[RpcInfoKey, Variant]
	)
	
	_rpc_batcher_mutex.unlock()

func invoke_async(callable: Callable, ...args: Array) -> void:
	_rpc_batcher_mutex.lock()
	
	_rpc_batcher.put(
		{
			RpcInfoKey.Callable: callable,
			RpcInfoKey.Args: args,
			RpcInfoKey.Type: NetGameRpcConfig.Type.Async
		} as Dictionary[RpcInfoKey, Variant]
	)
	
	_rpc_batcher_mutex.unlock()

func _flush_rpc_threaded() -> void:
	_rpc_batcher_mutex.lock()
	var unprocessed: Dictionary[int, Array] = _rpc_batcher.swap_and_clear()
	_rpc_batcher_mutex.unlock()
	
	if unprocessed.is_empty():
		return
	
	var task: int = WorkerThreadPool.add_group_task(
		_process_rpc_batch_task.bind(unprocessed),
		unprocessed.size(),
		-1,
		true
	)
	
	WorkerThreadPool.wait_for_group_task_completion(task)
	

func _process_rpc_batch_task(index: int, unprocessed: Dictionary[int, Array]) -> void:
	for batch_id: int in unprocessed:
		var rpcs: Array = unprocessed[batch_id]
		for rpc_info: Dictionary[RpcInfoKey, Variant] in rpcs:
			var callable: Callable = rpc_info[RpcInfoKey.Callable]
			var callable_id: int = NetGameRpcRegistry.get_callable_id(callable)
			
			if callable_id < 0:
				push_error.call_deferred("Failed to validate callable ID %s, %s, %s" % [callable_id, callable.get_object(), callable])
				continue
			
			var config: NetGameRpcConfig = NetGameRpcRegistry.get_callable_config(callable)
			if !is_instance_valid(config):
				push_error.call_deferred("Failed to find config %s, %s, %s" % [callable_id, callable.get_object(), callable])
				continue
			

func _get_callable_unique_id(callable: Callable) -> void:
	pass
