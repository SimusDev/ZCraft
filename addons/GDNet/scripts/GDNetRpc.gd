@static_unload
extends RefCounted
class_name GDNetRpc

const SHARP_SCRIPT_PATH: CSharpScript = preload("res://addons/GDNet/scripts/GDNetRpc.cs")

var _base: RefCounted

const CONFIG_EXAMPLE: Dictionary[String, Variant] = {
	"permission": "authority", #authority; any_peer; server
	"mode": MultiplayerPeer.TransferMode.TRANSFER_MODE_RELIABLE,
	"channel": 0,
}

func get_remote_sender_id() -> int:
	return _base.RemoteSenderID

static func config(callable: Callable, cfg: Dictionary[String, Variant] = {}) -> GDNetRpc:
	var _rpc: GDNetRpc = GDNetRpc.new()
	_rpc._initialize(callable, cfg)
	return _rpc

func _initialize(callable: Callable, cfg: Dictionary[String, Variant]) -> void:
	_base = SHARP_SCRIPT_PATH.Config(callable, cfg)

func invoke(...args: Array) -> void:
	_base._GDInvoke(args)

func invoke_for(id: int, ...args: Array) -> void:
	_base._GDInvokeFor(args)
