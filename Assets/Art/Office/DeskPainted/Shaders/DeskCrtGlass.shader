// The painted PC's CRT glass over the desktop's clone (desk v3, round 12:
// "a subtle phosphor/scanline look"). Drawn on the curved CRT face
// (CrtFace.Dome, its UVs 0 to 1 across the glass) a hair in front of the
// glass that shows the desktop (TimeDesk/PlanarScreen, UI-PC's), in two
// passes: a multiply (the phosphor's tint, the scanlines and the tube's edge
// falloff) and an add (a soft sheen of the room on the curved glass, strongest
// at the top right, as the painting draws it). The scanlines fade out where they would be finer than
// about two screen pixels (fwidth), so they never shimmer or moire at a
// smaller resolution. With the screen off (the art's black glass under it) it
// reads as a dark curved tube with its sheen.
Shader "NOPE/Desk CRT Glass"
{
    Properties
    {
        _Tint ("Phosphor tint (multiplies the picture)", Color) = (0.93, 1.0, 0.95, 1)
        _ScanLines ("Scanlines across the glass's height", Float) = 72
        _ScanDepth ("Scanline depth (0: none)", Range(0, 0.5)) = 0.16
        _EdgeFalloff ("The tube's edge falloff (0: none)", Range(0, 1)) = 0.45
        _SheenColor ("Sheen (added)", Color) = (0.85, 0.9, 0.86, 1)
        _Sheen ("Sheen strength", Range(0, 0.3)) = 0.06
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        ZWrite Off
        ZTest LEqual
        Cull Back

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _Tint;
            float _ScanLines;
            half _ScanDepth;
            half _EdgeFalloff;
            half4 _SheenColor;
            half _Sheen;
        CBUFFER_END

        struct Attributes
        {
            float4 positionOS : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 uv : TEXCOORD0;
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;
            output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
            output.uv = input.uv;
            return output;
        }

        // 1 at the glass's middle, falling toward its rim (a CRT tube is darker at its edges).
        half Falloff(float2 uv)
        {
            float2 c = abs(uv - 0.5) * 2.0;
            float r = saturate(max(c.x, c.y) * 0.65 + length(c) * 0.35);
            return 1.0 - _EdgeFalloff * r * r * r;
        }
        ENDHLSL

        // The multiply: tint, scanlines, edge falloff.
        Pass
        {
            Name "Multiply"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Blend DstColor Zero

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            half4 Frag(Varyings input) : SV_Target
            {
                float lines = input.uv.y * _ScanLines;
                // How many screen pixels one scanline spans: below about two, fade the lines out (no moire).
                float perPixel = fwidth(lines);
                half fade = saturate(1.6 - perPixel * 2.0);
                half scan = 1.0 - _ScanDepth * fade * (0.5 + 0.5 * cos(lines * 6.2831853));
                half3 m = _Tint.rgb * scan * Falloff(input.uv);
                return half4(m, 1.0);
            }
            ENDHLSL
        }

        // The add: the room's soft sheen on the curved glass.
        Pass
        {
            Name "Sheen"
            Tags { "LightMode" = "UniversalForward" }
            Blend One One

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.uv - float2(0.74, 0.8);
                half band = exp(-dot(p * float2(1.6, 3.2), p * float2(1.6, 3.2)) * 3.0);
                half rim = smoothstep(0.82, 1.0, max(abs(input.uv.x - 0.5), abs(input.uv.y - 0.5)) * 2.0) * 0.35;
                return half4(_SheenColor.rgb * _Sheen * (band + rim), 0.0);
            }
            ENDHLSL
        }
    }
}
