using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// One fault a desk machine shows and the print does not (the desk machines
/// spec §1-3): on paper <see cref="Document"/>, the box of
/// <see cref="Category"/> reads another value under the UV torch (a ghost of
/// the value washed off it) or in the chip (the value the issuing office
/// wrote). A hidden lie's tell: the papers themselves are perfect to the eye.
/// </summary>
public readonly struct HiddenTell
{
    /// <summary>A tell.</summary>
    public HiddenTell(int document, ClueCategory category, ScanLayer layer, string value)
    {
        Document = document;
        Category = category;
        Layer = layer;
        Value = value ?? string.Empty;
    }

    /// <summary>The paper, by its index in the case.</summary>
    public int Document { get; }

    /// <summary>The box's category.</summary>
    public ClueCategory Category { get; }

    /// <summary>Where it shows: Uv (the torch) or Chip (the reader).</summary>
    public ScanLayer Layer { get; }

    /// <summary>What it reads there (the ghost's or the chip's value).</summary>
    public string Value { get; }
}

/// <summary>
/// The agency's hidden-layer content (world_source.json agency.hidden; the
/// desk machines spec §3): which forms carry a chip, and how often a doctored
/// value leaves a ghost under UV. Content, written into the content library by
/// Generate World.
/// </summary>
[Serializable]
public sealed class HiddenContent
{
    /// <summary>The forms whose paper carries a chip the reader reads (TC-101 the Passport, TC-240 the Transponder Card).</summary>
    public List<string> chipForms = new List<string>();

    /// <summary>The chance a value a forger printed over the issued one (a record lie's box) leaves the ghost of the issued value under UV (0 to 1; one draw per such box on the case's hidden stream).</summary>
    public float ghostChance;

    /// <summary>The forms whose paper prints a microprint line along its foot repeating the holder's Citizen ID (the desk machines spec §10); the lens stand's magnifiers that read it were cut by P0-C, 2026-10-20: until the scanner shows the microprint, FIX_PLAN_v2 #17, nothing reads it.</summary>
    public List<string> microForms = new List<string>();

    /// <summary>True when paper <paramref name="formNumber"/> carries a chip.</summary>
    public bool CarriesChip(string formNumber) => !string.IsNullOrEmpty(formNumber) && chipForms != null && chipForms.Contains(formNumber);

    /// <summary>True when paper <paramref name="formNumber"/> prints a microprint line.</summary>
    public bool PrintsMicro(string formNumber) => !string.IsNullOrEmpty(formNumber) && microForms != null && microForms.Contains(formNumber);

    /// <summary>What Generate World and the validator refuse: a blank or repeated chip or microprint form, a ghost chance outside 0 to 1. Empty when sound.</summary>
    public List<string> Problems()
    {
        var problems = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (string form in chipForms ?? new List<string>())
        {
            if (string.IsNullOrWhiteSpace(form))
                problems.Add("agency.hidden.chipForms lists a blank form.");
            else if (!seen.Add(form))
                problems.Add($"agency.hidden.chipForms lists '{form}' twice.");
        }
        if (ghostChance < 0f || ghostChance > 1f)
            problems.Add($"agency.hidden.ghostChance is {ghostChance}: a chance from 0 to 1.");
        var micro = new HashSet<string>(StringComparer.Ordinal);
        foreach (string form in microForms ?? new List<string>())
        {
            if (string.IsNullOrWhiteSpace(form))
                problems.Add("agency.hidden.microForms lists a blank form.");
            else if (!micro.Add(form))
                problems.Add($"agency.hidden.microForms lists '{form}' twice.");
        }
        return problems;
    }
}

/// <summary>One paper as the hidden layers read it: its form and its fields as printed (a forged box keeps the value it covers, DocumentField.erased).</summary>
public readonly struct HiddenPaper
{
    /// <summary>A paper.</summary>
    public HiddenPaper(string formNumber, IReadOnlyList<DocumentField> fields)
    {
        FormNumber = formNumber ?? string.Empty;
        Fields = fields ?? Array.Empty<DocumentField>();
    }

    /// <summary>The agency form number.</summary>
    public string FormNumber { get; }

    /// <summary>The printed fields, in form order.</summary>
    public IReadOnlyList<DocumentField> Fields { get; }
}

