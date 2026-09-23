using Unity.Mathematics;
using UnityEngine;

public sealed class Eyespot
{
    private const int LayerCount = 4;
    private const float EllipseDepth = 0.07f;
    private const float LayerNoiseFrequency = 1.6f;
    private const float LayerSeedStep = 7.3f;
    private const float LayerPhaseStep = 1.3f;
    private const float Reach = 1.45f;
    private const float FibreDepth = 0.06f;
    private const float FibreFrequency = 9.0f;
    private const float FibreRun = 1.2f;
    private const float GlintSize = 0.3f;
    private const float GlintStretch = 1.8f;
    private const float GlintRoughness = 0.3f;
    private const float GlintSoftness = 0.1f;
    private const float GlintSpeckleFrequency = 4.0f;
    private const float GlintStrength = 0.7f;
    private const float GlintOutlineFrequency = 2.0f;
    private const float SpeckleLow = -0.6f;
    private const float SpeckleHigh = 0.6f;
    private const float MinDirectionLength = 1e-6f;

    private static readonly float[] LayerShares = { 1.0f, 0.8f, 0.56f, 0.3f };
    private static readonly Vector2 GlintOffset = new Vector2(-0.3f, 0.3f);
    private static readonly float2 GlintSeedOffset = new float2(3.1f, -8.7f);

    private readonly Vector2 centre;
    private readonly Vector2 outward;
    private readonly Vector2 across;
    private readonly float radius;
    private readonly float stretch;
    private readonly float roughness;
    private readonly Vector2 shift;
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
        float shiftAngle = wing.eyeShiftAngle * Mathf.Deg2Rad;
        shift = new Vector2(Mathf.Sin(shiftAngle), Mathf.Cos(shiftAngle)) * wing.eyeShift;
    }

    public Color Paint(Color colour, Vector2 point, float pixel, ButterflySettings palette, out float coverage)
    {
        Vector2 fromCentre = point - centre;
        Vector2 local = new Vector2(Vector2.Dot(fromCentre, across) / stretch, Vector2.Dot(fromCentre, outward));
        float reach = radius * Reach + pixel;
        coverage = 0.0f;
        if (local.sqrMagnitude > reach * reach)
        {
            return colour;
        }

        Color[] layerColours = { palette.eyeOuter, palette.eyeRing, palette.eyeIris, palette.eyePupil };
        Color rings = layerColours[0];
        float irisMask = 0.0f;
        for (int layer = 0; layer < LayerCount; layer++)
        {
            Vector2 offset = local - shift * (radius * layer / (LayerCount - 1));
            float mask = PatternMath.Within(offset.magnitude, LayerRadius(offset, layer), pixel);
            if (layer == 0)
            {
                coverage = mask;
                continue;
            }

            rings = Color.Lerp(rings, layerColours[layer], mask);
            irisMask = layer == 2 ? mask : irisMask;
        }

        rings = PatternMath.Shade(rings, Fibres(local));
        rings = Color.Lerp(rings, palette.eyeGlint, GlintMask(local, pixel) * irisMask * GlintStrength);
        return Color.Lerp(colour, rings, coverage);
    }

    private static Vector2 Direction(Vector2 offset)
    {
        float length = offset.magnitude;
        return length > MinDirectionLength ? offset / length : Vector2.up;
    }

    private float LayerRadius(Vector2 offset, int layer)
    {
        Vector2 direction = Direction(offset);
        float cosDouble = direction.y * direction.y - direction.x * direction.x;
        float sinDouble = 2.0f * direction.x * direction.y;
        float phase = seed.x + layer * LayerPhaseStep;
        float ellipse = EllipseDepth * (cosDouble * Mathf.Cos(phase) - sinDouble * Mathf.Sin(phase));
        float wobble = roughness * noise.snoise((float2)direction * LayerNoiseFrequency + seed + layer * LayerSeedStep);
        return LayerShares[layer] * radius * (1.0f + ellipse + wobble);
    }

    private float Fibres(Vector2 local)
    {
        float2 around = (float2)Direction(local) * FibreFrequency;
        return 1.0f + FibreDepth * noise.snoise(around + local.magnitude / radius * FibreRun + seed);
    }

    private float GlintMask(Vector2 local, float pixel)
    {
        Vector2 offset = local - GlintOffset * radius;
        offset.x /= GlintStretch;
        float outline = GlintSize * radius * (1.0f + GlintRoughness * noise.snoise((float2)Direction(offset) * GlintOutlineFrequency + seed + GlintSeedOffset));
        float patch = PatternMath.Within(offset.magnitude, outline, pixel + GlintSoftness * radius);
        float speckle = Mathf.SmoothStep(0.0f, 1.0f, Mathf.InverseLerp(SpeckleLow, SpeckleHigh, noise.snoise((float2)(local / radius) * GlintSpeckleFrequency + seed)));
        return patch * speckle;
    }
}
