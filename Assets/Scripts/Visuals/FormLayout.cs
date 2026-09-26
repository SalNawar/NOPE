using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

/// <summary>
/// What one form shows (PC spec §6.1), built by the caller: the header's
/// words, the serial, whether there is a photo, each template field's label
/// and shown value, and a page kind's slot contents. Forms are diegetic: every
/// word is printed English and never follows the UI language.
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

    /// <summary>A page kind's text slots (a Paragraph's slot, a named cell's slot).</summary>
    public IReadOnlyDictionary<string, string> Text = new Dictionary<string, string>();

    /// <summary>A page kind's table rows by slot, each row its cells' texts (one cell in a table of several columns: a heading row).</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string[]>> Rows = new Dictionary<string, IReadOnlyList<string[]>>();

    /// <summary>A record's groups, for a RecordGroups block (the traveller-types spec's R1: a record's shape is data).</summary>
    public IReadOnlyList<FormGroup> Groups = Array.Empty<FormGroup>();

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

    /// <summary>A paragraph's size.</summary>
    public float paragraphSize = 0.034f;

    /// <summary>A caption's size (the issuing line, the stamp area's and a signature's caption).</summary>
    public float captionSize = 0.024f;

    /// <summary>Fine print's size.</summary>
    public float finePrintSize = 0.020f;

    /// <summary>The seal's side, printed behind the header.</summary>
    public float sealSize = 0.1f;

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
    /// <summary>The agency seal, printed faintly behind the header.</summary>
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
    RowBand
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
    FinePrint
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

    /// <summary>True for the labels, drawn in small capitals.</summary>
    public static bool IsSmallCaps(FormTextRole role) => role == FormTextRole.Label;

    /// <summary>True for the roles printed in capitals (the agency line, the title and section heads in capitals, the labels in small capitals): a line of them is FormMetrics.capsLead ems.</summary>
    public static bool IsCapitals(FormTextRole role) =>
        role == FormTextRole.Agency || role == FormTextRole.Title || role == FormTextRole.Section || role == FormTextRole.Label;
}

