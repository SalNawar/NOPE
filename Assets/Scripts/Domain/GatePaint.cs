using System;

/// <summary>
/// An RGBA canvas the dialer's art is painted on (GatePaint): rows top
/// first, straight alpha, each shape antialiased by its signed distance (one
/// sample a pixel), laid over what is there. Pure.
/// </summary>
public sealed class GateCanvas
{
    /// <summary>A clear canvas of <paramref name="width"/> by <paramref name="height"/> pixels.</summary>
    public GateCanvas(int width, int height)
    {
        Width = Math.Max(1, width);
        Height = Math.Max(1, height);
        Pixels = new byte[Width * Height * 4];
    }

    /// <summary>Its width in pixels.</summary>
    public int Width { get; }

    /// <summary>Its height in pixels.</summary>
    public int Height { get; }

    /// <summary>R, G, B, A for every pixel, rows top first.</summary>
    public byte[] Pixels { get; }

    /// <summary>The colour at (<paramref name="x"/>, <paramref name="y"/>) as (r, g, b, a) 0 to 255.</summary>
    public (byte r, byte g, byte b, byte a) At(int x, int y)
    {
        int i = (Math.Max(0, Math.Min(Height - 1, y)) * Width + Math.Max(0, Math.Min(Width - 1, x))) * 4;
        return (Pixels[i], Pixels[i + 1], Pixels[i + 2], Pixels[i + 3]);
    }

    /// <summary>Lays <paramref name="colour"/> (alpha 0 to 1 scaled by <paramref name="cover"/>) over pixel (x, y).</summary>
    public void Blend(int x, int y, GateColour colour, float cover)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height)
            return;
        float a = Math.Max(0f, Math.Min(1f, cover)) * colour.A;
        if (a <= 0f)
            return;
        int i = (y * Width + x) * 4;
        float da = Pixels[i + 3] / 255f;
        float oa = a + da * (1f - a);
        if (oa <= 0f)
            return;
        Pixels[i] = Mix(Pixels[i] / 255f, colour.R, da, a, oa);
        Pixels[i + 1] = Mix(Pixels[i + 1] / 255f, colour.G, da, a, oa);
        Pixels[i + 2] = Mix(Pixels[i + 2] / 255f, colour.B, da, a, oa);
        Pixels[i + 3] = (byte)MathF.Round(oa * 255f);
    }

    private static byte Mix(float d, float s, float da, float a, float oa) => (byte)MathF.Round(Math.Max(0f, Math.Min(1f, (s * a + d * da * (1f - a)) / oa)) * 255f);

    /// <summary>Fills every pixel within <paramref name="x0"/>..<paramref name="x1"/>, <paramref name="y0"/>..<paramref name="y1"/> whose signed distance (<paramref name="sdf"/>, pixels, negative inside) is inside, the edge antialiased; <paramref name="shade"/> (0 at the top of the box to 1 at its bottom) picks the colour.</summary>
    public void Fill(float x0, float y0, float x1, float y1, Func<float, float, float> sdf, Func<float, GateColour> shade)
    {
        int ix0 = Math.Max(0, (int)MathF.Floor(x0) - 1), iy0 = Math.Max(0, (int)MathF.Floor(y0) - 1);
        int ix1 = Math.Min(Width - 1, (int)MathF.Ceiling(x1) + 1), iy1 = Math.Min(Height - 1, (int)MathF.Ceiling(y1) + 1);
        float h = Math.Max(1f, y1 - y0);
        for (int y = iy0; y <= iy1; y++)
        {
            GateColour c = shade((y + 0.5f - y0) / h);
            for (int x = ix0; x <= ix1; x++)
            {
                float d = sdf(x + 0.5f, y + 0.5f);
                if (d < 0.5f)
                    Blend(x, y, c, 0.5f - d);
            }
        }
    }

    /// <summary>A rounded rectangle (<paramref name="r"/>: the corners' radius), filled with <paramref name="fill"/> to <paramref name="fillBottom"/> top to bottom.</summary>
    public void RoundRect(BoardRect rect, float r, GateColour fill, GateColour fillBottom) =>
        Fill(rect.X, rect.Y, rect.X + rect.W, rect.Y + rect.H, (x, y) => GatePaint.RoundRectDistance(rect, r, x, y), t => fill.Lerp(fillBottom, t));

    /// <summary>A rounded rectangle's outline <paramref name="width"/> pixels wide, inside its edge.</summary>
    public void RoundRectLine(BoardRect rect, float r, float width, GateColour ink) =>
        Fill(rect.X, rect.Y, rect.X + rect.W, rect.Y + rect.H, (x, y) => Math.Abs(GatePaint.RoundRectDistance(rect, r, x, y) + width / 2f) - width / 2f, t => ink);

    /// <summary>A ring between <paramref name="inner"/> and <paramref name="outer"/> about (<paramref name="cx"/>, <paramref name="cy"/>) (inner 0: a disc), shaded top to bottom.</summary>
    public void Annulus(float cx, float cy, float inner, float outer, GateColour top, GateColour bottom) =>
        Fill(cx - outer, cy - outer, cx + outer, cy + outer, (x, y) =>
        {
            float d = MathF.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            return Math.Max(d - outer, inner - d);
        }, t => top.Lerp(bottom, t));
}

