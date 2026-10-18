// The desk documents' modelled bodies (Saleh 2026-10-17: "use canva and blender to make actual
// 3d documents"; the models in Assets/Art/Office/Gameplay/Documents): their art (or the edge atlas)
// times their baked shading (vertex colours, and the shadows each model casts on itself: _ShadeMap,
// baked by the same script), in the 80s/90s anime cel look: three tones (lit, shade, dark;
// purple-tinted, never grey) from one light fixed over the desk at the top left of the view
// (_LightDir, in world space: Saleh's 1018a playtest, "weird lighting issue when you move
// documents": the light used to turn with the camera, so the office view's low camera put every
// face on the edge of its shade band and a paper's few degrees of tilt, or the camera tilting into
// the reading view, flipped it between tones). A face lying on the desk is always lit, whatever the
// camera or the hour; sides and folds fall into its shade. The tones meet in a narrow soft edge
// (_Soft), so a curl or a bend crossing one never flickers. Unlit by the hall: no hour's light or
// real-time shadow ever crosses the printed text. Drawn from both sides (the normal turned to the
// viewer), opaque, writing depth; an optional alpha cut-out (_Cutoff above 0) for art drawn on a
// cut-out. While it moves the paper bends (PaperBend.hlsl: its bow, twist and ripple, as much as its
// vertex alpha lets each part: stiff boards 0, loose pages 1), its tones staying its shape's at rest and
// a soft airbrushed sheen showing the bend (_BendSheen: from the bent normal).
Shader "TimeDesk/DocumentCel"
{
    Properties
    {
        _BaseMap ("Art", 2D) = "white" {}
        _BaseColor ("Tint", Color) = (1, 1, 1, 1)
        _ShadeMap ("Baked shadows (multiplied, on the mesh's UVs)", 2D) = "white" {}
        _ShadeColor ("Shade tone", Color) = (0.84, 0.79, 0.9, 1)
        _DarkColor ("Dark tone", Color) = (0.62, 0.55, 0.72, 1)
        _LightDir ("Light (world: toward it; the top left of the view, over the desk)", Vector) = (-0.3, 0.9, 0.3, 0)
        _LitAt ("Lit above (light along the normal)", Range(0, 1)) = 0.62
        _ShadeAt ("Shade above (else dark)", Range(0, 1)) = 0.25
        _Soft ("The tones' soft edge (half width)", Range(0.001, 0.2)) = 0.04
        _BendSheen ("Airbrushed sheen along a bend", Range(0, 3)) = 0.9
        _Cutoff ("Alpha cut-out (0: none)", Range(0, 1)) = 0
        _Flex ("Bend: travel (x, y), bow (m), lag", Vector) = (1, 0, 0, 0)
        _Flex2 ("Bend: twist (m), ripple (m), its phase, its half waves", Vector) = (0, 0, 0, 0)
        _FlexSize ("The paper's size (m)", Vector) = (1, 1, 0, 0)
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
            #include "PaperBend.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_ShadeMap);
            SAMPLER(sampler_ShadeMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _ShadeColor;
                half4 _DarkColor;
                float4 _LightDir;
                half _LitAt;
                half _ShadeAt;
                half _Soft;
                half _BendSheen;
                half _Cutoff;
                half4 _HoleColor;
                float4 _Flex;
                float4 _Flex2;
                float4 _FlexSize;
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
                float3 normalWS : TEXCOORD1;
                float2 meshUv : TEXCOORD2;
                float3 flatNormalWS : TEXCOORD3;
                float3 positionWS : TEXCOORD4;
                half4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                // The bend, as much as the part's vertex alpha lets it (PaperBend.hlsl; flat at rest).
                float3 p = input.positionOS.xyz;
                float2 slope;
                float depth = PaperBendDepth(p.xy, _Flex, _Flex2, _FlexSize.xy, slope) * input.color.a;
                slope *= input.color.a;
                p.z += depth;
                // the bent normal: a face's normal (0, 0, -1) tilts to (slope, -1); sides barely change
                float3 n = input.normalOS;
                float3 bent = normalize(n + float3(slope * -n.z, 0));
                output.positionWS = TransformObjectToWorld(p);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.meshUv = input.uv;
                output.normalWS = TransformObjectToWorldNormal(bent);
                output.flatNormalWS = TransformObjectToWorldNormal(n);
                output.color = input.color;
                return output;
            }

            // The light along the normal turned to the viewer.
            float Facing(float3 normal, float3 view, float3 light)
            {
                float3 n = normalize(normal);
                if (dot(n, view) < 0)
                    n = -n;
                return dot(n, light);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 art = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                if (_Cutoff > 0)
                    clip(art.a - _Cutoff);
                else if (art.a < 0.5)
                    art.rgb = _HoleColor.rgb; // a model's edge over the art's clear margin: the card's own colour, not the clear texels' leftovers
                float3 view = normalize(GetWorldSpaceViewDir(input.positionWS));
                float3 light = normalize(_LightDir.xyz);
                // Three cel tones from the model's own shape (its normal at rest: a bend never flips a face into
                // shade), their edges a narrow soft step: lit, shade, dark.
                float flat = Facing(input.flatNormalWS, view, light);
                half lit = smoothstep(_LitAt - _Soft, _LitAt + _Soft, flat);
                half shade = smoothstep(_ShadeAt - _Soft, _ShadeAt + _Soft, flat);
                half3 tone = lerp(_DarkColor.rgb, lerp(_ShadeColor.rgb, half3(1, 1, 1), lit), shade);
                // A soft airbrushed sheen along a bend: a little darker where it turns from the light, a touch brighter toward it.
                float ndl = Facing(input.normalWS, view, light);
                half sheen = 1 + clamp((ndl - flat) * _BendSheen, -0.1, 0.06);
                half3 baked = SAMPLE_TEXTURE2D(_ShadeMap, sampler_ShadeMap, input.meshUv).rgb;
                return half4(art.rgb * input.color.rgb * baked * tone * sheen, 1);
            }
            ENDHLSL
        }
    }
}
