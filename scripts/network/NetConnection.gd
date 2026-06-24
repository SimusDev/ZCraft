extends Node
class_name NetConnection

signal peer_closed()

var _current_peer:ENetMultiplayerPeer

func get_peer() -> ENetMultiplayerPeer:
	return _current_peer

func create_peer() -> ENetMultiplayerPeer:
	return ENetMultiplayerPeer.new()

func close_peer() -> void:
	_current_peer.close()
	peer_closed.emit()

func create_server(port: int, max_clients:int = 32) -> Error:
	close_peer()
	_current_peer = create_peer()
	var error = get_peer().create_server(port, max_clients)
	
	
	return error

func create_client(address: String, port: int) -> Error:
	close_peer()
	_current_peer = create_peer()
	var error = get_peer().create_client(address, port)
	
	return error
