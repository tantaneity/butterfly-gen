using UnityEngine;

public readonly struct WingShape
{
    private readonly WingSettings settings;
    private readonly float startRadians;
    private readonly float spanRadians;

    public WingShape(WingSettings settings)
    {
        this.settings = settings;
        startRadians = settings.startAngle * Mathf.Deg2Rad;
        spanRadians = (settings.endAngle - settings.startAngle) * Mathf.Deg2Rad;
    }

    public int CellCount => settings.veinCount;
    public float SpanRadians => spanRadians;
    public float Length => settings.length;

    public float Angle(float t)
    {
        return startRadians + spanRadians * t;
    }

    public static Vector2 Direction(float angle)
    {
        return new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
    }

    public float Radius(float t)
    {
        float scallopPhase = Mathf.Repeat(t * settings.veinCount, 1.0f);
        float scallop = 1.0f - settings.scallopDepth * Mathf.Sin(Mathf.PI * scallopPhase) * Mathf.Sin(Mathf.PI * scallopPhase);
        float tailOffset = (t - settings.tailPosition) / Mathf.Max(settings.tailWidth, 1e-4f);
        float tail = settings.tailLength * Mathf.Exp(-tailOffset * tailOffset);
        return settings.length * (Profile(t) * scallop + tail);
    }

    public Vector2 Point(float t, float s)
    {
        return Direction(Angle(t)) * (s * Radius(t));
    }

    public void ToPolar(Vector2 point, out float t, out float s, out float radius)
    {
        float angle = Mathf.Atan2(point.x, point.y);
        t = Mathf.Clamp01((angle - startRadians) / spanRadians);
        radius = Mathf.Max(Radius(t), 1e-5f);
        s = point.magnitude / radius;
    }

    public float VeinT(int vein, float s)
    {
        if (vein <= 0)
        {
            return 0.0f;
        }

        if (vein >= settings.veinCount)
        {
            return 1.0f;
        }

        float end = vein / (float)settings.veinCount;
        float start = DiscalT(end);
        float outward = Mathf.InverseLerp(settings.discalEnd, 1.0f, s);
        return Mathf.Lerp(start, end, outward);
    }

    public float DiscalT(float marginT)
    {
        return 0.5f + (marginT - 0.5f) * settings.discalSpread;
    }

    public float CellMiddle(int cell, float s)
    {
        return 0.5f * (VeinT(cell, s) + VeinT(cell + 1, s));
    }

    private float Profile(float t)
    {
        float[] radii = settings.radii;
        float position = Mathf.Clamp01(t) * (radii.Length - 1);
        int index = Mathf.Min(Mathf.FloorToInt(position), radii.Length - 2);
        float local = position - index;

        float before = radii[Mathf.Max(index - 1, 0)];
        float from = radii[index];
        float to = radii[index + 1];
        float after = radii[Mathf.Min(index + 2, radii.Length - 1)];
        return CatmullRom(before, from, to, after, local);
    }

    private static float CatmullRom(float before, float from, float to, float after, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * (2.0f * from + (to - before) * t + (2.0f * before - 5.0f * from + 4.0f * to - after) * t2
                       + (3.0f * from - before - 3.0f * to + after) * t3);
    }
}
