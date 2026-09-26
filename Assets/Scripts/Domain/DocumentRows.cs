using System.Collections.Generic;

/// <summary>
/// One row of a document as every view shows it: the field, its index in the
/// document's field list, and its place among the rows in the tongue (piece
/// 9's row stagger: a row's flip waits for the rows in the tongue above it).
/// </summary>
public readonly struct DocumentRow
{
    /// <summary>A row of field <paramref name="index"/>, the <paramref name="tongueRow"/>-th row in the tongue before it.</summary>
    public DocumentRow(int index, DocumentField field, int tongueRow)
    {
        Index = index;
        Field = field;
        TongueRow = tongueRow;
    }

    /// <summary>The field's index in the document's field list (its pick key: PickKeys.Field).</summary>
    public int Index { get; }

    /// <summary>The field (never null).</summary>
    public DocumentField Field { get; }

    /// <summary>How many rows in the tongue come before this one (the flip's stagger); rows not in the tongue do not count.</summary>
    public int TongueRow { get; }
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
    /// page; a stable order), nulls skipped; TongueRow counts the rows in the
    /// tongue (Translation.InTongue) before it on the whole document. A null
    /// list gives none.
    /// </summary>
    public static IReadOnlyList<DocumentRow> Ordered(IReadOnlyList<DocumentField> fields)
    {
        var rows = new List<DocumentRow>();
        if (fields == null)
            return rows;

        int pages = PageCount(fields);
        int tongue = 0;
        for (int page = 0; page < pages; page++)
            for (int i = 0; i < fields.Count; i++)
            {
                DocumentField f = fields[i];
                if (f == null || f.page != page)
                    continue;
                rows.Add(new DocumentRow(i, f, tongue));
                if (Translation.InTongue(f.category))
                    tongue++;
            }
        return rows;
    }

    /// <summary>The fields of one page in authored order, nulls skipped; TongueRow counts within the page (the scanned page's stagger). A null list gives none.</summary>
    public static IReadOnlyList<DocumentRow> OnPage(IReadOnlyList<DocumentField> fields, int page)
    {
        var rows = new List<DocumentRow>();
        int tongue = 0;
        int count = fields != null ? fields.Count : 0;
        for (int i = 0; i < count; i++)
        {
            DocumentField f = fields[i];
            if (f == null || f.page != page)
                continue;
            rows.Add(new DocumentRow(i, f, tongue));
            if (Translation.InTongue(f.category))
                tongue++;
        }
        return rows;
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

    /// <summary>True when a field is in the tongue (Translation.InTongue): a written reveal has something to flip. False for no fields.</summary>
    public static bool HasTongue(IReadOnlyList<DocumentField> fields)
    {
        int count = fields != null ? fields.Count : 0;
        for (int i = 0; i < count; i++)
            if (fields[i] != null && Translation.InTongue(fields[i].category))
                return true;
        return false;
    }
}
