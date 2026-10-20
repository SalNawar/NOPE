using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// The layout of a form drawn on its art (FormArt; the Canva documents, run
/// 7): the face prints the labels, frames and emblems, and the placed form
/// holds only what the game prints over it, at the art's places on a page
/// as wide as asked and as tall as the art's aspect makes it: a booklet's
/// cover margins (in its holder's nation's colour) and spine; over a field not
/// introduced yet, a patch of the blank face hiding its baked label (the
/// renderers draw it from ArtSlots.PaperBlank); the ENTRY VISA box with its
/// caption and the nation's emblem faint inside it; each field's value
/// (a Signature block's field in a hand, UNSIGNED when blank), a relabelled
/// field's own label, the issuing seal at its spot and the photo fitted
/// into its window, each field a slot whose box is its value's place (so
/// inspect mode's line lands on the value); then a photo window on a paper
/// with no Photo field (never picked) and the prints that are no field.
/// The template's blocks still say which field is the seal (the header's) and
/// which the photo (the photo cell's) and which are signed. Every slot keeps
/// the field's place whatever its value (the document design spec, D2): a long
/// value shrinks to its place, never moves it. Problems is the art's half of
/// FormLayout.Check. Pure and engine-free.
/// </summary>
public static class ArtLayout
{
    /// <summary>The least share of the art's own size a value or a label shrinks to so that it fits its place (the renderers' fit; the check measures the longest value at it): TextFit.Least. A grown text may shrink to the same absolute size (FitFloorOf); below TextFit.Floor it counts as shrunk (the cap-height gate).</summary>
    public const float FitFloor = TextFit.Least;

    /// <summary>The least share of its size an item of <paramref name="role"/> on <paramref name="art"/> shrinks to: FitFloor of the art's own size, so less of a grown size (labelGrowth for a label, valueGrowth for the rest).</summary>
    public static float FitFloorOf(FormArt art, FormTextRole role) =>
        TextFit.LeastOf(FitFloor, art == null ? 1f : role == FormTextRole.Label ? art.labelGrowth : art.valueGrowth);

    /// <summary>A printed label's line over its size (the art's label face, Big Shoulders: 1.2 em, and a hair): the band a re-laid label takes at its cell's top.</summary>
    public const float LabelLine = 1.25f;

    /// <summary>The most of its place's height a grown value's size takes (the art's value face, Geist Mono, sets a line 1.3 em tall: a larger size would wrap or shrink).</summary>
    public const float ValueFill = 0.74f;

    /// <summary>The nation's emblem inside a visa box, as a share of the box's smaller side.</summary>
    public const float EmblemShare = 0.7f;

    /// <summary>The visa box's caption's size as a share of the box's height.</summary>
    public const float CaptionShare = 0.06f;

    /// <summary>The share of its place's height UNSIGNED takes (a caption's size in a hand's place).</summary>
    public const float UnsignedShare = 0.6f;

    /// <summary>
    /// <paramref name="text"/> in capitals as a printed form sets them: upper
    /// case (invariant), a Greek capital without its accent (Greek writes
    /// capitals bare: "Υπηρεσία" gives ΥΠΗΡΕΣΙΑ; the diaeresis stays) and a final sigma as Σ (Unity's runtime leaves ς lower case); a
    /// rich-text tag ("&lt;size=60%&gt;") is kept as it is.
    /// </summary>
    public static string Capitals(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text ?? string.Empty;
        if (text.IndexOf('<') < 0)
            return PlainCapitals(text);
        var sb = new System.Text.StringBuilder(text.Length);
        int at = 0;
        while (at < text.Length)
        {
            int open = text.IndexOf('<', at);
            int close = open >= 0 ? text.IndexOf('>', open) : -1;
            if (open < 0 || close < 0)
            {
                sb.Append(PlainCapitals(text.Substring(at)));
                break;
            }
            sb.Append(PlainCapitals(text.Substring(at, open - at)));
            sb.Append(text, open, close - open + 1);
            at = close + 1;
        }
        return sb.ToString();
    }

