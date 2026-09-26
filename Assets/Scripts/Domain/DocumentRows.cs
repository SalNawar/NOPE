using System.Collections.Generic;

/// <summary>One row of a document as every view shows it: the field and its index in the document's field list.</summary>
public readonly struct DocumentRow
{
    /// <summary>A row of field <paramref name="index"/>.</summary>
    public DocumentRow(int index, DocumentField field)
    {
        Index = index;
        Field = field;
    }

    /// <summary>The field's index in the document's field list (its pick key: PickKeys.Field).</summary>
    public int Index { get; }

    /// <summary>The field (never null).</summary>
    public DocumentField Field { get; }
}

/// <summary>
/// The one row source of a document (piece 10): the scanned page on the PC
/// (a page at a time) and the paper on the desk (every page at once) read
/// their rows here, so both show the same labels and values in the same order.
/// </summary>
public static class DocumentRows
{
    /// <summary>
    /// Every field, page by page (page ascending, then authored order within a
    /// page; a stable order), nulls skipped. A null list gives none.
    /// </summary>
    public static IReadOnlyList<DocumentRow> Ordered(IReadOnlyList<DocumentField> fields)
    {
        var rows = new List<DocumentRow>();
        int pages = fields != null ? PageCount(fields) : 0;
        for (int page = 0; page < pages; page++)
            AppendPage(fields, page, rows);
        return rows;
    }

    /// <summary>The fields of one page in authored order, nulls skipped. A null list gives none.</summary>
    public static IReadOnlyList<DocumentRow> OnPage(IReadOnlyList<DocumentField> fields, int page)
    {
        var rows = new List<DocumentRow>();
        AppendPage(fields, page, rows);
        return rows;
    }

    /// <summary>Appends the rows of one page in authored order, nulls skipped (the one loop both views read, audit R1-013).</summary>
    private static void AppendPage(IReadOnlyList<DocumentField> fields, int page, List<DocumentRow> rows)
    {
        int count = fields != null ? fields.Count : 0;
        for (int i = 0; i < count; i++)
            if (fields[i] != null && fields[i].page == page)
                rows.Add(new DocumentRow(i, fields[i]));
    }

    /// <summary>The pages a document spans: its highest page + 1, at least 1 (no fields, or a null list, give 1).</summary>
    public static int PageCount(IReadOnlyList<DocumentField> fields)
    {
        int max = 1;
        int count = fields != null ? fields.Count : 0;
        for (int i = 0; i < count; i++)
            if (fields[i] != null && fields[i].page + 1 > max)
                max = fields[i].page + 1;
        return max;
    }
}
