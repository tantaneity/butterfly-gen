using System.Collections.Generic;
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
    private readonly float marginSpotRadius;
    private readonly Eye[] eyes;

    public WingPattern(WingShape shape, WingSettings wing, ButterflySettings palette)
    {
        this.shape = shape;
        this.wing = wing;
        this.palette = palette;

        veinStarts = new float[shape.CellCount + 1];
        veinEnds = new float[shape.CellCount + 1];
        for (int vein = 0; vein <= shape.CellCount; vein++)
        {
            veinStarts[vein] = shape.VeinT(vein, 0.0f);
            veinEnds[vein] = shape.VeinT(vein, 1.0f);
        }

        marginSpotRadius = wing.marginSpotSize * shape.Length;
        marginSpots = marginSpotRadius > 0.0f && wing.marginWidth > 0.0f ? CellCentres(1.0f - wing.marginWidth * 0.5f) : new Vector2[0];
        eyes = PlaceEyes();
    }

    public Color ColourAt(Vector2 point, float pixel)
    {
        shape.ToPolar(point, out float t, out float s, out float radius);
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
        colour = PaintEyes(colour, point, pixel);
        return Color.Lerp(colour, palette.vein, VeinMask(t, s, radius, pixel));
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
        foreach (Vector2 centre in marginSpots)
        {
            mask = Mathf.Max(mask, Within(Vector2.Distance(point, centre), marginSpotRadius, pixel));
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

    private float VeinMask(float t, float s, float radius, float pixel)
    {
        if (s < VeinRootReach)
        {
            return 0.0f;
        }

        float halfWidth = wing.veinWidth * shape.Length * (1.0f + VeinBaseThickening * (1.0f - s));
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