    /// <summary><see cref="Capitals"/> of a text without tags.</summary>
    private static string PlainCapitals(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text ?? string.Empty;
        bool greek = false;
        foreach (char c in text)
            greek |= c >= '\u0370' && c <= '\u03FF' || c >= '\u1F00' && c <= '\u1FFF';
        if (!greek)
            return text.ToUpperInvariant();
        var sb = new System.Text.StringBuilder(text.Length);
        foreach (char c in text.Normalize(System.Text.NormalizationForm.FormD).ToUpperInvariant())
            if (c != '\u0301' && c != '\u0300' && c != '\u0342' && c != '\u0313' && c != '\u0314' && c != '\u0345')
                sb.Append(c == '\u03C2' ? '\u03A3' : c);
        return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
    }

    /// <summary>True when <paramref name="text"/> has letters and none is lower case (an English word printed in capitals, so its translation is too).</summary>
    public static bool IsCapitals(string text)
    {
        bool letters = false;
        foreach (char c in text ?? string.Empty)
        {
            if (!char.IsLetter(c))
                continue;
            letters = true;
            if (char.IsLower(c))
                return false;
        }
        return letters;
    }

    /// <summary>True when <paramref name="spec"/> is drawn on its art (its look's FormArt is set).</summary>
    public static bool IsArt(FormSpec spec) => spec != null && spec.look != null && spec.look.art != null && spec.look.art.IsSet;

    /// <summary>True when a paper of <paramref name="spec"/> showing <paramref name="data"/> prints the traveller's photo: its template's (FormData.HasPhoto), or its art's window on a paper with no Photo field (FormArt.photo).</summary>
    public static bool ShowsPhoto(FormSpec spec, FormData data) => (data != null && data.HasPhoto) || (IsArt(spec) && spec.look.art.photo.IsSet);