/// <summary>A colour of the dialer's art, 0 to 1 a channel. Not serialized.</summary>
public readonly struct GateColour
{
    /// <summary>Red, green, blue, alpha.</summary>
    public readonly float R, G, B, A;

    /// <summary>The colour (<paramref name="r"/>, <paramref name="g"/>, <paramref name="b"/>) 0 to 255, at <paramref name="a"/> 0 to 1.</summary>
    public GateColour(int r, int g, int b, float a = 1f)
    {
        R = r / 255f;
        G = g / 255f;
        B = b / 255f;
        A = a;
    }

    private GateColour(float r, float g, float b, float a, bool _)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    /// <summary><paramref name="t"/> of the way to <paramref name="other"/>.</summary>
    public GateColour Lerp(GateColour other, float t)
    {
        t = Math.Max(0f, Math.Min(1f, t));
        return new GateColour(R + (other.R - R) * t, G + (other.G - G) * t, B + (other.B - B) * t, A + (other.A - A) * t, true);
    }

    /// <summary>The colour at alpha <paramref name="a"/>.</summary>
    public GateColour WithAlpha(float a) => new GateColour(R, G, B, a, true);
}

/// <summary>
/// The dialer's art, painted in code in the board kit's palette (brass,
/// oxblood, slate, ink: the v3 board's): the slate face over the Classic
/// board's jacks (its wells, rings, rack and placards), a dial's inner ring
/// with its glyph discs, the locked glyph's lit disc, a chevron, a power cell,
/// the gate's horizon, the legend plates, and the eras' glyphs and the point
/// of origin as inked shapes (the nations wear their passport emblems,
/// EmblemArt). Pure: the office turns each canvas into a sprite (GateArt).
/// </summary>
public static class GatePaint
{
    /// <summary>The kit's palette.</summary>
    public static readonly GateColour Ink = new GateColour(18, 14, 12), Slate = new GateColour(48, 52, 57), SlateDeep = new GateColour(31, 34, 38),
        SlateWell = new GateColour(22, 24, 27), Brass = new GateColour(205, 160, 84), BrassDeep = new GateColour(118, 84, 38),
        BrassLight = new GateColour(236, 198, 122), Oxblood = new GateColour(112, 32, 26), Cream = new GateColour(240, 230, 204),
        Hazard = new GateColour(232, 186, 44), Danger = new GateColour(176, 42, 32), CellCore = new GateColour(255, 222, 156),
        DiscBrass = new GateColour(210, 182, 126), DiscBrassDeep = new GateColour(166, 126, 66), Lit = new GateColour(255, 250, 234), LitDeep = new GateColour(250, 236, 200);

    // ------------------------------------------------------------------ shapes

