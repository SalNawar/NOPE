// The office PC's live desktop on its glass (PcScreenClone). The picture is
// projected across the glass's object space (two axes and a content rectangle
// set per glass), so the mesh's own UVs are never used and any CRT works;
// outside the fitted picture the glass shows _EdgeColor. Unlit: a lit screen.
Shader "TimeDesk/PlanarScreen"
{
    Properties
    {
        _BaseMap ("Desktop", 2D) = "black" {}
        _AxisU ("U axis (object space, to the viewer's right)", Vector) = (1, 0, 0, 0)
        _AxisV ("V axis (object space, up)", Vector) = (0, 1, 0, 0)
        _Rect ("Picture (centre u, centre v, width, height) in object units", Vector) = (0, 0, 1, 1)
        _Brightness ("Brightness", Float) = 1
        _EdgeColor ("Glass outside the picture", Color) = (0.015, 0.025, 0.025, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _AxisU;
            float4 _AxisV;
            float4 _Rect;
            float _Brightness;
            half4 _EdgeColor;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 plane : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.plane = float2(dot(input.positionOS.xyz, _AxisU.xyz), dot(input.positionOS.xyz, _AxisV.xyz));
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = (input.plane - _Rect.xy) / _Rect.zw + 0.5;
                if (any(uv < 0.0) || any(uv > 1.0))
                    return _EdgeColor;
                half3 colour = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).rgb;
                return half4(colour * _Brightness, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings DepthVert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half DepthFrag(Varyings input) : SV_Target
            {
                return input.positionCS.z;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma vertex NormalsVert
            #pragma fragment NormalsFrag
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_fragment _ _WRITE_RENDERING_LAYERS

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
            };

            Varyings NormalsVert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            void NormalsFrag(Varyings input, out half4 outNormalWS : SV_Target0
            #ifdef _WRITE_RENDERING_LAYERS
                , out uint outRenderingLayers : SV_Target1
            #endif
            )
            {
            #if defined(_GBUFFER_NORMALS_OCT)
                float3 normalWS = normalize(input.normalWS);
                float2 oct = saturate(PackNormalOctQuadEncode(normalWS) * 0.5 + 0.5);
                outNormalWS = half4(PackFloat2To888(oct), 0.0);
            #else
                outNormalWS = half4(NormalizeNormalPerPixel(input.normalWS), 0.0);
            #endif
            #ifdef _WRITE_RENDERING_LAYERS
                outRenderingLayers = EncodeMeshRenderingLayer();
            #endif
            }
            ENDHLSL
        }
    }
}
