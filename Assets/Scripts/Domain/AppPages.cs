using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// The Record Extract (TC-901, the PC redesign FO9, §2.5) as the Records tab
/// fills it: the way back from a placed row to the record's row (the slot's
/// Row counts the rows across every group, as FormLayout numbers a
/// RecordGroups block's boxes), a link's row and which rows pick. Pure.
/// </summary>
public static class RecordExtractPage
{
    /// <summary>The RecordGroups block's slot on Form_RecordExtract.</summary>
    public const string GroupsSlot = "groups";

    /// <summary>The query line's slot (the lookup, whether a record is on file, today's date).</summary>
    public const string QuerySlot = "query";

    /// <summary>The record's row at <paramref name="flat"/> (counted across the groups, a placed slot's Row); false past the end.</summary>
    public static bool TryRow(CitizenRecord record, int flat, out RecordRow row)
    {
        row = default;
        if (record == null || flat < 0)
            return false;
        int at = 0;
        foreach (RecordGroup group in record.Groups)
            foreach (RecordRow r in group.Rows)
            {
                if (at++ == flat)
                {
                    row = r;
                    return true;
                }
            }
        return false;
    }

    /// <summary>The flat index of the record's first evidence row of <paramref name="category"/> (a link's row), or -1.</summary>
    public static int RowOf(CitizenRecord record, ClueCategory category)
    {
        if (record == null)
            return -1;
        int at = 0;
        foreach (RecordGroup group in record.Groups)
            foreach (RecordRow r in group.Rows)
            {
                if (r.IsEvidence && r.Category == category)
                    return at;
                at++;
            }
        return -1;
    }

    /// <summary>True for a row the player can pick: an evidence row with a value.</summary>
    public static bool IsPickable(RecordRow row) => row.IsEvidence && !string.IsNullOrEmpty(row.Value);
}

/// <summary>
/// A reference book's Register (TC-911 to TC-916, the PC redesign FO9, §2.6)
/// as the Reference tab fills it: the arranged lines (ReferenceRows) as the
/// form's table rows, an era heading as a one-cell row across the table, a
/// fact row as PLACE, ERA, VALUE and NOTE (the claimed row and a row history
/// revised say so in NOTE); a row keeps its line's index, so a placed slot's
/// Row is its line. Each book's number counts on from the asset's. Pure.
/// </summary>
public static class RegisterPage
{
    /// <summary>The table's slot on Form_Register.</summary>
    public const string RowsSlot = "rows";

    /// <summary>The edition line's slot.</summary>
    public const string EditionSlot = "edition";

    /// <summary>The form number of book <paramref name="index"/> of the library: the asset's number with its trailing digits raised by the index ("TC-911", 2: "TC-913"); the asset's own when it ends in no digit.</summary>
    public static string FormNumber(string first, int index)
    {
        first ??= string.Empty;
        int digits = first.Length;
        while (digits > 0 && char.IsDigit(first[digits - 1]))
            digits--;
        if (digits == first.Length || index <= 0)
            return first;
        int number = int.Parse(first.Substring(digits), NumberStyles.None, CultureInfo.InvariantCulture) + index;
        return first.Substring(0, digits) + number.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// The register's rows from its <paramref name="lines"/>: a heading line
    /// as its era's name alone; a row line as the place (its label without
    /// the era), the era's name (<paramref name="eraName"/> of the id), the
    /// value and the note (<paramref name="claimedNote"/> for the claimed
    /// row, <paramref name="revisedNote"/> for a row <paramref name="revised"/>
    /// says history changed, both joined by " · ", else blank).
    /// </summary>
    public static List<string[]> Rows(IReadOnlyList<ReferenceLine> lines, Func<string, string> eraName, Func<FactRow, bool> revised, string claimedNote, string revisedNote)
    {
        var rows = new List<string[]>();
        if (lines == null)
            return rows;
        foreach (ReferenceLine line in lines)
        {
            if (line.IsHeading)
            {
                rows.Add(new[] { Era(eraName, line.EraHeading) });
                continue;
            }
            FactRow fact = line.Row;
            string era = Era(eraName, fact.EraId);
            var notes = new List<string>(2);
            if (line.Claimed)
                notes.Add(claimedNote ?? string.Empty);
            if (revised != null && revised(fact))
                notes.Add(revisedNote ?? string.Empty);
            rows.Add(new[] { OriginLabels.Place(fact.OriginLabel, era), era, fact.Value ?? string.Empty, string.Join(" · ", notes) });
        }
        return rows;
    }

    /// <summary>The line (and so the row) the book row key <paramref name="rowKey"/> names (PickKeys.BookRow), or -1.</summary>
    public static int LineOf(IReadOnlyList<ReferenceLine> lines, string rowKey)
    {
        if (lines == null || string.IsNullOrEmpty(rowKey))
            return -1;
        for (int i = 0; i < lines.Count; i++)
            if (!lines[i].IsHeading && PickKeys.BookRow(lines[i].Row.Category, lines[i].Row.NationId, lines[i].Row.EraId) == rowKey)
                return i;
        return -1;
    }

    /// <summary>An era's name by its id, else the id itself (or blank for none).</summary>
    private static string Era(Func<string, string> eraName, string eraId) =>
        eraId == null ? string.Empty : (eraName != null ? eraName(eraId) : null) ?? eraId;
}

/// <summary>
/// The Interview Record (TC-920, the PC redesign FO9, §2.7) as the Transcript
/// tab fills it: the transcript's lines as the form's table rows, NO. (an
/// answer marked), SPEAKER and STATEMENT (the line as shown: English, or a
/// displaced traveller's glyphs), the desk's lines left out when only the
/// answers show; each row remembers its line. Pure.
/// </summary>
public static class InterviewPage
{
    /// <summary>The table's slot on Form_InterviewRecord.</summary>
    public const string RowsSlot = "rows";

