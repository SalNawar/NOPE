using System;

/// <summary>
/// The anime hall's day-night cycle (docs/HALL_LIGHTING.md): from the hour of
/// the day (the shift clock's minute, or a preview hour), how much daylight
/// the hall has (dawn, day, dusk, night), where the day stands on a canonical
/// solar day (the colour gradients of HallLightingSO are read there, so moving
/// sunrise or sunset moves every gradient with it), how far the sun has gone
/// along its arc (the window shafts turn with it), how far each interior
/// fixture has switched on (at dusk, one after another; off again at dawn in
/// the same order), and the flicker of a fixture striking (none with reduced
/// motion). Pure: HallLightingRig reads it every frame, so nothing here allocates.
/// </summary>
public static class HallDayCycle
{
    /// <summary>Hours in a day.</summary>
    public const float HoursPerDay = 24f;

    /// <summary>How long a fixture's switch-on flicker lasts (real seconds).</summary>
    public const float FlickerSeconds = 0.6f;

    /// <summary>The cycle's knobs (HallLightingSO's), repaired on use so any values still give a working cycle.</summary>
    public readonly struct Settings
    {
        /// <summary>The hour the sun rises (daylight half up).</summary>
        public readonly float SunriseHour;

        /// <summary>The hour the sun sets (daylight half down).</summary>
        public readonly float SunsetHour;

        /// <summary>How long dawn and dusk each last (hours), centred on sunrise and sunset.</summary>
        public readonly float TwilightHours;

        /// <summary>The daylight below which an interior fixture starts to switch on (0..1).</summary>
        public readonly float FixturesOnBelow;

        /// <summary>How much further the daylight falls while a fixture comes fully on (0..1).</summary>
        public readonly float FixtureFadeBand;

        /// <summary>In-game minutes between one fixture switching and the next.</summary>
        public readonly float FixtureStaggerMinutes;

        /// <summary>Creates the knobs.</summary>
        public Settings(float sunriseHour, float sunsetHour, float twilightHours, float fixturesOnBelow, float fixtureFadeBand, float fixtureStaggerMinutes)
        {
            SunriseHour = sunriseHour;
            SunsetHour = sunsetHour;
            TwilightHours = twilightHours;
            FixturesOnBelow = fixturesOnBelow;
            FixtureFadeBand = fixtureFadeBand;
            FixtureStaggerMinutes = fixtureStaggerMinutes;
        }
    }

    /// <summary>The hour of the day (0 to 24) for a clock minute, wrapped into the day; a minute that is not a number reads as noon.</summary>
    public static float Hour(float minuteOfDay) => float.IsNaN(minuteOfDay) || float.IsInfinity(minuteOfDay) ? 12f : Wrap(minuteOfDay / 60f);

    /// <summary>How much daylight the hall has at <paramref name="hour"/>: 1 by day, 0 at night, eased through dawn and dusk (half at sunrise and at sunset).</summary>
    public static float Daylight(float hour, in Settings s)
    {
        Day(s, out float length, out float twilight);
        float v = SinceSunrise(hour, s, length);
        float half = twilight * 0.5f;
        return Smooth(-half, half, v) * (1f - Smooth(length - half, length + half, v));
    }

    /// <summary>The complement of the daylight: the art presentation's evening (AnimeHallPresentation.SetTime), 0 by day to 1 at night.</summary>
    public static float Evening(float hour, in Settings s) => 1f - Daylight(hour, s);

    /// <summary>
    /// Where <paramref name="hour"/> stands on a canonical solar day, 0 to 1:
    /// solar midnight 0, sunrise 0.25, solar noon 0.5, sunset 0.75, linear
    /// between (the day and the night each stretched to half the cycle).
    /// </summary>
    public static float SolarPosition(float hour, in Settings s)
    {
        Day(s, out float length, out _);
        float v = SinceSunrise(hour, s, length);
        float night = HoursPerDay - length;
        float position = v < 0f ? 0.25f + 0.25f * v / (night * 0.5f)
                       : v <= length ? 0.25f + 0.5f * v / length
                       : 0.75f + 0.25f * (v - length) / (night * 0.5f);
        position -= (float)Math.Floor(position);
        return position;
    }

