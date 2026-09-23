#ifndef BUTTERFLY_FLAT_CORE_INCLUDED
#define BUTTERFLY_FLAT_CORE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#define KIND_CARD 0.5
#define KIND_STEM 1.5
#define KIND_BILLBOARD 2.5
#define PROBE_STEP 0.002
#define EDGE_ON_FLOOR 0.22
#define EDGE_ON_RANGE 0.45
#define EDGE_ON_MIN_COSINE 0.12
#define SHADE_SURFACE 0.5
#define SHADE_SPHERE 1.5
#define SHADE_TUBE 2.5
#define TUBE_ROUNDNESS 1.3

CBUFFER_START(UnityPerMaterial)
    float _LineWidth;
    float _OutlineDistance;
    float _OutlineFloor;
    float _OutlineCeiling;
    float _OutlineWobble;
    float _InkRecess;
    float4 _LightView;
    float4 _ShadowTint;
    float _ShadeThreshold;
    float _ShadeSoftness;
    float _ShadeStrength;
CBUFFER_END

TEXTURE2D(_PatternTex);
SAMPLER(sampler_PatternTex);

struct Attributes
{
    float4 positionOS : POSITION;
    float3 expandOS : NORMAL;
    float4 tangentOS : TANGENT;
    float4 color : COLOR;
    float4 stroke : TEXCOORD0;
    float4 ink : TEXCOORD1;
    float3 facing : TEXCOORD2;
    float4 shading : TEXCOORD3;
    float3 pattern : TEXCOORD4;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float4 color : COLOR;
    float4 normalWS : TEXCOORD0;
    float3 pattern : TEXCOORD1;
};

float2 ScreenDirection(float3 positionWS, float3 directionWS, float4 clip)
{
    float4 shifted = TransformWorldToHClip(positionWS + directionWS * PROBE_STEP);
    float2 delta = shifted.xy / max(shifted.w, 1e-5) - clip.xy / max(clip.w, 1e-5);
    delta *= float2(_ScreenParams.x, _ScreenParams.y);

    float length2 = dot(delta, delta);
    return (length2 > 1e-12) ? delta * rsqrt(length2) : float2(0.0, 0.0);
}

float StrokeWobble(float3 positionOS)
{
    float noise = frac(sin(dot(positionOS, float3(12.9898, 78.233, 37.719))) * 43758.5453);
    return 1.0 + (noise - 0.5) * _OutlineWobble;
}

float FacingCosine(float3 facingOS, float3 eyeDirection)
{
    if (dot(facingOS, facingOS) < 0.25)
    {
        return 1.0;
    }

    float3 facingWS = normalize(TransformObjectToWorldDir(facingOS, false));
    return abs(dot(facingWS, eyeDirection));
}

float4 ShadingNormal(float4 shading, float3 expandWS, float3 eyeDirection)
{
    if (shading.w < SHADE_SURFACE)
    {
        return float4(eyeDirection, 0.0);
    }

    if (shading.w < SHADE_SPHERE)
    {
        float3 normalWS = normalize(TransformObjectToWorldDir(shading.xyz, false));
        return float4(dot(normalWS, eyeDirection) < 0.0 ? -normalWS : normalWS, 1.0);
    }

    if (shading.w < SHADE_TUBE)
    {
        float3 right = UNITY_MATRIX_I_V._m00_m10_m20;
        float3 up = UNITY_MATRIX_I_V._m01_m11_m21;
        float2 offset = shading.xy;
        return float4(right * offset.x + up * offset.y + eyeDirection * sqrt(saturate(1.0 - dot(offset, offset))), 1.0);
    }

    return float4(normalize(expandWS * TUBE_ROUNDNESS + eyeDirection), 1.0);
}

