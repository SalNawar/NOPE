using System;
using System.Collections.Generic;

/// <summary>
/// The morning paper's debt-theme lines as authored (world_source.json
/// "news"; the traveller-types spec's §10, T1; redesign phase 13), written by
/// Generate World into the content library: the debt lines and the
/// strandings' line ("news.stranded": the paper's line of a stranding fate
/// with no lines of its own, StrandingFates.Line).
/// </summary>
[Serializable]
public sealed class NewsContent
{
    /// <summary>The debt-economy lines, one of which each morning's paper carries ("news.debt"; DebtNews.Line). Empty prints none.</summary>
    public List<string> debt = new();

    /// <summary>The line the morning paper prints for a traveller stranded the day before whose fate has no lines of its own (a carry of 2150 technology; "news.stranded"; tokens {name} and {place}; StrandingFates.Line).</summary>
    public string stranded = string.Empty;

    /// <summary>The line the morning paper prints when the last shift approved Debt Relief departures ("news.debtReliefCount"; the token {count}; DebtNews.YesterdayLine).</summary>
    public string debtReliefCount = string.Empty;

    /// <summary>The desk section's line for a traveller turned away who came back and was let through ("news.returnedAccepted"; {name}, {day} the first denial, {back} the return; Returns.Lines; wave 5, lesson 9).</summary>
    public string returnedAccepted = string.Empty;

    /// <summary>The desk section's line for a traveller turned away who came back and was turned away again ("news.returnedDenied"; {name}, {day}, {back}).</summary>
    public string returnedDenied = string.Empty;

    /// <summary>What Generate World and the validator refuse: a blank debt line, a stranding line that is blank or lacks {name} or {place}, a Debt Relief count line that is blank or lacks {count}, or a returned traveller's line that is blank or lacks {name} or {day}. Empty when sound.</summary>
    public List<string> Problems()
    {
        var problems = new List<string>();
        if (string.IsNullOrWhiteSpace(stranded))
            problems.Add("news.stranded is blank: the line the morning paper prints for each stranded traveller, with {name} and {place}.");
        else if (!Interview.HoldsToken(stranded, Interview.NameToken) || !Interview.HoldsToken(stranded, Interview.PlaceToken))
            problems.Add("news.stranded must hold {name} and {place}: the stranded traveller and where they are lost.");
        if (string.IsNullOrWhiteSpace(debtReliefCount))
            problems.Add("news.debtReliefCount is blank: the line the morning paper prints for yesterday's Debt Relief departures, with {count}.");
        else if (!Interview.HoldsToken(debtReliefCount, DebtNews.CountToken))
            problems.Add("news.debtReliefCount must hold {count}: how many citizens left on Debt Relief yesterday.");
        foreach ((string key, string line) in new[] { ("returnedAccepted", returnedAccepted), ("returnedDenied", returnedDenied) })
            if (string.IsNullOrWhiteSpace(line))
                problems.Add($"news.{key} is blank: the desk section's line for a traveller who came back after a denial, with {{name}} and {{day}}.");
            else if (!Interview.HoldsToken(line, Interview.NameToken) || !Interview.HoldsToken(line, Returns.DeniedDayToken))
                problems.Add($"news.{key} must hold {{name}} and {{day}}: who came back and the day they were first turned away.");
        if (debt == null)
            return problems;
        for (int i = 0; i < debt.Count; i++)
            if (string.IsNullOrWhiteSpace(debt[i]))
                problems.Add($"news.debt[{i}] is blank: each debt line is a sentence the morning paper prints.");
        return problems;
    }
}

/// <summary>
/// Which debt line the morning paper carries (the traveller-types spec's §10):
/// the pool in the run's own shuffled order (<see cref="Seeds.ForDebtNews"/>),
/// one line a day in turn, so no line repeats before the pool has run through.
/// Pure; its stream is apart from every other, so the paper never shifts who
/// travels.
/// </summary>
public static class DebtNews
{
    /// <summary>The token of the count line ("{count}").</summary>
    public const string CountToken = "count";

    /// <summary>
    /// The paper's count of yesterday's Debt Relief departures (§10: "43
    /// citizens left on Debt Relief yesterday."): <paramref name="template"/>
    /// (news.debtReliefCount) with {count} filled; null for a count of 0 or
    /// less (nothing to report) or a blank template.
    /// </summary>
    public static string YesterdayLine(string template, int count) =>
        count <= 0 || string.IsNullOrWhiteSpace(template) ? null : Interview.Fill(template, CountToken, count.ToString(System.Globalization.CultureInfo.InvariantCulture));

    /// <summary>The debt line of <paramref name="day"/>'s paper in run <paramref name="runSeed"/>, or null when the pool is empty.</summary>
    public static string Line(IReadOnlyList<string> pool, int runSeed, int day)
    {
        if (pool == null || pool.Count == 0)
            return null;

        var order = new int[pool.Count];
        for (int i = 0; i < order.Length; i++)
            order[i] = i;
        var rng = new SeededRandom(Seeds.ForDebtNews(runSeed));
        for (int i = order.Length - 1; i > 0; i--)
        {
            int j = rng.Range(0, i + 1);
            (order[i], order[j]) = (order[j], order[i]);
        }

        int at = ((day - 1) % order.Length + order.Length) % order.Length;
        return pool[order[at]];
    }
}
