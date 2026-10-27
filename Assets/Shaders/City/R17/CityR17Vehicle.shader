// The r17 city's flying vehicles (CITY-R17): the look-dev's js/r17traffic.js shaders, for Unity GPU instancing.
// Each instance's matrix is its model-to-camera frame in metres (x is the car's left, so the matrix is mirrored, as
// the page's); the vertex shader projects through the page's pinhole (x = cx + F X / Z, y = cy - F Y / Z, plate px)
// with the instance's own parallax, then through the stage's view (_CityVP). Depth is the vehicle's own Z, so the
// vehicles of one slot sort among themselves (the stage clears depth per slot group, as the page's per-slot passes).
//   pass 0 Body  three-tone toon shading from the plate sun (lit / mid / floor; floor = the vertex colour's alpha),
//                livery colours (kind 1, 2), lamps (brake, roof sign, indicators), a soft windscreen glint (kind 3),
//                depth haze toward the far air
//   pass 1 Ink   an outline hull of constant screen width (1.5 / 1 / 0.5 px by size: _VB.w, times the vertex's hull
//                alpha), pushed back in depth so it only shows round the silhouette (both faces drawn: Cull Off)
// Per instance (material property block arrays): _VA livery A + haze, _VB livery B + ink, _VM brake, roof sign,
// indicator (1 left, 2 right), length (plate px); _VP parallax. Globals from CityR17TrafficDraw: _CityCam (shift per
// unit parallax x, y; dolly; output px per plate px), _CityFocus, _CityPin (cx, cy, F, ink push m), _CitySun (xyz, glint),
// _CityHaze, _CityInk.
Shader "NOPE/City R17 Vehicle"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Cull Off
        ZWrite On
        ZTest LEqual
        Blend Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        float4x4 _CityVP;
        float4 _CityCam;
        float4 _CityFocus;
        float4 _CityPin;
        float4 _CitySun;
        float4 _CityHaze;
        float4 _CityInk;

        UNITY_INSTANCING_BUFFER_START(CityR17Vehicles)
            UNITY_DEFINE_INSTANCED_PROP(float4, _VA)
            UNITY_DEFINE_INSTANCED_PROP(float4, _VB)
            UNITY_DEFINE_INSTANCED_PROP(float4, _VM)
            UNITY_DEFINE_INSTANCED_PROP(float4, _VP)
        UNITY_INSTANCING_BUFFER_END(CityR17Vehicles)

        struct Attributes
        {
            float3 position : POSITION;
            float3 normal : NORMAL;
            float4 color : COLOR;
            float4 att : TEXCOORD0;
            float3 hull : TEXCOORD1;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 position : SV_POSITION;
            float3 n : TEXCOORD0;
            float3 w : TEXCOORD1;
            float4 color : TEXCOORD2;
            nointerpolation float2 lk : TEXCOORD3;
            nointerpolation float4 a : TEXCOORD4;
            nointerpolation float4 b : TEXCOORD5;
            nointerpolation float4 m : TEXCOORD6;
        };

        // the page's pinhole, then its setDepth for parallax f: plate px (y down)
        float2 Plate(float3 w, float f)
        {
            float z = max(w.z, 1e-3);
            float xp = _CityPin.x + _CityPin.z * w.x / z;
            float yp = _CityPin.y - _CityPin.z * w.y / z;
            float s = 1.0 + _CityCam.z * f;
            return float2(_CityFocus.x + (xp - _CityFocus.x) * s + _CityCam.x * f,
                          _CityFocus.y + (yp - _CityFocus.y) * s + _CityCam.y * f);
        }

        float4 ToClip(float2 p, float z)
        {
            float4 c = mul(_CityVP, float4(p.x, -p.y, 0.0, 1.0));
            float near01 = min(1.0 - saturate(z / 9000.0), 0.9999);
            c.z = lerp(UNITY_RAW_FAR_CLIP_VALUE, UNITY_NEAR_CLIP_VALUE, near01) * c.w;
            return c;
        }

        // a direction through the instance frame's inverse transpose (orthogonal axes, each scaled)
        float3 DirWorld(float4x4 M, float3 n)
        {
            float3 c0 = float3(M._m00, M._m10, M._m20);
            float3 c1 = float3(M._m01, M._m11, M._m21);
            float3 c2 = float3(M._m02, M._m12, M._m22);
            return normalize(c0 * (n.x / max(dot(c0, c0), 1e-8)) + c1 * (n.y / max(dot(c1, c1), 1e-8)) + c2 * (n.z / max(dot(c2, c2), 1e-8)));
        }

        Varyings Vert(Attributes v, bool hull)
        {
            Varyings o;
            UNITY_SETUP_INSTANCE_ID(v);
            float4x4 M = UNITY_MATRIX_M;
            float3 w = mul(M, float4(v.position, 1.0)).xyz;
            float4 A = UNITY_ACCESS_INSTANCED_PROP(CityR17Vehicles, _VA);
            float4 B = UNITY_ACCESS_INSTANCED_PROP(CityR17Vehicles, _VB);
            float4 Mm = UNITY_ACCESS_INSTANCED_PROP(CityR17Vehicles, _VM);
            float f = UNITY_ACCESS_INSTANCED_PROP(CityR17Vehicles, _VP).x;
            float2 p = Plate(w, f);
            float z = w.z;
            if (hull)
            {
                float3 hn = DirWorld(M, v.hull);
                float2 p2 = Plate(w + hn * 0.05, f);
                float2 dd = p2 - p;
                float len = length(dd);
                float2 dir = len > 1e-5 ? dd / len : float2(0.0, 0.0);
                float px = B.w * (v.att.z / 255.0 / 0.5);   // device px, as the page
                p += dir * px / max(_CityCam.w, 1e-4);
                z += _CityPin.w;
            }
            o.position = ToClip(p, z);
            o.n = DirWorld(M, v.normal);
            o.w = w;
            o.color = v.color;
            o.lk = v.att.xy;
            o.a = A;
            o.b = B;
            o.m = Mm;
            return o;
        }
        ENDHLSL

        Pass
        {
            Name "Body"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma instancing_options maxcount:250 nolightprobe nolodfade
            Varyings vert(Attributes v) { return Vert(v, false); }
            half4 frag(Varyings i) : SV_Target
            {
                int lamp = (int)(i.lk.x + 0.5);
                int kind = (int)(i.lk.y + 0.5);
                float3 base = kind == 1 ? i.a.rgb : (kind == 2 ? i.b.rgb : i.color.rgb);
                float3 n = normalize(i.n);
                float d = dot(n, _CitySun.xyz), fl = i.color.a, mid = 1.0 - 0.47 * (1.0 - fl);
                float k = d > 0.30 ? 1.0 : (d > -0.15 ? mid : fl);
                float3 col;
                if (lamp == 0 || lamp == 7)
                    col = base * (lamp == 7 ? k * 0.9 : k);
                else if (lamp == 2)
                    col = lerp(base * 0.85, float3(1.0, 0.45, 0.36), i.m.x * step(18.0, i.m.w));
                else if (lamp == 5)
                    col = i.m.y > 0.5 ? base : base * float3(0.62, 0.70, 0.72) * k;
                else if (lamp == 3 || lamp == 4)
                {
                    float on = ((int)(i.m.z + 0.5) == (lamp == 3 ? 1 : 2)) && i.m.w > 26.0 ? 1.0 : 0.0;
                    col = lerp(base * 0.7 * k, float3(1.0, 0.72, 0.30), on);
                }
                else
                    col = base;
                if (kind == 3 && _CitySun.w > 0.0)
                {
                    float3 vd = normalize(i.w);
                    float3 r = reflect(vd, n);
                    float g = smoothstep(0.990, 0.9985, dot(r, _CitySun.xyz)) * smoothstep(28.0, 40.0, i.m.w) * _CitySun.w;
                    col = lerp(col, float3(0.96, 0.95, 0.86), g);
                }
                return half4(lerp(col, _CityHaze.rgb, i.a.w), 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Ink"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma instancing_options maxcount:250 nolightprobe nolodfade
            Varyings vert(Attributes v) { return Vert(v, true); }
            half4 frag(Varyings i) : SV_Target
            {
                return half4(lerp(_CityInk.rgb, _CityHaze.rgb, i.a.w), 1.0);
            }
            ENDHLSL
        }
    }
}
