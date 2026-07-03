extends Node

static var flush_tickrate: float = 60.0

var _flush_time: float = 0.0

var _rpc_batcher: NetCoreElementBatcher = NetCoreElementBatcher.new(256)
var _rpc_batcher_mutex: Mutex = Mutex.new()

var _batch_step: int = 0

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
	
	var result: Array[Dictionary]
	var task: int = WorkerThreadPool.add_group_task(
		_process_rpc_batch_task.bind(unprocessed),
		unprocessed.size(),
		-1,
		true
	)
	
	WorkerThreadPool.wait_for_group_task_completion(task)
	

func _process_rpc_batch_task(index: int, unprocessed: Dictionary[int, Array]) -> void:
	var rpcs: Array = unprocessed[index]
	
	var buffer: NetGameBuffer = NetGameBuffer.new()
	
	var cached_callable_id: Dictionary[Callable, int] = {}
	var cached_configs: Dictionary[int, NetGameRpcConfig] = {}
	
	for rpc_info: Dictionary[RpcInfoKey, Variant] in rpcs:
		var callable: Callable = rpc_info[RpcInfoKey.Callable]
		var callable_id: int = cached_callable_id.get_or_add(callable, NetGameRpcRegistry.get_callable_id(callable))
		
		var object: Object = callable.get_object()
		
		if !is_instance_valid(object):
			push_error.call_deferred("Failed to validate callable %s, object is invalid" % [callable.get_method()])
			return
		
		if callable_id < 0:
			push_error.call_deferred("Failed to validate callable ID %s, %s, %s" % [callable_id, callable.get_object().get_in, callable])
			continue
		
		var config: NetGameRpcConfig = cached_configs.get_or_add(callable_id, NetGameRpcRegistry.get_callable_config(callable))
		if !is_instance_valid(config):
			push_error.call_deferred("Failed to find config %s, %s, %s" % [callable_id, callable.get_object(), callable])
			continue
		
		var serialized_args: PackedByteArray = serialize_args(buffer, rpc_info[RpcInfoKey.Args])
		

func serialize_args(buffer: NetGameBuffer, array: Array) -> PackedByteArray:
	buffer.write_int(array.size())
	for i in array:
		buffer.write(i)
	return buffer.get_data()

func deserialize_args(buffer: NetGameBuffer, bytes: PackedByteArray) -> Array:
	buffer.set_data(bytes)
	var size: int = buffer.read_int()
	var result: Array = []
	for i in size:
		result.append(buffer.read())
	return result
