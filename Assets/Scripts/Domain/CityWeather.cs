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

    /// <summary>
    /// The source y (top-left origin, px) of the rain tile at a time. It falls as time grows, so the streaks
    /// move down the screen (the source window moves up the tile); 0 under Reduced Motion.
    /// </summary>
    public static float RainSourceY(double seconds, float speed, int tile, bool reduced)
    {
        if (reduced)
            return 0f;
        double y = -seconds * speed % tile;
        return (float)(y < 0 ? y + tile : y);
    }
}
