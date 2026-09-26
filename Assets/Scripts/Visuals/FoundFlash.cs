using System;

/// <summary>
/// The found flash of a search jump (the PC redesign SE4): the item a result
/// opens pulses in the form style's found colour (its pulses over their time,
/// then a steady outline) and is scrolled to the middle of its view. Pure, so
/// the timing and the scroll are tested headless.
/// </summary>
public static class FoundFlash
{
    /// <summary>
    /// The found fill's strength (0 to 1) <paramref name="elapsed"/> seconds
    /// after the jump: <paramref name="pulses"/> rises and falls (sin²) spread
    /// over <paramref name="duration"/>; 0 before, after, or without time or
    /// pulses.
    /// </summary>
    public static float Pulse(float elapsed, float duration, int pulses)
    {
        if (duration <= 0f || pulses <= 0 || elapsed <= 0f || elapsed >= duration)
            return 0f;
        double s = Math.Sin(Math.PI * pulses * elapsed / duration);
        return (float)(s * s);
    }

    /// <summary>True once the pulses are over (<paramref name="elapsed"/> at or past <paramref name="duration"/>): the steady outline stays.</summary>
    public static bool Done(float elapsed, float duration) => elapsed >= duration;

    /// <summary>
    /// The vertical scroll position (1 = the top, 0 = the bottom, as a
    /// ScrollRect's) that puts a point <paramref name="centre"/> units below
    /// the top of content <paramref name="content"/> tall in the middle of a
    /// viewport <paramref name="viewport"/> tall, clamped to the content; 1
    /// when the content fits.
    /// </summary>
    public static float CentredScroll(float content, float viewport, float centre)
    {
        float range = content - viewport;
        if (range <= 0f)
            return 1f;
        float top = Math.Max(0f, Math.Min(range, centre - viewport / 2f));
        return 1f - top / range;
    }
}
