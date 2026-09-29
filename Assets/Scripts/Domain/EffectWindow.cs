/// <summary>
/// When a timeline effect is in force (audit R3-006): from its start day, for
/// its duration in days (a negative duration: until it is removed). An effect
/// that starts tomorrow is not in force today, so a consequence earned at the
/// end of a shift waits for the next day; it has not ended either, so the
/// night's expiry keeps it. Pure, so the window is tested headless;
/// ActiveEffectEntry asks it.
/// </summary>
public static class EffectWindow
{
    /// <summary>True on <paramref name="day"/> when the effect has started and not yet ended.</summary>
    public static bool IsActive(int startDay, int durationDays, int day) =>
        day >= startDay && !HasEnded(startDay, durationDays, day);

    /// <summary>True once the effect's last day is behind <paramref name="day"/> (never for a permanent effect, nor before it starts).</summary>
    public static bool HasEnded(int startDay, int durationDays, int day) =>
        durationDays >= 0 && day >= startDay + durationDays;
}
