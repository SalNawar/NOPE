// The portal rings' effects (the portals spec v3 VX1, VX5: PortalEffect): a
// sprite drawn unlit, so an open ring glows through the hall's evening, and
// premultiplied: where it is opaque it covers the floor seen through the ring
// with its colour, brightened by _Boost (light added, as a glow), so the tint
// reads over the hall's light floor (a plain additive glow washes to white
// there); the SpriteRenderer's colour tints it (its alpha scales it). No depth
// write; it depth-tests, so the preserved 3D desk in front hides it. Two
// passes of the same code: UniversalForward for the office camera, and
// Universal2D for the anime hall's 2D backdrop camera (HallBackdrop), which
// draws the effects among the painted layers (unlit: a glow ignores the
// hall's Light2Ds).
Shader "NOPE/Hall Deep Portal Glow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Effect", 2D) = "white" {}
        _Boost ("Brightness over the tint", Float) = 1.5
        _PortalMask ("Approved portal openings",2D)="white"{}
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" "CanUseSpriteAtlas" = "True" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        TEXTURE2D(_PortalMask);SAMPLER(sampler_PortalMask);
        float4x4 _ArtToLocal;float4 _CanvasMetrics,_CanvasSize;

        CBUFFER_START(UnityPerMaterial)
            float _Boost;
        CBUFFER_END

        struct Attributes
        {
            float4 positionOS : POSITION;
            float2 uv : TEXCOORD0;
            half4 color : COLOR;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 uv : TEXCOORD0;
            half4 color : COLOR;
            float2 aperture : TEXCOORD1;
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;
            output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
            output.uv = input.uv;
            float3 world=TransformObjectToWorld(input.positionOS.xyz);
            float2 local=mul(_ArtToLocal,float4(world,1)).xy;
            output.aperture=(local*_CanvasMetrics.x+_CanvasMetrics.yz)/_CanvasSize.xy;
            // The renderer's colour: in the vertices, or per draw (unity_SpriteColor) under the SRP batcher, as URP's own sprite shaders read it.
            output.color = input.color * unity_SpriteColor;
            return output;
        }

        half4 Frag(Varyings input) : SV_Target
        {
            half4 texel = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
            float radius=length((input.uv-.5)*2);
            // Confine the glow to the ring's inner opening; its lower security
            // fence stays in front. The registered proxy supplies position/size.
            half opening=(1-smoothstep(.68,.78,radius))*smoothstep(.30,.35,input.uv.y);
            half a=texel.a*input.color.a*opening;
            return half4(texel.rgb * input.color.rgb * a * _Boost, a);
        }
        ENDHLSL

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }
            Blend One OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }

        Pass
        {
            Name "Unlit2D"
            Tags { "LightMode" = "Universal2D" }
            Blend One OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
    }
}
