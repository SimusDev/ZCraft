using System;
using System.Collections.Generic;
using Godot;
using ZCraft.Source.Content;
using ZCraft.Source.Networking;
using ZCraft.Source.World;

namespace ZCraft.Source.Scenes;

public partial class Boot : Node
{
    public const int ServerPort = 8080;
	public static bool contentLoaded = false;

    public override void _Ready()
    {
        if (Multiplayer.MultiplayerPeer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Connected)
            Multiplayer.MultiplayerPeer.Close();
        
        if (!contentLoaded)
            {
                ScanAndSetupContentIn("res://Content");

                World.Environment.Environment.Initialize();

                #if DEBUG
                Registry.PrintDebugInfo();
                #endif
            }

        if (OS.HasFeature("server"))
        {
            Error error = NetworkManager.Instance.StartServer(ServerPort);

            if (error == Error.Ok)
            {
                GD.Print($"Starting server on {ServerPort} port...");
                Engine.MaxFps = 48;
                Engine.MaxPhysicsStepsPerFrame = 48;
                ZCraftWorldDebug.ServerStartTimeStopWatch.Start();
                GetTree().CallDeferred("change_scene_to_file", "res://Scenes/ServerWorld.tscn");
                return;
            }

            else
            {
                GD.PrintErr($"Failed to start server on {ServerPort} port. {error}");
            }
        }

        GetTree().CallDeferred("change_scene_to_file", "res://Scenes/MainClient.tscn");
    }


    public void ScanAndSetupContentIn(string path)
    {
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

        Content.Content.All = contents;

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
