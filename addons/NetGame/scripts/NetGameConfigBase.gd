extends Resource
class_name NetGameConfigBase

@export var transfer_mode: MultiplayerPeer.TransferMode = MultiplayerPeer.TransferMode.TRANSFER_MODE_RELIABLE
@export var channel: int = 0
@export var immediate: bool = false

var buffer: NetGameBuffer = NetGameBuffer.new()

func flag_buffer(buffer: NetGameBuffer) -> NetGameConfigBase:
	self.buffer = buffer
	return self

func flag_channel(channel: int) -> NetGameConfigBase:
	self.channel = channel
	return self

func flag_immediate(immediate: int) -> NetGameConfigBase:
	self.immediate = immediate
	return self

func flag_transfer_mode_reliable() -> NetGameConfigBase:
	transfer_mode = MultiplayerPeer.TransferMode.TRANSFER_MODE_RELIABLE
	return self

func flag_transfer_mode_unreliable() -> NetGameConfigBase:
	transfer_mode = MultiplayerPeer.TransferMode.TRANSFER_MODE_UNRELIABLE
	return self

func flag_transfer_mode_unreliable_ordered() -> NetGameConfigBase:
	transfer_mode = MultiplayerPeer.TransferMode.TRANSFER_MODE_UNRELIABLE_ORDERED
	return self
