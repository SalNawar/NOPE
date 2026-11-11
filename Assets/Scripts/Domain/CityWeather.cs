/// <summary>
/// The day's weather on the city (CITY-WEATHER, Saleh 2026-11-09: "did you make the city at different times of day
/// different weather?"): one state a day, Clear or Rain, drawn from the day's seed against
/// GameConfigSO.rainChance. The cheat state (DevToolsState.ForcedRain, the QA rig's weather op) overrides it. Pure.
/// </summary>
public static class CityWeather
{
    /// <summary>True when the day rains: <paramref name="forced"/> when set, else the day's draw is under <paramref name="rainChance"/> (0: never, 1: always).</summary>
    public static bool Rains(int daySeed, float rainChance, bool? forced)
    {
        if (forced.HasValue)
            return forced.Value;
        double roll = (uint)Seeds.ForWeather(daySeed) / 4294967296.0;
        return roll < rainChance;
    }

    /// <summary>The rain streak's slant: it leans 1 px across for every this many px down (the tile's streaks and the fall share it).</summary>
    public const int RainSlantRun = 5;

    /// <summary>
    /// The source window's corner (x, y: px, top-left origin) in the rain tile at a time. The window moves up the tile, so
    /// the streaks fall down the screen, and right by 1/<see cref="RainSlantRun"/> of that, so each streak slides along its
    /// own slant, lower end first. It repeats every tile * slant px of fall (the tile's own period in both axes); 0 under Reduced Motion.
    /// </summary>
    public static (float x, float y) RainSource(double seconds, float speed, int tile, bool reduced)
    {
        if (reduced)
            return (0f, 0f);
        double period = (double)tile * RainSlantRun, s = seconds * speed % period;
        return ((float)(s / RainSlantRun), (float)((period - s) % period));
    }
}
