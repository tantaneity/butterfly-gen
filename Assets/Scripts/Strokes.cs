using System.Collections.Generic;
using UnityEngine;

public static class Strokes
{
    private const int DiscSteps = 20;
    private const float FullTurn = 2.0f * Mathf.PI;

    public static void AddRibbon(MeshBuffer mesh, IReadOnlyList<Vector3> points, Color fill, float halfWidth,
        float outlineWeight, float depthBias, StrokeKind kind = StrokeKind.Stem)
    {
        float[] halfWidths = new float[points.Count];
        System.Array.Fill(halfWidths, halfWidth);
        AddRibbon(mesh, points, fill, halfWidths, outlineWeight, depthBias, kind);
    }

    public static void AddRibbon(MeshBuffer mesh, IReadOnlyList<Vector3> points, Color fill, IReadOnlyList<float> halfWidths,
        float outlineWeight, float depthBias, StrokeKind kind = StrokeKind.Stem)
    {
        Color[] fills = new Color[points.Count];
        System.Array.Fill(fills, fill);
        AddRibbon(mesh, points, fills, halfWidths, outlineWeight, depthBias, kind);
    }

    public static void AddRibbon(MeshBuffer mesh, IReadOnlyList<Vector3> points, IReadOnlyList<Color> fills, IReadOnlyList<float> halfWidths,
        float outlineWeight, float depthBias, StrokeKind kind = StrokeKind.Stem)
    {
        int first = mesh.VertexCount;

        for (int i = 0; i < points.Count; i++)
        {
            Vector3 ahead = points[Mathf.Min(i + 1, points.Count - 1)];
            Vector3 behind = points[Mathf.Max(i - 1, 0)];
            Vector3 tangent = Vector3.Normalize(ahead - behind);

            mesh.AddVertex(points[i], Vector3.zero, new Vector4(tangent.x, tangent.y, tangent.z, -1.0f), fills[i],
                kind, halfWidths[i], outlineWeight, depthBias, Shading.Flat);
            mesh.AddVertex(points[i], Vector3.zero, new Vector4(tangent.x, tangent.y, tangent.z, 1.0f), fills[i],
                kind, halfWidths[i], outlineWeight, depthBias, Shading.Flat);

            if (i > 0)
            {
                int here = first + i * 2;
                mesh.AddQuad(here - 2, here - 1, here + 1, here);
            }
        }
    }

    public static void AddDisc(MeshBuffer mesh, Vector3 anchor, float radius, Color fill, float outlineWeight)
    {
        int first = mesh.VertexCount;
        mesh.AddVertex(anchor, Vector3.zero, Vector4.zero, fill, StrokeKind.Billboard, 0.0f, 0.0f, 0.0f, Shading.Flat);

        for (int i = 0; i < DiscSteps; i++)
        {
            float angle = FullTurn * i / DiscSteps;
            Vector2 outward = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            mesh.AddVertex(anchor, outward * radius, outward, fill, StrokeKind.Billboard, 0.0f, outlineWeight, 0.0f, Shading.Flat);
            mesh.AddTriangle(first, first + 1 + i, first + 1 + (i + 1) % DiscSteps);
        }
    }
}
