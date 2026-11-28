using System;
using System.Collections.Generic;
using Godot;

namespace ZCraft.Source.World.Audio;

[GlobalClass]
public partial class AudioStreamDistantBundle : Resource
{
    [Export] private Godot.Collections.Array<AudioDistantPack> _packs = new();

    #nullable enable
    public AudioStream? QueryStream(Vector3 cameraPosition, Vector3 audioSourcePosition)
    {
        float distanceSq = cameraPosition.DistanceSquaredTo(audioSourcePosition);

        for (int i = 0; i < _packs.Count; i++)
        {
            AudioDistantPack pack = _packs[i];

            if (pack == null) continue;

            if (distanceSq <= pack.MaxDistance * pack.MaxDistance)
                return pack.PickRandom();
        }

        return null;
    }
    #nullable disable


}
