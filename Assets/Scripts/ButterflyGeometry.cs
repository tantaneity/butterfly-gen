using UnityEngine;

public static class ButterflyGeometry
{
    private const int WingAngleSteps = 160;
    private const int WingRadialSteps = 18;
    private const float HindLayer = 0.004f;
    private const float BodyHeight = 0.03f;
    private const float WingRootSpacing = 0.025f;
    private const float ForeRootZ = 0.04f;
    private const float HindRootZ = -0.01f;
    private const float ThoraxZ = 0.02f;
    private const float ThoraxRadius = 0.055f;
    private const float HeadZ = 0.1f;
    private const float HeadRadius = 0.036f;
    private const float AbdomenStartZ = -0.03f;
    private const float AbdomenLength = 0.3f;
    private const float AbdomenHalfWidth = 0.028f;
    private const int AbdomenSteps = 6;
    private const float AntennaSpread = 24.0f;
    private const float AntennaBend = 0.06f;
    private const int AntennaSteps = 8;
    private const float AntennaHalfWidth = 0.004f;
    private const float AntennaClubRadius = 0.014f;
    private const float BodyDepthBias = 0.002f;

    private static readonly Color Ink = ButterflySettings.Hex(0x2a2420);

    private readonly struct WingPlacement
    {
        public readonly Quaternion rotation;
        public readonly Vector3 root;
        public readonly float side;
        public readonly float layer;

        public WingPlacement(float side, float lift, float rootZ, float layer)
        {
            this.side = side;
            this.layer = layer;
            rotation = Quaternion.AngleAxis(side * lift, Vector3.forward);
            root = new Vector3(side * WingRootSpacing, BodyHeight * 0.5f, rootZ);
        }

        public Vector3 Place(Vector2 point)
        {
            return root + rotation * new Vector3(side * point.x, layer, point.y);
        }

        public Vector3 Turn(Vector2 direction)
        {
            return rotation * new Vector3(side * direction.x, 0.0f, direction.y);
        }

        public Vector3 Normal => rotation * Vector3.up;
    }

    public static void Build(MeshBuffer mesh, ButterflySettings settings)
    {
        mesh.SetInk(Ink);
        WingShape fore = new WingShape(settings.fore);
        WingShape hind = new WingShape(settings.hind);

        foreach (float side in new[] { 1.0f, -1.0f })
        {
            AddWing(mesh, hind, WingPainter.HindFrame(hind), new WingPlacement(side, settings.wingLift, HindRootZ, -HindLayer), settings.ground);
            AddWing(mesh, fore, WingPainter.ForeFrame(fore), new WingPlacement(side, settings.wingLift, ForeRootZ, 0.0f), settings.ground);
        }

        mesh.SetFacing(Vector3.zero);
        AddBody(mesh, settings);
    }

    private static void AddWing(MeshBuffer mesh, WingShape shape, WingFrame frame, WingPlacement placement, Color fill)
    {
        int first = mesh.VertexCount;
        Vector3 normal = placement.Normal;
        mesh.SetFacing(normal);

        for (int i = 0; i <= WingAngleSteps; i++)
        {
            float t = i / (float)WingAngleSteps;
            for (int j = 0; j <= WingRadialSteps; j++)
            {
                float s = j / (float)WingRadialSteps;
                Vector2 point = shape.Point(t, s);
                Vector3 expansion = placement.Turn(RimDirection(shape, i, j, t));
                mesh.AddVertex(placement.Place(point), expansion, Vector4.zero, fill, StrokeKind.Card, 0.0f,
                    Outline.Silhouette, 0.0f, Shading.Surface(normal));
                mesh.SetPattern(frame.AtlasUv(point));
            }
        }

        int columns = WingRadialSteps + 1;
        for (int i = 0; i < WingAngleSteps; i++)
        {
            for (int j = 0; j < WingRadialSteps; j++)
            {
                int corner = first + i * columns + j;
                mesh.AddQuad(corner, corner + columns, corner + columns + 1, corner + 1);
            }
        }
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
        for (int i = 0; i <= AbdomenSteps; i++)
        {
            abdomen[i] = new Vector3(0.0f, BodyHeight, AbdomenStartZ - AbdomenLength * i / AbdomenSteps);
        }

        Strokes.AddRibbon(mesh, abdomen, settings.body, AbdomenHalfWidth, Outline.Silhouette, BodyDepthBias);
        Strokes.AddDisc(mesh, abdomen[AbdomenSteps], AbdomenHalfWidth, settings.body, Outline.Silhouette);
        Strokes.AddDisc(mesh, new Vector3(0.0f, BodyHeight, ThoraxZ), ThoraxRadius, settings.body, Outline.Silhouette);

        Vector3 head = new Vector3(0.0f, BodyHeight, HeadZ);
        Strokes.AddDisc(mesh, head, HeadRadius, settings.body, Outline.Silhouette);

        foreach (float side in new[] { 1.0f, -1.0f })
        {
            AddAntenna(mesh, head, side, settings);
        }
    }

    private static void AddAntenna(MeshBuffer mesh, Vector3 head, float side, ButterflySettings settings)
    {
        Vector3 reach = Quaternion.AngleAxis(side * AntennaSpread, Vector3.up) * Vector3.forward * settings.antennaLength;
        Vector3[] points = new Vector3[AntennaSteps + 1];
        for (int i = 0; i <= AntennaSteps; i++)
        {
            float along = i / (float)AntennaSteps;
            points[i] = head + reach * along + Vector3.up * (AntennaBend * Mathf.Sin(Mathf.PI * along * 0.5f));
        }

        Strokes.AddRibbon(mesh, points, settings.body, AntennaHalfWidth, Outline.Contour, BodyDepthBias);
        Strokes.AddDisc(mesh, points[AntennaSteps], AntennaClubRadius, settings.body, Outline.Contour);
    }
}
