using System.Threading.Tasks;
using UnityEngine;

public readonly struct WingFrame
{
    private const int OutlineSamples = 128;
    private const float Padding = 0.02f;

    private readonly Vector2 corner;
    private readonly float side;
    private readonly Vector2 atlasOffset;

    public WingFrame(WingShape shape, Vector2 atlasOffset)
    {
        Vector2 min = Vector2.zero;
        Vector2 max = Vector2.zero;
        for (int i = 0; i <= OutlineSamples; i++)
        {
            Vector2 point = shape.Point(i / (float)OutlineSamples, 1.0f);
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }

        side = Mathf.Max(max.x - min.x, max.y - min.y) + 2.0f * Padding;
        corner = (min + max) * 0.5f - Vector2.one * (side * 0.5f);
        this.atlasOffset = atlasOffset;
    }

    public float Side => side;

    public Vector2 AtlasUv(Vector2 point)
    {
        Vector2 local = (point - corner) / side;
        return atlasOffset + local * WingPainter.HalfShare;
    }

    public Vector2 PointAt(float u, float v)
    {
        return corner + new Vector2(u, v) * side;
    }
}

public static class WingPainter
{
    public const float HalfShare = 0.5f;
    public const int Resolution = 1024;
    public const int DraftResolution = 256;

    public const float LeftSeedOffset = 0.37f;
    private const int QuadrantCount = 4;

    public static WingFrame Frame(WingShape shape, bool isFore, bool isLeft)
    {
        return new WingFrame(shape, new Vector2(isFore ? 0.0f : HalfShare, isLeft ? HalfShare : 0.0f));
    }

    public static Texture2D CreateAtlas(int resolution = Resolution)
    {
        return new Texture2D(resolution * 2, resolution * 2, TextureFormat.RGBA32, true)
        {
            name = "WingPattern",
            hideFlags = HideFlags.DontSave,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Trilinear,
            anisoLevel = 4
        };
    }

    public static void Paint(Texture2D atlas, ButterflySettings settings)
    {
        Color32[] pixels = new Color32[atlas.width * atlas.height];
        int resolution = atlas.height / 2;
        WingShape fore = new WingShape(settings.fore);
        WingShape hind = new WingShape(settings.hind);
        Parallel.For(0, QuadrantCount, quadrant =>
        {
            bool isFore = quadrant < QuadrantCount / 2;
            bool isLeft = quadrant % 2 == 1;
            WingShape shape = isFore ? fore : hind;
            float seed = settings.seed + (isLeft ? LeftSeedOffset : 0.0f);
            WingPattern pattern = new WingPattern(shape, isFore ? settings.fore : settings.hind, settings, seed);
            PaintQuadrant(pixels, resolution, isFore ? 0 : resolution, isLeft ? resolution : 0, pattern, Frame(shape, isFore, isLeft));
        });
        atlas.SetPixels32(pixels);
        atlas.Apply(true);
    }

    private static void PaintQuadrant(Color32[] pixels, int resolution, int columnOffset, int rowOffset, WingPattern pattern, WingFrame frame)
    {
        float pixel = frame.Side / resolution;
        int stride = resolution * 2;
        Parallel.For(0, resolution, row =>
        {
            float v = (row + 0.5f) / resolution;
            int rowStart = (rowOffset + row) * stride + columnOffset;
            for (int column = 0; column < resolution; column++)
            {
                Vector2 point = frame.PointAt((column + 0.5f) / resolution, v);
                pixels[rowStart + column] = pattern.ColourAt(point, pixel);
            }
        });
    }
}
