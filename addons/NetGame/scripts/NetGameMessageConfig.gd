extends NetGameConfigBase
class_name NetGameMessageConfig

@export var send_mode: SendMode = SendMode.ToClients

enum SendMode
{
	ToServer,
	ToClients,
}
