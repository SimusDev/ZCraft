using System;
using Godot;
using ZCraft.Source.Networking;
using ZCraft.Source.World;

namespace ZCraft.Source.Scenes;

public partial class Boot : Node
{
    public const int ServerPort = 8080;

    public override void _Ready()
    {
        if (Multiplayer.MultiplayerPeer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Connected)
            Multiplayer.MultiplayerPeer.Close();
        
        if (OS.HasFeature("server"))
        {
            Error error = NetworkManager.Instance.StartServer(ServerPort);

            if (error == Error.Ok)
            {
                GD.Print($"Starting server on {ServerPort} port...");
                Engine.MaxFps = 48;
                Engine.MaxPhysicsStepsPerFrame = 48;
                ZCraftWorldDebug.ServerStartTimeStopWatch.Start();
                GetTree().CallDeferred("change_scene_to_file", "res://Scenes/SetupWorldAndContent.tscn");
                return;
            }

            else
            {
                GD.PrintErr($"Failed to start server on {ServerPort} port. {error}");
            }
        }



        GetTree().CallDeferred("change_scene_to_file", "res://Scenes/MainClient.tscn");
    }
}
