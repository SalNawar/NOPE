// The Helix River (stability without a number; docs/superpowers/specs/2026-10-07-helix-river-design.md):
// history as a double helix (amber front strand, blue back strand, rungs in the eight nations' colours)
// flowing left to right between two green banks on dark CRT glass, over a faint water band with current
// streaks, bright motes riding the strands downstream. Drawn procedurally (distances to curves, no
// textures) from the parameters HelixRiverMonitor sets each frame from the pure HelixRiver: the meander,
// the twist pushing past the banks (red bank segments, blue flood blooms), snapped and red-mutated rungs,
// drifting oxbows, the unzipping and fraying leading end, a citation's red pulse, and the CRT finish
// (scanlines, vignette, grain, glitch band shifts, chroma split, flicker). Lengths are in the reference
// sheet's pixels (a 380 px tall glass; helixriver.py), so it looks the same at any size; lines widen to at
// least a screen pixel when drawn small. Composited additively in gamma space as the reference was, then
// converted to linear. One unlit pass without a LightMode, so it draws on a world-space quad (the desk's
// stability monitor) and in uGUI (the taskbar strip, the shift report, the Home HUD; stencil and
// RectMask2D clipping supported).
Shader "TimeDesk/HelixRiver"
{
    Properties
    {
        [PerRendererData] _MainTex ("Unused (uGUI)", 2D) = "white" {}
        _Calm ("Calm", Range(0, 1)) = 1
        _RiverTime ("Clock (s)", Float) = 0
        _Meander ("Meander (px)", Float) = 6
        _Twist ("Twist (px)", Float) = 26
        _Unzip ("Unzip (px)", Float) = 0
        _UnzipStart ("Unzip start (share)", Float) = 0.97
        _Snap ("Snapped rungs", Range(0, 1)) = 0
        _Mutate ("Mutated share", Range(0, 1)) = 0
        _Oxbows ("Oxbows", Float) = 0
        _Fray ("Fray", Float) = 0
        _Flow ("Mote speed (px/s)", Float) = 90
        _Stray ("Mote stray (px)", Float) = 0
        _Drift ("Colour drift", Range(0, 1)) = 0
        _Glitch ("Glitch", Range(0, 1)) = 0
        _Flicker ("Flicker", Range(0, 1)) = 0
        _Pulse ("Pulse head (-1 none)", Float) = -1
        _PulseGlow ("Pulse glow", Range(0, 1)) = 0
        _Aspect ("Width / height", Float) = 2.37
        _Zoom ("Zoom", Float) = 1
        _Corner ("Corner radius (share of height)", Range(0, 0.5)) = 0

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" "CanUseSpriteAtlas" = "True" "RenderPipeline" = "UniversalPipeline" }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "HelixRiver"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _Calm, _RiverTime, _Meander, _Twist, _Unzip, _UnzipStart, _Snap, _Mutate, _Oxbows, _Fray;
                float _Flow, _Stray, _Drift, _Glitch, _Flicker, _Pulse, _PulseGlow, _Aspect, _Zoom, _Corner;
                float4 _ClipRect;
            CBUFFER_END

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
                half4 color : COLOR;
                float2 local : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                output.local = input.positionOS.xy;
                return output;
            }

            // The reference glass (px) and the banks' distance from its middle.
            static const float H = 380.0;
            static const float BANK = 86.0;

            static const float3 AMBER = float3(1.0, 0.72, 0.32);
            static const float3 AMBER_RED = float3(1.0, 0.42, 0.25);
            static const float3 RED = float3(1.0, 0.30, 0.22);
            static const float3 BLUE = float3(0.45, 0.70, 1.0);
            static const float3 GREEN = float3(0.42, 0.80, 0.50);
            static const float3 FLOOD = float3(0.35, 0.55, 1.0);
            static const float3 WATER = float3(0.30, 0.50, 0.85);
            static const float3 STREAK = float3(0.55, 0.75, 1.0);
            static const float3 MOTE = float3(1.0, 0.95, 0.80);
            static const float3 GLASS_TOP = float3(0.075, 0.085, 0.07);
            static const float3 GLASS_BOTTOM = float3(0.045, 0.05, 0.045);

            // The eight nations' colours, in the reference's order.
            float3 Nation(float index)
            {
                int i = (int)(index - 8.0 * floor(index / 8.0) + 0.5) % 8;
                if (i == 0) return float3(0.86, 0.36, 0.40);
                if (i == 1) return float3(0.45, 0.62, 0.95);
                if (i == 2) return float3(1.0, 0.45, 0.30);
                if (i == 3) return float3(1.0, 0.75, 0.35);
                if (i == 4) return float3(0.55, 0.85, 0.45);
                if (i == 5) return float3(0.72, 0.55, 0.95);
                if (i == 6) return float3(0.40, 0.85, 0.85);
                return float3(0.95, 0.62, 0.30);
            }

            float Hash11(float p)
            {
                p = frac(p * 0.1031);
                p *= p + 33.33;
                p *= p + p;
                return frac(p);
            }

            float Hash21(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            // A line's light at distance d (px): its body (width px, antialiased over a screen pixel of size px) and its
            // phosphor glow (two gaussians, as the reference's blur); drawn at least a screen pixel wide, its light kept
            // (a thinner line is dimmer), as the reference reads scaled down.
            float Glow(float d, float width, float sigma, float px)
            {
                float drawn = max(width, px * 1.2);
                sigma = max(sigma, px);
                float body = saturate((drawn * 0.5 - d) / px + 0.5) * (width / drawn);
                float g1 = width / (sigma * 2.5066) * exp(-d * d / (2.0 * sigma * sigma)) * 1.5;
                float g2 = width / (sigma * 7.52) * exp(-d * d / (18.0 * sigma * sigma)) * 0.6;
                return body + 0.5 * (g1 + g2);
            }

            // Distance from p to the segment a-b.
            float Segment(float2 p, float2 a, float2 b)
            {
                float2 ab = b - a;
                float h = saturate(dot(p - a, ab) / max(dot(ab, ab), 1e-5));
                return length(p - a - ab * h);
            }

            float Centre(float x, float t)
            {
                return H * 0.5 + _Meander * (0.65 * sin(x / 150.0 - t * 0.35) + 0.35 * sin(x / 63.0 + t * 0.5 + 1.1));
            }

            // The strands at x: front (y1) and back (y2), the centre line and the twist's phase.
            void Strands(float x, float t, float w, out float y1, out float y2, out float yc, out float phase)
            {
                yc = Centre(x, t);
                phase = x / 44.0 - t * 1.6;
                float unzip = saturate((x - w * _UnzipStart) / (w * 0.35)) * _Unzip;
                float a = _Twist * sin(phase);
                y1 = yc + a + unzip;
                y2 = yc - a - unzip;
            }

            float3 Scene(float x, float y, float t, float w, float px)
            {
                float cy = H * 0.5;
                float3 amber = lerp(AMBER, AMBER_RED, _Drift);
                float3 back = amber * 0.45;
                float3 col = 0;

                // The water between the banks, and its current streaks.
                float dy = abs(y - cy);
                col += WATER * 0.35 * (55.0 / 255.0) * (smoothstep(BANK + 3.0, BANK - 3.0, dy) + 1.05 * smoothstep(BANK + 12.0, BANK - 12.0, dy));
                if (dy < BANK - 2.0)
                {
                    float row = floor((y - (cy - BANK)) / 6.0);
                    float rowY = cy - BANK + row * 6.0 + 3.0;
                    float speed = 40.0 + Hash11(row * 7.13 + 1.0) * 40.0;
                    float u = x - t * speed;
                    float cell = floor(u / 90.0);
                    float there = Hash21(float2(row, cell));
                    if (there < 0.3)
                    {
                        float len = 10.0 + Hash21(float2(cell, row + 3.0)) * 18.0;
                        float x0 = cell * 90.0 + Hash21(float2(cell + 5.0, row)) * (90.0 - len);
                        float d = Segment(float2(u, y), float2(x0, rowY), float2(x0 + len, rowY));
                        col += STREAK * 0.45 * (0.47 + 1.3 * there) * Glow(d, 1.2, 1.2, px);
                    }
                }

                // The strands, front or back by the twist (a soft swap), at their true distance.
                float y1, y2, yc, ph, a1, a2, ac, aph, b1, b2, bc, bph;
                Strands(x, t, w, y1, y2, yc, ph);
                Strands(x - 0.5, t, w, a1, a2, ac, aph);
                Strands(x + 0.5, t, w, b1, b2, bc, bph);
                float d1 = abs(y - y1) / sqrt(1.0 + (b1 - a1) * (b1 - a1));
                float d2 = abs(y - y2) / sqrt(1.0 + (b2 - a2) * (b2 - a2));
                float front = smoothstep(-0.12, 0.12, cos(ph));
                col += lerp(back * Glow(d1, 3.0, 1.8, px), amber * Glow(d1, 3.0, 2.5, px), front);
                col += lerp(back * Glow(d2, 2.4, 1.8, px), BLUE * 0.9 * Glow(d2, 2.4, 2.2, px), 1.0 - front);

                // The banks: green, a fainter outer line; red where the helix pushes past them.
                float topBank = cy - BANK, bottomBank = cy + BANK;
                float overTop = saturate((topBank - min(y1, y2)) * 0.5 + 0.5);
                float overBottom = saturate((max(y1, y2) - bottomBank) * 0.5 + 0.5);
                float dt = abs(y - topBank), db = abs(y - bottomBank);
                // (Drawn small, the banks keep most of a screen pixel's light: they frame the river, and their red breaches are the warning.)
                float bankWidth = max(2.0, px * 0.8), breachWidth = max(2.6, px);
                col += lerp(GREEN * 0.75 * (Glow(dt, bankWidth, 1.6, px) + (110.0 / 255.0) * Glow(abs(y - (topBank - 6.0)), 1.2, 1.6, px)), RED * Glow(dt, breachWidth, 3.0, px), overTop);
                col += lerp(GREEN * 0.75 * (Glow(db, bankWidth, 1.6, px) + (110.0 / 255.0) * Glow(abs(y - (bottomBank + 6.0)), 1.2, 1.6, px)), RED * Glow(db, breachWidth, 3.0, px), overBottom);

                // Flood blooms leaking out of each breach.
                if (_Meander + _Twist + _Unzip > BANK * 0.6)
                {
                    float flood = 0.0;
                    float baseCell = floor(x / 19.0);
                    [unroll] for (int i = -3; i <= 3; i++)
                    {
                        float c = baseCell + i;
                        float xs = c * 19.0 + 9.5;
                        float s1, s2, sc, sph;
                        Strands(xs, t, w, s1, s2, sc, sph);
                        float r = 18.0 + (1.0 - _Calm) * 46.0 * Hash11(c * 1.731 + 0.5);
                        float upOver = topBank - min(s1, s2);
                        float downOver = max(s1, s2) - bottomBank;
                        float2 eu = float2((x - xs) / (1.6 * r), (y - (topBank - 0.4 * r)) / (0.6 * r));
                        float2 ed = float2((x - xs) / (1.6 * r), (y - (bottomBank + 0.4 * r)) / (0.6 * r));
                        flood = max(flood, saturate(upOver / 6.0) * exp(-dot(eu, eu) * 1.5));
                        flood = max(flood, saturate(downOver / 6.0) * exp(-dot(ed, ed) * 1.5));
                    }
                    col += FLOOD * 0.45 * (150.0 / 255.0) * 1.6 * flood;
                }

                // Rungs in the nations' colours, riding the twist; snapped (two stubs) and some mutated red.
                {
                    float speed = 70.4;
                    float id = floor((x - t * speed) / 16.0 + 0.5);
                    float xr = id * 16.0 + t * speed;
                    float r1, r2, rc, rph;
                    Strands(xr, t, w, r1, r2, rc, rph);
                    if (abs(r1 - r2) <= _Twist * 2.4)
                    {
                        float lo = min(r1, r2), hi = max(r1, r2);
                        float3 nation = Nation(id);
                        if (Hash11(id * 0.1731 + 0.3) >= _Snap)
                        {
                            float d = Segment(float2(x, y), float2(xr, lo), float2(xr, hi));
                            col += nation * 0.9 * (210.0 / 255.0) * Glow(d, 2.0, 1.5, px);
                        }
                        else
                        {
                            float stub = (hi - lo) * 0.3;
                            float d = min(Segment(float2(x, y), float2(xr, lo), float2(xr, lo + stub)), Segment(float2(x, y), float2(xr, hi - stub), float2(xr, hi)));
                            bool mutated = Hash11(id * 0.917 + 4.1) < _Mutate;
                            col += (mutated ? RED * Glow(d, 2.0, 2.2, px) : nation * 0.9 * Glow(d, 2.0, 1.5, px));
                        }
                    }
                }

                // The current: bright motes riding the strands downstream, straying when unstable.
                {
                    float j = floor((x - t * _Flow) / 61.0 + 0.5);
                    float xm = j * 61.0 + t * _Flow;
                    float m1, m2, mc, mph;
                    Strands(xm, t, w, m1, m2, mc, mph);
                    float hj = Hash11(j * 0.37 + 9.2);
                    float ym = (frac(j * 0.5) > 0.25 ? m1 : m2) + (hj * 2.0 - 1.0) * _Stray * sin(t * 2.3 + hj * 6.2832);
                    col += MOTE * 0.9 * Glow(length(float2(x - xm, y - ym)), 4.0, 1.6, px);
                }

                // Oxbows: loops of helix pinched off beyond the banks, drifting downstream and fading.
                if (_Oxbows > 0.0)
                {
                    [loop] for (int k = 0; k < 8; k++)
                    {
                        float fade = saturate(_Oxbows - k);
                        if (fade <= 0.0)
                            break;
                        float seed = k * 3.17 + 100.0;
                        float life = 7.0;
                        float age = fmod(t + Hash11(seed) * life, life);
                        float x0 = fmod(Hash11(seed + 1.0) * 0.6 * w + 0.3 * w + age * 40.0, w);
                        float side = Hash11(seed + 2.0) < 0.5 ? -1.0 : 1.0;
                        float y0 = cy + side * (BANK + 30.0 + Hash11(seed + 3.0) * 60.0);
                        float rx = 26.0 + Hash11(seed + 4.0) * 24.0, ry = 12.0 + Hash11(seed + 5.0) * 8.0;
                        float alpha = (220.0 / 255.0) * (1.0 - age / life) * saturate(age / 0.6) * fade;
                        float2 q = float2(x - x0, y - y0);
                        float e = length(q / float2(rx, ry));
                        float grad = length(float2(q.x / (rx * rx), q.y / (ry * ry))) / max(e, 1e-3);
                        col += back * alpha * Glow(abs(e - 1.0) / max(grad, 1e-4), 2.4, 1.8, px);
                        float n = clamp(floor((x - (x0 - rx * 0.8)) / (rx * 0.32) + 0.5), 0.0, 5.0);
                        float xx = x0 - rx * 0.8 + n * rx * 0.32;
                        float d = length(float2(x - xx, max(abs(y - y0) - ry * 0.7, 0.0)));
                        col += Nation(k + n) * 0.9 * alpha * 0.8 * Glow(d, 1.6, 1.5, px);
                    }
                }

                // Frayed fibres at the leading ("now") end.
                if (_Fray > 0.0 && x > w * 0.75)
                {
                    [loop] for (int f = 0; f < 24; f++)
                    {
                        float fade = saturate(_Fray - f);
                        if (fade <= 0.0)
                            break;
                        float seed = f * 5.31 + 300.0;
                        float fx = w * (0.8 + 0.2 * Hash11(seed));
                        float fy = Centre(fx, t) + (Hash11(seed + 1.0) * 2.0 - 1.0) * 90.0;
                        float2 to = float2(fx + 8.0 + Hash11(seed + 2.0) * 32.0, fy + (Hash11(seed + 3.0) * 2.0 - 1.0) * 30.0 + sin(t * 1.7 + Hash11(seed + 4.0) * 6.2832) * 4.0);
                        col += amber * (140.0 / 255.0) * fade * Glow(Segment(float2(x, y), float2(fx, fy), to), 1.1, 2.5, px);
                    }
                }

                // A citation: a red pulse running downstream along the river (or, under Reduced Motion, a fading glow).
                float dc = abs(y - yc) / sqrt(1.0 + (bc - ac) * (bc - ac));
                if (_Pulse >= 0.0)
                {
                    float head = _Pulse * (w + 200.0) - 100.0;
                    float weight = exp(-((x - head) / 40.0) * ((x - head) / 40.0));
                    col += RED * 1.6 * weight * Glow(dc, 16.0 * weight + 2.0, 5.0, px);
                }
                col += RED * _PulseGlow * 0.6 * Glow(dc, 6.0, 5.0, px);
                return col;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float viewHeight = H / max(_Zoom, 0.01);
                float w = viewHeight * max(_Aspect, 0.01);
                float x = uv.x * w;
                float y = H * 0.5 + (0.5 - uv.y) * viewHeight;
                float px = max(fwidth(y), 1e-3);
                float t = _RiverTime;
                float slot = floor(t * 10.0);

                // Glitch: some bands slip sideways for a tenth of a second.
                if (_Glitch > 0.0)
                {
                    float band = floor(y / 8.0);
                    if (Hash21(float2(band, slot)) < _Glitch * 0.12)
                        x += (Hash21(float2(band + 3.1, slot)) * 2.0 - 1.0) * 16.0;
                }

                float3 col;
                if (_Glitch > 0.001)
                {
                    float shift = 1.0 + _Glitch * 3.0;
                    col.r = Scene(x - shift, y, t, w, px).r;
                    col.g = Scene(x, y, t, w, px).g;
                    col.b = Scene(x + shift, y, t, w, px).b;
                }
                else
                {
                    col = Scene(x, y, t, w, px);
                }

                // The glass behind the light, a soft reflection, scanlines, vignette, grain, flicker.
                col += lerp(GLASS_TOP, GLASS_BOTTOM, saturate(1.0 - uv.y));
                float2 r = (uv - float2(0.3, 0.85)) / float2(0.45, 0.35);
                col += float3(1.0, 0.97, 0.9) * 0.06 * exp(-dot(r, r) * 2.0);
                col *= 1.0 - 0.10 * step(0.3, sin(input.positionCS.y * PI / 1.5));
                float2 v = (uv - 0.5) * 2.0;
                col *= 1.0 - 0.35 * saturate(v.x * v.x * 0.6 + v.y * v.y - 0.25);
                col += (Hash21(input.positionCS.xy + frac(t * 30.0) * 97.0) - 0.5) * 0.04;
                col *= 1.0 - _Flicker * 0.18 * step(0.6, Hash11(floor(t * 15.0) * 0.13 + 0.7));
                col = saturate(col);
                #ifndef UNITY_COLORSPACE_GAMMA
                col = SRGBToLinear(col);
                #endif

                // Rounded glass corners (a share of the height), the vertex colour and uGUI's rect clipping.
                float alpha = input.color.a;
                if (_Corner > 0.0)
                {
                    float2 halfSize = float2(_Aspect * 0.5, 0.5);
                    float2 q = abs((uv - 0.5) * float2(_Aspect, 1.0)) - halfSize + _Corner;
                    float d = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - _Corner;
                    alpha *= saturate(0.5 - d / max(fwidth(d), 1e-5));
                }
                #ifdef UNITY_UI_CLIP_RECT
                float2 inside = step(_ClipRect.xy, input.local) * step(input.local, _ClipRect.zw);
                alpha *= inside.x * inside.y;
                #endif
                return half4(col * input.color.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
