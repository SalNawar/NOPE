using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

/// <summary>
/// What one form shows (PC spec §6.1), built by the caller: the header's
/// words, the serial, whether there is a photo, each template field's label
/// and shown value, and a page kind's slot contents. The form's fixed words
/// (labels, titles, captions, section heads, column heads, fine print) print
/// in the reading language through <see cref="Words"/> (Saleh 2026-10-07: "I
/// want the language to change on all documents and the apps"); the values,
/// the serial and the form number always print as they are.
/// </summary>
public sealed class FormData
{
    /// <summary>The agency's printed name ("TEMPORAL CUSTOMS").</summary>
    public string Agency = string.Empty;

    /// <summary>The programme line under it ("Debt Relief Departures").</summary>
    public string Programme = string.Empty;

    /// <summary>The form number printed at the header's right ("TC-610").</summary>
    public string FormNumber = string.Empty;

    /// <summary>The form's title (printed in capitals).</summary>
    public string Title = string.Empty;

    /// <summary>The paper's serial ("TC-610/583021"); blank: no barcode and no serial.</summary>
    public string Serial = string.Empty;

    /// <summary>True when the photo cell shows the traveller's photo.</summary>
    public bool HasPhoto;

    /// <summary>Each template field's label, by field index.</summary>
    public IReadOnlyList<string> FieldLabels = Array.Empty<string>();

    /// <summary>Each template field's shown value, by field index.</summary>
    public IReadOnlyList<string> FieldValues = Array.Empty<string>();

    /// <summary>
    /// The value each field's box keeps room for, by field index (the document
    /// design spec, D2: a document's longest value, FormLayout.Probe's): a box
    /// is as tall as this value needs, whatever it shows, so no field moves
    /// when a value changes; empty (a PC page kind) lets boxes fit their values.
    /// </summary>
    public IReadOnlyList<string> FieldReserve = Array.Empty<string>();

    /// <summary>
    /// The fields not introduced yet, by field index (the desk-first redesign,
    /// item 3: "hide everything until introduced"; Introductions.ShowsField):
    /// a hidden field's box, label, value or seal is not drawn and cannot be
    /// picked, but keeps its place, so nothing moves when it arrives (D2).
    /// Empty or shorter than the fields: every field shows.
    /// </summary>
    public IReadOnlyList<bool> FieldHidden = Array.Empty<bool>();

    /// <summary>The Seal Register's seals (a SealGrid block; the document design spec, D4): each office's name and its seal's value (its description, drawn by the renderers).</summary>
    public IReadOnlyList<(string Caption, string Value)> Seals = Array.Empty<(string, string)>();

    /// <summary>A booklet's cover colour ("#RRGGBB", the holder's nation's; the travel documents spec, TD3); blank: the look's accent.</summary>
    public string Cover = string.Empty;

    /// <summary>The holder's nation's id ("egypt"; TD3): a passport's page art for that nation is tried first (ArtSlots.PaperFaces); blank: none.</summary>
    public string Issuer = string.Empty;

    /// <summary>The holder's nation's emblem (an EmblemShapes name; TD3), at a booklet header's left and in an emblem watermark; blank: none.</summary>
    public string Emblem = string.Empty;

    /// <summary>The holder's nation's three-letter code ("EGY"; countries[].passport.code), printed under a booklet's emblem (Papers, Please's issuing nation; Saleh 2026-10-06 replaced the machine-readable zone with plain fields); blank: none.</summary>
    public string NationCode = string.Empty;

    /// <summary>A page kind's text slots (a Paragraph's slot, a named cell's slot).</summary>
    public IReadOnlyDictionary<string, string> Text = new Dictionary<string, string>();

    /// <summary>A page kind's table rows by slot, each row its cells' texts (one cell in a table of several columns: a heading row).</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string[]>> Rows = new Dictionary<string, IReadOnlyList<string[]>>();

    /// <summary>A record's groups, for a RecordGroups block (the traveller-types spec's R1: a record's shape is data).</summary>
    public IReadOnlyList<FormGroup> Groups = Array.Empty<FormGroup>();

    /// <summary>
    /// The reading language's words for an English fixed word of the form
    /// (UiText.FormWords: its doc.* string), or null to print the English as
    /// it is (English is read, or no culture's labels apply).
    /// </summary>
    public Func<string, string> Words;

    /// <summary>True when the form's fixed words print in a culture's language (<see cref="Words"/> is set).</summary>
    public bool Translated => Words != null;

    /// <summary><paramref name="english"/> in the reading language (<see cref="Words"/>), else as it is.</summary>
    public string Word(string english) => Words == null || string.IsNullOrEmpty(english) ? english : Words(english) ?? english;

    /// <summary>A copy (its lists shared).</summary>
    public FormData Copy() => (FormData)MemberwiseClone();

    /// <summary>What a PC page kind starts from: the agency's name and programme over <paramref name="spec"/>'s own form number and title (a caller may name the page otherwise: a book's register).</summary>
    public static FormData Page(FormSpec spec, string agency, string programme) => new FormData
    {
        Agency = agency ?? string.Empty,
        Programme = programme ?? string.Empty,
        FormNumber = spec != null ? spec.formNumber ?? string.Empty : string.Empty,
        Title = spec != null ? spec.title ?? string.Empty : string.Empty
    };
}

/// <summary>One group of a record's rows (a RecordGroups block prints it as a numbered section of boxes): its title and its (label, value) rows.</summary>
public sealed class FormGroup
{
    /// <summary>A group from its title and rows.</summary>
    public FormGroup(string title, IReadOnlyList<(string Label, string Value)> rows)
    {
        Title = title ?? string.Empty;
        Rows = rows ?? Array.Empty<(string, string)>();
    }

    /// <summary>The group's title ("Records", "Forms on file", "Travel"); printed in capitals after its number.</summary>
    public string Title { get; }

    /// <summary>The group's rows, each a box: its label and value.</summary>
    public IReadOnlyList<(string Label, string Value)> Rows { get; }
}

/// <summary>
/// A form's sizes (PC spec FO6, §6.3), in page heights H: a fixed page is H
/// tall, and a flow page takes H as the height of a document page at its
/// width. Held by FormStyleSO, shared by the desk paper and the PC.
/// </summary>
[Serializable]
public sealed class FormMetrics
{
    /// <summary>The page's width over its height (the desk paper's 0.26 x 0.34 m).</summary>
    public float aspect = 0.765f;

    /// <summary>The margin left and right of the content.</summary>
    public float marginX = 0.05f;

    /// <summary>The margin above the header.</summary>
    public float marginTop = 0.025f;

    /// <summary>The margin under the last block (a fixed page's content must end above it).</summary>
    public float marginBottom = 0.015f;

    /// <summary>The gutter between two columns of the 12-column grid.</summary>
    public float gutter = 0.012f;

    /// <summary>The gap under a row of boxes.</summary>
    public float rowGap = 0.005f;

    /// <summary>The gap under the header, a paragraph or a table.</summary>
    public float blockGap = 0.008f;

    /// <summary>The padding inside a box, a band and a table cell.</summary>
    public float boxPadding = 0.005f;

    /// <summary>The width of a box's outline and a rule.</summary>
    public float ruleWidth = 0.0025f;

    /// <summary>The agency line's size (bold capitals).</summary>
    public float agencySize = 0.030f;

    /// <summary>The programme line's size.</summary>
    public float programmeSize = 0.022f;

    /// <summary>The form number's size.</summary>
    public float formNumberSize = 0.026f;

    /// <summary>The title's size (bold capitals).</summary>
    public float titleSize = 0.052f;

    /// <summary>The smallest size a long title shrinks to, to stay on one line.</summary>
    public float titleFloor = 0.032f;

    /// <summary>A line of capitals (the agency line, the title, section heads, labels), in ems: capitals have no descenders, so they take less room than a measured line.</summary>
    public float capsLead = 1.05f;

    /// <summary>The serial's size.</summary>
    public float serialSize = 0.026f;

    /// <summary>A section head's size (bold capitals).</summary>
    public float sectionSize = 0.036f;

    /// <summary>A box's label size (small capitals).</summary>
    public float labelSize = 0.038f;

    /// <summary>A value's size (15.5 px on a paper held at 720p).</summary>
    public float valueSize = 0.049f;

    /// <summary>The smallest size a value shrinks to before it wraps.</summary>
    public float valueFloor = 0.042f;

    /// <summary>The most lines a value may take; a longer one is reported by Check.</summary>
    public int maxValueLines = 2;

    /// <summary>A table cell's and a checkbox option's size.</summary>
    public float cellSize = 0.034f;

    /// <summary>The smallest size a table cell (or a column head) shrinks to so that its longest word keeps one line in its column; a word wider than that breaks.</summary>
    public float cellFloor = 0.026f;

    /// <summary>A paragraph's size.</summary>
    public float paragraphSize = 0.034f;

    /// <summary>A caption's size (the issuing line, the stamp area's and a signature's caption).</summary>
    public float captionSize = 0.024f;

    /// <summary>Fine print's size.</summary>
    public float finePrintSize = 0.020f;

    /// <summary>The seal's side: faint behind the header, or the issuing office's seal at the header's right (the document design spec, D4) and each seal of the Seal Register.</summary>
    public float sealSize = 0.085f;

    /// <summary>The barcode's height.</summary>
    public float barcodeHeight = 0.03f;

    /// <summary>The barcode's width.</summary>
    public float barcodeWidth = 0.22f;

    /// <summary>The modules the barcode is drawn in (Barcode.Bars).</summary>
    public int barcodeModules = 60;

    /// <summary>The stamp area's width.</summary>
    public float stampWidth = 0.26f;

    /// <summary>The stamp area's least height.</summary>
    public float stampHeight = 0.07f;
}

/// <summary>Measures text as the renderer draws it (TMP's preferred height; tests fake it).</summary>
public interface ITextMeasure
{
    /// <summary>The height <paramref name="text"/> takes in <paramref name="role"/>'s style at <paramref name="size"/> (an em), wrapped at <paramref name="width"/>, in the same units.</summary>
    float Height(string text, FormTextRole role, float size, float width);
}

