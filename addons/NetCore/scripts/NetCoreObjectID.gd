extends RefCounted
class_name NetCoreObjectID

var _owner: WeakRef = WeakRef.new()

const META: StringName = "NetCoreObjectID"
const HASH_SALT: StringName = "SimusDevNetCore"
const HASH_SALT_RESOURCE: StringName = "SimusDevNetCoreResource"

var _network_id: int = 0

static var _next_network_id: int = 0

enum NodeEvent {
	Renamed,
	EnteredTree
}

func get_owner() -> Object:
	return _owner.get_ref()

func _init() -> void:
	_next_network_id += 1

func _initialize() -> void:
	var owner: Object = get_owner()
	if owner is Node:
		_initialize_as_node()
	elif owner is Resource:
		if !owner.resource_path.is_empty():
			_initialize_as_resource()

func _node_update_network_id_by_path() -> void:
	var owner: Node = get_owner()
	if owner.is_inside_tree():
		_network_id = owner.get_path().hash()

func _initialize_as_node() -> void:
	_node_update_network_id_by_path()
	
	var owner: Node = get_owner()
	owner.renamed.connect(_node_update_network_id_by_path)
	owner.tree_entered.connect(_node_update_network_id_by_path)

func _initialize_as_resource() -> void:
	var owner: Resource = get_owner()
	_network_id = ("%s:%s" % [HASH_SALT_RESOURCE, owner.resource_path]).hash()

func get_network_id() -> int:
	return _network_id

func set_network_id(value: int) -> NetCoreObjectID:
	_network_id = value
	return self

static func find_in(object: Object) -> NetCoreObjectID:
	var founded: NetCoreObjectID = NetGame.get_object_meta(object, META, null)
	if is_instance_valid(founded) and founded.get_owner() == object:
		return founded
	return null

static func get_or_create(object: Object) -> NetCoreObjectID:
	var founded: NetCoreObjectID = find_in(object)
	if founded:
		return founded
	var id: NetCoreObjectID = NetCoreObjectID.new()
	assign(object, id)
	return id

static func assign(object: Object, id: NetCoreObjectID) -> void:
	NetGame.set_object_meta(object, META, id)
	id._owner = weakref(object)
	id._initialize()
