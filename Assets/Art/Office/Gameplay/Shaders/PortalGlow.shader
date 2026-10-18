// The portal rings' vortex (Saleh's demo 1018a: "the vfx for portals looks bad and is not even
// alligned"; the portals spec v3 VX1, VX4, VX5; PortalEffect, PortalVortex): a quad PortalVortex.Margin
// times its ring's opening each way, drawn among the hall's painted layers, in the 80s/90s cel look of
// the UI kit: hard-edged bands, no soft gradients. Inside the opening a swirl of three tones (_Deep,
// _Mid, _Light) turning around a bright core, an ink line and a hard rim (_Rim) at its edge, and cel
// sparkles (four-point stars) drifting inward; outside it the ring glow, light the opening throws on
// the metal ring above the bay's fence. The painting under the quad (_Drawing, its texture coordinates
// _DrawingRect) says what stands in front of the opening: dark ink, brass rails, the ring's metal and
// the fence posts keep their painted pixels; below the fence's top rail (_Shape.y) the vortex shows
// through the glass at _Shape.z. Unlit (it glows through the evening; brighter at night: _Life.z),
// premultiplied: the opening's fill covers the wall seen through the ring, the ring glow only adds
// light. Every time-varying input comes from PortalEffect (the swirl's angle, the sparkles' drift, the
// iris, the flare), so reduced motion holds it still. No depth write; it depth-tests, so the preserved
// 3D desk in front hides it. Two passes of the same code: UniversalForward for the office camera, and
// Universal2D for the anime hall's 2D backdrop camera (HallBackdrop), which draws it among the painted
// layers (unlit: it ignores the hall's Light2Ds).
Shader "TimeDesk/PortalGlow"
{
    Properties
    {
        _Drawing ("The hall drawing under the quad", 2D) = "white" {}
        _DrawingRect ("The quad in the drawing's texture coordinates (x, y, width, height)", Vector) = (0, 0, 1, 1)
        _Shape ("x: has a drawing (0/1), y: the fence's v, z: the vortex's opacity through the glass, w: its opacity", Vector) = (0, 0, 0.55, 0.94)
        _Deep ("Deep tone", Color) = (0.05, 0.2, 0.45, 1)
        _Mid ("Mid tone", Color) = (0.1, 0.55, 0.85, 1)
        _Light ("Light tone", Color) = (0.55, 0.95, 1, 1)
        _Rim ("Rim and ring glow", Color) = (0.85, 1, 1, 1)
        _Ink ("Ink line", Color) = (0.17, 0.11, 0.14, 1)
        _Swirl ("x: arms, y: twist (negative: the other way), z: angle (radians), w: the sparkles' drift", Vector) = (3, 2.2, 0, 0)
        _Life ("x: iris (share of the opening), y: flare 0-1, z: night 0-1, w: the flare's shock ring radius (negative: none)", Vector) = (1, 0, 0, -1)
        _Glow ("x: the ring glow's strength, y: sparkle density 0-1, z: brightness over the tones, w: the night's extra brightness", Vector) = (0.6, 0.5, 1.15, 0.35)
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

        TEXTURE2D(_Drawing);
        SAMPLER(sampler_Drawing);

        CBUFFER_START(UnityPerMaterial)
            float4 _DrawingRect;
            float4 _Shape;
            half4 _Deep;
            half4 _Mid;
            half4 _Light;
            half4 _Rim;
            half4 _Ink;
            float4 _Swirl;
            float4 _Life;
            float4 _Glow;
        CBUFFER_END

        struct Attributes
        {
            float4 positionOS : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 uv : TEXCOORD0;
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;
            output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
            output.uv = input.uv;
            return output;
        }

        static const float Margin = 1.5;
        static const float TwoPi = 6.28318530718;

        float Hash(float2 p)
        {
            p = frac(p * float2(123.34, 456.21));
            p += dot(p, p + 45.32);
            return frac(p.x * p.y);
        }

        // A hard cel edge: 1 inside (x below edge), 0 outside, one pixel of antialiasing.
        float Cel(float x, float edge)
        {
            float w = max(fwidth(x), 1e-4);
            return 1.0 - smoothstep(edge - w, edge + w, x);
        }

        // What the painting under this pixel is, from its colour (as painted, gamma space): how much it stands in
        // front of the opening (ink, brass, the ring's metal, the fence posts) and how much it is the bay's glass.
        void Painted(float2 uv, out float front, out float glass)
        {
            front = 0;
            glass = 0;
            if (_Shape.x < 0.5)
                return;
            float3 c = SAMPLE_TEXTURE2D(_Drawing, sampler_Drawing, _DrawingRect.xy + uv * _DrawingRect.zw).rgb;
            #if !defined(UNITY_COLORSPACE_GAMMA)
            c = LinearToSRGB(c);
            #endif
            float hi = max(c.r, max(c.g, c.b));
            float lo = min(c.r, min(c.g, c.b));
            float sat = (hi - lo) / max(hi, 1e-3);
            float dark = 1.0 - smoothstep(0.28, 0.38, hi);
            float brass = smoothstep(0.16, 0.26, c.r - c.b) * smoothstep(0.30, 0.42, sat);
            float cool = smoothstep(0.0, 0.06, c.b - c.r);
            glass = cool * smoothstep(0.68, 0.78, hi);
            float metal = (1.0 - smoothstep(0.6, 0.74, hi)) * (1.0 - brass);
            front = saturate(dark + brass + metal);
        }

        half4 Frag(Varyings input) : SV_Target
        {
            float2 q = (input.uv - 0.5) * 2.0 * Margin;
            float r = length(q);
            float angle = atan2(q.y, q.x);
            float night = saturate(_Life.z);
            float flare = saturate(_Life.y);

            float front, glass;
            Painted(input.uv, front, glass);
            float behindFence = step(input.uv.y, _Shape.y);

            // The iris: the opening's edge springs out from the core (PortalVortex.Iris).
            float iris = max(_Life.x, 0.0);
            float edge = max(iris, 1e-3);
            float inside = Cel(r, edge);
            float rr = r / max(edge, 1e-3);

            // The swirl: arms banded in two hard tones over the deep fill, winding toward the core.
            float s = frac(angle / TwoPi * _Swirl.x + _Swirl.y * rr - _Swirl.z / TwoPi * _Swirl.x);
            half3 col = _Deep.rgb;
            col = lerp(col, _Mid.rgb, Cel(s, 0.55));
            col = lerp(col, _Light.rgb, Cel(s, 0.16) * Cel(rr, 0.82));
            // The core: a light disc and a white-hot heart, pulsing with the flare.
            float core = 0.24 + 0.08 * flare + 0.04 * night;
            col = lerp(col, _Light.rgb, Cel(rr, core));
            col = lerp(col, _Rim.rgb, Cel(rr, core * 0.45));
            // The rim: an ink line, then a hard bright band at the opening's edge.
            col = lerp(col, _Ink.rgb, Cel(rr, 0.9) * (1.0 - Cel(rr, 0.86)));
            col = lerp(col, _Rim.rgb, 1.0 - Cel(rr, 0.9));

            // Sparkles: four-point cel stars in polar cells, drifting inward, more of them at night.
            float sparkle = 0;
            [unroll] for (int layer = 0; layer < 2; layer++)
            {
                float n = 9.0 + layer * 5.0, m = 3.0 + layer;
                float a = angle / TwoPi * n + layer * 0.37;
                float b = rr * m + _Swirl.w * (0.6 + layer * 0.35);
                float2 cell = floor(float2(a, b));
                float h = Hash(cell + layer * 17.0);
                float2 l = float2(frac(a) - 0.5, frac(b) - 0.5);
                l.x *= TwoPi * max(rr, 0.15) / n * m;
                float size = 0.6 + 0.6 * Hash(cell + 3.1);
                l *= 2.2 / size;
                float star = max(saturate(1.0 - abs(l.x) * 9.0) * saturate(1.0 - abs(l.y) * 2.2),
                                 saturate(1.0 - abs(l.y) * 9.0) * saturate(1.0 - abs(l.x) * 2.2));
                float twinkle = 0.75 + 0.25 * sin(_Swirl.w * 9.0 + h * TwoPi);
                float shown = step(1.0 - (_Glow.y * (0.55 + 0.45 * night)) * 0.5, h);
                sparkle = max(sparkle, shown * step(0.42, star * twinkle) * Cel(rr, 0.84));
            }
            col = lerp(col, _Rim.rgb, sparkle);

            // The flare's shock ring, running out to the rim.
            float shock = _Life.w >= 0 ? (1.0 - Cel(abs(rr - _Life.w), 0.05)) : 0;
            col = lerp(col, _Rim.rgb, shock);

            float bright = _Glow.z * (1.0 + _Glow.w * night + 0.5 * flare);
            float fill = inside * (1.0 - front) * lerp(1.0, _Shape.z, behindFence) * _Shape.w;
            half3 rgb = col * bright * fill;
            float a = fill;

            // The ring glow: light the opening throws on the inner lip of its ring, above the fence, outside the opening:
            // one hard band in the light tone, wider and stronger at night and in a flare.
            float lip = 0.08 + 0.05 * night + 0.06 * flare;
            float halo = (1.0 - Cel(r, edge)) * Cel(r, edge + lip) * (1.0 - behindFence);
            rgb += _Light.rgb * halo * _Glow.x * (0.7 + 0.3 * night + 0.5 * flare) * saturate(iris);
            return half4(rgb, a);
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
