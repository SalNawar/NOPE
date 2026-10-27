// The hover outline on office props (HoverHighlighter; Saleh's D15: papers lift, props get an outline):
// a rim of one flat colour around the prop's silhouette, the same screen px on every prop and at
// every distance, drawn in two steps through Graphics.RenderMesh after the opaque office, on the
// camera the pointer looks through alone (BUG-10a), never depth-tested against the desk (the review
// of the desk v3 capture: the felt and the shell cut a depth-tested rim into dashes and stipples; a
// pull toward the camera would fill thin parts with the rim's colour):
//   1. the mask (_Mask 1, colour off, queue Transparent+49): the prop's mesh as it is, both faces,
//      writes the outline's stencil bit over its whole silhouette, whatever stands in front of it
//      (the stencil bit of value 8, bit 3: one of the user bits 0 to 3 URP never writes);
//   2. the hull (_Mask 0, queue Transparent+50): the mesh again, both faces, pushed out in screen
//      space by _WidthPx pixels at 1080p (scaled by the screen's height) away from the mesh's centre
//      _Centre (BUG-10a: a push along the normals shattered on split normals, and a normal facing the
//      camera gives a flat painted part no rim at all), drawn only where the stencil bit is not set,
//      so only the rim around the silhouette shows, never over the prop itself, not even across a
//      part thinner than the rim.
// Its colour's alpha fades it in and out (HoverHighlighter, HoverFade: it never pops). The render
// state comes from the material, so HoverHighlighter makes one material of each (Stencil).
Shader "TimeDesk/HoverHull"
{
    Properties
    {
        _Color ("Colour", Color) = (1, 1, 1, 1)
        _WidthPx ("Width (screen px at 1080p, scaled by the screen's height)", Float) = 2
        _Centre ("The mesh's centre (object space; the hull pushes out from it)", Vector) = (0, 0, 0, 0)
        [ToggleUI] _Mask ("Mask: the silhouette into the stencil, no colour, no push", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil compare (the hull: NotEqual; the mask: Always)", Float) = 6
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilPass ("Stencil pass (the hull: Keep; the mask: Replace)", Float) = 0
        _ColorMask ("Colour mask (the hull: 15; the mask: 0)", Float) = 15
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent+50" }

        Pass
        {
            Name "Hull"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Off
            ZWrite Off
            ZTest Always
            ColorMask [_ColorMask]
            Blend SrcAlpha OneMinusSrcAlpha
            Stencil
            {
                Ref 8
                ReadMask 8
                WriteMask 8
                Comp [_StencilComp]
                Pass [_StencilPass]
            }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _WidthPx, _Mask;
                float4 _Centre;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                if (_Mask > 0.5)
                {
                    output.positionCS = TransformWorldToHClip(positionWS);
                    return output;
                }
                float4 positionCS = TransformWorldToHClip(positionWS);
                if (_WidthPx > 0.0)
                {
                    // Out from the mesh's centre on the screen, by a fixed number of px at 1080p.
                    float4 centreCS = TransformObjectToHClip(_Centre.xyz);
                    float2 screen = _ScreenParams.xy;
                    float2 away = (positionCS.xy / max(positionCS.w, 1e-5) - centreCS.xy / max(centreCS.w, 1e-5)) * screen;
                    float length2 = dot(away, away);
                    float2 toward = length2 > 1e-8 ? away * rsqrt(length2) : float2(0.0, 0.0);
                    float px = _WidthPx * screen.y / 1080.0;
                    positionCS.xy += toward * px * 2.0 / screen * positionCS.w;
                }
                output.positionCS = positionCS;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return _Color;
            }
            ENDHLSL
        }
    }
}
