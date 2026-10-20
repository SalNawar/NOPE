// The portal rings' vortex (Saleh's demo 1018a: "the vfx for portals looks bad and is not even
// alligned"; Saleh's portal note of 2026-10-19; HALL-FX BUG-26a; PortalEffect, PortalVortex): a quad
// PortalVortex.Margin times its ring's opening each way, drawn among the hall's painted layers, in the
// 80s/90s cel look of the UI kit: hard-edged bands, no soft gradients. It fills exactly the hole the
// ring's metal encloses, from the ring's painted-aperture mask (HallPortalsSO.masks, made from the
// painting by tools/hall/portal_masks.py; never a colour test at run time): R the hole, G what stands in
// front of it (the fence rails, the mullion: the painting keeps its pixels there), B the part seen
// through the bay's glass (_Shape.y), A the depth inside the hole's edge (the ink line and the rim
// follow the painted edge). Nothing is drawn outside the hole: no halo on the metal (the glow around
// the ring is the bloom of LIGHT's Volume on the vortex's bright core). Inside, a layered cel vortex: a
// slow back layer of dim arms turning against the front, the front's arms in two hard tones winding
// toward a bright core, cel sparkles drifting inward (faded out on a ring too small on screen to hold
// them), every hard edge antialiased on its unwrapped coordinate. Every time-varying input comes from
// PortalEffect (the swirl's angle, the sparkles' drift, the iris, the flare), so reduced motion holds
// it still. Unlit (it glows through the evening; brighter at night: _Life.z), premultiplied. No depth
// write; it depth-tests, so the preserved 3D desk in front hides it. Two passes of the same code:
// UniversalForward for the office camera, and Universal2D for the anime hall's 2D backdrop camera
// (HallBackdrop), which draws it among the painted layers (unlit: it ignores the hall's Light2Ds).
Shader "TimeDesk/PortalGlow"
{
    Properties
    {
        _Mask ("The ring's painted-aperture mask atlas (R hole, G in front, B behind glass, A edge depth)", 2D) = "black" {}
        _MaskST ("The quad's uv to the mask atlas (xy scale, zw offset)", Vector) = (1, 1, 0, 0)
        _MaskRect ("The ring's crop in the atlas (x, y, width, height)", Vector) = (0, 0, 1, 1)
        _Shape ("x: the hole's reach (opening radii), y: opacity through the glass, z: opacity, w: has a mask (0/1)", Vector) = (1, 0.72, 0.94, 0)
        _Deep ("Deep tone", Color) = (0.05, 0.2, 0.45, 1)
        _Mid ("Mid tone", Color) = (0.1, 0.55, 0.85, 1)
        _Light ("Light tone", Color) = (0.55, 0.95, 1, 1)
        _Rim ("Rim and core", Color) = (0.85, 1, 1, 1)
        _Ink ("Ink line", Color) = (0.17, 0.11, 0.14, 1)
        _Swirl ("x: arms, y: twist (negative: the other way), z: angle (radians), w: the sparkles' drift", Vector) = (3, 2.2, 0, 0)
        _Life ("x: iris (share of the opening), y: flare 0-1, z: night 0-1, w: the flare's shock ring radius (negative: none)", Vector) = (1, 0, 0, -1)
        _Glow ("x: the core's bloom lift, y: sparkle density 0-1, z: brightness over the tones, w: the night's extra brightness", Vector) = (0.6, 0.5, 1.15, 0.35)
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_Mask);
        SAMPLER(sampler_Mask);

        CBUFFER_START(UnityPerMaterial)
            float4 _MaskST;
            float4 _MaskRect;
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

        // A hard cel edge: 1 inside (x below edge), 0 outside, antialiased over w (a pixel of x).
        float Cel(float x, float edge, float w)
        {
            w = max(w, 1e-4);
            return 1.0 - smoothstep(edge - w, edge + w, x);
        }

        // A band of a periodic coordinate s (period 1): 1 within half-width hw of centre c, antialiased over w;
        // measured on the periodic distance, so the band has no seam where s wraps.
        float Band(float s, float c, float hw, float w)
        {
            float d = abs(frac(s - c + 0.5) - 0.5);
            return Cel(d, hw, w);
        }

        // The mask under the quad's uv: (hole, in front, behind glass, edge depth); without a mask the whole
        // ellipse is the hole, nothing stands in front and the edge depth is the iris rim's.
        float4 Aperture(float2 uv, float rr)
        {
            if (_Shape.w < 0.5)
                return float4(1.0, 0.0, 0.0, saturate((1.0 - rr) / 0.1));
            float2 m = uv * _MaskST.xy + _MaskST.zw;
            float2 local = (m - _MaskRect.xy) / max(_MaskRect.zw, 1e-6);
            float inCrop = step(0.0, local.x) * step(local.x, 1.0) * step(0.0, local.y) * step(local.y, 1.0);
            return SAMPLE_TEXTURE2D(_Mask, sampler_Mask, m) * inCrop;
        }

        half4 Frag(Varyings input) : SV_Target
        {
            float2 q = (input.uv - 0.5) * 2.0 * Margin;     // in opening radii
            float r = length(q);
            float angle = atan2(q.y, q.x);
            float night = saturate(_Life.z);
            float flare = saturate(_Life.y);
            float pixel = max(length(fwidth(q)), 1e-4);      // opening radii per screen pixel

            // The iris: its edge springs out from the core (PortalVortex.Iris) to the hole's reach, so open it uncovers all of it.
            float iris = max(_Life.x, 0.0);
            float edge = max(iris * max(_Shape.x, 1.0), 1e-3);
            float inside = Cel(r, edge, pixel);
            float rr = r / edge;                              // 1 at the iris's edge
            float4 m = Aperture(input.uv, rr);

            // The arms' coordinate: turns of the angle plus the twist toward the core; its width per pixel, unwrapped.
            float arms = max(_Swirl.x, 1.0);
            float sw = (arms / (TwoPi * max(r, 0.05)) + abs(_Swirl.y) / edge) * pixel;
            float s = angle / TwoPi * arms + _Swirl.y * rr - _Swirl.z / TwoPi * arms;

            // The back layer: one more arm, turning slowly the other way, a dim tone over the deep fill.
            float backArms = arms + 1.0;
            float sb = -angle / TwoPi * backArms + 0.6 * _Swirl.y * rr - 0.45 * _Swirl.z / TwoPi * backArms;
            float swb = (backArms / (TwoPi * max(r, 0.05)) + 0.6 * abs(_Swirl.y) / edge) * pixel;
            half3 col = _Deep.rgb;
            col = lerp(col, lerp(_Deep.rgb, _Mid.rgb, 0.45), Band(sb, 0.25, 0.22, swb));

            // The front layer: arms banded in two hard tones, winding toward the core.
            col = lerp(col, _Mid.rgb, Band(s, 0.275, 0.275, sw));
            col = lerp(col, _Light.rgb, Band(s, 0.08, 0.08, sw) * Cel(rr, 0.82, pixel / edge));
            // The core: a light disc and a white-hot heart, swelling with the flare (its heart lifts past 1 for the bloom).
            float core = 0.24 + 0.08 * flare + 0.04 * night;
            float heart = Cel(rr, core * 0.45, pixel / edge);
            col = lerp(col, _Light.rgb, Cel(rr, core, pixel / edge));
            col = lerp(col, _Rim.rgb, heart);

            // Sparkles: four-point cel stars in polar cells drifting inward, more at night; faded out when the
            // opening is under about 20 px on screen (they would shimmer on the small rear rings).
            float sparkleLod = saturate((1.0 / pixel - 12.0) / 8.0);
            float sparkle = 0;
            [unroll] for (int layer = 0; layer < 2; layer++)
            {
                float n = 9.0 + layer * 5.0, mRows = 3.0 + layer;
                float a = angle / TwoPi * n + layer * 0.37;
                float b = rr * mRows + _Swirl.w * (0.6 + layer * 0.35);
                float2 cell = floor(float2(a, b));
                float h = Hash(cell + layer * 17.0);
                float2 l = float2(frac(a) - 0.5, frac(b) - 0.5);
                l.x *= TwoPi * max(rr, 0.15) / n * mRows;
                float size = 0.6 + 0.6 * Hash(cell + 3.1);
                l *= 2.2 / size;
                float star = max(saturate(1.0 - abs(l.x) * 9.0) * saturate(1.0 - abs(l.y) * 2.2),
                                 saturate(1.0 - abs(l.y) * 9.0) * saturate(1.0 - abs(l.x) * 2.2));
                float twinkle = 0.75 + 0.25 * sin(_Swirl.w * 9.0 + h * TwoPi);
                float shown = step(1.0 - (_Glow.y * (0.55 + 0.45 * night)) * 0.5, h);
                float sw2 = max(fwidth(star * twinkle), 1e-3);
                sparkle = max(sparkle, shown * smoothstep(0.42 - sw2, 0.42 + sw2, star * twinkle) * Cel(rr, 0.84, pixel / edge));
            }
            col = lerp(col, _Rim.rgb, sparkle * sparkleLod);

            // The flare's shock ring, running out to the rim.
            float shock = _Life.w >= 0 ? Cel(abs(rr - _Life.w), 0.05, pixel / edge) : 0;
            col = lerp(col, _Rim.rgb, shock);

            // The rim: along the hole's painted edge (the mask's edge depth: an ink line, then a hard bright band at
            // the edge), and along the iris's own edge while it springs open inside the hole.
            float depth = m.a;
            float dw = max(fwidth(depth), 1e-3);
            col = lerp(col, _Ink.rgb, Band(depth, 0.33, 0.1, dw));
            col = lerp(col, _Rim.rgb, Cel(depth, 0.2, dw));
            col = lerp(col, _Ink.rgb, Band(rr, 0.88, 0.02, pixel / edge));
            col = lerp(col, _Rim.rgb, 1.0 - Cel(rr, 0.9, pixel / edge));

            float bright = _Glow.z * (1.0 + _Glow.w * night + 0.5 * flare) * (1.0 + _Glow.x * heart);
            float fill = inside * m.r * (1.0 - m.g) * lerp(1.0, _Shape.y, m.b) * _Shape.z;
            return half4(col * bright * fill, fill);
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
