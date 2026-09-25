using System;
using System.Collections.Generic;

/// <summary>
/// An axis-aligned rectangle in a desk's local plane (the rectangle paper
/// centres stay in, a scanner's drop area). Negative sizes count as 0.
/// </summary>
public readonly struct DeskRect
{
    private readonly float _centreX;
    private readonly float _centreY;
    private readonly float _halfWidth;
    private readonly float _halfHeight;

    /// <summary>Creates a rectangle from its centre and size.</summary>
    public DeskRect(float centreX, float centreY, float width, float height)
    {
        _centreX = centreX;
        _centreY = centreY;
        _halfWidth = Math.Max(width, 0f) / 2f;
        _halfHeight = Math.Max(height, 0f) / 2f;
    }

    /// <summary>True when the point lies inside or on an edge.</summary>
    public bool Contains(float x, float y) =>
        x >= _centreX - _halfWidth && x <= _centreX + _halfWidth &&
        y >= _centreY - _halfHeight && y <= _centreY + _halfHeight;

    /// <summary>The nearest point inside the rectangle.</summary>
    public (float x, float y) Clamp(float x, float y) =>
        (Math.Min(Math.Max(x, _centreX - _halfWidth), _centreX + _halfWidth),
         Math.Min(Math.Max(y, _centreY - _halfHeight), _centreY + _halfHeight));

    /// <summary>The point at (u, v) across the rectangle, each clamped to 0..1; (0, 0) is the bottom left.</summary>
    public (float x, float y) PointAt(float u, float v) =>
        (_centreX - _halfWidth + 2f * _halfWidth * Math.Min(Math.Max(u, 0f), 1f),
         _centreY - _halfHeight + 2f * _halfHeight * Math.Min(Math.Max(v, 0f), 1f));
}

/// <summary>Keeps a span inside a range, one axis at a time (desktop windows, overlay callouts).</summary>
public static class RectClamp
{
    /// <summary>
    /// The offset that moves the span [min, max] inside [lo, hi]: 0 when it
    /// already is inside; for a span longer than the range, the offset that
    /// aligns max with hi when <paramref name="keepMax"/> (a window's title bar
    /// stays visible), else min with lo.
    /// </summary>
    public static float Shift(float min, float max, float lo, float hi, bool keepMax)
    {
        if (max - min > hi - lo)
            return keepMax ? hi - max : lo - min;
        if (min < lo)
            return lo - min;
        if (max > hi)
            return hi - max;
        return 0f;
    }
}

/// <summary>
/// The z order of the papers on the desk by paper id, bottom first. Papers
/// never leave the stack one by one; the case's end clears it.
/// </summary>
public sealed class PaperStack
{
    private readonly List<int> _order = new List<int>();

    /// <summary>Puts the paper on top (re-adding moves it there).</summary>
    public void Add(int id)
    {
        _order.Remove(id);
        _order.Add(id);
    }

    /// <summary>Moves a stacked paper to the top (an absent one stays absent).</summary>
    public void BringToFront(int id)
    {
        if (_order.Remove(id))
            _order.Add(id);
    }

    /// <summary>Empties the stack.</summary>
    public void Clear() => _order.Clear();

    /// <summary>The paper's place from the bottom (0), or -1 when it is not stacked.</summary>
    public int IndexOf(int id) => _order.IndexOf(id);
}

/// <summary>The booth's sorting bands on the Default layer, checked by the builder so input and drawing order agree.</summary>
public static class SortingBands
{
    /// <summary>
    /// Every band that does not sit strictly above the one below it: props &lt;
    /// focus exit zone &lt; glass zone &lt; bezel &lt; screen canvas &lt; paper
    /// base, and the held paper above the top stacked paper (paper base +
    /// <paramref name="maxPapers"/> - 1). Each problem names both bands and
    /// their values; empty when the bands are sound.
    /// </summary>
    public static List<string> Problems(int maxPropOrder, int focusExitOrder, int glassOrder, int bezelOrder,
                                        int screenCanvasOrder, int paperBaseOrder, int maxPapers, int heldPaperOrder)
    {
        var problems = new List<string>();
        void Above(string upper, int upperOrder, string lower, int lowerOrder)
        {
            if (upperOrder <= lowerOrder)
                problems.Add($"the {upper} ({upperOrder}) must sit above the {lower} ({lowerOrder})");
        }

        Above("focus exit zone", focusExitOrder, "highest prop order", maxPropOrder);
        Above("glass zone", glassOrder, "focus exit zone", focusExitOrder);
        Above("bezel", bezelOrder, "glass zone", glassOrder);
        Above("screen canvas", screenCanvasOrder, "bezel", bezelOrder);
        Above("paper base", paperBaseOrder, "screen canvas", screenCanvasOrder);
        Above("held paper", heldPaperOrder, "top paper order", paperBaseOrder + maxPapers - 1);
        return problems;
    }
}
