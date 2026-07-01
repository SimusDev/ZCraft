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

enum Type {
	All,
	Async,
	OnServer,
	Target
}
