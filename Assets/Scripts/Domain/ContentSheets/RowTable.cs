using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>How a workbook column's cells are written (the CSV text is the same for all).</summary>
public enum CellKind
{
    /// <summary>Text: an inline string in a text-formatted column, so the spreadsheet never turns it into a number or a date.</summary>
    Text,

    /// <summary>A number cell.</summary>
    Number,

    /// <summary>A TRUE/FALSE cell.</summary>
    Bool
}

/// <summary>
/// One sheet as rows of text cells: a header row, then data rows with the spreadsheet
/// row number each came from (the header is row 1), for error messages. Fully blank
/// rows are never kept.
/// </summary>
public sealed class RowTable
{
    /// <summary>A table with its headers and no rows.</summary>
    public RowTable(string name, IEnumerable<string> headers)
    {
        Name = name;
        Headers = headers.ToList();
    }

    /// <summary>The sheet name (the CSV file name without ".csv").</summary>
    public string Name { get; }

    /// <summary>The header row.</summary>
    public List<string> Headers { get; }

    /// <summary>The data rows, each as wide as <see cref="Headers"/>.</summary>
    public List<string[]> Rows { get; } = new List<string[]>();

    /// <summary>The spreadsheet row number of each data row.</summary>
    public List<int> RowNumbers { get; } = new List<int>();

    /// <summary>How the workbook writes each column (null: all text).</summary>
    public CellKind[] Kinds { get; set; }

    /// <summary>Appends a row; its number is the next row down unless given.</summary>
    public void Add(string[] cells, int rowNumber = 0)
    {
        Rows.Add(cells);
        RowNumbers.Add(rowNumber > 0 ? rowNumber : (RowNumbers.Count > 0 ? RowNumbers[RowNumbers.Count - 1] : 1) + 1);
    }

    /// <summary>Whether two tables hold the same headers and cells (names and row numbers aside).</summary>
    public static bool SameCells(RowTable a, RowTable b) =>
        a.Headers.SequenceEqual(b.Headers) && a.Rows.Count == b.Rows.Count && a.Rows.Zip(b.Rows, (x, y) => x.SequenceEqual(y)).All(same => same);

    /// <summary>The spreadsheet column letter of a 0-based column (0 = A, 26 = AA).</summary>
    public static string ColumnLetter(int index)
    {
        string s = string.Empty;
        for (int n = index + 1; n > 0; n = (n - 1) / 26)
            s = (char)('A' + (n - 1) % 26) + s;
        return s;
    }

    /// <summary>
    /// Builds a table from raw records (the first is the header row): drops fully blank
    /// rows, pads short rows, widens the header row (with blank headers) for cells beyond
    /// it, then drops trailing columns that have neither a header nor a value.
    /// </summary>
    public static RowTable FromRecords(string name, IList<string[]> records, IList<int> rowNumbers)
    {
        int width = records.Count == 0 ? 0 : records.Max(r => r.Length);
        bool Used(int col) => records.Any(r => col < r.Length && !string.IsNullOrEmpty(r[col]));
        while (width > 0 && !Used(width - 1))
            width--;
        string[] Pad(string[] r) => Enumerable.Range(0, width).Select(i => i < r.Length ? r[i] ?? string.Empty : string.Empty).ToArray();

        var table = new RowTable(name, records.Count == 0 ? Array.Empty<string>() : Pad(records[0]).Select(h => h.Trim()));
        for (int i = 1; i < records.Count; i++)
            if (records[i].Any(c => !string.IsNullOrEmpty(c)))
                table.Add(Pad(records[i]), rowNumbers[i]);
        return table;
    }
}

/// <summary>
/// Comma-separated values as spreadsheets write them (RFC 4180): cells with a comma,
/// quote or line break are quoted, quotes doubled; UTF-8 (a leading byte-order mark is
/// skipped). A file whose header line has no comma but semicolons or tabs is read with
/// that separator (a spreadsheet saved in a comma-decimal locale).
/// </summary>
public static class Csv
{
    /// <summary>Reads one sheet; an unterminated quote is an error naming the sheet and row.</summary>
    public static RowTable Read(string name, string text, List<string> errors)
    {
        text = text ?? string.Empty;
        if (text.Length > 0 && text[0] == '﻿')
            text = text.Substring(1);
        char sep = Separator(text);

        var records = new List<string[]>();
        var numbers = new List<int>();
        var row = new List<string>();
        var cell = new StringBuilder();
        bool quoted = false, any = false;
        int record = 1, quoteStart = 0;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (quoted)
            {
                if (c == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        cell.Append('"');
                        i++;
                    }
                    else
                        quoted = false;
                }
                else
                    cell.Append(c);
                continue;
            }
            if (c == '"' && cell.Length == 0)
            {
                quoted = true;
                quoteStart = record;
                any = true;
            }
            else if (c == sep)
            {
                row.Add(cell.ToString());
                cell.Clear();
                any = true;
            }
            else if (c == '\r' || c == '\n')
            {
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                    i++;
                row.Add(cell.ToString());
                cell.Clear();
                records.Add(row.ToArray());
                numbers.Add(record++);
                row.Clear();
                any = false;
            }
            else
            {
                cell.Append(c);
                any = true;
            }
        }
        if (quoted)
            errors.Add($"{name}: row {quoteStart}: a quoted cell is never closed.");
        if (any || cell.Length > 0 || row.Count > 0)
        {
            row.Add(cell.ToString());
            records.Add(row.ToArray());
            numbers.Add(record);
        }
        return RowTable.FromRecords(name, records, numbers);
    }

    /// <summary>Writes one sheet: the header row, then the rows, CRLF line endings (no byte-order mark; add one when saving for spreadsheets).</summary>
    public static string Write(RowTable table)
    {
        var sb = new StringBuilder();
        WriteRow(sb, table.Headers);
        foreach (string[] r in table.Rows)
            WriteRow(sb, r);
        return sb.ToString();
    }

    private static void WriteRow(StringBuilder sb, IList<string> cells)
    {
        for (int i = 0; i < cells.Count; i++)
        {
            if (i > 0)
                sb.Append(',');
            string c = cells[i] ?? string.Empty;
            if (c.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0)
                sb.Append('"').Append(c.Replace("\"", "\"\"")).Append('"');
            else
                sb.Append(c);
        }
        sb.Append("\r\n");
    }

    private static char Separator(string text)
    {
        int end = text.IndexOfAny(new[] { '\r', '\n' });
        string first = end < 0 ? text : text.Substring(0, end);
        if (first.IndexOf(',') >= 0)
            return ',';
        if (first.IndexOf(';') >= 0)
            return ';';
        return first.IndexOf('\t') >= 0 ? '\t' : ',';
    }
}
