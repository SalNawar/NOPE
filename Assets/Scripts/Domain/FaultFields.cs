using System;
using System.Collections.Generic;

/// <summary>
/// The cheat menu's "reveal faults" (Saleh 2026-10-06: "reveal the current
/// traveller's faults (highlight them)"): which boxes of a traveller's papers
/// show what is wrong, so the desk can mark them as it marks a difference the
/// clerk found (DeskInspect.Reveal). A box shows a fault when it holds a
/// forger's false value (a RecordTell: its paper, its category), when it is an
/// anachronism, or when its value is one a citation for the traveller's fault
/// names (CitationFacts.Values: the expired date, the closed destination, the
/// unsigned signature). Pure: no paper, no record or speech is read twice.
/// </summary>
public static class FaultFields
{
    /// <summary>
    /// The (paper, box) pairs of <paramref name="documents"/> (each paper's
    /// fields, in paper order) that show a fault: each of
    /// <paramref name="tells"/>' boxes (the first box of its category on its
    /// paper, the one printing its value when there is one), every
    /// anachronism, and every box whose value equals one of
    /// <paramref name="citedValues"/> (trimmed, ignoring case; blank values
    /// name nothing). Paper order, then box order, each pair once.
    /// </summary>
    public static List<(int document, int field)> Of(IReadOnlyList<IReadOnlyList<DocumentField>> documents, IEnumerable<RecordTell> tells,
                                                     IEnumerable<CitationValue> citedValues)
    {
        var found = new SortedSet<(int, int)>();
        if (documents == null)
            return new List<(int, int)>();

        foreach (RecordTell tell in tells ?? Array.Empty<RecordTell>())
        {
            IReadOnlyList<DocumentField> fields = tell.Document >= 0 && tell.Document < documents.Count ? documents[tell.Document] : null;
            int box = BoxOf(fields, tell.Category, tell.Value);
            if (box >= 0)
                found.Add((tell.Document, box));
        }

        var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (CitationValue cited in citedValues ?? Array.Empty<CitationValue>())
            if (!string.IsNullOrWhiteSpace(cited.Value))
                values.Add(cited.Value.Trim());

        for (int d = 0; d < documents.Count; d++)
        {
            IReadOnlyList<DocumentField> fields = documents[d];
            if (fields == null)
                continue;
            for (int f = 0; f < fields.Count; f++)
            {
                DocumentField field = fields[f];
                if (field != null && (field.isAnachronism || (!string.IsNullOrWhiteSpace(field.value) && values.Contains(field.value.Trim()))))
                    found.Add((d, f));
            }
        }
        return new List<(int, int)>(found);
    }

    /// <summary>The box of <paramref name="fields"/> of <paramref name="category"/> printing <paramref name="value"/>, else the first of that category; -1 when none.</summary>
    private static int BoxOf(IReadOnlyList<DocumentField> fields, ClueCategory category, string value)
    {
        if (fields == null)
            return -1;
        int first = -1;
        for (int i = 0; i < fields.Count; i++)
        {
            DocumentField field = fields[i];
            if (field == null || field.category != category)
                continue;
            if (string.Equals(field.value?.Trim(), value?.Trim(), StringComparison.OrdinalIgnoreCase))
                return i;
            if (first < 0)
                first = i;
        }
        return first;
    }
}
