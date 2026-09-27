using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>One marked pair (the PC redesign SC4): two fields on two scanned documents that state one category differently.</summary>
public readonly struct AnalysisMark
{
    /// <summary>A mark from its parts.</summary>
    public AnalysisMark(int docA, int fieldA, int docB, int fieldB)
    {
        DocA = docA;
        FieldA = fieldA;
        DocB = docB;
        FieldB = fieldB;
    }

    /// <summary>The first document's index in the case (the lower one).</summary>
    public int DocA { get; }

    /// <summary>The field's index in the first document's field list.</summary>
    public int FieldA { get; }

    /// <summary>The second document's index in the case.</summary>
    public int DocB { get; }

    /// <summary>The field's index in the second document's field list.</summary>
    public int FieldB { get; }

    /// <summary>The marked fields of document <paramref name="doc"/>: its field's index for each side the mark lies on.</summary>
    public IEnumerable<int> FieldsOf(int doc)
    {
        if (DocA == doc)
            yield return FieldA;
        if (DocB == doc)
            yield return FieldB;
    }
}

/// <summary>
/// The Analysis Scanner's rule (the PC redesign SC4, SC5; traveller types L4:
/// papers prove, so two papers that disagree are a contradiction). A scan by
/// hand reads the traveller's scanned papers against each other, never the
/// books, the records, the answers, the dress or the dates, and marks one
/// pair: the first contradicting pair whose category the Deviation Report does
/// not hold yet, in document order then field order. It names neither side as
/// the forgery and picks nothing: the player still compares the two fields.
/// Pure, so the rule is tested headless; CaseDocumentsPresenter runs it.
/// </summary>
public static class PaperAnalysis
{
    /// <summary>
    /// The first pair of <see cref="Contradictions"/> among the scanned papers
    /// (<paramref name="papers"/>: CasePapers.State is Scanned) whose category
    /// is not in <paramref name="documented"/> (the Deviation Report's
    /// categories; null: none documented); null when there is none.
    /// </summary>
    public static AnalysisMark? First(IReadOnlyList<CaseDocument> documents, CasePapers papers, IReadOnlyCollection<ClueCategory> documented)
    {
        foreach (AnalysisMark pair in Contradictions(documents, i => papers != null && papers.State(i) == PaperState.Scanned))
            if (documented == null || !documented.Contains(documents[pair.DocA].fields[pair.FieldA].category))
                return pair;
        return null;
    }

    /// <summary>
    /// Every pair of fields on two of the <paramref name="documents"/> that
    /// <paramref name="included"/> admits (null: all) with the same compared
    /// category (<see cref="IsCompared"/>) and values that differ
    /// (DiscrepancyLog.ValuesMatch; a blank value states nothing), in document
    /// order then field order: the lower document first, its fields in order,
    /// each against the later documents in order. The seam for the traveller
    /// types spec's PaperChecks.Contradictions (redesign phase 7): when it
    /// lands, this body becomes a call to it, so the scanner marks exactly the
    /// pairs the cross proof accepts.
    /// </summary>
    public static List<AnalysisMark> Contradictions(IReadOnlyList<CaseDocument> documents, Func<int, bool> included)
    {
        var pairs = new List<AnalysisMark>();
        int count = documents != null ? documents.Count : 0;
        for (int a = 0; a < count; a++)
        {
            if (!Readable(documents, a, included))
                continue;
            IReadOnlyList<DocumentField> fieldsA = documents[a].fields;
            for (int fa = 0; fa < fieldsA.Count; fa++)
            {
                DocumentField fieldA = fieldsA[fa];
                if (fieldA == null || !IsCompared(fieldA.category) || string.IsNullOrWhiteSpace(fieldA.value))
                    continue;
                for (int b = a + 1; b < count; b++)
                {
                    if (!Readable(documents, b, included))
                        continue;
                    IReadOnlyList<DocumentField> fieldsB = documents[b].fields;
                    for (int fb = 0; fb < fieldsB.Count; fb++)
                    {
                        DocumentField fieldB = fieldsB[fb];
                        if (fieldB != null && fieldB.category == fieldA.category && !string.IsNullOrWhiteSpace(fieldB.value) &&
                            !DiscrepancyLog.ValuesMatch(fieldA.value, fieldB.value))
                            pairs.Add(new AnalysisMark(a, fa, b, fb));
                    }
                }
            }
        }
        return pairs;
    }

    /// <summary>
    /// True for a category two papers are compared on (traveller types F4's
    /// invariant: one value per compared category per traveller): every
    /// category but the directive-only dates, which differ from form to form by
    /// nature and are read against the calendar and the Directives instead.
    /// </summary>
    public static bool IsCompared(ClueCategory category) =>
        category != ClueCategory.DepartureDate && category != ClueCategory.Expiry;

    /// <summary>True when document <paramref name="i"/> is read: it exists, has fields and <paramref name="included"/> admits it.</summary>
    private static bool Readable(IReadOnlyList<CaseDocument> documents, int i, Func<int, bool> included) =>
        documents[i] != null && documents[i].fields != null && (included == null || included(i));
}
