using UnityEngine;

public static class ButterflyGeometry
{
    private const int WingAngleSteps = 160;
    private const int WingRadialSteps = 18;
    private const float HindLayer = 0.004f;
    private const float BodyHeight = 0.03f;
    private const float WingRootSpacing = 0.025f;
    private const float ForeRootZ = 0.04f;
    private const float HindRootZ = 0.02f;
    private const float ThoraxFrontZ = 0.08f;
    private const float ThoraxBackZ = -0.07f;
    private const float ThoraxHalfWidth = 0.05f;
    private const float ThoraxRoundness = 0.45f;
    private const int ThoraxSteps = 12;
    private const float HeadZ = 0.1f;
    private const float HeadRadius = 0.032f;
    private const float AbdomenStartZ = -0.02f;
    private const float AbdomenLength = 0.3f;
    private const float AbdomenHalfWidth = 0.042f;
    private const int AbdomenSteps = 40;
    private const float AbdomenTaperStart = 0.3f;
    private const float AbdomenTaperPower = 0.75f;
    private const int AbdomenSegments = 7;
    private const float SegmentBulge = 0.07f;
    private const float AbdomenDroop = 0.025f;
    private const float MinBodyHalfWidth = 0.01f;
    private const float AbdomenTipHalfWidth = 0.004f;
    private const float AntennaSpread = 24.0f;
    private const float AntennaBend = 0.06f;
    private const float AntennaSplay = 0.7f;
    private const int AntennaSteps = 24;
    private const float AntennaHalfWidth = 0.0035f;
    private const float AntennaClubHalfWidth = 0.011f;
    private const float AntennaClubStart = 0.78f;
    private const float AntennaClubRoundness = 0.6f;
    private const float BodyDepthBias = 0.002f;

    private static readonly Color Ink = ButterflySettings.Hex(0x2a2420);

    public static Vector3 Root(bool isFore, float side)
    {
        return new Vector3(side * WingRootSpacing, BodyHeight * 0.5f, isFore ? ForeRootZ : HindRootZ);
    }

    public static Quaternion Hinge(float side, float lift)
    {
        return Quaternion.AngleAxis(side * lift, Vector3.forward);
    }

    public static void BuildWing(MeshBuffer mesh, WingShape shape, WingFrame frame, float side, bool isFore, Color fill)
    {
        mesh.SetInk(Ink);
        mesh.SetFacing(Vector3.up);
        float layer = isFore ? 0.0f : -HindLayer;

        for (int i = 0; i <= WingAngleSteps; i++)
        {
            float t = i / (float)WingAngleSteps;
            for (int j = 0; j <= WingRadialSteps; j++)
            {
                float s = j / (float)WingRadialSteps;
                Vector2 point = shape.Point(t, s);
                Vector2 rim = RimDirection(shape, i, j, t);
                mesh.AddVertex(new Vector3(side * point.x, layer, point.y), new Vector3(side * rim.x, 0.0f, rim.y), Vector4.zero, fill,
                    StrokeKind.Card, 0.0f, Outline.Silhouette, 0.0f, Shading.Surface(Vector3.up));
                mesh.SetPattern(frame.AtlasUv(point));
            }
        }

        int columns = WingRadialSteps + 1;
        for (int i = 0; i < WingAngleSteps; i++)
        {
            for (int j = 0; j < WingRadialSteps; j++)
            {
                int corner = i * columns + j;
                mesh.AddQuad(corner, corner + columns, corner + columns + 1, corner + 1);
            }
        }
    }

    public static void BuildBody(MeshBuffer mesh, ButterflySettings settings)
    {
        mesh.SetInk(Ink);
        mesh.SetFacing(Vector3.zero);
        AddBody(mesh, settings);
    }

