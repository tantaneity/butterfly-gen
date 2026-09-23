using System;

[Serializable]
public sealed class WingSettings
{
    public float startAngle;
    public float endAngle;
    public float length;
    public float[] radii;
    public float tailLength;
    public float tailPosition = 0.6f;
    public float tailWidth = 0.05f;
    public float scallopDepth;

    public int veinCount;
    public float discalEnd = 0.42f;
    public float discalSpread = 0.5f;
    public float veinWidth = 0.004f;

    public float basalReach;
    public float bandCenter = 0.6f;
    public float bandWidth;
    public float bandTilt;
    public int stripeCount = 3;
    public float stripeWidth;
    public float stripeStart = 0.2f;
    public float stripeEnd = 0.85f;
    public float marginWidth;
    public float apexPatch;
    public float marginSpotSize;

    public float eyeReach = 0.7f;
    public float eyeSize;
    public float[] eyeCells;

    public static WingSettings Fore()
    {
        const int cells = 9;
        return new WingSettings
        {
            startAngle = 60.0f, endAngle = 128.0f, length = 0.62f,
            radii = new[] { 0.96f, 1.0f, 0.94f, 0.84f, 0.74f, 0.66f, 0.6f },
            veinCount = cells, eyeCells = new float[cells]
        };
    }

    public static WingSettings Hind()
    {
        const int cells = 7;
        return new WingSettings
        {
            startAngle = 96.0f, endAngle = 176.0f, length = 0.47f,
            radii = new[] { 0.78f, 0.92f, 1.0f, 1.0f, 0.94f, 0.82f, 0.6f },
            veinCount = cells, eyeCells = new float[cells]
        };
    }
}
