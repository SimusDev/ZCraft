extends Node

func _ready() -> void:
	pass

func _on_button_pressed() -> void:
	pass

func _process(delta: float) -> void:
	for i in 1000:
		_receive_rpc.invoke("hello world!")

var _receive_rpc := NetGameRpc.new(_receive)
func _receive(message: String) -> void:
	pass