    /// <summary>
    /// <paramref name="spec"/>'s art placed <paramref name="width"/> wide
    /// showing <paramref name="data"/> (the page as tall as the look's aspect
    /// makes it; sizes in the caller's units, the print unit the style's,
    /// FormLayout.PrintUnit, which a stamp's mark and a rule are sized by).
    /// </summary>
    public static PlacedForm Place(FormSpec spec, FormData data, float width, FormMetrics m)
    {
        data ??= new FormData();
        FormArt art = spec.look.art;
        bool worded = art.Worded(data);
        float aspect = spec.look.AspectOr((m ?? new FormMetrics()).aspect);
        float height = width / aspect;
        var items = new List<FormItem>();
        var slots = new List<FormSlot>();
        FaceRect On(ArtBox b) => b.On(width, height);

        foreach (ArtBox cover in art.covers ?? new ArtBox[0])
            if (cover.IsSet)
                items.Add(Item(FormItemKind.Cover, On(cover), data.Cover ?? string.Empty));
        if (art.spine.IsSet)
            items.Add(Item(FormItemKind.Spine, On(art.spine)));
        foreach (ArtField f in Ordered(art))
            if (f.label.IsSet && Hidden(data, f.field) && !worded)
                items.Add(Item(FormItemKind.Patch, On(f.label)));

        if (art.stamp.IsSet)
        {
            FaceRect box = On(art.stamp);
            if (!string.IsNullOrEmpty(data.Emblem))
            {
                float side = EmblemShare * Math.Min(box.Width, box.Height);
                items.Add(Item(FormItemKind.Watermark, FaceRect.FromTop(box.CentreX - side / 2f, box.CentreY - side / 2f, side, side), data.Emblem));
            }
            items.Add(Item(FormItemKind.StampArea, box, FormLayout.VisaBox));
            if (!string.IsNullOrEmpty(art.stampCaption))
            {
                float size = CaptionShare * box.Height;
                items.Add(new FormItem(FormItemKind.Text, FormTextRole.Caption, FaceRect.FromTop(box.XMin, box.YMin + size, box.Width, size * 1.4f), -1, data.Word(art.stampCaption), size, FormTextAlign.Centre));
            }
        }

        int sealField = SealField(spec), photoField = PhotoField(spec);
        HashSet<int> signed = SignedFields(spec);
        foreach (ArtField f in Ordered(art))
        {
            int slot = slots.Count;
            FaceRect place = On(f.value);
            float valueHeight = place.Height;
            bool cell = worded && art.Regrown && Stacked(f) && !Hidden(data, f.field) && f.field != sealField && f.field != photoField;
            FaceRect labelPlace = cell ? Cell(On(f.label), place, art, out place) : default;
            if (f.field == sealField)
            {
                items.Add(new FormItem(FormItemKind.Seal, FormTextRole.Value, place, slot, Value(data, f.field), 0f, FormTextAlign.Centre));
            }
            else if (f.field == photoField)
            {
                if (!data.HasPhoto)
                    continue;
                place = Photo(place);
                items.Add(Item(FormItemKind.Photo, place, string.Empty, slot));
            }
            else
            {
                string value = Value(data, f.field);
                bool hand = signed.Contains(f.field);
                float size = Grown(art.valueShare * valueHeight, art.valueGrowth, ValueFill * place.Height);
                if (hand && string.IsNullOrWhiteSpace(value))
                    items.Add(new FormItem(FormItemKind.Text, FormTextRole.Caption, place, slot, data.Word(FormLayout.Unsigned), UnsignedShare * size, FormTextAlign.Left));
                else if (!string.IsNullOrEmpty(value))
                    items.Add(new FormItem(FormItemKind.Text, hand ? FormTextRole.Hand : FormTextRole.Value, place, slot, value, size, FormTextAlign.Left));
                if ((f.relabel || worded) && f.label.IsSet && !Hidden(data, f.field))
                {
                    FaceRect label = cell ? labelPlace : On(f.label);
                    float labelSize = cell ? labelPlace.Height / LabelLine : Grown(art.labelShare * label.Height, worded ? art.labelGrowth : 1f, label.Height / LabelLine);
                    string english = !f.relabel && !string.IsNullOrEmpty(f.labelText) ? f.labelText : f.field < data.FieldLabels.Count ? data.FieldLabels[f.field] ?? string.Empty : string.Empty;
                    items.Add(new FormItem(FormItemKind.Text, FormTextRole.Label, label, slot, Capitals(data.Word(english)), labelSize, FormTextAlign.Left));
                }
            }
            slots.Add(new FormSlot(slot, f.field, -1, string.Empty, place, 0));
        }

        if (art.photo.IsSet && !data.HasPhoto)
            items.Add(Item(FormItemKind.Photo, Photo(On(art.photo))));
        if (worded)
            foreach (ArtCaption c in art.captions ?? new ArtCaption[0])
                if (c != null && c.rect.IsSet && !string.IsNullOrWhiteSpace(c.text))
                    items.Add(Caption(c, On(c.rect), data, art));
        foreach (ArtPrint p in art.prints ?? new ArtPrint[0])
        {
            if (p == null || !p.rect.IsSet || (p.field >= 0 && Hidden(data, p.field)))
                continue;
            string words = PrintText(p, data);
            if (string.IsNullOrEmpty(words))
                continue;
            FaceRect place = On(p.rect);
            bool hand = p.kind == ArtPrintKind.Signature || p.kind == ArtPrintKind.Office;
            items.Add(new FormItem(FormItemKind.Text, hand ? FormTextRole.Hand : FormTextRole.Value, place, -1, words, Grown(art.valueShare * place.Height, art.valueGrowth, ValueFill * place.Height), FormTextAlign.Left));
        }
        return new PlacedForm(width, height, height, items, slots, new List<float> { 0f }, FormLayout.PrintUnit(spec, width, m));
    }

