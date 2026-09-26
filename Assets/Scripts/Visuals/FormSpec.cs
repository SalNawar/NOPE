using System;

/// <summary>
/// The blocks a form is made of (PC spec FO2). Serialized in every form
/// asset as an int, so the values are fixed: 14 (Masthead) and 15 (Headline)
/// are held for the Internet's sites.
/// </summary>
public enum FormBlockKind
{
    /// <summary>The agency seal (behind), the agency and programme lines, the form number and the title.</summary>
    Header = 0,

    /// <summary>A numbered heading bar ("1  DISPLACED PERSON"): the block's text on a band.</summary>
    Section = 1,

    /// <summary>A row of boxed fields on the 12-column grid (cells); a cell may span rows (the photo).</summary>
    FieldRow = 2,

    /// <summary>A record's groups (FormData.Groups): each a numbered section of boxes, two to a row, a value too long for half a row across it; each row a slot of the block's slot.</summary>
    RecordGroups = 3,

    /// <summary>Options with a box each; the one equal to the block's field's value is ticked.</summary>
    Checkboxes = 4,

    /// <summary>A header row (columns, shares) and the rows of the block's slot, each row a slot; a row of one cell in a table of several columns is a heading across it (a band, no slot).</summary>
    Table = 5,

    /// <summary>A paragraph: the block's text, else its slot's content.</summary>
    Paragraph = 6,

    /// <summary>The block's field (on a page kind, its slot's text) as a signatory's hand over a rule (UNSIGNED when blank) and the block's text as its caption.</summary>
    Signature = 7,

    /// <summary>The issuing facsimile line: the block's text, else "Issued by" and the agency.</summary>
    Issued = 8,

    /// <summary>The barcode drawn from the serial, and the serial under it.</summary>
    Barcode = 9,

    /// <summary>Fine print: the block's text.</summary>
    FinePrint = 10,

    /// <summary>The dashed box captioned FOR OFFICIAL USE · DESK STAMP, where the verdict ink lands.</summary>
    StampArea = 11,

    /// <summary>The standard foot: a rule, the issuing line over the barcode and serial beside the stamp area, and the block's text as fine print.</summary>
    Footer = 12,

    /// <summary>The next page starts here (fixed pages); PageOf counts pages at these.</summary>
    PageBreak = 13
}

/// <summary>The named content slots a form's cells and blocks read.</summary>
public static class FormSlots
{
    /// <summary>The traveller's 4:5 photo (a cell).</summary>
    public const string Photo = "photo";
}

/// <summary>One box of a FieldRow (PC spec FO2): a template field, or a named slot such as the photo.</summary>
[Serializable]
public sealed class FormCell
{
    /// <summary>The template field shown here (its index in the template's fields), or -1.</summary>
    public int field = -1;

    /// <summary>A named content slot ("photo", or a page kind's "to", "date", ...) when the cell shows no field.</summary>
    public string slot = string.Empty;

    /// <summary>The box's label when it is not its field's own label.</summary>
    public string caption = string.Empty;

    /// <summary>The columns it spans, of 12.</summary>
    public int span = 12;

    /// <summary>The rows it spans (the photo spans 2); the next rows' cells fill the columns it leaves free.</summary>
    public int rows = 1;

    /// <summary>True for the photo's cell.</summary>
    public bool IsPhoto => slot == FormSlots.Photo;
}

/// <summary>One block of a form (PC spec FO2); which fields matter depends on its kind.</summary>
[Serializable]
public sealed class FormBlock
{
    /// <summary>What the block is.</summary>
    public FormBlockKind kind;

    /// <summary>The section title, paragraph, fine print, caption or issuing line: printed English (forms are diegetic).</summary>
    public string text = string.Empty;

    /// <summary>A FieldRow's boxes, left to right.</summary>
    public FormCell[] cells = new FormCell[0];

    /// <summary>A Table's column heads.</summary>
    public string[] columns = new string[0];

    /// <summary>A Table's column widths as shares of the content width (missing or zero: equal shares).</summary>
    public float[] shares = new float[0];

    /// <summary>A Checkboxes block's options.</summary>
    public string[] options = new string[0];

    /// <summary>The template field a Checkboxes or Signature block shows, or -1.</summary>
    public int field = -1;

    /// <summary>The content slot a Table's rows, a Paragraph's text, a page kind's Signature or a RecordGroups block's picks come from.</summary>
    public string slot = string.Empty;
}

/// <summary>
/// A form (PC spec FO1-FO4): its blocks top to bottom. A document's form lives
/// on its DocumentTemplateSO and prints the template's number and name; a PC
/// page kind's form carries its own number and title and flows (its height
/// grows with its rows). Where a field sits is where its form places it:
/// PageOf is the page CaseFactory copies to DocumentField.page.
/// </summary>
[Serializable]
public sealed class FormSpec
{
    /// <summary>A page kind's form number ("TC-930"); blank on a document's form, which prints its template's.</summary>
    public string formNumber = string.Empty;

    /// <summary>A page kind's title ("DEVIATION REPORT"); blank on a document's form, which prints its template's name.</summary>
    public string title = string.Empty;

    /// <summary>True for a document: pages of the paper's aspect. False for a PC page kind, which flows.</summary>
    public bool fixedPage = true;

    /// <summary>The blocks, top to bottom.</summary>
    public FormBlock[] blocks = new FormBlock[0];

    /// <summary>How many pages the form has (one more than its page breaks).</summary>
    public int PageCount
    {
        get
        {
            int pages = 1;
            foreach (FormBlock b in blocks ?? new FormBlock[0])
                if (b != null && b.kind == FormBlockKind.PageBreak)
                    pages++;
            return pages;
        }
    }

    /// <summary>The page the form first places template field <paramref name="field"/> on (pages split at PageBreak), or -1 when it never places it.</summary>
    public int PageOf(int field)
    {
        int page = 0;
        foreach (FormBlock b in blocks ?? new FormBlock[0])
        {
            if (b == null)
                continue;
            if (b.kind == FormBlockKind.PageBreak)
                page++;
            else if (field >= 0 && Array.IndexOf(FieldsOf(b), field) >= 0)
                return page;
        }
        return -1;
    }

    /// <summary>The template fields a block shows: a FieldRow's cells' fields, a Checkboxes or Signature block's field (none for a null block).</summary>
    public static int[] FieldsOf(FormBlock block)
    {
        if (block == null)
            return new int[0];
        if (block.kind == FormBlockKind.FieldRow)
        {
            var fields = new System.Collections.Generic.List<int>();
            foreach (FormCell c in block.cells ?? new FormCell[0])
                if (c != null && c.field >= 0)
                    fields.Add(c.field);
            return fields.ToArray();
        }
        return (block.kind == FormBlockKind.Checkboxes || block.kind == FormBlockKind.Signature) && block.field >= 0 ? new[] { block.field } : new int[0];
    }
}
