using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>One violation a citation lists: the mistake, the rule it broke (a small second line), the paper and box it refers to, and its penalty's words.</summary>
public sealed class CitationRow
{
    /// <summary>The mistake ("Approved an expired paper.").</summary>
    public string Violation = string.Empty;

    /// <summary>The rule it broke, with its Directive Memo row ("Directive 2: ..."); blank: none.</summary>
    public string Rule = string.Empty;

    /// <summary>The paper's code and the box ("TC-101 · VALID UNTIL"); blank: the decision refers to no box.</summary>
    public string Ref = string.Empty;

    /// <summary>What this row costs ("120 CR", "WARNING", "INCL.").</summary>
    public string Penalty = string.Empty;
}

/// <summary>
/// A citation as the desk prints it (the Citation, TC-900; Saleh 2026-10-07:
/// "it's a citation; think of a traffic violation"): one per wrong decision,
/// listing every violation behind it (a box of the traveller's papers that
/// shows the fault, CitationTickets.Rows), its number, the day's date, the
/// desk and the clerk, the one penalty of the decision (Saleh's rule: one
/// penalty for any wrong decision) and which kind it is. Runtime only.
/// </summary>
public sealed class CitationTicket
{
    /// <summary>The citation's number ("C-01-0003"): the day and the run's count of citations.</summary>
    public string Number = string.Empty;

    /// <summary>Today's date in the agency's calendar.</summary>
    public string Date = string.Empty;

    /// <summary>The clerk's desk.</summary>
    public string Desk = string.Empty;

    /// <summary>The clerk's Citizen ID.</summary>
    public string Clerk = string.Empty;

    /// <summary>The violations, in order.</summary>
    public readonly List<CitationRow> Rows = new List<CitationRow>();

    /// <summary>The decision's penalty in credits (0 for a free warning).</summary>
    public int Penalty;

    /// <summary>True for the day's free warning (no deduction).</summary>
    public bool Warning;
}

/// <summary>
/// The citation's rows and sheets (pure). Rows: each (paper, box) of the
/// traveller's papers that shows the fault (FaultFields.Of: a forger's false
/// box, an anachronism, a box holding a value the citation names) is one
/// violation referring to that paper's code and box; a decision that refers
/// to no box (an honest traveller denied, a denial without evidence) is one
/// row without a reference. The first row carries the decision's penalty
/// and the others print as included in it: one penalty per wrong decision
/// (Saleh 2026-09-29). Sheets: RowsPerSheet violations to a sheet (the art's
/// four numbered rows, Saleh: "this caps at 4"), a continuation sheet per
/// four more, numbered on (5-8, ...), the total only on the last sheet (the
/// earlier ones "SEE NEXT SHEET"), each later sheet's stub "CONT. k/n".
/// </summary>
public static class CitationTickets
{
    /// <summary>The violations a sheet holds (the art's rows).</summary>
    public const int RowsPerSheet = 4;

    /// <summary>The desk the clerk works at (the rulebook folder's "DESK 3").</summary>
    public const string Desk = "3";

    /// <summary>A sheet's printed values, in CitationFields order (the citation form's fields).</summary>
    public enum Field
    {
        /// <summary>The date.</summary>
        Date,

        /// <summary>The desk.</summary>
        Desk,

        /// <summary>The clerk.</summary>
        Clerk,

        /// <summary>The total penalty (or "SEE NEXT SHEET").</summary>
        Total,

        /// <summary>The warning box's tick.</summary>
        Warning,

        /// <summary>The fine box's tick.</summary>
        Fine,

        /// <summary>The docked wages box's tick (no such penalty yet: never ticked).</summary>
        Docked,

        /// <summary>The stub's citation number.</summary>
        StubNumber,

        /// <summary>The stub's total penalty.</summary>
        StubTotal
    }

    /// <summary>A row's printed values, in order after the sheet's own (Field) values.</summary>
    public enum RowField
    {
        /// <summary>The row's number (1-4 on the first sheet, 5-8 on the next).</summary>
        Number,

        /// <summary>The violation.</summary>
        Violation,

        /// <summary>The rule, under it.</summary>
        Rule,

        /// <summary>The paper and box.</summary>
        Ref,

        /// <summary>The penalty.</summary>
        Penalty
    }

    /// <summary>How many values a sheet prints: its own, then RowsPerSheet rows of RowField each.</summary>
    public static int FieldCount => Enum.GetValues(typeof(Field)).Length + RowsPerSheet * Enum.GetValues(typeof(RowField)).Length;

    /// <summary>The value index of row <paramref name="row"/> (0 to RowsPerSheet - 1)'s <paramref name="field"/> on a sheet.</summary>
    public static int IndexOf(int row, RowField field) => Enum.GetValues(typeof(Field)).Length + row * Enum.GetValues(typeof(RowField)).Length + (int)field;

    /// <summary>What a ticked box prints.</summary>
    public const string Tick = "X";

    /// <summary>The penalty words of a free warning (the citation is diegetic: English).</summary>
    public const string WarningWords = "WARNING · NO DEDUCTION";

    /// <summary>A further row's penalty words: the decision's one penalty is on the first row.</summary>
    public const string IncludedWords = "INCL.";

