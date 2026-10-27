// The hover outline on office objects (HoverHighlighter): a rim of one flat
// colour around the object's silhouette, drawn in two steps through
// Graphics.RenderMesh, after the opaque office, never depth-tested against
// the desk (the review of the desk v3 capture: the felt and the shell cut the
// depth-tested rim into dashes and stipples; a pull toward the camera would
// fill thin parts with the rim's colour):
//   1. the mask (_Mask 1, colour off, queue Transparent+49): the object's
//      mesh as it is, both faces, writes the outline's stencil bit over its
//      whole silhouette, whatever stands in front of it (the stencil bit
//      of value 8, bit 3: one of the user bits 0 to 3 URP never writes);
//   2. the hull (_Mask 0, queue Transparent+50): the mesh again, both faces,
//      pushed out along its normals, drawn only where the stencil bit is not
//      set, so only the rim around the silhouette shows, never over the
//      object itself, not even across a part thinner than the rim.
// With _WidthPx over 0 the rim is that many screen px at 1080p (scaled by the
// screen's height) for every object, near or far, big or small (3-4 px on
// BABEL against 1 px on the intercom before); else _Width metres. Its colour's
// alpha fades it in and out (HoverHighlighter, HoverFade: it never pops).
Shader "TimeDesk/HoverHull"
{
    Properties
    {
        _Color ("Colour", Color) = (1, 1, 1, 1)
        _Width ("Width (metres)", Float) = 0.006
        _WidthPx ("Width (screen px at 1080p; over 0 it replaces the metres)", Float) = 0
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
                float _Width, _WidthPx, _Mask;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
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
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                if (_WidthPx <= 0.0)
                {
                    output.positionCS = TransformWorldToHClip(positionWS + normalWS * _Width);
                    return output;
                }
                float4 positionCS = TransformWorldToHClip(positionWS);
                float2 normalCS = mul((float3x3)GetWorldToHClipMatrix(), normalWS).xy;
                float length2 = dot(normalCS, normalCS);
                float2 toward = length2 > 1e-10 ? normalCS * rsqrt(length2) : float2(0.0, 0.0);
                float2 ndc = toward * (_WidthPx * 2.0 / 1080.0);
                ndc.x *= _ScreenParams.y / max(_ScreenParams.x, 1.0);
                positionCS.xy += ndc * positionCS.w;
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
