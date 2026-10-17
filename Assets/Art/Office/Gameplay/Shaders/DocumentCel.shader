// The desk documents' modelled bodies (Saleh 2026-10-17: "use canva and blender to make actual
// 3d documents"; the models in Assets/Art/Office/Gameplay/Documents): their art (or the edge atlas)
// times their baked shading (vertex colours, and the shadows each model casts on itself: _ShadeMap,
// baked by the same script), in the 80s/90s anime cel look: three hard tones from a fixed
// light at the top left of the view (lit, shade, dark; purple-tinted, never grey), so a face turned
// to the camera always shows its art at full strength and a document's sides and folds fall into
// its shade. Unlit by the hall: no hour's light or real-time shadow ever crosses the printed text.
// Drawn from both sides (the normal turned to the viewer), opaque, writing depth; an optional
// alpha cut-out (_Cutoff above 0) for art drawn on a cut-out.
Shader "TimeDesk/DocumentCel"
{
    Properties
    {
        _BaseMap ("Art", 2D) = "white" {}
        _BaseColor ("Tint", Color) = (1, 1, 1, 1)
        _ShadeMap ("Baked shadows (multiplied, on the mesh's UVs)", 2D) = "white" {}
        _ShadeColor ("Shade tone", Color) = (0.84, 0.79, 0.9, 1)
        _DarkColor ("Dark tone", Color) = (0.62, 0.55, 0.72, 1)
        _Cutoff ("Alpha cut-out (0: none)", Range(0, 1)) = 0
        _HoleColor ("Colour where the art is clear (no cut-out)", Color) = (0.68, 0.53, 0.36, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "DocumentCel"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_ShadeMap);
            SAMPLER(sampler_ShadeMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _ShadeColor;
                half4 _DarkColor;
                half _Cutoff;
                half4 _HoleColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalVS : TEXCOORD1;
                float2 meshUv : TEXCOORD2;
                half4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.meshUv = input.uv;
                output.normalVS = TransformWorldToViewDir(TransformObjectToWorldNormal(input.normalOS));
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 art = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                if (_Cutoff > 0)
                    clip(art.a - _Cutoff);
                else if (art.a < 0.5)
                    art.rgb = _HoleColor.rgb; // a model's edge over the art's clear margin: the card's own colour, not the clear texels' leftovers
                float3 n = normalize(input.normalVS);
                // The side turned to the viewer (view space looks down -z: a face toward the camera has n.z > 0).
                if (n.z < 0)
                    n = -n;
                // The light from the top left of the view and from the camera: a face toward the camera is lit.
                float ndl = dot(n, normalize(float3(-0.35, 0.35, 0.87)));
                half3 tone = ndl > 0.62 ? half3(1, 1, 1) : (ndl > 0.25 ? _ShadeColor.rgb : _DarkColor.rgb);
                half3 baked = SAMPLE_TEXTURE2D(_ShadeMap, sampler_ShadeMap, input.meshUv).rgb;
                return half4(art.rgb * input.color.rgb * baked * tone, 1);
            }
            ENDHLSL
        }
    }
}
