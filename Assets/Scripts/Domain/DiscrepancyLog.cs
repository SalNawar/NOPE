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
    ReferenceEntry,

    /// <summary>A field from the agency's citizen records.</summary>
    RecordField,

    /// <summary>A traveller's spoken answer in the interview transcript.</summary>
    Answer
}

/// <summary>How a discrepancy was proved.</summary>
public enum DiscrepancyProof
{
    /// <summary>The statement (papers or answer) differs from the claimed place's reference entry.</summary>
    ClaimMismatch,

    /// <summary>The statement matches a reference entry that belongs to a different origin.</summary>
    ForeignOrigin,

    /// <summary>The statement differs from the agency's citizen record.</summary>
    RecordMismatch
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

    /// <summary>Statement side (document field or answer): true if the value is a liar's tell.</summary>
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

    /// <summary>Evidence for a clicked citizen-record field.</summary>
    public static CompareEvidence ForRecordField(ClueCategory category, string value) => new CompareEvidence
    {
        kind = EvidenceKind.RecordField,
        category = category,
        value = value,
        entryOriginLabel = "agency records"
    };

    /// <summary>Evidence for a clicked interview answer row: the canonical value the traveller said, a tell when <paramref name="isTell"/>.</summary>
    public static CompareEvidence ForAnswer(ClueCategory category, string value, bool isTell) => new CompareEvidence
    {
        kind = EvidenceKind.Answer,
        category = category,
        value = value,
        isAnachronism = isTell
    };
}

/// <summary>One documented contradiction on the current case.</summary>
public sealed class Discrepancy
{
    /// <summary>Category that was disproved (Language, Currency, ...).</summary>
    public ClueCategory category;

    /// <summary>The tell's value, as printed or as spoken.</summary>
    public string documentValue;

    /// <summary>
    /// Mismatch proof: the historically consistent value for the claim per the
    /// reference book. Null when this discrepancy was proved by origin match.
    /// </summary>
    public string expectedValue;

    /// <summary>
    /// Match proof: where the printed or spoken value actually belongs (a
    /// different nation/era than claimed). Null when proved by mismatch.
    /// </summary>
    public string actualOrigin;

    /// <summary>How this contradiction was proved.</summary>
    public DiscrepancyProof provedBy;

    /// <summary>Where the tell was stated: DocumentField (papers) or Answer (the traveller said it).</summary>
    public EvidenceKind source;

    /// <summary>Player-facing report line ("CAPITAL INCORRECT — traveller said: ..."), naming where the tell was stated.</summary>
    public string Summary
    {
        get
        {
            string what = ClueLabels.Report(category);
            bool said = source == EvidenceKind.Answer;
            switch (provedBy)
            {
                case DiscrepancyProof.ForeignOrigin:
                    return said
                        ? $"{what} INCORRECT — traveller said \"{documentValue}\", which belongs to {actualOrigin}"
                        : $"{what} INCORRECT — papers show \"{documentValue}\", which belongs to {actualOrigin}";
                case DiscrepancyProof.RecordMismatch:
                    return $"{what} INCORRECT — {Stated(said)}: \"{documentValue}\"  /  agency records: \"{expectedValue}\"";
                default:
                    return $"{what} INCORRECT — {Stated(said)}: \"{documentValue}\"  /  expected: \"{expectedValue}\"";
            }
        }
    }

    /// <summary>Who stated the value, as a mismatch line names it.</summary>
    private static string Stated(bool said) => said ? "traveller said" : "papers";
}

