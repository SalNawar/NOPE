// The hover outline on office props (HoverHighlighter; Saleh's D15: papers lift, props get an outline).
// A fixed-pixel rim round the prop's silhouette, drawn only on the camera the pointer looks through
// (BUG-10a: the old hull was pushed out along the normals in metres, so it shattered on split normals,
// showed on every camera and grew with distance). Two draws of the prop's meshes, both through
// Graphics.RenderMesh after the opaque office:
// - the mask (_WidthPx 0, ColorMask 0, the stencil bit written: Comp Always, Pass Replace), so the
//   prop's own pixels are marked;
// - the rim (the same meshes pushed out in screen space by _WidthPx pixels from the mesh's centre
//   _Centre, which split normals cannot tear apart, drawn only where the stencil bit is not set:
//   Comp NotEqual), so only a band round the outside shows, as wide on every prop and at every
//   distance.
// The render state comes from the material, so HoverHighlighter makes one material of each.
Shader "TimeDesk/HoverHull"
{
    Properties
    {
        _Color ("Colour", Color) = (1, 1, 1, 1)
        _WidthPx ("Width (pixels; 0 for the mask)", Float) = 2
        _Centre ("The mesh's centre (object space; the rim pushes out from it)", Vector) = (0, 0, 0, 0)
        _StencilRef ("Stencil bit", Float) = 128
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil compare", Float) = 8
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilPass ("Stencil pass", Float) = 2
        _ColorMask ("Colour mask (0 for the mask)", Float) = 15
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent+50" }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Off
            ZWrite Off
            ZTest LEqual
            ColorMask [_ColorMask]
            Stencil
            {
                Ref [_StencilRef]
                ReadMask [_StencilRef]
                WriteMask [_StencilRef]
                Comp [_StencilComp]
                Pass [_StencilPass]
            }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _WidthPx;
                float4 _Centre;
                float _StencilRef;
                float _StencilComp;
                float _StencilPass;
                float _ColorMask;
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
                float4 clip = TransformObjectToHClip(input.positionOS.xyz);
                if (_WidthPx > 0)
                {
                    // out from the mesh's centre on the screen, by a fixed number of pixels
                    float4 centre = TransformObjectToHClip(_Centre.xyz);
                    float2 screen = _ScreenParams.xy;
                    float2 away = (clip.xy / max(clip.w, 1e-5) - centre.xy / max(centre.w, 1e-5)) * screen;
                    float len = length(away);
                    float2 dir = len > 1e-4 ? away / len : float2(0, 0);
                    clip.xy += dir * _WidthPx * 2.0 / screen * clip.w;
                }
                output.positionCS = clip;
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
