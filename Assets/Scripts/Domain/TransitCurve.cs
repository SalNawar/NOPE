using System;

/// <summary>
/// The screen transitions' timing (FLOW, BUG-32a; the bible's J5: one shared
/// kit of 0.2-0.3 s): how far a fade, a slide or a fade through black has
/// gone after some real seconds, eased in and out (a cubic smoothstep: it
/// leaves and lands at rest, so a fade never pops). Pure; Transit plays it.
/// </summary>
public static class TransitCurve
{
    /// <summary>The kit's shortest and longest transition (seconds): every screen change eases within them (MotionTuningSO.transitSeconds is clamped here).</summary>
    public const float MinSeconds = 0.2f, MaxSeconds = 0.3f;

    /// <summary>The ease of <paramref name="t"/> (0..1, clamped): 0 at the start, 1 at the end, monotonic, flat at both ends.</summary>
    public static float Ease(float t)
    {
        if (float.IsNaN(t) || t <= 0f)
            return 0f;
        if (t >= 1f)
            return 1f;
        return t * t * (3f - 2f * t);
    }

    /// <summary>How far a transition of <paramref name="seconds"/> has gone after <paramref name="elapsed"/> seconds (eased); 1 for no length.</summary>
    public static float At(float elapsed, float seconds) =>
        !(seconds > 0f) ? 1f : Ease(elapsed / seconds);

    /// <summary>True when a transition of <paramref name="seconds"/> cuts at once instead: outside play (<paramref name="playing"/> false: edit mode, the EditMode tests) or no length.</summary>
    public static bool Instant(float seconds, bool playing) => !playing || !(seconds > 0f);

    /// <summary>The kit's length from a tuning value: within <see cref="MinSeconds"/>..<see cref="MaxSeconds"/> (a value that is not a number reads the middle).</summary>
    public static float KitSeconds(float tuned) =>
        float.IsNaN(tuned) ? (MinSeconds + MaxSeconds) / 2f : Math.Max(MinSeconds, Math.Min(MaxSeconds, tuned));
}
