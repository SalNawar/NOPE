// The papers' hidden layer under the UV torch (the desk machines spec §1; Saleh
// 2026-10-17: "an actual uv flashlight you can use to see the hidden watermarks
// and symbols"): a mark's quad (the Bureau's watermark, a seal's microprint
// ring, a found mark's tick) drawn only inside the torch's cone. The cone is a
// circle on the desk the torch sets every frame (_UvCone: its centre xyz and
// radius w; _UvConeOn: 1 while switched on); its rim fades over _UvConeSoft of
// the radius, so the reveal follows the light pixel by pixel. Unlit and
// additive-glowing (a fluorescent ink), never lit by the hall. Outside the cone
// (or with the torch off) nothing draws. _Always 1 draws regardless of the
// cone (a found mark's tick, which stays).
Shader "TimeDesk/UvReveal"
{
    Properties
    {
        _MainTex ("Mark", 2D) = "white" {}
        _Color ("Glow colour", Color) = (0.72, 0.55, 1, 1)
        _UvConeSoft ("Rim softness (share of the radius)", Range(0.01, 1)) = 0.25
        _Always ("Draw outside the cone", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            Name "UvReveal"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha One

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half _UvConeSoft;
                half _Always;
            CBUFFER_END
            float4 _UvCone;
            half _UvConeOn;

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
                float3 positionWS : TEXCOORD1;
                half4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                return output;
            }

            half Cone(float3 world)
            {
                float2 d = world.xz - _UvCone.xz;
                float r = max(_UvCone.w, 1e-4);
                float inner = r * (1.0 - _UvConeSoft);
                return _UvConeOn * (1.0 - smoothstep(inner, r, length(d)));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 mark = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color * input.color;
                half mask = max(_Always, Cone(input.positionWS));
                mark.a *= mask;
                return mark;
            }
            ENDHLSL
        }
    }
}
