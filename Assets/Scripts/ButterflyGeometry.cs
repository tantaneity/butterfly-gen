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

    public static readonly Color Ink = ButterflySettings.Hex(0x2a2420);

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
}
