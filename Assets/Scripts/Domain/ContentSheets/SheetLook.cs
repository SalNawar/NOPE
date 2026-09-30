using System.Collections.Generic;

/// <summary>How a styled workbook draws one cell (<see cref="SheetLook"/>; the plain content workbook uses none).</summary>
public enum CellLook
{
    /// <summary>Read-only text: grey, locked while the sheet is protected.</summary>
    Locked,

    /// <summary>A cell the author edits and the import reads: pale yellow, unlocked.</summary>
    Editable,

    /// <summary>A cell that does not apply to its row: shaded, locked.</summary>
    NotApplicable,

    /// <summary>A section banner: bold on a blue band, locked.</summary>
    Section,

    /// <summary>A binding cell the import reads but the author never edits (a ref, a hidden copy): grey, locked, never wrapped, so it never grows its row.</summary>
    Binding
}

/// <summary>A list rule on some cells of a styled sheet: the spreadsheet offers <see cref="Source"/>'s values in a drop-down and refuses others.</summary>
public sealed class ListRule
{
    /// <summary>A rule offering the values of <paramref name="source"/>: a range ("Lists!$A$2:$A$9") or a quoted list ("\"a,b\"", at most 255 characters).</summary>
    public ListRule(string source) => Source = source;

    /// <summary>The spreadsheet formula naming the values the cells may hold.</summary>
    public string Source { get; }

    /// <summary>The cells it covers, as runs of data rows in one column: (first data row, last data row, column), 0-based (data row 0 is the spreadsheet's row 2).</summary>
    public List<(int first, int last, int column)> Spans { get; } = new List<(int, int, int)>();

    /// <summary>Covers one cell.</summary>
    public ListRule Add(int row, int column)
    {
        Spans.Add((row, row, column));
        return this;
    }

    /// <summary>Covers a column from data row <paramref name="first"/> to <paramref name="last"/> (rows typed below the data included).</summary>
    public ListRule AddColumn(int column, int first, int last)
    {
        Spans.Add((first, last, column));
        return this;
    }
}

/// <summary>
/// The look of one sheet of a styled workbook (Xlsx.Write draws it; the narrative
/// workbook uses it): each column's look (editable columns pale yellow and unlocked, the
/// rest locked; text wraps and sits at the top), per-cell overrides, per-row section
/// banners, widths, hidden columns, frozen columns, a filter on the header row, protection (no password:
/// unlocked cells stay editable, rows can be inserted, sorted and filtered), list rules
/// and a hidden sheet. Reading ignores all of it: a styled sheet reads back as its text.
/// </summary>
public sealed class SheetLook
{
    /// <summary>Each column's look (a column past the end is <see cref="CellLook.Locked"/>).</summary>
    public CellLook[] Columns { get; set; } = new CellLook[0];

    /// <summary>Each column's width in characters (0 or past the end: sized to its text, 8 to 60).</summary>
    public int[] Widths { get; set; } = new int[0];

    /// <summary>The columns the spreadsheet hides (true at a column's index).</summary>
    public bool[] HiddenColumns { get; set; } = new bool[0];

    /// <summary>Cells whose look differs from their column's: (data row index, column index).</summary>
    public Dictionary<(int row, int column), CellLook> Cells { get; } = new Dictionary<(int, int), CellLook>();

    /// <summary>Data rows drawn as section banners (every cell <see cref="CellLook.Section"/>).</summary>
    public HashSet<int> SectionRows { get; } = new HashSet<int>();

    /// <summary>How many columns stay in view when scrolling right (the header row always does).</summary>
    public int FreezeColumns { get; set; }

    /// <summary>A filter on the header row.</summary>
    public bool Filter { get; set; }

    /// <summary>The sheet is protected without a password: locked cells refuse edits (Review &gt; Unprotect Sheet lifts it).</summary>
    public bool Protect { get; set; }

    /// <summary>The sheet is hidden (the first sheet never should be).</summary>
    public bool Hidden { get; set; }

    /// <summary>The sheet tab's colour as ARGB hex ("FFB4C7E7"); null for none.</summary>
    public string TabColor { get; set; }

    /// <summary>The list rules.</summary>
    public List<ListRule> Lists { get; } = new List<ListRule>();

    /// <summary>How the cell at (data row, column) is drawn.</summary>
    public CellLook LookOf(int row, int column)
    {
        if (SectionRows.Contains(row))
            return CellLook.Section;
        if (Cells.TryGetValue((row, column), out CellLook look))
            return look;
        return column < Columns.Length ? Columns[column] : CellLook.Locked;
    }
}
