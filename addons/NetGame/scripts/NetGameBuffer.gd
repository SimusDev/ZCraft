extends RefCounted
class_name NetGameBuffer

var _base: StreamPeerBuffer = StreamPeerBuffer.new()

enum DataType {
	NULL,
	VARIABLE,
	BOOL_TRUE,
	BOOL_FALSE,
	PACKED_BYTE_ARRAY_EMPTY,
	PACKED_BYTE_ARRAY_1B,
	PACKED_BYTE_ARRAY_DYNAMIC,
	INT_8,
	INT_16,
	INT_32,
	INT_64,
	STRING,
	ARRAY,
	ARRAY_COMPLEX,
}

var _auto_write_functions: Dictionary[int, Callable] = {
	TYPE_PACKED_BYTE_ARRAY: write_bytes,
	TYPE_NIL: write_null,
	TYPE_BOOL: write_bool,
	TYPE_INT: write_int,
	TYPE_STRING: write_string,
	TYPE_STRING_NAME: write_string,
}
var _auto_read_functions: Dictionary[DataType, Callable] = {
	DataType.PACKED_BYTE_ARRAY_EMPTY: read_bytes,
	DataType.PACKED_BYTE_ARRAY_1B: read_bytes,
	DataType.PACKED_BYTE_ARRAY_DYNAMIC: read_bytes,
	DataType.NULL: read_null,
	DataType.BOOL_TRUE: read_bool,
	DataType.BOOL_FALSE: read_bool,
	DataType.INT_8: read_int,
	DataType.INT_16: read_int,
	DataType.INT_32: read_int,
	DataType.INT_64: read_int,
	DataType.STRING: read_string,
}

func _write_type(type: DataType) -> NetGameBuffer:
	_base.put_u8(type)
	return self

func _read_type() -> DataType:
	var type: DataType = _base.get_u8()
	return type

func get_data() -> PackedByteArray:
	return _base.data_array

func set_data(bytes: PackedByteArray) -> NetGameBuffer:
	_base.data_array = bytes
	return self

func get_size() -> int:
	return _base.get_size()

func get_position() -> int:
	return _base.get_position()

func seek(position: int) -> NetGameBuffer:
	_base.seek(0)
	return self

func clear() -> NetGameBuffer:
	seek(0)
	_base.clear()
	return self

func write(value: Variant) -> NetGameBuffer:
	var typeof: int = typeof(value)
	var callable = _auto_write_functions.get(typeof, null)
	if callable == null:
		write_var(value)
		return self
	callable.call(value)
	return self

func read() -> Variant:
	var type: DataType = _read_type()
	var callable = _auto_read_functions.get(type, null)
	if callable != null:
		return callable.call()
	return null

func write_var(variant: Variant) -> NetGameBuffer:
	_write_type(DataType.VARIABLE)
	_base.put_var(variant)
	return self

func read_var() -> Variant:
	var type: DataType = _read_type()
	if type == DataType.VARIABLE:
		return _base.get_var()
	return null

func write_null() -> NetGameBuffer:
	_write_type(DataType.NULL)
	return self

func read_null() -> Variant:
	_read_type()
	return null

func write_bool(value: bool) -> NetGameBuffer:
	if value:
		_write_type(DataType.BOOL_TRUE)
	else:
		_write_type(DataType.BOOL_FALSE)
	return self

func read_bool() -> bool:
	return _read_type() == DataType.BOOL_TRUE

func write_int(value: int) -> NetGameBuffer:
	if value >= -128 and value <= 127:
		_write_type(DataType.INT_8)
		_base.put_8(value)
	elif value >= -32768 and value <= 32767:
		_write_type(DataType.INT_16)
		_base.put_16(value)
	elif value >= -2147483648 and value <= 2147483647:
		_write_type(DataType.INT_32)
		_base.put_32(value)
	else:
		_write_type(DataType.INT_64)
		_base.put_64(value)
	return self

func read_int() -> int:
	var type: DataType = _read_type()
	if type == DataType.INT_8:
		return _base.get_8()
	elif type == DataType.INT_16:
		return _base.get_16()
	elif type == DataType.INT_32:
		return _base.get_32()
	elif type == DataType.INT_64:
		return _base.get_64()
	return 0

func write_bytes(bytes: PackedByteArray) -> NetGameBuffer:
	if bytes.is_empty():
		return _write_type(DataType.PACKED_BYTE_ARRAY_EMPTY)
	
	if bytes.size() == 1:
		_write_type(DataType.PACKED_BYTE_ARRAY_1B)
		_base.put_data(bytes)
		return self
	
	_write_type(DataType.PACKED_BYTE_ARRAY_DYNAMIC)
	write_int(bytes.size())
	_base.put_data(bytes)
	
	return self

func read_bytes() -> PackedByteArray:
	var type: DataType = _read_type()
	match type:
		DataType.PACKED_BYTE_ARRAY_1B:
			return _base.get_data(1)
		DataType.PACKED_BYTE_ARRAY_DYNAMIC:
			var size: int = read_int()
			return _base.get_data(size)[1]
	
	return PackedByteArray()

func write_string(string: String) -> NetGameBuffer:
	_write_type(DataType.STRING)
	_base.put_string(string)
	return self

func read_string() -> String:
	var type: DataType = _read_type()
	return _base.get_string()

func write_array(array: Array) -> NetGameBuffer:
	_write_type(DataType.ARRAY)
	_base.put_var(array)
	return self

func read_array(array: Array) -> Array:
	var type: DataType = _read_type()
	return _base.get_var() as Array

func write_array_complex(array: Array) -> NetGameBuffer:
	_write_type(DataType.ARRAY_COMPLEX)
	return self

func read_array_complex(array: Array) -> Array:
	return []
