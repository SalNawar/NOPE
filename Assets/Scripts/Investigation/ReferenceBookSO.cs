using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A player-readable reference book for one clue category (Currency, Language,
/// Technology, ...). It is the ground truth the player cross-references the
/// visitor's documents against: for a given nation + era it lists the value
/// that is historically consistent. The investigation UI renders these entries
/// as flippable pages.
/// </summary>
[CreateAssetMenu(fileName = "RefBook_", menuName = "TimeDesk/Reference Book", order = 5)]
public sealed class ReferenceBookSO : ScriptableObject
{
    /// <summary>Title shown on the book (e.g., "Currency Ledger").</summary>
    public string displayName = "Reference";

    /// <summary>Category this book covers; matches DocumentField.category.</summary>
    public ClueCategory category;

    /// <summary>Flavor / how-to-read note shown on the cover page.</summary>
    [TextArea] public string description;

    /// <summary>Authored truth entries (nation+era -> consistent value).</summary>
    public List<ReferenceEntry> entries = new();

    /// <summary>
    /// Returns the canonical value for a nation+era, or null if not authored.
    /// An entry with a null nation matches any nation for that era.
    /// </summary>
    public string GetValue(NationSO nation, EraSO era)
    {
        if (era == null)
            return null;

        // Prefer an exact nation+era match, then fall back to era-only entries.
        string eraOnly = null;

        foreach (ReferenceEntry e in entries)
        {
            if (e == null || e.era != era)
                continue;

            if (e.nation == nation)
                return e.value;

            if (e.nation == null && eraOnly == null)
                eraOnly = e.value;
        }

        return eraOnly;
    }

    /// <summary>
    /// Returns some value from this book that differs from <paramref name="exclude"/>,
    /// used to forge an anachronistic field. Null if none available.
    /// </summary>
    public string GetAnyOtherValue(string exclude)
    {
        foreach (ReferenceEntry e in entries)
            if (e != null && !string.IsNullOrEmpty(e.value) && e.value != exclude)
                return e.value;

        return null;
    }
}

/// <summary>
/// One authored truth: in this nation during this era, the category's value is X.
/// </summary>
[Serializable]
public sealed class ReferenceEntry
{
    /// <summary>Nation this entry applies to (null = any nation in the era).</summary>
    public NationSO nation;

    /// <summary>Era this entry applies to.</summary>
    public EraSO era;

    /// <summary>The historically consistent value (e.g., "Denarius").</summary>
    public string value;

    /// <summary>Optional designer note / extra context shown in the book.</summary>
    [TextArea] public string note;
}
