#nullable enable

using Godot;
using System;

namespace ZCraft.AIGenerated;

public partial class UpperBodyPoseLayer : SkeletonModifier3D
{
    private const double DefaultBlend = 0.16;
    private static readonly StringName[] ArmBones = ["UpperArmR", "ForearmR", "UpperArmL", "ForearmL"];
    private static readonly NodePath[] ArmTracks = [
        "Skeleton3D:UpperArmR", "Skeleton3D:ForearmR", "Skeleton3D:UpperArmL", "Skeleton3D:ForearmL"
    ];

    [Export] public AnimationLibrary? Clips { get; set; }

    private readonly int[] _boneIndices = new int[ArmBones.Length];
    private readonly Quaternion[] _previousPose = new Quaternion[ArmBones.Length];
    private readonly Quaternion[] _sampledPose = new Quaternion[ArmBones.Length];
    private int _spine = -1;
    private Clip? _holdingPose;
    private Clip? _playing;
    private double _clipTime;
    private double _blendTime;
    private double _blendDuration;
    private float _previousWeight;
    private float _weight;

    private sealed record Clip(Animation Animation, int[] Tracks);

    public override void _Ready()
    {
        Skeleton3D? skeleton = GetSkeleton();
        _spine = skeleton?.FindBone("Spine") ?? -1;
        for (int i = 0; i < ArmBones.Length; i++)
            _boneIndices[i] = skeleton?.FindBone(ArmBones[i]) ?? -1;
    }

    public bool PlayClip(StringName name, double blendSeconds = DefaultBlend)
    {
        if (Clips == null || !Clips.HasAnimation(name))
            return false;

        Animation animation = Clips.GetAnimation(name);
        bool loop = animation.LoopMode == Animation.LoopModeEnum.Linear;
        if (loop && _playing?.Animation == animation)
            return true;

        var tracks = new int[ArmTracks.Length];
        for (int i = 0; i < ArmTracks.Length; i++)
        {
            tracks[i] = animation.FindTrack(ArmTracks[i], Animation.TrackType.Rotation3D);
            if (tracks[i] < 0 || !animation.TrackIsEnabled(tracks[i]) || animation.TrackGetKeyCount(tracks[i]) == 0)
                return false;
        }

        var clip = new Clip(animation, tracks);
        if (loop)
            _holdingPose = clip;

        StartBlend(clip, blendSeconds);
        return true;
    }

    public void Clear(double blendSeconds = DefaultBlend)
    {
        _holdingPose = null;
        if (_playing != null)
            StartBlend(null, blendSeconds);
    }

    public override void _ProcessModificationWithDelta(double delta)
    {
        Skeleton3D? skeleton = GetSkeleton();
        if (skeleton == null)
            return;

        if (_playing is { } clip)
        {
            _clipTime += delta;
            if (clip.Animation.LoopMode == Animation.LoopModeEnum.Linear)
                _clipTime %= clip.Animation.Length;
            else if (_clipTime >= clip.Animation.Length)
                StartBlend(_holdingPose, DefaultBlend);
        }

        _blendTime += delta;
        float fade = _blendDuration <= 0 ? 1 : (float)Math.Clamp(_blendTime / _blendDuration, 0, 1);
        fade = fade * fade * (3 - 2 * fade);
        _weight = Mathf.Lerp(_previousWeight, _playing == null ? 0 : 1, fade);
        if (_playing == null && _weight <= 0)
        {
            Active = false;
            return;
        }

        float upright = _spine >= 0 ? skeleton.GetBoneGlobalPose(_spine).Basis.Y.Normalized().Y : 1;
        float prone = 1 - Mathf.SmoothStep(0.1f, 0.52f, upright);
        // Keep the arm arc in front of the character when its chest is near the floor.
        var shoulderLift = new Quaternion(Vector3.Right, Mathf.DegToRad(104 * prone));
        var forearmLift = new Quaternion(Vector3.Right, Mathf.DegToRad(24 * prone));
        for (int i = 0; i < _boneIndices.Length; i++)
        {
            int bone = _boneIndices[i];
            if (bone < 0)
                continue;

            Quaternion target = _playing != null
                ? _playing.Animation.RotationTrackInterpolate(_playing.Tracks[i], _clipTime).Normalized()
                : _previousPose[i];
            Quaternion pose = _previousWeight > 0 ? _previousPose[i].Slerp(target, fade).Normalized() : target;
            _sampledPose[i] = pose;
            Quaternion locomotion = skeleton.GetBonePoseRotation(bone);
            Quaternion lift = i % 2 == 0 ? shoulderLift : forearmLift;
            skeleton.SetBonePoseRotation(bone, locomotion.Slerp((lift * pose).Normalized(), _weight));
        }
    }

    private void StartBlend(Clip? clip, double seconds)
    {
        Array.Copy(_sampledPose, _previousPose, _sampledPose.Length);
        _previousWeight = _weight;
        _playing = clip;
        _clipTime = 0;
        _blendTime = 0;
        _blendDuration = double.IsFinite(seconds) ? Math.Max(0, seconds) : DefaultBlend;
        Active = true;
    }
}
