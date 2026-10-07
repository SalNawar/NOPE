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
    /// <summary>The least share of its size a value or a label shrinks to so that it fits its place (the renderers' fit; the check measures the longest value at it).</summary>
    public const float FitFloor = 0.5f;

    /// <summary>The nation's emblem inside a visa box, as a share of the box's smaller side.</summary>
    public const float EmblemShare = 0.7f;

    /// <summary>The visa box's caption's size as a share of the box's height.</summary>
    public const float CaptionShare = 0.06f;

    /// <summary>The share of its place's height UNSIGNED takes (a caption's size in a hand's place).</summary>
    public const float UnsignedShare = 0.6f;

    /// <summary>
    /// <paramref name="text"/> in capitals as a printed form sets them: upper
    /// case (invariant), a Greek capital without its accent (Greek writes
    /// capitals bare: "Υπηρεσία" gives ΥΠΗΡΕΣΙΑ; the diaeresis stays).
    /// </summary>
    public static string Capitals(string text)
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
                sb.Append(c);
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
                if (hand && string.IsNullOrWhiteSpace(value))
                    items.Add(new FormItem(FormItemKind.Text, FormTextRole.Caption, place, slot, data.Word(FormLayout.Unsigned), UnsignedShare * art.valueShare * place.Height, FormTextAlign.Left));
                else if (!string.IsNullOrEmpty(value))
                    items.Add(new FormItem(FormItemKind.Text, hand ? FormTextRole.Hand : FormTextRole.Value, place, slot, value, art.valueShare * place.Height, f.centre ? FormTextAlign.Centre : FormTextAlign.Left));
                if ((f.relabel || worded) && f.label.IsSet && !Hidden(data, f.field))
                {
                    FaceRect label = On(f.label);
                    string english = !f.relabel && !string.IsNullOrEmpty(f.labelText) ? f.labelText : f.field < data.FieldLabels.Count ? data.FieldLabels[f.field] ?? string.Empty : string.Empty;
                    items.Add(new FormItem(FormItemKind.Text, FormTextRole.Label, label, slot, Capitals(data.Word(english)), art.labelShare * label.Height, FormTextAlign.Left));
                }
            }
            slots.Add(new FormSlot(slot, f.field, -1, string.Empty, place, 0));
        }

        if (art.photo.IsSet && !data.HasPhoto)
            items.Add(Item(FormItemKind.Photo, Photo(On(art.photo))));
        if (worded)
            foreach (ArtCaption c in art.captions ?? new ArtCaption[0])
                if (c != null && c.rect.IsSet && !string.IsNullOrWhiteSpace(c.text))
                    items.Add(Caption(c, On(c.rect), data));
        foreach (ArtPrint p in art.prints ?? new ArtPrint[0])
        {
            if (p == null || !p.rect.IsSet || (p.field >= 0 && Hidden(data, p.field)))
                continue;
            string words = PrintText(p, data);
            if (string.IsNullOrEmpty(words))
                continue;
            FaceRect place = On(p.rect);
            bool hand = p.kind == ArtPrintKind.Signature || p.kind == ArtPrintKind.Office;
            items.Add(new FormItem(FormItemKind.Text, hand ? FormTextRole.Hand : FormTextRole.Value, place, -1, words, art.valueShare * place.Height, FormTextAlign.Left));
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
            float need = measure.Height(item.Text, item.Role, item.Size * FitFloor, item.Rect.Width);
            if (need > item.Rect.Height * 1.001f)
            {
                int field = full.Slots[item.Slot].Field;
                problems.Add($"field {field} ({probe.FieldLabels[field]})'s longest value does not fit its place on the art even at {FitFloor:0.0} of its size");
            }
        }
        return problems;
    }

    /// <summary>An art caption printed in the reading language at its place (a label or title in capitals, a paragraph as written), never picked.</summary>
    private static FormItem Caption(ArtCaption c, FaceRect place, FormData data)
    {
        string words = data.Word(c.text) ?? string.Empty;
        bool paragraph = c.kind == ArtCaptionKind.Paragraph;
        FormTextRole role = c.kind == ArtCaptionKind.Title ? FormTextRole.Title : paragraph ? FormTextRole.Paragraph : FormTextRole.Label;
        return new FormItem(FormItemKind.Text, role, place, -1, paragraph ? words : Capitals(words), c.share * place.Height,
                            c.centre ? FormTextAlign.Centre : FormTextAlign.Left, string.IsNullOrEmpty(c.ink) ? null : c.ink);
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
                return data.Programme ?? string.Empty;
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
