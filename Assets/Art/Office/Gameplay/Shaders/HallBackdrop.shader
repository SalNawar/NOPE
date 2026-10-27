// The anime hall's lit painting (HallBackdrop): the 2D Renderer's picture of
// the hall's painted layers under its Light2Ds, drawn by the office camera as
// one full-screen triangle before anything else (the Background queue, no
// depth test or write), so the preserved 3D desk and every gameplay drawing
// land over it. The triangle's corners are placed in clip space (the mesh is
// the triangle itself), flipped as the camera's projection is when it renders
// into a texture; the picture is read by screen position. _Brightness scales
// the picture as it reads on screen (its sRGB values; 1 shows it as lit):
// HallBackdrop.Brightness, which HallLightingRig sets from the knobs'
// paintedDeskBrightness while the painted desk is installed (desk v3).
Shader "TimeDesk/HallBackdrop"
{
    Properties
    {
        [NoScaleOffset] _BackdropTex ("The lit hall", 2D) = "black" {}
        _Brightness ("Brightness on screen (the sRGB values' scale; 1: as lit)", Range(0, 2)) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Background" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" "PreviewType" = "Plane" }

        Pass
        {
            Name "Backdrop"
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            TEXTURE2D(_BackdropTex);
            SAMPLER(sampler_BackdropTex);

            CBUFFER_START(UnityPerMaterial)
                float _Brightness;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float2 ndc = input.positionOS.xy;
                output.uv = ndc * 0.5 + 0.5;
                output.positionCS = float4(ndc.x, ndc.y * _ProjectionParams.x, (UNITY_NEAR_CLIP_VALUE + UNITY_RAW_FAR_CLIP_VALUE) * 0.5, 1.0);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 hall = SAMPLE_TEXTURE2D(_BackdropTex, sampler_BackdropTex, input.uv).rgb;
                // The brightness scales what the screen shows (the sRGB values the
                // desk v3 review measured against the painting), not the light; at 1
                // the picture passes untouched.
                UNITY_BRANCH
                if (_Brightness != 1.0)
                {
                #ifdef UNITY_COLORSPACE_GAMMA
                    hall *= _Brightness;
                #else
                    hall = SRGBToLinear(LinearToSRGB(hall) * _Brightness);
                #endif
                }
                return half4(hall, 1.0);
            }
            ENDHLSL
        }
    }
}
