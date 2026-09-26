/// <summary>
/// One pickable value, from any surface (a scanned-copy or desk-paper row, a
/// book row, a record row, a transcript or bubble answer, a garment): its
/// identity (PickKeys), the compare bar's label, the text the bar shows (the
/// canonical value, or piece 9's placeholder for an untranslated statement)
/// and the typed evidence (always canonical).
/// </summary>
public readonly struct ComparePick
{
    /// <summary>A pick of one value.</summary>
    public ComparePick(string key, string label, string shown, CompareEvidence evidence)
    {
        Key = key;
        Label = label;
        Shown = shown;
        Evidence = evidence;
    }

    /// <summary>The value's identity (PickKeys): the same value has one key on every surface; blank = none.</summary>
    public string Key { get; }

    /// <summary>The compare bar's label for this side ("Travel Passport · Coin of Issue").</summary>
    public string Label { get; }

    /// <summary>The text the bar shows for this side (canonical, or the untranslated placeholder).</summary>
    public string Shown { get; }

    /// <summary>The typed evidence (canonical; DiscrepancyLog.Prove reads it).</summary>
    public CompareEvidence Evidence { get; }
}

/// <summary>What a pick did to the comparison.</summary>
public enum CompareStep
{
    /// <summary>The value was already picked: the comparison is empty again.</summary>
    Cleared,

    /// <summary>The first side is picked; the next pick pairs with it.</summary>
    Pending,

    /// <summary>Both sides are picked.</summary>
    Paired
}

/// <summary>
/// The two sides of a comparison, from any surface (piece 10: the PC and the
/// desk share one): a value picked again (the same non-blank key as either
/// side) clears it; a first pick waits; a second pairs; a pick after a pair
/// starts a new comparison with it as the first side. Pure; CompareController
/// draws it.
/// </summary>
public sealed class ComparePair
{
    /// <summary>True once the first side is picked.</summary>
    public bool HasA { get; private set; }

    /// <summary>True once both sides are picked.</summary>
    public bool IsPaired { get; private set; }

    /// <summary>The first side (default until picked).</summary>
    public ComparePick A { get; private set; }

    /// <summary>The second side (default until paired).</summary>
    public ComparePick B { get; private set; }

    /// <summary>
    /// True when a paired comparison matches: each side's CompareEvidence.MatchValue
    /// (a garment shows its item but matches on its place's Culture value)
    /// under DiscrepancyLog.ValuesMatch; false before a pair.
    /// </summary>
    public bool Matches => IsPaired && DiscrepancyLog.ValuesMatch(A.Evidence.MatchValue(A.Shown), B.Evidence.MatchValue(B.Shown));

    /// <summary>Picks a value (see the class summary) and says what it did.</summary>
    public CompareStep Select(ComparePick pick)
    {
        if ((HasA && SameKey(pick, A)) || (IsPaired && SameKey(pick, B)))
        {
            Clear();
            return CompareStep.Cleared;
        }

        if (IsPaired)
            Clear();

        if (!HasA)
        {
            A = pick;
            HasA = true;
            return CompareStep.Pending;
        }

        B = pick;
        IsPaired = true;
        return CompareStep.Paired;
    }

    /// <summary>Empties both sides.</summary>
    public void Clear()
    {
        A = default;
        B = default;
        HasA = false;
        IsPaired = false;
    }

    /// <summary>True when both keys are the same non-blank key.</summary>
    private static bool SameKey(ComparePick x, ComparePick y) =>
        !string.IsNullOrWhiteSpace(x.Key) && string.Equals(x.Key, y.Key, System.StringComparison.Ordinal);
}

/// <summary>
/// The keys of pickable values (piece 10): a value has one key on every
/// surface, so the same field picked on the desk paper and on its scanned copy
/// is the same pick, and kinds never collide.
/// </summary>
public static class PickKeys
{
    /// <summary>A document's field by the document's index in the case and the field's index in its list ("field:0:2").</summary>
    public static string Field(int document, int field) => "field:" + document + ":" + field;

    /// <summary>A transcript line by its index in the transcript ("line:5"): the transcript row and the bubble's answer.</summary>
    public static string Line(int transcriptIndex) => "line:" + transcriptIndex;

    /// <summary>A garment by its index in the traveller's look ("garment:1").</summary>
    public static string Garment(int index) => "garment:" + index;

    /// <summary>A reference book's row by category and place ("book:Currency:greece:ancient").</summary>
    public static string BookRow(ClueCategory category, string nationId, string eraId) => "book:" + category + ":" + nationId + ":" + eraId;

    /// <summary>A Citizen Records row by the record it belongs to (CitizenRecord.Id: its number, else its name) and its category ("record:552-1804-33:BirthDate"), so two records' rows are two picks (audit R4-009).</summary>
    public static string Record(ClueCategory category, string recordId) => "record:" + recordId + ":" + category;

    /// <summary>Reads a Field key back: true with its document's and field's indices.</summary>
    public static bool TryField(string key, out int document, out int field)
    {
        document = -1;
        field = -1;
        string[] parts = Parts(key, "field:", 2);
        return parts != null && TryIndex(parts[0], out document) && TryIndex(parts[1], out field);
    }

    /// <summary>Reads a Line key back: true with its transcript index.</summary>
    public static bool TryLine(string key, out int transcriptIndex)
    {
        transcriptIndex = -1;
        string[] parts = Parts(key, "line:", 1);
        return parts != null && TryIndex(parts[0], out transcriptIndex);
    }

    /// <summary>Reads a BookRow key back: true with its category and place.</summary>
    public static bool TryBookRow(string key, out ClueCategory category, out string nationId, out string eraId)
    {
        category = default;
        nationId = null;
        eraId = null;
        string[] parts = Parts(key, "book:", 3);
        if (parts == null || !TryCategory(parts[0], out category))
            return false;
        nationId = parts[1];
        eraId = parts[2];
        return true;
    }

    /// <summary>Reads a Record key back: true with the record's id (the text up to the last ':', so a name keeps its own characters) and the row's category.</summary>
    public static bool TryRecord(string key, out string recordId, out ClueCategory category)
    {
        recordId = null;
        category = default;
        const string prefix = "record:";
        if (key == null || !key.StartsWith(prefix, System.StringComparison.Ordinal))
            return false;
        int split = key.LastIndexOf(':');
        if (split <= prefix.Length - 1 || !TryCategory(key.Substring(split + 1), out category))
            return false;
        recordId = key.Substring(prefix.Length, split - prefix.Length);
        return recordId.Length > 0;
    }

    /// <summary>The key's parts after <paramref name="prefix"/> when there are exactly <paramref name="count"/> non-blank ones, else null.</summary>
    private static string[] Parts(string key, string prefix, int count)
    {
        if (key == null || !key.StartsWith(prefix, System.StringComparison.Ordinal))
            return null;
        string[] parts = key.Substring(prefix.Length).Split(':');
        if (parts.Length != count)
            return null;
        foreach (string part in parts)
            if (part.Length == 0)
                return null;
        return parts;
    }

    /// <summary>A non-negative index written in a key.</summary>
    private static bool TryIndex(string text, out int index) =>
        int.TryParse(text, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out index);

    /// <summary>A category written by its name in a key (never by its number).</summary>
    private static bool TryCategory(string text, out ClueCategory category)
    {
        foreach (ClueCategory c in (ClueCategory[])System.Enum.GetValues(typeof(ClueCategory)))
            if (string.Equals(c.ToString(), text, System.StringComparison.Ordinal))
            {
                category = c;
                return true;
            }
        category = default;
        return false;
    }
}
