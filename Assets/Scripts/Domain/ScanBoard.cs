using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>A scanned paper as the case board reads it: its index in the case, its name and its fields (form order).</summary>
public sealed class BoardPaper
{
    /// <summary>A scanned paper.</summary>
    public BoardPaper(int document, string name, IReadOnlyList<DocumentField> fields)
    {
        Document = document;
        Name = name ?? string.Empty;
        Fields = fields ?? Array.Empty<DocumentField>();
    }

    /// <summary>The paper's index in the case (PickKeys.Field's document).</summary>
    public int Document { get; }

    /// <summary>The paper's name ("Travel Passport").</summary>
    public string Name { get; }

    /// <summary>Its fields, in form order (a field's index is PickKeys.Field's field).</summary>
    public IReadOnlyList<DocumentField> Fields { get; }
}

/// <summary>A paper the traveller's kind needs today (the rules check's "required papers"): its request id (FormRequests.IdOf), its name, and whether it was handed over.</summary>
public readonly struct RequiredPaper
{
    /// <summary>A required paper.</summary>
    public RequiredPaper(string id, string label, bool handedOver)
    {
        Id = id ?? string.Empty;
        Label = label ?? string.Empty;
        HandedOver = handedOver;
    }

    /// <summary>Its request id (the Papers menu's flag).</summary>
    public string Id { get; }

    /// <summary>Its name as the desk asks for it ("Entry Ticket", "Proof of means").</summary>
    public string Label { get; }

    /// <summary>True once it was handed over.</summary>
    public bool HandedOver { get; }
}

/// <summary>What a rules-check row says (the scanner app spec §2.2): its chip.</summary>
public enum RuleChip
{
    /// <summary>The value meets the rule: VALID.</summary>
    Valid,

    /// <summary>A Valid Until that has passed: EXPIRED.</summary>
    Expired,

    /// <summary>A destination today's rules close: CLOSED.</summary>
    Closed,

    /// <summary>A required paper not handed over: MISSING.</summary>
    Missing,

    /// <summary>A departure dated another day than today: WRONG DATE (the orchestrator's addition: a departure is not "expired" when it is in the future).</summary>
    WrongDate
}

/// <summary>Which of the rules check's three questions a row answers.</summary>
public enum RuleCheckKind
{
    /// <summary>The destination against today's closures.</summary>
    Destination,

    /// <summary>A paper's date against today.</summary>
    Date,

    /// <summary>A required paper handed over.</summary>
    Paper
}

/// <summary>One row of the rules check: the rule it reads (its index in today's directives, -1 for none), the value (its paper and field, -1 when no scanned box holds it), the chip.</summary>
public sealed class RuleCheckRow
{
    /// <summary>Which question it answers.</summary>
    public RuleCheckKind Kind;

    /// <summary>The rule's index in today's directives (EntryKeys.Rule's), -1 for none (a required paper with no paper-set rule).</summary>
    public int Rule = -1;

    /// <summary>The value's paper (BoardPaper.Document), -1 when no scanned paper holds it.</summary>
    public int Document = -1;

    /// <summary>The value's field index on its paper, -1 for none.</summary>
    public int Field = -1;

    /// <summary>The value's paper's name (blank for none).</summary>
    public string Paper = string.Empty;

    /// <summary>The detail it reads (Destination, Expiry, DepartureDate; a paper row: none, Name).</summary>
    public ClueCategory Category;

    /// <summary>The value shown (the destination, the date, the paper's name).</summary>
    public string Value = string.Empty;

    /// <summary>A required paper's request id (blank for the other rows).</summary>
    public string RequestId = string.Empty;

    /// <summary>The verdict.</summary>
    public RuleChip Chip;

    /// <summary>True when the row fails (any chip but VALID): a click logs it (a value to hold the rule against) or flags the paper missing.</summary>
    public bool Failing => Chip != RuleChip.Valid;

    /// <summary>True when a click can log it as evidence: a failing rule held against a scanned value (MatchBoard.PickRule, then the value).</summary>
    public bool Loggable => Failing && Rule >= 0 && Document >= 0 && Field >= 0;
}

