using System.Collections.Generic;

/// <summary>
/// The Orders tree's zoom and pan (Saleh 2026-09-29, on reaching the Contacts
/// band of the maximised tree: "zoom drag and scroll"): the zoom levels the
/// tree may take (DesktopConfigSO.ordersZoomLevels, stepped by AppZoom.Step),
/// never so far out that its smallest text reads under the tree's text floor;
/// a zoom about a point (the pointer stays over the same spot of the tree); the
/// scroll kept inside the tree; and the least scroll that shows a node whole
/// (the arrows' selection). A scroll is measured along one axis from the
/// tree's top-left corner (right or down, in the view's units), so the same
/// rules serve both axes. Pure.
/// </summary>
public static class TreeZoom
{
    /// <summary>
    /// The levels (percent) at which the tree's smallest text,
    /// <paramref name="smallestText"/> units at 100 %, reads at least
    /// <paramref name="textFloor"/> units, in the levels' order; when none
    /// does, the one level where it just does (rounded up to a whole percent).
    /// </summary>
    public static List<int> ReadableLevels(IReadOnlyList<int> levels, float smallestText, float textFloor)
    {
        int least = LeastReadable(smallestText, textFloor);
        var readable = new List<int>();
        if (levels != null)
            foreach (int level in levels)
                if (level >= least)
                    readable.Add(level);
        if (readable.Count == 0)
            readable.Add(least);
        return readable;
    }

    /// <summary>The lowest whole percent at which <paramref name="smallestText"/> reads at least <paramref name="textFloor"/> (100 without sizes).</summary>
    public static int LeastReadable(float smallestText, float textFloor)
    {
        if (smallestText <= 0f || textFloor <= 0f)
            return AppZoom.Normal;
        // A hair under whole percents, so a floor met exactly is not pushed up a step by rounding.
        return (int)System.Math.Ceiling(textFloor / smallestText * 100.0 - 1e-4);
    }

    /// <summary>The level Ctrl+0 goes back to: 100 % when it is a readable level, else the lowest readable level.</summary>
    public static int ResetLevel(IReadOnlyList<int> readable)
    {
        if (readable == null || readable.Count == 0)
            return AppZoom.Normal;
        int lowest = readable[0];
        foreach (int level in readable)
        {
            if (level == AppZoom.Normal)
                return level;
            if (level < lowest)
                lowest = level;
        }
        return lowest;
    }

    /// <summary>
    /// The scroll after zooming from <paramref name="oldScale"/> to
    /// <paramref name="newScale"/> about a point <paramref name="pointer"/>
    /// units into the view, so the spot of the tree under it stays under it
    /// (before the scroll is kept inside the tree).
    /// </summary>
    public static float ZoomAbout(float scroll, float pointer, float oldScale, float newScale)
    {
        if (oldScale <= 0f)
            return scroll;
        return (scroll + pointer) * newScale / oldScale - pointer;
    }

    /// <summary>The scroll kept inside the tree: from 0 to the zoomed tree's overhang past the view (0 when it fits).</summary>
    public static float ClampScroll(float scroll, float zoomedContent, float view)
    {
        float most = zoomedContent - view;
        if (most <= 0f || scroll <= 0f)
            return 0f;
        return scroll > most ? most : scroll;
    }

    /// <summary>
    /// The least change of <paramref name="scroll"/> that shows the span from
    /// <paramref name="start"/> to <paramref name="end"/> (zoomed units from
    /// the tree's corner) inside the view with <paramref name="margin"/> to
    /// spare; a span longer than the view shows its start.
    /// </summary>
    public static float Reveal(float scroll, float start, float end, float view, float margin)
    {
        if (end - start + 2f * margin > view)
            return start - margin;
        if (start - margin < scroll)
            return start - margin;
        if (end + margin > scroll + view)
            return end + margin - view;
        return scroll;
    }
}
