extends RefCounted
class_name NetGameAoI

var _peers: PackedInt32Array = []

const META: StringName = "NetGameAoI"

func add_peer(pid: int) -> NetGameAoI:
	if !_peers.has(pid):
		_peers.append(pid)
	return self

func remove_peer(pid: int) -> NetGameAoI:
	_peers.erase(pid)
	return self

func set_peer(pid: int, visible: bool) -> NetGameAoI:
	if visible:
		add_peer(pid)
	else:
		remove_peer(pid)
	return self

func set_peers(peers: PackedInt32Array) -> NetGameAoI:
	_peers = peers
	return self

func get_peers() -> PackedInt32Array:
	return _peers

func is_visible_for(pid: int) -> bool:
	if _peers.is_empty():
		return true
	
	return _peers.has(pid)

static func get_or_create(object: Object) -> NetGameAoI:
	if object.has_meta(META):
		return object.get_meta(META)
	var aoi: NetGameAoI = NetGameAoI.new()
	object.set_meta(META, aoi)
	return aoi

static func is_object_visible_for(object: Object, pid: int) -> bool:
	return get_or_create(object).is_visible_for(pid)