    /// <summary>How far the sun has gone along its arc: 0 at sunrise, 1 at sunset; 0 before dawn and 1 after dusk (the night holds the ends).</summary>
    public static float SunArc(float hour, in Settings s)
    {
        Day(s, out float length, out _);
        return Clamp01(SinceSunrise(hour, s, length) / length);
    }

    /// <summary>
    /// How far interior fixture <paramref name="index"/> (0 first) is switched
    /// on at <paramref name="hour"/>, 0 to 1: it reads the daylight the
    /// stagger times its index earlier, and comes on as that falls from the
    /// knob's threshold through its fade band (so the fixtures come on one
    /// after another at dusk and go off in the same order at dawn).
    /// </summary>
    public static float FixtureLevel(float hour, int index, in Settings s)
    {
        float stagger = Math.Max(0f, s.FixtureStaggerMinutes) / 60f * Math.Max(0, index);
        float daylight = Daylight(hour - stagger, s);
        float threshold = Clamp01(s.FixturesOnBelow);
        float band = Math.Max(0.01f, s.FixtureFadeBand);
        return Clamp01((threshold - daylight) / band);
    }

    /// <summary>
    /// A fixture's brightness share <paramref name="secondsSinceOn"/> after it
    /// started to switch on: a short strike (blinks for FlickerSeconds), then
    /// steady 1; always 1 before a switch-on (a negative time) and with <paramref name="reduced"/> motion.
    /// </summary>
    public static float Flicker(float secondsSinceOn, bool reduced)
    {
        if (reduced || !(secondsSinceOn >= 0f) || secondsSinceOn >= FlickerSeconds)
            return 1f;
        if (secondsSinceOn < 0.06f) return 1f;
        if (secondsSinceOn < 0.16f) return 0.15f;
        if (secondsSinceOn < 0.24f) return 1f;
        if (secondsSinceOn < 0.38f) return 0.15f;
        if (secondsSinceOn < 0.44f) return 0.7f;
        return 1f;
    }

    /// <summary>The day's length and the twilight, repaired: a day of 0.5 to 23.5 hours (12 when sunrise and sunset coincide), a twilight no longer than the day or the night.</summary>
    private static void Day(in Settings s, out float length, out float twilight)
    {
        length = Wrap(s.SunsetHour - s.SunriseHour);
        if (length <= 0f)
            length = 12f;
        length = Math.Min(23.5f, Math.Max(0.5f, length));
        float most = Math.Min(length, HoursPerDay - length);
        twilight = float.IsNaN(s.TwilightHours) ? 0.01f : Math.Min(most, Math.Max(0.01f, s.TwilightHours));
    }

    /// <summary>Hours since sunrise, from half the night before sunrise to half the night after sunset.</summary>
    private static float SinceSunrise(float hour, in Settings s, float length)
    {
        float u = Wrap(hour - s.SunriseHour);
        float nightMiddle = length + (HoursPerDay - length) * 0.5f;
        return u < nightMiddle ? u : u - HoursPerDay;
    }

    /// <summary><paramref name="hours"/> wrapped into 0 to 24 (a value that is not a number reads as 0).</summary>
    private static float Wrap(float hours)
    {
        if (float.IsNaN(hours) || float.IsInfinity(hours))
            return 0f;
        float w = hours % HoursPerDay;
        return w < 0f ? w + HoursPerDay : w;
    }

    private static float Smooth(float from, float to, float x)
    {
        float t = Clamp01((x - from) / (to - from));
        return t * t * (3f - 2f * t);
    }

    private static float Clamp01(float x) => x < 0f ? 0f : x > 1f ? 1f : float.IsNaN(x) ? 0f : x;
}
