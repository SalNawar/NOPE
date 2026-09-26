using System.Collections.Generic;

/// <summary>A pinned or recent item: what it is and the label the sidebar lists it by.</summary>
public readonly struct EntryItem
{
    /// <summary>An item.</summary>
    public EntryItem(EntryRef entry, string label)
    {
        Ref = entry;
        Label = label ?? string.Empty;
    }

    /// <summary>The item.</summary>
    public EntryRef Ref { get; }

    /// <summary>Its label ("Visa · Visa Class", "Costume Guide").</summary>
    public string Label { get; }
}

/// <summary>What a pin toggle did.</summary>
public enum PinChange
{
    /// <summary>The item is pinned now.</summary>
    Pinned,

    /// <summary>The item was pinned and is not any more.</summary>
    Unpinned,

    /// <summary>The board is full: nothing changed (unpin something first).</summary>
    Full
}

/// <summary>
/// The sidebar's pins (the PC redesign PR1, PR2): any item with an entry key,
/// in pin order, at most the cap (20), never twice. Toggle pins or unpins; a
/// full board refuses a new pin. Case items (documents, fields, lines) are
/// unpinned when the case ends; day items (books, Reference rows, records)
/// stay until the day ends. Kept for the office session, never saved. Pure;
/// the app's pins panel draws it.
/// </summary>
public sealed class PinBoard
{
    private readonly List<EntryItem> _items = new List<EntryItem>();
    private readonly int _cap;

    /// <summary>A board of at most <paramref name="cap"/> pins (below 1 counts as 1).</summary>
    public PinBoard(int cap)
    {
        _cap = cap < 1 ? 1 : cap;
    }

    /// <summary>The pins, oldest first.</summary>
    public IReadOnlyList<EntryItem> Items => _items;

    /// <summary>True while the item with <paramref name="key"/> is pinned.</summary>
    public bool IsPinned(string key) => IndexOf(key) >= 0;

    /// <summary>Unpins the item when pinned, else pins it last (unless the board is full).</summary>
    public PinChange Toggle(EntryRef entry, string label)
    {
        int at = IndexOf(entry.Key);
        if (at >= 0)
        {
            _items.RemoveAt(at);
            return PinChange.Unpinned;
        }
        if (_items.Count >= _cap)
            return PinChange.Full;
        _items.Add(new EntryItem(entry, label));
        return PinChange.Pinned;
    }

    /// <summary>Unpins the item with <paramref name="key"/>; false when it was not pinned.</summary>
    public bool Unpin(string key)
    {
        int at = IndexOf(key);
        if (at < 0)
            return false;
        _items.RemoveAt(at);
        return true;
    }

    /// <summary>The case ended (or a new one starts): its items are unpinned.</summary>
    public void EndCase() => _items.RemoveAll(i => i.Ref.Scope == EntryScope.Case);

    /// <summary>The day ended (or a new one starts): every pin goes.</summary>
    public void EndDay() => _items.Clear();

    private int IndexOf(string key)
    {
        for (int i = 0; i < _items.Count; i++)
            if (_items[i].Ref.Key == key)
                return i;
        return -1;
    }
}
