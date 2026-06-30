extends Node

func _ready() -> void:
	pass

func _on_button_pressed() -> void:
	for i in 20:
		NetGame.send_packet(
			NetGame.PacketType.RpcRequest,
			var_to_bytes("hello_world!")
		)

#func _process(delta: float) -> void:
	#for i in 1000:
		#_receive_rpc.invoke("hello world!")
#
#var _receive_rpc := NetGameRpc.new(_receive)
#func _receive(message: String) -> void:
	#pass
