using System;

/// <summary>
/// How one drawn piece of the cyberpunk PC is painted (PcChromeArt.Pixel):
/// its fill (top and bottom of a soft vertical gradient), its edge, the lit
/// top edge, the glow round it in its sprite's pad, and its geometry in sprite
/// pixels (the cel kit's 2x: a 2-pixel edge is one design pixel). Not serialized.
/// </summary>
public readonly struct PcPaint
{
    /// <summary>The fill at the piece's top.</summary>
    public readonly Rgba Fill;

    /// <summary>The fill at the piece's bottom (the bevel's shade).</summary>
    public readonly Rgba FillLow;

    /// <summary>The edge round the piece (its sides and bottom; a field's underline).</summary>
    public readonly Rgba Edge;

    /// <summary>The lit top edge (D2: "a one-pixel lit top edge").</summary>
    public readonly Rgba Lit;

    /// <summary>The glow round the piece, in its pad (alpha 0: none).</summary>
    public readonly Rgba Halo;

    /// <summary>The empty room round the piece in its sprite (the cel kit's pad, 28 px).</summary>
    public readonly float Pad;

    /// <summary>The corner radius.</summary>
    public readonly float Radius;

    /// <summary>The edge's width.</summary>
    public readonly float EdgeWidth;

    /// <summary>The glow's reach outside the piece (at most the pad).</summary>
    public readonly float HaloWidth;

    /// <summary>True for a field: the edge is strong only along the bottom (an underline), faint elsewhere.</summary>
    public readonly bool Underline;

    /// <summary>A paint.</summary>
    public PcPaint(Rgba fill, Rgba fillLow, Rgba edge, Rgba lit, Rgba halo, float pad, float radius, float edgeWidth, float haloWidth, bool underline = false)
    {
        Fill = fill;
        FillLow = fillLow;
        Edge = edge;
        Lit = lit;
        Halo = halo;
        Pad = pad;
        Radius = radius;
        EdgeWidth = edgeWidth;
        HaloWidth = haloWidth;
        Underline = underline;
    }
}

/// <summary>
/// The cyberpunk PC's chrome drawn in code (D2, the night of 2026-11-07): one
/// pixel of a piece (a panel, a plate, a title bar, the deck, a field, a row,
/// a badge) from its paint, one pixel of the monitor's bezel round its glass,
/// and the glass overlay's scan rows and vignette. Every colour comes from the
/// theme's tokens, so a token change redraws the PC. Rows count from the
/// bottom (a texture's row 0). Pure, so it is tested headless; PcKit and
/// PcSkin turn the pixels into sprites.
/// </summary>
public static class PcChromeArt
{
    /// <summary>
    /// The pixel (<paramref name="x"/>, <paramref name="y"/>) of a piece
    /// <paramref name="w"/> x <paramref name="h"/> pixels painted with
    /// <paramref name="p"/>: clear in the pad but for the glow; inside, the
    /// fill's gradient, the edge, and the lit top edge along the straight
    /// part of the top side. The outline is anti-aliased over one pixel.
    /// </summary>
    public static Rgba Pixel(in PcPaint p, int x, int y, int w, int h)
    {
        float px = x + 0.5f, py = y + 0.5f;
        float d = RoundedBox(px, py, p.Pad, p.Pad, w - p.Pad, h - p.Pad, p.Radius);
        Rgba halo = p.HaloWidth > 0f && d > 0f && d < p.HaloWidth
            ? p.Halo.WithAlpha(p.Halo.A * Sq(1f - d / p.HaloWidth))
            : new Rgba(0f, 0f, 0f, 0f);
        float cover = Clamp01(0.5f - d);
        if (cover <= 0f)
            return halo;

        float inner = -d;
        float t = Clamp01((py - p.Pad) / Math.Max(1f, h - 2f * p.Pad));
        Rgba body = Lerp(p.FillLow, p.Fill, t);
        bool top = h - p.Pad - py < p.EdgeWidth && px > p.Pad + p.Radius && px < w - p.Pad - p.Radius;
        float edge = p.Underline ? (py - p.Pad < p.EdgeWidth ? 1f : inner < p.EdgeWidth ? 0.35f : 0f) : inner < p.EdgeWidth ? 1f : 0f;
        Rgba ink = top && !p.Underline ? p.Lit : p.Edge;
        Rgba shape = edge > 0f ? Over(body, ink.WithAlpha(ink.A * edge)) : body;
        return Over(halo, shape.WithAlpha(shape.A * cover));
    }

