Shader "Custom/ButterflyFlat"
{
    Properties
    {
        _LineWidth ("Line Width", Range(0.0005, 0.02)) = 0.0021
        _OutlineDistance ("Outline Reference Distance", Range(0.5, 12)) = 3.9
        _OutlineFloor ("Outline Thin Limit", Range(0.2, 1)) = 0.66
        _OutlineCeiling ("Outline Thick Limit", Range(1, 2)) = 1.22
        _OutlineWobble ("Outline Wobble", Range(0, 0.6)) = 0.22
        _InkRecess ("Ink Recess", Range(0, 0.02)) = 0.0007
        _LightView ("Light Direction (view space)", Vector) = (-0.55, 0.65, 0.52, 0)
        _ShadowTint ("Shadow Multiplier (linear)", Vector) = (0.42, 0.40, 0.52, 0)
        _ShadeThreshold ("Shade Threshold", Range(-1, 1)) = 0.12
        _ShadeSoftness ("Shade Softness", Range(0.001, 0.3)) = 0.07
        _ShadeStrength ("Shade Strength", Range(0, 1)) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _InkCull ("Ink Cull", Float) = 0
        [NoScaleOffset] _PatternTex ("Pattern", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite On
        ZTest LEqual

        Pass
        {
            Name "ButterflyInk"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull [_InkCull]

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma target 3.5
            #define BUTTERFLY_INK_PASS 1
            #include "ButterflyFlatCore.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ButterflyFill"
            Tags { "LightMode" = "UniversalForward" }
            Offset -1, -1

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma target 3.5
            #include "ButterflyFlatCore.hlsl"
            ENDHLSL
        }
    }
}
