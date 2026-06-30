extends Node
class_name NetGameCommunicator

static var instance: NetGameCommunicator

static var flush_tickrate: float = 60.0

var _flush_time: float = 0.0

var _unprocessed_rpc_queue: NetCoreAllocatedArray = NetCoreAllocatedArray.new()
var _processed_rpc_queue: NetCoreAllocatedArray = NetCoreAllocatedArray.new()

var _rpc_unprocessed_queue_mutex: Mutex = Mutex.new()
var _rpc_processed_queue_mutex: Mutex = Mutex.new()
var _rpc_flush_mutex: Mutex = Mutex.new()

var _is_flush_rpc_busy: bool = false
var _flush_rpc_target: int = 0

enum RpcInfoKey
{
	SendTo,
	Args,
	RpcObject,
	NetID,
}

func _enter_tree() -> void:
	instance = self

func _process(delta: float) -> void:
	_flush_time += delta
	if _flush_time >= 1.0 / flush_tickrate:
		_flush()
		_flush_time = 0

func _flush() -> void:
	WorkerThreadPool.add_task(_flush_rpc_threaded, true)

#region RPC

func _queue_rpc(_rpc: NetGameRpc, send_to: NetGameRpcConfig.SendTo, args: Array) -> void:
	var rpc_info: Dictionary[RpcInfoKey, Variant] = {
		RpcInfoKey.SendTo: send_to,
		RpcInfoKey.Args: args,
		RpcInfoKey.RpcObject: _rpc,
		RpcInfoKey.NetID: NetCoreObjectID.get_or_create(_rpc.get_owner())
	}
	
	_rpc_unprocessed_queue_mutex.lock()
	_unprocessed_rpc_queue.append(rpc_info)
	_rpc_unprocessed_queue_mutex.unlock()

func _flush_rpc_threaded() -> void:
	_rpc_unprocessed_queue_mutex.lock()
	var unprocessed: Array = _unprocessed_rpc_queue.get_data()
	_unprocessed_rpc_queue.clear()
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
	var info: Dictionary[RpcInfoKey, Variant] = unprocessed[index]
	var _rpc: NetGameRpc = info[RpcInfoKey.RpcObject]
	if !is_instance_valid(_rpc):
		return
	
	var net_id: NetCoreObjectID = info[RpcInfoKey.NetID]
	if !is_instance_valid(net_id):
		return
	
	if !is_instance_valid(net_id.get_owner()):
		return
	
	var config: NetGameRpcConfig = _rpc.config
	if !is_instance_valid(config):
		return
	
	var validation: bool = config.validate(NetGame.unique_id, NetGame.get_object_authority(net_id.get_owner()))
	if !validation:
		push_error("Failed to validate RPC: %s" % [net_id.get_owner()])
		return
	
	
