// NOPE/Desk Painted: the painted desk v3 (DESK-ART; UNITY_PLAN 4.4). The desk's paint is
// baked into UV textures (never projected at runtime), so the paint already holds its light.
// This shader keeps that look and only moves the light with the hour:
//   paint * lerp(1, Band(N.L hour) / Band(N.L ref), relight * G) * DeskLight() * shadow
//         + paint * R * glow
// where Band is a two-tone cel band (the painted shade and the lit tone), the reference light
// is the one the paint was painted under (_RefLightDir), the hour's light is URP's main light,
// DeskLight() is LIGHT's seam (Shaders/Include/DeskLight.hlsl: consumed, never edited) and the
// shadow is the main light's, so a lifted or slid tool casts onto the plate (moving objects
// cast; the shell's renderer casts none). The mask (_MaskMap, linear, when _MASKMAP is on):
// R glow (lamp windows, the CRT bezel, AVAILABLE), G the time-of-day light mask (how much of
// the hour's relight a texel takes), B the locked-home desaturation (scaled by _Locked, set per
// insert). Without a mask: no glow, the full relight, nothing locked. Hover outlines are
// HoverHull.shader's; papers are not drawn with this shader.
// Passes: DeskPaintedForward (UniversalForwardOnly), ShadowCaster and DepthOnly, on one
// UnityPerMaterial buffer (SRP Batcher compatible), as DeskAnime's.
Shader "NOPE/Desk Painted"
{
    Properties
    {
        [MainTexture] _BaseMap("Paint (baked, UV bake)", 2D) = "white" {}
        [MainColor] _BaseColor("Paint tint", Color) = (1, 1, 1, 1)
        [Toggle(_MASKMAP)] _UseMask("Use the mask", Float) = 0
        [NoScaleOffset] _MaskMap("Mask (R glow, G time of day, B locked; linear)", 2D) = "black" {}
        _RefLightDir("Painted light direction (world, toward the light)", Vector) = (0.35, 0.8, -0.45, 0)
        _Relight("Relight with the hour", Range(0, 1)) = 0.7
        _ShadeValue("Painted shade tone", Range(0.1, 1)) = 0.55
        _BandThreshold("Band boundary (N.L)", Range(-0.5, 0.5)) = 0.05
        _BandSoftness("Band softness", Range(0.005, 0.3)) = 0.08
        _ShadowTint("Cast shadow multiplier", Color) = (0.72, 0.78, 0.9, 1)
        _ShadowReception("Cast shadow strength", Range(0, 1)) = 1
        [HDR] _GlowColor("Glow colour", Color) = (1, 0.86, 0.6, 1)
        _Glow("Glow", Range(0, 4)) = 1
        _Locked("Locked home", Range(0, 1)) = 0
        _LockedDim("Locked home value", Range(0.3, 1)) = 0.8
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        TEXTURE2D(_MaskMap); SAMPLER(sampler_MaskMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor, _ShadowTint, _GlowColor;
            float4 _RefLightDir;
            half _Relight, _ShadeValue, _BandThreshold, _BandSoftness, _ShadowReception, _Glow, _Locked, _LockedDim;
        CBUFFER_END

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float2 uv : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            half3 normalWS : TEXCOORD1;
            float2 uv : TEXCOORD2;
            half fog : TEXCOORD3;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
        };

        Varyings Vert(Attributes v)
        {
            Varyings o = (Varyings)0;
            UNITY_SETUP_INSTANCE_ID(v);
            UNITY_TRANSFER_INSTANCE_ID(v, o);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
            VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
            o.positionCS = p.positionCS;
            o.positionWS = p.positionWS;
            o.normalWS = TransformObjectToWorldNormal(v.normalOS);
            o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
            o.fog = ComputeFogFactor(p.positionCS.z);
            return o;
        }
        ENDHLSL

        Pass
        {
            Name "DeskPaintedForward"
            Tags { "LightMode" = "UniversalForwardOnly" }
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #pragma shader_feature_local_fragment _MASKMAP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Assets/Shaders/Include/DeskLight.hlsl"

            // The cel band of a light at N.L = nl: the painted shade below the boundary, full tone above.
            half Band(half nl, half width)
            {
                return lerp(_ShadeValue, 1.0h, smoothstep(_BandThreshold - width, _BandThreshold + width, nl));
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half3 paint = (SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor).rgb;
                half4 mask = half4(0, 1, 0, 0);
                #if defined(_MASKMAP)
                    mask = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, i.uv);
                #endif

                half3 n = normalize(i.normalWS);
                float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    shadowCoord = ComputeScreenPos(TransformWorldToHClip(i.positionWS));
                #endif
                Light hour = GetMainLight(shadowCoord, i.positionWS, half4(1, 1, 1, 1));

                // The hour's light against the painted one, as cel bands (derivative widening keeps thin edges from shimmering).
                half nlHour = dot(n, hour.direction);
                half nlRef = dot(n, (half3)normalize(_RefLightDir.xyz));
                half width = max(_BandSoftness, fwidth(nlHour) * 1.25h);
                half ratio = Band(nlHour, width) / max(Band(nlRef, width), 0.05h);
                half relight = lerp(1.0h, ratio, _Relight * mask.g);

                half cast = smoothstep(0.14h, 0.78h, hour.shadowAttenuation);
                half3 shadow = lerp(half3(1, 1, 1), lerp(_ShadowTint.rgb, half3(1, 1, 1), cast), _ShadowReception);

                half3 color = paint * relight * DeskLight() * shadow + paint * mask.r * _Glow * _GlowColor.rgb;

                // A locked home's insert: desaturated and a little darker where the mask marks it.
                half grey = dot(color, half3(0.2126h, 0.7152h, 0.0722h)) * _LockedDim;
                color = lerp(color, grey.xxx, saturate(_Locked * mask.b));

                return half4(MixFog(color, i.fog), 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0 Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection, _LightPosition;

            Varyings ShadowVert(Attributes v)
            {
                Varyings o = Vert(v);
                float3 direction = _LightDirection;
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    direction = normalize(_LightPosition - o.positionWS);
                #endif
                o.positionCS = TransformWorldToHClip(ApplyShadowBias(o.positionWS, o.normalWS, direction));
                o.positionCS = ApplyShadowClamping(o.positionCS);
                return o;
            }

            half4 ShadowFrag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On ColorMask R Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing

            half4 DepthFrag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                return i.positionCS.z;
            }
            ENDHLSL
        }
    }
}