    /// <summary>
    /// The art's half of the face check (FormLayout.Check, FO10) for
    /// <paramref name="spec"/> showing <paramref name="probe"/> (each field at
    /// its longest): a field with no place on the art or two, a place naming a
    /// field the template does not have, a place off the face or without an
    /// area, a relabelled field without a label's place, a print naming no
    /// field or printing nothing, a caption without a visa box, an art without
    /// its aspect, and a value that does not fit its place at FitFloor of its
    /// size (measured wrapped at its place's width). Empty when the art fits.
    /// </summary>
    public static List<string> Problems(FormSpec spec, FormData probe, ITextMeasure measure)
    {
        var problems = new List<string>();
        FormArt art = spec.look.art;
        int count = probe.FieldLabels.Count;
        if (spec.look.aspect <= 0f)
            problems.Add("its art has no aspect: set the look's aspect to the art's width over its height");
        for (int f = 0; f < count; f++)
        {
            int placed = art.fields.Count(a => a != null && a.field == f);
            if (placed == 0)
                problems.Add($"field {f} ({probe.FieldLabels[f]}) has no place on the art");
            else if (placed > 1)
                problems.Add($"field {f} ({probe.FieldLabels[f]}) has {placed} places on the art");
        }
        foreach (ArtField a in art.fields)
        {
            if (a == null)
                continue;
            if (a.field < 0 || a.field >= count)
                problems.Add($"the art places field {a.field}, but the template has {count} fields");
            if (!a.value.IsSet || !a.value.OnFace)
                problems.Add($"field {a.field}'s place on the art is empty or off the face");
            if ((a.relabel && !a.label.IsSet) || (a.label.IsSet && !a.label.OnFace))
                problems.Add($"field {a.field}'s label place on the art is missing or off the face");
        }
        foreach (ArtPrint p in art.prints ?? new ArtPrint[0])
        {
            if (p == null)
                continue;
            if (!p.rect.IsSet || !p.rect.OnFace)
                problems.Add($"a {p.kind} print's place on the art is empty or off the face");
            if ((p.kind == ArtPrintKind.Copy || p.kind == ArtPrintKind.Signature) && (p.field < 0 || p.field >= count))
                problems.Add($"a {p.kind} print repeats field {p.field}, but the template has {count} fields");
            if (p.kind == ArtPrintKind.Text && string.IsNullOrWhiteSpace(p.text))
                problems.Add("a Text print on the art has no words");
        }
        if (!string.IsNullOrEmpty(art.stampCaption) && !art.stamp.IsSet)
            problems.Add("the art captions a visa box it does not place");
        if (problems.Count > 0 || measure == null)
            return problems;

        PlacedForm full = Place(spec, probe, spec.look.AspectOr(1f), null);
        foreach (FormItem item in full.Items)
        {
            if (item.Kind != FormItemKind.Text || item.Slot < 0 || item.Role == FormTextRole.Label)
                continue;
            float need = measure.Height(item.Text, item.Role, item.Size * FitFloorOf(art, item.Role), item.Rect.Width);
            if (need > item.Rect.Height * 1.001f)
            {
                int field = full.Slots[item.Slot].Field;
                problems.Add($"field {field} ({probe.FieldLabels[field]})'s longest value does not fit its place on the art even at {FitFloor:0.0} of its size");
            }
        }
        return problems;
    }

    /// <summary>How much taller than one line a single word may measure in its place before it counts as broken across lines (a hair over one line).</summary>
    private const float OneLine = 1.2f;

    /// <summary>The width a word is measured at on one line (in the form's units: more than any page).</summary>
    private const float Unwrapped = 1e5f;

