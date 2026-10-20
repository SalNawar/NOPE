using System;

/// <summary>A scanned page's fit in its pane (PageFit.Of). Not serialized.</summary>
public readonly struct PageFitResult
{
    /// <summary>The page's own scale inside its parent (the pane's text zoom multiplies it on screen).</summary>
    public readonly float Scale;

    /// <summary>The page's box at that scale (its own units): the parent's width and height, so the box is never wider than its pane.</summary>
    public readonly float Width, Height;

    /// <summary>True when the page's design width does not fit its box: its scroll moves sideways too (never a cut edge).</summary>
    public readonly bool Sideways;

    /// <summary>A fit.</summary>
    public PageFitResult(float scale, float width, float height, bool sideways)
    {
        Scale = scale;
        Width = width;
        Height = height;
        Sideways = sideways;
    }
}

/// <summary>
/// A scanned page's fit under the pane's text zoom (CODE-111: Ctrl+= and
/// Settings' Text size barely changed a scanned document, and at 150 % cut
/// its edges). PaneZoom lays a pane's views out at the pane's width over the
/// zoom and scales them by it, so a page fitted to its parent's width undid
/// the zoom (the two scales cancelled). Here the page fits the pane's own
/// width (the parent's times the zoom), within the scale range, so on screen
/// the zoom grows it as it grows every other text; its box is the parent's
/// size at that scale, never wider, and when the page is wider than its box
/// (zoomed in, or a narrow split pane under the floor) its scroll moves
/// sideways instead of the pane's mask cutting the edges. The page keeps its
/// own layout (its fields never reflow: the document design spec D2). Pure;
/// FitToWidth applies it.
/// </summary>
public static class PageFit
{
    /// <summary>The least zoom and scale used (a zero or negative one counts as this).</summary>
    private const float Least = 0.01f;

    /// <summary>
    /// The fit of a page <paramref name="designWidth"/> wide in a parent
    /// <paramref name="parentWidth"/> by <paramref name="parentHeight"/> (its
    /// own units) under the pane's <paramref name="zoom"/> (1 = 100 %), its
    /// scale kept within <paramref name="minScale"/> and <paramref name="maxScale"/>.
    /// </summary>
    public static PageFitResult Of(float parentWidth, float parentHeight, float zoom, float designWidth, float minScale, float maxScale)
    {
        float z = Math.Max(Least, zoom);
        float design = Math.Max(1f, designWidth);
        float min = Math.Max(Least, minScale), max = Math.Max(min, maxScale);
        float scale = Math.Max(min, Math.Min(max, parentWidth * z / design));
        float width = Math.Max(0f, parentWidth) / scale;
        return new PageFitResult(scale, width, Math.Max(0f, parentHeight) / scale, width < design - 0.5f);
    }
}
