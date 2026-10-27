// The r17 city's 2D layers (CITY-R17): planes, clouds, ads, train, fire, smoke and the parapet, drawn by
// CityR17Stage through a command buffer into the city's render texture. The texture is sampled raw (imported
// without sRGB decoding) and blended in the look-dev's gamma space, so every blend matches the canvas.
//   pass 0 Alpha      straight-alpha source-over; _Haze tints toward the far air (the page's source-atop fill)
//   pass 1 Add        'lighter': adds colour x alpha x _Tint.a (the furnace spill, the train's window glow)
//   pass 2 DepthMask  writes the nearest depth where the texture is opaque (the far towers cut the far traffic)
//   pass 3 Morph      a cloud: two drawings cross-dissolved in premultiplied colour by _Morph, then source-over
// _UV / _UV2: the region (offset xy, scale zw; a negative x scale mirrors). _ClipX: a world-x window (the train's mattes).
Shader "NOPE/City R17 Sprite"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Cull Off
        ZWrite Off
        ZTest Always

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        float4x4 _CityVP;
        float4 _UV;
        float4 _UV2;
        float4 _Tint;
        float4 _Haze;
        float4 _ClipX;
        float _Morph;

        struct Attributes
        {
            float4 position : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct Varyings
        {
            float4 position : SV_POSITION;
            float2 uv : TEXCOORD0;
            float2 uv2 : TEXCOORD1;
            float wx : TEXCOORD2;
        };

        Varyings vert(Attributes v)
        {
            Varyings o;
            float3 w = mul(UNITY_MATRIX_M, float4(v.position.xyz, 1.0)).xyz;
            o.position = mul(_CityVP, float4(w, 1.0));
            o.uv = _UV.xy + v.uv * _UV.zw;
            o.uv2 = _UV2.xy + v.uv * _UV2.zw;
            o.wx = w.x;
            return o;
        }

        half4 Straight(Varyings i)
        {
            clip(i.wx - _ClipX.x);
            clip(_ClipX.y - i.wx);
            half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
            c.rgb = lerp(c.rgb, (half3)_Haze.rgb, (half)_Haze.a);
            return c * (half4)_Tint;
        }
        ENDHLSL

        Pass
        {
            Name "Alpha"
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            half4 frag(Varyings i) : SV_Target { return Straight(i); }
            ENDHLSL
        }

        Pass
        {
            Name "Add"
            Blend SrcAlpha One, Zero One
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            half4 frag(Varyings i) : SV_Target { return Straight(i); }
            ENDHLSL
        }

        Pass
        {
            Name "DepthMask"
            ColorMask 0
            ZWrite On
            HLSLPROGRAM
            #pragma vertex vertMask
            #pragma fragment frag
            Varyings vertMask(Attributes v)
            {
                Varyings o = vert(v);
                o.position.z = lerp(UNITY_RAW_FAR_CLIP_VALUE, UNITY_NEAR_CLIP_VALUE, 0.99999) * o.position.w;
                return o;
            }
            half4 frag(Varyings i) : SV_Target
            {
                half a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).a;
                clip(a - 0.5h);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "Morph"
            Blend One OneMinusSrcAlpha, One OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            half4 frag(Varyings i) : SV_Target
            {
                half4 a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half4 b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv2);
                a.rgb *= a.a;
                b.rgb *= b.a;
                half4 c = min(lerp(a, b, (half)_Morph), 1.0h);
                return c * (half)_Tint.a;
            }
            ENDHLSL
        }
    }
}