    /// <summary>
    /// The words a paper of <paramref name="spec"/> would cut (BUG-9: the
    /// paper never breaks a word across lines, TmpFormText.Unbreak, so a word
    /// wider than its place even at the least size, FitFloorOf, is cut with a
    /// full stop, and a cut name or origin would read as a false discrepancy
    /// against another paper): for each field, each of
    /// <paramref name="words"/>[field] (the content's longest words that field
    /// prints: its names' or its origins'), measured alone in the field's
    /// value or hand style at the least size its place lets it shrink to; a
    /// problem for each field one of whose words breaks there. Empty off the
    /// art, without a measure, or when every word fits.
    /// </summary>
    public static List<string> CutWords(FormSpec spec, IReadOnlyList<IReadOnlyList<string>> words, ITextMeasure measure)
    {
        var problems = new List<string>();
        if (!IsArt(spec) || words == null || measure == null)
            return problems;
        FormArt art = spec.look.art;
        int count = Math.Max(words.Count, art.fields.Where(f => f != null).Select(f => f.field + 1).DefaultIfEmpty(0).Max());
        List<string> labels = Enumerable.Repeat("FIELD", count).ToList();
        var data = new FormData { FieldLabels = labels, FieldValues = Enumerable.Repeat("W", count).ToList(), HasPhoto = true };
        PlacedForm form = Place(spec, data, spec.look.AspectOr(1f), null);
        var reported = new HashSet<int>();
        foreach (FormItem item in form.Items)
        {
            if (item.Kind != FormItemKind.Text || item.Slot < 0 || (item.Role != FormTextRole.Value && item.Role != FormTextRole.Hand))
                continue;
            int field = form.Slots[item.Slot].Field;
            if (field < 0 || field >= words.Count || words[field] == null || reported.Contains(field))
                continue;
            float size = item.Size * FitFloorOf(art, item.Role);
            foreach (string word in words[field])
            {
                if (string.IsNullOrEmpty(word) || measure.Height(word, item.Role, size, item.Rect.Width) <= measure.Height(word, item.Role, size, Unwrapped) * OneLine)
                    continue;
                problems.Add($"field {field}'s value word '{word}' is wider than its place on the art even at its least size: the paper would cut it (re-lay the place, or shorten the content)");
                reported.Add(field);
                break;
            }
        }
        return problems;
    }

    /// <summary>An art caption printed in the reading language at its place (a label or title in capitals, a paragraph as written), never picked; a label caption grows with the art's labels (labelGrowth), never past a line of its place.</summary>
    private static FormItem Caption(ArtCaption c, FaceRect place, FormData data, FormArt art)
    {
        string words = data.Word(c.text) ?? string.Empty;
        bool paragraph = c.kind == ArtCaptionKind.Paragraph;
        FormTextRole role = c.kind == ArtCaptionKind.Title ? FormTextRole.Title : paragraph ? FormTextRole.Paragraph : FormTextRole.Label;
        float size = c.share * place.Height;
        if (role == FormTextRole.Label)
            size = Grown(size, art.labelGrowth, place.Height / LabelLine);
        return new FormItem(FormItemKind.Text, role, place, -1, paragraph ? words : Capitals(words), size,
                            c.centre ? FormTextAlign.Centre : FormTextAlign.Left, string.IsNullOrEmpty(c.ink) ? null : c.ink);
    }

    /// <summary>True when <paramref name="f"/>'s label stands above its value (a cell to re-lay: the label a band at its top, ArtLayout.Cell); a label beside its value keeps its place, and each grows inside its own.</summary>
    public static bool Stacked(ArtField f) => f != null && f.label.IsSet && f.value.IsSet && f.label.y1 <= f.value.y0 + 0.25f * (f.value.y1 - f.value.y0);

    /// <summary><paramref name="size"/> grown by <paramref name="growth"/> but never past <paramref name="room"/> (nor below the size it was); a growth of 1 or less leaves it as it is.</summary>
    public static float Grown(float size, float growth, float room) => growth > 1f ? Math.Max(size, Math.Min(size * growth, room)) : size;