/// <summary>What a placed item is: the renderers draw each kind their way.</summary>
public enum FormItemKind
{
    /// <summary>A seal: its text is the seal's value (Seals.Describe's words, drawn by the renderers: the issuing office's seal, the Seal Register's), or blank for the agency's faint seal behind the header.</summary>
    Seal,

    /// <summary>A text (its role gives its style).</summary>
    Text,

    /// <summary>A box's outline (and its fill).</summary>
    Box,

    /// <summary>A printed line.</summary>
    Rule,

    /// <summary>The traveller's 4:5 photo.</summary>
    Photo,

    /// <summary>A checkbox; its text is FormLayout.Tick when ticked.</summary>
    Checkbox,

    /// <summary>One barcode bar.</summary>
    Bar,

    /// <summary>The dashed stamp area.</summary>
    StampArea,

    /// <summary>A filled band: a section head's bar, a table's head row.</summary>
    RowBand,

    /// <summary>A frame's band in the look's accent (the document design spec, D1).</summary>
    Stripe,

    /// <summary>One hole of a ticket frame's perforation.</summary>
    Perforation,

    /// <summary>A booklet's cover edge (the travel documents spec, TD1): its text is the cover colour (FormData.Cover; blank: the accent).</summary>
    Cover,

    /// <summary>A booklet's spine across the page (a Fold block).</summary>
    Spine,

    /// <summary>A card's chip (a chip cell).</summary>
    Chip,

    /// <summary>The holder's nation's emblem: its text is the emblem's name (EmblemShapes), drawn by the renderers in the cover colour.</summary>
    Emblem,

    /// <summary>A large faint mark under the page's boxes: its text is a seal's value (the issuing office's) or an emblem's name, drawn by the renderers.</summary>
    Watermark,

    /// <summary>A folded card's crease down the page (FormPaint shades it faintly over the boxes).</summary>
    Crease,

    /// <summary>A form drawn on its art (ArtLayout): the blank face's piece over its rectangle (ArtSlots.PaperBlank), hiding a baked label whose field is not introduced yet.</summary>
    Patch
}

/// <summary>A text's role: its style and colour class. Bold, capitals and small capitals follow it (FormTextStyles).</summary>
public enum FormTextRole
{
    /// <summary>The agency line.</summary>
    Agency,

    /// <summary>The programme line.</summary>
    Programme,

    /// <summary>The form number.</summary>
    FormNumber,

    /// <summary>The title.</summary>
    Title,

    /// <summary>The serial under the barcode.</summary>
    Serial,

    /// <summary>A section head.</summary>
    Section,

    /// <summary>A box's label, a table's column head.</summary>
    Label,

    /// <summary>A field's value, a signature.</summary>
    Value,

    /// <summary>A table cell, a checkbox option.</summary>
    Cell,

    /// <summary>A paragraph.</summary>
    Paragraph,

    /// <summary>A caption: the issuing line, the stamp area's, a signature's.</summary>
    Caption,

    /// <summary>Fine print.</summary>
    FinePrint,

    /// <summary>A signatory's hand on a form drawn on its art (ArtLayout): a signature, the authorising office (italic, in the value's ink).</summary>
    Hand
}

/// <summary>How a text sits in its rectangle.</summary>
public enum FormTextAlign
{
    /// <summary>At the left.</summary>
    Left,

    /// <summary>Centred.</summary>
    Centre,

    /// <summary>At the right.</summary>
    Right
}

/// <summary>The one table of a role's type style, read by the layout's measure and by both renderers.</summary>
public static class FormTextStyles
{
    /// <summary>True for the bold roles: the agency line, the title, the section heads and the box labels (small capitals at 12 px on a paper held at 720p need the weight to read, as drawn).</summary>
    public static bool IsBold(FormTextRole role) => role == FormTextRole.Agency || role == FormTextRole.Title || role == FormTextRole.Section || role == FormTextRole.Label;

    /// <summary>True for a hand (a signature on the art), drawn in italics.</summary>
    public static bool IsItalic(FormTextRole role) => role == FormTextRole.Hand;

    /// <summary>True for the labels, drawn in small capitals.</summary>
    public static bool IsSmallCaps(FormTextRole role) => role == FormTextRole.Label;

    /// <summary>True for the roles printed in capitals (the agency line, the title and section heads in capitals, the labels in small capitals): a line of them is FormMetrics.capsLead ems.</summary>
    public static bool IsCapitals(FormTextRole role) =>
        role == FormTextRole.Agency || role == FormTextRole.Title || role == FormTextRole.Section || role == FormTextRole.Label;
}

/// <summary>One placed part of a form, in form space (top-left origin, y down, the caller's units).</summary>
public readonly struct FormItem
{
    /// <summary>An item from its parts (<paramref name="ink"/>: a text's own ink, "#RRGGBB", or null for its role's).</summary>
    public FormItem(FormItemKind kind, FormTextRole role, FaceRect rect, int slot, string text, float size, FormTextAlign align, string ink = null)
    {
        Kind = kind;
        Role = role;
        Rect = rect;
        Slot = slot;
        Text = text;
        Size = size;
        Align = align;
        Ink = ink;
    }

    /// <summary>A text's own ink ("#RRGGBB": an art caption's, ArtCaption.ink), or null: its role's.</summary>
    public string Ink { get; }

    /// <summary>What it is.</summary>
    public FormItemKind Kind { get; }

    /// <summary>A text's role (Text items only).</summary>
    public FormTextRole Role { get; }

    /// <summary>Where it is.</summary>
    public FaceRect Rect { get; }

    /// <summary>The slot it belongs to, or -1.</summary>
    public int Slot { get; }

    /// <summary>A text's words; a ticked checkbox's FormLayout.Tick; else empty.</summary>
    public string Text { get; }

    /// <summary>A text's size (an em), in the caller's units.</summary>
    public float Size { get; }

    /// <summary>How a text sits in its rectangle.</summary>
    public FormTextAlign Align { get; }
}

/// <summary>One pickable place of a form, in reading order: a field's box, or a table's row.</summary>
public readonly struct FormSlot
{
    /// <summary>A slot from its parts (<paramref name="hidden"/>: a field not introduced yet, FormData.FieldHidden).</summary>
    public FormSlot(int index, int field, int row, string source, FaceRect hit, int page, bool hidden = false)
    {
        Index = index;
        Field = field;
        Row = row;
        Source = source;
        Hit = hit;
        Page = page;
        Hidden = hidden;
    }

    /// <summary>True for a hidden field's slot (FormData.FieldHidden): nothing of it is drawn, its box hits nothing, and no pick, highlight or ↗ is armed on it.</summary>
    public bool Hidden { get; }

    /// <summary>Its place in reading order (its index in PlacedForm.Slots).</summary>
    public int Index { get; }

    /// <summary>The template field it shows, or -1 (a table row).</summary>
    public int Field { get; }

    /// <summary>A table row's index in its slot's rows, or -1.</summary>
    public int Row { get; }

    /// <summary>The content slot a table row or a named cell comes from; empty for a field.</summary>
    public string Source { get; }

    /// <summary>Where a click or a hover picks it (its box).</summary>
    public FaceRect Hit { get; }

    /// <summary>The page it is on.</summary>
    public int Page { get; }
}

/// <summary>A laid-out form (FormLayout.Layout).</summary>
public sealed class PlacedForm
{
    /// <summary>A placed form from its parts.</summary>
    public PlacedForm(float width, float height, float pageHeight, IReadOnlyList<FormItem> items, IReadOnlyList<FormSlot> slots, IReadOnlyList<float> pageTops, float unit = 0f)
    {
        Width = width;
        Height = height;
        PageHeight = pageHeight;
        Unit = unit > 0f ? unit : pageHeight;
        Items = items;
        Slots = slots;
        PageTops = pageTops;
    }

    /// <summary>The width it was laid out at.</summary>
    public float Width { get; }

    /// <summary>Its height: its pages on a fixed page, its content on a flow page.</summary>
    public float Height { get; }

    /// <summary>One page's height (width / aspect; width × aspect on a landscape page).</summary>
    public float PageHeight { get; }

    /// <summary>H, the unit of every size (FormLayout.PrintUnit): the width over the style's aspect (the page's height at the style's aspect; the travel documents spec, TD2).</summary>
    public float Unit { get; }

    /// <summary>The items in drawing order.</summary>
    public IReadOnlyList<FormItem> Items { get; }

    /// <summary>The slots in reading order.</summary>
    public IReadOnlyList<FormSlot> Slots { get; }

    /// <summary>Each page's top.</summary>
    public IReadOnlyList<float> PageTops { get; }
}

/// <summary>
/// The one form layout engine (PC spec FO1, §6.1): a FormSpec and its content
/// become placed items (texts, boxes, rules, bars, the photo, the seal) and
/// slots in reading order, at a width in the caller's units, with every size
/// in page heights (FO6). The desk paper (DeskDocument) and the PC (FormView)
/// draw the same placed form, so a paper and its scanned copy are one form.
/// FieldRows sit on a 12-column grid; a cell may span rows (the 4:5 photo
/// beside two boxes). A value keeps its size on one line, else shrinks to the
/// floor, where it may wrap to two lines; a document's box keeps the room its
/// longest value needs (FormData.FieldReserve, the document design spec D2),
/// so no field ever moves, and a value that would need more shrinks below
/// the floor instead. A form's look (FormLook) sets its page's aspect (its
/// print sized by its width, PrintUnit) and adds its frame's bands.
/// SlotAt is the hit test; Check is Build Office UI's and the validator's face
/// check (FO10). Pure and engine-free.
/// </summary>
public static class FormLayout
{
    /// <summary>The columns of a FieldRow's grid.</summary>
    public const int Columns = 12;

    /// <summary>What a ticked checkbox holds.</summary>
    public const string Tick = "X";

    /// <summary>What a passport's visa box carries as its stamp area's text (FormPaint draws it bolder: the one box a verdict stamp takes, Papers, Please's ENTRY VISA box).</summary>
    public const string VisaBox = "visa";

    /// <summary>What an empty signature line prints (a real fault on a waiver, traveller-types F3).</summary>
    public const string Unsigned = "UNSIGNED";

    /// <summary>The stamp area's caption.</summary>
    public const string StampCaption = "FOR OFFICIAL USE · DESK STAMP";

