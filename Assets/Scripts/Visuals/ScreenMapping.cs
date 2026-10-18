using System;

/// <summary>
/// Maps the PC's 4:3 desktop onto screens: fitted inside a CRT glass of any
/// aspect (the office PC's live clone), and the PC frame's glass rectangle as
/// a camera viewport (the frame camera draws the desktop there); and how far
/// a piece of the overlay moves to keep clear of something on the screen (the
/// office's edge pull tabs and the calendar).
/// </summary>
public static class ScreenMapping
{
    /// <summary>
    /// The share of the glass (0..1 on each axis) the content fills when it is
    /// fitted inside at its own aspect, centred: pillar-boxed on a wider glass,
    /// letter-boxed on a taller one. A size or aspect of 0 or less fills the
    /// whole glass (1, 1).
    /// </summary>
    public static (float width, float height) Fit(float glassWidth, float glassHeight, float contentAspect)
    {
        if (glassWidth <= 0f || glassHeight <= 0f || contentAspect <= 0f)
            return (1f, 1f);

        float glassAspect = glassWidth / glassHeight;
        return glassAspect > contentAspect
            ? (contentAspect / glassAspect, 1f)
            : (1f, glassAspect / contentAspect);
    }

    /// <summary>
    /// A rectangle of screen pixels (from the bottom left) as a camera's
    /// normalized viewport rectangle (x, y, width, height), clipped to the
    /// screen; all zero for an empty screen or a rectangle wholly off it.
    /// </summary>
    public static (float x, float y, float width, float height) Viewport(float xMin, float yMin, float width, float height,
                                                                          float screenWidth, float screenHeight)
    {
        if (screenWidth <= 0f || screenHeight <= 0f || width <= 0f || height <= 0f)
            return (0f, 0f, 0f, 0f);

        float x0 = Math.Max(xMin / screenWidth, 0f);
        float y0 = Math.Max(yMin / screenHeight, 0f);
        float x1 = Math.Min((xMin + width) / screenWidth, 1f);
        float y1 = Math.Min((yMin + height) / screenHeight, 1f);
        if (x1 <= x0 || y1 <= y0)
            return (0f, 0f, 0f, 0f);
        return (x0, y0, x1 - x0, y1 - y0);
    }

    /// <summary>
    /// How far (signed, along one axis) the span [<paramref name="start"/>,
    /// <paramref name="end"/>] moves so it no longer overlaps the obstacle
    /// [<paramref name="lo"/>, <paramref name="hi"/>] grown by
    /// <paramref name="margin"/>: 0 when it is clear; else just past the
    /// obstacle on the side its centre lies on (below or at the obstacle's
    /// centre: toward lower values), or the other side when that one would
    /// leave [<paramref name="floor"/>, <paramref name="ceiling"/>]; with no
    /// room either way, the preferred side kept inside the bounds.
    /// </summary>
    public static float ClearShift(float start, float end, float lo, float hi, float margin, float floor, float ceiling)
    {
        float a = lo - margin, b = hi + margin;
        if (end <= a || start >= b)
            return 0f;
        float down = a - end, up = b - start;
        bool fitsDown = start + down >= floor, fitsUp = end + up <= ceiling;
        bool preferDown = (start + end) / 2f <= (lo + hi) / 2f;
        if (preferDown ? fitsDown : !fitsUp && fitsDown)
            return down;
        if (fitsUp)
            return up;
        float d = preferDown ? down : up;
        return Math.Min(Math.Max(d, floor - start), ceiling - end);
    }
}
