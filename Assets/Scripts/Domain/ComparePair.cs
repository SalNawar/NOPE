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

    /// <summary>A Citizen Records row by category ("record:BirthDate").</summary>
    public static string Record(ClueCategory category) => "record:" + category;
}