    /// <summary>The signed distance from (<paramref name="x"/>, <paramref name="y"/>) to a rounded rectangle (negative inside).</summary>
    public static float RoundRectDistance(BoardRect rect, float r, float x, float y)
    {
        r = Math.Max(0f, Math.Min(r, Math.Min(rect.W, rect.H) / 2f));
        float cx = rect.X + rect.W / 2f, cy = rect.Y + rect.H / 2f;
        float qx = Math.Abs(x - cx) - (rect.W / 2f - r), qy = Math.Abs(y - cy) - (rect.H / 2f - r);
        float ox = Math.Max(qx, 0f), oy = Math.Max(qy, 0f);
        return MathF.Sqrt(ox * ox + oy * oy) + Math.Min(Math.Max(qx, qy), 0f) - r;
    }

    /// <summary>True when (<paramref name="x"/>, <paramref name="y"/>) lies inside <paramref name="poly"/> (x, y pairs).</summary>
    public static bool InPolygon(float[] poly, float x, float y)
    {
        bool inside = false;
        for (int i = 0, j = poly.Length / 2 - 1; i < poly.Length / 2; j = i++)
        {
            float xi = poly[2 * i], yi = poly[2 * i + 1], xj = poly[2 * j], yj = poly[2 * j + 1];
            if (yi > y != yj > y && x < (xj - xi) * (y - yi) / (yj - yi) + xi)
                inside = !inside;
        }
        return inside;
    }

    private static readonly float[] Bolt = { 0.18f, -0.9f, -0.5f, 0.1f, -0.05f, 0.1f, -0.22f, 0.9f, 0.52f, -0.18f, 0.06f, -0.18f };
    private static readonly float[] Sail = { 0.05f, -0.82f, 0.05f, 0.28f, 0.66f, 0.28f };
    private static readonly float[] Jib = { -0.05f, -0.6f, -0.05f, 0.28f, -0.52f, 0.28f };
    private static readonly float[] Hull = { -0.82f, 0.38f, 0.82f, 0.38f, 0.56f, 0.72f, -0.56f, 0.72f };

    /// <summary>
    /// True when (<paramref name="x"/>, <paramref name="y"/>) in [-1, 1] (y
    /// down) is inked in era <paramref name="era"/>'s glyph (0 Ancient: a
    /// pyramid of 3 steps; 1 Medieval: a tower with 2 windows; 2 Early
    /// modern: a ship under 2 sails; 3 Industrial: a gear round 1 hub; 4
    /// Modern: 1 bolt: each glyph has as many parts as the bars its era
    /// needs, D54) or, for -1, the point of origin (the Bureau's hourglass).
    /// Nothing for another era.
    /// </summary>
    public static bool EraInked(int era, float x, float y)
    {
        switch (era)
        {
            case -1:
                if (Math.Abs(y) > 0.86f || Math.Abs(x) > 0.7f)
                    return false;
                if (Math.Abs(y) >= 0.72f)
                    return true;
                return Math.Abs(x) <= 0.08f + 0.56f * Math.Abs(y) / 0.72f && !(Math.Abs(x) < 0.04f + 0.4f * Math.Abs(y) / 0.72f && y < -0.1f);
            case 0:
                for (int t = 0; t < 3; t++)
                {
                    float top = 0.72f - (t + 1) * 0.46f, half = 0.86f - t * 0.27f;
                    if (y >= top && y <= top + 0.46f && Math.Abs(x) <= half)
                        return !(t == 0 && Math.Abs(x) < 0.14f && y > 0.42f);
                }
                return false;
            case 1:
                if (Math.Abs(x) <= 0.5f && y >= -0.42f && y <= 0.86f)
                    return !(Math.Abs(x) < 0.17f && y > 0.46f) && !(Math.Abs(x) < 0.11f && (Math.Abs(y + 0.22f) < 0.12f || Math.Abs(y - 0.14f) < 0.12f));
                return y >= -0.82f && y < -0.42f && Math.Abs(x) <= 0.66f && ((int)MathF.Floor((x + 0.66f) / 0.264f) % 2 == 0);
            case 2:
                return InPolygon(Sail, x, y) || InPolygon(Jib, x, y) || InPolygon(Hull, x, y) || (Math.Abs(x) < 0.04f && y > -0.9f && y < 0.4f);
            case 3:
            {
                float r = MathF.Sqrt(x * x + y * y), a = MathF.Atan2(y, x);
                float teeth = MathF.Cos(a * 8f) > 0.25f ? 0.9f : 0.68f;
                return r <= teeth && r >= 0.28f;
            }
            case 4:
                return InPolygon(Bolt, x, y);
            default:
                return false;
        }
    }

