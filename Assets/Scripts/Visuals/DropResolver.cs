using System;
using System.Collections.Generic;

/// <summary>
/// Where a thing let go on the desk comes to rest (DOC-HANDLE, BUG-6, BUG-8;
/// Saleh's note 25: "it should stay where I drop it"; FOLD-01, Saleh's
/// playtest of 2026-10-28, point 4: "You move the folder and it snaps
/// somewhere else"): Papers, Please's honesty, a thing let go lies where it
/// is let go. Every rectangle is in one desk frame (DeskController's: x to
/// the right, y away from the chair, metres from the desk's centre).
/// <list type="bullet">
/// <item>It rests exactly where it was released (its footprint at its final
/// size), unless that leaves part of it outside <c>area</c> (the desk the
/// reading view shows: the whole rect stays in view) or over something in
/// <c>keepOut</c> (a machine or an object standing on the desk, the
/// scanner's hidden shadow, the counter strip for a desk paper refused
/// there).</item>
/// <item>Out of the area, it takes the least move inward.</item>
/// <item>Over a keep-out, it takes the nearest place inside the area clear
/// of every keep-out at once (one joint solve, never one push at a time,
/// which used to land a paper in an earlier blocker).</item>
/// <item>Where no place is clear, the nearest place that overlaps less than
/// where it was let go, else it stays: never the least overlap anywhere
/// (that rule could take a thing across the desk).</item>
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

    /// <summary>How much less a place must overlap the keep-outs to count as overlapping less (square metres: one square millimetre, above float noise).</summary>
    private const float LessOverlap = 1e-6f;

    /// <summary>
    /// Where <paramref name="paper"/> (its footprint where it was released, at
    /// its final size) rests: its own centre when it lies wholly inside
    /// <paramref name="area"/> and clear of every <paramref name="keepOut"/>;
    /// partly outside the area, the least move inward; over a keep-out, the
    /// nearest place (to its release) inside the area clear of every
    /// keep-out; where none is clear, the nearest place that overlaps the
    /// keep-outs less than the release did, else the release moved inside the
    /// area. <paramref name="keepOut"/> may be null.
    /// </summary>
    public static (float x, float y) Rest(DeskRect paper, DeskRect area, IReadOnlyList<DeskRect> keepOut)
    {
        (float x, float y) inside = Inside(paper, area);
        float w = paper.Width, h = paper.Height;
        float atRelease = Overlap(new DeskRect(inside.x, inside.y, w, h), keepOut);
        if (atRelease <= 0f)
            return inside;

        float hw = w / 2f, hh = h / 2f;
        var xs = new List<float> { inside.x };
        var ys = new List<float> { inside.y };
        foreach (DeskRect r in keepOut)
        {
            xs.Add(r.CentreX - r.Width / 2f - Gap - hw);
            xs.Add(r.CentreX + r.Width / 2f + Gap + hw);
            ys.Add(r.CentreY - r.Height / 2f - Gap - hh);
            ys.Add(r.CentreY + r.Height / 2f + Gap + hh);
        }

        // The nearest clear place; failing that, the nearest place that overlaps less than the release (never the least overlap anywhere).
        bool clear = false, better = false;
        (float x, float y) nearestClear = inside, nearestBetter = inside;
        float clearMove = float.MaxValue, betterMove = float.MaxValue;
        foreach (float cx in xs)
            foreach (float cy in ys)
            {
                (float x, float y) c = Inside(new DeskRect(cx, cy, w, h), area);
                float overlap = Overlap(new DeskRect(c.x, c.y, w, h), keepOut);
                float move = Distance(c.x - paper.CentreX, c.y - paper.CentreY);
                if (overlap <= 0f)
                {
                    if (move < clearMove - 1e-6f)
                    {
                        clear = true;
                        nearestClear = c;
                        clearMove = move;
                    }
                }
                else if (overlap < atRelease - LessOverlap && move < betterMove - 1e-6f)
                {
                    better = true;
                    nearestBetter = c;
                    betterMove = move;
                }
            }
        return clear ? nearestClear : better ? nearestBetter : inside;
    }

    /// <summary>
    /// The area a thing on the desk rests wholly inside (FOLD-01), in the
    /// frame the reading view is posed on: <paramref name="desk"/> (its centre
    /// anywhere a drag may take it), short of <paramref name="far"/> (where the
    /// back step begins: the counter row stands on it, and its front draws
    /// through a paper laid across it; positive infinity for none), and the
    /// rectangle inside the reading view's four corners on
    /// the desk (<paramref name="view"/>: near left, near right, far left, far
    /// right; null for no view). Where they leave nothing, <paramref name="desk"/>.
    /// The corners must be taken in the frame the view is posed on: in a frame
    /// turned against it (the office camera's line to the scanner, 27 degrees
    /// off) the rectangle inside them shrank to a band 10 cm deep, and every
    /// rest's depth was put at its middle.
    /// </summary>
    public static DeskRect Area(DeskRect desk, float far, IReadOnlyList<(float x, float y)> view)
    {
        float left = desk.CentreX - desk.Width / 2f, right = desk.CentreX + desk.Width / 2f;
        float near = desk.CentreY - desk.Height / 2f;
        far = Math.Min(desk.CentreY + desk.Height / 2f, far);
        if (view != null && view.Count >= 4)
        {
            left = Math.Max(left, Math.Max(view[0].x, view[2].x));
            right = Math.Min(right, Math.Min(view[1].x, view[3].x));
            near = Math.Max(near, Math.Max(view[0].y, view[1].y));
            far = Math.Min(far, Math.Min(view[2].y, view[3].y));
        }
        if (right <= left || far <= near)
            return desk;
        return new DeskRect((left + right) / 2f, (near + far) / 2f, right - left, far - near);
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