    /// <summary>
    /// The pixel (<paramref name="x"/>, <paramref name="y"/>) of the monitor's
    /// bezel, <paramref name="w"/> x <paramref name="h"/> pixels with corner
    /// radius <paramref name="radius"/>, round the glass's hole (pixels from the
    /// bottom left; clear inside): the casing's gradient from
    /// <paramref name="body"/> (top) to <paramref name="bodyLow"/>, a thin
    /// <paramref name="lit"/> line along its outer edge (D2: "a chunky dark
    /// bezel with a thin lit edge"), and a dark gasket <paramref name="gasket"/>
    /// round the glass with a faint lit rim at the glass.
    /// </summary>
    public static Rgba Bezel(int x, int y, int w, int h, float radius, float holeX0, float holeY0, float holeX1, float holeY1,
                             Rgba body, Rgba bodyLow, Rgba lit, Rgba gasket, float gasketWidth)
    {
        float px = x + 0.5f, py = y + 0.5f;
        float d = RoundedBox(px, py, 0f, 0f, w, h, radius);
        float cover = Clamp01(0.5f - d);
        if (cover <= 0f)
            return new Rgba(0f, 0f, 0f, 0f);
        if (px > holeX0 && px < holeX1 && py > holeY0 && py < holeY1)
            return new Rgba(0f, 0f, 0f, 0f);

        Rgba casing = Lerp(bodyLow, body, Clamp01(py / h));
        float fromEdge = -d;
        if (fromEdge < 1.5f)
            casing = Over(casing, lit);
        else if (fromEdge < 4f)
            casing = Over(casing, lit.WithAlpha(lit.A * 0.18f));

        float dx = Math.Max(Math.Max(holeX0 - px, 0f), px - holeX1), dy = Math.Max(Math.Max(holeY0 - py, 0f), py - holeY1);
        float fromHole = (float)Math.Sqrt(dx * dx + dy * dy);
        if (fromHole < 1.5f)
            casing = Over(gasket, lit.WithAlpha(lit.A * 0.55f));
        else if (fromHole < gasketWidth)
            casing = Lerp(gasket, casing, Sq((fromHole - 1.5f) / Math.Max(1f, gasketWidth - 1.5f)) * 0.35f);
        return casing.WithAlpha(casing.A * cover);
    }

    /// <summary>The scan rows' darkness on row <paramref name="row"/>: <paramref name="strength"/> on one row in every <paramref name="pitch"/>, clear on the others.</summary>
    public static float ScanRow(int row, int pitch, float strength) => pitch > 1 && Mod(row, pitch) == 0 ? Clamp01(strength) : 0f;

    /// <summary>The glass's vignette at (<paramref name="u"/>, <paramref name="v"/>) of the glass (0..1 each): clear in the middle, <paramref name="strength"/> dark in the corners.</summary>
    public static float Vignette(float u, float v, float strength)
    {
        float dx = (u - 0.5f) * 2f, dy = (v - 0.5f) * 2f;
        return Clamp01(strength) * Clamp01((dx * dx + dy * dy - 0.45f) / 1.55f);
    }

    /// <summary>The glass's bloom at (<paramref name="u"/>, <paramref name="v"/>): a soft phosphor glow that rises towards the glass's edges.</summary>
    public static float Bloom(float u, float v, float strength)
    {
        float edge = Math.Min(Math.Min(u, 1f - u), Math.Min(v, 1f - v));
        return Clamp01(strength) * Sq(Clamp01(1f - edge / 0.12f));
    }

    /// <summary>The signed distance from (px, py) to a rounded box (negative inside).</summary>
    private static float RoundedBox(float px, float py, float x0, float y0, float x1, float y1, float r)
    {
        float cx = (x0 + x1) / 2f, cy = (y0 + y1) / 2f;
        float hx = Math.Max(0f, (x1 - x0) / 2f), hy = Math.Max(0f, (y1 - y0) / 2f);
        r = Math.Min(r, Math.Min(hx, hy));
        float qx = Math.Abs(px - cx) - (hx - r), qy = Math.Abs(py - cy) - (hy - r);
        float outside = (float)Math.Sqrt(Sq(Math.Max(qx, 0f)) + Sq(Math.Max(qy, 0f)));
        return outside + Math.Min(Math.Max(qx, qy), 0f) - r;
    }

    /// <summary><paramref name="top"/> over <paramref name="under"/> (straight alpha).</summary>
    public static Rgba Over(Rgba under, Rgba top)
    {
        float a = top.A + under.A * (1f - top.A);
        if (a <= 0f)
            return new Rgba(0f, 0f, 0f, 0f);
        float k = under.A * (1f - top.A);
        return new Rgba((top.R * top.A + under.R * k) / a, (top.G * top.A + under.G * k) / a, (top.B * top.A + under.B * k) / a, a);
    }

    /// <summary>From <paramref name="a"/> to <paramref name="b"/> by <paramref name="t"/>, every channel.</summary>
    public static Rgba Lerp(Rgba a, Rgba b, float t) =>
        new Rgba(a.R + (b.R - a.R) * t, a.G + (b.G - a.G) * t, a.B + (b.B - a.B) * t, a.A + (b.A - a.A) * t);

    private static float Sq(float v) => v * v;

    private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;

    private static int Mod(int a, int m) => (a % m + m) % m;
}
