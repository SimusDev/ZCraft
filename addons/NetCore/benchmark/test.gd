extends Node

@export var PORT: int = 8080

@onready var _send_every_frame: CheckBox = %SendEveryFrame
@onready var _packet_string_line: LineEdit = %PacketStringLine
@onready var _h_slider_packet_count: HSlider = $ConnectedScreen/VBoxContainer/HSliderPacketCount

func _ready() -> void:
	NetCore.setup()
	
	NetCore.compression_enabled = %CompressionEnabled.button_pressed
	#NetCore.batching_enabled = $%BatchingEnabled.button_pressed
	
	$MainScreen.show()
	$ConnectedScreen.hide()
	
	multiplayer.connected_to_server.connect(_on_connected_to_server)
	multiplayer.server_disconnected.connect(_on_server_disconnected)
	
	$Timers.process_mode = Node.PROCESS_MODE_DISABLED
	set_process(false)
	$ConnectedScreen/VBoxContainer/HSlider.value = %SendIntervalTimer.wait_time
	_on_h_slider_value_changed(%SendIntervalTimer.wait_time)
	
	%PacketStringLabel.text = "Packet String var_to_bytes: (%s raw bytes)" % [var_to_bytes(_packet_string_line.text).size()]
	$%PacketCountLabel.text = "Packet Count: %s" % [int(%HSliderPacketCount.value)]
	
	%SendIntervalTimer.start()

func _on_connected_to_server() -> void:
	$MainScreen.hide()
	$ConnectedScreen.show()
	$Timers.process_mode = Node.PROCESS_MODE_INHERIT
	set_process(true)

func _on_server_disconnected() -> void:
	$MainScreen.show()
	$ConnectedScreen.hide()
	$Timers.process_mode = Node.PROCESS_MODE_DISABLED
	set_process(false)

func _on_compression_enabled_toggled(toggled_on: bool) -> void:
	NetCore.compression_enabled = %CompressionEnabled.button_pressed

func _on_batching_enabled_toggled(toggled_on: bool) -> void:
	NetCore.batching_enabled = $%BatchingEnabled.button_pressed

func _on_server_pressed() -> void:
	var enet_peer: ENetMultiplayerPeer = ENetMultiplayerPeer.new()
	var err: Error = enet_peer.create_server(PORT)
	if err == OK:
		_on_connected_to_server()
	multiplayer.multiplayer_peer = enet_peer

func _on_client_pressed() -> void:
	var enet_peer: ENetMultiplayerPeer = ENetMultiplayerPeer.new()
	var err: Error = enet_peer.create_client("localhost", PORT)
	if err == OK:
		multiplayer.multiplayer_peer = enet_peer

func _on_send_interval_timer_timeout() -> void:
	if _send_every_frame.button_pressed:
		return
	
	_send_string_packet()

func _on_h_slider_value_changed(value: float) -> void:
	%SendIntervalTimer.wait_time = value
	$%SendIntervalLabel.text = "Send Interval: %s/s" % [%SendIntervalTimer.wait_time]

func _on_close_connection_pressed() -> void:
	multiplayer.multiplayer_peer.close()

func _send_string_packet() -> void:
	for i in _h_slider_packet_count.value:
		var bytes: PackedByteArray = var_to_bytes(_packet_string_line.text)
		NetCore.multiplayer_send_bytes(
			bytes
		)

func _process(delta: float) -> void:
	if !_send_every_frame.button_pressed:
		return
	
	_send_string_packet()

func _on_packet_string_line_text_changed(new_text: String) -> void:
	%PacketStringLabel.text = "Packet String var_to_bytes: (%s raw bytes)" % [var_to_bytes(_packet_string_line.text).size()]

func _on_h_slider_packet_count_value_changed(value: float) -> void:
	$%PacketCountLabel.text = "Packet Count: %s" % [int(%HSliderPacketCount.value)]