    // ------------------------------------------------------------------ the face

    /// <summary>
    /// The slate face (<paramref name="layout"/>'s cover, its own pixels: the
    /// cover's top-left is (0, 0)) with everything on it that does not move:
    /// the call row's plates, emblem wells, lamp bezels and the pip wells of
    /// <paramref name="periods"/> eras; each of the <paramref name="dials"/>
    /// dials' outer ring, inner ring bed and hub; the big dials' cradle wells
    /// and display bezels; the rack and its bays; the sealed dials (04: hazard
    /// stripes, 05: the red plate, as the Classic plate's LOCKED and OUT OF
    /// SERVICE placards).
    /// </summary>
    public static GateCanvas Face(GateBoardLayout layout, int dials, int periods)
    {
        BoardRect cover = layout.Cover;
        var canvas = new GateCanvas((int)cover.W, (int)cover.H);
        float ox = cover.X, oy = cover.Y;
        BoardRect Shift(BoardRect r) => new BoardRect(r.X - ox, r.Y - oy, r.W, r.H);

        // The cover: slate, lighter at the top, a brass edge and a dark inner line.
        var face = new BoardRect(0f, 0f, cover.W, cover.H);
        canvas.RoundRect(face, 12f, new GateColour(56, 61, 66), Slate.Lerp(SlateDeep, 0.6f));
        canvas.RoundRectLine(face, 12f, 4f, BrassDeep);
        canvas.RoundRectLine(new BoardRect(4f, 4f, cover.W - 8f, cover.H - 8f), 9f, 2f, Ink.WithAlpha(0.55f));

        // The call row.
        for (int d = 0; d < layout.Nations; d++)
        {
            BoardRect plate = Shift(layout.CallPlate(d));
            canvas.RoundRect(plate, 8f, SlateDeep, SlateWell);
            canvas.RoundRectLine(plate, 8f, 2f, BrassDeep);
            BoardPoint e = layout.CallEmblem(d), l = layout.CallLamp(d);
            canvas.Annulus(e.X - ox, e.Y - oy, 0f, 27f, new GateColour(70, 76, 82), new GateColour(50, 55, 60));
            canvas.Annulus(e.X - ox, e.Y - oy, 25f, 28f, Brass, BrassDeep);
            canvas.Annulus(l.X - ox, l.Y - oy, 0f, 13f, BrassLight, BrassDeep);
            canvas.Annulus(l.X - ox, l.Y - oy, 0f, 10f, new GateColour(58, 36, 26), new GateColour(34, 20, 14));
            for (int p = 0; p < periods; p++)
            {
                BoardPoint pip = layout.CallPip(d, p, periods);
                canvas.Annulus(pip.X - ox, pip.Y - oy, 0f, 8f, SlateWell, Ink);
            }
        }

        for (int m = 0; m < dials; m++)
        {
            BoardPoint c = layout.DialCentre(m);
            float k = layout.DialScale(m), cx = c.X - ox, cy = c.Y - oy;
            // A soft shadow, the outer ring (brass), the inner ring's bed and the hub.
            canvas.Annulus(cx + 3f, cy + 5f, 0f, (GateBoardLayout.OuterRing + 6f) * k, Ink.WithAlpha(0.45f), Ink.WithAlpha(0.45f));
            canvas.Annulus(cx, cy, GateBoardLayout.RingOuter * k, GateBoardLayout.OuterRing * k, BrassLight, BrassDeep);
            canvas.Annulus(cx, cy, (GateBoardLayout.OuterRing - 2f) * k, GateBoardLayout.OuterRing * k, Ink, Ink);
            canvas.Annulus(cx, cy, 0f, GateBoardLayout.RingOuter * k, SlateWell, Ink);
            canvas.Annulus(cx, cy, (GateBoardLayout.HubRadius - 3f) * k, (GateBoardLayout.RingInner + 1f) * k, BrassDeep, BrassDeep.Lerp(Ink, 0.5f));
            if (!GateBoardLayout.Big(m))
            {
                bool first = m == GateBoardLayout.BigDials;
                var placard = new BoardRect(cx - 70f, cy - 18f, 140f, 36f);
                canvas.RoundRect(placard, 4f, first ? Hazard : Danger, first ? Hazard.Lerp(BrassDeep, 0.3f) : Danger.Lerp(Ink, 0.3f));
                if (first)
                    canvas.Fill(placard.X, placard.Y, placard.X + placard.W, placard.Y + placard.H, (x, y) =>
                    {
                        float band = ((x - placard.X) + (placard.Y + placard.H - y)) % 28f;
                        float inside = RoundRectDistance(placard, 4f, x, y);
                        return Math.Max(inside, Math.Abs(band - 7f) - 6f);
                    }, t => Ink);
                canvas.RoundRectLine(placard, 4f, 2f, Ink);
                canvas.Annulus(placard.X + 12f, cy, 0f, 4f, Ink, Ink);
                canvas.Annulus(placard.X + placard.W - 12f, cy, 0f, 4f, Ink, Ink);
                continue;
            }
            // The cradle's wells and the brass rail under them.
            for (int s = 0; s < layout.CradleSlots; s++)
            {
                BoardRect slot = Shift(layout.CradleSlot(m, s));
                canvas.RoundRect(slot, 6f, SlateWell, Ink);
                canvas.RoundRectLine(slot, 6f, 2f, new GateColour(84, 86, 88));
            }
            BoardRect first0 = Shift(layout.CradleSlot(m, 0)), last = Shift(layout.CradleSlot(m, layout.CradleSlots - 1));
            canvas.RoundRect(new BoardRect(first0.X - 8f, first0.Y + first0.H + 6f, last.X + last.W - first0.X + 16f, 6f), 3f, Brass, BrassDeep);
            // The display's bezel.
            BoardPoint disp = layout.Display(m);
            canvas.RoundRect(new BoardRect(disp.X - ox - 56f, disp.Y - oy - 17f, 112f, 34f), 6f, BrassDeep, Ink);
        }

        // The legend plates (D54): brass with an ink line inside the edge; the view prints the glyphs and their names on them.
        for (int col = 0; col < GateBoardLayout.LegendColumns; col++)
        {
            BoardRect plate = Shift(layout.LegendPlate(col));
            canvas.RoundRect(plate, 6f, BrassLight.Lerp(Brass, 0.35f), Brass.Lerp(BrassDeep, 0.35f));
            canvas.RoundRectLine(plate, 6f, 2f, Ink.WithAlpha(0.7f));
        }

        // The cell rack.
        BoardRect rack = Shift(layout.Rack);
        canvas.RoundRect(rack, 10f, SlateDeep, SlateWell);
        canvas.RoundRectLine(rack, 10f, 3f, Brass);
        for (int b = 0; b < layout.Bays; b++)
        {
            BoardPoint bay = layout.Bay(b);
            var well = new BoardRect(bay.X - ox - GateBoardLayout.BayW / 2f, bay.Y - oy - GateBoardLayout.BayH / 2f, GateBoardLayout.BayW, GateBoardLayout.BayH);
            canvas.RoundRect(well, 8f, Ink, SlateWell);
            canvas.RoundRectLine(well, 8f, 2f, BrassDeep);
        }
        return canvas;
    }

