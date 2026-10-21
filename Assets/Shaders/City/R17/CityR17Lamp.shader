// The r17 city's noon lamps (CITY-R17): the look-dev's cel lamp (js/traffic.js lamp(): one flat halo ring at
// alpha 0.24 out to 2.6 r, a hard core of radius r, a warm white centre of 0.45 r; no soft bloom), drawn source-over
// at the lamp's brightness (vertex alpha, js/r16lights.js), and the ring lamps' small painted base disc. Each quad's
// uv runs -1..1 over 2.6 r (lamps, uv.z 0) or over the disc's radius (discs, uv.z 1). Gamma-space blending, as the stage.
Shader "NOPE/City R17 Lamp"
{
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha

        Pass
        {
            Name "Lamp"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4x4 _CityVP;

            struct Attributes
            {
                float4 position : POSITION;
                float4 color : COLOR;
                float3 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 position : SV_POSITION;
                float4 color : COLOR;
                float3 uv : TEXCOORD0;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 w = mul(UNITY_MATRIX_M, float4(v.position.xyz, 1.0)).xyz;
                o.position = mul(_CityVP, float4(w, 1.0));
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }

            // a disc's coverage with a one-pixel soft edge (the canvas antialiases its arcs)
            half Disc(float d, float radius, float aa)
            {
                return (half)saturate((radius - d) / max(aa, 1e-4) + 0.5);
            }

            half4 frag(Varyings i) : SV_Target
            {
                float d = length(i.uv.xy);
                float aa = max(fwidth(d), 1e-4);
                if (i.uv.z > 0.5)
                    return half4(i.color.rgb, Disc(d, 1.0, aa) * i.color.a);
                const float core = 1.0 / 2.6, centre = 0.45 / 2.6;
                half halo = Disc(d, 1.0, aa) * 0.24h;
                half body = Disc(d, core, aa);
                half white = Disc(d, centre, aa);
                // halo, then the core over it, then the white centre over that (source-over inside the glyph)
                half3 c = (half3)i.color.rgb;
                half a = halo + body * (1.0h - halo);
                half3 rgb = a > 0.0h ? (c * halo * (1.0h - body) + c * body) / max(a, 1e-4h) : c;
                half3 centreCol = half3(1.0h, 0.99215686h, 0.94901961h);   // #fffdf2
                rgb = lerp(rgb, centreCol, white);
                a = a + white * (1.0h - a);
                return half4(rgb, a * (half)i.color.a);
            }
            ENDHLSL
        }
    }
}
