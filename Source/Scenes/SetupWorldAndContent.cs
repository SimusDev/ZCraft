using Godot;
using ZCraft.Source.Networking;
using ZCraft.Source.World;

namespace ZCraft.Source.Scenes
{
	public partial class SetupWorldAndContent : CanvasLayer
	{
		public override void _Ready()
		{
			ScanAndSetupContentIn("res://Content");
			
			if (NetworkManager.IsServer)
			{
				ZCraftWorldDebug.ServerStartTimeStopWatch.Start();
				GetTree().CallDeferred("change_scene_to_file", "res://Scenes/ServerWorld.tscn");
			}
			
			else
			{
				GetTree().CallDeferred("change_scene_to_file", "res://Scenes/ClientWorld.tscn");
			}
		}

		public void ScanAndSetupContentIn(string path)
		{
			DirAccess content = DirAccess.Open(path);
			content.ListDirBegin();
			string fileName = content.GetNext();

			while (fileName != string.Empty)
			{
				string fullPath = path.PathJoin(fileName);

				if (content.CurrentIsDir())
				{
					ScanAndSetupContentIn(fullPath);
				}

				else
				{
					if (fileName.EndsWith(".tres"))
					{
						//GD.Print($"Content found: {fullPath}");
						Resource contentResource = GD.Load(fullPath);

						if (contentResource is Content.Content resource)
						{
							string id = path.Replace("res://Content/", "");
							resource.Initialize(path, id);
							resource.Enable();
						}

						else
						{
							GD.PushError($"Failed to load Content, script must inherit Content Class. {fullPath}");
						}
					}
					
				}

				fileName = content.GetNext();
			}
		}
	}
}
