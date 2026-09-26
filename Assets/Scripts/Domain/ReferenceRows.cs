using System.Collections.Generic;

/// <summary>One line of a reference book's register: an era's heading, or one of today's fact rows (the claimed place's flagged).</summary>
public readonly struct ReferenceLine
{
    private ReferenceLine(string eraHeading, FactRow row, bool claimed)
    {
        EraHeading = eraHeading;
        Row = row;
        Claimed = claimed;
    }

    /// <summary>The era id a heading line names; null for a row line.</summary>
    public string EraHeading { get; }

    /// <summary>The fact row of a row line (today's row, unchanged: its pick is the book row's).</summary>
    public FactRow Row { get; }

    /// <summary>True for the claimed place's row.</summary>
    public bool Claimed { get; }

    /// <summary>True for an era's heading line.</summary>
    public bool IsHeading => EraHeading != null;

    /// <summary>An era's heading line.</summary>
    public static ReferenceLine Heading(string eraId) => new ReferenceLine(eraId, default, false);

    /// <summary>A row line (<paramref name="claimed"/> for the claimed place's).</summary>
    public static ReferenceLine ForRow(FactRow row, bool claimed) => new ReferenceLine(null, row, claimed);
}

/// <summary>
/// A reference book's register in the Investigation app (the PC redesign AP5,
/// §2.6; the traveller-types spec's Q9: the Costume Guide by era). The claimed
/// place's row comes first, flagged; the others follow in today's order.
/// "Claimed place only" shows that row alone (every row when the claim is not
/// in today's rows). A book grouped by era puts the others under an era
/// heading each: the claimed place's era first, then the eras in their order,
/// the present's era last (it closes the page); an era with no other place
/// gets no heading. The rows are today's FactTable rows, so a row's pick is
/// unchanged. Pure; the Reference view draws it.
/// </summary>
public static class ReferenceRows
{
    /// <summary>
    /// The book's lines from today's <paramref name="rows"/> for the claim
    /// (<paramref name="claimedNationId"/>, <paramref name="claimedEraId"/>;
    /// null ids for none). <paramref name="eraOrder"/> lists the era ids in
    /// order (an era missing from it comes after the listed ones, before the
    /// present, in the order it first appears); <paramref name="presentEraId"/>
    /// is the present's era (null for none).
    /// </summary>
    public static IReadOnlyList<ReferenceLine> Arrange(IReadOnlyList<FactRow> rows, string claimedNationId, string claimedEraId, bool claimedOnly,
                                                       bool groupByEra, IReadOnlyList<string> eraOrder, string presentEraId)
    {
        var lines = new List<ReferenceLine>();
        if (rows == null || rows.Count == 0)
            return lines;

        int claimed = -1;
        for (int i = 0; i < rows.Count && claimedNationId != null && claimedEraId != null; i++)
            if (rows[i].NationId == claimedNationId && rows[i].EraId == claimedEraId)
            {
                claimed = i;
                break;
            }

        if (claimed >= 0)
        {
            lines.Add(ReferenceLine.ForRow(rows[claimed], true));
            if (claimedOnly)
                return lines;
        }

        if (!groupByEra)
        {
            for (int i = 0; i < rows.Count; i++)
                if (i != claimed)
                    lines.Add(ReferenceLine.ForRow(rows[i], false));
            return lines;
        }

        foreach (string era in EraSequence(rows, claimed >= 0 ? claimedEraId : null, eraOrder, presentEraId))
        {
            bool headed = false;
            for (int i = 0; i < rows.Count; i++)
            {
                if (i == claimed || rows[i].EraId != era)
                    continue;
                if (!headed)
                {
                    lines.Add(ReferenceLine.Heading(era));
                    headed = true;
                }
                lines.Add(ReferenceLine.ForRow(rows[i], false));
            }
        }
        return lines;
    }

    /// <summary>The eras in page order: the claimed era, the listed eras, the unlisted ones as they first appear, the present.</summary>
    private static List<string> EraSequence(IReadOnlyList<FactRow> rows, string claimedEra, IReadOnlyList<string> eraOrder, string presentEra)
    {
        var sequence = new List<string>();
        void Add(string era)
        {
            if (era != null && era != presentEra && !sequence.Contains(era))
                sequence.Add(era);
        }

        Add(claimedEra);
        if (eraOrder != null)
            foreach (string era in eraOrder)
                Add(era);
        foreach (FactRow row in rows)
            Add(row.EraId);
        if (presentEra != null)
            sequence.Add(presentEra);
        return sequence;
    }
}
