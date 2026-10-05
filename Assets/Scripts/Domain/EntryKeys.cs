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
/// as items, a rule ("rule:1") and a deviation ("dev:Currency") as search
/// results, a rule and the calendar's today ("cal:today") as values the
/// workbench holds (the PC workbench spec §4.3), beside PickKeys' row keys (a field, a line, a book's row, a
/// record's row; PickKeys reads those back). TryRef says which tab a key's
/// item lives in (and so its scope), for pins and recent items; where a key
/// leads is SmartLinks.ForEntry. Pure.
/// </summary>
public static class EntryKeys
{
    private const string DocumentPrefix = "doc:";
    private const string BookPrefix = "bookof:";
    private const string RecordCardPrefix = "rec:";
    private const string RulePrefix = "rule:";
    private const string DeviationPrefix = "dev:";

    /// <summary>The calendar's today as a value the workbench holds and matches (the PC workbench spec IA6).</summary>
    public const string CalendarToday = "cal:today";

    /// <summary>Document <paramref name="document"/> of the case, as an item ("doc:0").</summary>
    public static string Document(int document) => DocumentPrefix + document.ToString(CultureInfo.InvariantCulture);

    /// <summary>A reference book, as an item ("bookof:Currency").</summary>
    public static string Book(ClueCategory category) => BookPrefix + category;

    /// <summary>A citizen record, as an item ("rec:MRV-552"; the record's id: its number, else its name).</summary>
    public static string RecordCard(string recordId) => RecordCardPrefix + recordId;

    /// <summary>Rule <paramref name="index"/> of the day's directives ("rule:1"; a search result's key).</summary>
    public static string Rule(int index) => RulePrefix + index.ToString(CultureInfo.InvariantCulture);

    /// <summary>The case's deviation of <paramref name="category"/> ("dev:Currency"; a search result's key).</summary>
    public static string Deviation(ClueCategory category) => DeviationPrefix + category;

    /// <summary>Reads a rule's key (<see cref="Rule"/>).</summary>
    public static bool TryRule(string key, out int index) => TryIndex(key, RulePrefix, out index);

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

    /// <summary>The item a key names, with the tab it lives in; false for a key that is not the app's (a garment, a malformed key).</summary>
    public static bool TryRef(string key, out EntryRef entry)
    {
        entry = default;
        AppTab source;
        if (TryDocument(key, out _) || PickKeys.TryField(key, out _, out _))
            source = AppTab.Documents;
        else if (PickKeys.TryLine(key, out _))
            source = AppTab.Transcript;
        else if (TryBook(key, out _) || PickKeys.TryBookRow(key, out _, out _, out _))
            source = AppTab.Reference;
        else if (TryRecordCard(key, out _) || PickKeys.TryRecord(key, out _, out _))
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
