// The soft contact shadow under each desk document (PaperDepth.Shadow; the documents themselves
// are modelled, DeskPaperDepth and TimeDesk/DocumentCel): a runtime mesh whose vertex colours
// (authored in sRGB, their alpha the shadow's darkness) are the whole look. Unlit, so no hour's
// light ever crosses the text: the shadow is drawn, not lit. Paper_Shadow blends it over what lies
// under it without writing depth; the blend and depth-write are properties so one shader serves.
Shader "TimeDesk/PaperDepth"
{
    Properties
    {
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Source blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination blend", Float) = 0
        [Enum(Off, 0, On, 1)] _ZWrite ("Depth write", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            Name "PaperDepth"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Off
            ZWrite [_ZWrite]
            ZTest LEqual
            Blend [_SrcBlend] [_DstBlend]

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 c = input.color;
                #if !defined(UNITY_COLORSPACE_GAMMA)
                c.rgb = SRGBToLinear(c.rgb);
                #endif
                return c;
            }
            ENDHLSL
        }
    }
}
