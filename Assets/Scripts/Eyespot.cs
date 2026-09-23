using Unity.Mathematics;
using UnityEngine;

public readonly struct EyeMass
{
    public readonly float drift;
    public readonly float size;
    public readonly float aspect;
    public readonly float tear;
    public readonly float roughnessScale;
    public readonly float seedShift;

    public EyeMass(float drift, float size, float aspect, float tear, float roughnessScale, float seedShift)
    {
        this.drift = drift;
        this.size = size;
        this.aspect = aspect;
        this.tear = tear;
        this.roughnessScale = roughnessScale;
        this.seedShift = seedShift;
    }
}

public sealed class Eyespot
{
    private const float Reach = 1.6f;
    private const float ShapeFrequency = 1.3f;
    private const float ShapeDetailFrequency = 2.6f;
    private const float ShapeDetailShare = 0.4f;
    private const float StreakAcross = 3.5f;
    private const float StreakAlong = 0.7f;
    private const float StreakDepth = 0.08f;
    private const float GlintSize = 0.3f;
    private const float GlintStretch = 1.8f;
    private const float GlintRoughness = 0.3f;
    private const float GlintSoftness = 0.1f;
    private const float GlintSpeckleFrequency = 4.0f;
    private const float GlintStrength = 0.5f;
    private const float GlintOutlineFrequency = 2.0f;
    private const float SpeckleLow = -0.6f;
    private const float SpeckleHigh = 0.6f;
    private const float MinDirectionLength = 1e-6f;

    private static readonly EyeMass Halo = new EyeMass(0.0f, 0.97f, 1.0f, 0.15f, 2.6f, 0.0f);
    private static readonly EyeMass Cream = new EyeMass(0.6f, 0.76f, 1.05f, 0.25f, 1.6f, 3.1f);
    private static readonly EyeMass Iris = new EyeMass(1.5f, 0.5f, 1.25f, 0.35f, 1.4f, 6.7f);
    private static readonly EyeMass Pupil = new EyeMass(2.2f, 0.16f, 1.3f, 0.2f, 0.6f, 9.2f);
    private static readonly Vector2 GlintOffset = new Vector2(-0.3f, 0.3f);
    private static readonly float2 GlintSeedOffset = new float2(3.1f, -8.7f);
    private static readonly float2 StreakSeedOffset = new float2(-5.3f, 12.1f);

    private readonly Vector2 centre;
    private readonly Vector2 outward;
    private readonly Vector2 across;
    private readonly float radius;
    private readonly float stretch;
    private readonly float roughness;
    private readonly float shiftAmount;
    private readonly Vector2 shiftDirection;
    private readonly float2 seed;

    public Eyespot(WingShape shape, WingSettings wing, float centreT, float centreS, float radius, float2 seed)
    {
        this.radius = radius;
        this.seed = seed;
        centre = shape.Point(centreT, centreS);
        outward = WingShape.Direction(shape.Angle(centreT));
        across = new Vector2(outward.y, -outward.x);
        stretch = Mathf.Max(wing.eyeStretch, MinDirectionLength);
        roughness = wing.eyeRoughness;
        shiftAmount = wing.eyeShift;
        shiftDirection = WingShape.Direction(wing.eyeShiftAngle * Mathf.Deg2Rad);
    }

    public Color Paint(Color colour, Vector2 point, float pixel, ButterflySettings palette, out float coverage)
    {
        Vector2 fromCentre = point - centre;
        Vector2 unit = new Vector2(Vector2.Dot(fromCentre, across) / stretch, Vector2.Dot(fromCentre, outward)) / radius;
        coverage = 0.0f;
        if (unit.sqrMagnitude > Reach * Reach)
        {
            return colour;
        }

        float pixelUnits = pixel / radius;
        float streak = noise.snoise(new float2(unit.x * StreakAcross, unit.y * StreakAlong) + seed + StreakSeedOffset);
        float darkPatches = MassMask(Halo, unit, streak, pixelUnits);
        float cream = MassMask(Cream, unit, streak, pixelUnits);
        float iris = MassMask(Iris, unit, streak, pixelUnits);
        float pupil = MassMask(Pupil, unit, streak, pixelUnits);
        coverage = Mathf.Max(darkPatches, Mathf.Max(cream, iris));

        colour = Color.Lerp(colour, palette.eyeOuter, darkPatches);
        colour = Color.Lerp(colour, palette.eyeRing, cream);
        colour = Color.Lerp(colour, palette.eyeIris, iris);
        colour = Color.Lerp(colour, palette.eyeGlint, GlintMask(unit, pixelUnits) * iris * GlintStrength);
        return Color.Lerp(colour, palette.eyePupil, pupil);
    }

    private static Vector2 Direction(Vector2 offset)
    {
        float length = offset.magnitude;
        return length > MinDirectionLength ? offset / length : Vector2.up;
    }

    private float MassMask(EyeMass mass, Vector2 unit, float streak, float pixelUnits)
    {
        Vector2 offset = unit - shiftDirection * (shiftAmount * mass.drift);
        offset.y /= mass.aspect;
        Vector2 direction = Direction(offset);
        float2 around = (float2)direction;
        float shape = noise.snoise(around * ShapeFrequency + seed + mass.seedShift)
                      + ShapeDetailShare * noise.snoise(around * ShapeDetailFrequency + seed - mass.seedShift);
        float tear = mass.tear * Vector2.Dot(direction, -shiftDirection);
        float outline = mass.size * (1.0f + tear + roughness * mass.roughnessScale * shape + StreakDepth * streak);
        return PatternMath.Within(offset.magnitude, outline, pixelUnits);
    }

    private float GlintMask(Vector2 unit, float pixelUnits)
    {
        Vector2 offset = unit - GlintOffset;
        offset.x /= GlintStretch;
        float outline = GlintSize * (1.0f + GlintRoughness * noise.snoise((float2)Direction(offset) * GlintOutlineFrequency + seed + GlintSeedOffset));
        float patch = PatternMath.Within(offset.magnitude, outline, pixelUnits + GlintSoftness);
        float speckle = Mathf.SmoothStep(0.0f, 1.0f, Mathf.InverseLerp(SpeckleLow, SpeckleHigh, noise.snoise((float2)unit * GlintSpeckleFrequency + seed)));
        return patch * speckle;
    }
}