/// <summary>One placed part of a form, in form space (top-left origin, y down, the caller's units).</summary>
public readonly struct FormItem
{
    /// <summary>An item from its parts.</summary>
    public FormItem(FormItemKind kind, FormTextRole role, FaceRect rect, int slot, string text, float size, FormTextAlign align)
    {
        Kind = kind;
        Role = role;
        Rect = rect;
        Slot = slot;
        Text = text;
        Size = size;
        Align = align;
    }

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
    /// <summary>A slot from its parts.</summary>
    public FormSlot(int index, int field, int row, string source, FaceRect hit, int page)
    {
        Index = index;
        Field = field;
        Row = row;
        Source = source;
        Hit = hit;
        Page = page;
    }

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
    public PlacedForm(float width, float height, float pageHeight, IReadOnlyList<FormItem> items, IReadOnlyList<FormSlot> slots, IReadOnlyList<float> pageTops)
    {
        Width = width;
        Height = height;
        PageHeight = pageHeight;
        Items = items;
        Slots = slots;
        PageTops = pageTops;
    }

    /// <summary>The width it was laid out at.</summary>
    public float Width { get; }

    /// <summary>Its height: its pages on a fixed page, its content on a flow page.</summary>
    public float Height { get; }

    /// <summary>H: one page's height (width / aspect), the unit of every size.</summary>
    public float PageHeight { get; }

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
/// floor, where it may wrap to two lines, and its box grows to fit.
/// SlotAt is the hit test; Check is Build Office UI's and the validator's face
/// check (FO10). Pure and engine-free.
/// </summary>
public static class FormLayout
{
    /// <summary>The columns of a FieldRow's grid.</summary>
    public const int Columns = 12;

    /// <summary>What a ticked checkbox holds.</summary>
    public const string Tick = "X";

    /// <summary>What an empty signature line prints (a real fault on a waiver, traveller-types F3).</summary>
    public const string Unsigned = "UNSIGNED";

    /// <summary>The stamp area's caption.</summary>
    public const string StampCaption = "FOR OFFICIAL USE · DESK STAMP";

    /// <summary>The words a probe value is cut from: real word lengths, so it wraps like a value.</summary>
    private const string ProbeWords = "Middle Egyptian, hieroglyphs and a reed brush ";

    /// <summary>Lays out <paramref name="spec"/> showing <paramref name="data"/> at <paramref name="width"/> (the caller's units).</summary>
    public static PlacedForm Layout(FormSpec spec, FormData data, float width, FormMetrics m, ITextMeasure measure) =>
        new Placer(spec, data, width, m, measure, null).Run();

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
        var values = new string[data.FieldValues.Count];
        for (int i = 0; i < values.Length; i++)
            values[i] = longest != null && i < longest.Count ? ProbeValue(longest[i]) : data.FieldValues[i];
        probe.FieldValues = values;
        return probe;
    }

    /// <summary>
    /// The face check (FO10): every problem of <paramref name="spec"/> showing
    /// <paramref name="probe"/> (FormLayout.Probe: each field at its longest),
    /// one message each: a field placed twice or never, a cell naming a field
    /// the template does not have, a row wider than the grid, a photo cell on a
    /// form without a photo (or the reverse), a value that needs more lines
    /// than a box holds at the floor, and a fixed page whose content runs past
    /// its bottom margin. Empty when the form fits.
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

        new Placer(spec, probe, m.aspect, m, measure, problems).Run();
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

    /// <summary>"Issued by" and the agency's name in title case (the issuing facsimile).</summary>
    private static string IssuedLine(string agency) =>
        "Issued by " + CultureInfo.InvariantCulture.TextInfo.ToTitleCase((agency ?? string.Empty).ToLowerInvariant());

    /// <summary>One layout run: the state of the pen as it goes down the blocks.</summary>
    private sealed class Placer
    {
        private readonly FormSpec _spec;
        private readonly FormData _data;
        private readonly FormMetrics _m;
        private readonly ITextMeasure _measure;
        private readonly List<string> _problems;
        private readonly float _width;
        private readonly float _h;
        private readonly float _left;
        private readonly float _content;
        private readonly float _column;
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

        public Placer(FormSpec spec, FormData data, float width, FormMetrics m, ITextMeasure measure, List<string> problems)
        {
            _spec = spec ?? new FormSpec();
            _data = data ?? new FormData();
            _m = m ?? new FormMetrics();
            _measure = measure;
            _problems = problems;
            _width = width;
            _h = width / _m.aspect;
            _left = _m.marginX * _h;
            _content = width - 2f * _left;
            _column = (_content - (Columns - 1) * G(_m.gutter)) / Columns;
            _y = G(_m.marginTop);
        }

        /// <summary>A size in H to the caller's units.</summary>
        private float G(float h) => h * _h;

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
                    case FormBlockKind.Header: Header(); break;
                    case FormBlockKind.Section: Section(b.text); break;
                    case FormBlockKind.FieldRow: FieldRow(b); break;
                    case FormBlockKind.RecordGroups: RecordGroups(b); break;
                    case FormBlockKind.Checkboxes: Checkboxes(b); break;
                    case FormBlockKind.Table: Table(b); break;
                    case FormBlockKind.Paragraph: Words(FormTextRole.Paragraph, !string.IsNullOrEmpty(b.text) ? b.text : SlotText(b.slot), _m.paragraphSize); break;
                    case FormBlockKind.FinePrint: Words(FormTextRole.FinePrint, b.text, _m.finePrintSize); break;
                    case FormBlockKind.Issued: Words(FormTextRole.Caption, !string.IsNullOrEmpty(b.text) ? b.text : IssuedLine(_data.Agency), _m.captionSize); break;
                    case FormBlockKind.Barcode: _y += DrawBarcode(_left, _y, G(_m.barcodeWidth), _content) + G(_m.blockGap); break;
                    case FormBlockKind.StampArea: _y += DrawStamp(_y, G(_m.stampHeight)) + G(_m.blockGap); break;
                    case FormBlockKind.Signature: Signature(b); break;
                    case FormBlockKind.Footer: Footer(b); break;
                    case FormBlockKind.PageBreak: PageBreak(); break;
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
                    if (over > 1e-4f * _h)
                        _problems?.Add($"page {p + 1} runs {over / _h:0.000} H past the page (its bottom margin included)");
                }
            }
            else
            {
                height = _y + G(_m.marginBottom);
            }
            return new PlacedForm(_width, height, _h, _items, _slots, _pageTops);
        }

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

        /// <summary>The size a text keeps on one line, from <paramref name="max"/> down to <paramref name="min"/> (H), in the caller's units.</summary>
        private float OneLine(string text, FormTextRole role, float max, float min, float width)
        {
            const int steps = 8;
            for (int i = 0; i < steps; i++)
            {
                float size = G(max - (max - min) * i / steps);
                if (Lines(Measure(text, role, size, width), role, size) <= 1)
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

        /// <summary>
        /// The header: the seal behind it; the agency line at the left and the
        /// programme line at the right; under them the title (one line,
        /// shrinking to its floor) and the form number at its right.
        /// </summary>
        private void Header()
        {
            float top = _y;
            Add(FormItemKind.Seal, FaceRect.FromTop(_left, top, G(_m.sealSize), G(_m.sealSize)));
            float agencyWidth = _content * 0.55f;
            float agency = Text(FormTextRole.Agency, (_data.Agency ?? string.Empty).ToUpperInvariant(), _left, top, agencyWidth, G(_m.agencySize));
            float programme = Text(FormTextRole.Programme, _data.Programme, _left + agencyWidth, top, _content - agencyWidth, G(_m.programmeSize), -1, FormTextAlign.Right);
            float titleTop = top + Math.Max(agency, programme) + G(_m.rowGap);
            float titleWidth = _content * 0.84f;
            string title = (_data.Title ?? string.Empty).ToUpperInvariant();
            float size = OneLine(title, FormTextRole.Title, _m.titleSize, _m.titleFloor, titleWidth);
            float titleHeight = Text(FormTextRole.Title, title, _left, titleTop, titleWidth, size);
            float number = string.IsNullOrEmpty(_data.FormNumber) ? 0f : Line(FormTextRole.FormNumber, G(_m.formNumberSize));
            Text(FormTextRole.FormNumber, _data.FormNumber, _left + titleWidth, titleTop + Math.Max(0f, titleHeight - number), _content - titleWidth, G(_m.formNumberSize), -1, FormTextAlign.Right);
            _y = titleTop + Math.Max(titleHeight, number) + G(_m.blockGap);
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
            BoxContent(!string.IsNullOrEmpty(c.caption) ? c.caption : FieldLabel(c.field), c.field >= 0 ? FieldValue(c.field) : SlotText(c.slot), width, $"field {c.field}");

        /// <summary>A box's content: its label and value, the value's fitted size and height; and the box's height (a value over the lines a box holds is reported as <paramref name="what"/>).</summary>
        private (string label, string value, float valueSize, float valueHeight, float labelHeight, float boxHeight) BoxContent(string label, string value, float width, string what)
        {
            float pad = G(_m.boxPadding);
            float inner = width - 2f * pad;
            label ??= string.Empty;
            value ??= string.Empty;
            float labelHeight = string.IsNullOrEmpty(label) ? 0f : Height(label, FormTextRole.Label, G(_m.labelSize), inner);
            (float size, float height, int lines) = FitValue(value, inner);
            if (lines > _m.maxValueLines)
                _problems?.Add($"{what} ({label})'s longest value needs {lines} lines at the floor; a box holds {_m.maxValueLines}");
            return (label, value, size, height, labelHeight, pad + labelHeight + height + pad);
        }

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
                if (c.rows > 1)
                {
                    int slot = c.IsPhoto || (c.field < 0 && string.IsNullOrEmpty(c.slot)) ? -1 : AddSlot(c.field, -1, c.field >= 0 ? string.Empty : c.slot, FaceRect.FromTop(x, top, w, 0f));
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
                if (t.Cell.IsPhoto)
                {
                    Add(FormItemKind.Box, rect);
                    if (_data.HasPhoto)
                    {
                        float pad = G(_m.boxPadding);
                        float innerW = rect.Width - 2f * pad, innerH = rect.Height - 2f * pad;
                        float w = Math.Min(innerW, innerH * LookCanvas.PhotoAspect);
                        float h = w / LookCanvas.PhotoAspect;
                        Add(FormItemKind.Photo, FaceRect.FromTop(rect.CentreX - w / 2f, rect.CentreY - h / 2f, w, h));
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
            for (int i = 0; i < n; i++)
                headHeight = Math.Max(headHeight, Height(heads[i], FormTextRole.Label, G(_m.labelSize), widths[i] - 2f * pad));
            var band = FaceRect.FromTop(_left, _y, _content, headHeight + 2f * pad);
            Add(FormItemKind.RowBand, band);
            float x = _left;
            for (int i = 0; i < n; i++)
            {
                Text(FormTextRole.Label, heads[i], x + pad, _y + pad, widths[i] - 2f * pad, G(_m.labelSize));
                x += widths[i];
            }
            _y = band.YMax;

            IReadOnlyList<string[]> rows = !string.IsNullOrEmpty(b.slot) && _data.Rows != null && _data.Rows.TryGetValue(b.slot, out IReadOnlyList<string[]> r) ? r : Array.Empty<string[]>();
            float size = G(_m.cellSize);
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
                    height = Math.Max(height, Measure(i < cells.Length ? cells[i] : string.Empty, FormTextRole.Cell, size, widths[i] - 2f * pad));
                var rect = FaceRect.FromTop(_left, _y, _content, height + 2f * pad);
                int slot = AddSlot(-1, row, b.slot, rect);
                x = _left;
                for (int i = 0; i < n; i++)
                {
                    Text(FormTextRole.Cell, i < cells.Length ? cells[i] : string.Empty, x + pad, _y + pad, widths[i] - 2f * pad, size, slot);
                    x += widths[i];
                }
                Add(FormItemKind.Rule, FaceRect.FromTop(_left, rect.YMax - G(_m.ruleWidth), _content, G(_m.ruleWidth)), slot);
                _y = rect.YMax;
            }
            _y += G(_m.blockGap);
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
            float hand = Text(FormTextRole.Value, blank ? Unsigned : value, _left + pad, top, width - 2f * pad, blank ? G(_m.captionSize) : G(_m.valueSize), slot);
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
            float caption = Measure(StampCaption, FormTextRole.Caption, G(_m.captionSize), captionWidth);
            height = Math.Max(height, caption + 2f * pad);
            var rect = FaceRect.FromTop(_left + _content - width, top, width, height);
            Add(FormItemKind.StampArea, rect);
            Text(FormTextRole.Caption, StampCaption, rect.XMin + pad, top + pad, captionWidth, G(_m.captionSize), -1, FormTextAlign.Centre);
            return height;
        }

        private void Footer(FormBlock b)
        {
            float gap = G(_m.rowGap);
            _y += G(_m.blockGap) - gap;
            Add(FormItemKind.Rule, FaceRect.FromTop(_left, _y, _content, G(_m.ruleWidth)));
            float top = _y + G(_m.ruleWidth) + gap;
            float leftWidth = _content - G(_m.stampWidth) - G(_m.gutter);
            float issued = Text(FormTextRole.Caption, IssuedLine(_data.Agency), _left, top, leftWidth, G(_m.captionSize));
            float code = DrawBarcode(_left, top + issued + gap, Math.Min(G(_m.barcodeWidth), leftWidth / 2f), leftWidth);
            float stamp = DrawStamp(top, Math.Max(G(_m.stampHeight), issued + gap + code));
            _y = top + stamp + gap;
            float fine = Text(FormTextRole.FinePrint, b.text, _left, _y, _content, G(_m.finePrintSize));
            _y += fine + (fine > 0f ? gap : 0f);
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
