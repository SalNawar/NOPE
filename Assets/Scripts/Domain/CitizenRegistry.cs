using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// One row of a citizen record (the traveller-types spec's R1): its label, its
/// value and, when the row is evidence, the category it proves (a compare pick
/// of RecordField evidence). A row without a category is shown, never picked.
/// </summary>
public readonly struct RecordRow
{
    /// <summary>A row that is not evidence (an origin, a note).</summary>
    public RecordRow(string label, string value)
    {
        Label = label;
        Value = value;
        IsEvidence = false;
        Category = default;
    }

    /// <summary>A row that is evidence of <paramref name="category"/>.</summary>
    public RecordRow(string label, string value, ClueCategory category)
    {
        Label = label;
        Value = value;
        IsEvidence = true;
        Category = category;
    }

    /// <summary>The row's label ("Born").</summary>
    public string Label { get; }

    /// <summary>The row's value ("3 May 1131").</summary>
    public string Value { get; }

    /// <summary>True when the row is compare-clickable evidence of its <see cref="Category"/>.</summary>
    public bool IsEvidence { get; }

    /// <summary>The category the row proves (only meaningful when <see cref="IsEvidence"/>).</summary>
    public ClueCategory Category { get; }
}

/// <summary>
/// A titled group of a record's rows: the art's icons for a 2150 citizen's
/// account (Records, Forms on file, Travel), one group for a registry entry;
/// a blank title is an untitled group (an account's closing note).
/// </summary>
public sealed class RecordGroup
{
    /// <summary>A group of <paramref name="rows"/> (copied; null is none).</summary>
    public RecordGroup(string title, IEnumerable<RecordRow> rows)
    {
        Title = title ?? string.Empty;
        Rows = rows != null ? rows.ToArray() : Array.Empty<RecordRow>();
    }

    /// <summary>The group's heading ("REGISTRY ENTRY"); blank = untitled.</summary>
    public string Title { get; }

    /// <summary>The group's rows, in order.</summary>
    public IReadOnlyList<RecordRow> Rows { get; }
}

/// <summary>
/// One entry in the agency's master record of every (fake) human: rows in
/// groups (R1), found by the agency number or the name (R3, through the search
/// index). Records carry the
/// registered identity (a liar's is their cover); the note row is the hook for
/// flavour and future easter eggs.
/// </summary>
public sealed class CitizenRecord
{
    /// <summary>A record of <paramref name="fullName"/> (<paramref name="number"/>: the agency number, blank when none is on file) with <paramref name="groups"/> (copied; null is none).</summary>
    public CitizenRecord(string fullName, string number, IEnumerable<RecordGroup> groups)
    {
        FullName = fullName;
        Number = number;
        Groups = groups != null ? groups.Where(g => g != null).ToArray() : Array.Empty<RecordGroup>();
    }

    /// <summary>The registered full name (a lookup key, and whose record a row's evidence is).</summary>
    public string FullName { get; }

    /// <summary>The agency number (a Citizen ID or a Displacement No.; a lookup key, whole only); blank when none is on file.</summary>
    public string Number { get; }

    /// <summary>The record's groups, in order.</summary>
    public IReadOnlyList<RecordGroup> Groups { get; }

    /// <summary>The record's identity in pick keys (PickKeys.Record): the trimmed number, else the name.</summary>
    public string Id => string.IsNullOrWhiteSpace(Number) ? FullName : Number.Trim();
}

/// <summary>
/// The agency's citizen master record: today's records, in order. The Records
/// tab's lookup by name or number is search's (the index scoped to Records,
/// redesign phase 19: IndexEntries.Records), so search and the lookup find the
/// same records. Future (noted, not built): deliberately missing/corrupted
/// records that force indirect verification via family history (father/sister
/// records).
/// </summary>
public sealed class CitizenRegistry
{
    private readonly List<CitizenRecord> _records = new();

    /// <summary>The records on file, in the order added.</summary>
    public IReadOnlyList<CitizenRecord> Records => _records;

    /// <summary>Adds a record (ignored when null or unnamed).</summary>
    public void Add(CitizenRecord record)
    {
        if (record != null && !string.IsNullOrWhiteSpace(record.FullName))
            _records.Add(record);
    }
}
