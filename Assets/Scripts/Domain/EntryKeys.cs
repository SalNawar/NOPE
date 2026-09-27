using System;
using System.Globalization;

/// <summary>Whether an item lives for the case (it points at the traveller at the desk) or for the day (the PC redesign PR2). Not serialized.</summary>
public enum EntryScope
{
    /// <summary>A document, a field, a transcript line: gone when the case ends.</summary>
    Case,

    /// <summary>A book, a Reference row, a record: gone when the day ends.</summary>
    Day
}

/// <summary>A navigable item of the Investigation app: its key, the tab it lives in and its scope (the PC redesign section 4.2).</summary>
public readonly struct EntryRef
{
    /// <summary>An item.</summary>
    public EntryRef(string key, AppTab source)
    {
        Key = key;
        Source = source;
    }

    /// <summary>The item's key (PickKeys' for a pickable row, EntryKeys' for an item).</summary>
    public string Key { get; }

    /// <summary>The tab the item lives in.</summary>
    public AppTab Source { get; }

    /// <summary>A case source's item is the case's, the others the day's (TabOrder.IsCaseSource).</summary>
    public EntryScope Scope => TabOrder.IsCaseSource(Source) ? EntryScope.Case : EntryScope.Day;
}

/// <summary>
/// The keys of the Investigation app's items (the PC redesign section 4.2):
/// a document ("doc:0"), a book ("bookof:Currency") and a record ("rec:{id}")
/// as items, beside PickKeys' row keys (a field, a line, a book's row, a
/// record's row), which this reads back. TryRef says which tab a key's item
/// lives in (and so its scope), for pins and recent items. Pure.
/// </summary>
public static class EntryKeys
{
    private const string DocumentPrefix = "doc:";
    private const string BookPrefix = "bookof:";
    private const string RecordCardPrefix = "rec:";
    private const string FieldPrefix = "field:";
    private const string LinePrefix = "line:";
    private const string BookRowPrefix = "book:";
    private const string RecordRowPrefix = "record:";

    /// <summary>Document <paramref name="document"/> of the case, as an item ("doc:0").</summary>
    public static string Document(int document) => DocumentPrefix + document.ToString(CultureInfo.InvariantCulture);

    /// <summary>A reference book, as an item ("bookof:Currency").</summary>
    public static string Book(ClueCategory category) => BookPrefix + category;

    /// <summary>A citizen record, as an item ("rec:552-1804-33"; the record's id: its number, else its name).</summary>
    public static string RecordCard(string recordId) => RecordCardPrefix + recordId;

    /// <summary>Reads a document item's key.</summary>
    public static bool TryDocument(string key, out int document) => TryIndex(key, DocumentPrefix, out document);

    /// <summary>Reads a book item's key.</summary>
    public static bool TryBook(string key, out ClueCategory category) => TryCategory(Rest(key, BookPrefix), out category);

    /// <summary>Reads a record item's key.</summary>
    public static bool TryRecordCard(string key, out string recordId)
    {
        recordId = Rest(key, RecordCardPrefix);
        return !string.IsNullOrEmpty(recordId);
    }

    /// <summary>Reads PickKeys.Field ("field:0:2").</summary>
    public static bool TryField(string key, out int document, out int field)
    {
        document = field = 0;
        string rest = Rest(key, FieldPrefix);
        int colon = rest != null ? rest.IndexOf(':') : -1;
        return colon > 0 && Index(rest.Substring(0, colon), out document) && Index(rest.Substring(colon + 1), out field);
    }

    /// <summary>Reads PickKeys.Line ("line:5").</summary>
    public static bool TryLine(string key, out int line) => TryIndex(key, LinePrefix, out line);

    /// <summary>Reads PickKeys.BookRow ("book:Currency:greece:ancient").</summary>
    public static bool TryBookRow(string key, out ClueCategory category, out string nationId, out string eraId)
    {
        category = default;
        nationId = eraId = null;
        string rest = Rest(key, BookRowPrefix);
        string[] parts = rest != null ? rest.Split(':') : null;
        if (parts == null || parts.Length != 3 || parts[1].Length == 0 || parts[2].Length == 0 || !TryCategory(parts[0], out category))
            return false;
        nationId = parts[1];
        eraId = parts[2];
        return true;
    }

    /// <summary>Reads PickKeys.Record ("record:552-1804-33:BirthDate"; the id may hold a colon, the category is after the last one).</summary>
    public static bool TryRecordRow(string key, out string recordId, out ClueCategory category)
    {
        recordId = null;
        category = default;
        string rest = Rest(key, RecordRowPrefix);
        int colon = rest != null ? rest.LastIndexOf(':') : -1;
        if (colon <= 0 || !TryCategory(rest.Substring(colon + 1), out category))
            return false;
        recordId = rest.Substring(0, colon);
        return true;
    }

    /// <summary>The item a key names, with the tab it lives in; false for a key that is not the app's (a garment, a malformed key).</summary>
    public static bool TryRef(string key, out EntryRef entry)
    {
        entry = default;
        AppTab source;
        if (TryDocument(key, out _) || TryField(key, out _, out _))
            source = AppTab.Documents;
        else if (TryLine(key, out _))
            source = AppTab.Transcript;
        else if (TryBook(key, out _) || TryBookRow(key, out _, out _, out _))
            source = AppTab.Reference;
        else if (TryRecordCard(key, out _) || TryRecordRow(key, out _, out _))
            source = AppTab.Records;
        else
            return false;
        entry = new EntryRef(key, source);
        return true;
    }

    /// <summary>The key after its prefix, or null.</summary>
    private static string Rest(string key, string prefix) =>
        key != null && key.StartsWith(prefix, StringComparison.Ordinal) ? key.Substring(prefix.Length) : null;

    private static bool TryIndex(string key, string prefix, out int index)
    {
        index = 0;
        string rest = Rest(key, prefix);
        return rest != null && Index(rest, out index);
    }

    /// <summary>A non-negative whole number, digits only.</summary>
    private static bool Index(string text, out int index) =>
        int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out index);

    /// <summary>A category by its enum name (never a number).</summary>
    private static bool TryCategory(string text, out ClueCategory category)
    {
        category = default;
        return !string.IsNullOrEmpty(text) && !char.IsDigit(text[0]) && text[0] != '-' &&
               Enum.TryParse(text, false, out category) && Enum.IsDefined(typeof(ClueCategory), category);
    }
}
