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
                // Moving, layered folds in a liquid membrane. Broad colour masses
                // and two restrained crests stay readable at the office camera.
                float2 drift = p + float2(sin(p.y * 3.2 + phase * 0.6),
                                          cos(p.x * 2.5 - phase * 0.4)) * 0.20;
                float bend = drift.x * 3.1 + drift.y * 2.7
                           + sin(drift.y * 3.6 - phase * 0.35) * 0.65;
                float fold = sin(bend - phase);
                float secondFold = sin(bend * 1.67 + drift.y * 1.2 + phase * 0.55);
                float flow = smoothstep(-0.65, 0.5, fold) * 0.48
                           + smoothstep(0.1, 0.8, secondFold) * 0.20;
                float crest = pow(saturate(fold), 24.0) * 0.11
                            + pow(saturate(secondFold), 36.0) * 0.035;
                float edge = smoothstep(0.83, 1.0, radius);
                float innerGlow = smoothstep(0.60, 0.98, radius)
                                * (0.65 + 0.35 * sin(p.y * 3.0 + phase));
                half3 colour = lerp(_DeepColor.rgb, _FlowColor.rgb, flow);
                colour = lerp(colour, _RimColor.rgb, crest * (1.0 - edge));
                colour += _FlowColor.rgb * innerGlow * 0.16;
                colour = lerp(colour, _RimColor.rgb, edge * 0.64);
                return half4(colour, 1);
            }
            ENDHLSL
        }
    }
}
