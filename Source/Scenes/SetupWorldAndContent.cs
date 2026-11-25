using System.Collections.Generic;
using Godot;
using ZCraft.Source.Networking;
using ZCraft.Source.World;

namespace ZCraft.Source.Scenes
{
	public partial class SetupWorldAndContent : CanvasLayer
	{
		public static bool contentLoaded = false;

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
			if (contentLoaded)
				return;
			
			contentLoaded = true;
			DirAccess content = DirAccess.Open(path);
			content.ListDirBegin();
			string fileName = content.GetNext();
			
			List<Content.Content> contents = new();

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
							contents.Add(resource);
						}

						else
						{
							//GD.PushError($"Failed to load Content, script must inherit Content Class. {fullPath}");
						}
					}
					
				}

				fileName = content.GetNext();
			}

			foreach(Content.Content c in contents)
			{
				c.OnRegistryStep();
			}

			foreach(Content.Content c in contents)
			{
				c.OnRegistryModifyStep();
			}

			

		}
	}
}
