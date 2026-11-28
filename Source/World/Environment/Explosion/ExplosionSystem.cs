using System;
using System.Numerics;
using Godot;
using ZCraft.AIGenerated;
using ZCraft.Source.Networking;
using ZCraft.Source.Serialization;
using ZCraft.Source.World.Audio;

namespace ZCraft.Source.World.Environment.Explosion;

public class ExplosionSystem : INetworkReplicationObject
{
    public const string ServerExplosionScenePath = "res://Source/World/Environment/Explosion/ServerExplosionScene.tscn";
    public const string ClientExplosionScenePath = "res://Content/Base/Prefabs/Environment/Explosion/ExplosionVfx.tscn";

    private static PackedScene _serverExplosionScene;
    private static PackedScene _clientExplosionScene;

    private static AudioStreamDistantBundle _smallerExplosionSound;
    private static AudioStreamDistantBundle _smallExplosionSound;
    private static AudioStreamDistantBundle _mediumExplosionSound;
    private static AudioStreamDistantBundle _heavyExplosionSound;

    public void Initialize()
    {
        NetworkReplicator.AddObject(this, new NetworkId("EnvironmentExplosionSystemClass"));
        _serverExplosionScene = GD.Load<PackedScene>(ServerExplosionScenePath);
        _clientExplosionScene = GD.Load<PackedScene>(ClientExplosionScenePath);

        _smallerExplosionSound = GD.Load<AudioStreamDistantBundle>("res://Content/Base/Sounds/Environment/Explosion/SmallerBundle.tres");
        _smallExplosionSound = GD.Load<AudioStreamDistantBundle>("res://Content/Base/Sounds/Environment/Explosion/SmallBundle.tres");
        _mediumExplosionSound = GD.Load<AudioStreamDistantBundle>("res://Content/Base/Sounds/Environment/Explosion/MediumBundle.tres");
        _heavyExplosionSound = GD.Load<AudioStreamDistantBundle>("res://Content/Base/Sounds/Environment/Explosion/HeavyBundle.tres");
    }

    public void Explode(Level level, Godot.Vector3 globalPosition, Explosion explosion)
    {
        ServerExplosionScene serverExplosion = _serverExplosionScene.Instantiate<ServerExplosionScene>();
        level.GetParent().AddChild(serverExplosion);
        serverExplosion.Setup(explosion.GetStrength());
        serverExplosion.GlobalPosition = globalPosition;

        BytePacker packer = BytePackerPool.Rent();
        packer.WriteVector3(globalPosition);
        packer.WriteFloat(explosion.GetStrength());

        NetworkReplicator.SendMessageToPeers(level.OnlinePlayerPeers, this, packer);

        BytePackerPool.Return(packer);
    }

    public void OnNetworkMessage(int fromPeer, BytePacker packer)
    {
        Godot.Vector3 globalPosition = packer.ReadVector3();
        float strength = packer.ReadFloat();

        ExplosionVfx clientExplosion = _clientExplosionScene.Instantiate<ExplosionVfx>();
        clientExplosion.ExplosionStrength = 1.5f;
        clientExplosion.FreeOnFinish = true;
        Level.ClientLocalLevel.GetParent().AddChild(clientExplosion);
        clientExplosion.GlobalPosition = globalPosition;
        clientExplosion.Scale = new Godot.Vector3(strength, strength, strength);

        Camera3D camera = Level.ClientLocalLevel.GetViewport().GetCamera3D();
        if (camera == null)
            return;
        
        Godot.Vector3 cameraPos = camera.GlobalPosition;

        var audioPlayer = new AudioStreamPlayer3D();
        clientExplosion.AddChild(audioPlayer);

        if (strength >= 15)
        {
            audioPlayer.Stream = _heavyExplosionSound.QueryStream(cameraPos, globalPosition);
            audioPlayer.Play();
            return;
        }

        if (strength >= 5)
        {
            audioPlayer.Stream = _mediumExplosionSound.QueryStream(cameraPos, globalPosition);
            audioPlayer.Play();
            return;
        }

        if (strength >= 2.5)
        {
            audioPlayer.Stream = _smallExplosionSound.QueryStream(cameraPos, globalPosition);
            audioPlayer.Play();
            return;
        }
        
        if (strength >= 0)
        {
            audioPlayer.Stream = _smallerExplosionSound.QueryStream(cameraPos, globalPosition);
            audioPlayer.Play();
            return;
        }

    }
}
