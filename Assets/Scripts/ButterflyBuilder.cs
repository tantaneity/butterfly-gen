using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public sealed class ButterflyBuilder : MonoBehaviour
{
    private const int WingCount = 4;

    private static readonly int PatternProperty = Shader.PropertyToID("_PatternTex");
    private static readonly string[] WingNames = { "ForeRight", "ForeLeft", "HindRight", "HindLeft" };

    public ButterflySettings settings = ButterflySpecies.Preset(0);

    private readonly Transform[] wings = new Transform[WingCount];
    private readonly Mesh[] wingMeshes = new Mesh[WingCount];
    private Mesh bodyMesh;
    private Texture2D fullPattern;
    private Texture2D draftPattern;
    private MaterialPropertyBlock properties;
    private bool isDirty;
    private float foreSweep;
    private float hindSweep;

    private void OnEnable()
    {
        Rebuild();
    }

    private void OnValidate()
    {
        isDirty = true;
    }

    private void Update()
    {
        if (!isDirty)
        {
            return;
        }

        isDirty = false;
        Rebuild();
    }

    private static bool IsFore(int wing) => wing < WingCount / 2;

    private static float Side(int wing) => wing % 2 == 0 ? 1.0f : -1.0f;

    public void SetAtlasResolution(int resolution)
    {
        EnsureParts();
        DestroyImmediate(fullPattern);
        fullPattern = WingPainter.CreateAtlas(resolution);
        Rebuild();
    }

    public void Rebuild(bool isDraft = false)
    {
        EnsureParts();

        for (int wing = 0; wing < WingCount; wing++)
        {
            bool isFore = IsFore(wing);
            bool isLeft = Side(wing) < 0.0f;
            WingShape shape = new WingShape(isFore ? settings.fore : settings.hind, WingPainter.Seed(settings, isLeft));
            WingFrame frame = WingPainter.Frame(shape, isFore, isLeft);
            MeshBuffer buffer = new MeshBuffer();
            ButterflyGeometry.BuildWing(buffer, shape, frame, Side(wing), isFore, settings.ground);
            buffer.WriteTo(wingMeshes[wing]);
            wings[wing].localPosition = ButterflyGeometry.Root(isFore, Side(wing));
        }

        MeshBuffer body = new MeshBuffer();
        BodyGeometry.Build(body, settings.body, ButterflyGeometry.Ink);
        body.WriteTo(bodyMesh);

        Texture2D pattern = isDraft ? draftPattern : fullPattern;
        WingPainter.Paint(pattern, settings);
        properties.SetTexture(PatternProperty, pattern);
        ApplyProperties();
        foreSweep = settings.foreSweep;
        hindSweep = settings.hindSweep;
        Pose(settings.wingLift, settings.wingLift);
    }

    public void Pose(float foreLift, float hindLift)
    {
        EnsureParts();
        for (int wing = 0; wing < WingCount; wing++)
        {
            bool isFore = IsFore(wing);
            wings[wing].localRotation = ButterflyGeometry.Hinge(Side(wing), isFore ? foreLift : hindLift,
                isFore ? foreSweep : hindSweep);
        }
    }

    private void EnsureParts()
    {
        if (bodyMesh == null)
        {
            bodyMesh = new Mesh { name = "ButterflyBody", hideFlags = HideFlags.DontSave };
            GetComponent<MeshFilter>().sharedMesh = bodyMesh;
        }

        if (fullPattern == null)
        {
            fullPattern = WingPainter.CreateAtlas();
            draftPattern = WingPainter.CreateAtlas(WingPainter.DraftResolution);
        }

        properties ??= new MaterialPropertyBlock();

        for (int wing = 0; wing < WingCount; wing++)
        {
            if (wings[wing] != null)
            {
                continue;
            }

            wingMeshes[wing] = new Mesh { name = WingNames[wing], hideFlags = HideFlags.DontSave };
            wings[wing] = FindOrCreateWing(WingNames[wing]);
            wings[wing].GetComponent<MeshFilter>().sharedMesh = wingMeshes[wing];
        }
    }

    private Transform FindOrCreateWing(string wingName)
    {
        Transform existing = transform.Find(wingName);
        if (existing != null)
        {
            return existing;
        }

        GameObject wing = new GameObject(wingName, typeof(MeshFilter), typeof(MeshRenderer)) { hideFlags = HideFlags.DontSave };
        wing.transform.SetParent(transform, false);
        return wing.transform;
    }

    private void ApplyProperties()
    {
        MeshRenderer bodyRenderer = GetComponent<MeshRenderer>();
        bodyRenderer.SetPropertyBlock(properties);
        foreach (Transform wing in wings)
        {
            MeshRenderer wingRenderer = wing.GetComponent<MeshRenderer>();
            wingRenderer.sharedMaterial = bodyRenderer.sharedMaterial;
            wingRenderer.shadowCastingMode = bodyRenderer.shadowCastingMode;
            wingRenderer.receiveShadows = false;
            wingRenderer.SetPropertyBlock(properties);
        }
    }
}
