using System.Collections.Generic;
using System.Linq;

/// <summary>
/// What each day introduces (Papers Please lesson 4, "one new rule or check
/// per day, announced in the morning briefing"; lesson D7): the papers the
/// travellers carry for the first time and the directives listed for the
/// first time, measured against every earlier day. A paper counts by its
/// request id (FormRequests.IdOf: the three proofs of means are one paper);
/// the closures of every kind count once, on the first day any is listed
/// (a closure changes daily, the check does not), while a closure for some
/// kinds (the Economy range limit) is a check of its own. A day may bring
/// at most one new paper and one new directive (a paper and the procedure
/// that asks for it arrive together), and a day that brings anything names
/// it in its bulletin (world_source.json days[].bulletin), the first line of
/// the morning briefing. Generate World and Validate Content Library refuse
/// a day that breaks this, in the same words. Pure; tested headless.
/// </summary>
public static class DayPacing
{
    /// <summary>The key every closure of every kind shares: the closures are one check.</summary>
    public const string Closures = "closures";

    /// <summary>The most new papers, and the most new directives, a day may bring.</summary>
    public const int MaxNewPerDay = 1;

    /// <summary>A paper's pacing key: its request id (a group's id: the proofs of means are one paper).</summary>
    public static string PaperKey(string askGroup, string formNumber) => FormRequests.IdOf(askGroup, formNumber);

    /// <summary>A directive's pacing key: <see cref="Closures"/> for a closure of every kind and for the open destinations (whatever kinds they list: the day's open places change, the check does not), else its asset (a procedure, or a closure listing kinds).</summary>
    public static string RuleKey(string asset, bool closure, bool listsKinds, bool openDestinations = false) => openDestinations || (closure && !listsKinds) ? Closures : asset;

    /// <summary>
    /// The new keys of each day, in day order: item i holds the keys of
    /// <paramref name="days"/>[i] that no earlier day had, each once, in
    /// their order (null days and keys count as none).
    /// </summary>
    public static List<List<string>> NewByDay(IReadOnlyList<IEnumerable<string>> days)
    {
        var result = new List<List<string>>();
        var seen = new HashSet<string>();
        foreach (IEnumerable<string> day in days ?? new List<IEnumerable<string>>())
        {
            var fresh = new List<string>();
            foreach (string key in day ?? Enumerable.Empty<string>())
                if (key != null && seen.Add(key))
                    fresh.Add(key);
            result.Add(fresh);
        }
        return result;
    }

    /// <summary>
    /// One message per pacing problem of a day: more than <see cref="MaxNewPerDay"/>
    /// new papers, more than <see cref="MaxNewPerDay"/> new directives, and
    /// something new with a blank <paramref name="bulletin"/>. A bulletin on
    /// a day with nothing new is allowed (a notice). The run's
    /// <paramref name="firstDay"/> sets the baseline (the desk-first ramp:
    /// the passport, the one open destination and its dates), so only its
    /// bulletin is checked.
    /// </summary>
    public static List<string> Problems(string asset, IReadOnlyCollection<string> newPapers, IReadOnlyCollection<string> newRules, string bulletin, bool firstDay = false)
    {
        var problems = new List<string>();
        newPapers = newPapers ?? new string[0];
        newRules = newRules ?? new string[0];
        if (firstDay)
        {
            if ((newPapers.Count > 0 || newRules.Count > 0) && string.IsNullOrWhiteSpace(bulletin))
                problems.Add($"Day '{asset}' is the first day but has no \"bulletin\": the morning briefing names what the desk checks.");
            return problems;
        }
        if (newPapers.Count > MaxNewPerDay)
            problems.Add($"Day '{asset}' brings {newPapers.Count} new papers ({string.Join(", ", newPapers)}); a day brings at most {MaxNewPerDay} (stage them over days in days[].papers).");
        if (newRules.Count > MaxNewPerDay)
            problems.Add($"Day '{asset}' lists {newRules.Count} new directives ({string.Join(", ", newRules)}); a day brings at most {MaxNewPerDay} new check.");
        if ((newPapers.Count > 0 || newRules.Count > 0) && string.IsNullOrWhiteSpace(bulletin))
            problems.Add($"Day '{asset}' brings {string.Join(", ", newPapers.Concat(newRules))} but has no \"bulletin\": the morning briefing names each day's new check in one line.");
        return problems;
    }
}
