using System;
using System.Collections.Generic;

/// <summary>A rectangle on the screen in pixels (x right, y up from the bottom left corner); one with no width or height is empty.</summary>
public readonly struct ScreenRect
{
    /// <summary>A rectangle from its edges (a max below its min makes it empty).</summary>
    public ScreenRect(float xMin, float yMin, float xMax, float yMax)
    {
        XMin = xMin;
        YMin = yMin;
        XMax = xMax;
        YMax = yMax;
    }

    /// <summary>The left edge.</summary>
    public float XMin { get; }

    /// <summary>The bottom edge.</summary>
    public float YMin { get; }

    /// <summary>The right edge.</summary>
    public float XMax { get; }

    /// <summary>The top edge.</summary>
    public float YMax { get; }

    /// <summary>The width (never negative).</summary>
    public float Width => Math.Max(0f, XMax - XMin);

    /// <summary>The height (never negative).</summary>
    public float Height => Math.Max(0f, YMax - YMin);

    /// <summary>The area (0 for an empty rectangle).</summary>
    public float Area => Width * Height;

    /// <summary>True when the rectangle has no area.</summary>
    public bool IsEmpty => Width <= 0f || Height <= 0f;

    /// <summary>The overlap of two rectangles (empty when they do not overlap).</summary>
    public ScreenRect Intersect(ScreenRect other) =>
        new ScreenRect(Math.Max(XMin, other.XMin), Math.Max(YMin, other.YMin), Math.Min(XMax, other.XMax), Math.Min(YMax, other.YMax));

    /// <summary>The smallest rectangle holding both (an empty one adds nothing).</summary>
    public static ScreenRect Enclosing(ScreenRect a, ScreenRect b)
    {
        if (a.IsEmpty)
            return b;
        if (b.IsEmpty)
            return a;
        return new ScreenRect(Math.Min(a.XMin, b.XMin), Math.Min(a.YMin, b.YMin), Math.Max(a.XMax, b.XMax), Math.Max(a.YMax, b.YMax));
    }
}

/// <summary>Where a handed-over paper lands (PaperLanding.Choose): the spot, and whether the paper held longest goes back first to make room.</summary>
public readonly struct LandingChoice
{
    /// <summary>A choice.</summary>
    public LandingChoice(int spot, bool putBackHeldLongest)
    {
        Spot = spot;
        PutBackHeldLongest = putBackHeldLongest;
    }

    /// <summary>The index of the chosen spot, or -1 with no spots.</summary>
    public int Spot { get; }

    /// <summary>True when the paper held longest goes back to where it lay before the paper lands.</summary>
    public bool PutBackHeldLongest { get; }
}

/// <summary>
/// Where a paper handed over while papers are held lands, so the player sees
/// it (piece 10 Q7, Saleh: "third paper lands visibly"): every candidate spot
/// is the lying paper's rectangle on the screen, tested against what hides the
/// desk there: each held paper's places (its office slot, raised and dipped
/// under the wheel), the office case HUD's strips, the speech bubble and the
/// wheel's ring. Pure, so the rule is tested headless; DeskController projects
/// the spots and the covers.
/// </summary>
public static class PaperLanding
{
    /// <summary>The share of a spot that must show for it to count as whole (edges within a hair).</summary>
    private const float Whole = 0.999f;

    /// <summary>
    /// The share of <paramref name="paper"/> that shows: its part inside
    /// <paramref name="screen"/> that none of <paramref name="covers"/> hides,
    /// over its whole area (0 for an empty paper).
    /// </summary>
    public static float VisibleShare(ScreenRect paper, IReadOnlyList<ScreenRect> covers, ScreenRect screen)
    {
        if (paper.IsEmpty)
            return 0f;

        ScreenRect inside = paper.Intersect(screen);
        if (inside.IsEmpty)
            return 0f;

        // The covered area is a union of rectangles: split the paper along every
        // cover edge and add the cells no cover holds.
        var xs = new List<float> { inside.XMin, inside.XMax };
        var ys = new List<float> { inside.YMin, inside.YMax };
        var clipped = new List<ScreenRect>();
        foreach (ScreenRect cover in covers ?? Array.Empty<ScreenRect>())
        {
            ScreenRect c = cover.Intersect(inside);
            if (c.IsEmpty)
                continue;
            clipped.Add(c);
            xs.Add(c.XMin);
            xs.Add(c.XMax);
            ys.Add(c.YMin);
            ys.Add(c.YMax);
        }
        xs.Sort();
        ys.Sort();

        float shown = 0f;
        for (int i = 0; i + 1 < xs.Count; i++)
        {
            float w = xs[i + 1] - xs[i];
            if (w <= 0f)
                continue;
            float cx = (xs[i] + xs[i + 1]) / 2f;
            for (int j = 0; j + 1 < ys.Count; j++)
            {
                float h = ys[j + 1] - ys[j];
                if (h <= 0f)
                    continue;
                float cy = (ys[j] + ys[j + 1]) / 2f;
                bool hidden = false;
                foreach (ScreenRect c in clipped)
                    if (cx > c.XMin && cx < c.XMax && cy > c.YMin && cy < c.YMax)
                    {
                        hidden = true;
                        break;
                    }
                if (!hidden)
                    shown += w * h;
            }
        }
        return shown / paper.Area;
    }

