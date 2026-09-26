using System;
using System.Collections.Generic;

/// <summary>
/// The day ranges a displaced person's file is drawn from (world_source.json
/// agency.displaced, written by Generate World; traveller types §4.2-4.3).
/// </summary>
[Serializable]
public sealed class DisplacementRanges
{
    /// <summary>The person was found 1 to this many days before today (30: "a date in the last 30 days").</summary>
    public int foundWithinDays;

    /// <summary>The fewest days after today their certificate is valid (3).</summary>
    public int validDaysMin;

    /// <summary>The most days after today their certificate is valid (365).</summary>
    public int validDaysMax;
}

/// <summary>
/// A displaced person's agency file (traveller types §4.2): the numbers and
/// dates their registry entry and their forms print, drawn once per traveller.
/// </summary>
public sealed class DisplacementFile
{
    /// <summary>The Displacement No. ("DP-4471-02"), unique within the day: the category CitizenId.</summary>
    public string Number;

    /// <summary>The rift that took them ("R-0311-07": its month and day, then a serial): the category Incident.</summary>
    public string Incident;

    /// <summary>The day they were found, the incident's day ("11 Mar 2150"); the registry's Found row.</summary>
    public string Found;

    /// <summary>The Displacement Certificate's honest Valid Until ("27 Mar 2150"): the category Expiry.</summary>
    public string ValidUntil;
}

/// <summary>
/// The agency's number and date makers (traveller types R2, §4.3): every
/// value is drawn on the traveller's account stream (Seeds.ForAccount) in a
/// fixed order, so tuning a range never changes who travels, and every
/// number is unique within the day (like names), so a number never belongs
/// to two travellers. Dates are the 2150 calendar's, written as
/// BirthDates.Format writes dates. Pure, so every maker is tested headless.
/// </summary>
public static class AgencyNumbers
{
    /// <summary>How many draws <see cref="TakeUnique"/> makes before it keeps a taken value (a full number space must never hang generation).</summary>
    public const int MaxAttempts = 1000;

    /// <summary>
    /// A displaced person's file, in the fixed draw order: the number
    /// (<see cref="DisplacementNumber"/>, two draws, redrawn while taken
    /// today), the day they were found (<see cref="DaysAgo"/>, one draw; it is
    /// also the incident's day), the incident's serial
    /// (<see cref="IncidentNumber"/>, one draw), then the certificate's Valid
    /// Until (<see cref="DaysAhead"/>, one draw). The number joins
    /// <paramref name="takenToday"/>.
    /// </summary>
    public static DisplacementFile Displaced(DateTime today, DisplacementRanges ranges, ISet<string> takenToday, IRandomSource rng)
    {
        string number = TakeUnique(takenToday, () => DisplacementNumber(rng));
        DateTime found = DaysAgo(today, ranges.foundWithinDays, rng);
        string incident = IncidentNumber(found, rng);
        DateTime validUntil = DaysAhead(today, ranges.validDaysMin, ranges.validDaysMax, rng);
        return new DisplacementFile { Number = number, Incident = incident, Found = Date(found), ValidUntil = Date(validUntil) };
    }

    /// <summary>A Displacement No., "DP-nnnn-nn": two draws, 0000-9999 then 00-99.</summary>
    public static string DisplacementNumber(IRandomSource rng) =>
        $"DP-{rng.Range(0, 10000):D4}-{rng.Range(0, 100):D2}";

    /// <summary>An incident number, "R-mmdd-nn": the incident's month and day, then a serial 01-99 (one draw).</summary>
    public static string IncidentNumber(DateTime incidentDay, IRandomSource rng) =>
        $"R-{incidentDay.Month:D2}{incidentDay.Day:D2}-{rng.Range(1, 100):D2}";

    /// <summary>A day 1 to <paramref name="maxDays"/> (at least 1) days before <paramref name="today"/>: one draw.</summary>
    public static DateTime DaysAgo(DateTime today, int maxDays, IRandomSource rng) =>
        today.AddDays(-rng.Range(1, Math.Max(1, maxDays) + 1));

    /// <summary>A day <paramref name="minDays"/> to <paramref name="maxDays"/> (either order) days after <paramref name="today"/>: one draw.</summary>
    public static DateTime DaysAhead(DateTime today, int minDays, int maxDays, IRandomSource rng)
    {
        if (maxDays < minDays)
            (minDays, maxDays) = (maxDays, minDays);
        return today.AddDays(rng.Range(minDays, maxDays + 1));
    }

    /// <summary>A date as the papers and the records print it ("14 Mar 2150", BirthDates.Format).</summary>
    public static string Date(DateTime date) => BirthDates.Format(date.Day, date.Month - 1, date.Year);

    /// <summary>
    /// Draws until a value nobody has today, adds it to <paramref name="taken"/>
    /// and returns it; after <see cref="MaxAttempts"/> draws it keeps the last
    /// one (the space is then full).
    /// </summary>
    public static string TakeUnique(ISet<string> taken, Func<string> draw)
    {
        string value = draw();
        for (int attempt = 1; attempt < MaxAttempts && taken.Contains(value); attempt++)
            value = draw();
        taken.Add(value);
        return value;
    }
}
