@static_unload
extends RefCounted
class_name NetGameRpc

static var sender_id: int = 1

var _owner: WeakRef = WeakRef.new()
var config: NetGameRpcConfig

func get_owner() -> Object:
	return _owner.get_ref()

func _init(callable: Callable, config: NetGameRpcConfig = null) -> void:
	if !is_instance_valid(config):
		config = NetGameRpcConfig.new()
	
	self.config = config
	_owner = weakref(callable.get_object())

func invoke(...args: Array) -> void:
	NetGameCommunicator.instance._queue_rpc(self, NetGameRpcConfig.SendTo.All, args)

func invoke_on(id: int, ...args: Array) -> void:
	NetGameCommunicator.instance._queue_rpc(self, NetGameRpcConfig.SendTo.Target, args)

func invoke_on_server(id: int, ...args: Array) -> void:
	NetGameCommunicator.instance._queue_rpc(self, NetGameRpcConfig.SendTo.Server, args)
