using System.Threading.Tasks;
using UnityEngine;

public readonly struct WingFrame
{
    private const int OutlineSamples = 128;
    private const float Padding = 0.02f;

    private readonly Vector2 corner;
    private readonly float side;
    private readonly float atlasOffset;

    public WingFrame(WingShape shape, float atlasOffset)
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
        return new Vector2(atlasOffset + local.x * WingPainter.HalfShare, local.y);
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

    public static WingFrame ForeFrame(WingShape shape) => new WingFrame(shape, 0.0f);
    public static WingFrame HindFrame(WingShape shape) => new WingFrame(shape, HalfShare);

    public static Texture2D CreateAtlas()
    {
        return new Texture2D(Resolution * 2, Resolution, TextureFormat.RGBA32, true)
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
        WingShape fore = new WingShape(settings.fore);
        WingShape hind = new WingShape(settings.hind);
        PaintHalf(pixels, 0, fore, ForeFrame(fore), settings.fore, settings);
        PaintHalf(pixels, Resolution, hind, HindFrame(hind), settings.hind, settings);
        atlas.SetPixels32(pixels);
        atlas.Apply(true);
    }

    private static void PaintHalf(Color32[] pixels, int columnOffset, WingShape shape, WingFrame frame, WingSettings wing, ButterflySettings palette)
    {
        WingPattern pattern = new WingPattern(shape, wing, palette);
        float pixel = frame.Side / Resolution;
        int stride = Resolution * 2;
        Parallel.For(0, Resolution, row =>
        {
            float v = (row + 0.5f) / Resolution;
            for (int column = 0; column < Resolution; column++)
            {
                Vector2 point = frame.PointAt((column + 0.5f) / Resolution, v);
                pixels[row * stride + columnOffset + column] = pattern.ColourAt(point, pixel);
            }
        });
    }
}
