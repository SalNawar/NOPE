// The anime hall's lit painting (HallBackdrop): the 2D Renderer's picture of
// the hall's painted layers under its Light2Ds, drawn by the office camera as
// one full-screen triangle before anything else (the Background queue, no
// depth test or write), so the preserved 3D desk and every gameplay drawing
// land over it. The triangle's corners are placed in clip space (the mesh is
// the triangle itself), flipped as the camera's projection is when it renders
// into a texture; the picture is read by screen position.
Shader "TimeDesk/HallBackdrop"
{
    Properties
    {
        [NoScaleOffset] _BackdropTex ("The lit hall", 2D) = "black" {}
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

            TEXTURE2D(_BackdropTex);
            SAMPLER(sampler_BackdropTex);

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
                return half4(SAMPLE_TEXTURE2D(_BackdropTex, sampler_BackdropTex, input.uv).rgb, 1.0);
            }
            ENDHLSL
        }
    }
}