    /// <summary>The words a probe value is cut from: real word lengths, so it wraps like a value.</summary>
    private const string ProbeWords = "Middle Egyptian, hieroglyphs and a reed brush ";

    /// <summary>
    /// Lays out <paramref name="spec"/> showing <paramref name="data"/> at
    /// <paramref name="width"/> (the caller's units). Every size is in page
    /// heights, H = width / aspect (width × aspect on a landscape page; the
    /// aspect is the form's look's, else the style's): the
    /// wider the page, the larger its print. A document copy on the PC takes
    /// 520 u (H = 679 u); a page kind in a pane takes the pane's width, so its
    /// table cells reach 13 px at 720p from about 564 u. A page whose table
    /// rows carry a smart link's ↗ at their top right (FormView) passes the
    /// ↗'s side, <paramref name="rowLinkRoom"/>, in the caller's units: each
    /// table's last column then wraps its text that much short of the row's
    /// right edge (its padding counted), so the ↗ sits after the text, never
    /// over it; 0 (a document, the desk paper) keeps every column whole.
    /// </summary>
    /// A form whose fixed words print in a culture's language
    /// (FormData.Translated) is laid out from its words in that language
    /// (<see cref="Translate"/>), so it is measured as it prints.
    public static PlacedForm Layout(FormSpec spec, FormData data, float width, FormMetrics m, ITextMeasure measure, float rowLinkRoom = 0f)
    {
        if (ArtLayout.IsArt(spec))
            return Hide(ArtLayout.Place(spec, data, width, m), data);
        if (data != null && data.Translated)
            (spec, data) = Translate(spec, data);
        return Hide(new Placer(spec, data, width, m, measure, null, rowLinkRoom).Run(), data);
    }

    /// <summary>
    /// <paramref name="spec"/> and <paramref name="data"/> with every fixed
    /// word in the reading language (FormData.Word): the title, the agency and
    /// programme lines, the field labels, and the blocks' words (a section
    /// head, a paragraph's or fine print's own text, a signature's or the
    /// visa's caption, the issuing line, a table's column heads, a cell's
    /// caption); copies, the originals untouched. Values, slot texts, a
    /// record's groups and a checkbox's options (matched against a value) stay
    /// as they are.
    /// </summary>
    public static (FormSpec spec, FormData data) Translate(FormSpec spec, FormData data)
    {
        spec ??= new FormSpec();
        FormData d = data.Copy();
        d.Title = data.Word(data.Title);
        d.Agency = data.Word(data.Agency);
        d.Programme = data.Word(data.Programme);
        d.FieldLabels = (data.FieldLabels ?? Array.Empty<string>()).Select(data.Word).ToList();

        var s = new FormSpec
        {
            formNumber = spec.formNumber,
            title = data.Word(spec.title),
            fixedPage = spec.fixedPage,
            landscape = spec.landscape,
            look = spec.look,
            blocks = (spec.blocks ?? new FormBlock[0]).Select(b => b == null ? null : new FormBlock
            {
                kind = b.kind,
                text = TranslatesText(b.kind) ? data.Word(b.text) : b.text,
                cells = (b.cells ?? new FormCell[0]).Select(c => c == null ? null : new FormCell { field = c.field, slot = c.slot, caption = data.Word(c.caption), span = c.span, rows = c.rows }).ToArray(),
                columns = (b.columns ?? new string[0]).Select(data.Word).ToArray(),
                shares = b.shares,
                options = b.options,
                field = b.field,
                slot = b.slot
            }).ToArray()
        };
        return (s, d);
    }

    /// <summary>
    /// Every fixed English word <paramref name="spec"/> prints showing
    /// <paramref name="data"/> (each field shown, its values as given), in
    /// first-printed order, once each: what a culture's language translates
    /// (FormData.Words), so the content validator can require a doc.* string
    /// for each (an art paper's when it has a clean face).
    /// </summary>
    public static List<string> PrintedWords(FormSpec spec, FormData data)
    {
        var words = new List<string>();
        FormData d = (data ?? new FormData()).Copy();
        d.FieldHidden = Array.Empty<bool>();
        d.Words = w =>
        {
            if (!string.IsNullOrWhiteSpace(w) && !words.Contains(w))
                words.Add(w);
            return w;
        };
        spec ??= new FormSpec();
        if (ArtLayout.IsArt(spec))
        {
            ArtLayout.Place(spec, d, 1f, null);
            return words;
        }
        Translate(spec, d);
        foreach (FormBlock b in spec.blocks ?? new FormBlock[0])
        {
            if (b == null)
                continue;
            if (b.kind == FormBlockKind.Signature)
                d.Word(Unsigned);
            if (b.kind == FormBlockKind.StampArea || b.kind == FormBlockKind.Footer)
                d.Word(StampCaption);
            if ((b.kind == FormBlockKind.Issued && string.IsNullOrEmpty(b.text)) || b.kind == FormBlockKind.Footer)
                d.Word(IssuedBy);
        }
        return words;
    }

    /// <summary>True for a block kind whose text is a fixed word printed on the form (FormBlockKind's docs).</summary>
    private static bool TranslatesText(FormBlockKind kind) =>
        kind == FormBlockKind.Section || kind == FormBlockKind.Paragraph || kind == FormBlockKind.Signature || kind == FormBlockKind.Issued ||
        kind == FormBlockKind.FinePrint || kind == FormBlockKind.Footer || kind == FormBlockKind.Visa;

    /// <summary>
    /// <paramref name="form"/> without the fields <paramref name="data"/> hides
    /// (FormData.FieldHidden): their items are dropped and their slots keep
    /// their index and field but hit nothing, so every other field keeps its
    /// box and its slot number.
    /// </summary>
    private static PlacedForm Hide(PlacedForm form, FormData data)
    {
        IReadOnlyList<bool> hidden = data != null ? data.FieldHidden : null;
        if (form == null || hidden == null || !hidden.Contains(true))
            return form;
        bool Hidden(int slot) => slot >= 0 && slot < form.Slots.Count && form.Slots[slot].Field >= 0 && form.Slots[slot].Field < hidden.Count && hidden[form.Slots[slot].Field];
        var items = form.Items.Where(i => !Hidden(i.Slot)).ToList();
        var slots = form.Slots.Select(s => !Hidden(s.Index) ? s : new FormSlot(s.Index, s.Field, s.Row, s.Source, new FaceRect(s.Hit.XMin, s.Hit.YMin, s.Hit.XMin - 1f, s.Hit.YMin - 1f), s.Page, true)).ToList();
        return new PlacedForm(form.Width, form.Height, form.PageHeight, items, slots, form.PageTops, form.Unit);
    }

    /// <summary>
    /// H, the unit every size of <paramref name="spec"/> laid out
    /// <paramref name="width"/> wide is printed in: the height a style page of
    /// that width has (the width over the style's aspect; the travel
    /// documents spec, TD2), whatever the page's own aspect, so every paper
    /// drawn as wide prints at one size: a narrower look (a long legal sheet)
    /// is a longer page, a wider one (a ticket, a card) a shorter one. A
    /// landscape page kind keeps its own: its width times its aspect.
    /// </summary>
    public static float PrintUnit(FormSpec spec, float width, FormMetrics m)
    {
        m ??= new FormMetrics();
        FormLook look = spec != null && spec.look != null ? spec.look : new FormLook();
        return spec != null && spec.landscape ? width * look.AspectOr(m.aspect) : width / m.aspect;
    }

    /// <summary>The slot whose box holds the point (form space), or -1: a margin, a gutter, the header, the photo, off the page, or no form.</summary>
    public static int SlotAt(PlacedForm form, float x, float y)
    {
        if (form == null)
            return -1;
        for (int i = 0; i < form.Slots.Count; i++)
            if (form.Slots[i].Hit.Contains(x, y))
                return i;
        return -1;
    }

    /// <summary>
    /// A copy of <paramref name="data"/> whose value of field i is a probe
    /// <paramref name="longest"/>[i] characters long (cut from real words), for
    /// Check; fields past the list keep their value.
    /// </summary>
    public static FormData Probe(FormData data, IReadOnlyList<int> longest)
    {
        FormData probe = data.Copy();
        probe.FieldValues = Reserve(data.FieldValues, longest);
        return probe;
    }

    /// <summary>
    /// The value each field's box keeps room for (FormData.FieldReserve; the
    /// document design spec, D2): a probe <paramref name="longest"/>[i]
    /// characters long, cut from real words (Probe's); fields past the list
    /// keep <paramref name="values"/>' own.
    /// </summary>
    public static IReadOnlyList<string> Reserve(IReadOnlyList<string> values, IReadOnlyList<int> longest)
    {
        values = values ?? Array.Empty<string>();
        var reserve = new string[values.Count];
        for (int i = 0; i < reserve.Length; i++)
            reserve[i] = longest != null && i < longest.Count ? ProbeValue(longest[i]) : values[i];
        return reserve;
    }