/// <summary>A paper's microprint line (the desk machines spec §10): the box it repeats (the Citizen ID's index in the paper's fields) and what it repeats.</summary>
public readonly struct MicroLine
{
    /// <summary>A line.</summary>
    public MicroLine(int field, string value)
    {
        Field = field;
        _value = value;
    }

    private readonly string _value;

    /// <summary>The box it repeats: the Citizen ID's index in the paper's fields.</summary>
    public int Field { get; }

    /// <summary>The Citizen ID it repeats.</summary>
    public string Value => _value ?? string.Empty;

    /// <summary>The line as printed: the Bureau's mark, the form and the value, over and over (<paramref name="repeats"/> times, at least once).</summary>
    public string Printed(string formNumber, int repeats) =>
        string.Concat(Enumerable.Repeat("TCB · " + formNumber + " · " + Value + " · ", Math.Max(1, repeats)));
}

/// <summary>
/// One case's hidden layers (the desk machines spec §1-3): per paper, the
/// marks the UV torch shows and the record its chip holds. The scanned copy's
/// UV and CHIP tabs read it (MachineLayers), and so do the torch and the reader
/// at the desk. Built once per case by <see cref="HiddenLayers.Build"/>.
/// </summary>
public sealed class CaseHidden
{
    private readonly IReadOnlyList<UvMark>[] _marks;
    private readonly IReadOnlyList<DocumentField>[] _chips;
    private readonly MicroLine?[] _micro;

    /// <summary>No paper, no layer (a case without papers).</summary>
    public static readonly CaseHidden None = new CaseHidden(Array.Empty<IReadOnlyList<UvMark>>(), Array.Empty<IReadOnlyList<DocumentField>>());

    /// <summary>A case's layers: the marks, chip (null: none) and microprint line (null: none) of each paper, by index.</summary>
    public CaseHidden(IReadOnlyList<UvMark>[] marks, IReadOnlyList<DocumentField>[] chips, MicroLine?[] micro = null)
    {
        _marks = marks ?? Array.Empty<IReadOnlyList<UvMark>>();
        _chips = chips ?? Array.Empty<IReadOnlyList<DocumentField>>();
        _micro = micro ?? Array.Empty<MicroLine?>();
    }

    /// <summary>Paper <paramref name="document"/>'s microprint line, or null when it prints none.</summary>
    public MicroLine? MicroOf(int document) => document >= 0 && document < _micro.Length ? _micro[document] : null;

    /// <summary>How many papers it covers.</summary>
    public int Count => _marks.Length;

    /// <summary>The marks paper <paramref name="document"/> shows under UV light (none for an unknown paper).</summary>
    public IReadOnlyList<UvMark> Marks(int document) =>
        document >= 0 && document < _marks.Length && _marks[document] != null ? _marks[document] : Array.Empty<UvMark>();

    /// <summary>The chip paper <paramref name="document"/> carries, one field per printed field in the print's order; null when it carries none.</summary>
    public IReadOnlyList<DocumentField> ChipOf(int document) =>
        document >= 0 && document < _chips.Length ? _chips[document] : null;

    /// <summary>True when paper <paramref name="document"/> carries a chip.</summary>
    public bool HasChip(int document) => ChipOf(document) != null;

    /// <summary>The marks of paper <paramref name="document"/> that show a fault (a ghost, a missing microprint).</summary>
    public List<UvMark> Faults(int document) => Marks(document).Where(m => m.Fault).ToList();
}

/// <summary>
/// The hidden layers' rules (the desk machines spec §1-3; Saleh 2026-10-17:
/// the UV light shows "the hidden watermarks and symbols", the chip reader
/// "reads the chip"). Every paper the agency issues shows its watermark under
/// UV, and every printed seal the issuing office's microprint; a forged seal
/// shows none. A value a forger printed over the issued one (a record lie's
/// box, DocumentField.erased) leaves the ghost of the issued value at the
/// content's ghost chance. A paper of a chip form carries a chip holding what
/// the office issued (the erased value where a forger printed over it). A
/// hidden lie's tells (<see cref="HiddenTell"/>) are written in last: a UV
/// tell is a ghost on its box, a chip tell the chip's value. Nothing
/// differs on an honest paper, so neither machine ever raises a false alarm.
/// Draws: one Value per forged box, in paper then field order, on the case's
/// own hidden stream (Seeds.ForHidden), so no other draw moves. Pure.
/// </summary>
public static class HiddenLayers
{
    /// <summary>The Bureau's watermark, as it reads under UV.</summary>
    public const string Watermark = "TEMPORAL CUSTOMS";

