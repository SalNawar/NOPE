using System.Collections.Generic;

/// <summary>
/// The sidebar's recent items (the PC redesign PR2): the last items opened or
/// jumped to, newest first, never twice (touching one again moves it to the
/// front with its new label), at most the cap (10; the oldest drops). The
/// pins' scopes: case items go when the case ends, everything when the day
/// ends. Kept for the office session, never saved. Pure.
/// </summary>
public sealed class RecentList
{
    private readonly List<EntryItem> _items = new List<EntryItem>();
    private readonly int _cap;

    /// <summary>A list of at most <paramref name="cap"/> items (below 1 counts as 1).</summary>
    public RecentList(int cap)
    {
        _cap = cap < 1 ? 1 : cap;
    }

    /// <summary>The items, newest first.</summary>
    public IReadOnlyList<EntryItem> Items => _items;

    /// <summary>The item was opened or jumped to: it goes first.</summary>
    public void Touch(EntryRef entry, string label)
    {
        _items.RemoveAll(i => i.Ref.Key == entry.Key);
        _items.Insert(0, new EntryItem(entry, label));
        if (_items.Count > _cap)
            _items.RemoveRange(_cap, _items.Count - _cap);
    }

    /// <summary>The case ended (or a new one starts): its items go.</summary>
    public void EndCase() => _items.RemoveAll(i => i.Ref.Scope == EntryScope.Case);

    /// <summary>The day ended (or a new one starts): every item goes.</summary>
    public void EndDay() => _items.Clear();
}
