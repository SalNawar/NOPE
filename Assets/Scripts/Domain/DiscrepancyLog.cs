using System;
using System.Collections.Generic;

/// <summary>Which kind of row a compare selection came from.</summary>
public enum EvidenceKind
{
    /// <summary>No typed evidence (plain visual compare).</summary>
    None,

    /// <summary>A field on one of the visitor's documents.</summary>
    DocumentField,

    /// <summary>A reference-book truth entry.</summary>
    ReferenceEntry
}

/// <summary>
/// Typed metadata carried by one side of a comparison so the discrepancy
/// system can tell a real contradiction from a meaningless mismatch.
/// Pure data — identified by string ids so the domain stays decoupled from
/// ScriptableObjects and scenes.
/// </summary>
public struct CompareEvidence
{
    /// <summary>What kind of row this evidence came from.</summary>
    public EvidenceKind kind;

    /// <summary>Clue category of the field / book.</summary>
    public ClueCategory category;

    /// <summary>The displayed value.</summary>
    public string value;

    /// <summary>Document side: true if the value is forged for the claim.</summary>
    public bool isAnachronism;

    /// <summary>Reference side: nation id the entry applies to (null/empty = any).</summary>
    public string entryNationId;

    /// <summary>Reference side: era id the entry applies to.</summary>
    public string entryEraId;

    /// <summary>Reference side: display label for the entry's origin ("Latia — Ancient Rome").</summary>
    public string entryOriginLabel;

    /// <summary>Evidence for a clicked document-field row.</summary>
    public static CompareEvidence FromDocumentField(DocumentField field) => new CompareEvidence
    {
        kind = EvidenceKind.DocumentField,
        category = field.category,
        value = field.value,
        isAnachronism = field.isAnachronism
    };

    /// <summary>Evidence for a clicked reference-book entry row.</summary>
    public static CompareEvidence ForReferenceEntry(ClueCategory bookCategory, string value, string nationId, string eraId, string originLabel) => new CompareEvidence
    {
        kind = EvidenceKind.ReferenceEntry,
        category = bookCategory,
        value = value,
        entryNationId = nationId,
        entryEraId = eraId,
        entryOriginLabel = originLabel
    };
}

/// <summary>One documented contradiction on the current case.</summary>
public sealed class Discrepancy
{
    /// <summary>Category that was disproved (Language, Currency, ...).</summary>
    public ClueCategory category;

    /// <summary>Forged value printed on the visitor's papers.</summary>
    public string documentValue;

    /// <summary>
    /// Mismatch proof: the historically consistent value for the claim per the
    /// reference book. Null when this discrepancy was proved by origin match.
    /// </summary>
    public string expectedValue;

    /// <summary>
    /// Match proof: where the printed value actually belongs (a different
    /// nation/era than claimed). Null when proved by mismatch.
    /// </summary>
    public string actualOrigin;

    /// <summary>Player-facing report line ("LANGUAGE INCORRECT — ...").</summary>
    public string Summary => actualOrigin != null
        ? $"{category.ToString().ToUpperInvariant()} INCORRECT — papers show \"{documentValue}\", which belongs to {actualOrigin}"
        : $"{category.ToString().ToUpperInvariant()} INCORRECT — papers: \"{documentValue}\"  /  expected: \"{expectedValue}\"";
}

/// <summary>
/// Per-case list of documented contradictions (the "Deviation Report").
/// Pure C# so the registration rules are unit-testable. Registration only
/// succeeds for TRUE contradictions, proved either way:
/// - MISMATCH proof: a forged document field differs from the reference entry
///   that applies to the CLAIMED nation+era.
/// - MATCH proof: a forged document field equals a reference entry that does
///   NOT apply to the claim — the value provably belongs somewhere else
///   (e.g. papers claim Medieval but the declared device matches Ancient Rome).
/// </summary>
public sealed class DiscrepancyLog
{
    private readonly List<Discrepancy> _items = new();

    /// <summary>Registered discrepancies, in registration order.</summary>
    public IReadOnlyList<Discrepancy> Items => _items;

    /// <summary>Number of registered discrepancies.</summary>
    public int Count => _items.Count;

    /// <summary>Empties the log (call when a new case starts).</summary>
    public void Clear() => _items.Clear();

    /// <summary>
    /// Validates a compared pair against the current claim and registers it if
    /// it is a true contradiction. Returns the new discrepancy, or null when
    /// the pair proves nothing (or its category is already documented).
    /// </summary>
    public Discrepancy TryRegister(CompareEvidence a, CompareEvidence b, string claimedNationId, string claimedEraId)
    {
        CompareEvidence doc = a.kind == EvidenceKind.DocumentField ? a : b;
        CompareEvidence book = b.kind == EvidenceKind.ReferenceEntry ? b : a;

        // Must be one document field against one reference entry.
        if (doc.kind != EvidenceKind.DocumentField || book.kind != EvidenceKind.ReferenceEntry)
            return null;

        // Same category — comparing Language against Currency proves nothing.
        if (doc.category != book.category)
            return null;

        // Without a claim there is nothing to contradict.
        if (string.IsNullOrEmpty(claimedEraId))
            return null;

        // Only a genuinely forged field is a contradiction; a coincidental
        // mismatch/match on an honest field proves nothing.
        if (!doc.isAnachronism)
            return null;

        bool entryAppliesToClaim =
            !string.IsNullOrEmpty(book.entryEraId) && book.entryEraId == claimedEraId &&
            (string.IsNullOrEmpty(book.entryNationId) || book.entryNationId == claimedNationId);

        bool valuesMatch = ValuesMatch(doc.value, book.value);

        Discrepancy found = null;

        if (entryAppliesToClaim && !valuesMatch)
        {
            // Mismatch proof: the claim's reference disagrees with the papers.
            found = new Discrepancy
            {
                category = doc.category,
                documentValue = doc.value,
                expectedValue = book.value
            };
        }
        else if (!entryAppliesToClaim && valuesMatch)
        {
            // Match proof: the papers' value belongs to a different origin.
            found = new Discrepancy
            {
                category = doc.category,
                documentValue = doc.value,
                actualOrigin = string.IsNullOrEmpty(book.entryOriginLabel) ? "a different era" : book.entryOriginLabel
            };
        }

        if (found == null)
            return null;

        // One documented discrepancy per category is enough evidence.
        foreach (Discrepancy existing in _items)
            if (existing.category == found.category)
                return null;

        _items.Add(found);
        return found;
    }

    /// <summary>Case-insensitive, trimmed equality (mirrors the compare bar).</summary>
    private static bool ValuesMatch(string x, string y) =>
        string.Equals((x ?? string.Empty).Trim(), (y ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase);
}