    /// <summary>The layers of <paramref name="papers"/>, with <paramref name="tells"/> (a hidden lie's, or none) written in.</summary>
    public static CaseHidden Build(IReadOnlyList<HiddenPaper> papers, IReadOnlyList<HiddenTell> tells, HiddenContent content, IRandomSource rng)
    {
        int n = papers?.Count ?? 0;
        var marks = new IReadOnlyList<UvMark>[n];
        var chips = new IReadOnlyList<DocumentField>[n];
        var micro = new MicroLine?[n];
        for (int d = 0; d < n; d++)
        {
            HiddenPaper paper = papers[d];
            var list = new List<UvMark> { new UvMark(UvMarkKind.Watermark, -1, Watermark, false) };
            for (int f = 0; f < paper.Fields.Count; f++)
            {
                DocumentField field = paper.Fields[f];
                if (field == null)
                    continue;
                if (field.category == ClueCategory.Seal)
                {
                    bool forged = !string.IsNullOrEmpty(field.erased);
                    list.Add(new UvMark(UvMarkKind.Microprint, f, forged ? string.Empty : Microprint(field.issuer), forged));
                }
                else if (Forged(field) && rng != null && rng.Value() < (content?.ghostChance ?? 0f))
                    list.Add(new UvMark(UvMarkKind.Ghost, f, field.erased, true));
            }
            if (content != null && content.CarriesChip(paper.FormNumber))
                chips[d] = paper.Fields.Select(Issued).ToList();
            int id = IndexOf(paper.Fields, ClueCategory.CitizenId);
            if (content != null && content.PrintsMicro(paper.FormNumber) && id >= 0)
                micro[d] = new MicroLine(id, Issued(paper.Fields[id]).value);
            marks[d] = list;
        }

        foreach (HiddenTell tell in tells ?? Array.Empty<HiddenTell>())
        {
            if (tell.Document < 0 || tell.Document >= n)
                continue;
            int f = IndexOf(papers[tell.Document].Fields, tell.Category);
            if (f < 0)
                continue;
            if (tell.Layer == ScanLayer.Uv)
                marks[tell.Document] = marks[tell.Document].Append(new UvMark(UvMarkKind.Ghost, f, tell.Value, true)).ToList();
            else if (tell.Layer == ScanLayer.Chip)
            {
                List<DocumentField> chip = (chips[tell.Document] ?? papers[tell.Document].Fields.Select(Issued)).ToList();
                if (chip[f] != null)
                    chip[f].value = tell.Value;
                chips[tell.Document] = chip;
            }
            else if (tell.Layer == ScanLayer.Micro && micro[tell.Document].HasValue)
                micro[tell.Document] = new MicroLine(micro[tell.Document].Value.Field, tell.Value);
        }
        return new CaseHidden(marks, chips, micro);
    }

    /// <summary>The microprint an office prints in its seal (its id in capitals; "AGENCY" when the seal names none).</summary>
    public static string Microprint(string issuer) => string.IsNullOrWhiteSpace(issuer) ? "AGENCY" : issuer.Trim().ToUpperInvariant();

    /// <summary>True when a forger printed over the issued value of <paramref name="field"/> (a seal, a photo excepted: they have their own marks and proofs).</summary>
    private static bool Forged(DocumentField field) =>
        field.category != ClueCategory.Photo && !string.IsNullOrEmpty(field.erased) && !Values.Match(field.erased, field.value);

    /// <summary>The field as the office issued it: the value a forger printed over, else the print.</summary>
    private static DocumentField Issued(DocumentField f) =>
        f == null ? null : new DocumentField
        {
            category = f.category, label = f.label, value = string.IsNullOrEmpty(f.erased) ? f.value : f.erased, page = f.page, issuer = f.issuer
        };

    /// <summary>The first field of <paramref name="category"/>, or -1.</summary>
    private static int IndexOf(IReadOnlyList<DocumentField> fields, ClueCategory category)
    {
        for (int i = 0; i < (fields?.Count ?? 0); i++)
            if (fields[i] != null && fields[i].category == category)
                return i;
        return -1;
    }
}