    /// <summary>
    /// The face check (FO10): every problem of <paramref name="spec"/> showing
    /// <paramref name="probe"/> (FormLayout.Probe: each field at its longest),
    /// one message each: a field placed twice or never, a cell naming a field
    /// the template does not have, a row wider than the grid, a photo cell on a
    /// form without a photo (or the reverse), a value that needs more lines
    /// than a box holds at the floor, a fixed page whose content runs past
    /// its bottom margin (at the look's aspect), and, when the probe reserves
    /// its boxes (FieldReserve), a field whose box moves between its longest
    /// value and none (the document design spec, D2). Empty when the form fits.
    /// </summary>
    public static List<string> Check(FormSpec spec, FormData probe, FormMetrics m, ITextMeasure measure)
    {
        var problems = new List<string>();
        int count = probe.FieldLabels.Count;
        foreach (FormBlock b in spec.blocks ?? new FormBlock[0])
            foreach (int f in FormSpec.FieldsOf(b))
                if (f >= count)
                    problems.Add($"a {b.kind} names field {f}, but the template has {count} fields");
        for (int f = 0; f < count; f++)
        {
            int placed = 0;
            foreach (FormBlock b in spec.blocks ?? new FormBlock[0])
                foreach (int g in FormSpec.FieldsOf(b))
                    if (g == f)
                        placed++;
            if (placed == 0)
                problems.Add($"field {f} ({probe.FieldLabels[f]}) is never placed");
            else if (placed > 1)
                problems.Add($"field {f} ({probe.FieldLabels[f]}) is placed twice");
        }

        bool photoCell = false;
        foreach (FormBlock b in spec.blocks ?? new FormBlock[0])
            if (b != null && b.kind == FormBlockKind.FieldRow)
                foreach (FormCell c in b.cells ?? new FormCell[0])
                    photoCell |= c != null && c.IsPhoto;
        if (photoCell && !probe.HasPhoto)
            problems.Add("a photo cell on a form whose template shows no photo");
        if (!photoCell && probe.HasPhoto)
            problems.Add("the template shows a photo but its form has no photo cell");
        if (ArtLayout.IsArt(spec))
        {
            problems.AddRange(ArtLayout.Problems(spec, probe, measure));
            return problems;
        }

        float aspect = (spec.look ?? new FormLook()).AspectOr(m.aspect);
        PlacedForm full = new Placer(spec, probe, aspect, m, measure, problems).Run();
        if (probe.FieldReserve != null && probe.FieldReserve.Count > 0)
        {
            FormData blank = probe.Copy();
            blank.FieldValues = probe.FieldValues.Select(_ => string.Empty).ToList();
            PlacedForm empty = new Placer(spec, blank, aspect, m, measure, null).Run();
            foreach (FormSlot s in full.Slots)
            {
                FormSlot t = empty.Slots.FirstOrDefault(x => x.Field == s.Field && x.Row == s.Row && x.Source == s.Source);
                if (s.Field >= 0 && (t.Hit.XMin != s.Hit.XMin || t.Hit.YMin != s.Hit.YMin || t.Hit.XMax != s.Hit.XMax || t.Hit.YMax != s.Hit.YMax))
                    problems.Add($"field {s.Field} ({probe.FieldLabels[s.Field]}) moves when its value changes; a document's boxes are fixed");
            }
        }
        return problems;
    }

    /// <summary>A probe value <paramref name="length"/> characters long, cut from real words.</summary>
    private static string ProbeValue(int length)
    {
        var sb = new StringBuilder();
        while (sb.Length < length)
            sb.Append(ProbeWords);
        return sb.ToString(0, Math.Max(0, length)).TrimEnd().PadRight(Math.Max(0, length), 'e');
    }

    /// <summary>The issuing facsimile's words: "Issued by" and the agency's name ({0}; FormData.Word translates the template).</summary>
    public const string IssuedBy = "Issued by {0}";

    /// <summary>"Issued by" and the agency's name in title case (the issuing facsimile), in the reading language of <paramref name="data"/>.</summary>
    private static string IssuedLine(FormData data) =>
        data.Word(IssuedBy).Replace("{0}", data.Translated ? data.Agency ?? string.Empty : CultureInfo.InvariantCulture.TextInfo.ToTitleCase((data.Agency ?? string.Empty).ToLowerInvariant()));

    /// <summary>One layout run: the state of the pen as it goes down the blocks.</summary>
    private sealed class Placer
    {
        private readonly FormSpec _spec;
        private readonly FormData _data;
        private readonly FormMetrics _m;
        private readonly ITextMeasure _measure;
        private readonly List<string> _problems;
        private readonly float _width;

        /// <summary>The page's height.</summary>
        private readonly float _h;

        /// <summary>The print unit (PrintUnit): the width over the style's aspect.</summary>
        private readonly float _u;
        private readonly float _left;
        private readonly float _content;
        private readonly float _column;

        /// <summary>The room a row's ↗ takes at a table row's top right (Layout's rowLinkRoom; 0: none).</summary>
        private readonly float _rowLinkRoom;
        private readonly List<FormItem> _items = new List<FormItem>();
        private readonly List<FormSlot> _slots = new List<FormSlot>();
        private readonly List<float> _pageTops = new List<float> { 0f };
        private readonly List<float> _pageBottoms = new List<float> { 0f };
        private readonly Dictionary<(FormTextRole, float), float> _lines = new Dictionary<(FormTextRole, float), float>();
        private readonly int[] _occupied = new int[Columns];
        private readonly List<Tall> _tall = new List<Tall>();
        private float _y;
        private int _page;
        private float _lastRowBottom;

        /// <summary>A cell spanning rows: where it started, how many rows it still takes, and its slot (-1: the photo).</summary>
        private sealed class Tall
        {
            public FormCell Cell;
            public float X;
            public float Width;
            public float Top;
            public int RowsLeft;
            public int Column;
            public int Span;
            public int Slot;
        }

        public Placer(FormSpec spec, FormData data, float width, FormMetrics m, ITextMeasure measure, List<string> problems, float rowLinkRoom = 0f)
        {
            _rowLinkRoom = Math.Max(0f, rowLinkRoom);
            _spec = spec ?? new FormSpec();
            _data = data ?? new FormData();
            _m = m ?? new FormMetrics();
            _measure = measure;
            _problems = problems;
            _width = width;
            float aspect = (_spec.look ?? new FormLook()).AspectOr(_m.aspect);
            _h = _spec.landscape ? width * aspect : width / aspect;
            _u = PrintUnit(_spec, width, _m);
            _left = _m.marginX * _u;
            _content = width - 2f * _left;
            _column = (_content - (Columns - 1) * G(_m.gutter)) / Columns;
            _y = G(_m.marginTop);
        }

        /// <summary>A size in H (the print unit) to the caller's units.</summary>
        private float G(float h) => h * _u;

        /// <summary>The look's frame (Plain without a look).</summary>
        private FormFrame Frame => _spec.look != null ? _spec.look.frame : FormFrame.Plain;

        public PlacedForm Run()
        {
            foreach (FormBlock b in _spec.blocks ?? new FormBlock[0])
            {
                if (b == null)
                    continue;
                if (b.kind != FormBlockKind.FieldRow)
                    FinishTall(true);
                switch (b.kind)
                {
                    case FormBlockKind.Header: Header(b); break;
                    case FormBlockKind.SealGrid: SealGrid(b); break;
                    case FormBlockKind.Section: Section(b.text); break;
                    case FormBlockKind.FieldRow: FieldRow(b); break;
                    case FormBlockKind.RecordGroups: RecordGroups(b); break;
                    case FormBlockKind.Checkboxes: Checkboxes(b); break;
                    case FormBlockKind.Table: Table(b); break;
                    case FormBlockKind.Paragraph: Words(FormTextRole.Paragraph, !string.IsNullOrEmpty(b.text) ? b.text : SlotText(b.slot), _m.paragraphSize); break;
                    case FormBlockKind.FinePrint: Words(FormTextRole.FinePrint, b.text, _m.finePrintSize); break;
                    case FormBlockKind.Issued: Words(FormTextRole.Caption, !string.IsNullOrEmpty(b.text) ? b.text : IssuedLine(_data), _m.captionSize); break;
                    case FormBlockKind.Barcode: _y += DrawBarcode(_left, _y, G(_m.barcodeWidth), _content) + G(_m.blockGap); break;
                    case FormBlockKind.StampArea: _y += DrawStamp(_y, G(_m.stampHeight)) + G(_m.blockGap); break;
                    case FormBlockKind.Signature: Signature(b); break;
                    case FormBlockKind.Footer: Footer(b); break;
                    case FormBlockKind.PageBreak: PageBreak(); break;
                    case FormBlockKind.Fold: Fold(b); break;
                    case FormBlockKind.Visa: Visa(b); break;
                    case FormBlockKind.Watermark: Watermark(b); break;
                }
            }
            FinishTall(true);

            float height;
            if (_spec.fixedPage)
            {
                height = (_page + 1) * _h;
                for (int p = 0; p < _pageBottoms.Count; p++)
                {
                    float over = _pageBottoms[p] - ((p + 1) * _h - G(_m.marginBottom));
                    if (over > 1e-4f * _u)
                        _problems?.Add($"page {p + 1} runs {over / _u:0.000} H past the page (its bottom margin included)");
                }
            }
            else
            {
                height = _y + G(_m.marginBottom);
            }
            DrawFrame(height);
            return new PlacedForm(_width, height, _h, _items, _slots, _pageTops, _u);
        }

        // ---------------- The frame (the document design spec, D1) ----------------

        /// <summary>A frame band's thickness, in H: a top band, a side band, a certificate's frame, a ticket's stub, a letterhead's foot and head.</summary>
        private const float TopBand = 0.022f, SideBand = 0.03f, FrameBand = 0.008f, FrameInset = 0.006f, StubBand = 0.02f, FootBand = 0.014f, HeadBand = 0.008f;

        /// <summary>A booklet's cover edge (the travel documents spec, TD1), in H: at the sides, the top and the bottom (each inside its margin).</summary>
        private const float CoverSide = 0.03f, CoverTop = 0.018f, CoverBottom = 0.012f;

        /// <summary>A booklet's spine's thickness, a folded card's crease's width and a card's sheen band's, in H.</summary>
        private const float SpineBand = 0.024f, CreaseBand = 0.008f, SheenBand = 0.03f;

        /// <summary>A ticket's perforation: its line's distance from the left edge, a hole's side and the step between holes, in H.</summary>
        private const float PerforationX = 0.03f, Hole = 0.005f, HoleStep = 0.012f;

