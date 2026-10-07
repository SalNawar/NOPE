using System;

/// <summary>
/// The daters' impression face (the desk machine spec §1; Saleh's reference,
/// the S-401's "RECEIVED"): a bold slab-serif display capital whose outline
/// alone prints (a hollow, outlined word), drawn as signed distances so the
/// painter (DaterImpressionArt) can stroke its contour at any size. The
/// letters of APPROVED and DENIED (A, D, E, I, N, O, P, R, V) are drawn;
/// any other letter falls back to the font (Has is false). Units are the
/// letter's cap height: x from its left edge (0 to Width), y up from the
/// baseline (0 to 1); a distance is negative inside the letter. Pure.
/// </summary>
public static class DaterLetters
{
    /// <summary>A stem's width, a slab serif's height and how far a serif reaches past its stem.</summary>
    private const float Stem = 0.21f, Slab = 0.1f, Reach = 0.075f;

    /// <summary>The letters drawn here.</summary>
    private const string Drawn = "ADEINOPRV";

    /// <summary>True when <paramref name="c"/> (upper case) is drawn here.</summary>
    public static bool Has(char c) => Drawn.IndexOf(char.ToUpperInvariant(c)) >= 0;

    /// <summary>The advance width of <paramref name="c"/> in cap heights (0 for a letter not drawn here).</summary>
    public static float Width(char c)
    {
        switch (char.ToUpperInvariant(c))
        {
            case 'A': return 0.86f;
            case 'D': return 0.8f;
            case 'E': return 0.72f;
            case 'I': return 0.44f;
            case 'N': return 0.86f;
            case 'O': return 0.82f;
            case 'P': return 0.74f;
            case 'R': return 0.8f;
            case 'V': return 0.86f;
            default: return 0f;
        }
    }

    /// <summary>The signed distance (cap heights; negative inside) from (<paramref name="x"/>, <paramref name="y"/>) to the letter <paramref name="c"/>'s edge; a large positive value for a letter not drawn here.</summary>
    public static float Distance(char c, float x, float y)
    {
        switch (char.ToUpperInvariant(c))
        {
            case 'A': return A(x, y);
            case 'D': return D(x, y);
            case 'E': return E(x, y);
            case 'I': return I(x, y);
            case 'N': return N(x, y);
            case 'O': return O(x, y);
            case 'P': return P(x, y);
            case 'R': return R(x, y);
            case 'V': return V(x, y);
            default: return 1e3f;
        }
    }

    // The letters (cap height 1; the left serifs start at x 0).

    private static float I(float x, float y)
    {
        const float sx = 0.22f;
        return Min(StemAt(x, y, sx), Serif(x, y, sx, 0.11f, true), Serif(x, y, sx, 0.11f, false));
    }

    private static float E(float x, float y)
    {
        const float sx = Reach + Stem / 2f, right = 0.7f, arm = 0.16f;
        float left = sx - Stem / 2f;
        float d = StemAt(x, y, sx);
        d = Min(d, Rect(x, y, left, 1f - arm, right, 1f), Rect(x, y, left, 0f, right + 0.02f, arm));
        d = Min(d, Rect(x, y, left, 0.5f - 0.065f, right - 0.12f, 0.5f + 0.065f));
        d = Min(d, Rect(x, y, right - 0.075f, 1f - 0.3f, right, 1f), Rect(x, y, right - 0.055f, 0f, right + 0.02f, 0.3f)); // the arms' beaks
        return Min(d, Rect(x, y, 0f, 1f - Slab, left, 1f), Rect(x, y, 0f, 0f, left, Slab));
    }

    private static float D(float x, float y)
    {
        const float sx = Reach + Stem / 2f, right = 0.78f;
        float left = sx - Stem / 2f;
        float outer = Min(RoundRect(x, y, sx, 0f, right, 1f, 0.42f), Rect(x, y, sx, 0f, sx + 0.3f, 1f));
        float inner = Min(RoundRect(x, y, left + Stem, 0.15f, right - 0.21f, 0.85f, 0.26f), Rect(x, y, left + Stem, 0.15f, left + Stem + 0.12f, 0.85f));
        float d = Min(Cut(outer, inner), StemAt(x, y, sx));
        return Min(d, Rect(x, y, 0f, 1f - Slab, left, 1f), Rect(x, y, 0f, 0f, left, Slab));
    }

    private static float P(float x, float y) => Min(PStem(x, y), Rect(x, y, 0f, 0f, Reach + Stem + Reach + 0.03f, Slab));

    private static float R(float x, float y)
    {
        float d = Min(PStem(x, y), Rect(x, y, 0f, 0f, Reach + Stem + Reach, Slab));
        d = Min(d, Clip(Segment(x, y, 0.44f, 0.44f, 0.67f, 0f, 0.1f), y)); // the leg
        return Min(d, Rect(x, y, 0.54f, 0f, 0.8f, Slab)); // its foot
    }

    /// <summary>P's (and R's) stem, bowl and top-left serif.</summary>
    private static float PStem(float x, float y)
    {
        const float sx = Reach + Stem / 2f, right = 0.72f, bottom = 0.4f;
        float left = sx - Stem / 2f;
        float r = (1f - bottom) / 2f;
        float outer = Min(RoundRect(x, y, sx, bottom, right, 1f, r), Rect(x, y, sx, bottom, sx + 0.2f, 1f));
        float inner = Min(RoundRect(x, y, left + Stem, bottom + 0.14f, right - 0.2f, 1f - 0.15f, r - 0.16f),
                          Rect(x, y, left + Stem, bottom + 0.14f, left + Stem + 0.1f, 1f - 0.15f));
        float d = Min(Cut(outer, inner), StemAt(x, y, sx));
        return Min(d, Rect(x, y, 0f, 1f - Slab, left, 1f));
    }

