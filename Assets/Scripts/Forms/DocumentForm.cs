using System.Collections.Generic;

/// <summary>
/// The form a document prints (redesign phase 4, PC spec FO1, FO3): its
/// template's FormSpec and what it shows (FormData): the agency block's name
/// and, as its programme line, the name of the office that issues the form
/// (agency.offices; the document design spec, D4), the template's form number
/// and name, the paper's serial, the photo, and each field's label and value,
/// always in English (forms are diegetic), each box keeping the room its
/// longest value needs (FieldReserve; D2); a passport also its holder's
/// nation's cover and emblem and its machine-readable zone (the travel
/// documents spec, TD3). The desk paper prints it, and the
/// PC's scanned copy draws it (phase 5). Probe and Problems are the one form
/// check Build Office UI and the content validator run on every template (FO10).
/// </summary>
public sealed class DocumentForm
{
    /// <summary>A form from its spec and content.</summary>
    public DocumentForm(FormSpec spec, FormData data)
    {
        Spec = spec ?? new FormSpec();
        Data = data ?? new FormData();
    }

    /// <summary>The template's form.</summary>
    public FormSpec Spec { get; }

    /// <summary>What it shows.</summary>
    public FormData Data { get; }

    /// <summary>The form <paramref name="doc"/> prints, with <paramref name="agency"/>'s name and its office's (the programme's without one), each box keeping the room of its category's longest value (an origin at <paramref name="longestOrigin"/> characters: ContentLibrarySO.LongestOriginLabel); a booklet in the cover and emblem of <paramref name="passportNation"/> (CaseInstance.passportNation; none: the form's own cover), and a form with a machine-readable zone the zone of its printed values (MachineZone, the nation's code); hiding each field <paramref name="shows"/> refuses (its form number and category: Introductions.ShowsField; null shows every field); null without a template.</summary>
    public static DocumentForm For(DocumentInstance doc, AgencyContent agency, int longestOrigin, NationSO passportNation = null, System.Func<string, ClueCategory, bool> shows = null)
    {
        if (doc == null || doc.template == null)
            return null;
        var labels = new List<string>(doc.fields.Count);
        var values = new List<string>(doc.fields.Count);
        foreach (DocumentField f in doc.fields)
        {
            labels.Add(f != null ? f.label : string.Empty);
            values.Add(f != null ? f.value : string.Empty);
        }
        FormData data = Heading(doc.template, agency);
        data.Serial = doc.serial ?? string.Empty;
        data.FieldLabels = labels;
        data.FieldValues = values;
        if (shows != null)
            data.FieldHidden = doc.fields.ConvertAll(f => f != null && !shows(doc.template.formNumber, f.category));
        FieldSpecsOf(doc.template, longestOrigin, out _, out List<int> longest);
        data.FieldReserve = FormLayout.Reserve(values, longest);
        PassportLook passport = passportNation != null ? passportNation.passport : null;
        if (passport != null)
        {
            data.Cover = passport.cover ?? string.Empty;
            data.Emblem = passport.emblem ?? string.Empty;
            data.Issuer = passportNation.id ?? string.Empty;
        }
        if (HasZone(doc.template.form))
            data.Mrz = MachineZone.Lines(passport != null ? passport.code : string.Empty, ValueOf(doc, ClueCategory.Name), ValueOf(doc, ClueCategory.CitizenId),
                                         ValueOf(doc, ClueCategory.BirthDate), ValueOf(doc, ClueCategory.Expiry));
        return new DocumentForm(doc.template.form, data);
    }

    /// <summary>True when <paramref name="spec"/> prints a machine-readable zone (an Mrz block).</summary>
    private static bool HasZone(FormSpec spec)
    {
        foreach (FormBlock b in spec != null && spec.blocks != null ? spec.blocks : new FormBlock[0])
            if (b != null && b.kind == FormBlockKind.Mrz)
                return true;
        return false;
    }

    /// <summary>The value of <paramref name="doc"/>'s first field of <paramref name="category"/>, or blank.</summary>
    private static string ValueOf(DocumentInstance doc, ClueCategory category)
    {
        foreach (DocumentField f in doc.fields)
            if (f != null && f.category == category)
                return f.value ?? string.Empty;
        return string.Empty;
    }

