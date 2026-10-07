using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// One day's desk hours (night shifts, docs/superpowers/specs/2026-10-07-night-shifts-design.md;
/// Saleh 2026-10-07: "the hours grow"): the minute of the day the desk opens
/// and the minute it closes, 24:00 (1440) at the latest. A day plan authors
/// them (world_source.json days[].shiftStart / shiftEnd, "13:00"), else the day
/// keeps GameConfigSO's standard day (shiftStartHour to shiftEndHour). The
/// shift clock runs the day's span in GameConfigSO's real seconds, and every
/// visual system reads the clock's real minute, so a late shift is dark.
/// </summary>
public readonly struct ShiftHours : IEquatable<ShiftHours>
{
    /// <summary>The minute of the day the desk opens (540 = 09:00).</summary>
    public int StartMinute { get; }

    /// <summary>The minute of the day the desk closes (1020 = 17:00; 1440 = midnight).</summary>
    public int EndMinute { get; }

    /// <summary>The hours from <paramref name="startMinute"/> to <paramref name="endMinute"/> (not checked: see <see cref="Problems"/>).</summary>
    public ShiftHours(int startMinute, int endMinute)
    {
        StartMinute = startMinute;
        EndMinute = endMinute;
    }

    /// <summary>The shift's length on the clock face, in minutes.</summary>
    public int LengthMinutes => EndMinute - StartMinute;

    /// <summary>The opening time as the clock shows it ("13:00").</summary>
    public string Open => ShiftClock.Format(StartMinute);

    /// <summary>The closing time as the clock shows it ("21:00"; midnight shows "00:00").</summary>
    public string Close => ShiftClock.Format(EndMinute);

    /// <summary>
    /// A day's hours: the plan's own when it authors them (<paramref name="planStartMinute"/>
    /// 0 or more), else <paramref name="standard"/>, the config's standard day.
    /// </summary>
    public static ShiftHours For(int planStartMinute, int planEndMinute, ShiftHours standard) =>
        planStartMinute >= 0 ? new ShiftHours(planStartMinute, planEndMinute) : standard;

    /// <summary>
    /// Reads an authored time, "HH:MM" on the 24-hour clock from "00:00" to
    /// "24:00" (midnight at the end of the day); surrounding spaces are
    /// ignored. False for anything else.
    /// </summary>
    public static bool TryParse(string text, out int minute)
    {
        minute = -1;
        string t = text?.Trim();
        if (t == null || t.Length != 5 || t[2] != ':')
            return false;
        if (!int.TryParse(t.Substring(0, 2), NumberStyles.None, CultureInfo.InvariantCulture, out int hours)
            || !int.TryParse(t.Substring(3, 2), NumberStyles.None, CultureInfo.InvariantCulture, out int minutes))
            return false;
        if (minutes > 59 || hours > 24 || (hours == 24 && minutes > 0))
            return false;
        minute = hours * 60 + minutes;
        return true;
    }

    /// <summary>
    /// What is wrong with a day's authored hours (<paramref name="startMinute"/>
    /// and <paramref name="endMinute"/>, -1 for none), each problem naming
    /// <paramref name="owner"/>: both or neither, opening before closing,
    /// closing at 24:00 at the latest, and a length of
    /// <paramref name="minHours"/> to <paramref name="maxHours"/> (GameConfigSO's
    /// knobs). Generate World and the validator read the hours alike.
    /// </summary>
    public static List<string> Problems(string owner, int startMinute, int endMinute, int minHours, int maxHours)
    {
        var problems = new List<string>();
        bool hasStart = startMinute >= 0, hasEnd = endMinute >= 0;
        if (!hasStart && !hasEnd)
            return problems;
        if (hasStart != hasEnd)
        {
            problems.Add($"{owner} needs both \"shiftStart\" and \"shiftEnd\", or neither (the standard day).");
            return problems;
        }

        if (endMinute > ShiftClock.MinutesPerDay)
            problems.Add($"{owner}'s shift closes after 24:00.");
        if (startMinute >= endMinute || startMinute >= ShiftClock.MinutesPerDay)
            problems.Add($"{owner}'s shift must open before it closes ({ShiftClock.Format(startMinute)} to {ShiftClock.Format(endMinute)}).");
        else
        {
            int length = endMinute - startMinute;
            if (length < minHours * 60)
                problems.Add($"{owner}'s shift lasts {length / 60f:0.#} h; it must last at least {minHours} h (GameConfigSO shiftMinHours).");
            if (length > maxHours * 60)
                problems.Add($"{owner}'s shift lasts {length / 60f:0.#} h; it may last at most {maxHours} h (GameConfigSO shiftMaxHours).");
        }

        return problems;
    }

    /// <summary>True when the briefing announces today's hours: they differ from <paramref name="yesterday"/>'s (none on the first day).</summary>
    public static bool Announces(ShiftHours? yesterday, ShiftHours today) =>
        yesterday.HasValue && !yesterday.Value.Equals(today);

    /// <summary>
    /// Where <paramref name="minuteOfDay"/> stands on <paramref name="standard"/>
    /// (the config's standard day): 0 at or before its opening, 1 at or after
    /// its closing, linear between; a minute that is not a number reads 0. The
    /// art's evening curves (the crowds' palette, CrowdPaletteBlend) read it, so
    /// they follow the real hour and a late shift opens in the evening.
    /// </summary>
    public static float StandardProgress(float minuteOfDay, ShiftHours standard)
    {
        if (float.IsNaN(minuteOfDay) || standard.LengthMinutes <= 0)
            return 0f;
        float t = (minuteOfDay - standard.StartMinute) / standard.LengthMinutes;
        return t <= 0f ? 0f : t >= 1f ? 1f : t;
    }

    /// <summary>
    /// How late a shift closing at <paramref name="endMinute"/> sends the clerk
    /// home: 0 at or before <paramref name="standard"/>'s closing, 1 at
    /// midnight, linear between (Home's deep night, GameConfigSO homeDeepNightTint).
    /// </summary>
    public static float Lateness(int endMinute, ShiftHours standard)
    {
        int span = ShiftClock.MinutesPerDay - standard.EndMinute;
        if (span <= 0)
            return endMinute >= ShiftClock.MinutesPerDay ? 1f : 0f;
        float t = (endMinute - standard.EndMinute) / (float)span;
        return t <= 0f ? 0f : t >= 1f ? 1f : t;
    }

    /// <summary>Same opening and closing minutes.</summary>
    public bool Equals(ShiftHours other) => StartMinute == other.StartMinute && EndMinute == other.EndMinute;

    /// <summary>Same opening and closing minutes.</summary>
    public override bool Equals(object obj) => obj is ShiftHours other && Equals(other);

    /// <summary>A hash of the two minutes.</summary>
    public override int GetHashCode() => StartMinute * 1441 + EndMinute;

    /// <summary>"09:00-17:00" (logs and test messages).</summary>
    public override string ToString() => $"{Open}-{Close}";
}