    /// <summary>The head line's slot (the traveller, the desk officer, the day).</summary>
    public const string HeadSlot = "head";

    /// <summary>
    /// The rows of <paramref name="lines"/> (every line, or the answers alone
    /// with <paramref name="answersOnly"/>): the line's number, marked with
    /// <paramref name="answerMark"/> when it is an answer; its speaker
    /// (<paramref name="deskName"/> or <paramref name="travellerName"/>); and
    /// its statement as <paramref name="shown"/> writes it ("►4": the mark
    /// and the number share the narrow NO. column). Each row's line
    /// index goes into <paramref name="lineOfRow"/>.
    /// </summary>
    public static List<string[]> Rows(IReadOnlyList<DialogLine> lines, bool answersOnly, string deskName, string travellerName, Func<DialogLine, string> shown,
                                      string answerMark, List<int> lineOfRow)
    {
        var rows = new List<string[]>();
        lineOfRow?.Clear();
        if (lines == null)
            return rows;
        for (int i = 0; i < lines.Count; i++)
        {
            DialogLine line = lines[i];
            if (line == null || (answersOnly && !line.IsAnswer))
                continue;
            string number = (i + 1).ToString(CultureInfo.InvariantCulture);
            rows.Add(new[]
            {
                line.IsAnswer && !string.IsNullOrEmpty(answerMark) ? answerMark + number : number,
                line.Speaker == DialogSpeaker.Desk ? deskName ?? string.Empty : travellerName ?? string.Empty,
                shown != null ? shown(line) ?? string.Empty : line.Text ?? string.Empty
            });
            lineOfRow?.Add(i);
        }
        return rows;
    }

    /// <summary>The row of line <paramref name="lineIndex"/> in <paramref name="lineOfRow"/>, or -1 (the line is hidden or not there).</summary>
    public static int RowOf(IReadOnlyList<int> lineOfRow, int lineIndex)
    {
        for (int i = 0; lineOfRow != null && i < lineOfRow.Count; i++)
            if (lineOfRow[i] == lineIndex)
                return i;
        return -1;
    }
}

/// <summary>
/// One line of the Deviation Report (the PC redesign §2.8): the documented
/// contradiction and the two picks that proved it, the statement's (a
/// paper's field, an answer, a garment) and the truth's (a book row, a
/// record row, or the other paper of a cross proof), so the report's row
/// can link back to both.
/// </summary>
public sealed class ReportEntry
{
    /// <summary>An entry from its deviation and its two sides.</summary>
    public ReportEntry(Discrepancy deviation, ComparePick statement, ComparePick truth)
    {
        Deviation = deviation;
        Statement = statement;
        Truth = truth;
    }

    /// <summary>The documented contradiction.</summary>
    public Discrepancy Deviation { get; }

    /// <summary>The statement's pick (its key links to where it was picked).</summary>
    public ComparePick Statement { get; }

    /// <summary>The truth's pick (the other paper in a cross proof).</summary>
    public ComparePick Truth { get; }

