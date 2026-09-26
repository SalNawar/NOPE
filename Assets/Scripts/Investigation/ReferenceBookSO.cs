using UnityEngine;

/// <summary>
/// A player-readable reference book for one fact category (Currency, Language,
/// Technology, ...). The book is only a cover: its rows are today's facts from
/// the FactTable (ContentLibrarySO.BuildToday), the same values the case
/// generator prints on papers, so books and papers can never disagree.
/// </summary>
[CreateAssetMenu(fileName = "RefBook_", menuName = "TimeDesk/Reference Book", order = 5)]
public sealed class ReferenceBookSO : ScriptableObject
{
    /// <summary>Title shown on the book (e.g., "Currency Ledger").</summary>
    public string displayName = "Reference";

    /// <summary>Category this book covers; matches DocumentField.category and the fact category.</summary>
    public ClueCategory category;

    /// <summary>True when the register lists today's places under era headings (the Costume Guide: the traveller-types spec's Q9; ReferenceRows).</summary>
    public bool groupByEra;
}
