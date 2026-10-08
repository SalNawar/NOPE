using System;

/// <summary>
/// The look at the city's timeline (Saleh 2026-10-07: "hook the city
/// background to transition smoothly when the player presses left, you
/// should be able to see the FULL picture", then "there should be a fade
/// away maybe, because the city is not actually visible from the hall"):
/// one clock in seconds, 0 at the desk and <see cref="Total"/> at the city.
/// First the hall turns toward its window wall (the art's left pan, eased
/// over the turn's seconds); from a share of the turn on, the fade's seconds
/// take the screen through the matte to the city: the matte veils the hall
/// over the fade's first half, then the city panorama comes up over the
/// matte in its second half, so the hall (whose windows show the same city,
/// at another place and scale) and the panorama never show at once (Saleh
/// 2026-10-08: "city parallax effect is weird it shows everything doubled").
/// Turning back runs the same clock down, so the city goes first, then the
/// matte, and the hall turns back after it. The city's flying traffic flies
/// whole across it, entering and leaving off the frame (<see cref="Flight"/>).
/// Engine-free (tested headless); CityView advances the clock.
/// </summary>
public static class CityLookTimeline
{
    /// <summary>
    /// The clock's length in seconds: the end of the turn
    /// (<paramref name="turnSeconds"/>) or of the fade (which starts at
    /// <paramref name="fadeFrom"/> of the turn and lasts
    /// <paramref name="fadeSeconds"/>), whichever is later; never below 0.
    /// </summary>
    public static float Total(float turnSeconds, float fadeFrom, float fadeSeconds)
    {
        float turn = Math.Max(0f, turnSeconds);
        return Math.Max(turn, FadeStart(turn, fadeFrom) + Math.Max(0f, fadeSeconds));
    }

    /// <summary>The hall's left pan (0 the desk, 1 the art's full left pan) at <paramref name="seconds"/> on the clock, eased; 1 for a turn of no length.</summary>
    public static float Pan(float seconds, float turnSeconds) =>
        turnSeconds <= 0f ? 1f : Ease(seconds / turnSeconds);

    /// <summary>The matte's opacity over the hall (0..1) at <paramref name="seconds"/> on the clock: eased over the fade's first half; a fade of no length cuts at its start.</summary>
    public static float Veil(float seconds, float turnSeconds, float fadeFrom, float fadeSeconds) =>
        Ease(FadeShare(seconds, turnSeconds, fadeFrom, fadeSeconds) * 2f);

    /// <summary>The city panorama's opacity over the matte (0..1) at <paramref name="seconds"/> on the clock: eased over the fade's second half, once the matte hides the hall; a fade of no length cuts at its start.</summary>
    public static float Reveal(float seconds, float turnSeconds, float fadeFrom, float fadeSeconds) =>
        Ease(FadeShare(seconds, turnSeconds, fadeFrom, fadeSeconds) * 2f - 1f);

    /// <summary>Moves the clock <paramref name="seconds"/> toward the city (<paramref name="toCity"/>) or the desk by <paramref name="delta"/>, kept inside 0..<paramref name="total"/>; a cut (Reduced Motion) jumps to the end.</summary>
    public static float Step(float seconds, bool toCity, float delta, float total, bool cut)
    {
        if (cut)
            return toCity ? total : 0f;
        return Math.Min(total, Math.Max(0f, seconds + (toCity ? delta : -delta)));
    }

    /// <summary>
    /// The city's parallax as it comes into view (Saleh 2026-10-07: "no
    /// parallax when you switch to it"): the share (1..0, eased) of its sweep
    /// left after the city has shown for <paramref name="shownSeconds"/> of a
    /// <paramref name="settleSeconds"/> settle: the panorama starts turned
    /// with the hall and settles, its flying traffic (nearer) sweeping further
    /// than the painting; 0 once settled or for a settle of no length.
    /// </summary>
    public static float Sweep(float shownSeconds, float settleSeconds) =>
        settleSeconds <= 0f ? 0f : 1f - Ease(shownSeconds / settleSeconds);

    /// <summary>
    /// Where a vehicle of a traffic lane is along it (0 its start, 1 its end)
    /// after <paramref name="seconds"/>: from its <paramref name="phase"/>, at
    /// <paramref name="speed"/> pixels a second over the lane's
    /// <paramref name="length"/> pixels, wrapping (the hall window's own
    /// traffic rule, HallCityExterior; the city view draws the same lanes).
    /// </summary>
    public static float Travel(float seconds, float phase, float speed, float length)
    {
        float along = phase + (length > 0f ? seconds * speed / length : 0f);
        return along - (float)Math.Floor(along);
    }

    /// <summary>
    /// Where a flyer is across the painting (its centre, in painting widths:
    /// 0 its left edge, 1 its right) at <paramref name="along"/> (0..1) of its
    /// pass, flying <paramref name="leftToRight"/> or back: each pass runs
    /// from wholly outside the view to wholly outside it on the other side, so
    /// its wrap is never seen. <paramref name="halfWidth"/> is its half width
    /// and <paramref name="reach"/> how far past the painting's edges the view
    /// can see it (its parallax can shift it further than the painting's crop
    /// allows: <see cref="Reach"/>), both in painting widths.
    /// </summary>
    public static float Flight(float along, bool leftToRight, float halfWidth, float reach)
    {
        float margin = Math.Max(0f, halfWidth) + Math.Max(0f, reach);
        float x = -margin + along * (1f + 2f * margin);
        return leftToRight ? x : 1f - x;
    }

    /// <summary>The length of a flyer's pass (<see cref="Flight"/>) in painting widths: the painting and its margin past each edge.</summary>
    public static float FlightLength(float halfWidth, float reach) =>
        1f + 2f * (Math.Max(0f, halfWidth) + Math.Max(0f, reach));

    /// <summary>
    /// How far past the painting's edges (painting widths) the view sees a
    /// layer that pans <paramref name="depth"/> times the painting, when the
    /// painting is cropped by <paramref name="crop"/> at each side and pans at
    /// most that much: 0 for a layer that pans no more than the painting.
    /// </summary>
    public static float Reach(float crop, float depth) => Math.Max(0f, crop * (depth - 1f));

    private static float FadeStart(float turnSeconds, float fadeFrom) => turnSeconds * Math.Min(1f, Math.Max(0f, fadeFrom));

    /// <summary>The share of the fade done at <paramref name="seconds"/> (0..1, linear); a fade of no length is all or nothing at its start.</summary>
    private static float FadeShare(float seconds, float turnSeconds, float fadeFrom, float fadeSeconds)
    {
        float start = FadeStart(Math.Max(0f, turnSeconds), fadeFrom);
        if (fadeSeconds <= 0f)
            return seconds >= start ? 1f : 0f;
        return Math.Min(1f, Math.Max(0f, (seconds - start) / fadeSeconds));
    }

    /// <summary>Smoothstep of <paramref name="t"/> clamped to 0..1.</summary>
    private static float Ease(float t)
    {
        t = Math.Min(1f, Math.Max(0f, t));
        return t * t * (3f - 2f * t);
    }
}