    /// <summary>
    /// Chooses the spot a handed-over paper lands on, from
    /// <paramref name="spots"/> in the order given (the spawn slots from the
    /// next in turn, then the fallback spots): the first that shows whole
    /// under <paramref name="covers"/> (what hides the desk whatever happens:
    /// the other held papers' places, the case HUD's strips, the bubble, the
    /// wheel) and <paramref name="heldLongest"/> (the places of the paper held
    /// longest; empty when no paper is held). When none shows whole, the paper
    /// held longest goes back to where it lay, as a third paper picked up
    /// sends it back, and the first spot that shows whole under the covers and
    /// <paramref name="heldLongestRest"/> (where it will lie) is taken. When
    /// none shows whole either way, the spot that shows most (the earliest on
    /// a tie), putting the paper back only when that shows more.
    /// </summary>
    public static LandingChoice Choose(IReadOnlyList<ScreenRect> spots, IReadOnlyList<ScreenRect> covers, ScreenRect heldLongest, ScreenRect heldLongestRest, ScreenRect screen)
    {
        if (spots == null || spots.Count == 0)
            return new LandingChoice(-1, false);

        var keep = new List<ScreenRect>(covers ?? Array.Empty<ScreenRect>());
        var putBack = new List<ScreenRect>(keep);
        keep.Add(heldLongest);
        putBack.Add(heldLongestRest);
        bool canPutBack = !heldLongest.IsEmpty;

        int bestKeep = Best(spots, keep, screen, out float keepShare);
        if (keepShare >= Whole)
            return new LandingChoice(bestKeep, false);
        if (!canPutBack)
            return new LandingChoice(bestKeep, false);

        int bestPut = Best(spots, putBack, screen, out float putShare);
        return putShare > keepShare + 0.001f ? new LandingChoice(bestPut, true) : new LandingChoice(bestKeep, false);
    }

    /// <summary>The first spot that shows whole, else the one that shows most (the earliest on a tie), and its share.</summary>
    private static int Best(IReadOnlyList<ScreenRect> spots, IReadOnlyList<ScreenRect> covers, ScreenRect screen, out float share)
    {
        int best = 0;
        share = -1f;
        for (int i = 0; i < spots.Count; i++)
        {
            float s = VisibleShare(spots[i], covers, screen);
            if (s >= Whole)
            {
                share = s;
                return i;
            }
            if (s > share + 0.0001f)
            {
                share = s;
                best = i;
            }
        }
        return best;
    }

    /// <summary>
    /// The fallback spots over the desk's landing area, as (u, v) from 0 to 1
    /// across it: a grid of <paramref name="columns"/> by <paramref name="rows"/>
    /// cell centres, nearest the area's centre first (then from the near edge
    /// up, left to right). None for a grid with no cells.
    /// </summary>
    public static IReadOnlyList<(float u, float v)> GridSpots(int columns, int rows)
    {
        var spots = new List<(float u, float v)>();
        if (columns <= 0 || rows <= 0)
            return spots;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < columns; c++)
                spots.Add(((c + 0.5f) / columns, (r + 0.5f) / rows));

        // A stable sort by distance from the centre (List.Sort is not stable).
        var order = new List<int>();
        for (int i = 0; i < spots.Count; i++)
            order.Add(i);
        order.Sort((a, b) =>
        {
            float da = Dist(spots[a]), db = Dist(spots[b]);
            return Math.Abs(da - db) > 1e-6f ? da.CompareTo(db) : a.CompareTo(b);
        });
        var sorted = new List<(float u, float v)>();
        foreach (int i in order)
            sorted.Add(spots[i]);
        return sorted;
    }

    /// <summary>The squared distance of a grid spot from the area's centre.</summary>
    private static float Dist((float u, float v) p) => (p.u - 0.5f) * (p.u - 0.5f) + (p.v - 0.5f) * (p.v - 0.5f);
}
