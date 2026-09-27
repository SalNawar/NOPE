Shader "NOPE/Anime Hall Registered Layers"
{
    Properties
    {
        [PerRendererData] _MainTex("Registered artwork", 2D) = "white" {}
        _Color("Material tint", Color) = (1,1,1,1)
        _SurfaceNormal("Approximate world surface normal", Vector) = (0,0,-1,0)
        _AmbientColor("Ambient illumination", Color) = (0.55,0.55,0.55,1)
        _LightingAmount("Realtime lighting", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            float4 _SurfaceNormal;
            float4 _AmbientColor;
            float _LightingAmount;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD1; float2 uv:TEXCOORD0; half4 color:COLOR; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.uv = input.uv;
                output.color = input.color * _Color;
                return output;
            }
            half3 Response(Light light, half3 normal)
            {
                half facing = saturate(dot(normal, light.direction));
                // Broad, soft cel transitions preserve the painted contours.
                half band = 0.18h + 0.52h * smoothstep(0.15h,0.35h,facing)
                           + 0.30h * smoothstep(0.65h,0.8h,facing);
                return light.color * band * light.distanceAttenuation;
            }
            half4 Frag(Varyings input):SV_Target
            {
                half4 artwork = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv) * input.color;
                half3 normal = normalize(_SurfaceNormal.xyz);
                half3 illumination = _AmbientColor.rgb + Response(GetMainLight(), normal);
                #if defined(_ADDITIONAL_LIGHTS)
                    uint count = GetAdditionalLightsCount();
                    for (uint index = 0u; index < count; ++index)
                        illumination += Response(GetAdditionalLight(index,input.positionWS),normal);
                #endif
                artwork.rgb *= lerp(half3(1,1,1),illumination,saturate(_LightingAmount));
                return artwork;
            }
            ENDHLSL
        }
    }
}