        /// <summary>The look's frame on each page (a flow page: one page as tall as its content), in the page's margins, so nothing printed moves; a folded card's crease runs down the middle (first in drawing order; painted as a faint shade over the boxes).</summary>
        private void DrawFrame(float height)
        {
            FormFrame frame = Frame;
            if (frame == FormFrame.Plain)
                return;
            int pages = _spec.fixedPage ? _page + 1 : 1;
            for (int p = 0; p < pages; p++)
            {
                float top = _spec.fixedPage ? p * _h : 0f;
                float h = _spec.fixedPage ? _h : height;
                switch (frame)
                {
                    case FormFrame.TopBand:
                        Stripe(0f, top, _width, G(TopBand));
                        break;
                    case FormFrame.SideBand:
                        Stripe(0f, top, G(SideBand), h);
                        break;
                    case FormFrame.Framed:
                        float i = G(FrameInset), t = G(FrameBand);
                        Stripe(i, top + i, _width - 2f * i, t);
                        Stripe(i, top + h - i - t, _width - 2f * i, t);
                        Stripe(i, top + i + t, t, h - 2f * (i + t));
                        Stripe(_width - i - t, top + i + t, t, h - 2f * (i + t));
                        break;
                    case FormFrame.Ticket:
                        Stripe(0f, top, G(StubBand), h);
                        for (float y = top + G(HoleStep) / 2f; y + G(Hole) <= top + h; y += G(HoleStep))
                            _items.Add(new FormItem(FormItemKind.Perforation, FormTextRole.Paragraph, FaceRect.FromTop(G(PerforationX), y, G(Hole), G(Hole)), -1, string.Empty, 0f, FormTextAlign.Left));
                        break;
                    case FormFrame.BottomBand:
                        Stripe(0f, top, _width, G(HeadBand));
                        Stripe(0f, top + h - G(FootBand), _width, G(FootBand));
                        break;
                    case FormFrame.Booklet:
                        Band(FormItemKind.Cover, 0f, top, _width, G(CoverTop), _data.Cover);
                        Band(FormItemKind.Cover, 0f, top + h - G(CoverBottom), _width, G(CoverBottom), _data.Cover);
                        Band(FormItemKind.Cover, 0f, top + G(CoverTop), G(CoverSide), h - G(CoverTop) - G(CoverBottom), _data.Cover);
                        Band(FormItemKind.Cover, _width - G(CoverSide), top + G(CoverTop), G(CoverSide), h - G(CoverTop) - G(CoverBottom), _data.Cover);
                        break;
                    case FormFrame.Card:
                        Stripe(0f, top, G(SheenBand), h);
                        break;
                    case FormFrame.Folded:
                        Stripe(0f, top, _width, G(TopBand));
                        _items.Insert(0, new FormItem(FormItemKind.Crease, FormTextRole.Paragraph, FaceRect.FromTop((_width - G(CreaseBand)) / 2f, top, G(CreaseBand), h), -1, string.Empty, 0f, FormTextAlign.Left));
                        break;
                }
            }
        }

        /// <summary>One band of a frame of <paramref name="kind"/> carrying <paramref name="text"/> (a cover's colour), not counted in a page's content.</summary>
        private void Band(FormItemKind kind, float x, float y, float w, float h, string text) =>
            _items.Add(new FormItem(kind, FormTextRole.Paragraph, FaceRect.FromTop(x, y, w, h), -1, text ?? string.Empty, 0f, FormTextAlign.Left));

        /// <summary>One band of the frame (not counted in a page's content: it lies in the margins).</summary>
        private void Stripe(float x, float y, float w, float h) =>
            _items.Add(new FormItem(FormItemKind.Stripe, FormTextRole.Paragraph, FaceRect.FromTop(x, y, w, h), -1, string.Empty, 0f, FormTextAlign.Left));

        // ---------------- Measuring ----------------

        /// <summary>A width no line of a form reaches: a thousand pages across.</summary>
        private float OneLineWidth => Math.Max(_width, _h) * 1000f;

        /// <summary>One line's height in a role's style at a size.</summary>
        private float Line(FormTextRole role, float size)
        {
            if (!_lines.TryGetValue((role, size), out float line))
            {
                line = _measure.Height("Hg", role, size, OneLineWidth);
                _lines[(role, size)] = line;
            }
            return line;
        }

        /// <summary>The height a text takes, at least one line.</summary>
        private float Measure(string text, FormTextRole role, float size, float width) =>
            string.IsNullOrEmpty(text) ? Line(role, size) : Math.Max(Line(role, size), _measure.Height(text, role, size, width));

        /// <summary>The room a text takes: its measured height, or for capitals (no descenders under the last line) the measured lines but the last at FormMetrics.capsLead ems.</summary>
        private float Height(string text, FormTextRole role, float size, float width)
        {
            float h = Measure(text, role, size, width);
            return FormTextStyles.IsCapitals(role) ? (Lines(h, role, size) - 1) * Line(role, size) + size * _m.capsLead : h;
        }

        /// <summary>How many lines a measured height is.</summary>
        private int Lines(float height, FormTextRole role, float size) => Math.Max(1, (int)Math.Round(height / Line(role, size)));

        /// <summary>A one-line fit is judged at this share of the width: a bold line TextMeshPro measures as just fitting can still wrap when drawn (the title of a narrow page).</summary>
        private const float OneLineGuard = 0.96f;

        /// <summary>The size a text keeps on one line, from <paramref name="max"/> down to <paramref name="min"/> (H), in the caller's units.</summary>
        private float OneLine(string text, FormTextRole role, float max, float min, float width)
        {
            const int steps = 8;
            for (int i = 0; i < steps; i++)
            {
                float size = G(max - (max - min) * i / steps);
                if (Lines(Measure(text, role, size, width * OneLineGuard), role, size) <= 1)
                    return size;
            }
            return G(min);
        }

        // ---------------- Adding items ----------------

        private void Add(FormItemKind kind, FaceRect rect, int slot = -1, string text = "", FormTextRole role = FormTextRole.Paragraph, float size = 0f, FormTextAlign align = FormTextAlign.Left)
        {
            _items.Add(new FormItem(kind, role, rect, slot, text ?? string.Empty, size, align));
            if (_pageBottoms[_page] < rect.YMax)
                _pageBottoms[_page] = rect.YMax;
        }

        /// <summary>A text at (x, y), wrapped at <paramref name="width"/>; returns its height. An empty text adds nothing and takes no room.</summary>
        private float Text(FormTextRole role, string text, float x, float y, float width, float size, int slot = -1, FormTextAlign align = FormTextAlign.Left)
        {
            if (string.IsNullOrEmpty(text))
                return 0f;
            float h = Height(text, role, size, width);
            Add(FormItemKind.Text, FaceRect.FromTop(x, y, width, h), slot, text, role, size, align);
            return h;
        }

        private string SlotText(string slot) =>
            !string.IsNullOrEmpty(slot) && _data.Text != null && _data.Text.TryGetValue(slot, out string text) ? text : string.Empty;

        private string FieldLabel(int field) => field >= 0 && field < _data.FieldLabels.Count ? _data.FieldLabels[field] ?? string.Empty : string.Empty;

        private string FieldValue(int field) => field >= 0 && field < _data.FieldValues.Count ? _data.FieldValues[field] ?? string.Empty : string.Empty;

        private int AddSlot(int field, int row, string source, FaceRect hit)
        {
            _slots.Add(new FormSlot(_slots.Count, field, row, source ?? string.Empty, hit, _page));
            return _slots.Count - 1;
        }

        // ---------------- Blocks ----------------

        /// <summary>Beside an office seal the agency and programme lines shrink to keep one line, down to this share of their size, and the title down to this share of its floor.</summary>
        private const float OfficeLineFloor = 0.8f;

        /// <summary>The form number shrinks to keep one line in its column, down to this share of its size (a booklet's narrower header).</summary>
        private const float NumberFloor = 0.7f;

        /// <summary>
        /// The header: the agency line at the left and the programme line at
        /// the right (beside an office seal each, and the title, shrinks further to keep one line); under them the title (one line, shrinking to its floor)
        /// and the form number at its right. The seal: faint behind the
        /// header, or, when the block names a field (a document's Seal field,
        /// the document design spec D4), the issuing office's seal in a box at
        /// the header's right, a pickable slot the header's words stay clear of.
        /// A booklet (the travel documents spec, TD3) keeps the seal's side at
        /// the header's left for the holder's nation's emblem (FormData.Emblem;
        /// the room is kept without one, so nothing moves) with the nation's
        /// code under it (FormData.NationCode, centred in the agency line's
        /// style; the emblem shrinks to leave it the room), and prints no faint seal.
        /// </summary>
        private void Header(FormBlock b)
        {
            float top = _y;
            float side = G(_m.sealSize);
            bool office = b.field >= 0;
            bool emblem = Frame == FormFrame.Booklet;
            float left = _left + (emblem ? side + G(_m.gutter) : 0f);
            float width = _content - (office ? side + G(_m.gutter) : 0f) - (emblem ? side + G(_m.gutter) : 0f);
            if (office)
            {
                var box = FaceRect.FromTop(_left + _content - side, top, side, side);
                int slot = AddSlot(b.field, -1, string.Empty, box);
                Add(FormItemKind.Seal, box, slot, FieldValue(b.field));
            }
            else if (!emblem)
            {
                Add(FormItemKind.Seal, FaceRect.FromTop(_left, top, side, side));
            }
            float code = emblem && !string.IsNullOrEmpty(_data.NationCode) ? Line(FormTextRole.Agency, G(_m.agencySize)) : 0f;
            if (emblem && !string.IsNullOrEmpty(_data.Emblem))
                Add(FormItemKind.Emblem, FaceRect.FromTop(_left + code / 2f, top, side - code, side - code), -1, _data.Emblem);
            float agencyWidth = width * 0.55f;
            string agencyLine = ArtLayout.Capitals(_data.Agency ?? string.Empty);
            float agencySize = office ? OneLine(agencyLine, FormTextRole.Agency, _m.agencySize, _m.agencySize * OfficeLineFloor, agencyWidth) : G(_m.agencySize);
            float programmeSize = office ? OneLine(_data.Programme ?? string.Empty, FormTextRole.Programme, _m.programmeSize, _m.programmeSize * OfficeLineFloor, width - agencyWidth) : G(_m.programmeSize);
            float agency = Text(FormTextRole.Agency, agencyLine, left, top, agencyWidth, agencySize);
            float programme = Text(FormTextRole.Programme, _data.Programme, left + agencyWidth, top, width - agencyWidth, programmeSize, -1, FormTextAlign.Right);
            float titleTop = top + Math.Max(agency, programme) + G(_m.rowGap);
            float titleWidth = width * 0.84f;
            string title = ArtLayout.Capitals(_data.Title ?? string.Empty);
            float size = OneLine(title, FormTextRole.Title, _m.titleSize, office ? _m.titleFloor * OfficeLineFloor : _m.titleFloor, titleWidth);
            float titleHeight = Text(FormTextRole.Title, title, left, titleTop, titleWidth, size);
            float numberSize = string.IsNullOrEmpty(_data.FormNumber) ? G(_m.formNumberSize) : OneLine(_data.FormNumber, FormTextRole.FormNumber, _m.formNumberSize, _m.formNumberSize * NumberFloor, width - titleWidth);
            float number = string.IsNullOrEmpty(_data.FormNumber) ? 0f : Line(FormTextRole.FormNumber, numberSize);
            Text(FormTextRole.FormNumber, _data.FormNumber, left + titleWidth, titleTop + Math.Max(0f, titleHeight - number), width - titleWidth, numberSize, -1, FormTextAlign.Right);
            if (code > 0f)
                Text(FormTextRole.Agency, _data.NationCode, _left, top + side - code, side, G(_m.agencySize), -1, FormTextAlign.Centre);
            _y = Math.Max(titleTop + Math.Max(titleHeight, number), office || emblem ? top + side : top) + G(_m.blockGap);
        }

