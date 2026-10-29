using System;

/// <summary>
/// How a held thing's drag lift goes on at the pick-up and comes off at the
/// drop (FOLD-01 round 2, the verifier's F2; Saleh's playtest of 2026-10-28,
/// point 4: "You move the folder and it snaps somewhere else"; DOC-HANDLE's
/// obstacles check: "a frame diff at the press and release frames ≤1 px").
/// The rulebook folder set its lift at once, so every release dropped its
/// picture 8 to 11 px toward the screen's middle in one frame. Its lift now
/// moves on a smootherstep over <see cref="Seconds"/>: it starts and ends at
/// no speed, so the frame of the release moves the picture less than a
/// pixel (the first 30 fps frame takes 3.5% of the way), and it lands with
/// no bump. Pure, so it is tested headless (DeskLiftEaseTests).
/// </summary>
public static class DeskLiftEase
{
    /// <summary>How long the lift takes to go on or off (seconds): six frames at 30 fps.</summary>
    public const float Seconds = 0.2f;

    /// <summary>The share of the way the lift has gone after <paramref name="elapsed"/> of <paramref name="seconds"/> (0 to 1; 1 for no time).</summary>
    public static float Share(float elapsed, float seconds = Seconds)
    {
        if (seconds <= 0f)
            return 1f;
        float t = Math.Min(1f, Math.Max(0f, elapsed / seconds));
        return t * t * t * (t * (t * 6f - 15f) + 10f);
    }

    /// <summary>The lift after <paramref name="elapsed"/> seconds of an ease from <paramref name="from"/> to <paramref name="to"/> (metres).</summary>
    public static float At(float from, float to, float elapsed, float seconds = Seconds) => from + (to - from) * Share(elapsed, seconds);
}
