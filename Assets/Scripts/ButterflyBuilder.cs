using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public sealed class ButterflyBuilder : MonoBehaviour
{
    private static readonly int PatternProperty = Shader.PropertyToID("_PatternTex");

    public ButterflySettings settings = ButterflySpecies.Preset(0);

    private Mesh mesh;
    private Texture2D pattern;
    private MaterialPropertyBlock properties;

    private void OnEnable()
    {
        Rebuild();
    }

    private void OnValidate()
    {
        Rebuild();
    }

    public void Rebuild()
    {
        if (mesh == null)
        {
            mesh = new Mesh { name = "Butterfly", hideFlags = HideFlags.DontSave };
        }

        if (pattern == null)
        {
            pattern = WingPainter.CreateAtlas();
        }

        properties ??= new MaterialPropertyBlock();

        MeshBuffer buffer = new MeshBuffer();
        ButterflyGeometry.Build(buffer, settings);
        buffer.WriteTo(mesh);
        GetComponent<MeshFilter>().sharedMesh = mesh;

        WingPainter.Paint(pattern, settings);
        properties.SetTexture(PatternProperty, pattern);
        GetComponent<MeshRenderer>().SetPropertyBlock(properties);
    }
}