        /// <summary>
        /// The Seal Register's grid (the document design spec, D4): each of
        /// FormData.Seals in reading order, three to a row, its seal centred
        /// over its office's name in a box (the name sized as a table cell:
        /// no word breaks while the floor holds it); each box a slot of the
        /// block's slot, its Row the seal's place.
        /// </summary>
        private void SealGrid(FormBlock b)
        {
            const int perRow = 3;
            float pad = G(_m.boxPadding), side = G(_m.sealSize);
            int span = Columns / perRow;
            float width = span * _column + (span - 1) * G(_m.gutter);
            IReadOnlyList<(string Caption, string Value)> seals = _data.Seals ?? Array.Empty<(string, string)>();
            for (int start = 0; start < seals.Count; start += perRow)
            {
                float top = _y, captions = 0f;
                var sizes = new float[perRow];
                for (int k = start; k < Math.Min(seals.Count, start + perRow); k++)
                {
                    sizes[k - start] = CellSize(seals[k].Caption ?? string.Empty, FormTextRole.Cell, width - 2f * pad);
                    captions = Math.Max(captions, Height(seals[k].Caption ?? string.Empty, FormTextRole.Cell, sizes[k - start], width - 2f * pad));
                }
                float height = pad + side + pad + captions + pad;
                for (int k = start; k < Math.Min(seals.Count, start + perRow); k++)
                {
                    float x = _left + (k - start) * (width + G(_m.gutter));
                    var rect = FaceRect.FromTop(x, top, width, height);
                    int slot = AddSlot(-1, k, b.slot, rect);
                    Add(FormItemKind.Box, rect, slot);
                    Add(FormItemKind.Seal, FaceRect.FromTop(x + (width - side) / 2f, top + pad, side, side), slot, seals[k].Value);
                    Text(FormTextRole.Cell, seals[k].Caption, x + pad, top + pad + side + pad, width - 2f * pad, sizes[k - start], slot, FormTextAlign.Centre);
                }
                _lastRowBottom = top + height;
                _y = _lastRowBottom + G(_m.rowGap);
            }
            _y += G(_m.blockGap);
        }

        /// <summary>A section head: its text in capitals on a band across the content.</summary>
        private void Section(string text)
        {
            float pad = G(_m.boxPadding);
            float size = G(_m.sectionSize);
            string head = (text ?? string.Empty).ToUpperInvariant();
            float line = Height(head, FormTextRole.Section, size, _content - 2f * pad);
            var band = FaceRect.FromTop(_left, _y, _content, line + pad);
            Add(FormItemKind.RowBand, band);
            Text(FormTextRole.Section, head, _left + pad, _y + pad / 2f, _content - 2f * pad, size);
            _y = band.YMax + G(_m.rowGap);
        }

        /// <summary>A cell's box content: its label (its caption, else its field's) and value (its field's, else its slot's).</summary>
        private (string label, string value, float valueSize, float valueHeight, float labelHeight, float boxHeight) BoxContent(FormCell c, float width) =>
            BoxContent(!string.IsNullOrEmpty(c.caption) ? c.caption : FieldLabel(c.field), c.field >= 0 ? FieldValue(c.field) : SlotText(c.slot), width, $"field {c.field}", FieldReserve(c.field));

        /// <summary>
        /// A box's content: its label and value, the value's fitted size and
        /// height; and the box's height (a value over the lines a box holds is
        /// reported as <paramref name="what"/>). With a <paramref name="reserve"/>
        /// (the document design spec, D2) the box is as tall as the reserve
        /// needs, whatever its value; a value taller than that shrinks below
        /// the floor until it fits (never moving a box).
        /// </summary>
        private (string label, string value, float valueSize, float valueHeight, float labelHeight, float boxHeight) BoxContent(string label, string value, float width, string what, string reserve = null)
        {
            float pad = G(_m.boxPadding);
            float inner = width - 2f * pad;
            label ??= string.Empty;
            value ??= string.Empty;
            float labelHeight = string.IsNullOrEmpty(label) ? 0f : Height(label, FormTextRole.Label, G(_m.labelSize), inner);
            (float size, float height, int lines) = FitValue(value, inner);
            if (lines > _m.maxValueLines)
                _problems?.Add($"{what} ({label})'s longest value needs {lines} lines at the floor; a box holds {_m.maxValueLines}");
            if (reserve != null)
            {
                float room = FitValue(reserve, inner).height;
                (size, height) = Squeeze(value, size, height, room, inner);
                height = room;
            }
            return (label, value, size, height, labelHeight, pad + labelHeight + height + pad);
        }

        /// <summary>The smallest share of the floor a value shrinks to when it would overflow its reserved room.</summary>
        private const float SqueezeFloor = 0.7f;

        /// <summary>A value that needs more than <paramref name="room"/> at <paramref name="size"/>: smaller in eighths down to SqueezeFloor of the floor, until it fits.</summary>
        private (float size, float height) Squeeze(string value, float size, float height, float room, float width)
        {
            const int steps = 8;
            float floor = G(_m.valueFloor), least = floor * SqueezeFloor;
            for (int i = 1; height > room + 1e-4f * _h && i <= steps; i++)
            {
                size = floor - (floor - least) * i / steps;
                height = Measure(value, FormTextRole.Value, size, width);
            }
            return (size, height);
        }

        /// <summary>The value field <paramref name="field"/>'s box keeps room for (FormData.FieldReserve), or null (no field, or no reserve: the box fits its value).</summary>
        private string FieldReserve(int field) =>
            field >= 0 && _data.FieldReserve != null && field < _data.FieldReserve.Count ? _data.FieldReserve[field] ?? string.Empty : null;

        /// <summary>A value's size and height: full size on one line, else the floor, on one line or wrapped (a box reserves the lines its value needs at the floor, FO6).</summary>
        private (float size, float height, int lines) FitValue(string value, float width)
        {
            float full = G(_m.valueSize), floor = G(_m.valueFloor);
            float hFull = Measure(value, FormTextRole.Value, full, width);
            if (Lines(hFull, FormTextRole.Value, full) <= 1)
                return (full, hFull, 1);
            float hFloor = Measure(value, FormTextRole.Value, floor, width);
            return (floor, hFloor, Lines(hFloor, FormTextRole.Value, floor));
        }

        private void DrawBox(FaceRect rect, int slot, (string label, string value, float valueSize, float valueHeight, float labelHeight, float boxHeight) box)
        {
            float pad = G(_m.boxPadding);
            Add(FormItemKind.Box, rect, slot);
            Text(FormTextRole.Label, box.label, rect.XMin + pad, rect.YMin + pad, rect.Width - 2f * pad, G(_m.labelSize), slot);
            Text(FormTextRole.Value, box.value, rect.XMin + pad, rect.YMin + pad + box.labelHeight, rect.Width - 2f * pad, box.valueSize, slot);
        }

        private void FieldRow(FormBlock b)
        {
            float top = _y;
            float pad = G(_m.boxPadding);
            float rowHeight = pad + G(_m.labelSize) * _m.capsLead + Line(FormTextRole.Value, G(_m.valueSize)) + pad;
            var placed = new List<(FormCell cell, FaceRect rect, int slot, (string, string, float, float, float, float) box)>();
            var chips = new List<(float x, float w)>();
            int col = 0;
            foreach (FormCell c in b.cells ?? new FormCell[0])
            {
                if (c == null)
                    continue;
                int span = Math.Max(1, Math.Min(Columns, c.span));
                while (col < Columns && _occupied[col] > 0)
                    col++;
                int free = 0;
                while (col + free < Columns && _occupied[col + free] == 0)
                    free++;
                if (span > free)
                {
                    _problems?.Add($"a FieldRow needs {col + span} columns; the grid has {Columns} (cells past it are dropped)");
                    if (free == 0)
                        break;
                    span = free;
                }
                float x = _left + col * (_column + G(_m.gutter));
                float w = span * _column + (span - 1) * G(_m.gutter);
                if (c.IsChip && c.rows <= 1)
                {
                    chips.Add((x, w));
                }
                else if (c.rows > 1)
                {
                    int slot = c.IsPhoto ? (c.field >= 0 ? AddSlot(c.field, -1, string.Empty, FaceRect.FromTop(x, top, w, 0f)) : -1)
                        : c.IsChip || (c.field < 0 && string.IsNullOrEmpty(c.slot)) ? -1 : AddSlot(c.field, -1, c.field >= 0 ? string.Empty : c.slot, FaceRect.FromTop(x, top, w, 0f));
                    _tall.Add(new Tall { Cell = c, X = x, Width = w, Top = top, RowsLeft = c.rows, Column = col, Span = span, Slot = slot });
                    for (int k = col; k < col + span; k++)
                        _occupied[k] = c.rows;
                }
                else
                {
                    var box = BoxContent(c, w);
                    rowHeight = Math.Max(rowHeight, box.boxHeight);
                    int slot = c.field >= 0 || !string.IsNullOrEmpty(c.slot) ? AddSlot(c.field, -1, c.field >= 0 ? string.Empty : c.slot, FaceRect.FromTop(x, top, w, 0f)) : -1;
                    placed.Add((c, FaceRect.FromTop(x, top, w, 0f), slot, box));
                }
                col += span;
            }

            foreach ((FormCell cell, FaceRect rect, int slot, var box) in placed)
            {
                var full = FaceRect.FromTop(rect.XMin, top, rect.Width, rowHeight);
                if (slot >= 0)
                    _slots[slot] = new FormSlot(slot, _slots[slot].Field, -1, _slots[slot].Source, full, _page);
                DrawBox(full, slot, box);
            }
            foreach ((float x, float w) in chips)
                Add(FormItemKind.Chip, ChipRect(FaceRect.FromTop(x, top, w, rowHeight)));

            _lastRowBottom = top + rowHeight;
            _y = _lastRowBottom + G(_m.rowGap);
            for (int k = 0; k < Columns; k++)
                if (_occupied[k] > 0)
                    _occupied[k]--;
            foreach (Tall t in _tall)
                t.RowsLeft--;
            FinishTall(false);
        }

