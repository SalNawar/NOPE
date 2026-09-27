using System.Collections.Generic;

/// <summary>One pair of boxes on two papers that disagree: the two papers' indices, the two fields' indices and their category.</summary>
public readonly struct PaperContradiction
{
    /// <summary>The first paper, by its index in the case (the lower one).</summary>
    public readonly int DocumentA;

    /// <summary>The box on the first paper, by its index in the paper's fields.</summary>
    public readonly int FieldA;

    /// <summary>The second paper, by its index in the case.</summary>
    public readonly int DocumentB;

    /// <summary>The box on the second paper, by its index in the paper's fields.</summary>
    public readonly int FieldB;

    /// <summary>The category the two boxes share.</summary>
    public readonly ClueCategory Category;

    /// <summary>Creates a contradiction.</summary>
    public PaperContradiction(int documentA, int fieldA, int documentB, int fieldB, ClueCategory category)
    {
        DocumentA = documentA;
        FieldA = fieldA;
        DocumentB = documentB;
        FieldB = fieldB;
        Category = category;
    }
}

/// <summary>
/// The paper cross check (traveller types L4, §6.2; Saleh's Q3: papers
/// prove, answers hint): the agency's forms are the agency's word, so two of
/// a traveller's papers that disagree on one compared category prove one of
/// them forged. One rule serves the cross proof (DiscrepancyLog.Prove, when
/// one side is a tell) and the Analysis Scanner (PaperAnalysis, the PC
/// spec's SC4), so the scanner can never mark a pair the player cannot log.
/// Pure.
/// </summary>
public static class PaperChecks
{
    /// <summary>
    /// True when two papers are compared on <paramref name="category"/>: every
    /// category but a name (never a tell, Forgery) and the directive-only
    /// dates (read against the calendar, Forgery.IsDirectiveOnly).
    /// </summary>
    public static bool IsCompared(ClueCategory category) =>
        category != ClueCategory.Name && !Forgery.IsDirectiveOnly(category);

    /// <summary>
    /// The one rule: two boxes contradict when they share a compared category,
    /// both state a value (a blank box states nothing) and their values are
    /// not the same value (Values.Match).
    /// </summary>
    public static bool Contradict(ClueCategory categoryA, string valueA, ClueCategory categoryB, string valueB) =>
        categoryA == categoryB && IsCompared(categoryA) && !string.IsNullOrWhiteSpace(valueA) && !string.IsNullOrWhiteSpace(valueB) && !Values.Match(valueA, valueB);

    /// <summary>
    /// Every pair of boxes on two different papers of <paramref name="documents"/>
    /// (each paper's fields in form order) that <see cref="Contradict"/>, in
    /// paper order then field order: for each paper, each of its boxes
    /// against every later paper's boxes. A paper never contradicts itself.
    /// Null papers and null boxes are skipped; empty for an honest set, since
    /// an honest traveller has one value per category on every form.
    /// </summary>
    public static List<PaperContradiction> Contradictions(IReadOnlyList<IReadOnlyList<DocumentField>> documents)
    {
        var found = new List<PaperContradiction>();
        if (documents == null)
            return found;

        for (int a = 0; a < documents.Count; a++)
        {
            IReadOnlyList<DocumentField> paperA = documents[a];
            if (paperA == null)
                continue;

            for (int i = 0; i < paperA.Count; i++)
            {
                DocumentField boxA = paperA[i];
                if (boxA == null)
                    continue;

                for (int b = a + 1; b < documents.Count; b++)
                {
                    IReadOnlyList<DocumentField> paperB = documents[b];
                    if (paperB == null)
                        continue;

                    for (int j = 0; j < paperB.Count; j++)
                    {
                        DocumentField boxB = paperB[j];
                        if (boxB != null && Contradict(boxA.category, boxA.value, boxB.category, boxB.value))
                            found.Add(new PaperContradiction(a, i, b, j, boxA.category));
                    }
                }
            }
        }

        return found;
    }
}
