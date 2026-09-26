using System.Collections.Generic;

/// <summary>
/// The form a document prints (redesign phase 4, PC spec FO1, FO3): its
/// template's FormSpec and what it shows (FormData): the agency block's name
/// and programme, the template's form number and name, the paper's serial, the
/// photo, and each field's label and value, always in English (forms are
/// diegetic). The desk paper prints it; the PC's scanned copy will (phase 5).
/// Probe and Problems are the one form check Build Office UI and the content
/// validator run on every template (FO10).
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

    /// <summary>The form <paramref name="doc"/> prints, with <paramref name="agency"/>'s name and programme; null without a template.</summary>
    public static DocumentForm For(DocumentInstance doc, AgencyContent agency)
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
        return new DocumentForm(doc.template.form, data);
    }

    /// <summary>
    /// Every problem of <paramref name="template"/>'s form (FormLayout.Check):
    /// its fields' labels, each value at its category's longest
    /// (FieldLengths, an origin at <paramref name="longestOrigin"/>
    /// characters), a full serial; plus a form that sets its own number or
    /// title (a document prints its template's) or breaks onto a second page
    /// (a paper is one page, traveller-types F1). Empty when it fits.
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
        if (spec.PageCount > 1)
            problems.Add($"its form has {spec.PageCount} pages; a paper is one page");

        FieldSpecsOf(template, longestOrigin, out List<string> labels, out List<int> longest);
        FormData probe = Heading(template, agency);
        probe.Serial = FormSerials.Make(template.formNumber, 0, 0);
        probe.FieldLabels = labels;
        probe.FieldValues = labels.ConvertAll(_ => string.Empty);
        foreach (string p in FormLayout.Check(spec, FormLayout.Probe(probe, longest), metrics, measure))
            problems.Add(p);
        return problems;
    }

    /// <summary>The header's words and the photo: the agency's name and programme, the template's number and name.</summary>
    private static FormData Heading(DocumentTemplateSO template, AgencyContent agency) => new FormData
    {
        Agency = agency != null ? agency.name ?? string.Empty : string.Empty,
        Programme = agency != null ? agency.programme ?? string.Empty : string.Empty,
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
