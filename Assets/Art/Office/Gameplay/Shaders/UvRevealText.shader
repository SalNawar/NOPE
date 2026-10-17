// The papers' hidden words under the UV torch (the desk machines spec §1): a
// TextMeshPro text (the ghost of a value washed off a box, a seal's microprint)
// drawn from its font's signed-distance atlas only inside the torch's cone
// (_UvCone, _UvConeOn: the torch sets them every frame; the rim fades over
// _UvConeSoft of the radius), so the words light up pixel by pixel under the
// violet light and vanish outside it. A minimal SDF text shader: the atlas's
// alpha is a distance, cut at 0.5 with a screen-space edge (fwidth), tinted
// by the text's vertex colour and _FaceColor, glowing additively (a
// fluorescent ink). Assign the font's atlas as _MainTex (DeskHardware does).
Shader "TimeDesk/UvRevealText"
{
    Properties
    {
        _MainTex ("Font atlas (SDF)", 2D) = "white" {}
        _FaceColor ("Glow colour", Color) = (0.78, 0.6, 1, 1)
        _UvConeSoft ("Rim softness (share of the radius)", Range(0.01, 1)) = 0.25
        _Thicken ("Thicken", Range(-0.2, 0.2)) = 0.03
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            Name "UvRevealText"
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
                half4 _FaceColor;
                half _UvConeSoft;
                half _Thicken;
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
                output.uv = input.uv;
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
                half distance = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
                half edge = max(fwidth(distance), 1e-4);
                half cut = 0.5 - _Thicken;
                half shape = smoothstep(cut - edge, cut + edge, distance);
                half4 c = _FaceColor * half4(1, 1, 1, input.color.a);
                c.a *= shape * Cone(input.positionWS);
                return c;
            }
            ENDHLSL
        }
    }
}
