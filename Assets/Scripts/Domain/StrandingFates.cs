using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// What becomes of a stranded traveller (the endings and strandings spec §6.2;
/// Saleh's answers Q11-Q13): one fate per stranding, drawn after the roll.
/// Serialized in the content library's fate rows and in the saved stranding
/// log (StrandingRecord.fate): append only (SerializedEnumsTests pins every value).
/// </summary>
public enum StrandingFate
{
    /// <summary>They blend in or vanish: only the agency's failure report in Mail remembers them (Q12).</summary>
    Forgotten,

    /// <summary>They talk: the past remembers a stranger with impossible knowledge (the morning paper; no fact changes).</summary>
    News,

    /// <summary>They bring 2150 technology: the present's Technology carries into the destination (the carries; the paper).</summary>
    Carry,

    /// <summary>They shake the timeline: stability loses the fate row's share of where it stands (the paper; the readout).</summary>
    Tremor,

    /// <summary>The Time Police find and remove them; nothing of 2150 stays behind (the paper, dry and deadpan: Q13).</summary>
    Police
}

/// <summary>One of a fate's paper lines (world_source.json agency.strandingFates[].lines[]): for one era, or any era when blank; {name} and {place}.</summary>
[Serializable]
public sealed class StrandingFateLine
{
    /// <summary>The destination's era (EraSO.id) the line is for; blank: any era.</summary>
    public string era = string.Empty;

    /// <summary>The line ({place} required, {name} optional).</summary>
    public string text = string.Empty;
}

/// <summary>
/// One fate of the fate table (world_source.json agency.strandingFates[];
/// the spec's §6.2, Saleh's Q11 odds): its weight in each column (a valid
/// signed waiver on file, or not), a tremor's stability share, the failure
/// report's status line and the paper's lines.
/// </summary>
[Serializable]
public sealed class StrandingFateRow
{
    /// <summary>Stable id ("police").</summary>
    public string id = string.Empty;

    /// <summary>What the fate does.</summary>
    public StrandingFate fate;

    /// <summary>Its weight when a valid signed waiver was on file (first cut: 40 / 25 / 20 / 10 / 5).</summary>
    public float weightWaivered;

    /// <summary>Its weight when none was (first cut: 20 / 20 / 20 / 15 / 25).</summary>
    public float weightUnwaivered;

    /// <summary>A tremor's loss: the percent of where stability stands (StabilityRules.ApplyPercent); 0 for every other fate.</summary>
    public float stability;

    /// <summary>The failure report's status line ("Status: not recovered.").</summary>
    public string status = string.Empty;

    /// <summary>The morning paper's lines (none for Forgotten; none elsewhere: the paper prints news.stranded).</summary>
    public List<StrandingFateLine> lines = new List<StrandingFateLine>();
}

/// <summary>
/// The agency's failure report as authored (world_source.json
/// agency.strandingReport; the spec's §6.3): the Mail message every stranding
/// sends the next morning, one line per template, in this order.
/// </summary>
[Serializable]
public sealed class StrandingReportContent
{
    /// <summary>The unit's line ({unit}, {place}): "Unit HP-40718 failed in {place}."</summary>
    public string unit = string.Empty;

    /// <summary>The traveller's line ({name}, {id}).</summary>
    public string traveller = string.Empty;

    /// <summary>A valid signed waiver on file ({waiver}, {debt}): the debt passes to kin.</summary>
    public string waivered = string.Empty;

    /// <summary>No valid signed waiver on file: liability referred to the desk.</summary>
    public string unwaivered = string.Empty;

    /// <summary>The stranding fine charged to the desk ({fine}); printed only when one was charged.</summary>
    public string fine = string.Empty;
}

/// <summary>
/// The strandings' fates (the endings and strandings spec §6; Saleh's answers
/// Q10-Q13, §15.2): each stranded traveller draws one fate from the column of
/// their waiver (a valid signed one on file, or not), a personality's tilt
/// multiplying one fate's weight; the paper's line for the fate (by the
/// destination's era, else any era; Forgotten prints none; a fate with no line
/// prints news.stranded); the failure report's lines; the fate table's and the
/// report's content rules, which Generate World and the validator share. Two
/// values of the day's fate stream per stranded traveller, whatever the table
/// holds (Seeds.ForStrandingFates), so tuning never shifts a later draw. Pure.
/// </summary>
public static class StrandingFates
{
    /// <summary>The report's unit token ("{unit}").</summary>
    public const string UnitToken = "unit";

    /// <summary>The report's Citizen ID token ("{id}").</summary>
    public const string IdToken = "id";

    /// <summary>The report's waiver number token ("{waiver}").</summary>
    public const string WaiverToken = "waiver";

    /// <summary>The report's debt token ("{debt}").</summary>
    public const string DebtToken = "debt";

    /// <summary>The report's fine token ("{fine}").</summary>
    public const string FineToken = "fine";

