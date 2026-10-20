using System;
using System.Collections.Generic;

/// <summary>
/// Where a thing let go on the desk comes to rest (DOC-HANDLE, BUG-6, BUG-8;
/// Saleh's note 25: "it should stay where I drop it"): Papers, Please's
/// honesty. Every rectangle is in one desk frame (DeskController's: x to the
/// right, y away from the office camera, metres from the desk's centre).
/// <list type="bullet">
/// <item>It rests exactly where it was released (its footprint at its final
/// size), unless that leaves part of it outside <c>area</c> (the desk the
/// reading view shows: the whole rect stays in view) or over something in
/// <c>keepOut</c> (a prop's footprint, the scanner's hidden shadow, the
/// counter strip for a desk paper refused there).</item>
/// <item>Then it moves the least it can: one joint solve over every keep-out
/// at once (never one push at a time, which used to land a paper in an
/// earlier blocker), to a place clear of all of them and inside the area;
/// where no place is clear, the one that overlaps least.</item>
/// <item>A refusal (a machine that does not take it, a desk paper on the
/// counter before the verdict) is the same rest with the refusing area added
/// to the keep-outs: it shakes in place and lies just beside it, never flying
/// back to where it was picked up.</item>
/// <item>The dock a drop goes to is judged by the footprint, not the pointer
/// (<see cref="DockOf"/>: the dock under the footprint's centre).</item>
/// </list>
/// Pure, so every case is tested headless; DeskController applies it.
/// </summary>
public static class DropResolver
{
    /// <summary>The gap left between a moved thing and the keep-out it was moved off (metres).</summary>
    public const float Gap = 0.005f;

    /// <summary>
    /// Where <paramref name="paper"/> (its footprint where it was released, at
    /// its final size) rests: its own centre when it lies wholly inside
    /// <paramref name="area"/> and clear of every <paramref name="keepOut"/>;
    /// else the nearest place (to its release) that is; else, where nothing is
    /// clear, the place inside the area that overlaps the keep-outs least
    /// (nearest among equals). <paramref name="keepOut"/> may be null.
    /// </summary>
    public static (float x, float y) Rest(DeskRect paper, DeskRect area, IReadOnlyList<DeskRect> keepOut)
    {
        (float x, float y) inside = Inside(paper, area);
        float w = paper.Width, h = paper.Height;
        if (Overlap(new DeskRect(inside.x, inside.y, w, h), keepOut) <= 0f)
            return inside;

        float hw = w / 2f, hh = h / 2f;
        var xs = new List<float> { inside.x };
        var ys = new List<float> { inside.y };
        if (keepOut != null)
            foreach (DeskRect r in keepOut)
            {
                xs.Add(r.CentreX - r.Width / 2f - Gap - hw);
                xs.Add(r.CentreX + r.Width / 2f + Gap + hw);
                ys.Add(r.CentreY - r.Height / 2f - Gap - hh);
                ys.Add(r.CentreY + r.Height / 2f + Gap + hh);
            }

        bool found = false;
        (float x, float y) best = inside;
        float bestOverlap = float.MaxValue, bestMove = float.MaxValue;
        foreach (float cx in xs)
            foreach (float cy in ys)
            {
                (float x, float y) c = Inside(new DeskRect(cx, cy, w, h), area);
                float overlap = Overlap(new DeskRect(c.x, c.y, w, h), keepOut);
                float move = Distance(c.x - paper.CentreX, c.y - paper.CentreY);
                bool better = overlap < bestOverlap - 1e-9f || (Math.Abs(overlap - bestOverlap) <= 1e-9f && move < bestMove - 1e-6f);
                if (!found || better)
                {
                    found = true;
                    best = c;
                    bestOverlap = overlap;
                    bestMove = move;
                }
            }
        return best;
    }

    /// <summary>
    /// Where <paramref name="paper"/>'s centre goes so the whole rect lies
    /// inside <paramref name="area"/>: its own centre when it already does;
    /// each axis shifted the least it can; a paper wider (or deeper) than the
    /// area is centred on it on that axis.
    /// </summary>
    public static (float x, float y) Inside(DeskRect paper, DeskRect area) =>
        (Axis(paper.CentreX, paper.Width, area.CentreX, area.Width), Axis(paper.CentreY, paper.Height, area.CentreY, area.Height));

    /// <summary>The total area <paramref name="at"/> shares with the keep-outs (square metres; 0 when clear or with none).</summary>
    public static float Overlap(DeskRect at, IReadOnlyList<DeskRect> keepOut)
    {
        float total = 0f;
        if (keepOut != null)
            foreach (DeskRect r in keepOut)
                total += Shared(at, r);
        return total;
    }

    /// <summary>
    /// The dock a thing dropped with footprint <paramref name="paper"/> goes
    /// to (DeskController.DockAt's rule, for a paper and the folder alike):
    /// the index of the first of <paramref name="docks"/> (in priority order:
    /// the desk lists the chip reader first, it stands clear of the scanner)
    /// whose drop area holds the footprint's centre (<paramref name="holds"/>:
    /// the dock, then the centre's x and y in the footprint's frame), or -1.
    /// The footprint, not the pointer, decides (a paper grabbed by its edge
    /// hangs away from the pointer: BUG-6).
    /// </summary>
    public static int DockOf<T>(DeskRect paper, IReadOnlyList<T> docks, Func<T, float, float, bool> holds)
    {
        if (docks == null || holds == null)
            return -1;
        for (int i = 0; i < docks.Count; i++)
            if (holds(docks[i], paper.CentreX, paper.CentreY))
                return i;
        return -1;
    }

    /// <summary>The area two rectangles share (square metres).</summary>
    private static float Shared(DeskRect a, DeskRect b)
    {
        float x = Math.Min(a.CentreX + a.Width / 2f, b.CentreX + b.Width / 2f) - Math.Max(a.CentreX - a.Width / 2f, b.CentreX - b.Width / 2f);
        float y = Math.Min(a.CentreY + a.Height / 2f, b.CentreY + b.Height / 2f) - Math.Max(a.CentreY - a.Height / 2f, b.CentreY - b.Height / 2f);
        return x > 0f && y > 0f ? x * y : 0f;
    }

    /// <summary>One axis of Inside: the centre <paramref name="c"/> of a span <paramref name="size"/> long kept inside the range centred at <paramref name="rangeCentre"/>, <paramref name="rangeSize"/> long.</summary>
    private static float Axis(float c, float size, float rangeCentre, float rangeSize)
    {
        float room = (rangeSize - size) / 2f;
        if (room <= 0f)
            return rangeCentre;
        return Math.Min(Math.Max(c, rangeCentre - room), rangeCentre + room);
    }

    private static float Distance(float dx, float dy) => (float)Math.Sqrt(dx * dx + dy * dy);
}
