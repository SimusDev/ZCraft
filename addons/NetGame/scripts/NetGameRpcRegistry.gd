@static_unload
extends RefCounted
class_name NetGameRpcRegistry

const META_CALLABLES_ID: StringName = "NetGameRpcCallableID"
const META_CONFIG: StringName = "NetGameRpcConfig"

func _init(callables: Array[Callable], config: NetGameRpcConfig = null) -> void:
	for callable in callables:
		var object: Object = callable.get_object()
		if !is_instance_valid(object):
			push_error("Failed to register Rpc, object is %s" % object)
			return
		
		if !config:
			config = NetGameRpcConfig.new()
		
		NetCoreObjectID.get_or_create(object)
		
		var callables_id: Dictionary[Callable, int] = get_callables_id(object)
		callables_id.set(callable, callables_id.size())
		
		var configs: Dictionary[Callable, NetGameRpcConfig] = NetGame.get_or_add_object_meta(callable.get_object(), META_CONFIG, {} as Dictionary[Callable, NetGameRpcConfig])
		configs.set(callable, config)

static func get_callable_config(callable: Callable) -> NetGameRpcConfig:
	var configs: Dictionary[Callable, NetGameRpcConfig] = NetGame.get_or_add_object_meta(callable.get_object(), META_CONFIG, {} as Dictionary[Callable, NetGameRpcConfig])
	return configs.get(callable, null)

static func get_callables_id(object: Object) -> Dictionary[Callable, int]:
	return NetGame.get_or_add_object_meta(object, META_CALLABLES_ID, {} as Dictionary[Callable, int])

static func get_callable_id(callable: Callable) -> int:
	return get_callables_id(callable.get_object()).get(callable, -1)

static func _validate_rpc(callable: Callable) -> bool:
	return false