    /// <summary>
    /// The entry of <paramref name="deviation"/> proved by the pair
    /// <paramref name="a"/> and <paramref name="b"/> (in the order picked): the
    /// statement is the side whose evidence is of the deviation's stated kind
    /// (a cross proof's two papers: the first picked).
    /// </summary>
    public static ReportEntry From(Discrepancy deviation, ComparePick a, ComparePick b)
    {
        bool aIsStatement = deviation == null || a.Evidence.kind == deviation.source || b.Evidence.kind != deviation.source;
        return aIsStatement ? new ReportEntry(deviation, a, b) : new ReportEntry(deviation, b, a);
    }
}

/// <summary>
/// The Deviation Report (TC-930, the PC redesign FO9, §2.8) as the Report tab
/// fills it: a table row per documented deviation (NO., CATEGORY, STATEMENT,
/// CONTRADICTED BY, PROOF), each of its two sides a cell that links back to
/// where it was picked. Pure.
/// </summary>
public static class ReportPage
{
    /// <summary>The table's slot on Form_DeviationReport.</summary>
    public const string RowsSlot = "rows";

    /// <summary>The case line's slot (the traveller and the claim).</summary>
    public const string CaseLineSlot = "caseLine";

    /// <summary>The tail paragraph's slot (nothing logged, or the count).</summary>
    public const string TailSlot = "tail";

    /// <summary>The desk officer's sign-off slot.</summary>
    public const string SignatureSlot = "signature";

    /// <summary>The table's STATEMENT column (its cell links to the statement's pick).</summary>
    public const int StatementCell = 2;

    /// <summary>The table's CONTRADICTED BY column (its cell links to the truth's pick).</summary>
    public const int TruthCell = 3;

    /// <summary>The rows: each entry numbered from 1, its category (<paramref name="category"/> words it), "label: value" for each side (the pick's label and the text the dock showed), and the proof's word (<paramref name="proof"/>).</summary>
    public static List<string[]> Rows(IReadOnlyList<ReportEntry> entries, Func<ClueCategory, string> category, Func<DiscrepancyProof, string> proof)
    {
        var rows = new List<string[]>();
        if (entries == null)
            return rows;
        for (int i = 0; i < entries.Count; i++)
        {
            ReportEntry e = entries[i];
            Discrepancy d = e.Deviation;
            rows.Add(new[]
            {
                (i + 1).ToString(CultureInfo.InvariantCulture),
                category != null && d != null ? category(d.category) ?? string.Empty : string.Empty,
                Side(e.Statement.Label, e.Statement.Shown),
                Side(e.Truth.Label, e.Truth.Shown),
                proof != null && d != null ? proof(d.provedBy) ?? string.Empty : string.Empty
            });
        }
        return rows;
    }

    /// <summary>The pick key a row's cell links to: the statement's in the STATEMENT column, the truth's in CONTRADICTED BY; null elsewhere.</summary>
    public static string LinkKey(ReportEntry entry, int cell)
    {
        if (entry == null)
            return null;
        if (cell == StatementCell)
            return entry.Statement.Key;
        return cell == TruthCell ? entry.Truth.Key : null;
    }

    /// <summary>"Label: value" (the label alone without a value, the value alone without a label).</summary>
    private static string Side(string label, string value)
    {
        bool hasLabel = !string.IsNullOrEmpty(label), hasValue = !string.IsNullOrEmpty(value);
        return hasLabel && hasValue ? label + ": " + value : hasLabel ? label : value ?? string.Empty;
    }
}

/// <summary>
/// The Directive Memo (TC-940, the PC redesign FO9, §2.9) as the Rules tab
/// fills it: the day's directives as numbered table rows (NO., DIRECTIVE). Pure.
/// </summary>
public static class DirectiveMemoPage
{
    /// <summary>The table's slot on Form_DirectiveMemo.</summary>
    public const string RowsSlot = "rows";

    /// <summary>The TO, DATE, FROM and REF boxes' slots.</summary>
    public const string ToSlot = "to", DateSlot = "date", FromSlot = "from", RefSlot = "ref";

    /// <summary>The line shown when there is no directive (its slot).</summary>
    public const string NoneSlot = "none";

    /// <summary>The rows: each directive numbered from 1 (a null or blank one skipped).</summary>
    public static List<string[]> Rows(IReadOnlyList<string> directives)
    {
        var rows = new List<string[]>();
        if (directives == null)
            return rows;
        foreach (string directive in directives)
        {
            if (string.IsNullOrWhiteSpace(directive))
                continue;
            rows.Add(new[] { (rows.Count + 1).ToString(CultureInfo.InvariantCulture), directive.Trim() });
        }
        return rows;
    }
}