/// <summary>
/// The hidden lies (the desk machines spec §2-3): a traveller who is who they
/// say, whose every printed value is true, but one of whose papers carries a
/// cloned chip (another citizen's Citizen ID, shown only by the chip reader)
/// or was washed and printed over (the ghost of its first holder's name,
/// shown only under the UV torch). Each draws only from the published canon
/// (agency.faults; FaultCanon) on the traveller's hidden stream
/// (Seeds.ForHidden), after the papers are printed. The day ramp issues the
/// machine before any day lists the lie (the validator's rule). Pure.
/// </summary>
public static class HiddenLies
{
    /// <summary>The categories a hidden lie's row may name: the identity a chip or a washed paper carries.</summary>
    public static readonly ClueCategory[] Fields = { ClueCategory.CitizenId, ClueCategory.Name };

    /// <summary>
    /// A cloned chip: the box among <paramref name="forms"/> the canon's
    /// ClonedChip rows name that a chipped paper (<paramref name="content"/>)
    /// prints, one Range draw when two or more; its chip's value a fresh
    /// Citizen ID nobody holds today (RecordLies.FreshCitizenId, which joins
    /// <paramref name="takenToday"/>). Empty, with no draw, when no carried
    /// paper can show it.
    /// </summary>
    public static List<HiddenTell> PlanChip(IReadOnlyList<FaultEntry> canon, IReadOnlyList<RecordForm> forms, HiddenContent content,
                                            ISet<string> takenToday, IRandomSource rng)
    {
        List<(int document, ClueCategory category)> boxes = Boxes(canon, forms, LieKind.ClonedChip, f => content != null && content.CarriesChip(f));
        var tells = new List<HiddenTell>();
        if (boxes.Count == 0 || rng == null)
            return tells;
        (int document, ClueCategory category) box = boxes.Count == 1 ? boxes[0] : boxes[rng.Range(0, boxes.Count)];
        string printed = PrintedOf(forms[box.document], box.category);
        string value = box.category == ClueCategory.CitizenId ? RecordLies.FreshCitizenId(takenToday ?? new HashSet<string>(), rng) : null;
        if (string.IsNullOrEmpty(value) || Values.Match(value, printed))
            return tells;
        tells.Add(new HiddenTell(box.document, box.category, ScanLayer.Chip, value));
        return tells;
    }

    /// <summary>
    /// A reprinted paper (the desk machines spec §10): the Citizen ID box
    /// among <paramref name="forms"/> the canon's ReprintedPaper rows name
    /// that a microprinted paper (<paramref name="content"/>) prints, one
    /// Range draw when two or more; its microprint repeats a fresh Citizen ID
    /// nobody holds today (RecordLies.FreshCitizenId, which joins
    /// <paramref name="takenToday"/>). Empty, with no draw, when no carried
    /// paper can show it.
    /// </summary>
    public static List<HiddenTell> PlanReprint(IReadOnlyList<FaultEntry> canon, IReadOnlyList<RecordForm> forms, HiddenContent content,
                                               ISet<string> takenToday, IRandomSource rng)
    {
        List<(int document, ClueCategory category)> boxes = Boxes(canon, forms, LieKind.ReprintedPaper, f => content != null && content.PrintsMicro(f))
            .Where(b => b.Item2 == ClueCategory.CitizenId).ToList();
        var tells = new List<HiddenTell>();
        if (boxes.Count == 0 || rng == null)
            return tells;
        (int document, ClueCategory category) box = boxes.Count == 1 ? boxes[0] : boxes[rng.Range(0, boxes.Count)];
        string printed = PrintedOf(forms[box.document], box.category);
        string value = RecordLies.FreshCitizenId(takenToday ?? new HashSet<string>(), rng);
        if (string.IsNullOrEmpty(value) || Values.Match(value, printed))
            return tells;
        tells.Add(new HiddenTell(box.document, box.category, ScanLayer.Micro, value));
        return tells;
    }