    // ------------------------------------------------------------------ the live parts

    /// <summary>A big dial's inner ring (the part the hand turns), <paramref name="slots"/> glyph discs on it, slot 0 at the top: a brass band with ink edges, a groove between two slots and a lighter disc under each glyph. Its side is 2 x RingOuter + 4.</summary>
    public static GateCanvas Ring(int slots)
    {
        int size = (int)(2f * GateBoardLayout.RingOuter + 4f);
        var canvas = new GateCanvas(size, size);
        float c = size / 2f;
        canvas.Annulus(c, c, GateBoardLayout.RingInner, GateBoardLayout.RingOuter, BrassLight, BrassDeep);
        canvas.Annulus(c, c, GateBoardLayout.RingOuter - 2f, GateBoardLayout.RingOuter, Ink, Ink);
        canvas.Annulus(c, c, GateBoardLayout.RingInner, GateBoardLayout.RingInner + 2f, Ink, Ink);
        float step = GateDial.SlotDegrees(slots);
        for (int s = 0; s < slots; s++)
        {
            float a = (s + 0.5f) * step * MathF.PI / 180f, sx = MathF.Sin(a), sy = -MathF.Cos(a);
            canvas.Fill(c - GateBoardLayout.RingOuter, c - GateBoardLayout.RingOuter, c + GateBoardLayout.RingOuter, c + GateBoardLayout.RingOuter, (x, y) =>
            {
                float px = x - c, py = y - c, along = px * sx + py * sy, across = MathF.Abs(px * sy - py * sx), r = MathF.Sqrt(px * px + py * py);
                return along < 0f ? 99f : Math.Max(across - 1f, Math.Max(GateBoardLayout.RingInner + 3f - r, r - GateBoardLayout.RingOuter + 3f));
            }, t => Ink.WithAlpha(0.55f));
            float g = s * step * MathF.PI / 180f;
            float gx = c + GateBoardLayout.GlyphRadius * MathF.Sin(g), gy = c - GateBoardLayout.GlyphRadius * MathF.Cos(g);
            canvas.Annulus(gx, gy, 0f, 20f, DiscBrass, DiscBrassDeep);
            canvas.Annulus(gx, gy, 18f, 20f, BrassDeep, BrassDeep);
        }
        return canvas;
    }

