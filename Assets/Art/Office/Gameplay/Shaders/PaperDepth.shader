// The soft contact shadow under each desk document (PaperDepth.Shadow; the documents themselves
// are modelled, DeskPaperDepth and TimeDesk/DocumentCel): a runtime mesh whose vertex colours
// (authored in sRGB, their alpha the shadow's darkness) are the whole look. Unlit, so no hour's
// light ever crosses the text: the shadow is drawn, not lit. Paper_Shadow blends it over what lies
// under it without writing depth; the blend and depth-write are properties so one shader serves.
// While its paper bends (PaperBend.hlsl, _Flex*: Saleh's 1018a playtest) it bends with it, so the
// paper's drooping face never dips through its own shadow.
Shader "TimeDesk/PaperDepth"
{
    Properties
    {
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Source blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination blend", Float) = 0
        [Enum(Off, 0, On, 1)] _ZWrite ("Depth write", Float) = 1
        _Flex ("Bend: travel (x, y), bow (m), lag", Vector) = (1, 0, 0, 0)
        _Flex2 ("Bend: twist (m), ripple (m), its phase, its half waves", Vector) = (0, 0, 0, 0)
        _FlexSize ("The paper's size (m), and how far the shadow reaches beside it (m)", Vector) = (1, 1, 0, 0)
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
            #include "PaperBend.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Flex;
                float4 _Flex2;
                float4 _FlexSize;
            CBUFFER_END

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
                float3 p = input.positionOS.xyz;
                float2 slope;
                p.z += PaperBendDepth(p.xy, _Flex, _Flex2, _FlexSize.xy, slope);
                // its corners lie up to _FlexSize.z metres beside the face over them (its spread and lean): it sinks by as
                // much as the bend's slope rises over that, so a steep bend never lifts it through the face
                p.z += length(slope / max(_FlexSize.xy, 1e-4)) * _FlexSize.z;
                output.positionCS = TransformObjectToHClip(p);
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
