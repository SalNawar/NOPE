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
}
