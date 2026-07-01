extends RefCounted
class_name NetGameRpcRegistry

func _init(callable: Callable, config: NetGameRpcConfig = null) -> void:
	var object: Object = callable.get_object()
	if !is_instance_valid(object):
		return
	
	NetCoreObjectID.get_or_create(object)
