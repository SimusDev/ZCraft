extends Node

var test: NetCoreElementBatcher = NetCoreElementBatcher.new()

func _ready() -> void:
	NetGame.on_packet_received.connect(
		_on_packet_received
	)
	
	NetGameRpcRegistry.new([_test_rpc])

func _on_button_pressed() -> void:
	NetGameRpc.invoke(_test_rpc)
	return
	
	for i in 10:
		NetGame.send_packet(NetGame.PacketType.RpcRequest,var_to_bytes("hello_world!"),1)
		NetGame.send_packet(NetGame.PacketType.RpcRequest,var_to_bytes("pidaras!"),1)
		NetGame.send_packet(NetGame.PacketType.RpcRequest,var_to_bytes("sus!"),1)
		NetGame.send_packet(NetGame.PacketType.RpcRequest,var_to_bytes("sas!"),1)



func _on_packet_received(type: NetGame.PacketType, bytes: PackedByteArray, peer: int) -> void:
	print(bytes_to_var(bytes))

func _test_rpc() -> void:
	pass
	#NetGameRpc.invoke(_test_rpc)

func _process(delta: float) -> void:
	for i in 20000:
		NetGameRpc.invoke(_test_rpc)
