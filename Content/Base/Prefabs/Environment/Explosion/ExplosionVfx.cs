#nullable enable

using Godot;
using System;

namespace ZCraft.AIGenerated;

public partial class ExplosionVfx : Node3D
{
    private const int CloudCapacity = 224;
    private const int SparkCapacity = 88;
    private const float GoldenAngle = 2.39996323f;

    private static readonly Color WhiteHot = new Color("ffffd1").SrgbToLinear();
    private static readonly Color Amber = new Color("ffb345").SrgbToLinear();
    private static readonly Color Flame = new Color("e75025").SrgbToLinear();
    private static readonly Color Ember = new Color("9b3e2d").SrgbToLinear();
    private static readonly Color LightAsh = new Color("a58872").SrgbToLinear();
    private static readonly Color LitAsh = new Color("d4aa83").SrgbToLinear();
    private static readonly Color MidAsh = new Color("765442").SrgbToLinear();
    private static readonly Color DarkAsh = new Color("403439").SrgbToLinear();

    private enum Layer { Fire, Smoke, Dust, Stem, Crown, Spark }

    private readonly record struct Particle(
        Layer Layer, Vector3 Origin, Vector3 Destination, Vector3 Extents,
        float Birth, float Life, float Heading, float Turn, float Variation);

    private readonly Particle[] _cloudParticles = new Particle[CloudCapacity];
    private readonly Particle[] _sparkParticles = new Particle[SparkCapacity];
    private readonly bool[] _cloudVisible = new bool[CloudCapacity];
    private int _cloudCount;
    private int _sparkCount;
    private MultiMesh? _clouds;
    private MultiMesh? _sparks;
    private MeshInstance3D _wave = null!;
    private MeshInstance3D _echo = null!;
    private MeshInstance3D _halo = null!;
    private OmniLight3D _light = null!;
    private StandardMaterial3D _wavePaint = null!;
    private StandardMaterial3D _echoPaint = null!;
    private StandardMaterial3D _haloPaint = null!;
    private float _elapsed;
    private float _power;
    private float _column;
    private bool _playWhenReady;

    [Signal] public delegate void FinishedEventHandler();

    [Export(PropertyHint.Range, "0,100,0.1")] public float ExplosionStrength { get; set; } = 12;
    [Export] public bool Autoplay { get; set; } = true;
    [Export] public bool FreeOnFinish { get; set; }
    [Export] public bool LightFlash { get; set; } = true;
    [Export] public int Seed { get; set; }

    [ExportGroup("Mesh Resources")]
    [Export] private Mesh CloudMesh { get; set; } = null!;
    [Export] private Mesh SparkMesh { get; set; } = null!;

    public float Duration { get; private set; }
    public float VisualRadius { get; private set; }
    public float VisualHeight { get; private set; }
    public bool IsPlaying { get; private set; }

    public override void _Ready()
    {
        _clouds = CreateBatch(GetNode<MultiMeshInstance3D>("Clouds"), CloudMesh, CloudCapacity);
        _sparks = CreateBatch(GetNode<MultiMeshInstance3D>("Sparks"), SparkMesh, SparkCapacity);
        _wave = GetNode<MeshInstance3D>("Wave");
        _echo = GetNode<MeshInstance3D>("Echo");
        _halo = GetNode<MeshInstance3D>("Halo");
        _light = GetNode<OmniLight3D>("FlashLight");
        _wavePaint = (StandardMaterial3D)_wave.MaterialOverride;
        _echoPaint = (StandardMaterial3D)_echo.MaterialOverride;
        _haloPaint = (StandardMaterial3D)_halo.MaterialOverride;

        if (Autoplay || _playWhenReady)
            Play(ExplosionStrength);
        else
            Stop();
    }

    public void Play(float explosionStrength)
    {
        ExplosionStrength = float.IsFinite(explosionStrength) ? Mathf.Clamp(explosionStrength, 0, 100) : 0;
        if (ExplosionStrength <= 0)
        {
            Stop();
            return;
        }

        if (_clouds == null || _sparks == null)
        {
            _playWhenReady = true;
            return;
        }

        _playWhenReady = false;
        _power = Mathf.Log(1 + ExplosionStrength) / Mathf.Log(101);
        _column = Mathf.SmoothStep(.62f, .96f, _power);
        VisualRadius = .42f + 4.83f * Mathf.Pow(_power, 1.38f);
        VisualHeight = VisualRadius * Mathf.Lerp(1.58f, 3.72f, _column);
        Duration = 1.36f + 5.54f * Mathf.Pow(_power, 1.20f);
        _elapsed = 0;
        var random = new RandomNumberGenerator();
        if (Seed != 0)
            random.Seed = unchecked((ulong)Seed);
        else
            random.Randomize();
        Populate(random);

        float bound = VisualRadius * 4.2f;
        var aabb = new Aabb(new Vector3(-bound, -.45f * VisualRadius, -bound),
            new Vector3(bound * 2, VisualHeight + VisualRadius, bound * 2));
        _clouds.CustomAabb = aabb;
        _sparks.CustomAabb = aabb;
        ResetCloudSlots();
        _light.OmniRange = VisualRadius * 4.2f;
        IsPlaying = true;
        SetProcess(true);
        Draw(0);
    }

