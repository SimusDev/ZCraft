extends Node

@onready var gd_net_optimized_send: GDNetOptimizedSend = $GDNetOptimizedSend

func _ready() -> void:
	NetGame.setup()

var _my_remote_func_rpc: GDNetRpc = GDNetRpc.Config(_my_remote_func, {})
func _my_remote_func() -> void:
	pass

func _physics_process(delta: float) -> void:
	gd_net_optimized_send.ProcessAll()

func _process(delta: float) -> void:
	return
	for i in 1000:
		gd_net_optimized_send.MultiplayerSendBytes([], 0, 2, 0)
