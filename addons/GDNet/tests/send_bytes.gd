extends Node

@onready var gd_net_optimized_send: GDNetOptimizedSend = $GDNetOptimizedSend

func _ready() -> void:
	var api := SceneMultiplayer.new()
	get_tree().set_multiplayer(api)
	gd_net_optimized_send.Setup(api)
	gd_net_optimized_send.MultiplayerPeerPacket.connect(
		_on_multiplayer_peer_packet
	)
	
	GDNet.OnNetworkReady.connect(_network_ready)
	GDNet.OnNetworkDisconnected.connect(_network_disconnected)

func _network_ready() -> void:
	$Server.hide()
	$Client.hide()

func _network_disconnected() -> void:
	$Server.show()
	$Client.show()

func _on_multiplayer_peer_packet(peer: int, bytes: PackedByteArray) -> void:
	print(bytes_to_var(bytes))

func _physics_process(delta: float) -> void:
	gd_net_optimized_send.ProcessAll()

func _on_button_pressed() -> void:
	gd_net_optimized_send.MultiplayerSendBytes(var_to_bytes("hello world!"), 1, 2, 0)
	gd_net_optimized_send.MultiplayerSendBytes(var_to_bytes("privet!"), 1, 2, 0)
	gd_net_optimized_send.MultiplayerSendBytes(var_to_bytes("hi!"), 1, 2, 0)
	gd_net_optimized_send.MultiplayerSendBytes(var_to_bytes("halir!"), 1, 2, 0)

func _process(delta: float) -> void:
	for i in 5000:
		gd_net_optimized_send.MultiplayerSendBytes(var_to_bytes([Vector3()]), 0, 2, 0)

func _on_server_pressed() -> void:
	var peer := ENetMultiplayerPeer.new()
	peer.create_server(8080)
	multiplayer.multiplayer_peer = peer

func _on_client_pressed() -> void:
	var peer := ENetMultiplayerPeer.new()
	peer.create_client("localhost", 8080)
	multiplayer.multiplayer_peer = peer
