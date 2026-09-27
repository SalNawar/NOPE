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
/// not hold yet, in document order then field order. The pairs are exactly
/// those the cross proof accepts (PaperChecks.Contradictions is the one rule,
/// so the scanner can never mark a pair the player cannot log). It names
/// neither side as the forgery and picks nothing: the player still compares
/// the two fields. Pure, so the rule is tested headless;
/// CaseDocumentsPresenter runs it.
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
    /// <paramref name="included"/> admits (null: all) that contradict, as
    /// marks: PaperChecks.Contradictions over the admitted papers' fields (a
    /// paper not admitted, missing or without fields reads as absent), in
    /// document order then field order.
    /// </summary>
    public static List<AnalysisMark> Contradictions(IReadOnlyList<CaseDocument> documents, Func<int, bool> included)
    {
        var read = new IReadOnlyList<DocumentField>[documents != null ? documents.Count : 0];
        for (int i = 0; i < read.Length; i++)
            read[i] = documents[i] != null && (included == null || included(i)) ? documents[i].fields : null;

        return PaperChecks.Contradictions(read)
            .Select(c => new AnalysisMark(c.DocumentA, c.FieldA, c.DocumentB, c.FieldB))
            .ToList();
    }
}
