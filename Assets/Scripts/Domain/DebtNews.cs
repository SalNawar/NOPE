using System;
using System.Collections.Generic;

/// <summary>
/// The morning paper's debt-theme lines as authored (world_source.json
/// "news"; the traveller-types spec's §10, T1; redesign phase 13), written by
/// Generate World into the content library. The strandings' line
/// ("news.stranded") joins this block with the strandings.
/// </summary>
[Serializable]
public sealed class NewsContent
{
    /// <summary>The debt-economy lines, one of which each morning's paper carries ("news.debt"; DebtNews.Line). Empty prints none.</summary>
    public List<string> debt = new();

    /// <summary>What Generate World and the validator refuse: a blank debt line. Empty when sound.</summary>
    public List<string> Problems()
    {
        var problems = new List<string>();
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
