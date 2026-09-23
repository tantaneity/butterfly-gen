using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public sealed class WingPattern
{
    private const float BasalSoftness = 0.025f;
    private const float PupilShare = 0.22f;
    private const float IrisShare = 0.5f;
    private const float RingShare = 0.72f;
    private const float ApexFalloff = 0.3f;
    private const float VeinBaseThickening = 0.8f;
    private const float StripeFade = 0.04f;
    private const float VeinRootReach = 0.08f;

    private const float PatternWarpAmount = 0.011f;
    private const float PatternWarpFrequency = 3.0f;
    private const float VeinWarpAmount = 0.004f;
    private const float VeinWarpFrequency = 2.0f;
    private const float DetailWarpShare = 0.3f;
    private const float DetailWarpFrequency = 2.7f;
    private const float ToneDepth = 0.09f;
    private const float ToneFrequency = 2.2f;
    private const float GrainDepth = 0.1f;
    private const float SpotJitter = 0.35f;
    private const float SpotJitterStep = 1.7f;
    private const float GrainAcross = 700.0f;
    private const float GrainAlong = 45.0f;
    private const float VeinShadowWidth = 5.0f;
    private const float VeinShadowDepth = 0.14f;
    private const float EdgeShadowStart = 0.72f;
    private const float EdgeShadowDepth = 0.12f;
    private const float SeedSpread = 53.0f;

    private static readonly float2 SecondAxis = new float2(19.7f, -7.3f);
    private static readonly float2 GrainOffset = new float2(-41.1f, 13.9f);

    private readonly struct Eye
    {
        public readonly Vector2 centre;
        public readonly float radius;

        public Eye(Vector2 centre, float radius)
        {
            this.centre = centre;
            this.radius = radius;
        }
    }

    private readonly WingShape shape;
    private readonly WingSettings wing;
    private readonly ButterflySettings palette;
    private readonly float[] veinStarts;
    private readonly float[] veinEnds;
    private readonly Vector2[] marginSpots;
    private readonly float[] marginSpotRadii;
    private readonly Eye[] eyes;
    private readonly float2 seedOffset;

    public WingPattern(WingShape shape, WingSettings wing, ButterflySettings palette, float seed)
    {
        this.shape = shape;
        this.wing = wing;
        this.palette = palette;
        seedOffset = new float2(seed * SeedSpread, seed * SeedSpread * 0.61f);

        veinStarts = new float[shape.CellCount + 1];
        veinEnds = new float[shape.CellCount + 1];
        for (int vein = 0; vein <= shape.CellCount; vein++)
        {
            veinStarts[vein] = shape.VeinT(vein, 0.0f);
            veinEnds[vein] = shape.VeinT(vein, 1.0f);
        }

        float marginSpotRadius = wing.marginSpotSize * shape.Length;
        marginSpots = marginSpotRadius > 0.0f && wing.marginWidth > 0.0f ? CellCentres(1.0f - wing.marginWidth * 0.5f) : new Vector2[0];
        marginSpotRadii = new float[marginSpots.Length];
        for (int spot = 0; spot < marginSpots.Length; spot++)
        {
            float jitter = noise.snoise(new float2(spot * SpotJitterStep, 0.0f) + seedOffset);
            marginSpotRadii[spot] = marginSpotRadius * (1.0f + SpotJitter * jitter);
        }
        eyes = PlaceEyes();
    }

    public Color ColourAt(Vector2 point, float pixel)
    {
        Vector2 warped = point + Warp(point, PatternWarpAmount, PatternWarpFrequency);
        shape.ToPolar(warped, out float t, out float s, out float radius);
        shape.ToPolar(point + Warp(point, VeinWarpAmount, VeinWarpFrequency), out float veinT, out float veinS, out float veinRadius);

        Color colour = Layers(warped, t, s, radius, pixel);
        colour = Color.Lerp(colour, palette.vein, VeinMask(veinT, veinS, veinRadius, pixel, 1.0f));
        return Weather(colour, point, veinT, veinS, veinRadius, pixel);
    }

    private Vector2 Warp(Vector2 point, float amount, float frequency)
    {
        float2 coarse = (float2)point * (frequency / shape.Length) + seedOffset;
        float2 fine = coarse * DetailWarpFrequency;
        float x = noise.snoise(coarse) + DetailWarpShare * noise.snoise(fine);
        float y = noise.snoise(coarse + SecondAxis) + DetailWarpShare * noise.snoise(fine + SecondAxis);
        return new Vector2(x, y) * (amount * shape.Length);
    }

    private Color Layers(Vector2 point, float t, float s, float radius, float pixel)
    {
        float pixelS = pixel / radius;

        Color colour = Color.Lerp(palette.ground, palette.basal, 1.0f - Mathf.SmoothStep(0.0f, 1.0f,
            Mathf.InverseLerp(wing.basalReach - BasalSoftness, wing.basalReach + BasalSoftness, s)));

        float bandCentre = wing.bandCenter + wing.bandTilt * (t - 0.5f);
        colour = Color.Lerp(colour, palette.band, Within(Mathf.Abs(s - bandCentre), wing.bandWidth * 0.5f, pixelS));
        colour = Color.Lerp(colour, palette.stripe, StripeMask(t, s, pixelS));

        float apex = wing.apexPatch * Mathf.Exp(-(t / ApexFalloff) * (t / ApexFalloff));
        float marginWidth = wing.marginWidth + apex;
        colour = Color.Lerp(colour, palette.margin, marginWidth > 0.0f ? Within(1.0f - s, marginWidth, pixelS) : 0.0f);
        colour = Color.Lerp(colour, palette.marginSpot, MarginSpotMask(point, pixel));
        return PaintEyes(colour, point, pixel);
    }

    private Color Weather(Color colour, Vector2 point, float t, float s, float radius, float pixel)
    {
        float2 toneAt = (float2)point * (ToneFrequency / shape.Length) + seedOffset + GrainOffset;
        float tone = 1.0f + ToneDepth * noise.snoise(toneAt);
        float grain = 1.0f + GrainDepth * noise.snoise(new float2(t * GrainAcross, s * GrainAlong) + seedOffset);
        float veinShadow = 1.0f - VeinShadowDepth * VeinMask(t, s, radius, pixel, VeinShadowWidth);
        float edgeShadow = 1.0f - EdgeShadowDepth * Mathf.SmoothStep(0.0f, 1.0f, Mathf.InverseLerp(EdgeShadowStart, 1.0f, s));
        float shade = tone * grain * veinShadow * edgeShadow;
        return new Color(colour.r * shade, colour.g * shade, colour.b * shade, colour.a);
    }

    private Vector2[] CellCentres(float reach)
    {
        Vector2[] centres = new Vector2[shape.CellCount];
        for (int cell = 0; cell < centres.Length; cell++)
        {
            centres[cell] = shape.Point(shape.CellMiddle(cell, reach), reach);
        }

        return centres;
    }

    private Eye[] PlaceEyes()
    {
        List<Eye> placed = new List<Eye>();
        if (wing.eyeSize <= 0.0f)
        {
            return placed.ToArray();
        }

        Vector2[] centres = CellCentres(wing.eyeReach);
        for (int cell = 0; cell < centres.Length; cell++)
        {
            if (wing.eyeCells[cell] > 0.0f)
            {
                placed.Add(new Eye(centres[cell], wing.eyeSize * shape.Length * wing.eyeCells[cell]));
            }
        }

        return placed.ToArray();
    }

    private static float Within(float distance, float edge, float softness)
    {
        return 1.0f - Mathf.SmoothStep(0.0f, 1.0f, Mathf.InverseLerp(edge - softness, edge + softness, distance));
    }

    private float StripeMask(float t, float s, float pixelS)
    {
        if (wing.stripeWidth <= 0.0f || wing.stripeCount <= 0)
        {
            return 0.0f;
        }

        float phase = Mathf.Repeat(t * wing.stripeCount, 1.0f) - 0.5f;
        float across = Mathf.Abs(phase) / wing.stripeCount;
        float halfWidth = wing.stripeWidth * 0.5f / wing.stripeCount;
        float along = Mathf.Min(Mathf.InverseLerp(wing.stripeStart - StripeFade, wing.stripeStart + StripeFade, s),
            1.0f - Mathf.InverseLerp(wing.stripeEnd - StripeFade, wing.stripeEnd + StripeFade, s));
        return Within(across, halfWidth, pixelS * 0.5f) * Mathf.SmoothStep(0.0f, 1.0f, along);
    }

    private float MarginSpotMask(Vector2 point, float pixel)
    {
        float mask = 0.0f;
        for (int spot = 0; spot < marginSpots.Length; spot++)
        {
            mask = Mathf.Max(mask, Within(Vector2.Distance(point, marginSpots[spot]), marginSpotRadii[spot], pixel));
        }

        return mask;
    }

    private Color PaintEyes(Color colour, Vector2 point, float pixel)
    {
        foreach (Eye eye in eyes)
        {
            float distance = Vector2.Distance(point, eye.centre);
            if (distance > eye.radius + pixel)
            {
                continue;
            }

            Color rings = Color.Lerp(palette.eyeOuter, palette.eyeRing, Within(distance, eye.radius * RingShare, pixel));
            rings = Color.Lerp(rings, palette.eyeIris, Within(distance, eye.radius * IrisShare, pixel));
            rings = Color.Lerp(rings, palette.eyePupil, Within(distance, eye.radius * PupilShare, pixel));
            colour = Color.Lerp(colour, rings, Within(distance, eye.radius, pixel));
        }

        return colour;
    }

    private float VeinMask(float t, float s, float radius, float pixel, float widthScale)
    {
        if (s < VeinRootReach)
        {
            return 0.0f;
        }

        float halfWidth = widthScale * wing.veinWidth * shape.Length * (1.0f + VeinBaseThickening * (1.0f - s));
        float arcPerT = shape.SpanRadians * s * radius;
        int lastVein = shape.CellCount - 1;
        float nearest;

        if (s >= wing.discalEnd)
        {
            float outward = Mathf.InverseLerp(wing.discalEnd, 1.0f, s);
            nearest = float.MaxValue;
            for (int vein = 1; vein <= lastVein; vein++)
            {
                nearest = Mathf.Min(nearest, Mathf.Abs(t - Mathf.Lerp(veinStarts[vein], veinEnds[vein], outward)));
            }

            nearest *= arcPerT;
        }
        else
        {
            nearest = Mathf.Min(Mathf.Abs(t - veinStarts[1]), Mathf.Abs(t - veinStarts[lastVein])) * arcPerT;
        }

        if (t >= veinStarts[1] && t <= veinStarts[lastVein])
        {
            nearest = Mathf.Min(nearest, Mathf.Abs(s - wing.discalEnd) * radius);
        }

        return Within(nearest, halfWidth, pixel);
    }
}
