using UnityEngine;

public static class PatternMath
{
    public static float Within(float distance, float edge, float softness)
    {
        return 1.0f - Mathf.SmoothStep(0.0f, 1.0f, Mathf.InverseLerp(edge - softness, edge + softness, distance));
    }

    public static Color Shade(Color colour, float shade)
    {
        return new Color(colour.r * shade, colour.g * shade, colour.b * shade, colour.a);
    }
}