Varyings Vertex(Attributes input)
{
    float kind = input.stroke.x;
    float baseWidth = input.stroke.y;
    float outlineWeight = input.stroke.z;
    float depthBias = input.stroke.w;

    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    float3 expandWS = float3(0.0, 0.0, 0.0);

    if (kind < KIND_CARD)
    {
        expandWS = TransformObjectToWorldDir(input.expandOS, false);
    }
    else if (kind < KIND_STEM || kind > KIND_BILLBOARD)
    {
        float3 tangentWS = TransformObjectToWorldDir(input.tangentOS.xyz, false);
        float3 toCamera = normalize(GetCameraPositionWS() - positionWS);
        float3 side = cross(tangentWS, toCamera);
        float sideLength = length(side);
        expandWS = (sideLength > 1e-5) ? (side / sideLength) * input.tangentOS.w : float3(0.0, 0.0, 0.0);
    }
    else
    {
        float3 right = UNITY_MATRIX_I_V._m00_m10_m20;
        float3 up = UNITY_MATRIX_I_V._m01_m11_m21;
        positionWS += right * input.expandOS.x + up * input.expandOS.y;
        expandWS = right * input.tangentOS.x + up * input.tangentOS.y;
    }

    float3 toEye = GetCameraPositionWS() - positionWS;
    float eyeDistance = max(length(toEye), 1e-4);
    float3 eyeDirection = toEye / eyeDistance;
    positionWS += eyeDirection * depthBias;

    float width = kind > KIND_BILLBOARD ? baseWidth * abs(UNITY_MATRIX_P._m11) / (2.0 * eyeDistance) : baseWidth;
#ifdef BUTTERFLY_INK_PASS
    float facingCosine = FacingCosine(input.facing, eyeDirection);
    float attenuation = clamp(_OutlineDistance / eyeDistance, _OutlineFloor, _OutlineCeiling);
    width += _LineWidth * outlineWeight * attenuation * StrokeWobble(input.positionOS.xyz)
           * lerp(EDGE_ON_FLOOR, 1.0, saturate(facingCosine / EDGE_ON_RANGE));

    float lineWorld = width * eyeDistance * 2.0 / abs(UNITY_MATRIX_P._m11);
    float slope = sqrt(saturate(1.0 - facingCosine * facingCosine)) / max(facingCosine, EDGE_ON_MIN_COSINE);
    positionWS -= eyeDirection * (_InkRecess + lineWorld * slope);
#endif

    float4 clip = TransformWorldToHClip(positionWS);

    if (width > 0.0 && dot(expandWS, expandWS) > 1e-12)
    {
        float2 direction = ScreenDirection(positionWS, expandWS, clip);
        float pixels = width * _ScreenParams.y;
        clip.xy += direction * pixels * float2(2.0 / _ScreenParams.x, 2.0 / _ScreenParams.y) * clip.w;
    }

    Varyings output;
    output.positionCS = clip;
    output.pattern = input.pattern;
#ifdef BUTTERFLY_INK_PASS
    output.color = input.ink;
    output.normalWS = float4(eyeDirection, 0.0);
#else
    output.color = input.color;
    output.normalWS = ShadingNormal(input.shading, expandWS, eyeDirection);
#endif
    return output;
}

float4 Fragment(Varyings input) : SV_Target
{
    float3 colour = input.color.rgb;
#ifndef BUTTERFLY_INK_PASS
    float3 painted = SAMPLE_TEXTURE2D(_PatternTex, sampler_PatternTex, input.pattern.xy).rgb;
    colour = lerp(colour, painted, input.pattern.z);
    float3 right = UNITY_MATRIX_I_V._m00_m10_m20;
    float3 up = UNITY_MATRIX_I_V._m01_m11_m21;
    float3 back = UNITY_MATRIX_I_V._m02_m12_m22;
    float3 light = normalize(right * _LightView.x + up * _LightView.y + back * _LightView.z);

    float lit = dot(normalize(input.normalWS.xyz), light);
    float band = smoothstep(_ShadeThreshold - _ShadeSoftness, _ShadeThreshold + _ShadeSoftness, lit);
    float3 shaded = lerp(colour * _ShadowTint.rgb, colour, band);
    colour = lerp(colour, shaded, _ShadeStrength * saturate(input.normalWS.w));
#endif
    return float4(colour, 1.0);
}

#endif
