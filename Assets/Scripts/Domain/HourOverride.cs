using System;

/// <summary>
/// The one hour override (GATE-22, D13: "the Time cheat drives the clock, the
/// hall and the city"). The cheat menu's "Time: Morning / Dusk / Night" and the
/// QA rig's hour op set one forced hour (DevToolsState.ForcedHour, cleared at
/// every morning). Everything that shows the hour reads it: the shift clock's
/// readouts (ShiftClockDriver.ShownMinute: the desk's CRT, the wall clock, the
/// PC's tray and a scanned page's strip), the hall's light (HallDayCycle.ShownHour)
/// and through the hall's state weights the city in the hall's windows. The
/// shift's own clock runs on under it: closing time, the last-hour alarm and the
/// pay keep the true minute. While an hour is forced the clock shows that hour
/// and holds there. Pure; tested headless.
/// </summary>
public static class HourOverride
{
    /// <summary>The last minute of the day the clock can show (24:00).</summary>
    public const float MaxMinute = 1440f;

    /// <summary>
    /// The minute the clock shows: <paramref name="forcedHour"/> (0 to 24,
    /// clamped) as a minute of the day when an hour is forced, else
    /// <paramref name="clockMinute"/>, the shift clock's own.
    /// </summary>
    public static float ShownMinute(float? forcedHour, float clockMinute) =>
        forcedHour.HasValue ? Math.Max(0f, Math.Min(MaxMinute, forcedHour.Value * 60f)) : clockMinute;
}
