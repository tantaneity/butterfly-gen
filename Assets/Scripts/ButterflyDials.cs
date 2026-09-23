using System;
using UnityEngine;

[Serializable]
public struct Dial
{
    public string label;
    public Vector3 start;
    public Vector3 end;
    [Range(0.0f, 1.0f)] public float value;

    public Vector3 PointAt(float t)
    {
        return Vector3.Lerp(start, end, t);
    }

    public Vector3 Handle => PointAt(value);
}

public static class ButterflyDials
{
    public const int Species = 0;
    public const int Eyes = 1;
    public const int Pattern = 2;
    public const int Tail = 3;

    private const float HandleRadius = 0.011f;
    private const float TrackWidth = 0.0022f;
    private const float TrackRecess = -0.02f;
    private const float ColumnDepth = 2.0f;
    private const float ColumnLeft = 0.36f;
    private const float ColumnRight = 0.48f;
    private const float ColumnTop = 0.11f;
    private const float RowStep = 0.07f;

    private const float ExtraTail = 0.4f;
    private const float MaxMarginWidth = 0.45f;
    private const float MaxBandWidth = 0.6f;
    private const float MaxStripeWidth = 0.9f;

    private static readonly Color TrackInk = new Color(0.66f, 0.68f, 0.70f, 1.0f);

    public static Dial[] Defaults()
    {
        return new[]
        {
            Row("species", 0, 0.0f),
            Row("eyes", 1, 0.5f),
            Row("pattern", 2, 0.5f),
            Row("tail", 3, 0.5f)
        };
    }

    private static Dial Row(string label, int row, float value)
    {
        float height = ColumnTop - row * RowStep;
        return new Dial
        {
            label = label,
            start = new Vector3(ColumnLeft, height, ColumnDepth),
            end = new Vector3(ColumnRight, height, ColumnDepth),
            value = value
        };
    }

    public static Vector3 RightEdgeOffset(Camera camera)
    {
        float halfHeight = ColumnDepth * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        return new Vector3(halfHeight * (camera.aspect - 1.0f), 0.0f, 0.0f);
    }

    public static float[] Values(Dial[] dials)
    {
        float[] values = new float[dials.Length];
        for (int i = 0; i < dials.Length; i++)
        {
            values[i] = dials[i].value;
        }

        return values;
    }

    public static ButterflySettings Resolve(float[] values)
    {
        ButterflySettings settings = ButterflySpecies.At(values[Species] * (ButterflySpecies.Count - 1));
        float eyes = values[Eyes] * 2.0f;
        float pattern = values[Pattern] * 2.0f;
        float tail = values[Tail] * 2.0f;

        foreach (WingSettings wing in new[] { settings.fore, settings.hind })
        {
            wing.eyeSize *= eyes;
            wing.marginWidth = Mathf.Min(MaxMarginWidth, wing.marginWidth * pattern);
            wing.apexPatch *= pattern;
            wing.bandWidth = Mathf.Min(MaxBandWidth, wing.bandWidth * pattern);
            wing.stripeWidth = Mathf.Min(MaxStripeWidth, wing.stripeWidth * pattern);
        }

        settings.hind.tailLength = settings.hind.tailLength * Mathf.Min(tail, 1.0f) + Mathf.Max(0.0f, tail - 1.0f) * ExtraTail;
        return settings;
    }

    public static void BuildMesh(MeshBuffer mesh, Dial[] dials, Color track, Color handle)
    {
        mesh.SetInk(TrackInk);

        foreach (Dial dial in dials)
        {
            Strokes.AddRibbon(mesh, new[] { dial.start, dial.end }, track, TrackWidth, Outline.Silhouette, TrackRecess);
        }

        foreach (Dial dial in dials)
        {
            Strokes.AddDisc(mesh, dial.Handle, HandleRadius, handle, Outline.Silhouette);
        }
    }

    public static int Pick(Dial[] dials, Transform space, Camera camera, Vector2 pointer, float pixelRadius)
    {
        int best = -1;
        float bestDistance = pixelRadius;

        for (int i = 0; i < dials.Length; i++)
        {
            float distance = Vector2.Distance(OnScreen(space, camera, dials[i].Handle), pointer);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = i;
            }
        }

        return best;
    }

    public static float ValueUnderPointer(Dial dial, Transform space, Camera camera, Vector2 pointer)
    {
        Vector2 start = OnScreen(space, camera, dial.start);
        Vector2 track = OnScreen(space, camera, dial.end) - start;
        float lengthSquared = track.sqrMagnitude;
        return lengthSquared > 0.0f ? Mathf.Clamp01(Vector2.Dot(pointer - start, track) / lengthSquared) : dial.value;
    }

    public static Vector2 OnScreen(Transform space, Camera camera, Vector3 local)
    {
        Vector3 screen = camera.WorldToScreenPoint(space.TransformPoint(local));
        return new Vector2(screen.x, screen.y);
    }
}
