// The hall's windows looking out on the r17 city (CITY-R17). CityR17Windows puts this on every hall renderer that
// drew "NOPE/Hall Living City", keeping its apertures (_Masks: red the left wall's windows, green the front
// clerestory; _Region picks one). A window's canvas uv maps to a plate rect (_LeftBox to _LeftPlate, _FrontBox to
// _FrontPlate: canvas uv with y down to plate px), and the plate point is read from the city's render texture
// (_CityR17Tex, a global; _CityR17View the plate rect it shows). The city is stored in gamma space, so it is decoded
// once here. _StateWeights (the hall's own hour blend, set on its layers) tints it by _HourTint; the city view itself
// has no hour grade.
Shader "NOPE/City R17 Window"
{
    Properties
    {
        [PerRendererData] _MainTex ("Window registration", 2D) = "white" {}
        _Masks ("Hall apertures", 2D) = "black" {}
        _Region ("Window side", Float) = 1
        _LeftBox ("Left windows' box (canvas uv, y down)", Vector) = (0, 0, 0.3131, 0.6809)
        _LeftPlate ("Left windows' plate rect (px)", Vector) = (300, 20, 1500, 1086)
        _FrontBox ("Front windows' box (canvas uv, y down)", Vector) = (0.4503, 0.2942, 0.1156, 0.1671)
        _FrontPlate ("Front windows' plate rect (px)", Vector) = (1280, 60, 1000, 480)
        _HourTint ("Hour tint", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

        TEXTURE2D(_Masks);
        SAMPLER(sampler_Masks);
        TEXTURE2D(_CityR17Tex);
        SAMPLER(sampler_CityR17Tex);
        float4 _CityR17View;
        float4 _LeftBox, _LeftPlate, _FrontBox, _FrontPlate;
        float4 _StateWeights;
        float _Region, _HourTint;

        struct Attributes
        {
            float4 position : POSITION;
            float2 uv : TEXCOORD0;
            float4 color : COLOR;
        };

        struct Varyings
        {
            float4 position : SV_POSITION;
            float2 uv : TEXCOORD0;
            float4 color : COLOR;
        };

        Varyings vert(Attributes v)
        {
            Varyings o;
            o.position = TransformObjectToHClip(v.position.xyz);
            o.uv = v.uv;
            o.color = v.color;
            return o;
        }

        half4 frag(Varyings v) : SV_Target
        {
            half4 mask = SAMPLE_TEXTURE2D(_Masks, sampler_Masks, v.uv);
            half aperture = _Region < 1.5 ? mask.r : mask.g;
            clip(aperture - 0.001h);
            float4 box = _Region < 1.5 ? _LeftBox : _FrontBox;
            float4 plate = _Region < 1.5 ? _LeftPlate : _FrontPlate;
            float2 canvas = float2(v.uv.x, 1.0 - v.uv.y);
            float2 p = plate.xy + (canvas - box.xy) / max(box.zw, 1e-5) * plate.zw;
            float2 t = (p - _CityR17View.xy) / max(_CityR17View.zw, 1e-3);
            half3 rgb = SAMPLE_TEXTURE2D(_CityR17Tex, sampler_CityR17Tex, float2(t.x, 1.0 - t.y)).rgb;
            #ifndef UNITY_COLORSPACE_GAMMA
            rgb = SRGBToLinear(rgb);
            #endif
            float4 w = _StateWeights;
            float sum = w.x + w.y + w.z + w.w;
            half3 hour = sum > 1e-4
                ? (half3)((float3(0.95, 0.97, 1.0) * w.x + float3(1.0, 1.0, 1.0) * w.y + float3(0.75, 0.55, 0.55) * w.z + float3(0.22, 0.29, 0.43) * w.w) / sum)
                : half3(1.0h, 1.0h, 1.0h);
            rgb *= lerp(half3(1.0h, 1.0h, 1.0h), hour, (half)_HourTint);
            return half4(rgb * (half3)v.color.rgb, aperture * (half)v.color.a);
        }
        ENDHLSL

        Pass
        {
            Tags { "LightMode" = "Universal2D" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }
    }
}