    private static float O(float x, float y)
    {
        float outer = RoundRect(x, y, 0.02f, 0f, 0.8f, 1f, 0.39f);
        float inner = RoundRect(x, y, 0.02f + 0.22f, 0.15f, 0.8f - 0.22f, 0.85f, 0.17f);
        return Cut(outer, inner);
    }

    private static float A(float x, float y)
    {
        // A thin left stroke and a thick right stroke meeting under a flat apex, a crossbar and slab feet.
        float d = Min(Clip(Segment(x, y, 0.1f, 0f, 0.39f, 1f, 0.07f), y), Clip(Segment(x, y, 0.76f, 0f, 0.47f, 1f, Stem / 2f), y));
        d = Min(d, Rect(x, y, 0.33f, 0.9f, 0.53f, 1f), Rect(x, y, 0.22f, 0.27f, 0.66f, 0.38f));
        return Min(d, Rect(x, y, 0f, 0f, 0.26f, Slab), Rect(x, y, 0.58f, 0f, 0.86f, Slab));
    }

    private static float V(float x, float y)
    {
        // A thick left stroke and a thin right stroke meeting at a flat foot, with slab serifs on top.
        float d = Min(Clip(Segment(x, y, 0.15f, 1f, 0.41f, 0f, Stem / 2f), y), Clip(Segment(x, y, 0.73f, 1f, 0.47f, 0f, 0.07f), y));
        d = Min(d, Rect(x, y, 0.35f, 0f, 0.53f, 0.08f));
        return Min(d, Rect(x, y, 0f, 1f - Slab, 0.32f, 1f), Rect(x, y, 0.6f, 1f - Slab, 0.86f, 1f));
    }

    private static float N(float x, float y)
    {
        const float thin = 0.12f, l = 0.16f, r = 0.7f;
        float d = Min(Rect(x, y, l - thin / 2f, 0f, l + thin / 2f, 1f), Rect(x, y, r - thin / 2f, 0f, r + thin / 2f, 1f));
        d = Min(d, Clip(Segment(x, y, l + 0.02f, 1f, r - 0.02f, 0f, Stem / 2f), y));
        d = Min(d, Rect(x, y, 0f, 1f - Slab, l + 0.1f, 1f), Rect(x, y, 0f, 0f, l + thin / 2f + Reach, Slab));
        return Min(d, Rect(x, y, r - thin / 2f - Reach, 1f - Slab, 0.86f, 1f));
    }

    // The shapes.

    /// <summary>A full-height stem centred at <paramref name="sx"/>.</summary>
    private static float StemAt(float x, float y, float sx) => Rect(x, y, sx - Stem / 2f, 0f, sx + Stem / 2f, 1f);

    /// <summary>A slab serif across a stem at <paramref name="sx"/>, reaching <paramref name="reach"/> past it each side, at the top or the foot.</summary>
    private static float Serif(float x, float y, float sx, float reach, bool top) =>
        top ? Rect(x, y, sx - Stem / 2f - reach, 1f - Slab, sx + Stem / 2f + reach, 1f) : Rect(x, y, sx - Stem / 2f - reach, 0f, sx + Stem / 2f + reach, Slab);

    /// <summary>The axis-aligned box from (x0, y0) to (x1, y1).</summary>
    private static float Rect(float x, float y, float x0, float y0, float x1, float y1) =>
        RoundRect(x, y, x0, y0, x1, y1, 0f);

    /// <summary>The box from (x0, y0) to (x1, y1) with its corners rounded by <paramref name="radius"/>.</summary>
    private static float RoundRect(float x, float y, float x0, float y0, float x1, float y1, float radius)
    {
        float hx = (x1 - x0) / 2f, hy = (y1 - y0) / 2f;
        float r = MathF.Min(radius, MathF.Min(hx, hy));
        float qx = MathF.Abs(x - (x0 + x1) / 2f) - hx + r, qy = MathF.Abs(y - (y0 + y1) / 2f) - hy + r;
        float outside = MathF.Sqrt(MathF.Max(qx, 0f) * MathF.Max(qx, 0f) + MathF.Max(qy, 0f) * MathF.Max(qy, 0f));
        return outside + MathF.Min(MathF.Max(qx, qy), 0f) - r;
    }

    /// <summary>A stroke of half width <paramref name="half"/> from (ax, ay) to (bx, by), its ends square (cut by Clip at the cap height and the baseline).</summary>
    private static float Segment(float x, float y, float ax, float ay, float bx, float by, float half)
    {
        float dx = bx - ax, dy = by - ay;
        float len = MathF.Sqrt(dx * dx + dy * dy);
        float ux = dx / len, uy = dy / len;
        float px = x - ax, py = y - ay;
        float along = px * ux + py * uy, across = MathF.Abs(-px * uy + py * ux);
        float qx = MathF.Abs(along - len / 2f) - len / 2f - half, qy = across - half;
        float outside = MathF.Sqrt(MathF.Max(qx, 0f) * MathF.Max(qx, 0f) + MathF.Max(qy, 0f) * MathF.Max(qy, 0f));
        return outside + MathF.Min(MathF.Max(qx, qy), 0f);
    }

    /// <summary>A shape cut to the band between the baseline and the cap height.</summary>
    private static float Clip(float d, float y) => MathF.Max(d, MathF.Abs(y - 0.5f) - 0.5f);

    /// <summary><paramref name="shape"/> with <paramref name="hole"/> cut out of it.</summary>
    private static float Cut(float shape, float hole) => MathF.Max(shape, -hole);

    private static float Min(float a, float b) => MathF.Min(a, b);

    private static float Min(float a, float b, float c) => MathF.Min(a, MathF.Min(b, c));
}