    /// <summary>A chevron (40 x 36): a brass wedge pointing at the dial's centre (down in the canvas) round its lens's socket.</summary>
    public static GateCanvas Chevron()
    {
        var canvas = new GateCanvas(40, 36);
        float[] wedge = { 2f, 3f, 38f, 3f, 20f, 34f };
        canvas.Fill(0f, 0f, 40f, 36f, (x, y) => InPolygon(wedge, x, y) ? -1f : 1f, t => BrassLight.Lerp(BrassDeep, t));
        canvas.Fill(0f, 0f, 40f, 36f, (x, y) => InPolygon(new[] { 9f, 8f, 31f, 8f, 20f, 26f }, x, y) ? -1f : 1f, t => Ink);
        return canvas;
    }

    /// <summary>A chevron's lens (18 x 14), white so the view tints it by its state.</summary>
    public static GateCanvas Lens()
    {
        var canvas = new GateCanvas(18, 14);
        canvas.Fill(0f, 0f, 18f, 14f, (x, y) =>
        {
            float dx = (x - 9f) / 8f, dy = (y - 5f) / 5f;
            return (MathF.Sqrt(dx * dx + dy * dy) - 1f) * 5f;
        }, t => new GateColour(255, 255, 255));
        return canvas;
    }

    /// <summary>The locked glyph's disc (44 x 44; D54 amended): a lit cream disc of a ring disc's size with a deep brass rim; the view draws it over the ring and the chevrons at the locked glyph, so the locked choice never hides. Its face is 1.5 times a ring disc's luma.</summary>
    public static GateCanvas LockDisc()
    {
        var canvas = new GateCanvas(44, 44);
        canvas.Annulus(22f, 22f, 0f, 20f, Lit, LitDeep);
        canvas.Annulus(22f, 22f, 18.5f, 20f, BrassDeep, BrassDeep);
        return canvas;
    }

