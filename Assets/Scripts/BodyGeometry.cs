using UnityEngine;

public static class BodyGeometry
{
    private const float BodyHeight = 0.03f;
    private const float ThoraxCentreZ = 0.005f;
    private const float ThoraxRoundness = 0.45f;
    private const int ThoraxSteps = 12;
    private const float HeadTuck = 0.4f;
    private const float AbdomenOverlap = 0.05f;
    private const int AbdomenSteps = 40;
    private const float AbdomenTaperStart = 0.3f;
    private const float AbdomenTaperPower = 0.75f;
    private const int AbdomenSegments = 7;
    private const float SegmentBulge = 0.07f;
    private const float BandSharpness = 2.5f;
    private const float AbdomenDroop = 0.025f;
    private const float MinBodyHalfWidth = 0.01f;
    private const float AbdomenTipHalfWidth = 0.004f;
    private const float AntennaBend = 0.06f;
    private const int AntennaSteps = 24;
    private const float AntennaHalfWidth = 0.0035f;
    private const float AntennaClubRoundness = 0.6f;
    private const float DepthBias = 0.002f;

    public static void Build(MeshBuffer mesh, BodySettings body, Color ink)
    {
        mesh.SetInk(ink);
        mesh.SetFacing(Vector3.zero);

        float thoraxFront = ThoraxCentreZ + body.thoraxLength * 0.5f;
        float thoraxBack = ThoraxCentreZ - body.thoraxLength * 0.5f;
        AddAbdomen(mesh, body, thoraxBack + AbdomenOverlap);
        AddThorax(mesh, body, thoraxFront, thoraxBack);

        Vector3 head = new Vector3(0.0f, BodyHeight, thoraxFront + body.headRadius * (1.0f - HeadTuck));
        Strokes.AddDisc(mesh, head, body.headRadius, body.colour, Outline.Silhouette);

        foreach (float side in new[] { 1.0f, -1.0f })
        {
            AddAntenna(mesh, head, side, body);
        }
    }

    private static void AddAbdomen(MeshBuffer mesh, BodySettings body, float startZ)
    {
        Vector3[] points = new Vector3[AbdomenSteps + 1];
        float[] halfWidths = new float[AbdomenSteps + 1];
        Color[] fills = new Color[AbdomenSteps + 1];
        for (int i = 0; i <= AbdomenSteps; i++)
        {
            float along = i / (float)AbdomenSteps;
            points[i] = new Vector3(0.0f, BodyHeight - AbdomenDroop * along * along, startZ - body.abdomenLength * along);
            halfWidths[i] = Mathf.Max(AbdomenTipHalfWidth, body.abdomenWidth * AbdomenProfile(along));
            float band = Mathf.Pow(Mathf.Abs(Mathf.Cos(Mathf.PI * along * AbdomenSegments)), BandSharpness);
            fills[i] = Color.Lerp(body.colour, body.accent, body.bandStrength * band);
        }

        Strokes.AddRibbon(mesh, points, fills, halfWidths, Outline.Silhouette, DepthBias, StrokeKind.Limb);
    }

    private static void AddThorax(MeshBuffer mesh, BodySettings body, float frontZ, float backZ)
    {
        Vector3[] points = new Vector3[ThoraxSteps + 1];
        float[] halfWidths = new float[ThoraxSteps + 1];
        for (int i = 0; i <= ThoraxSteps; i++)
        {
            float along = i / (float)ThoraxSteps;
            points[i] = new Vector3(0.0f, BodyHeight, Mathf.Lerp(frontZ, backZ, along));
            halfWidths[i] = Mathf.Max(MinBodyHalfWidth, body.thoraxWidth * Dome(along, ThoraxRoundness));
        }

        Strokes.AddRibbon(mesh, points, body.colour, halfWidths, Outline.Silhouette, DepthBias, StrokeKind.Limb);
    }

    private static void AddAntenna(MeshBuffer mesh, Vector3 head, float side, BodySettings body)
    {
        Vector3[] points = new Vector3[AntennaSteps + 1];
        float[] halfWidths = new float[AntennaSteps + 1];
        Vector3 flat = head;
        float stepLength = body.antennaLength / AntennaSteps;
        for (int i = 0; i <= AntennaSteps; i++)
        {
            float along = i / (float)AntennaSteps;
            points[i] = flat + Vector3.up * (AntennaBend * Mathf.Sin(Mathf.PI * along * 0.5f));
            float club = Dome(Mathf.InverseLerp(body.clubStart, 1.0f, along), AntennaClubRoundness);
            halfWidths[i] = Mathf.Lerp(AntennaHalfWidth, Mathf.Max(AntennaHalfWidth, body.clubWidth), club);
            float angle = side * body.antennaSpread * (1.0f + body.antennaSplay * along * along);
            flat += Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward * stepLength;
        }

        Strokes.AddRibbon(mesh, points, body.colour, halfWidths, Outline.Contour, DepthBias, StrokeKind.Limb);
    }

    private static float AbdomenProfile(float along)
    {
        float taper = Dome(Mathf.Lerp(AbdomenTaperStart, 1.0f, along), AbdomenTaperPower);
        float segment = 1.0f + SegmentBulge * Mathf.Abs(Mathf.Sin(Mathf.PI * along * AbdomenSegments));
        return taper * Mathf.Lerp(segment, 1.0f, along * along);
    }

    private static float Dome(float phase, float power)
    {
        return Mathf.Pow(Mathf.Max(0.0f, Mathf.Sin(Mathf.PI * phase)), power);
    }
}
