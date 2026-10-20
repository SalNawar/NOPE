using System;

/// <summary>
/// The booth's roll shutter (FLOW; the game feel bible's rows G3, G4 and J5).
/// <list type="bullet">
/// <item><b>The booth's own shutter</b> (BoothShutter, over the glass): it rolls
/// up as the shift opens (G3: 1.2 s, OutCubic, <see cref="OpenCue"/>) and down
/// once the last traveller's payoff has played (G4: 1.2 s, InCubic, ending on
/// <see cref="CloseCue"/>'s clang), before the report.</item>
/// <item><b>A scene change's veil</b> (TransitShutter, the in-world transition
/// of BUG-31a and J5): the same shutter, drawn over the screen, comes down and
/// goes up in the transition kit's length (TransitCurve), and while it is down
/// the hall's light moves through its slats (<see cref="Sweep"/>), so a load
/// never shows a frame held without motion (the [BP] pass: none over 0.25 s).</item>
/// </list>
/// Pure.
/// </summary>
public static class ShutterRoll
{
    /// <summary>The booth shutter's roll, up or down (seconds; G3, G4). MotionTuningSO.shutterSeconds tunes it.</summary>
    public const float DefaultSeconds = 1.2f;

    /// <summary>The booth shutter's sound cues (ids in the sound bank; the bible's G3 and G4).</summary>
    public const string OpenCue = "booth_shutter_open", CloseCue = "booth_shutter_close";

    /// <summary>How far the light moves down the veil's slats a second (screen heights): fast enough that every 30 fps frame differs, slow enough to read as light, not flicker; under Reduced Motion, slower.</summary>
    public const float SweepPerSecond = 0.75f, ReducedSweepPerSecond = 0.4f;

    /// <summary>
    /// How open the booth shutter is (0 shut, 1 open), <paramref name="elapsed"/>
    /// seconds into a roll of <paramref name="seconds"/> that started at
    /// <paramref name="from"/>: up (<paramref name="opening"/>) eases out (OutCubic:
    /// it starts at speed and settles at the top), down eases in (InCubic: it
    /// gathers speed and lands, the clang). No length: the end at once.
    /// </summary>
    public static float At(float from, bool opening, float elapsed, float seconds)
    {
        float start = Clamp01(from), end = opening ? 1f : 0f;
        float t = seconds > 0f ? Clamp01(elapsed / seconds) : 1f;
        float u = 1f - t;
        float eased = opening ? 1f - u * u * u : t * t * t;
        return start + (end - start) * eased;
    }

    /// <summary>True once a roll of <paramref name="seconds"/> has run <paramref name="elapsed"/> seconds (at once for no length).</summary>
    public static bool Done(float elapsed, float seconds) => !(seconds > 0f) || elapsed >= seconds;

    /// <summary>The veil's light, <paramref name="seconds"/> after <paramref name="phase"/> (0..1, one screen height a cycle), wrapped: it always moves on (<see cref="SweepPerSecond"/>; <paramref name="reduced"/>: <see cref="ReducedSweepPerSecond"/>).</summary>
    public static float Sweep(float phase, float seconds, bool reduced)
    {
        float next = phase + Math.Max(0f, seconds) * (reduced ? ReducedSweepPerSecond : SweepPerSecond);
        next -= (float)Math.Floor(next);
        return float.IsNaN(next) ? 0f : next;
    }

    private static float Clamp01(float v) => float.IsNaN(v) || v <= 0f ? 0f : v >= 1f ? 1f : v;
}
