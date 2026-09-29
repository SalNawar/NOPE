using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// How a world factor is answered today (world_source.json
/// world.factors[].answer). Serialized in WorldFactor.answer: append only
/// (SerializedEnumsTests pins every value). The endings spec's phase E1 adds
/// the answer read from the factor's outcomes and pulls.
/// </summary>
public enum FactorAnswer
{
    /// <summary>As the run found it: the factor's <see cref="WorldFactor.foundAs"/> text (no state moves it yet).</summary>
    AsFound,

    /// <summary>The timeline's leader (HistoryState.leaderId): the leading nation's name, else <see cref="WorldFactor.foundAs"/> while none leads.</summary>
    Leader
}

/// <summary>One question the end of the demo answers about 2150 ("Who runs 2150?"), a row of world_source.json world.factors.</summary>
[Serializable]
public sealed class WorldFactor
{
    /// <summary>Stable id ("government", "future", "money", "culture"); the endings spec's outcomes name their factor by it.</summary>
    public string id;

    /// <summary>The question as the player reads it ("Who runs 2150?").</summary>
    public string question;

    /// <summary>How it is answered today.</summary>
    public FactorAnswer answer;

    /// <summary>The answer as the run found 2150 ("The Directorate"); also the Leader answer while no nation leads.</summary>
    public string foundAs;
}

/// <summary>The world block of the content (world_source.json "world", written by Generate World into ContentLibrarySO.World).</summary>
[Serializable]
public sealed class WorldContent
{
    /// <summary>The factors in the order the end of the demo lists them.</summary>
    public List<WorldFactor> factors = new();

    /// <summary>What Generate World and the validator refuse: no factor, a blank id, question or found-as answer, and an id used twice. Empty when sound.</summary>
    public List<string> Problems()
    {
        var problems = new List<string>();
        if (factors == null || factors.Count == 0)
        {
            problems.Add("world.factors is empty: the end of the demo lists the world's outcomes from it.");
            return problems;
        }
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < factors.Count; i++)
        {
            WorldFactor f = factors[i];
            if (f == null || string.IsNullOrWhiteSpace(f.id))
            {
                problems.Add($"world.factors[{i}] has no id.");
                continue;
            }
            if (!seen.Add(f.id))
                problems.Add($"world.factors '{f.id}' is listed twice.");
            if (string.IsNullOrWhiteSpace(f.question))
                problems.Add($"world.factors '{f.id}' has no question.");
            if (string.IsNullOrWhiteSpace(f.foundAs))
                problems.Add($"world.factors '{f.id}' has no foundAs: the answer as the run found 2150.");
        }
        return problems;
    }
}

/// <summary>One line of the world's outcomes: a factor's question and its answer now.</summary>
public readonly struct OutcomeLine
{
    /// <summary>A line of <paramref name="factorId"/>'s question and answer.</summary>
    public OutcomeLine(string factorId, string question, string answer)
    {
        FactorId = factorId;
        Question = question;
        Answer = answer;
    }

    /// <summary>The factor's id.</summary>
    public string FactorId { get; }

    /// <summary>The question ("Who runs 2150?").</summary>
    public string Question { get; }

    /// <summary>The answer ("The Directorate", "Japan").</summary>
    public string Answer { get; }
}

/// <summary>
/// The world's outcomes at the end of the demo (Saleh, 2026-09-29: "for now
/// no ending, just end demo with those 4 outcomes"; the endings spec's phase
/// E0): each factor's question and its answer, listed plainly, never ranked,
/// scored or judged. Shown on the run's last day under END OF DEMO, and after
/// a failure as "the world you leave behind". Today only the culture moves
/// (the timeline's leader); the other factors read as the run found 2150,
/// from content text, until phase E1 answers them from their outcomes. Pure,
/// so it is tested headless.
/// </summary>
public static class WorldFactors
{
    /// <summary>The UI string key of the page a failure ending opens ("The world you leave behind"): its button and its heading (world_source.json ui.strings; Generate World and the validator require it).</summary>
    public const string LeftBehindKey = "ending.world.leftBehind";

    /// <summary>
    /// The outcome lines, in content order: an AsFound factor answers its
    /// foundAs; a Leader factor the leading nation's name
    /// (<paramref name="leaderName"/>), or its foundAs when none leads (null
    /// or blank). Null factors and factors without an id are skipped.
    /// </summary>
    public static List<OutcomeLine> Lines(IEnumerable<WorldFactor> factors, string leaderName) =>
        (factors ?? Enumerable.Empty<WorldFactor>())
            .Where(f => f != null && !string.IsNullOrWhiteSpace(f.id))
            .Select(f => new OutcomeLine(f.id, f.question, Answer(f, leaderName)))
            .ToList();

    /// <summary>A factor's answer now (see <see cref="Lines"/>).</summary>
    private static string Answer(WorldFactor f, string leaderName) =>
        f.answer == FactorAnswer.Leader && !string.IsNullOrWhiteSpace(leaderName) ? leaderName : f.foundAs;
}
