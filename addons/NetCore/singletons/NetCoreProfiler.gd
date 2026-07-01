extends RefCounted
class_name NetCoreProfiler

static var _total_bandwith_bytes: int = 0
static var _total_up_bandwith_bytes: int = 0
static var _total_down_bandwith_bytes: int = 0

static var _frame_raw_packets_max: int = 0

func _ready() -> void:
	Performance.add_custom_monitor("NetCore/Total Bandwith", 
	get_total_bandwith_bytes, [], Performance.MONITOR_TYPE_MEMORY)
	Performance.add_custom_monitor("NetCore/Total Up Bandwith", 
	get_total_up_bandwith_bytes, [], Performance.MONITOR_TYPE_MEMORY)
	Performance.add_custom_monitor("NetCore/Total Down Bandwith", 
	get_total_down_bandwith_bytes, [], Performance.MONITOR_TYPE_MEMORY)
	Performance.add_custom_monitor("NetCore/Frame Raw Packets Max",
	get_frame_raw_packets_max, [], Performance.MONITOR_TYPE_QUANTITY)
	

func _physics_process(delta: float) -> void:
	pass

static func _put_total_up_bandwith(bytes: int) -> void:
	_total_up_bandwith_bytes += bytes
	_total_bandwith_bytes += bytes

static func _put_total_down_bandwith(bytes: int) -> void:
	_total_down_bandwith_bytes += bytes
	_total_bandwith_bytes += bytes

static func get_total_bandwith_bytes() -> int:
	return _total_bandwith_bytes

static func get_total_up_bandwith_bytes() -> int:
	return _total_bandwith_bytes

static func get_total_down_bandwith_bytes() -> int:
	return _total_bandwith_bytes

static func get_frame_raw_packets_max() -> int:
	return _frame_raw_packets_max

static func _test_add(count: int = 1) -> void:
	_frame_raw_packets_max += count
