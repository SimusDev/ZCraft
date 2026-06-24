extends RefCounted
class_name NetID

var _id: int = 0

static var _global_id_offset: int = 0

const _META: StringName = "NetID"

func get_id() -> int:
	return _id

static func from_object(object: Object) -> NetID:
	var new: NetID = NetID.new()
	new._id = object.get_meta("_META", -1)
	if new._id == -1:
		push_error("Cant find NetID: %s" % object)
		return null
	return new
