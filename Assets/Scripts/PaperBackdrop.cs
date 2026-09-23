using Unity.Mathematics;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public sealed class PaperBackdrop : MonoBehaviour
{
    private const int TextureSize = 1024;
    private const float Depth = 40.0f;
    private const float TilesPerHeight = 1.2f;
    private const float CloudDepth = 0.018f;
    private const float CloudPeriod = 3.0f;
    private const float FibreDepth = 0.01f;
    private const float FibreLong = 6.0f;
    private const float FibreShort = 96.0f;
    private const float GrainDepth = 0.045f;
    private const float GrainPeriod = 320.0f;

    private static readonly int BaseMapProperty = Shader.PropertyToID("_BaseMap");

    public Color paper = ButterflySettings.Hex(0xf6f3ec);

    private Mesh quad;
    private Texture2D texture;
    private MaterialPropertyBlock properties;
    private float fittedAspect;
    private float fittedFieldOfView;

    private void OnEnable()
    {
        Paint();
        Camera view = transform.parent != null ? transform.parent.GetComponent<Camera>() : null;
        if (view != null)
        {
            Fit(view);
        }
    }

    private void OnValidate()
    {
        Paint();
    }

    private void OnWillRenderObject()
    {
        Camera view = Camera.current;
        if (view != null && view.transform == transform.parent)
        {
            Fit(view);
        }
    }

    public void Fit(Camera view)
    {
        if (quad != null && Mathf.Approximately(fittedAspect, view.aspect) && Mathf.Approximately(fittedFieldOfView, view.fieldOfView))
        {
            return;
        }

        fittedAspect = view.aspect;
        fittedFieldOfView = view.fieldOfView;
        if (quad == null)
        {
            quad = new Mesh { name = "PaperBackdrop", hideFlags = HideFlags.DontSave };
            GetComponent<MeshFilter>().sharedMesh = quad;
        }

        float halfHeight = Depth * Mathf.Tan(view.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float halfWidth = halfHeight * view.aspect;
        float tilesAcross = TilesPerHeight * view.aspect;
        quad.Clear();
        quad.vertices = new[]
        {
            new Vector3(-halfWidth, -halfHeight, Depth), new Vector3(-halfWidth, halfHeight, Depth),
            new Vector3(halfWidth, halfHeight, Depth), new Vector3(halfWidth, -halfHeight, Depth)
        };
        quad.uv = new[] { Vector2.zero, new Vector2(0.0f, TilesPerHeight), new Vector2(tilesAcross, TilesPerHeight), new Vector2(tilesAcross, 0.0f) };
        quad.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        quad.RecalculateBounds();
    }

    private void Paint()
    {
        if (texture == null)
        {
            texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, true)
            {
                name = "Paper",
                hideFlags = HideFlags.DontSave,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear
            };
        }

        Color32[] pixels = new Color32[TextureSize * TextureSize];
        for (int row = 0; row < TextureSize; row++)
        {
            for (int column = 0; column < TextureSize; column++)
            {
                float2 uv = new float2(column, row) / TextureSize;
                float shade = 1.0f + Texture(uv);
                pixels[row * TextureSize + column] = new Color(paper.r * shade, paper.g * shade, paper.b * shade, 1.0f);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(true);
        properties ??= new MaterialPropertyBlock();
        properties.SetTexture(BaseMapProperty, texture);
        GetComponent<MeshRenderer>().SetPropertyBlock(properties);
    }

    private static float Texture(float2 uv)
    {
        float cloud = Periodic(uv, new float2(CloudPeriod, CloudPeriod)) + 0.5f * Periodic(uv, new float2(CloudPeriod, CloudPeriod) * 2.0f);
        float fibres = Periodic(uv, new float2(FibreShort, FibreLong)) + Periodic(uv, new float2(FibreLong, FibreShort));
        float grain = Periodic(uv, new float2(GrainPeriod, GrainPeriod));
        return CloudDepth * cloud + FibreDepth * fibres + GrainDepth * grain;
    }

    private static float Periodic(float2 uv, float2 period)
    {
        return noise.pnoise(uv * period, period);
    }
}
