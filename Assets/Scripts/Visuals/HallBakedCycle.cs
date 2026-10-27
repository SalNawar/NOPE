using System;

/// <summary>
/// The hall painting's four lighting states (morning, noon, evening, night:
/// the tints HallDeepLayout blends, the city view's paintings, the daylight's
/// direction) read off the one day cycle (BUG-25b: the hard-coded keys that
/// disagreed with the lamps by up to 2.5 hours are gone). The night's share
/// is the cycle's evening (<see cref="HallDayCycle.Evening"/>), the same
/// value the lamps, the city's windows and the calendar's ink switch on, so
/// the painting is night exactly when they are; the daylight's share is split
/// by where the sun stands on the solar day: morning after sunrise, noon from
/// late morning, evening through the afternoon into the sunset.
/// </summary>
public static class HallBakedCycle
{
    /// <summary>How much of each baked state shows: x morning, y noon, z evening, w night (they sum to 1).</summary>
    public readonly struct Weights4
    {
        /// <summary>The morning state's share.</summary>
        public readonly float x;
        /// <summary>The noon state's share.</summary>
        public readonly float y;
        /// <summary>The evening state's share.</summary>
        public readonly float z;
        /// <summary>The night state's share.</summary>
        public readonly float w;

        /// <summary>The four shares, morning to night.</summary>
        public Weights4(float a, float b, float c, float d) { x = a; y = b; z = c; w = d; }
    }

    /// <summary>The share of the day (sunrise 0 to sunset 1) over which the morning hands over to noon.</summary>
    private const float MorningFrom = 0.05f, MorningTo = 0.45f;

    /// <summary>The share of the day over which noon hands over to the evening (full at the sunset).</summary>
    private const float EveningFrom = 0.6f, EveningTo = 1f;

    /// <summary>The states' shares at <paramref name="hour"/> on the shared cycle (<see cref="HallDayCycle.Active"/>, the hall rig's knobs).</summary>
    public static Weights4 Weights(float hour) => Weights(hour, HallDayCycle.Active);

    /// <summary>
    /// The states' shares at <paramref name="hour"/> (any real hour, wrapped to
    /// the day; NaN or infinity reads as noon) on the cycle <paramref name="s"/>:
    /// night = the evening; the daylight split into morning, noon and evening
    /// by the sun's place between sunrise and sunset (smoothstepped hand-overs).
    /// </summary>
    public static Weights4 Weights(float hour, in HallDayCycle.Settings s)
    {
        if (float.IsNaN(hour) || float.IsInfinity(hour))
            hour = 12f;
        float daylight = HallDayCycle.Daylight(hour, s);
        float t = Math.Min(1f, Math.Max(0f, (HallDayCycle.SolarPosition(hour, s) - 0.25f) / 0.5f));
        float morning = t < 0.5f ? 1f - Smooth(MorningFrom, MorningTo, t) : 0f;
        float evening = Smooth(EveningFrom, EveningTo, t);
        float noon = 1f - morning - evening;
        return new Weights4(morning * daylight, noon * daylight, evening * daylight, 1f - daylight);
    }

    private static float Smooth(float from, float to, float x)
    {
        float t = Math.Min(1f, Math.Max(0f, (x - from) / (to - from)));
        return t * t * (3f - 2f * t);
    }
}
