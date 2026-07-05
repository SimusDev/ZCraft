@static_unload
extends RefCounted
class_name NetGameRpcRegistry

const META_REGISTRY: StringName = "NetGameRpcRegistry"

var unique_id: int = 0

var config: NetGameRpcConfig

var _object_ref: WeakRef
var _net_object_ref: WeakRef

func _init(callable: Callable, cfg: NetGameRpcConfig = null) -> void:
	var object: Object = callable.get_object()
	if !is_instance_valid(object):
		push_error("Failed to register Rpc, object is %s" % object)
		return
	
	_object_ref = weakref(object)
	
	if !cfg:
		cfg = NetGameRpcConfig.new()
	
	config = cfg
	
	_net_object_ref = weakref(NetCoreObjectID.get_or_create(object))
	
	if !object.has_meta(META_REGISTRY):
		object.set_meta(META_REGISTRY, {} as Dictionary[int, NetGameRpcRegistry])
	
	var registry: Dictionary[int, NetGameRpcRegistry] = object.get_meta(META_REGISTRY)
	unique_id = registry.size()
	registry[unique_id] = self

func _get_network_object() -> NetCoreObjectID:
	return _net_object_ref.get_ref()

enum RpcInfoKey
{
	Args,
	UniqueID,
	NetID,
	MultiplayerAuthority
}

func invoke(...args: Array) -> void:
	NetGameRpc._rpc_send_queue.put(
		{
			RpcInfoKey.Args: config.buffer.write_array(),
			RpcInfoKey.UniqueID: unique_id,
			RpcInfoKey.NetID: _get_network_object().get_network_id(),
			RpcInfoKey.MultiplayerAuthority: NetGame.get_object_authority(_object_ref.get_ref())
		} as Dictionary[RpcInfoKey, Variant]
	)

func invoke_on_server(...args: Array) -> void:
	pass

func invoke_on(...args: Array) -> void:
	pass

func invoke_async(...args: Array) -> void:
	pass
