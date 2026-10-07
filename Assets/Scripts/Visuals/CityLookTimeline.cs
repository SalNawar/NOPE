using System;

/// <summary>
/// The look at the city's timeline (Saleh 2026-10-07: "hook the city
/// background to transition smoothly when the player presses left, you
/// should be able to see the FULL picture", then "there should be a fade
/// away maybe, because the city is not actually visible from the hall"):
/// one clock in seconds, 0 at the desk and <see cref="Total"/> at the city.
/// First the hall turns toward its window wall (the art's left pan, eased
/// over the turn's seconds); from a share of the turn on, the whole city
/// panorama fades in over the fade's seconds. Turning back runs the same
/// clock down, so the fade goes first and the hall turns back after it.
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

    /// <summary>The city panorama's opacity (0..1) at <paramref name="seconds"/> on the clock, eased; a fade of no length cuts at its start.</summary>
    public static float Fade(float seconds, float turnSeconds, float fadeFrom, float fadeSeconds)
    {
        float start = FadeStart(Math.Max(0f, turnSeconds), fadeFrom);
        if (fadeSeconds <= 0f)
            return seconds >= start ? 1f : 0f;
        return Ease((seconds - start) / fadeSeconds);
    }

    /// <summary>Moves the clock <paramref name="seconds"/> toward the city (<paramref name="toCity"/>) or the desk by <paramref name="delta"/>, kept inside 0..<paramref name="total"/>; a cut (Reduced Motion) jumps to the end.</summary>
    public static float Step(float seconds, bool toCity, float delta, float total, bool cut)
    {
        if (cut)
            return toCity ? total : 0f;
        return Math.Min(total, Math.Max(0f, seconds + (toCity ? delta : -delta)));
    }

    private static float FadeStart(float turnSeconds, float fadeFrom) => turnSeconds * Math.Min(1f, Math.Max(0f, fadeFrom));

    /// <summary>Smoothstep of <paramref name="t"/> clamped to 0..1.</summary>
    private static float Ease(float t)
    {
        t = Math.Min(1f, Math.Max(0f, t));
        return t * t * (3f - 2f * t);
    }
}
