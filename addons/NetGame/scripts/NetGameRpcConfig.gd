@static_unload
extends NetGameConfigBase
class_name NetGameRpcConfig

@export var permission: Permission = Permission.Authority

enum Permission
{
	Server,
	Authority,
	All,
}

enum SendTo {
	All,
	Server,
	Target
}

func validate(peer: int, object_authority: int = NetGame.SERVER_ID) -> bool:
	match permission:
		Permission.Server:
			return peer == NetGame.SERVER_ID
		Permission.Authority:
			return peer == object_authority
	return false
