using Unity.Mathematics;
using UnityEngine;

public static class CurlField
{
    private const float Step = 0.01f;

    private static readonly float3 SecondOffset = new float3(31.4f, 17.3f, 8.9f);
    private static readonly float3 ThirdOffset = new float3(-12.7f, 45.1f, 23.6f);

    public static Vector3 Sample(Vector3 point)
    {
        float3 p = point;
        float3 dx = new float3(Step, 0.0f, 0.0f);
        float3 dy = new float3(0.0f, Step, 0.0f);
        float3 dz = new float3(0.0f, 0.0f, Step);

        float3 ddx = (Potential(p + dx) - Potential(p - dx)) / (2.0f * Step);
        float3 ddy = (Potential(p + dy) - Potential(p - dy)) / (2.0f * Step);
        float3 ddz = (Potential(p + dz) - Potential(p - dz)) / (2.0f * Step);
        return new Vector3(ddy.z - ddz.y, ddz.x - ddx.z, ddx.y - ddy.x);
    }

    private static float3 Potential(float3 p)
    {
        return new float3(noise.snoise(p), noise.snoise(p + SecondOffset), noise.snoise(p + ThirdOffset));
    }
}
