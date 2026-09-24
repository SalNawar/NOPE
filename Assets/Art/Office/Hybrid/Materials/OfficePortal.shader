Shader "TimeSorter/OfficePortal"
{
    Properties
    {
        _DeepColor ("Deep colour", Color) = (0.035, 0.11, 0.13, 1)
        _FlowColor ("Flow colour", Color) = (0.10, 0.27, 0.28, 1)
        _RimColor ("Edge colour", Color) = (0.25, 0.53, 0.46, 1)
        _Speed ("Flow speed", Range(0, 2)) = 0.35
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite On
        Pass
        {
            Name "PortalSurface"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _DeepColor;
                half4 _FlowColor;
                half4 _RimColor;
                float _Speed;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.uv * 2.0 - 1.0;
                float radius = length(p);
                float phase = _Time.y * _Speed;
                // Broad drifting folds, with no concentric target pattern.
                float2 drift = p + float2(sin(p.y * 3.1 + phase),
                                          cos(p.x * 2.7 - phase * 0.7)) * 0.16;
                float field = sin(drift.x * 3.5 + drift.y * 2.1 - phase)
                            + sin(drift.y * 4.0 - drift.x * 1.3 + phase * 0.6) * 0.45;
                float flow = smoothstep(-0.3, 0.0, field) * 0.34
                           + smoothstep(0.6, 0.85, field) * 0.22;
                float edge = smoothstep(0.93, 1.0, radius);
                half3 colour = lerp(_DeepColor.rgb, _FlowColor.rgb, flow);
                colour = lerp(colour, _RimColor.rgb, edge * 0.45);
                return half4(colour, 1);
            }
            ENDHLSL
        }
    }
}