    /// <summary>
    /// A labelled field's cell re-laid for larger text (BUG-9; the art's
    /// labels painted out of its clean face, FormArt.printLabels): from its
    /// label's top to its value's foot, the label a band at the top
    /// (labelShare of the art's label grown by labelGrowth, LabelLine of it
    /// tall, as wide as the label and the value together) and the value the
    /// rest, below it (<paramref name="valuePlace"/>; the field is picked
    /// there), never less than half its own place. Returns the label's band.
    /// </summary>
    public static FaceRect Cell(FaceRect label, FaceRect value, FormArt art, out FaceRect valuePlace)
    {
        float top = Math.Min(label.YMin, value.YMin);
        float band = art.labelShare * label.Height * Math.Max(1f, art.labelGrowth) * LabelLine;
        float left = Math.Min(label.XMin, value.XMin);
        float right = Math.Max(label.XMax, value.XMax);
        float valueTop = Math.Min(top + band, value.YMax - value.Height * 0.5f);
        valuePlace = new FaceRect(value.XMin, valueTop, value.XMax, value.YMax);
        return new FaceRect(left, top, right, valueTop);
    }

    /// <summary>The art's fields in reading order (top to bottom, then left to right): the slots' order.</summary>
    private static IEnumerable<ArtField> Ordered(FormArt art) =>
        (art.fields ?? new ArtField[0]).Where(f => f != null).OrderBy(f => Math.Round(f.value.y0, 2)).ThenBy(f => f.value.x0);

    /// <summary>The photo's 4:5 window (LookCanvas.PhotoAspect) as large as fits <paramref name="window"/>, centred in it.</summary>
    private static FaceRect Photo(FaceRect window)
    {
        float w = Math.Min(window.Width, window.Height * LookCanvas.PhotoAspect);
        float h = w / LookCanvas.PhotoAspect;
        return FaceRect.FromTop(window.CentreX - w / 2f, window.CentreY - h / 2f, w, h);
    }

    /// <summary>The words a print shows from <paramref name="data"/> (empty: nothing to print).</summary>
    private static string PrintText(ArtPrint p, FormData data)
    {
        switch (p.kind)
        {
            case ArtPrintKind.Copy:
            case ArtPrintKind.Signature:
                return Value(data, p.field);
            case ArtPrintKind.NationCode:
                return data.NationCode ?? string.Empty;
            case ArtPrintKind.Serial:
                return data.Serial ?? string.Empty;
            case ArtPrintKind.Office:
                return data.Word(data.Programme) ?? string.Empty; // a name printed as a label: the reading language's (Saleh 2026-10-17)
            default:
                return p.text ?? string.Empty;
        }
    }

    /// <summary>Field <paramref name="field"/>'s shown value (empty past the values).</summary>
    private static string Value(FormData data, int field) =>
        field >= 0 && field < data.FieldValues.Count ? data.FieldValues[field] ?? string.Empty : string.Empty;

    /// <summary>True when field <paramref name="field"/> is not introduced yet (FormData.FieldHidden).</summary>
    private static bool Hidden(FormData data, int field) =>
        data.FieldHidden != null && field >= 0 && field < data.FieldHidden.Count && data.FieldHidden[field];

    /// <summary>The field the form's header prints as its seal (the document design spec, D4), or -1.</summary>
    private static int SealField(FormSpec spec) =>
        (spec.blocks ?? new FormBlock[0]).Where(b => b != null && b.kind == FormBlockKind.Header).Select(b => b.field).DefaultIfEmpty(-1).First();

    /// <summary>The field the form's photo cell names (D8), or -1.</summary>
    private static int PhotoField(FormSpec spec) =>
        (spec.blocks ?? new FormBlock[0]).Where(b => b != null).SelectMany(b => b.cells ?? new FormCell[0]).Where(c => c != null && c.IsPhoto).Select(c => c.field).DefaultIfEmpty(-1).First();

    /// <summary>The fields the form's Signature blocks sign (printed in a hand).</summary>
    private static HashSet<int> SignedFields(FormSpec spec) =>
        new HashSet<int>((spec.blocks ?? new FormBlock[0]).Where(b => b != null && b.kind == FormBlockKind.Signature && b.field >= 0).Select(b => b.field));

    private static FormItem Item(FormItemKind kind, FaceRect rect, string text = "", int slot = -1) =>
        new FormItem(kind, FormTextRole.Value, rect, slot, text, 0f, FormTextAlign.Left);
}
