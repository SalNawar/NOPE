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
// The rings as gates (the night of 2026-11-07, DECISIONS D6; PortalGate, PortalEffect): with _Gate.x on, an open
// ring shows the event horizon instead of the swirl: a slow rippling pool in two cel tones of the ring's light
// (_Mid dark water, _Light thin crests), irregular ripple bands running out to the rim under 1 Hz (_Gate.y, _Pool), a
// light centre, a soft rim glow inside the painted edge and the ink line on it; a send's kawoosh (_Gate.w, _Kawoosh) swells
// a plume of the light tone from the core out to its front and back, its heart white-hot and the whole ring lifted
// for the bloom. The chevrons (_Chevron, _ChevronRing) are drawn outside the hole, on the ring's metal band: the
// Stargate's V lamps in a dark housing with an ink outline, lit in the dial's order (three down the right, three
// down the left, the top one last), never over what stands in front. _Gate.z fades the horizon, _Chevron.w the
// chevrons (the close). Every time-varying input comes from PortalEffect, so reduced motion holds the pool still.
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
        _Gate ("x: the event horizon (1) or the swirl (0), y: the ripples' phase (bands), z: the horizon's opacity, w: the kawoosh's envelope", Vector) = (1, 0, 1, 0)
        _Pool ("x: ripple bands from centre to rim, y: their wander (bands), z: the wander's phase (turns), w: the light tone's share of a band", Vector) = (2.6, 0.45, 0, 0.28)
        _Kawoosh ("x: the plume's front (opening radii), y: the flare's brightness at the peak, z: the rim glow, w: the light centre (opening radii)", Vector) = (0.2, 0.7, 0.55, 0.18)
        _ChevronRing ("The chevrons' ellipse: xy its centre's offset, zw its radii (opening radii)", Vector) = (0, 0, 1.22, 1.22)
        _Chevron ("x: the angle between sites (radians), y: a chevron's half-width (mean opening radii), z: lit, w: the chevrons' opacity", Vector) = (0.698, 0.14, 0, 0)
        _ChevronShape ("x: chevrons, y: the opening's aspect (radius across / up)", Vector) = (7, 1, 0, 0)
        _ChevronLit ("A lit chevron", Color) = (1, 0.6, 0.2, 1)
        _ChevronDark ("An unlit chevron's housing", Color) = (0.3, 0.27, 0.3, 1)
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
            float4 _Gate;
            float4 _Pool;
            float4 _Kawoosh;
            float4 _ChevronRing;
            float4 _Chevron;
            float4 _ChevronShape;
            half4 _ChevronLit;
            half4 _ChevronDark;
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

        // The hole channel's coverage kept inside its painted edge (the half level the openings are fitted to):
        // 0 up to the edge, rising to 1 where the mask is whole (about half a mask texel further in), so no pixel whose
        // centre is on the metal is ever tinted.
        float InsideEdge(float v)
        {
            return saturate(2.0 * v - 1.0);
        }

        // What stands in front, kept whole up to its painted edge (the half level) and beyond: the vortex is
        // drawn only where the mask is clear of it, so a rail or a post keeps every pixel it reaches.
        float ClearOf(float v)
        {
            return 1.0 - saturate(2.0 * v);
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

        // The site of chevron i of n (PortalGate.ChevronAngle): three down the right side, three down the left, the top last.
        float ChevronAngle(float i, float n, float gap)
        {
            const float quarter = 1.5707963;
            if (i >= n - 1.0)
                return quarter;
            float right = floor(n * 0.5);
            return i < right ? quarter - gap * (i + 1.0) : quarter + gap * (i - right + 1.0);
        }

        // The opening's radii to its mean radius (a chevron is measured in it, so it keeps its shape on an oval ring).
        float2 ChevronIso()
        {
            float aspect = max(_ChevronShape.y, 1e-3);
            return float2(sqrt(aspect), 1.0 / sqrt(aspect));
        }

        // The chevrons at q (opening radii): rgb premultiplied by a, their coverage. Each is a V lamp pointing at the
        // centre in a dark housing, both inked; lit ones in the dial's order. Measured in the opening's mean radius
        // (ChevronIso), so a chevron keeps its shape on an oval ring. isoPixel: a screen pixel in mean radii (taken
        // outside any branch: no gradient in flow control).
        float4 Chevrons(float2 q, float night, float isoPixel)
        {
            float n = clamp(_ChevronShape.x, 1.0, 9.0);
            float2 iso = ChevronIso();
            float size = max(_Chevron.y, 1e-3);
            float pw = isoPixel / size;                              // a screen pixel in the chevron's own units
            float4 sum = 0;
            [unroll] for (int k = 0; k < 9; k++)
            {
                float i = (float)k;
                float shown = step(i + 0.5, n);
                float t = ChevronAngle(i, n, _Chevron.x);
                float2 dir = float2(cos(t), sin(t));
                float2 site = _ChevronRing.xy + _ChevronRing.zw * dir;
                float2 normal = normalize(dir / max(_ChevronRing.zw * iso, 1e-3));
                float2 tangent = float2(-normal.y, normal.x);
                float2 d = (q - site) * iso;
                float u = dot(d, tangent) / size, v = dot(d, normal) / size;
                float widen = 0.3 * (v + 0.8) / 1.6;                       // the housing widens outward
                float outer = Cel(abs(v), 1.0, pw) * Cel(abs(u) - widen, 1.17, pw) * shown;
                float house = Cel(abs(v), 0.78, pw) * Cel(abs(u) - widen, 0.95, pw);
                float base = -0.55 + 0.85 * abs(u);                         // the V: its tip toward the centre
                float lamp = Cel(abs(u), 1.0, pw) * Cel(base - v, 0.0, pw) * Cel(v - base - 0.5, 0.0, pw);
                float heart = Cel(abs(u), 0.7, pw) * Cel(base + 0.12 - v, 0.0, pw) * Cel(v - base - 0.36, 0.0, pw);
                float lit = step(i + 0.5, _Chevron.z);
                half3 c = _Ink.rgb;
                c = lerp(c, _ChevronDark.rgb * lerp(1.0, 0.55, night), house);
                c = lerp(c, lerp(_Ink.rgb * 1.6, _ChevronLit.rgb * 1.35, lit), lamp);
                c = lerp(c, lerp(_ChevronLit.rgb, half3(1.0, 0.95, 0.8), 0.6) * 1.5, lit * heart);
                sum += float4(c * outer, outer);
            }
            return min(sum, float4(8.0, 8.0, 8.0, 1.0));
        }

        // The event horizon at radius r (opening radii) and angle: two cel tones in irregular ripple bands running
        // out, a light centre, the soft rim glow inside the painted edge (edge depth d, 0 at the edge), the kawoosh's
        // plume and heart, and a light rim while the iris opens. pixel: opening radii per screen pixel.
        half3 Horizon(float r, float angle, float d, float pixel, float iris)
        {
            float rings = max(_Pool.x, 0.5);
            // Whole turns of the wander's phase in both terms, so its wrap at 1 (PortalEffect) is seamless.
            float wander = _Pool.y * (0.6 * sin(3.0 * angle + _Pool.z * TwoPi) + 0.4 * sin(5.0 * angle - _Pool.z * TwoPi + 2.0 * r));
            float s = r * rings + wander - _Gate.y;
            float share = saturate(_Pool.w);
            half3 col = lerp(_Mid.rgb, _Light.rgb, Band(s, 0.5 * share, 0.5 * share, 1.5 * rings * pixel));
            col = lerp(col, _Light.rgb, Cel(r, _Kawoosh.w, pixel));
            col = lerp(col, _Rim.rgb, saturate(1.0 - d / 0.9) * _Kawoosh.z);
            float e = saturate(_Gate.w);
            float front = max(_Kawoosh.x, 0.05);
            col = lerp(col, _Light.rgb, e * (1.0 - smoothstep(0.5 * front, 1.25 * front, r)));
            col = lerp(col, _Rim.rgb, e * (1.0 - smoothstep(0.15 * front, 0.6 * front, r)));
            float springing = 1.0 - smoothstep(0.85, 1.0, iris);
            col = lerp(col, _Rim.rgb, springing * 0.6);
            return col;
        }

        half4 Frag(Varyings input) : SV_Target
        {
            float2 q = (input.uv - 0.5) * 2.0 * Margin;     // in opening radii
            float r = length(q);
            float angle = atan2(q.y, q.x);
            float night = saturate(_Life.z);
            float flare = saturate(_Life.y);
            float pixel = max(length(fwidth(q)), 1e-4);      // opening radii per screen pixel
            float isoPixel = max(length(fwidth(q * ChevronIso())), 1e-4);

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
            // opening is under about 20 px on screen (they would shimmer on the small rear rings). Their arms are
            // at least about a screen pixel across (integration, 2026-10-21: at 1280 x 720 an arm was a third of
            // a pixel, so a star popped on and off a pixel within a frame or two, 3 times in 10 s under the flicker
            // check at H5), dimmed a little as they widen.
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
                // l per screen pixel, the same across and along (the cell's rows and columns are both scaled by it).
                float lPixel = mRows * pixel / edge * 2.2 / size;
                float arm = max(1.0 / 9.0, 1.2 * lPixel);
                float armDim = sqrt((1.0 / 9.0) / arm);
                float star = max(saturate(1.0 - abs(l.x) / arm) * saturate(1.0 - abs(l.y) * 2.2),
                                 saturate(1.0 - abs(l.y) / arm) * saturate(1.0 - abs(l.x) * 2.2));
                float twinkle = 0.75 + 0.25 * sin(_Swirl.w * 9.0 + h * TwoPi);
                float shown = step(1.0 - (_Glow.y * (0.55 + 0.45 * night)) * 0.5, h);
                float sw2 = max(fwidth(star * twinkle), 1e-3);
                sparkle = max(sparkle, armDim * shown * smoothstep(0.42 - sw2, 0.42 + sw2, star * twinkle) * Cel(rr, 0.84, pixel / edge));
            }
            col = lerp(col, _Rim.rgb, sparkle * sparkleLod);

            // The flare's shock ring, running out to the rim.
            float shock = _Life.w >= 0 ? Cel(abs(rr - _Life.w), 0.05, pixel / edge) : 0;
            col = lerp(col, _Rim.rgb, shock);

            // The rim: along the hole's painted edge (the mask's edge depth: an ink line, then a hard bright band at
            // the edge), and along the iris's own edge only while it springs open inside the hole: it fades out
            // as the iris reaches the hole's reach (_Life.x 0.85 to 1), so a full-open vortex has no second ink
            // line or rim band that does not follow the painted edge (an arch's corners past 0.9 of its reach).
            float depth = m.a;
            float dw = max(fwidth(depth), 1e-3);
            float springing = 1.0 - smoothstep(0.85, 1.0, iris);
            col = lerp(col, _Ink.rgb, Band(depth, 0.33, 0.1, dw));
            col = lerp(col, _Rim.rgb, Cel(depth, 0.2, dw));
            col = lerp(col, _Ink.rgb, springing * Band(rr, 0.88, 0.02, pixel / edge));
            col = lerp(col, _Rim.rgb, springing * (1.0 - Cel(rr, 0.9, pixel / edge)));

            float bright = _Glow.z * (1.0 + _Glow.w * night + 0.5 * flare) * (1.0 + _Glow.x * heart);

            // The event horizon in place of the swirl (the gate's look): its own tones, the ink line on the painted
            // edge kept, the kawoosh lifting the whole ring for the bloom.
            if (_Gate.x > 0.5)
            {
                col = Horizon(r, angle, depth, pixel, iris);
                col = lerp(col, _Ink.rgb, Band(depth, 0.33, 0.1, dw));
                bright = _Glow.z * (1.0 + _Glow.w * night) * (1.0 + _Kawoosh.y * saturate(_Gate.w));
            }

            // The hole and what stands in front of it end at their painted edges, the masks' half level (the edge
            // the openings are fitted to), the antialiasing kept on the hole's side (integration, 2026-10-21: the
            // bilinear fringe outside the half level drew the bright rim at 10-50 % on the metal's edge pixels,
            // half a pixel of halo all round, 3-9 % on a small ring's axes). _Gate.z fades it (the close).
            float fill = inside * InsideEdge(m.r) * ClearOf(m.g) * lerp(1.0, _Shape.y, m.b) * _Shape.z * saturate(_Gate.z);

            // The chevrons on the metal: never in the hole, never over what stands in front of it.
            float4 chevrons = 0;
            if (_Chevron.w > 0.0)
                chevrons = Chevrons(q, night, isoPixel) * saturate(_Chevron.w) * (1.0 - InsideEdge(m.r)) * ClearOf(m.g);
            float alpha = fill + chevrons.a * (1.0 - fill);
            return half4(col * bright * fill + chevrons.rgb * (1.0 - fill), alpha);
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