    /// <summary>A sheet's total before the last sheet.</summary>
    public const string SeeNextWords = "SEE NEXT SHEET";

    /// <summary>
    /// The rows of a wrong decision: one per (paper, box) of
    /// <paramref name="faultBoxes"/> referring to it as "{form} · {LABEL}"
    /// (the paper's form number from <paramref name="formNumbers"/>, the box's
    /// label from <paramref name="labels"/>, in capitals), each the
    /// <paramref name="violation"/> and <paramref name="rule"/>; or one row
    /// without a reference when no box shows it. The first row's penalty is
    /// <paramref name="penalty"/>, the others' <paramref name="included"/>.
    /// </summary>
    public static List<CitationRow> Rows(string violation, string rule, IReadOnlyList<(int document, int field)> faultBoxes,
                                         IReadOnlyList<string> formNumbers, IReadOnlyList<IReadOnlyList<string>> labels, string penalty, string included)
    {
        var rows = new List<CitationRow>();
        foreach ((int document, int field) in faultBoxes ?? Array.Empty<(int, int)>())
        {
            string form = formNumbers != null && document >= 0 && document < formNumbers.Count ? formNumbers[document] ?? string.Empty : string.Empty;
            IReadOnlyList<string> boxes = labels != null && document >= 0 && document < labels.Count ? labels[document] : null;
            string label = boxes != null && field >= 0 && field < boxes.Count ? (boxes[field] ?? string.Empty).ToUpperInvariant() : string.Empty;
            string reference = form.Length > 0 && label.Length > 0 ? form + " · " + label : form + label;
            rows.Add(new CitationRow { Violation = violation ?? string.Empty, Rule = rule ?? string.Empty, Ref = reference, Penalty = rows.Count == 0 ? penalty : included });
        }
        if (rows.Count == 0)
            rows.Add(new CitationRow { Violation = violation ?? string.Empty, Rule = rule ?? string.Empty, Penalty = penalty });
        return rows;
    }

    /// <summary>The citation's number on day <paramref name="day"/>, the run's citation number <paramref name="count"/> ("C-01-0003").</summary>
    public static string Number(int day, int count) =>
        string.Format(CultureInfo.InvariantCulture, "C-{0:00}-{1:0000}", Math.Max(0, day), Math.Max(0, count));

    /// <summary>A penalty's words: "{n} CR" (credits are the agency's "cr"), or <paramref name="warning"/> for a free warning.</summary>
    public static string PenaltyText(CitationTicket ticket, string warning) =>
        ticket.Warning ? warning : ticket.Penalty.ToString(CultureInfo.InvariantCulture) + " CR";

    /// <summary>
    /// The sheets <paramref name="ticket"/> prints: one per RowsPerSheet rows
    /// (at least one), each its values in sheet order (Field, then each row's
    /// RowField; a row past the violations blank), the rows numbered on
    /// across sheets, the total on the last sheet only (the others print
    /// <paramref name="seeNext"/>), the stub's number "{n} · CONT. k/n" on
    /// every sheet after the first, the warning box ticked for a free warning
    /// and the fine box for a penalty; <paramref name="warning"/> is a free
    /// warning's penalty words.
    /// </summary>
    public static List<string[]> Sheets(CitationTicket ticket, string warning, string seeNext)
    {
        var sheets = new List<string[]>();
        if (ticket == null)
            return sheets;
        int count = Math.Max(1, (ticket.Rows.Count + RowsPerSheet - 1) / RowsPerSheet);
        string total = PenaltyText(ticket, warning);
        for (int s = 0; s < count; s++)
        {
            var v = new string[FieldCount];
            for (int i = 0; i < v.Length; i++)
                v[i] = string.Empty;
            bool last = s == count - 1;
            v[(int)Field.Date] = ticket.Date ?? string.Empty;
            v[(int)Field.Desk] = ticket.Desk ?? string.Empty;
            v[(int)Field.Clerk] = ticket.Clerk ?? string.Empty;
            v[(int)Field.Total] = last ? total : seeNext;
            v[(int)Field.StubTotal] = last ? total : seeNext;
            v[(int)Field.Warning] = ticket.Warning ? Tick : string.Empty;
            v[(int)Field.Fine] = !ticket.Warning && ticket.Penalty > 0 ? Tick : string.Empty;
            v[(int)Field.StubNumber] = s == 0 ? ticket.Number ?? string.Empty
                : string.Format(CultureInfo.InvariantCulture, "{0} · CONT. {1}/{2}", ticket.Number, s + 1, count);
            for (int r = 0; r < RowsPerSheet; r++)
            {
                int at = s * RowsPerSheet + r;
                v[IndexOf(r, RowField.Number)] = (at + 1).ToString(CultureInfo.InvariantCulture);
                if (at >= ticket.Rows.Count)
                    continue;
                CitationRow row = ticket.Rows[at];
                v[IndexOf(r, RowField.Violation)] = row.Violation;
                v[IndexOf(r, RowField.Rule)] = row.Rule;
                v[IndexOf(r, RowField.Ref)] = row.Ref;
                v[IndexOf(r, RowField.Penalty)] = row.Penalty;
            }
            sheets.Add(v);
        }
        return sheets;
    }
}