        /// <summary>Closes the cells spanning rows that end here (all of them when <paramref name="all"/>): their boxes reach the last row's bottom.</summary>
        private void FinishTall(bool all)
        {
            for (int i = 0; i < _tall.Count; i++)
            {
                Tall t = _tall[i];
                if (!all && t.RowsLeft > 0)
                    continue;
                var rect = new FaceRect(t.X, t.Top, t.X + t.Width, Math.Max(_lastRowBottom, t.Top));
                if (t.Cell.IsChip)
                {
                    Add(FormItemKind.Chip, ChipRect(rect));
                }
                else if (t.Cell.IsPhoto)
                {
                    if (t.Slot >= 0)
                        _slots[t.Slot] = new FormSlot(t.Slot, _slots[t.Slot].Field, -1, _slots[t.Slot].Source, rect, _page);
                    Add(FormItemKind.Box, rect, t.Slot);
                    if (_data.HasPhoto)
                    {
                        float pad = G(_m.boxPadding);
                        float innerW = rect.Width - 2f * pad, innerH = rect.Height - 2f * pad;
                        float w = Math.Min(innerW, innerH * LookCanvas.PhotoAspect);
                        float h = w / LookCanvas.PhotoAspect;
                        Add(FormItemKind.Photo, FaceRect.FromTop(rect.CentreX - w / 2f, rect.CentreY - h / 2f, w, h), t.Slot);
                    }
                }
                else
                {
                    var box = BoxContent(t.Cell, t.Width);
                    if (t.Slot >= 0)
                        _slots[t.Slot] = new FormSlot(t.Slot, _slots[t.Slot].Field, -1, _slots[t.Slot].Source, rect, _page);
                    DrawBox(rect, t.Slot, box);
                }
                for (int k = t.Column; k < t.Column + t.Span; k++)
                    _occupied[k] = 0;
                _tall.RemoveAt(i);
                i--;
            }
        }

        /// <summary>A card's chip's width over its height.</summary>
        private const float ChipAspect = 1.3f;

        /// <summary>A chip in a cell (the travel documents spec, TD1): as large as fits at ChipAspect inside the cell's padding, at its left, centred down it.</summary>
        private FaceRect ChipRect(FaceRect cell)
        {
            float pad = G(_m.boxPadding);
            float w = Math.Min(cell.Width - 2f * pad, (cell.Height - 2f * pad) * ChipAspect);
            float h = w / ChipAspect;
            return FaceRect.FromTop(cell.XMin + pad, cell.CentreY - h / 2f, Math.Max(0f, w), Math.Max(0f, h));
        }

        private void Words(FormTextRole role, string text, float size)
        {
            float h = Text(role, text, _left, _y, _content, G(size));
            if (h > 0f)
                _y += h + G(_m.blockGap);
        }

        private void Checkboxes(FormBlock b)
        {
            float top = _y, pad = G(_m.boxPadding);
            float inner = _content - 2f * pad;
            float labelHeight = Text(FormTextRole.Label, b.text, _left + pad, top + pad, inner, G(_m.labelSize), _slots.Count);
            string value = FieldValue(b.field);
            string[] options = b.options ?? new string[0];
            int perRow = Math.Max(1, Math.Min(3, options.Length));
            float cellWidth = inner / perRow;
            float size = G(_m.cellSize);
            float line = Line(FormTextRole.Cell, size);
            float mark = line * 0.8f;
            float optionsTop = top + pad + labelHeight;
            for (int i = 0; i < options.Length; i++)
            {
                float x = _left + pad + (i % perRow) * cellWidth;
                float y = optionsTop + (i / perRow) * line;
                bool ticked = string.Equals((options[i] ?? string.Empty).Trim(), value.Trim(), StringComparison.OrdinalIgnoreCase);
                Add(FormItemKind.Checkbox, FaceRect.FromTop(x, y + (line - mark) / 2f, mark, mark), _slots.Count, ticked ? Tick : string.Empty);
                Text(FormTextRole.Cell, options[i], x + mark + pad, y, cellWidth - mark - 2f * pad, size, _slots.Count);
            }
            int rows = (options.Length + perRow - 1) / perRow;
            var rect = FaceRect.FromTop(_left, top, _content, pad + labelHeight + rows * line + pad);
            Add(FormItemKind.Box, rect, _slots.Count);
            AddSlot(b.field, -1, string.Empty, rect);
            _lastRowBottom = rect.YMax;
            _y = rect.YMax + G(_m.rowGap);
        }

        private void Table(FormBlock b)
        {
            float pad = G(_m.boxPadding);
            string[] heads = b.columns ?? new string[0];
            int n = heads.Length;
            if (n == 0)
                return;
            var widths = new float[n];
            float total = 0f;
            for (int i = 0; i < n; i++)
                total += b.shares != null && i < b.shares.Length && b.shares[i] > 0f ? b.shares[i] : 0f;
            for (int i = 0; i < n; i++)
                widths[i] = total > 0f && b.shares != null && i < b.shares.Length && b.shares[i] > 0f ? _content * b.shares[i] / total : _content / n;

            float headHeight = 0f;
            var headSizes = new float[n];
            for (int i = 0; i < n; i++)
            {
                headSizes[i] = CellSize(heads[i], FormTextRole.Label, widths[i] - 2f * pad);
                headHeight = Math.Max(headHeight, Height(heads[i], FormTextRole.Label, headSizes[i], widths[i] - 2f * pad));
            }
            var band = FaceRect.FromTop(_left, _y, _content, headHeight + 2f * pad);
            Add(FormItemKind.RowBand, band);
            float x = _left;
            for (int i = 0; i < n; i++)
            {
                Text(FormTextRole.Label, heads[i], x + pad, _y + pad, widths[i] - 2f * pad, headSizes[i]);
                x += widths[i];
            }
            _y = band.YMax;

            IReadOnlyList<string[]> rows = !string.IsNullOrEmpty(b.slot) && _data.Rows != null && _data.Rows.TryGetValue(b.slot, out IReadOnlyList<string[]> r) ? r : Array.Empty<string[]>();
            var sizes = new float[n];
            var textWidths = new float[n];
            for (int i = 0; i < n; i++)
                textWidths[i] = widths[i] - 2f * pad - (i == n - 1 ? LinkReserve(widths[i], pad) : 0f);
            for (int row = 0; row < rows.Count; row++)
            {
                string[] cells = rows[row] ?? new string[0];
                if (n > 1 && cells.Length == 1)
                {
                    HeadingRow(cells[0]);
                    continue;
                }
                float height = 0f;
                for (int i = 0; i < n; i++)
                {
                    string cell = i < cells.Length ? cells[i] : string.Empty;
                    sizes[i] = CellSize(cell, FormTextRole.Cell, textWidths[i]);
                    height = Math.Max(height, Measure(cell, FormTextRole.Cell, sizes[i], textWidths[i]));
                }
                var rect = FaceRect.FromTop(_left, _y, _content, height + 2f * pad);
                int slot = AddSlot(-1, row, b.slot, rect);
                x = _left;
                for (int i = 0; i < n; i++)
                {
                    Text(FormTextRole.Cell, i < cells.Length ? cells[i] : string.Empty, x + pad, _y + pad, textWidths[i], sizes[i], slot);
                    x += widths[i];
                }
                Add(FormItemKind.Rule, FaceRect.FromTop(_left, rect.YMax - G(_m.ruleWidth), _content, G(_m.ruleWidth)), slot);
                _y = rect.YMax;
            }
            _y += G(_m.blockGap);
        }

        /// <summary>How much more than its padding a table's last cell leaves at its right for a row's ↗ (the room less the padding, at most half the cell's text width).</summary>
        private float LinkReserve(float columnWidth, float pad) =>
            _rowLinkRoom > pad ? Math.Min(_rowLinkRoom - pad, (columnWidth - 2f * pad) / 2f) : 0f;

        /// <summary>
        /// A table cell's size in a column <paramref name="width"/> wide: the
        /// cell size, else the largest size down to the cell floor at which the
        /// text's widest word keeps one line (OneLine per word; a text that
        /// fits one line at the cell size needs no word checked), so a narrow
        /// column never breaks a word while the floor holds it.
        /// </summary>
        private float CellSize(string text, FormTextRole role, float width)
        {
            float full = G(_m.cellSize);
            if (string.IsNullOrEmpty(text) || Lines(Measure(text, role, full, width * OneLineGuard), role, full) <= 1)
                return full;
            float size = full;
            foreach (string word in text.Split(' '))
                if (word.Length > 0)
                    size = Math.Min(size, OneLine(word, role, _m.cellSize, _m.cellFloor, width));
            return size;
        }

        /// <summary>A heading row across a table (an era's name in a register): its text in section capitals on a band, no slot.</summary>
        private void HeadingRow(string text)
        {
            float pad = G(_m.boxPadding);
            string head = (text ?? string.Empty).ToUpperInvariant();
            float height = Height(head, FormTextRole.Section, G(_m.sectionSize), _content - 2f * pad);
            var band = FaceRect.FromTop(_left, _y, _content, height + pad);
            Add(FormItemKind.RowBand, band);
            Text(FormTextRole.Section, head, _left + pad, _y + pad / 2f, _content - 2f * pad, G(_m.sectionSize));
            _y = band.YMax;
        }