    private static Vector2 RimDirection(WingShape shape, int i, int j, float t)
    {
        if (j == 0)
        {
            return Vector2.zero;
        }

        Vector2 direction = Vector2.zero;
        if (j == WingRadialSteps)
        {
            float step = 1.0f / WingAngleSteps;
            Vector2 tangent = shape.Point(Mathf.Min(t + step, 1.0f), 1.0f) - shape.Point(Mathf.Max(t - step, 0.0f), 1.0f);
            direction += new Vector2(-tangent.y, tangent.x).normalized;
        }

        if (i == 0)
        {
            direction += WingShape.Direction(shape.Angle(0.0f) - Mathf.PI * 0.5f);
        }

        if (i == WingAngleSteps)
        {
            direction += WingShape.Direction(shape.Angle(1.0f) + Mathf.PI * 0.5f);
        }

        return direction.sqrMagnitude > 0.0f ? direction.normalized : Vector2.zero;
    }

    private static void AddBody(MeshBuffer mesh, ButterflySettings settings)
    {
        Vector3[] abdomen = new Vector3[AbdomenSteps + 1];
        float[] abdomenWidths = new float[AbdomenSteps + 1];
        for (int i = 0; i <= AbdomenSteps; i++)
        {
            float along = i / (float)AbdomenSteps;
            abdomen[i] = new Vector3(0.0f, BodyHeight - AbdomenDroop * along * along, AbdomenStartZ - AbdomenLength * along);
            abdomenWidths[i] = AbdomenHalfWidth * AbdomenProfile(along);
        }

        Strokes.AddRibbon(mesh, abdomen, settings.body, abdomenWidths, Outline.Silhouette, BodyDepthBias, StrokeKind.Limb);

        Vector3[] thorax = new Vector3[ThoraxSteps + 1];
        float[] thoraxWidths = new float[ThoraxSteps + 1];
        for (int i = 0; i <= ThoraxSteps; i++)
        {
            float along = i / (float)ThoraxSteps;
            thorax[i] = new Vector3(0.0f, BodyHeight, Mathf.Lerp(ThoraxFrontZ, ThoraxBackZ, along));
            thoraxWidths[i] = Mathf.Max(MinBodyHalfWidth, ThoraxHalfWidth * Dome(along, ThoraxRoundness));
        }

        Strokes.AddRibbon(mesh, thorax, settings.body, thoraxWidths, Outline.Silhouette, BodyDepthBias, StrokeKind.Limb);

        Vector3 head = new Vector3(0.0f, BodyHeight, HeadZ);
        Strokes.AddDisc(mesh, head, HeadRadius, settings.body, Outline.Silhouette);

        foreach (float side in new[] { 1.0f, -1.0f })
        {
            AddAntenna(mesh, head, side, settings);
        }
    }

    private static float AbdomenProfile(float along)
    {
        float taper = Dome(Mathf.Lerp(AbdomenTaperStart, 1.0f, along), AbdomenTaperPower);
        float segment = 1.0f + SegmentBulge * Mathf.Abs(Mathf.Sin(Mathf.PI * along * AbdomenSegments));
        return Mathf.Max(AbdomenTipHalfWidth / AbdomenHalfWidth, taper * Mathf.Lerp(segment, 1.0f, along * along));
    }

    private static void AddAntenna(MeshBuffer mesh, Vector3 head, float side, ButterflySettings settings)
    {
        Vector3[] points = new Vector3[AntennaSteps + 1];
        float[] halfWidths = new float[AntennaSteps + 1];
        Vector3 flat = head;
        float stepLength = settings.antennaLength / AntennaSteps;
        for (int i = 0; i <= AntennaSteps; i++)
        {
            float along = i / (float)AntennaSteps;
            points[i] = flat + Vector3.up * (AntennaBend * Mathf.Sin(Mathf.PI * along * 0.5f));
            halfWidths[i] = AntennaHalfWidth + (AntennaClubHalfWidth - AntennaHalfWidth) * ClubProfile(along);
            float angle = side * AntennaSpread * (1.0f + AntennaSplay * along * along);
            flat += Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward * stepLength;
        }

        Strokes.AddRibbon(mesh, points, settings.body, halfWidths, Outline.Contour, BodyDepthBias, StrokeKind.Limb);
    }

    private static float ClubProfile(float along)
    {
        return Dome(Mathf.InverseLerp(AntennaClubStart, 1.0f, along), AntennaClubRoundness);
    }

    private static float Dome(float phase, float power)
    {
        return Mathf.Pow(Mathf.Max(0.0f, Mathf.Sin(Mathf.PI * phase)), power);
    }
}
