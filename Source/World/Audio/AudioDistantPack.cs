using System;
using Godot;

namespace ZCraft.Source.World.Audio;

[GlobalClass]
public partial class AudioDistantPack : Resource
{
    [Export] public Godot.Collections.Array<AudioStream> Streams = new();
    [Export] public float MaxDistance = 0;

    #nullable enable
    public AudioStream? PickRandom() => Streams.PickRandom();
    #nullable disable
}