/// <summary>
/// The case board's rules check (the scanner app spec §2.2; the
/// orchestrator's Decision: dates, open destinations and required papers
/// only; lies, costume errors and forgeries stay the player's job), pure:
/// one row for the destination against today's closures (VALID or CLOSED,
/// RuleChecks' own reading), one per scanned date against today while the
/// papers' dates rule stands (VALID, EXPIRED or WRONG DATE, Directives.PaperDates
/// over that date alone) and one per paper the kind needs today (VALID once
/// handed over, else MISSING). Rules not read for the traveller's kind say
/// nothing.
/// </summary>
public static class RulesCheck
{
    /// <summary>
    /// The rows for a traveller of <paramref name="kind"/> claiming
    /// <paramref name="nationId"/> in <paramref name="eraId"/> (shown as
    /// <paramref name="claim"/>), under today's <paramref name="rules"/>, with
    /// the <paramref name="scanned"/> papers, the <paramref name="required"/>
    /// papers and <paramref name="today"/> (null: no date is judged).
    /// </summary>
    public static List<RuleCheckRow> Rows(IReadOnlyList<Directive> rules, TravellerKind kind, string nationId, string eraId, string claim,
                                          IReadOnlyList<BoardPaper> scanned, IReadOnlyList<RequiredPaper> required, DateTime? today)
    {
        var rows = new List<RuleCheckRow>();
        rules = rules ?? Array.Empty<Directive>();
        scanned = scanned ?? Array.Empty<BoardPaper>();

        // The destination: the first closure that closes the claim, else the first closure read for the kind.
        int closing = -1, firstClosure = -1;
        for (int i = 0; i < rules.Count; i++)
        {
            if (!Directives.IsClosure(rules[i].Type) || !rules[i].AppliesTo(kind))
                continue;
            if (firstClosure < 0)
                firstClosure = i;
            if (closing < 0 && rules[i].Closes(nationId, eraId))
                closing = i;
        }
        if (firstClosure >= 0)
        {
            var row = new RuleCheckRow
            {
                Kind = RuleCheckKind.Destination,
                Rule = closing >= 0 ? closing : firstClosure,
                Category = ClueCategory.Destination,
                Value = claim ?? string.Empty,
                Chip = closing >= 0 ? RuleChip.Closed : RuleChip.Valid
            };
            Place(row, scanned, ClueCategory.Destination);
            rows.Add(row);
        }

        // The papers' dates: every scanned departure and Valid Until the calendar can read, each alone.
        int dates = FirstOf(rules, TravelRuleType.PaperDates, kind);
        if (dates >= 0 && today.HasValue)
            foreach (BoardPaper paper in scanned)
                for (int f = 0; f < paper.Fields.Count; f++)
                {
                    DocumentField field = paper.Fields[f];
                    if (field == null || (field.category != ClueCategory.Expiry && field.category != ClueCategory.DepartureDate) || !AgencyCalendar.TryRead(field.value, out _))
                        continue;
                    bool departure = field.category == ClueCategory.DepartureDate;
                    DirectiveFault fault = departure ? Directives.PaperDates(new[] { field.value }, null, today.Value) : Directives.PaperDates(null, new[] { field.value }, today.Value);
                    rows.Add(new RuleCheckRow
                    {
                        Kind = RuleCheckKind.Date,
                        Rule = dates,
                        Document = paper.Document,
                        Field = f,
                        Paper = paper.Name,
                        Category = field.category,
                        Value = field.value,
                        Chip = fault == DirectiveFault.None ? RuleChip.Valid : departure ? RuleChip.WrongDate : RuleChip.Expired
                    });
                }

        // The papers the kind needs today.
        int paperSet = FirstOf(rules, TravelRuleType.PaperSet, kind);
        foreach (RequiredPaper paper in required ?? Array.Empty<RequiredPaper>())
            rows.Add(new RuleCheckRow
            {
                Kind = RuleCheckKind.Paper,
                Rule = paperSet,
                Category = ClueCategory.Name,
                Value = paper.Label,
                RequestId = paper.Id,
                Chip = paper.HandedOver ? RuleChip.Valid : RuleChip.Missing
            });
        return rows;
    }

    /// <summary>The first rule of <paramref name="type"/> read for <paramref name="kind"/>, or -1.</summary>
    private static int FirstOf(IReadOnlyList<Directive> rules, TravelRuleType type, TravellerKind kind)
    {
        for (int i = 0; i < rules.Count; i++)
            if (rules[i].Type == type && rules[i].AppliesTo(kind))
                return i;
        return -1;
    }

    /// <summary>The row's value from the first scanned box of <paramref name="category"/> (paper order, then field order), when one holds it.</summary>
    private static void Place(RuleCheckRow row, IReadOnlyList<BoardPaper> scanned, ClueCategory category)
    {
        foreach (BoardPaper paper in scanned)
            for (int f = 0; f < paper.Fields.Count; f++)
                if (paper.Fields[f] != null && paper.Fields[f].category == category && !string.IsNullOrWhiteSpace(paper.Fields[f].value))
                {
                    row.Document = paper.Document;
                    row.Field = f;
                    row.Paper = paper.Name;
                    row.Value = paper.Fields[f].value;
                    return;
                }
    }
}