    public void Stop()
    {
        _playWhenReady = false;
        IsPlaying = false;
        SetProcess(false);
        if (_clouds == null || _sparks == null)
            return;

        _clouds.VisibleInstanceCount = 0;
        _sparks.VisibleInstanceCount = 0;
        _wave.Hide();
        _echo.Hide();
        _halo.Hide();
        _light.Hide();
    }

    public override void _Process(double delta)
    {
        if (!IsPlaying)
            return;

        _elapsed += (float)delta;
        if (_elapsed < Duration)
        {
            Draw(_elapsed / Duration);
            return;
        }

        Stop();
        EmitSignal(SignalName.Finished);
        if (FreeOnFinish && !IsPlaying)
            QueueFree();
    }

    private static MultiMesh CreateBatch(MultiMeshInstance3D node, Mesh mesh, int capacity)
    {
        var batch = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            UseColors = true,
            Mesh = mesh,
            InstanceCount = capacity,
            VisibleInstanceCount = 0
        };
        node.Multimesh = batch;
        return batch;
    }

    private void Populate(RandomNumberGenerator random)
    {
        _cloudCount = 0;
        _sparkCount = 0;

        int fireCount = Count(19, 58, _power);
        for (int i = 0; i < fireCount; i++)
        {
            float spread = random.RandfRange(.09f, 1.04f);
            float y = random.RandfRange(.15f, 1.27f) * Mathf.Lerp(1, .78f, spread);
            Vector3 end = Radial(i, spread, y, random);
            float size = random.RandfRange(.22f, .62f) * Mathf.Lerp(1.18f, .77f, spread);
            AddCloud(Layer.Fire, new Vector3(0, .18f, 0), end, size, random.RandfRange(0, .034f),
                random.RandfRange(.25f, .45f), random);
        }

        int smokeCount = Count(13, 33, _power);
        for (int i = 0; i < smokeCount; i++)
        {
            float y = random.RandfRange(.38f, 1.39f);
            Vector3 end = Radial(i, random.RandfRange(.37f, 1.35f), y, random);
            AddCloud(Layer.Smoke, end * .34f + Vector3.Up * .04f, end,
                random.RandfRange(.34f, .79f), random.RandfRange(.125f, .37f), random.RandfRange(.46f, .62f), random);
        }

        for (int i = 0; i < Count(5, 25, _power); i++)
        {
            Vector3 end = Radial(i, random.RandfRange(1.11f, 2.55f), random.RandfRange(.075f, .21f), random);
            AddCloud(Layer.Dust, end * .21f + Vector3.Up * .045f, end, random.RandfRange(.20f, .54f),
                random.RandfRange(.073f, .26f), random.RandfRange(.33f, .56f), random);
        }

        if (_column > .055f)
            AddColumn(random);

        int sparks = Count(16, SparkCapacity, _power);
        for (int i = 0; i < sparks; i++)
        {
            Vector3 end = Radial(i, random.RandfRange(.55f, 2.80f), random.RandfRange(.16f, 1.19f), random);
            float size = random.RandfRange(.022f, .065f);
            _sparkParticles[_sparkCount++] = new Particle(Layer.Spark, Vector3.Up * .27f, end,
                new Vector3(size * random.RandfRange(2.0f, 4.8f), size, size),
                random.RandfRange(0, .065f), random.RandfRange(.22f, .57f), 0, 0, random.Randf());
        }
    }

    private void AddColumn(RandomNumberGenerator random)
    {
        int stemCount = Count(5, 36, _column);
        for (int i = 0; i < stemCount; i++)
        {
            float f = (i + .4f) / stemCount;
            float y = Mathf.Lerp(.33f, 2.72f, f) * Mathf.Lerp(.74f, 1, _column);
            float width = Mathf.Lerp(.51f, .24f, Mathf.Sin(f * Mathf.Pi * .86f));
            Vector3 end = Radial(i, width * random.RandfRange(.2f, 1), y, random);
            end.X += .14f * f;
            Vector3 start = new Vector3(end.X * .37f, .22f + f * .48f, end.Z * .34f);
            AddCloud(Layer.Stem, start, end, random.RandfRange(.38f, .63f) * Mathf.Lerp(.7f, 1, _column),
                .11f + .18f * f + random.RandfRange(.0f, .038f), random.RandfRange(.56f, .69f), random);
        }

        int capCount = Count(8, 65, _column);
        for (int i = 0; i < capCount; i++)
        {
            float f = (i + .35f) / capCount;
            float reach = Mathf.Sqrt(f);
            float lower = i % 4 == 0 ? .36f : 0;
            float y = 2.49f + .69f * Mathf.Sqrt(1 - f) - lower + random.RandfRange(-.08f, .14f);
            Vector3 end = Radial(i, reach * 1.38f * Mathf.Lerp(.70f, 1.0f, _column),
                y * Mathf.Lerp(.73f, 1, _column), random);
            end.X += .15f;
            Vector3 start = new Vector3(end.X * .20f, .80f, end.Z * .23f);
            AddCloud(Layer.Crown, start, end, random.RandfRange(.26f, .69f) * Mathf.Lerp(.68f, 1, _column),
                random.RandfRange(.19f, .34f), random.RandfRange(.55f, .68f), random);
        }
    }

    private void AddCloud(Layer layer, Vector3 start, Vector3 end, float size, float birth, float life, RandomNumberGenerator random)
    {
        var extents = new Vector3(size * random.RandfRange(.80f, 1.21f), size * random.RandfRange(.75f, 1.18f), size);
        if (layer == Layer.Dust)
            extents.Y *= .65f;
        else if (layer == Layer.Stem)
            extents *= new Vector3(.87f, 1.33f, .87f);
        _cloudParticles[_cloudCount++] = new Particle(layer, start, end, extents, birth, life,
            random.RandfRange(-Mathf.Pi, Mathf.Pi), random.RandfRange(-.49f, .49f), random.Randf());
    }

    private static Vector3 Radial(int index, float radius, float y, RandomNumberGenerator random)
    {
        float angle = index * GoldenAngle + random.RandfRange(-.22f, .22f);
        return new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius);
    }

    private void Draw(float time)
    {
        DrawClouds(time);
        DrawSparks(time);
        DrawWave(_wave, _wavePaint, time, .014f, 1.0f);
        DrawWave(_echo, _echoPaint, time, .105f, .49f);

        float flash = 1 - Mathf.SmoothStep(.018f, .23f, time);
        _halo.Visible = flash > .003f;
        if (_halo.Visible)
        {
            float size = VisualRadius * Mathf.Lerp(.66f, 3.02f, EaseOut(time / .22f));
            _halo.Scale = Vector3.One * size;
            _halo.Position = Vector3.Up * (VisualRadius * (.25f + time * 1.65f));
            _haloPaint.AlbedoColor = new Color(1.0f, .63f, .27f, flash * .68f);
        }

        _light.Visible = LightFlash && flash > .014f;
        _light.LightEnergy = flash * Mathf.Lerp(1.4f, 5.5f, _power);
        _light.Position = Vector3.Up * VisualRadius * .49f;
    }

    private void ResetCloudSlots()
    {
        for (int i = 0; i < _cloudCount; i++)
        {
            _cloudVisible[i] = false;
            _clouds!.SetInstanceColor(i, Colors.Transparent);
        }

        _clouds!.VisibleInstanceCount = _cloudCount;
    }

    private void DrawClouds(float time)
    {
        for (int i = 0; i < _cloudCount; i++)
        {
            Particle p = _cloudParticles[i];
            float age = (time - p.Birth) / p.Life;
            if (age < 0 || age >= 1)
            {
                if (_cloudVisible[i])
                {
                    _clouds!.SetInstanceColor(i, Colors.Transparent);
                    _cloudVisible[i] = false;
                }

                continue;
            }

            bool fire = p.Layer == Layer.Fire;
            float path = EaseOut(age / (fire ? .70f : .78f));
            Vector3 position = p.Origin.Lerp(p.Destination, path);
            position.Y += !fire ? age * p.Variation * .13f : Mathf.Sin(age * Mathf.Pi) * .21f;
            float growth = EaseOut(age / (fire ? .34f : .60f));
            float fadeIn = fire ? 1 : Mathf.SmoothStep(0, .11f, age);
            float opacity = fadeIn * Fade(age, fire ? .48f : .72f);
            float scale = fire ? Mathf.Lerp(.68f, 1f, opacity) : Mathf.Lerp(.55f, 1f, fadeIn);
            Vector3 size = p.Extents * (VisualRadius * (.19f + growth * .96f) * scale);
            var rotation = Basis.FromEuler(new Vector3(.19f * (p.Variation - .5f), p.Heading + p.Turn * age,
                .18f * p.Turn));
            Color tint = fire ? FireColor(age, p.Variation) : SmokeColor(p, age, time);
            // Keep the same instance index through birth, drift, and fade.
            _clouds!.SetInstanceTransform(i, new Transform3D(rotation.Scaled(size), position * VisualRadius));
            _clouds.SetInstanceColor(i, WithAlpha(tint, opacity));
            _cloudVisible[i] = true;
        }
    }

    private void DrawSparks(float time)
    {
        int visible = 0;
        for (int i = 0; i < _sparkCount; i++)
        {
            Particle p = _sparkParticles[i];
            float age = (time - p.Birth) / p.Life;
            if (age < 0 || age >= 1)
                continue;

            Vector3 position = p.Origin.Lerp(p.Destination, EaseOut(age * .87f));
            position.Y += Mathf.Sin(age * Mathf.Pi) * (.32f + p.Variation * .52f);
            position.Y -= .43f * age * age;
            if (position.Y < .032f)
                continue;

            Vector3 axis = p.Destination.Normalized();
            Vector3 side = axis.Cross(Vector3.Up).Normalized();
            var basis = new Basis(axis, side.Cross(axis), side).Scaled(p.Extents * VisualRadius * Mathf.Lerp(1, .55f, age));
            Color tint = Gain(Amber.Lerp(Flame, age), Mathf.Lerp(3.2f, 1.0f, age));
            _sparks!.SetInstanceTransform(visible, new Transform3D(basis, position * VisualRadius));
            _sparks.SetInstanceColor(visible, WithAlpha(tint, Fade(age, .37f)));
            visible++;
        }

        _sparks!.VisibleInstanceCount = visible;
    }

    private void DrawWave(MeshInstance3D ring, StandardMaterial3D paint, float time, float birth, float weight)
    {
        float age = (time - birth) / Mathf.Lerp(.32f, .53f, _power);
        float strength = Mathf.SmoothStep(.15f, .44f, _power);
        ring.Visible = age is >= 0 and < 1 && strength > .01f;
        if (!ring.Visible)
            return;

        float span = VisualRadius * weight * Mathf.Lerp(.43f, 3.35f, EaseOut(Mathf.Sqrt(age)));
        ring.Scale = new Vector3(span, 1, span);
        ring.Position = Vector3.Up * (.019f + .012f * weight);
        paint.AlbedoColor = new Color(1, Mathf.Lerp(.71f, .32f, age), .22f, Fade(age, .13f) * weight * strength * .78f);
    }

    private static Color FireColor(float age, float variation)
    {
        Color core = WhiteHot.Lerp(Amber, Mathf.SmoothStep(.04f, .56f, age + variation * .26f));
        Color edge = core.Lerp(Flame, Mathf.SmoothStep(.14f, .72f, age + variation * .22f));
        Color soot = edge.Lerp(Ember, Mathf.SmoothStep(.46f, .92f, age));
        return Gain(soot, 1 + 2.5f * (1 - Mathf.SmoothStep(.02f, .31f, age)));
    }

    private static Color SmokeColor(Particle p, float age, float time)
    {
        Color core = DarkAsh.Lerp(MidAsh, p.Variation * .88f);
        float heat = (1 - Mathf.SmoothStep(.31f, .81f, time)) * (.58f + p.Variation * .42f);
        float crownLight = Mathf.SmoothStep(2.45f, 3.18f, p.Destination.Y) * (.46f + .54f * heat);
        return p.Layer switch
        {
            Layer.Crown => core.Lerp(LitAsh, crownLight).Lerp(Amber, (1 - Mathf.SmoothStep(0, .41f, age)) * .52f),
            Layer.Stem => core.Lerp(Flame, heat * (1 - age) * .63f),
            Layer.Dust => core.Lerp(LightAsh, heat * .42f + .08f),
            _ => core.Lerp(LightAsh, heat * .65f).Lerp(Flame, (1 - Mathf.SmoothStep(0, .35f, age)) * .30f)
        };
    }

    private static int Count(int min, int max, float blend) => Mathf.RoundToInt(Mathf.Lerp(min, max, blend));
    private static float Fade(float age, float begin) => 1 - Mathf.SmoothStep(begin, 1, age);
    private static float EaseOut(float progress)
    {
        float t = 1 - Mathf.Clamp(progress, 0, 1);
        return 1 - t * t * t;
    }

    private static Color Gain(Color color, float gain) => new(color.R * gain, color.G * gain, color.B * gain, color.A);
    private static Color WithAlpha(Color color, float alpha) => new(color.R, color.G, color.B, alpha);
}
