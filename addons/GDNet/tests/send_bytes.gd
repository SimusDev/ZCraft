extends Node

@onready var gd_net_optimized_send: GDNetOptimizedSend = $GDNetOptimizedSend

func _ready() -> void:
	var api := SceneMultiplayer.new()
	get_tree().set_multiplayer(api)
	gd_net_optimized_send.Setup(api)
	#gd_net_optimized_send.MultiplayerPeerPacket.connect(
		#_on_multiplayer_peer_packet
	#)
	

func _on_multiplayer_peer_packet(peer: int, bytes: PackedByteArray) -> void:
	return
	print(bytes_to_var(bytes))

func _physics_process(delta: float) -> void:
	gd_net_optimized_send.ProcessAll()

func _on_button_pressed() -> void:
	gd_net_optimized_send.MultiplayerSendBytes(var_to_bytes("hello world!"), 1, 2, 0)
	gd_net_optimized_send.MultiplayerSendBytes(var_to_bytes("privet!"), 1, 2, 0)
	gd_net_optimized_send.MultiplayerSendBytes(var_to_bytes("hi!"), 1, 2, 0)
	gd_net_optimized_send.MultiplayerSendBytes(var_to_bytes("halir!"), 1, 2, 0)

func _process(delta: float) -> void:
	pass

func _on_timer_timeout() -> void:
	for i in 5000:
		gd_net_optimized_send.MultiplayerSendBytes(var_to_bytes([Vector3(), Vector3(), Vector3()]), 0, 2, 0)