    /// <summary>A power cell (32 x 64): brass caps top and bottom, the glowing core between, a glint down its left.</summary>
    public static GateCanvas Cell()
    {
        var canvas = new GateCanvas((int)GateBoardLayout.CellW, (int)GateBoardLayout.CellH);
        float w = GateBoardLayout.CellW, h = GateBoardLayout.CellH;
        canvas.RoundRect(new BoardRect(1f, 1f, w - 2f, h - 2f), 10f, Ink, Ink);
        canvas.RoundRect(new BoardRect(5f, 10f, w - 10f, h - 20f), 6f, new GateColour(255, 240, 200), CellCore.Lerp(Brass, 0.35f));
        canvas.RoundRect(new BoardRect(3f, 3f, w - 6f, 10f), 4f, BrassLight, BrassDeep);
        canvas.RoundRect(new BoardRect(3f, h - 13f, w - 6f, 10f), 4f, Brass, BrassDeep);
        canvas.RoundRect(new BoardRect(9f, 15f, 4f, h - 30f), 2f, new GateColour(255, 255, 255, 0.7f), new GateColour(255, 255, 255, 0.2f));
        return canvas;
    }

    /// <summary>The gate's horizon (a disc of <paramref name="radius"/>): white, brightest at its rim and soft at its heart, so the view tints it.</summary>
    public static GateCanvas Horizon(int radius)
    {
        int size = 2 * radius + 2;
        var canvas = new GateCanvas(size, size);
        float c = size / 2f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float r = MathF.Sqrt((x + 0.5f - c) * (x + 0.5f - c) + (y + 0.5f - c) * (y + 0.5f - c)) / radius;
                if (r > 1f + 1f / radius)
                    continue;
                float edge = Math.Max(0f, Math.Min(1f, (1f - r) * radius + 0.5f));
                float glow = 0.45f + 0.55f * r * r + 0.08f * MathF.Cos(r * 18f);
                canvas.Blend(x, y, new GateColour(255, 255, 255, Math.Min(1f, glow)), edge);
            }
        return canvas;
    }

    /// <summary>A glyph as white ink on clear (<paramref name="size"/> pixels): era <paramref name="era"/>'s, or -1 the point of origin (EraInked); 3 x 3 samples a pixel.</summary>
    public static GateCanvas Glyph(int era, int size)
    {
        var canvas = new GateCanvas(size, size);
        for (int py = 0; py < size; py++)
            for (int px = 0; px < size; px++)
            {
                int inked = 0;
                for (int sy = 0; sy < 3; sy++)
                    for (int sx = 0; sx < 3; sx++)
                        if (EraInked(era, (px + (sx + 0.5f) / 3f) / size * 2f - 1f, (py + (sy + 0.5f) / 3f) / size * 2f - 1f))
                            inked++;
                if (inked > 0)
                    canvas.Blend(px, py, new GateColour(255, 255, 255), inked / 9f);
            }
        return canvas;
    }

    /// <summary>A white disc of <paramref name="radius"/> (a closed dial's veil, a lamp's well), antialiased.</summary>
    public static GateCanvas Disc(int radius)
    {
        var canvas = new GateCanvas(2 * radius + 2, 2 * radius + 2);
        canvas.Annulus(radius + 1f, radius + 1f, 0f, radius, new GateColour(255, 255, 255), new GateColour(255, 255, 255));
        return canvas;
    }

    /// <summary>Lays <paramref name="from"/> over <paramref name="into"/> with its top-left corner at (<paramref name="left"/>, <paramref name="top"/>) (the board on the wall: the rings stamped on its face).</summary>
    public static void Stamp(GateCanvas into, GateCanvas from, int left, int top)
    {
        for (int y = 0; y < from.Height; y++)
            for (int x = 0; x < from.Width; x++)
            {
                (byte r, byte g, byte b, byte a) c = from.At(x, y);
                if (c.a > 0)
                    into.Blend(left + x, top + y, new GateColour(c.r, c.g, c.b), c.a / 255f);
            }
    }

    /// <summary>The era mark (a brass arrow pointing down, 18 x 12).</summary>
    public static GateCanvas MarkArrow()
    {
        var canvas = new GateCanvas(18, 12);
        canvas.Fill(0f, 0f, 18f, 12f, (x, y) => InPolygon(new[] { 1f, 1f, 17f, 1f, 9f, 11f }, x, y) ? -1f : 1f, t => BrassLight.Lerp(Brass, t));
        return canvas;
    }
}