    /// <summary>
    /// A washed paper: the box among <paramref name="forms"/> the canon's
    /// WashedPaper rows name, one Range draw when two or more; its ghost the
    /// first holder's name, one draw among <paramref name="names"/> other
    /// than the one printed. Empty, with no draw, when no carried paper can
    /// show it or no other name is listed.
    /// </summary>
    public static List<HiddenTell> PlanWash(IReadOnlyList<FaultEntry> canon, IReadOnlyList<RecordForm> forms, IReadOnlyList<string> names, IRandomSource rng)
    {
        List<(int document, ClueCategory category)> boxes = Boxes(canon, forms, LieKind.WashedPaper, _ => true);
        var tells = new List<HiddenTell>();
        if (boxes.Count == 0 || rng == null)
            return tells;
        (int document, ClueCategory category) box = boxes.Count == 1 ? boxes[0] : boxes[rng.Range(0, boxes.Count)];
        string printed = PrintedOf(forms[box.document], box.category);
        List<string> others = (names ?? Array.Empty<string>()).Where(n => !string.IsNullOrWhiteSpace(n) && !Values.Match(n, printed)).Distinct().ToList();
        if (box.category != ClueCategory.Name || others.Count == 0)
            return tells;
        tells.Add(new HiddenTell(box.document, box.category, ScanLayer.Uv, others[rng.Range(0, others.Count)]));
        return tells;
    }

    /// <summary>The canon rows of <paramref name="lie"/> as (paper, category) boxes the carried papers print, a paper's form passing <paramref name="paper"/>, in paper then row order.</summary>
    private static List<(int, ClueCategory)> Boxes(IReadOnlyList<FaultEntry> canon, IReadOnlyList<RecordForm> forms, LieKind lie, Func<string, bool> paper)
    {
        var boxes = new List<(int, ClueCategory)>();
        for (int d = 0; d < (forms?.Count ?? 0); d++)
        {
            if (!paper(forms[d].FormNumber))
                continue;
            foreach (FaultEntry row in (canon ?? Array.Empty<FaultEntry>()).Where(FaultCanon.Of(lie)))
                if (row.OnForm(forms[d].FormNumber) && row.TryField(out ClueCategory c) && Array.IndexOf(Fields, c) >= 0
                    && !string.IsNullOrWhiteSpace(PrintedOf(forms[d], c)) && !boxes.Contains((d, c)))
                    boxes.Add((d, c));
        }
        return boxes;
    }

    /// <summary>What paper <paramref name="form"/> prints in its first box of <paramref name="category"/> (null: none).</summary>
    private static string PrintedOf(RecordForm form, ClueCategory category) =>
        form.Fields?.FirstOrDefault(f => f != null && f.category == category)?.value;

    /// <summary>What Generate World and the validator refuse about the hidden lies: a hidden lie's canon row on a field other than a Citizen ID or a name, or a cloned chip's on a form no chip is carried on; a day listing a hidden lie before the day ramp issues its machine (<paramref name="firstDayOf"/>: the day a feature key is first introduced, 0 for never). Empty when sound.</summary>
    public static List<string> Problems(IReadOnlyList<FaultEntry> canon, HiddenContent content, IEnumerable<(int day, IEnumerable<LieKind> lies)> days, Func<string, int> firstDayOf)
    {
        var problems = new List<string>();
        foreach (FaultEntry row in canon ?? Array.Empty<FaultEntry>())
        {
            if (row == null || !row.TryLie(out LieKind lie) || !LieKinds.IsHiddenLie(lie))
                continue;
            if (!row.TryField(out ClueCategory c) || Array.IndexOf(Fields, c) < 0)
                problems.Add($"agency.faults '{row.id}': a {lie} row names a Citizen ID or a name ('{row.field}' cannot be cloned or washed).");
            else if (lie == LieKind.ClonedChip && (content == null || !content.CarriesChip(row.form)))
                problems.Add($"agency.faults '{row.id}': {row.form} carries no chip (agency.hidden.chipForms), so a cloned chip cannot show there.");
            else if (lie == LieKind.ReprintedPaper && (c != ClueCategory.CitizenId || content == null || !content.PrintsMicro(row.form)))
                problems.Add($"agency.faults '{row.id}': a reprinted paper's row names the Citizen ID of a form that prints a microprint line (agency.hidden.microForms).");
        }
        foreach ((int day, IEnumerable<LieKind> lies) in days ?? Array.Empty<(int, IEnumerable<LieKind>)>())
            foreach (LieKind lie in (lies ?? Array.Empty<LieKind>()).Where(LieKinds.IsHiddenLie))
            {
                string machine = LieKinds.MachineOf(lie);
                int issued = firstDayOf != null ? firstDayOf(machine) : 0;
                if (issued <= 0 || issued > day)
                    problems.Add($"Day {day} lists {lie}, but the day ramp issues '{machine}' {(issued <= 0 ? "on no day" : $"only on day {issued}")}: a fault only a machine shows arrives no earlier than the machine.");
            }
        return problems;
    }
}
