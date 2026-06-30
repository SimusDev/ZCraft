extends RefCounted
class_name NetGameBuffer

var _base: StreamPeerBuffer = StreamPeerBuffer.new()

enum DataType {
	BOOL_TRUE,
	BOOL_FALSE,
	PACKED_BYTE_ARRAY_EMPTY,
	PACKED_BYTE_ARRAY_1B,
	PACKED_BYTE_ARRAY_DYNAMIC,
	INT_8,
	INT_16,
	INT_32,
	INT_64,
}

func _write_type(type: DataType) -> NetGameBuffer:
	_base.put_u8(type)
	return self

func _read_type() -> DataType:
	return _base.get_u8()

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
	match type:
		DataType.INT_8:
			return _base.get_8()
		DataType.INT_16:
			return _base.get_16()
		DataType.INT_32:
			return _base.get_32()
		DataType.INT_64:
			return _base.get_64()
		_:
			push_error("Invalid data type for read_int: ", type)
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
