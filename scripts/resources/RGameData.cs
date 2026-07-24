using Godot;

public partial class RGameData : Resource
{
	private const string SavePathEditor = "res://.gamedata";
	private static string SavePathRelease => OS.GetExecutablePath().PathJoin("/.gamedata");

	public static string GetGlobalSavePath()
	{
		if (OS.HasFeature("editor"))
			return SavePathEditor;
		return SavePathRelease;
	}

	private static GDNetRpc _staticRpc;
	static RGameData()
	{
		_staticRpc = new GDNetRpc();
		_staticRpc.SynchronizeNetworkIDByUniqueName("RGameDataStaticRpcs");
	}

}
