using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// The present, 2150 (traveller types H1): where 2150 citizens come from and
/// the office's own time. Its facts (history applied), its label, its year
/// and its citizens' birth years. It is never a destination; its row is in
/// every book from day 1 (<see cref="Present.AddRow"/>).
/// </summary>
public sealed class PresentPlace
{
    /// <summary>A present of <paramref name="nationId"/> in <paramref name="eraId"/> with <paramref name="facts"/> (copied, in order; blank values skipped) and its clothes (<paramref name="wardrobe"/>; null for none).</summary>
    public PresentPlace(string nationId, string eraId, string label, int year, int birthYearMin, int birthYearMax,
                        IEnumerable<KeyValuePair<ClueCategory, string>> facts, PlaceWardrobe wardrobe = null)
    {
        Wardrobe = wardrobe;
        NationId = nationId;
        EraId = eraId;
        Label = label;
        Year = year;
        BirthYearMin = birthYearMin;
        BirthYearMax = birthYearMax;
        Facts = (facts ?? Enumerable.Empty<KeyValuePair<ClueCategory, string>>())
            .Where(f => !string.IsNullOrWhiteSpace(f.Value))
            .ToArray();
    }

    /// <summary>The nation id its facts are keyed by: the leader's, or <see cref="Present.NeutralNationId"/>.</summary>
    public string NationId { get; }

    /// <summary>The era id its facts are keyed by (the office's own era, "future").</summary>
    public string EraId { get; }

    /// <summary>Its label, as the books print it ("Temporal Customs Zone (Future)").</summary>
    public string Label { get; }

    /// <summary>The present's year (2150): a 2150 citizen's age is counted from it.</summary>
    public int Year { get; }

    /// <summary>The earliest birth year of a 2150 citizen (2080).</summary>
    public int BirthYearMin { get; }

    /// <summary>The latest birth year of a 2150 citizen (2132).</summary>
    public int BirthYearMax { get; }

    /// <summary>Its facts, one per category, in authored order.</summary>
    public IReadOnlyList<KeyValuePair<ClueCategory, string>> Facts { get; }

    /// <summary>What its people wear (a leader's Future outfit, or the neutral present's clothes): the 2150 clothes of a costume error; null when none is authored.</summary>
    public PlaceWardrobe Wardrobe { get; }

    /// <summary>True when no nation leads, so this is the authored neutral present.</summary>
    public bool IsNeutral => NationId == Present.NeutralNationId;

    /// <summary>Its fact of <paramref name="category"/>, or null when it has none.</summary>
    public string Fact(ClueCategory category)
    {
        foreach (KeyValuePair<ClueCategory, string> f in Facts)
            if (f.Key == category)
                return f.Value;
        return null;
    }
}

/// <summary>
/// The present's rules (traveller types H1): which place it is, and its row
/// in today's facts. Pure, so both are tested headless.
/// </summary>
public static class Present
{
    /// <summary>The neutral present's nation token ("neutral", as the neutral theme and the art's neutral items).</summary>
    public const string NeutralNationId = "neutral";

    /// <summary>
    /// The present: the leader's Future place (the one of
    /// <paramref name="futurePlaces"/> whose nation is
    /// <paramref name="leaderNationId"/>) while a nation leads history;
    /// otherwise, or when the leader has no Future place, the
    /// <paramref name="neutral"/> present authored in world_source.json.
    /// </summary>
    public static PresentPlace Choose(string leaderNationId, IReadOnlyList<PresentPlace> futurePlaces, PresentPlace neutral)
    {
        if (string.IsNullOrWhiteSpace(leaderNationId) || futurePlaces == null)
            return neutral;

        foreach (PresentPlace p in futurePlaces)
            if (p != null && string.Equals(p.NationId, leaderNationId, StringComparison.Ordinal))
                return p;

        return neutral;
    }

    /// <summary>
    /// Adds the present's facts to today's table, after today's places, so
    /// every book lists it from day 1. A place the table already lists (a
    /// leader's Future place that is a destination today) is not added again.
    /// True when the rows were added.
    /// </summary>
    public static bool AddRow(FactTable table, PresentPlace present)
    {
        if (table == null || present == null || table.HasPlace(present.NationId, present.EraId))
            return false;

        foreach (KeyValuePair<ClueCategory, string> f in present.Facts)
            table.Add(present.NationId, present.EraId, present.Label, f.Key, f.Value);
        return true;
    }
}