/// <summary>One value of a cross-check source: its category, where it is (a paper's field index, or a record's row index) and what it reads.</summary>
public readonly struct CrossValue
{
    /// <summary>A value.</summary>
    public CrossValue(ClueCategory category, int index, string label, string value)
    {
        Category = category;
        Index = index;
        Label = label ?? string.Empty;
        Value = value ?? string.Empty;
    }

    /// <summary>The detail it states.</summary>
    public ClueCategory Category { get; }

    /// <summary>Where it is in its source (a paper's field index; a record's flat row index).</summary>
    public int Index { get; }

    /// <summary>The detail's word on its source ("Date of Birth", "Born").</summary>
    public string Label { get; }

    /// <summary>The value as it reads.</summary>
    public string Value { get; }
}

/// <summary>One column of the cross-check table: a scanned paper (its document index) or the record.</summary>
public sealed class CrossColumn
{
    /// <summary>A column titled <paramref name="title"/>.</summary>
    public CrossColumn(string title, bool isRecord, int document, IEnumerable<CrossValue> values)
    {
        Title = title ?? string.Empty;
        IsRecord = isRecord;
        Document = document;
        Values = values != null ? values.ToList() : new List<CrossValue>();
    }

    /// <summary>The paper's name, or the record's.</summary>
    public string Title { get; }

    /// <summary>True for the citizen record (the truth a paper is held against).</summary>
    public bool IsRecord { get; }

    /// <summary>A paper's index in the case (-1 for the record).</summary>
    public int Document { get; }

    /// <summary>Its values (a category's first value counts).</summary>
    public IReadOnlyList<CrossValue> Values { get; }
}

/// <summary>One cell of the cross-check table: the value (null: the source states nothing of it) and whether it glows (it disagrees), with the column it is logged against.</summary>
public sealed class CrossCell
{
    /// <summary>The value, or null when the source states nothing of the row's detail.</summary>
    public CrossValue? Value;

    /// <summary>True when the cell disagrees: a click logs it against <see cref="Partner"/>.</summary>
    public bool Glows;

    /// <summary>The column a glowing cell is logged against (the record's when it states the detail, else the first column that disagrees), -1 for none.</summary>
    public int Partner = -1;
}

/// <summary>One row of the cross-check table: a shared detail and a cell per column.</summary>
public sealed class CrossRow
{
    /// <summary>The detail.</summary>
    public ClueCategory Category;

    /// <summary>A cell per column, in column order.</summary>
    public List<CrossCell> Cells = new List<CrossCell>();

    /// <summary>True when any cell glows.</summary>
    public bool Mismatch => Cells.Any(c => c.Glows);
}

/// <summary>
/// The case board's cross-check table (the scanner app spec §2.3), pure: a
/// row per detail two or more sources state (scanned papers and the record,
/// papers first; a detail a paper's checks never compare is left out: the
/// directive-only dates, read against today, and the seal and photo, held
/// against their truths), a column per source. A row disagrees when two of
/// its values are not the same value (Values.Match, the compare's own rule);
/// then, with the record stating the detail, every paper cell that differs
/// from the record glows (partnered with the record), and the record's cell
/// too (partnered with the first paper that differs); without the record,
/// every cell that differs from another glows, partnered with the first one
/// that differs from it. An honest set never glows. A cell's click logs the
/// pair through the one compare (FindingRules, DiscrepancyLog).
/// </summary>
public static class CrossCheck
{
    /// <summary>True when the table reads <paramref name="category"/>: a name (held against the record) or any detail the paper checks compare (PaperChecks.IsCompared).</summary>
    public static bool Reads(ClueCategory category) => category == ClueCategory.Name || PaperChecks.IsCompared(category);