/// <summary>
/// Per-case list of documented contradictions (the Deviation Report). Pure C#
/// so the rules are unit-testable. <see cref="Prove"/> decides whether a
/// compared pair is a true contradiction: a statement (document field or
/// answer) against one truth source (a reference entry or the citizen
/// record), proved either way:
/// - MISMATCH proof: a liar's tell differs from the reference entry that
///   applies to the CLAIMED nation+era, or from the agency's record.
/// - MATCH proof: a liar's tell equals a reference entry that does
///   NOT apply to the claim — the value provably belongs somewhere else
///   (e.g. papers claim Medieval but the declared device matches Ancient Rome).
/// <see cref="Add"/> documents it once per category.
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
    /// Whether a compared pair proves a contradiction of the current claim:
    /// the proof, or null when it proves nothing. The statement side (a
    /// document field or an answer) must be a liar's tell and face exactly one
    /// truth source (a reference entry or a record field) of the same
    /// category; two statements or two truths prove nothing. Pure: no log changes.
    /// </summary>
    public static Discrepancy Prove(CompareEvidence a, CompareEvidence b, string claimedNationId, string claimedEraId)
    {
        CompareEvidence statement, truth;
        if (IsStatement(a.kind))
        {
            statement = a;
            truth = b;
        }
        else
        {
            statement = b;
            truth = a;
        }

        // Must be one statement (papers or answer) against one truth source (book or records).
        if (!IsStatement(statement.kind))
            return null;
        if (truth.kind != EvidenceKind.ReferenceEntry && truth.kind != EvidenceKind.RecordField)
            return null;

        // Same category — comparing Language against Currency proves nothing.
        if (statement.category != truth.category)
            return null;

        // Only a liar's tell is a contradiction; a coincidental
        // mismatch/match on an honest statement proves nothing.
        if (!statement.isAnachronism)
            return null;

        if (truth.kind == EvidenceKind.RecordField)
        {
            // Record proof: the statement disagrees with the agency's own
            // records about who this person is. No era claim involved.
            if (ValuesMatch(statement.value, truth.value))
                return null;

            return new Discrepancy
            {
                category = statement.category,
                documentValue = statement.value,
                expectedValue = truth.value,
                provedBy = DiscrepancyProof.RecordMismatch,
                source = statement.kind
            };
        }

        // Without a claim there is nothing to contradict.
        if (string.IsNullOrEmpty(claimedEraId))
            return null;

        bool entryAppliesToClaim =
            !string.IsNullOrEmpty(truth.entryEraId) && truth.entryEraId == claimedEraId &&
            (string.IsNullOrEmpty(truth.entryNationId) || truth.entryNationId == claimedNationId);

        bool valuesMatch = ValuesMatch(statement.value, truth.value);

        if (entryAppliesToClaim && !valuesMatch)
        {
            // Mismatch proof: the claim's reference disagrees with the statement.
            return new Discrepancy
            {
                category = statement.category,
                documentValue = statement.value,
                expectedValue = truth.value,
                provedBy = DiscrepancyProof.ClaimMismatch,
                source = statement.kind
            };
        }

        if (!entryAppliesToClaim && valuesMatch)
        {
            // Match proof: the stated value belongs to a different origin.
            return new Discrepancy
            {
                category = statement.category,
                documentValue = statement.value,
                actualOrigin = string.IsNullOrEmpty(truth.entryOriginLabel) ? "a different era" : truth.entryOriginLabel,
                provedBy = DiscrepancyProof.ForeignOrigin,
                source = statement.kind
            };
        }

        return null;
    }

    /// <summary>
    /// Documents a proof. False, with nothing added, when the proof is null or
    /// its category is already documented: one discrepancy per category is
    /// enough evidence, whatever its source.
    /// </summary>
    public bool Add(Discrepancy proof)
    {
        if (proof == null)
            return false;

        foreach (Discrepancy existing in _items)
            if (existing.category == proof.category)
                return false;

        _items.Add(proof);
        return true;
    }

    /// <summary>True for a statement row: a document field or a spoken answer.</summary>
    private static bool IsStatement(EvidenceKind kind) =>
        kind == EvidenceKind.DocumentField || kind == EvidenceKind.Answer;

    /// <summary>Case-insensitive, trimmed equality (mirrors the compare bar; shared with Forgery.IsProvableTell and TravellerGenders.FromNameLists).</summary>
    internal static bool ValuesMatch(string x, string y) =>
        string.Equals((x ?? string.Empty).Trim(), (y ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase);
}
