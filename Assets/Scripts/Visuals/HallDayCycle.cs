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

    /// <summary>
    /// The cycle's default knobs, the one source of them: HallLightingSO's
    /// field defaults read these, and <see cref="Active"/> starts from them
    /// (sunrise 07:00, sunset 16:30, 1.5 h twilights, fixtures on below 0.6
    /// daylight over a 0.1 band, 4 minutes apart). The shipped asset
    /// (HallLighting_Default) tunes its own values (fixtures on below 0.8);
    /// they reach every reader once the rig publishes them, each frame.
    /// </summary>
    public static readonly Settings Defaults = new Settings(7f, 16.5f, 1.5f, 0.6f, 0.1f, 4f);

    /// <summary>
    /// The cycle's knobs every reader of the one day cycle shares (BUG-25b):
    /// the hall rig publishes its HallLightingSO's (<see cref="Publish"/>), so
    /// the hall's four painted states (HallBakedCycle), the lamps, the city's
    /// windows, the calendar and the desk light all read the same day. Until
    /// a rig publishes (a scene without the hall): <see cref="Defaults"/>.
    /// </summary>
    public static Settings Active { get; private set; } = Defaults;

    /// <summary>Makes <paramref name="settings"/> the cycle every reader shares (HallLightingRig, from its knobs, each frame).</summary>
    public static void Publish(in Settings settings) => Active = settings;

    /// <summary>
    /// The hour the cheat menu's Dusk sets (P0-A seam; owner LIGHT, read by
    /// RUN-SAFE's DevCheats): 18:30, the plan's H4 and the value the seam was
    /// stubbed with, so the LIGHT pass compares cheat Dusk with a natural
    /// 18:30 (FIX_PLAN LIGHT). On the one cycle with the shipped knobs 18:30
    /// is already full night; moving it to the cycle's sunset
    /// (<see cref="HourAtEvening"/> of 0.5, 16:30) is a proposal waiting
    /// for the orchestrator (docs/pending/LIGHT.md), not done here.
    /// </summary>
    public static float DuskHour => 18.5f;

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
    /// The afternoon hour (0 to 24) at which the evening reaches
    /// <paramref name="evening"/> (clamped to 0..1): the inverse of
    /// <see cref="Evening"/> through the dusk, from the dusk's start (0) to
    /// night (1), the sunset at a half. The desk lamp's switch-on hour
    /// (DeskLighting.LampOnHour) reads it.
    /// </summary>
    public static float HourAtEvening(float evening, in Settings s)
    {
        Day(s, out float length, out float twilight);
        double e = Clamp01(evening);
        // Through the dusk the evening is a smoothstep of the hours since its start; this is its inverse.
        double x = 0.5 - Math.Sin(Math.Asin(1.0 - 2.0 * e) / 3.0);
        return Wrap(s.SunriseHour + (length - twilight * 0.5f) + (float)x * twilight);
    }

    /// <summary>
    /// The hour the hall shows (BUG-25c: never noon before the clock exists):
    /// the cheat menu's forced hour, else the preview hour while it is on,
    /// else the shift clock's minute, else (in play, before the clock exists)
    /// today's opening minute, else the edit-mode hour. A null minute or
    /// forced hour is one that does not exist yet.
    /// </summary>
    public static float ShownHour(float? forcedHour, bool previewOn, float previewHour, float? clockMinute, float? openingMinute, float editModeHour)
    {
        if (forcedHour.HasValue)
            return forcedHour.Value;
        if (previewOn)
            return previewHour;
        if (clockMinute.HasValue)
            return Hour(clockMinute.Value);
        return openingMinute.HasValue ? Hour(openingMinute.Value) : editModeHour;
    }

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

    /// <summary>
    /// True when a light of a night stage (Saleh's demo 1018a: "need more
    /// lights to activate as it gets darker") is switched on at
    /// <paramref name="hour"/>: the hall's evening (<see cref="Evening"/>,
    /// 0 by day to 1 at night) read <paramref name="staggerMinutes"/> times
    /// its <paramref name="index"/> earlier has reached the stage's
    /// <paramref name="onAtEvening"/>, so the lights of one stage come on one
    /// after another as it darkens, and go off in the same order as it lightens.
    /// A stage at 0 or less is always on; one above 1 never.
    /// </summary>
    public static bool SwitchedOn(float hour, float onAtEvening, int index, float staggerMinutes, in Settings s)
    {
        if (!(onAtEvening > 0f))
            return true;
        float stagger = Math.Max(0f, staggerMinutes) / 60f * Math.Max(0, index);
        return Evening(hour - stagger, s) >= onAtEvening;
    }

    /// <summary>How long a lamp's warm-up blink lasts before it eases up (real seconds).</summary>
    public const float WarmBlinkSeconds = 0.08f;

    /// <summary>
    /// A filament lamp's share <paramref name="secondsSinceOn"/> after it was
    /// switched on (the desk lamp): a dim blink, then easing up to full over
    /// <paramref name="warmSeconds"/>; 0 while off (a negative time), full
    /// with <paramref name="reduced"/> motion or once warm.
    /// </summary>
    public static float WarmUp(float secondsSinceOn, float warmSeconds, bool reduced)
    {
        if (!(secondsSinceOn >= 0f))
            return 0f;
        if (reduced || secondsSinceOn >= warmSeconds)
            return 1f;
        if (secondsSinceOn < WarmBlinkSeconds)
            return 0.35f;
        float t = (secondsSinceOn - WarmBlinkSeconds) / Math.Max(1e-3f, warmSeconds - WarmBlinkSeconds);
        return 0.2f + 0.8f * t * t * (3f - 2f * t);
    }

    /// <summary>How long a sign's neon stutter lasts (real seconds).</summary>
    public const float NeonSeconds = 0.5f;

    /// <summary>
    /// A lit sign's share <paramref name="secondsSinceOn"/> after it was
    /// switched on: a neon stutter (on, off, on, off, on) for
    /// <see cref="NeonSeconds"/>, then steady; 0 while off (a negative time),
    /// steady at once with <paramref name="reduced"/> motion.
    /// </summary>
    public static float Neon(float secondsSinceOn, bool reduced)
    {
        if (!(secondsSinceOn >= 0f))
            return 0f;
        if (reduced || secondsSinceOn >= NeonSeconds)
            return 1f;
        if (secondsSinceOn < 0.05f) return 0.8f;
        if (secondsSinceOn < 0.12f) return 0f;
        if (secondsSinceOn < 0.2f) return 0.9f;
        if (secondsSinceOn < 0.32f) return 0.1f;
        if (secondsSinceOn < 0.4f) return 1f;
        return 0.3f;
    }

    /// <summary>The peak of a portal's night surge, over its night share.</summary>
    public const float SurgePeak = 1.4f;

    /// <summary>
    /// A portal's night share <paramref name="secondsSinceOn"/> after its night
    /// stage came (the vortex brightens): it surges to <see cref="SurgePeak"/>
    /// over the first 40 % of <paramref name="surgeSeconds"/> and settles to 1;
    /// 0 while off (a negative time), 1 at once with <paramref name="reduced"/> motion.
    /// </summary>
    public static float Surge(float secondsSinceOn, float surgeSeconds, bool reduced)
    {
        if (!(secondsSinceOn >= 0f))
            return 0f;
        if (reduced || surgeSeconds <= 0f || secondsSinceOn >= surgeSeconds)
            return 1f;
        float t = secondsSinceOn / surgeSeconds;
        return t < 0.4f ? SurgePeak * t / 0.4f : SurgePeak - (SurgePeak - 1f) * (t - 0.4f) / 0.6f;
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
