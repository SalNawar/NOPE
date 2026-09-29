using System;
using System.Collections.Generic;

/// <summary>One day plan's identity and size, as Generate World reads it from the source and Validate Content Library from the asset.</summary>
public readonly struct DayPlanEntry
{
    /// <summary>An entry for the plan named <paramref name="asset"/>.</summary>
    public DayPlanEntry(string asset, int day, int queue)
    {
        Asset = asset;
        Day = day;
        Queue = queue;
    }

    /// <summary>The plan's asset name, which is its file name ("DayPlan_Inv_Day1").</summary>
    public string Asset { get; }

    /// <summary>The day the plan is for (days start at 1).</summary>
    public int Day { get; }

    /// <summary>How many travellers the day queues.</summary>
    public int Queue { get; }
}

/// <summary>Which authored day plan a day uses, and whether the plans are sound; pure so both are tested headless.</summary>
public static class DayPlans
{
    /// <summary>
    /// Which day plan a day uses: its own, else the latest earlier one. The
    /// index of the first entry equal to <paramref name="day"/>; otherwise of
    /// the largest entry below it (the first such entry on a repeat);
    /// otherwise -1 (also for a null or empty list).
    /// </summary>
    public static int Pick(IReadOnlyList<int> dayNumbers, int day)
    {
        if (dayNumbers == null)
            return -1;

        int best = -1;
        for (int i = 0; i < dayNumbers.Count; i++)
        {
            if (dayNumbers[i] == day)
                return i;
            if (dayNumbers[i] < day && (best < 0 || dayNumbers[i] > dayNumbers[best]))
                best = i;
        }

        return best;
    }

    /// <summary>
    /// One message per problem in the day plans' identities and sizes (audit
    /// R6-001: a repeated asset name made one plan overwrite another, and a
    /// missing queue wrote a day of no travellers): a blank asset name; an
    /// asset name used twice (ignoring case: it is a file name); a day below
    /// 1; a day planned twice (once per day); a queue below 1. Generate World
    /// checks the source with it before writing anything and Validate Content
    /// Library checks the assets, in the same words. A null list has none.
    /// </summary>
    public static List<string> Problems(IReadOnlyList<DayPlanEntry> plans)
    {
        var problems = new List<string>();
        var assets = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var days = new HashSet<int>();
        var reported = new HashSet<int>();
        for (int i = 0; plans != null && i < plans.Count; i++)
        {
            DayPlanEntry p = plans[i];
            if (string.IsNullOrWhiteSpace(p.Asset))
                problems.Add($"Day plan for day {p.Day} has a blank asset name.");
            else if (assets.TryGetValue(p.Asset.Trim(), out int firstDay))
                problems.Add($"Day plan asset '{p.Asset}' is used twice (days {firstDay} and {p.Day}).");
            else
                assets.Add(p.Asset.Trim(), p.Day);

            if (p.Day < 1)
                problems.Add($"Day plan '{p.Asset}' is for day {p.Day}; days start at 1.");
            else if (!days.Add(p.Day) && reported.Add(p.Day))
                problems.Add($"Duplicate DayPlan for day {p.Day} (asset '{p.Asset}').");

            if (p.Queue < 1)
                problems.Add($"Day plan '{p.Asset}' queues {p.Queue} travellers; a day needs at least 1.");
        }
        return problems;
    }

    /// <summary>
    /// The unplanned tail (days 7-15 V1): one warning when the days after the
    /// latest planned one, up to <paramref name="lastDay"/> (the run's last
    /// day, the Retirement milestone's DayAtLeast threshold), have no plan of
    /// their own and would replay the latest's (Pick). None with every day
    /// planned, with no milestone (0), or with no plan at all (Gaps names
    /// that).
    /// </summary>
    public static List<string> Unplanned(IEnumerable<int> dayNumbers, int lastDay)
    {
        var warnings = new List<string>();
        int latest = 0;
        foreach (int day in dayNumbers ?? new int[0])
            if (day >= 1 && day <= lastDay && day > latest)
                latest = day;
        if (latest > 0 && latest < lastDay)
            warnings.Add($"Days {latest + 1}..{lastDay} have no plan of their own and replay day {latest}'s (the run ends after day {lastDay}, the Retirement milestone). Author their days[] or move the milestone.");
        return warnings;
    }

    /// <summary>
    /// One message per hole in the day sequence (audit R6-022): the plans
    /// not starting at day 1 (RunConfig.startingDay is 1, so the run would
    /// open on a day with no plan of its own), and each gap between two
    /// planned days (the days between reuse the earlier plan, Pick, which is
    /// rarely meant). Days below 1 are Problems' (ignored here); repeats are
    /// no gap. A null or empty list has none.
    /// </summary>
    public static List<string> Gaps(IEnumerable<int> dayNumbers)
    {
        var gaps = new List<string>();
        if (dayNumbers == null)
            return gaps;

        var days = new SortedSet<int>();
        foreach (int day in dayNumbers)
            if (day >= 1)
                days.Add(day);
        if (days.Count == 0)
            return gaps;

        int previous = 0;
        foreach (int day in days)
        {
            if (previous == 0 && day != 1)
                gaps.Add($"Day plans start at day {day}, not day 1 (RunConfig.startingDay): day(s) 1..{day - 1} have no plan.");
            else if (previous > 0 && day != previous + 1)
                gaps.Add($"DayPlans gap: day {previous} is followed by day {day} (day(s) {previous + 1}..{day - 1} have no plan).");
            previous = day;
        }

        return gaps;
    }
}
