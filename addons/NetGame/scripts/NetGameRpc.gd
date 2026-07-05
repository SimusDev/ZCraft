extends Node

static var flush_tickrate: float = 60.0

var _flush_time: float = 0.0

var _rpc_send_queue: NetCoreElementBatcher = NetCoreElementBatcher.new(256)
var _rpc_send_queue_mutex: Mutex = Mutex.new()

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
	if !_rpc_send_queue.get_data().is_empty():
		var task: int = WorkerThreadPool.add_task(_flush_rpc_threaded, true)
		WorkerThreadPool.wait_for_task_completion(task)

#region RPC

func _flush_rpc_threaded() -> void:
	_rpc_send_queue_mutex.lock()
	var unprocessed: Dictionary[int, Array] = _rpc_send_queue.swap_and_clear()
	_rpc_send_queue_mutex.unlock()
	
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
	var packet_buffer: NetGameBuffer = NetGameBuffer.new()
	
	var cached_callable_id: Dictionary[Callable, int] = {}
	var cached_configs: Dictionary[int, NetGameRpcConfig] = {}
	
	for rpc_info: Dictionary[RpcInfoKey, Variant] in rpcs:
		pass

func _validate_config(config: NetGameRpcConfig, from_peer: int, authority: int) -> bool:
	if config.permission == config.Permission.Server:
		return from_peer == 1
	elif config.permission == config.Permission.Authority:
		return from_peer == authority
	return true

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