    /// <summary>
    /// The fate of one stranded traveller: <paramref name="u"/> (a value of the
    /// fate stream, 0 to 1) picks a row by its weight in the column of
    /// <paramref name="waivered"/>, the <paramref name="tilt"/> fate's weight
    /// multiplied by <paramref name="tiltFactor"/> (a personality's tilt; the
    /// spec's §6.2); a weight of 0 is never picked. Null for no rows or no
    /// weight above 0.
    /// </summary>
    public static StrandingFateRow Pick(IReadOnlyList<StrandingFateRow> rows, bool waivered, StrandingFate? tilt, float tiltFactor, float u)
    {
        float Weight(StrandingFateRow r)
        {
            if (r == null)
                return 0f;
            float w = waivered ? r.weightWaivered : r.weightUnwaivered;
            return tilt.HasValue && r.fate == tilt.Value ? w * Math.Max(0f, tiltFactor) : w;
        }
        return WeightedRandom.Pick(rows, Weight, new FixedRandom(u));
    }

    /// <summary>The first row of <paramref name="fate"/>, or null.</summary>
    public static StrandingFateRow Of(IReadOnlyList<StrandingFateRow> rows, StrandingFate fate) =>
        (rows ?? Array.Empty<StrandingFateRow>()).FirstOrDefault(r => r != null && r.fate == fate);

    /// <summary>
    /// The morning paper's line for a stranding of <paramref name="row"/>'s
    /// fate: none for Forgotten (Q12: the report is the only trace); else one of
    /// the row's lines for the destination's era <paramref name="eraId"/> (else
    /// its lines for any era), picked by <paramref name="u"/> (0 to 1), or
    /// <paramref name="fallback"/> (news.stranded) when it has none; {name} and
    /// {place} filled. Empty for a null row or nothing to print.
    /// </summary>
    public static string Line(StrandingFateRow row, string eraId, string fallback, string name, string place, float u)
    {
        if (row == null || row.fate == StrandingFate.Forgotten)
            return string.Empty;
        List<StrandingFateLine> all = (row.lines ?? new List<StrandingFateLine>()).Where(l => l != null && !string.IsNullOrWhiteSpace(l.text)).ToList();
        List<StrandingFateLine> pool = all.Where(l => !string.IsNullOrEmpty(eraId) && l.era == eraId).ToList();
        if (pool.Count == 0)
            pool = all.Where(l => string.IsNullOrWhiteSpace(l.era)).ToList();
        string template = pool.Count > 0 ? pool[Math.Min(pool.Count - 1, (int)(Math.Max(0f, u) * pool.Count))].text : fallback;
        if (string.IsNullOrWhiteSpace(template))
            return string.Empty;
        return Interview.Fill(Interview.Fill(template, Interview.NameToken, name), Interview.PlaceToken, place);
    }

    /// <summary>
    /// The failure report's body for <paramref name="record"/> (the spec's
    /// §6.3): the unit's line, the traveller's, the waiver's (on file: the debt
    /// passes to kin; else liability referred to the desk), the fine's when one
    /// was charged, then the fate's <paramref name="status"/>; blank templates
    /// are skipped. {debt} and {fine} read <paramref name="credits"/>.
    /// </summary>
    public static List<string> Report(StrandingRecord record, StrandingReportContent content, string status, Func<int, string> credits)
    {
        var lines = new List<string>();
        if (record == null || content == null)
            return lines;
        string Cr(int amount) => credits != null ? credits(amount) : amount.ToString(System.Globalization.CultureInfo.InvariantCulture);
        void Add(string template, params (string token, string value)[] fills)
        {
            if (string.IsNullOrWhiteSpace(template))
                return;
            string text = template;
            foreach ((string token, string value) in fills)
                text = Interview.Fill(text, token, value);
            lines.Add(text);
        }

        Add(content.unit, (UnitToken, record.transponder), (Interview.PlaceToken, record.placeLabel));
        Add(content.traveller, (Interview.NameToken, record.travellerName), (IdToken, record.citizenId));
        if (record.waivered)
            Add(content.waivered, (WaiverToken, record.waiverNo), (DebtToken, Cr(record.debt)));
        else
            Add(content.unwaivered);
        if (record.fine > 0)
            Add(content.fine, (FineToken, Cr(record.fine)));
        Add(status);
        return lines;
    }

