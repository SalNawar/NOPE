using System;
using System.Collections.Generic;

/// <summary>
/// One fact about one place ("New Kingdom Egypt (Ancient)" pays in "Deben").
/// Rows are what the reference books list and what the scanner compares against.
/// </summary>
public readonly struct FactRow
{
    /// <summary>Nation id of the place (matches NationSO.id).</summary>
    public readonly string NationId;

    /// <summary>Era id of the place (matches EraSO.id).</summary>
    public readonly string EraId;

    /// <summary>Human label of the place, e.g. "Abbasid Baghdad (Medieval)".</summary>
    public readonly string OriginLabel;

    /// <summary>Which fact this is (Currency, Language, ...).</summary>
    public readonly ClueCategory Category;

    /// <summary>The fact's value, e.g. "Deben".</summary>
    public readonly string Value;

    /// <summary>Creates a row.</summary>
    public FactRow(string nationId, string eraId, string originLabel, ClueCategory category, string value)
    {
        NationId = nationId;
        EraId = eraId;
        OriginLabel = originLabel;
        Category = category;
        Value = value;
    }

    /// <summary>This row as scanner evidence (a clicked reference-book entry).</summary>
    public CompareEvidence ToEvidence() =>
        CompareEvidence.ForReferenceEntry(Category, Value, NationId, EraId, OriginLabel);
}

/// <summary>
/// Today's world facts, keyed by (nation id, era id, category): the single
/// lookup that case generation (field values, tells, the claim's origin
/// label) and the reference books read. Built once per day (a snapshot), so
/// papers and books always agree; Citizen Records carry the claimed place's
/// label the case took from here. Pure and string-keyed, so it is tested headless;
/// history changes what goes in (ContentLibrarySO.FillFacts), not how it is read.
/// </summary>
public sealed class FactTable
{
    /// <summary>The widest fact value a book row shows; every place fact, history value and derived Culture value fits (checked by Generate World and the content validator).</summary>
    public const int MaxValueLength = 28;

    /// <summary>Fact values per place and category.</summary>
    private readonly Dictionary<(string nation, string era, ClueCategory category), string> _values =
        new Dictionary<(string, string, ClueCategory), string>();

    /// <summary>Origin label per place.</summary>
    private readonly Dictionary<(string nation, string era), string> _labels =
        new Dictionary<(string, string), string>();

    /// <summary>Rows per category, in insertion order.</summary>
    private readonly Dictionary<ClueCategory, List<FactRow>> _rows = new Dictionary<ClueCategory, List<FactRow>>();

    /// <summary>Shared empty result.</summary>
    private static readonly IReadOnlyList<FactRow> NoRows = Array.Empty<FactRow>();

    /// <summary>
    /// Adds one fact. Blank values and repeats of an existing (place, category)
    /// are ignored (first wins), returning false.
    /// </summary>
    /// <exception cref="ArgumentException">A place id is blank.</exception>
    public bool Add(string nationId, string eraId, string originLabel, ClueCategory category, string value)
    {
        if (string.IsNullOrWhiteSpace(nationId) || string.IsNullOrWhiteSpace(eraId))
            throw new ArgumentException("A fact needs a nation id and an era id.");

        if (string.IsNullOrWhiteSpace(value) || _values.ContainsKey((nationId, eraId, category)))
            return false;

        _values[(nationId, eraId, category)] = value;
        if (!_labels.ContainsKey((nationId, eraId)))
            _labels[(nationId, eraId)] = originLabel;

        if (!_rows.TryGetValue(category, out List<FactRow> list))
            _rows[category] = list = new List<FactRow>();
        list.Add(new FactRow(nationId, eraId, _labels[(nationId, eraId)], category, value));
        return true;
    }

    /// <summary>The fact for a place, or null when it is not in today's table.</summary>
    public string Get(string nationId, string eraId, ClueCategory category) =>
        nationId != null && eraId != null && _values.TryGetValue((nationId, eraId, category), out string v) ? v : null;

    /// <summary>The place's origin label, or null when it is not in today's table.</summary>
    public string OriginLabel(string nationId, string eraId) =>
        nationId != null && eraId != null && _labels.TryGetValue((nationId, eraId), out string l) ? l : null;

    /// <summary>All rows of a category in insertion order (empty when none).</summary>
    public IReadOnlyList<FactRow> Rows(ClueCategory category) =>
        _rows.TryGetValue(category, out List<FactRow> list) ? list : NoRows;

    /// <summary>
    /// Another place of this category with this value (the uniqueness question
    /// behind provable tells and history checks): the first row of
    /// <paramref name="category"/> outside (<paramref name="nationId"/>,
    /// <paramref name="eraId"/>) whose value DiscrepancyLog.ValuesMatch
    /// <paramref name="value"/>. False, with a default row, when none or the
    /// value is blank.
    /// </summary>
    public bool TryFindOtherPlaceWith(ClueCategory category, string nationId, string eraId, string value, out FactRow row)
    {
        row = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        foreach (FactRow r in Rows(category))
        {
            if (r.NationId == nationId && r.EraId == eraId)
                continue;
            if (DiscrepancyLog.ValuesMatch(r.Value, value))
            {
                row = r;
                return true;
            }
        }

        return false;
    }
}
