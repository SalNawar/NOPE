using System.Collections.Generic;

/// <summary>One line of a reference book's page: an era heading, or a fact row (the claimed row marked).</summary>
public readonly struct BookLine
{
    /// <summary>The fact row (default for a heading).</summary>
    public readonly FactRow Row;

    /// <summary>A heading's era id; null for a row.</summary>
    public readonly string HeadingEraId;

    /// <summary>True for the claimed place's row, listed first.</summary>
    public readonly bool IsClaimed;

    /// <summary>True for an era heading.</summary>
    public bool IsHeading => HeadingEraId != null;

    private BookLine(FactRow row, string headingEraId, bool isClaimed)
    {
        Row = row;
        HeadingEraId = headingEraId;
        IsClaimed = isClaimed;
    }

    /// <summary>A fact row.</summary>
    public static BookLine Of(FactRow row, bool isClaimed = false) => new BookLine(row, null, isClaimed);

    /// <summary>An era heading.</summary>
    public static BookLine Heading(string eraId) => new BookLine(default, eraId, false);
}

/// <summary>
/// How a reference book lays out today's rows (traveller types C3, Saleh's
/// Q9; the PC spec §2.6). Pure, so the order is tested headless.
/// </summary>
public static class BookLines
{
    /// <summary>
    /// A book's lines. Ungrouped: the rows as they are. Grouped by era (the
    /// Costume Guide): the claimed place's row first, marked (when it is in
    /// the rows), then an era heading before each era's other rows, the
    /// claimed era first and the rest in <paramref name="eraOrder"/> (eras
    /// not in it last, in the order first seen), each era's rows in table
    /// order; an era with no row left gets no heading. Null lists count as
    /// empty.
    /// </summary>
    public static List<BookLine> Arrange(IReadOnlyList<FactRow> rows, bool groupByEra, IReadOnlyList<string> eraOrder,
                                         string claimNationId, string claimEraId)
    {
        var lines = new List<BookLine>();
        if (rows == null)
            return lines;

        if (!groupByEra)
        {
            foreach (FactRow row in rows)
                lines.Add(BookLine.Of(row));
            return lines;
        }

        int claimed = -1;
        for (int i = 0; i < rows.Count && claimed < 0; i++)
            if (claimEraId != null && rows[i].NationId == claimNationId && rows[i].EraId == claimEraId)
                claimed = i;
        if (claimed >= 0)
            lines.Add(BookLine.Of(rows[claimed], true));

        var eras = new List<string>();
        if (claimEraId != null)
            eras.Add(claimEraId);
        foreach (string era in eraOrder ?? System.Array.Empty<string>())
            if (era != null && !eras.Contains(era))
                eras.Add(era);
        foreach (FactRow row in rows)
            if (!eras.Contains(row.EraId))
                eras.Add(row.EraId);

        foreach (string era in eras)
        {
            bool headed = false;
            for (int i = 0; i < rows.Count; i++)
            {
                if (i == claimed || rows[i].EraId != era)
                    continue;
                if (!headed)
                {
                    lines.Add(BookLine.Heading(era));
                    headed = true;
                }
                lines.Add(BookLine.Of(rows[i]));
            }
        }

        return lines;
    }
}