        /// <summary>
        /// A record's groups (FormData.Groups): each group a section numbered
        /// from 1 ("1  RECORDS"), its rows boxes two to a row in order, a value
        /// too long for a half-row box at the floor on one line across the
        /// row. Each row is a slot of the block's slot, its Row the row's place
        /// counted across every group.
        /// </summary>
        private void RecordGroups(FormBlock b)
        {
            float half = 6 * _column + 5 * G(_m.gutter);
            float inner = half - 2f * G(_m.boxPadding);
            int number = 1, flat = 0;
            foreach (FormGroup group in _data.Groups ?? Array.Empty<FormGroup>())
            {
                if (group == null)
                    continue;
                Section(string.IsNullOrEmpty(group.Title) ? number.ToString(CultureInfo.InvariantCulture) : $"{number}  {group.Title}");
                number++;
                var pending = new List<(string label, string value, int row)>();
                foreach ((string label, string value) in group.Rows)
                {
                    if (FitValue(value ?? string.Empty, inner).lines > 1)
                    {
                        if (pending.Count > 0)
                            BoxRow(pending, b.slot, 6);
                        pending.Clear();
                        BoxRow(new List<(string, string, int)> { (label, value, flat) }, b.slot, Columns);
                    }
                    else
                    {
                        pending.Add((label, value, flat));
                        if (pending.Count == 2)
                        {
                            BoxRow(pending, b.slot, 6);
                            pending.Clear();
                        }
                    }
                    flat++;
                }
                if (pending.Count > 0)
                    BoxRow(pending, b.slot, 6);
            }
        }

        /// <summary>A row of boxes <paramref name="span"/> columns wide from the left, each a slot of <paramref name="source"/> (its row), as tall as the tallest.</summary>
        private void BoxRow(List<(string label, string value, int row)> boxes, string source, int span)
        {
            float top = _y, pad = G(_m.boxPadding);
            float width = span * _column + (span - 1) * G(_m.gutter);
            float rowHeight = pad + G(_m.labelSize) * _m.capsLead + Line(FormTextRole.Value, G(_m.valueSize)) + pad;
            var contents = new List<(string, string, float, float, float, float)>();
            foreach ((string label, string value, int _) in boxes)
            {
                var content = BoxContent(label, value, width, "a record row");
                contents.Add(content);
                rowHeight = Math.Max(rowHeight, content.boxHeight);
            }
            for (int k = 0; k < boxes.Count; k++)
            {
                var rect = FaceRect.FromTop(_left + k * span * (_column + G(_m.gutter)), top, width, rowHeight);
                DrawBox(rect, AddSlot(-1, boxes[k].row, source, rect), contents[k]);
            }
            _lastRowBottom = top + rowHeight;
            _y = _lastRowBottom + G(_m.rowGap);
        }

        private void Signature(FormBlock b)
        {
            float top = _y, pad = G(_m.boxPadding);
            float width = 7 * _column + 6 * G(_m.gutter);
            string value = b.field >= 0 ? FieldValue(b.field) : SlotText(b.slot);
            bool blank = string.IsNullOrWhiteSpace(value);
            int slot = _slots.Count;
            float hand = Text(FormTextRole.Value, blank ? _data.Word(Unsigned) : value, _left + pad, top, width - 2f * pad, blank ? G(_m.captionSize) : G(_m.valueSize), slot);
            string reserve = FieldReserve(b.field);
            if (reserve != null)
                hand = Math.Max(Measure(reserve, FormTextRole.Value, G(_m.valueSize), width - 2f * pad), Line(FormTextRole.Value, G(_m.valueSize)));
            float ruleTop = top + hand + pad;
            Add(FormItemKind.Rule, FaceRect.FromTop(_left, ruleTop, width, G(_m.ruleWidth)), slot);
            float caption = Text(FormTextRole.Caption, b.text, _left, ruleTop + G(_m.ruleWidth) + pad / 2f, width, G(_m.captionSize), slot);
            var rect = new FaceRect(_left, top, _left + width, ruleTop + G(_m.ruleWidth) + pad / 2f + caption);
            AddSlot(b.field, -1, string.Empty, rect);
            _y = rect.YMax + G(_m.blockGap);
        }

        /// <summary>The barcode's bars at (x, y), <paramref name="width"/> wide, and the serial beside them, within <paramref name="rowWidth"/>; returns their height (none without a serial).</summary>
        private float DrawBarcode(float x, float y, float width, float rowWidth)
        {
            if (string.IsNullOrEmpty(_data.Serial))
                return 0f;
            int modules = Math.Max(1, _m.barcodeModules);
            float module = width / modules;
            float height = G(_m.barcodeHeight);
            foreach ((int start, int bar) in Barcode.Bars(_data.Serial, modules))
                Add(FormItemKind.Bar, FaceRect.FromTop(x + start * module, y, bar * module, height));
            float gap = G(_m.boxPadding);
            float serial = Measure(_data.Serial, FormTextRole.Serial, G(_m.serialSize), rowWidth - width - gap);
            Text(FormTextRole.Serial, _data.Serial, x + width + gap, y + Math.Max(0f, height - serial), rowWidth - width - gap, G(_m.serialSize));
            return Math.Max(height, serial);
        }

        /// <summary>The stamp area at the content's right, <paramref name="height"/> tall at least, with its caption; returns its height.</summary>
        private float DrawStamp(float top, float height)
        {
            float pad = G(_m.boxPadding), width = G(_m.stampWidth);
            float captionWidth = width - 2f * pad;
            float caption = Measure(_data.Word(StampCaption), FormTextRole.Caption, G(_m.captionSize), captionWidth);
            height = Math.Max(height, caption + 2f * pad);
            var rect = FaceRect.FromTop(_left + _content - width, top, width, height);
            Add(FormItemKind.StampArea, rect);
            Text(FormTextRole.Caption, _data.Word(StampCaption), rect.XMin + pad, top + pad, captionWidth, G(_m.captionSize), -1, FormTextAlign.Centre);
            return height;
        }

        private void Footer(FormBlock b)
        {
            float gap = G(_m.rowGap);
            _y += G(_m.blockGap) - gap;
            Add(FormItemKind.Rule, FaceRect.FromTop(_left, _y, _content, G(_m.ruleWidth)));
            float top = _y + G(_m.ruleWidth) + gap;
            float leftWidth = _content - G(_m.stampWidth) - G(_m.gutter);
            float issued = Text(FormTextRole.Caption, IssuedLine(_data), _left, top, leftWidth, G(_m.captionSize));
            float code = DrawBarcode(_left, top + issued + gap, Math.Min(G(_m.barcodeWidth), leftWidth / 2f), leftWidth);
            float stamp = DrawStamp(top, Math.Max(G(_m.stampHeight), issued + gap + code));
            _y = top + stamp + gap;
            float fine = Text(FormTextRole.FinePrint, b.text, _left, _y, _content, G(_m.finePrintSize));
            _y += fine + (fine > 0f ? gap : 0f);
        }

        /// <summary>The top of the page the pen is on.</summary>
        private float PageTop => _spec.fixedPage ? _page * _h : _pageTops[_page];

        /// <summary>
        /// A booklet's fold (the travel documents spec, TD3): the spine across
        /// the whole page at the block's place (shares[0] of the page's height
        /// from its top; none: a block gap under the pen), the pen a top
        /// margin under it. A data page that runs into the spine is reported.
        /// </summary>
        private void Fold(FormBlock b)
        {
            float spine = G(SpineBand);
            float centre = b.shares != null && b.shares.Length > 0 && b.shares[0] > 0f ? PageTop + b.shares[0] * _h : _y + G(_m.blockGap) + spine / 2f;
            float top = centre - spine / 2f;
            if (_y > top + 1e-4f * _u)
                _problems?.Add($"the page above the fold runs {(_y - top) / _u:0.000} H into the spine");
            Add(FormItemKind.Spine, FaceRect.FromTop(0f, top, _width, spine));
            _y = top + spine + G(_m.marginTop);
            _lastRowBottom = _y;
        }

        /// <summary>A visa page's least height, in the style's stamp heights.</summary>
        private const float VisaLeast = 1.25f;

        /// <summary>
        /// A visa page (TD3): the stamp area across the content from the pen
        /// down to the page's bottom margin (at least VisaLeast of the style's
        /// stamp height), the block's text as its caption inside its top:
        /// where the verdict's stamp lands (StampSpots).
        /// </summary>
        private void Visa(FormBlock b)
        {
            float top = _y, pad = G(_m.boxPadding);
            float bottom = _spec.fixedPage ? PageTop + _h - G(_m.marginBottom) : top;
            var rect = FaceRect.FromTop(_left, top, _content, Math.Max(VisaLeast * G(_m.stampHeight), bottom - top));
            Add(FormItemKind.StampArea, rect, -1, VisaBox);
            Text(FormTextRole.Caption, b.text, _left + pad, top + pad, _content - 2f * pad, G(_m.captionSize), -1, FormTextAlign.Centre);
            _lastRowBottom = rect.YMax;
            _y = rect.YMax + G(_m.blockGap);
        }

        /// <summary>A watermark's side as a share of the page's content (the smaller of its width and its height).</summary>
        private const float WatermarkShare = 0.6f;

        /// <summary>
        /// A watermark (TD1): the seal of the block's field, else the
        /// holder's nation's emblem, centred across the page and down the room
        /// between the pen and the page's bottom margin (a letterhead's body
        /// after its header, a passport's visa page after its fold), under
        /// every item (first in drawing order; the renderers draw it over the
        /// boxes' fills), taking no room; nothing when there is no mark to
        /// draw (or no room, on a flowing page).
        /// </summary>
        private void Watermark(FormBlock b)
        {
            string mark = b.field >= 0 ? FieldValue(b.field) : _data.Emblem;
            float bottom = _spec.fixedPage ? PageTop + _h - G(_m.marginBottom) : _y;
            if (string.IsNullOrEmpty(mark) || bottom <= _y)
                return;
            float side = WatermarkShare * Math.Min(_content, bottom - _y);
            var rect = FaceRect.FromTop((_width - side) / 2f, (_y + bottom - side) / 2f, side, side);
            _items.Insert(0, new FormItem(FormItemKind.Watermark, FormTextRole.Paragraph, rect, -1, mark, 0f, FormTextAlign.Left));
        }

        private void PageBreak()
        {
            _page++;
            _pageBottoms.Add(0f);
            if (_spec.fixedPage)
            {
                _pageTops.Add(_page * _h);
                _y = _page * _h + G(_m.marginTop);
            }
            else
            {
                _y += G(_m.blockGap);
                _pageTops.Add(_y);
            }
            _lastRowBottom = _y;
        }
    }
}