    /// <summary>
    /// Every problem of the fate table (the rule Generate World and the
    /// validator share): no rows; a blank or repeated id; a fate listed twice
    /// or missing; a negative weight; a column with no weight above 0; a
    /// tremor's share outside 0 to 100 (above 0), another fate's not 0; a
    /// blank status; a Forgotten line (it prints none); a line with a blank
    /// text, an era <paramref name="eras"/> does not list, no {place}, or a
    /// token other than {name} and {place}. Empty when sound.
    /// </summary>
    public static List<string> Problems(IReadOnlyList<StrandingFateRow> rows, IReadOnlyCollection<string> eras)
    {
        var problems = new List<string>();
        if (rows == null || rows.Count == 0)
        {
            problems.Add("agency.strandingFates is empty: every stranded traveller draws one fate from it.");
            return problems;
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        var fates = new HashSet<StrandingFate>();
        float waivered = 0f, unwaivered = 0f;
        foreach (StrandingFateRow r in rows)
        {
            if (r == null)
            {
                problems.Add("agency.strandingFates: a row is empty.");
                continue;
            }
            string at = $"agency.strandingFates '{r.id}'";
            if (string.IsNullOrWhiteSpace(r.id) || !ids.Add(r.id))
                problems.Add($"{at}: the id is blank or listed twice.");
            if (!fates.Add(r.fate))
                problems.Add($"{at}: the fate {r.fate} is listed twice.");
            if (r.weightWaivered < 0f || r.weightUnwaivered < 0f || float.IsNaN(r.weightWaivered) || float.IsNaN(r.weightUnwaivered))
                problems.Add($"{at}: a weight is negative.");
            waivered += Math.Max(0f, r.weightWaivered);
            unwaivered += Math.Max(0f, r.weightUnwaivered);
            if (r.fate == StrandingFate.Tremor ? !(r.stability > 0f && r.stability <= 100f) : r.stability != 0f)
                problems.Add(r.fate == StrandingFate.Tremor
                    ? $"{at}: a tremor's stability is {r.stability}; it takes a percent of where stability stands, above 0 and at most 100."
                    : $"{at}: only a tremor moves stability; its stability must be 0 (it is {r.stability}).");
            if (string.IsNullOrWhiteSpace(r.status))
                problems.Add($"{at}: the status is blank (the failure report's last line).");
            List<StrandingFateLine> lines = r.lines ?? new List<StrandingFateLine>();
            if (r.fate == StrandingFate.Forgotten && lines.Count > 0)
                problems.Add($"{at}: a forgotten traveller makes no paper; its lines are never printed.");
            for (int i = 0; i < lines.Count; i++)
            {
                StrandingFateLine l = lines[i];
                string line = $"{at} line {i + 1}";
                if (l == null || string.IsNullOrWhiteSpace(l.text))
                {
                    problems.Add($"{line} is blank.");
                    continue;
                }
                if (!string.IsNullOrWhiteSpace(l.era) && (eras == null || !eras.Contains(l.era)))
                    problems.Add($"{line} names the era '{l.era}', which eras does not list.");
                if (!Interview.HoldsToken(l.text, Interview.PlaceToken))
                    problems.Add($"{line} must hold {{place}}: where the traveller is lost.");
                foreach (string token in Tokens(l.text))
                    if (token != Interview.PlaceToken && token != Interview.NameToken)
                        problems.Add($"{line} holds {{{token}}}; a paper line fills only {{name}} and {{place}}.");
            }
        }

        foreach (StrandingFate fate in (StrandingFate[])Enum.GetValues(typeof(StrandingFate)))
            if (!fates.Contains(fate))
                problems.Add($"agency.strandingFates has no {fate} row; every fate is a row (a weight of 0 benches it).");
        if (waivered <= 0f)
            problems.Add("agency.strandingFates: no fate has a waivered weight above 0.");
        if (unwaivered <= 0f)
            problems.Add("agency.strandingFates: no fate has an unwaivered weight above 0.");
        return problems;
    }

    /// <summary>Every problem of the failure report: a blank line, or a line missing its tokens (unit: {unit} and {place}; traveller: {name} and {id}; waivered: {waiver} and {debt}; fine: {fine}). Empty when sound.</summary>
    public static List<string> ReportProblems(StrandingReportContent content)
    {
        var problems = new List<string>();
        if (content == null)
        {
            problems.Add("agency.strandingReport is missing: the failure report Mail sends for every stranding.");
            return problems;
        }
        void Need(string name, string text, params string[] tokens)
        {
            if (string.IsNullOrWhiteSpace(text))
                problems.Add($"agency.strandingReport.{name} is blank.");
            else
                foreach (string token in tokens)
                    if (!Interview.HoldsToken(text, token))
                        problems.Add($"agency.strandingReport.{name} must hold {{{token}}}.");
        }
        Need("unit", content.unit, UnitToken, Interview.PlaceToken);
        Need("traveller", content.traveller, Interview.NameToken, IdToken);
        Need("waivered", content.waivered, WaiverToken, DebtToken);
        Need("unwaivered", content.unwaivered);
        Need("fine", content.fine, FineToken);
        return problems;
    }

    /// <summary>The tokens a template names, in order.</summary>
    private static IEnumerable<string> Tokens(string text)
    {
        for (int start = text.IndexOf('{'); start >= 0; start = text.IndexOf('{', start + 1))
        {
            int end = text.IndexOf('}', start + 1);
            if (end < 0)
                yield break;
            yield return text.Substring(start + 1, end - start - 1);
        }
    }
}