    /// <summary>
    /// Every problem of <paramref name="template"/>'s form (FormLayout.Check):
    /// its fields' labels, each value at its category's longest
    /// (FieldLengths, an origin at <paramref name="longestOrigin"/>
    /// characters), a full serial, each box reserving that longest value, so a
    /// box that would move with its value is one too (the document design spec,
    /// D2); plus a form that sets its own number or title (a document prints
    /// its template's) or breaks onto a second page (a paper is one page,
    /// traveller-types F1); its look (FormLook.Problems); and the visual
    /// checks' places (D4, D8): one Seal field, printed by the header; a
    /// photo document's one Photo field, named by its photo cell. Empty when it fits.
    /// </summary>
    public static List<string> Problems(DocumentTemplateSO template, AgencyContent agency, int longestOrigin, FormMetrics metrics, ITextMeasure measure)
    {
        var problems = new List<string>();
        if (template == null)
            return problems;
        FormSpec spec = template.form ?? new FormSpec();
        if (spec.blocks == null || spec.blocks.Length == 0)
        {
            problems.Add("has no form: its fields are printed nowhere");
            return problems;
        }
        if (!string.IsNullOrEmpty(spec.formNumber) || !string.IsNullOrEmpty(spec.title))
            problems.Add("its form sets a form number or title; a document prints its template's formNumber and displayName");
        if (!spec.fixedPage)
            problems.Add("its form flows; a document's form is a fixed page");
        if (spec.landscape)
            problems.Add("its form is landscape; a paper is portrait");
        if (spec.PageCount > 1)
            problems.Add($"its form has {spec.PageCount} pages; a paper is one page");
        foreach (string p in (spec.look ?? new FormLook()).Problems(metrics != null ? metrics.aspect : new FormMetrics().aspect))
            problems.Add(p);
        problems.AddRange(VisualFieldProblems(template, spec));

        FieldSpecsOf(template, longestOrigin, out List<string> labels, out List<int> longest);
        FormData probe = Heading(template, agency);
        probe.Serial = FormSerials.Make(template.formNumber, 0, 0);
        probe.FieldLabels = labels;
        probe.FieldValues = labels.ConvertAll(_ => string.Empty);
        FormData full = FormLayout.Probe(probe, longest);
        full.FieldReserve = full.FieldValues;
        if (HasZone(spec))
            full.Mrz = MachineZone.Lines("XXX", full.FieldValues.Count > 0 ? full.FieldValues[0] : string.Empty, string.Empty, string.Empty, string.Empty);
        foreach (string p in FormLayout.Check(spec, full, metrics, measure))
            problems.Add(p);
        return problems;
    }

    /// <summary>The seal's and the photo's places (the document design spec, D4, D8): exactly one Seal field, the one the header prints; with a photo, exactly one Photo field, the one the photo cell names; without, none.</summary>
    private static IEnumerable<string> VisualFieldProblems(DocumentTemplateSO template, FormSpec spec)
    {
        var specs = template.fieldSpecs ?? new DocumentFieldSpec[0];
        var seals = new List<int>();
        var photos = new List<int>();
        for (int i = 0; i < specs.Length; i++)
        {
            if (specs[i] != null && specs[i].category == ClueCategory.Seal)
                seals.Add(i);
            if (specs[i] != null && specs[i].category == ClueCategory.Photo)
                photos.Add(i);
        }
        int header = -1, cell = -1;
        foreach (FormBlock b in spec.blocks ?? new FormBlock[0])
        {
            if (b == null)
                continue;
            if (b.kind == FormBlockKind.Header)
                header = b.field;
            foreach (FormCell c in b.cells ?? new FormCell[0])
                if (c != null && c.IsPhoto)
                    cell = c.field;
        }
        if (seals.Count != 1)
            yield return $"it has {seals.Count} Seal fields; a paper has one, its issuing office's seal";
        else if (header != seals[0])
            yield return $"its header prints field {header}, not its Seal field {seals[0]}";
        if (template.showsPhoto && (photos.Count != 1 || cell != photos[0]))
            yield return $"it shows a photo but its photo cell names field {cell}, not its one Photo field";
        if (!template.showsPhoto && photos.Count > 0)
            yield return "it has a Photo field but shows no photo";
    }

    /// <summary>The header's words and the photo: the agency's name, the issuing office's name as the programme line (the agency's programme when no office issues the form), the template's number and name.</summary>
    private static FormData Heading(DocumentTemplateSO template, AgencyContent agency) => new FormData
    {
        Agency = agency != null ? agency.name ?? string.Empty : string.Empty,
        Programme = agency == null ? string.Empty : Seals.OfficeOf(agency.offices, template.formNumber)?.name ?? agency.programme ?? string.Empty,
        FormNumber = template.formNumber ?? string.Empty,
        Title = template.displayName ?? string.Empty,
        HasPhoto = template.showsPhoto
    };

    /// <summary>A template's field labels and each field's longest value length, by field index.</summary>
    private static void FieldSpecsOf(DocumentTemplateSO template, int longestOrigin, out List<string> labels, out List<int> longest)
    {
        labels = new List<string>();
        longest = new List<int>();
        foreach (DocumentFieldSpec spec in template.fieldSpecs ?? new DocumentFieldSpec[0])
        {
            labels.Add(spec != null ? spec.label : string.Empty);
            longest.Add(spec != null ? FieldLengths.Longest(spec.category, longestOrigin) : 0);
        }
    }
}
