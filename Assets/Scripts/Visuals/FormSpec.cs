using System;

/// <summary>
/// The blocks a form is made of (PC spec FO2). Serialized in every form
/// asset as an int, so the values are fixed (16 is SealGrid; 17, 19 and 20 the
/// travel documents': Fold, Visa, Watermark): 14 (Masthead) and 15 (Headline)
/// are held for the Internet's sites; 18 was the passport's machine-readable
/// zone (retired 2026-10-06: Saleh found it confusing; never reuse it).
/// </summary>
public enum FormBlockKind
{
    /// <summary>The agency and programme lines, the form number and the title; the seal faint behind them, or, when the block names a field (a document's Seal field, the document design spec D4), the issuing office's seal printed at the header's right as a pickable box.</summary>
    Header = 0,

    /// <summary>A numbered heading bar ("1  DISPLACED PERSON"): the block's text on a band.</summary>
    Section = 1,

    /// <summary>A row of boxed fields on the 12-column grid (cells); a cell may span rows (the photo).</summary>
    FieldRow = 2,

    /// <summary>A record's groups (FormData.Groups): each a numbered section of boxes, two to a row, a value too long for half a row across it; each row a slot of the block's slot.</summary>
    RecordGroups = 3,

    /// <summary>Options with a box each; the one equal to the block's field's value is ticked.</summary>
    Checkboxes = 4,

    /// <summary>A header row (columns, shares; the heads in the label style at the cells' size) and the rows of the block's slot, each row a slot; a row of one cell in a table of several columns is a heading across it (a band, no slot).</summary>
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
    PageBreak = 13,

    /// <summary>The Seal Register's grid (the document design spec, D4): FormData.Seals, three to a row, each seal pictured over its office's name, each a slot of the block's slot (its Row the seal's place).</summary>
    SealGrid = 16,

    /// <summary>A booklet's fold (the travel documents spec, TD3): the spine across the page at the block's place (shares[0], a share of the page's height; none: under the pen), the facing page's blocks below it.</summary>
    Fold = 17,

    /// <summary>A visa page (TD3): a dashed stamp area across the content from the pen down to the page's bottom margin (at least 1.25 of the style's stamp height), the block's text its caption inside its top: where the APPROVED or DENIED stamp lands.</summary>
    Visa = 19,

    /// <summary>A watermark (TD1): the seal of the block's field (a letterhead's issuing office), else the holder's nation's emblem (FormData.Emblem), large and faint in the middle of the room below the pen, under every box; it takes no room.</summary>
    Watermark = 20
}

/// <summary>The named content slots a form's cells and blocks read.</summary>
public static class FormSlots
{
    /// <summary>The traveller's 4:5 photo (a cell; a document's photo cell also names its Photo field, so the photo is a pickable box: the document design spec, D8).</summary>
    public const string Photo = "photo";

    /// <summary>The Seal Register's seals (a SealGrid block's slot; FormData.Seals).</summary>
    public const string Seals = "seals";

    /// <summary>A card's chip (a cell; the travel documents spec, TD1): drawn by the renderers' strokes, nothing to pick.</summary>
    public const string Chip = "chip";
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

    /// <summary>True for a card's chip cell.</summary>
    public bool IsChip => slot == FormSlots.Chip;
}

/// <summary>One block of a form (PC spec FO2); which fields matter depends on its kind.</summary>
[Serializable]
public sealed class FormBlock
{
    /// <summary>What the block is.</summary>
    public FormBlockKind kind;

    /// <summary>The section title, paragraph, fine print, caption, issuing line or a visa page's caption: printed English (forms are diegetic).</summary>
    public string text = string.Empty;

    /// <summary>A FieldRow's boxes, left to right.</summary>
    public FormCell[] cells = new FormCell[0];

    /// <summary>A Table's column heads.</summary>
    public string[] columns = new string[0];

    /// <summary>A Table's column widths as shares of the content width (missing or zero: equal shares); a Fold's place, shares[0], as a share of the page's height.</summary>
    public float[] shares = new float[0];

    /// <summary>A Checkboxes block's options.</summary>
    public string[] options = new string[0];

    /// <summary>The template field a Checkboxes, Signature or Header block shows (a header's: the paper's seal), or -1; a Watermark's: the field whose seal it pictures (it does not place the field).</summary>
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

    /// <summary>A page kind printed across (landscape): its width is the page's long side, so its page height H is the width times the aspect, not the width over it, and a wide table keeps a portrait page's type sizes (the statement's eight columns). Documents are portrait.</summary>
    public bool landscape;

    /// <summary>The blocks, top to bottom.</summary>
    public FormBlock[] blocks = new FormBlock[0];

    /// <summary>The form's silhouette (the document design spec, D1): its frame, colours, aspect and size on the desk.</summary>
    public FormLook look = new FormLook();

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

    /// <summary>The template fields a block shows: a FieldRow's cells' fields, a Checkboxes, Signature or Header block's field (a header's is its seal; none for a null block).</summary>
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
        return (block.kind == FormBlockKind.Checkboxes || block.kind == FormBlockKind.Signature || block.kind == FormBlockKind.Header) && block.field >= 0 ? new[] { block.field } : new int[0];
    }
}
