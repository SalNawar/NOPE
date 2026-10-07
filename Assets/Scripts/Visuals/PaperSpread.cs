using System;
using System.Collections.Generic;

/// <summary>
/// Something already lying in the spread's area: its footprint on the desk
/// (the office view's frame, metres), its key parts (a header, a photo: the
/// rectangles a new paper must not cover), how much covering the rest of it
/// costs (a paper 1, a citation less, the rulebook folder more).
/// </summary>
public readonly struct SpreadTaken
{
    /// <summary>The footprint.</summary>
    public readonly DeskRect Rect;

    /// <summary>The key parts' rectangles in the same frame (empty for none).</summary>
    public readonly IReadOnlyList<DeskRect> Keys;

    /// <summary>The cost of covering a square metre of it outside its keys.</summary>
    public readonly float Weight;

    /// <summary>Creates a taken place.</summary>
    public SpreadTaken(DeskRect rect, IReadOnlyList<DeskRect> keys, float weight)
    {
        Rect = rect;
        Keys = keys ?? Array.Empty<DeskRect>();
        Weight = weight;
    }
}

/// <summary>
/// Where a paper landing on the desk lies (Saleh's playtest 2026-10-07:
/// "documents overlap"; lay them out so they overlap as little as the desk
/// allows, a staggered fan, their headers and photos visible at once): the
/// place inside the area whose footprint covers the least of what lies there,
/// the others' key parts (KeyWeight a square metre) far more than the rest
/// of them (each taken's Weight), and among equal places the nearest to the
/// preferred point (the fan's next step). The player can still pile the
/// papers up by hand. Pure and deterministic: a grid of candidates, the first
/// least cost wins.
/// </summary>
public static class PaperSpread
{
    /// <summary>What covering a whole key (KeyArea) of another paper's header, photo or visa box costs, against 1 a square metre for the rest of a paper.</summary>
    public const float KeyWeight = 8f;

    /// <summary>What a metre from the preferred point costs (squared): only a tie-break between places that cover the same.</summary>
    public const float DistanceWeight = 0.002f;

    /// <summary>The candidates across each axis of the area.</summary>
    public const int Steps = 16;

    /// <summary>The overlap area of two rectangles (0 when apart).</summary>
    public static float Overlap(DeskRect a, DeskRect b)
    {
        float w = Math.Min(a.CentreX + a.Width / 2f, b.CentreX + b.Width / 2f) - Math.Max(a.CentreX - a.Width / 2f, b.CentreX - b.Width / 2f);
        float h = Math.Min(a.CentreY + a.Height / 2f, b.CentreY + b.Height / 2f) - Math.Max(a.CentreY - a.Height / 2f, b.CentreY - b.Height / 2f);
        return w > 0f && h > 0f ? w * h : 0f;
    }

    /// <summary>
    /// What a footprint at <paramref name="at"/> costs against <paramref name="taken"/>
    /// (no distance): each key's covered area at KeyWeight, steeper past
    /// KeptShare of it (a header or photo half in view still reads; one
    /// buried does not), the rest of each taken at its Weight.
    /// </summary>
    public static float Cost(DeskRect at, IReadOnlyList<SpreadTaken> taken)
    {
        float cost = 0f;
        if (taken == null)
            return cost;
        foreach (SpreadTaken t in taken)
        {
            float keys = 0f;
            foreach (DeskRect k in t.Keys)
            {
                float covered = Overlap(at, k), area = k.Width * k.Height;
                keys += covered;
                float share = area > 0f ? covered / area : 0f;
                cost += KeyWeight * KeyArea * (share + Buried * Math.Max(0f, share - KeptShare));
            }
            cost += t.Weight * Math.Max(0f, Overlap(at, t.Rect) - keys);
        }
        return cost;
    }

    /// <summary>What a key counts as, in square metres, whatever its size: a small ticket's header matters as much as a passport's.</summary>
    public const float KeyArea = 0.01f;

    /// <summary>The share of a key that may be covered before covering more costs Buried times as much again.</summary>
    public const float KeptShare = 0.5f, Buried = 4f;

    /// <summary>The rectangle both <paramref name="a"/> and <paramref name="b"/> hold (a zero-size one at a's centre when they do not meet).</summary>
    public static DeskRect Intersect(DeskRect a, DeskRect b)
    {
        float left = Math.Max(a.CentreX - a.Width / 2f, b.CentreX - b.Width / 2f), right = Math.Min(a.CentreX + a.Width / 2f, b.CentreX + b.Width / 2f);
        float near = Math.Max(a.CentreY - a.Height / 2f, b.CentreY - b.Height / 2f), far = Math.Min(a.CentreY + a.Height / 2f, b.CentreY + b.Height / 2f);
        return right > left && far > near ? new DeskRect((left + right) / 2f, (near + far) / 2f, right - left, far - near) : new DeskRect(a.CentreX, a.CentreY, 0f, 0f);
    }

    /// <summary>
    /// The centre for a paper of <paramref name="width"/> by <paramref name="height"/>
    /// inside <paramref name="area"/> (its footprint inside it where it fits; at the
    /// area's middle across an axis it does not), covering the least of
    /// <paramref name="taken"/>, ties to the nearest to (<paramref name="preferX"/>, <paramref name="preferY"/>).
    /// </summary>
    public static (float x, float y) Place(DeskRect area, float width, float height, IReadOnlyList<SpreadTaken> taken, float preferX, float preferY)
    {
        float minX = area.CentreX - area.Width / 2f + width / 2f, maxX = area.CentreX + area.Width / 2f - width / 2f;
        float minY = area.CentreY - area.Height / 2f + height / 2f, maxY = area.CentreY + area.Height / 2f - height / 2f;
        if (minX > maxX)
            minX = maxX = area.CentreX;
        if (minY > maxY)
            minY = maxY = area.CentreY;
        float px = Math.Max(minX, Math.Min(maxX, preferX)), py = Math.Max(minY, Math.Min(maxY, preferY));

        float bestX = px, bestY = py;
        float best = Cost(new DeskRect(px, py, width, height), taken);
        for (int j = 0; j <= Steps; j++)
            for (int i = 0; i <= Steps; i++)
            {
                float x = minX + (maxX - minX) * i / Steps, y = maxY - (maxY - minY) * j / Steps;
                float total = Cost(new DeskRect(x, y, width, height), taken) + DistanceWeight * ((x - px) * (x - px) + (y - py) * (y - py));
                if (total < best - 1e-7f)
                {
                    best = total;
                    bestX = x;
                    bestY = y;
                }
            }
        return (bestX, bestY);
    }

    /// <summary>
    /// The key parts of a footprint centred at (<paramref name="x"/>, <paramref name="y"/>)
    /// of <paramref name="width"/> by <paramref name="height"/> (y away from the
    /// camera: the paper's top is its far edge) from their
    /// <paramref name="shares"/> (each x from the paper's left, y from its top, 0 to 1:
    /// the share's left, top, width, height).
    /// </summary>
    public static List<DeskRect> Keys(float x, float y, float width, float height, IEnumerable<(float left, float top, float w, float h)> shares)
    {
        var keys = new List<DeskRect>();
        if (shares == null)
            return keys;
        foreach ((float left, float top, float w, float h) in shares)
            keys.Add(new DeskRect(x - width / 2f + (left + w / 2f) * width, y + height / 2f - (top + h / 2f) * height, w * width, h * height));
        return keys;
    }
}