    /// <summary>The table over <paramref name="columns"/>: rows in order of first appearance (column order, then value order).</summary>
    public static List<CrossRow> Rows(IReadOnlyList<CrossColumn> columns)
    {
        var rows = new List<CrossRow>();
        if (columns == null || columns.Count == 0)
            return rows;
        var order = new List<ClueCategory>();
        foreach (CrossColumn column in columns)
            foreach (CrossValue v in column.Values)
                if (Reads(v.Category) && !string.IsNullOrWhiteSpace(v.Value) && !order.Contains(v.Category))
                    order.Add(v.Category);

        int record = -1;
        for (int c = 0; c < columns.Count; c++)
            if (columns[c].IsRecord)
            {
                record = c;
                break;
            }

        foreach (ClueCategory category in order)
        {
            var row = new CrossRow { Category = category };
            foreach (CrossColumn column in columns)
            {
                CrossValue? found = null;
                foreach (CrossValue v in column.Values)
                    if (v.Category == category && !string.IsNullOrWhiteSpace(v.Value))
                    {
                        found = v;
                        break;
                    }
                row.Cells.Add(new CrossCell { Value = found });
            }
            if (row.Cells.Count(c => c.Value.HasValue) < 2)
                continue;

            bool recordStates = record >= 0 && row.Cells[record].Value.HasValue;
            for (int c = 0; c < row.Cells.Count; c++)
            {
                CrossCell cell = row.Cells[c];
                if (!cell.Value.HasValue)
                    continue;
                if (recordStates && c != record)
                {
                    if (!Values.Match(cell.Value.Value.Value, row.Cells[record].Value.Value.Value))
                    {
                        cell.Glows = true;
                        cell.Partner = record;
                    }
                    continue;
                }
                for (int o = 0; o < row.Cells.Count; o++)
                    if (o != c && row.Cells[o].Value.HasValue && !Values.Match(cell.Value.Value.Value, row.Cells[o].Value.Value.Value))
                    {
                        cell.Glows = true;
                        cell.Partner = o;
                        break;
                    }
            }
            rows.Add(row);
        }
        return rows;
    }
}

/// <summary>How the auto lookup found the record.</summary>
public enum LookupBy
{
    /// <summary>No scanned paper names an ID or a name: nothing was looked up.</summary>
    Nothing,

    /// <summary>By the papers' Citizen ID (or Displacement No.).</summary>
    Number,

    /// <summary>By the papers' name (no record holds their number).</summary>
    Name,

    /// <summary>Looked up, and no record is on file: the NO RECORD plate, itself a finding.</summary>
    NoRecord
}

/// <summary>
/// The auto record lookup (the scanner app spec §2.1): a scan of a paper
/// that carries a Citizen ID or a name opens the record it names, by the
/// first scanned number first (a whole number, Values.Match), else the first
/// scanned name; NO RECORD when neither finds one. Pure.
/// </summary>
public static class RecordLookup
{
    /// <summary>The record the <paramref name="scanned"/> papers name in <paramref name="registry"/>'s records, how it was found, and the query (the number or name looked up).</summary>
    public static (CitizenRecord Record, LookupBy By, string Query) Find(IReadOnlyList<CitizenRecord> registry, IReadOnlyList<BoardPaper> scanned)
    {
        string number = First(scanned, ClueCategory.CitizenId), name = First(scanned, ClueCategory.Name);
        if (number == null && name == null)
            return (null, LookupBy.Nothing, null);
        IEnumerable<CitizenRecord> records = (registry ?? Array.Empty<CitizenRecord>()).Where(r => r != null);
        if (number != null)
        {
            CitizenRecord byNumber = records.FirstOrDefault(r => !string.IsNullOrWhiteSpace(r.Number) && Values.Match(r.Number, number));
            if (byNumber != null)
                return (byNumber, LookupBy.Number, number.Trim());
        }
        if (name != null)
        {
            CitizenRecord byName = records.FirstOrDefault(r => Values.Match(r.FullName, name));
            if (byName != null)
                return (byName, LookupBy.Name, name.Trim());
        }
        return (null, LookupBy.NoRecord, (number ?? name).Trim());
    }

    /// <summary>The first scanned value of <paramref name="category"/>, or null.</summary>
    private static string First(IReadOnlyList<BoardPaper> scanned, ClueCategory category)
    {
        foreach (BoardPaper paper in scanned ?? Array.Empty<BoardPaper>())
            foreach (DocumentField field in paper.Fields)
                if (field != null && field.category == category && !string.IsNullOrWhiteSpace(field.value))
                    return field.value;
        return null;
    }

    /// <summary>A record's values for the cross-check (its evidence rows, by flat row index, RecordExtractPage's order: groups, then rows).</summary>
    public static List<CrossValue> RecordValues(CitizenRecord record)
    {
        var values = new List<CrossValue>();
        if (record == null)
            return values;
        int flat = 0;
        foreach (RecordGroup group in record.Groups)
            foreach (RecordRow row in group.Rows)
            {
                if (row.IsEvidence)
                    values.Add(new CrossValue(row.Category, flat, row.Label, row.Value));
                flat++;
            }
        return values;
    }
}
