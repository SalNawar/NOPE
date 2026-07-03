using System;
using System.Collections.Generic;

/// <summary>
/// One entry in the agency's master record of every (fake) human.
/// Pure data; the note field is the hook for flavor and future easter eggs.
/// </summary>
public sealed class CitizenRecord
{
    /// <summary>Full legal name (lookup key).</summary>
    public string fullName;

    /// <summary>Recorded date of birth.</summary>
    public string birthDate;

    /// <summary>Where/when this citizen belongs ("Latia — Ancient Rome").</summary>
    public string origin;

    /// <summary>Clerk's note — flavor now, easter eggs and plot hooks later.</summary>
    public string note;
}

/// <summary>
/// The agency's citizen master record: name-keyed lookup used by the Citizen
/// Records desktop app. Pure C# so lookup rules are unit-testable.
/// Future (noted, not built): deliberately missing/corrupted records that force
/// indirect verification via family history (father/sister records).
/// </summary>
public sealed class CitizenRegistry
{
    private readonly List<CitizenRecord> _records = new();

    /// <summary>All records, in insertion order.</summary>
    public IReadOnlyList<CitizenRecord> Records => _records;

    /// <summary>Number of records on file.</summary>
    public int Count => _records.Count;

    /// <summary>Adds a record (ignored when null or unnamed).</summary>
    public void Add(CitizenRecord record)
    {
        if (record != null && !string.IsNullOrWhiteSpace(record.fullName))
            _records.Add(record);
    }

    /// <summary>Removes all records.</summary>
    public void Clear() => _records.Clear();

    /// <summary>
    /// Finds a record by name: exact (trimmed, case-insensitive) match first,
    /// then the first name containing the query. Null when nothing matches.
    /// </summary>
    public CitizenRecord Find(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return null;

        string q = query.Trim();

        foreach (CitizenRecord r in _records)
            if (string.Equals(r.fullName.Trim(), q, StringComparison.OrdinalIgnoreCase))
                return r;

        foreach (CitizenRecord r in _records)
            if (r.fullName.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                return r;

        return null;
    }
}
