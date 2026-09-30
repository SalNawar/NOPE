using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// Reads an edited narrative workbook back onto the content tables (the Narrative
/// sheet's text column and the Lines sheet's yellow columns; every other sheet is a view
/// and is ignored). Each bound row names its content row ("ref") and carries that row as
/// it was exported ("was"): the row is found at its number, or else as the one row that
/// still equals the copy; a cell the author changed is written there; a changed cell whose
/// row moved on in the source meanwhile is a conflict; an unchanged row that moved on is
/// skipped. Lines' new rows are added after their voice's last row of the slot (else at the
/// end) and rows marked "remove" are removed. Nothing is dropped quietly: a row it cannot
/// place is an error naming the workbook's sheet and row, and any error leaves
/// <see cref="Tables"/> null. The tables then go through the content spreadsheet's own
/// import (<see cref="ContentSheets.Import"/>). Pure.
/// </summary>
public sealed class NarrativeImport
{
    /// <summary>The content tables with the edits applied (the same sheets, in order); null when there is an error.</summary>
    public List<RowTable> Tables { get; private set; }

    /// <summary>What stops the import, each naming the workbook's sheet and row.</summary>
    public List<string> Errors { get; } = new List<string>();

    /// <summary>Every change, in words: the content sheet, row and column, the old and the new text, and where it was typed.</summary>
    public List<string> Changes { get; } = new List<string>();

    /// <summary>The bound rows read (Narrative and Lines).</summary>
    public int BoundRows { get; private set; }

    /// <summary>The bound rows whose content row changed in the source since the export, left alone because the author did not edit them.</summary>
    public int Stale { get; private set; }

    /// <summary>The content cells changed.</summary>
    public int Edited { get; private set; }

    /// <summary>The Lines rows added.</summary>
    public int Added { get; private set; }

    /// <summary>The Lines rows removed.</summary>
    public int Removed { get; private set; }

    /// <summary>The workbook row behind each changed or added content row: (content sheet, row number) to "Lines row 12".</summary>
    private readonly Dictionary<(string sheet, int row), string> _origins = new Dictionary<(string, int), string>();

    /// <summary>The row number the first added row of a sheet gets in the content tables (added rows sit past every real row, so error messages can name them).</summary>
    public const int AddedRowNumbers = 100000;

    private static readonly Regex RefPattern = new Regex(@"^(?<sheet>[A-Za-z0-9_]+)#(?<row>[0-9]+)(:(?<column>.+))?$");
    private static readonly Regex EngineRow = new Regex(@"^(?<sheet>[^:]+): row (?<row>[0-9]+)");

    /// <summary>
    /// Applies <paramref name="workbook"/> (the sheets read from the narrative workbook)
    /// to <paramref name="content"/> (today's content tables, <see cref="ContentSheets.Export"/>).
    /// The content tables are not changed; <see cref="Tables"/> holds the result.
    /// </summary>
    public static NarrativeImport Apply(IReadOnlyList<RowTable> content, IReadOnlyList<RowTable> workbook)
    {
        var import = new NarrativeImport();
        import.Run(content, workbook);
        return import;
    }

    /// <summary>
    /// <paramref name="errors"/> (the content import's, naming content sheets and rows)
    /// with the workbook row each came from where the row was edited or added here.
    /// </summary>
    public List<string> Explain(IEnumerable<string> errors) =>
        errors.Select(e =>
        {
            Match m = EngineRow.Match(e);
            return m.Success && _origins.TryGetValue((m.Groups["sheet"].Value, int.Parse(m.Groups["row"].Value, CultureInfo.InvariantCulture)), out string origin)
                ? $"{e} (typed at {origin})"
                : e;
        }).ToList();

    // ---- the run ----

    private sealed class Edit
    {
        public string Value;
        public string Origin;
    }

    private readonly Dictionary<string, RowTable> _content = new Dictionary<string, RowTable>();
    private readonly Dictionary<(string sheet, int row, int column), Edit> _edits = new Dictionary<(string, int, int), Edit>();
    private readonly Dictionary<(string sheet, int row), string> _removals = new Dictionary<(string, int), string>();
    private readonly List<(string sheet, string[] cells, string origin)> _additions = new List<(string, string[], string)>();

    private void Run(IReadOnlyList<RowTable> content, IReadOnlyList<RowTable> workbook)
    {
        foreach (RowTable t in content)
            _content[t.Name] = t;

        RowTable narrative = workbook.FirstOrDefault(t => t.Name == NarrativeWorkbook.NarrativeSheet);
        RowTable lines = workbook.FirstOrDefault(t => t.Name == NarrativeWorkbook.LinesSheet);
        if (narrative == null || lines == null)
        {
            Errors.Add($"The workbook has no '{(narrative == null ? NarrativeWorkbook.NarrativeSheet : NarrativeWorkbook.LinesSheet)}' sheet: it is not a narrative workbook, or a sheet was renamed (export a new one).");
            return;
        }

        ReadNarrative(narrative);
        ReadLines(lines);
        if (Errors.Count == 0)
            Tables = Build(content);
    }

    private static int Column(RowTable t, string header) => t.Headers.IndexOf(header);

    private static string Cell(string[] row, int column) => column >= 0 && column < row.Length ? row[column] ?? string.Empty : string.Empty;

    private bool Require(RowTable t, params string[] headers)
    {
        bool ok = true;
        foreach (string h in headers.Where(h => Column(t, h) < 0))
        {
            Errors.Add($"{t.Name}: the column '{h}' is missing (keep the workbook's columns as they are).");
            ok = false;
        }
        return ok;
    }

    private void ReadNarrative(RowTable t)
    {
        if (!Require(t, "text", "ref", "was"))
            return;
        int text = Column(t, "text"), reference = Column(t, "ref"), was = Column(t, "was");
        for (int i = 0; i < t.Rows.Count; i++)
        {
            string[] row = t.Rows[i];
            string origin = $"{t.Name} row {t.RowNumbers[i]}";
            string r = Cell(row, reference).Trim();
            if (r.Length == 0)
            {
                if (Cell(row, text).Length > 0)
                    Errors.Add($"{origin}: text in a row with no ref is not imported (new rows go in Lines for voice lines, or in the content spreadsheet for new structure).");
                continue;
            }
            Match m = RefPattern.Match(r);
            if (!m.Success || !m.Groups["column"].Success)
            {
                Errors.Add($"{origin}: the ref '{r}' is damaged (export a new workbook).");
                continue;
            }
            Bind(origin, m.Groups["sheet"].Value, int.Parse(m.Groups["row"].Value, CultureInfo.InvariantCulture), Cell(row, was),
                 new[] { (m.Groups["column"].Value, Cell(row, text)) }, false);
        }
    }

    private void ReadLines(RowTable t)
    {
        if (!Require(t, new[] { "slot", "remove", "ref", "was" }.Concat(NarrativeWorkbook.LineContentHeaders).ToArray()))
            return;
        int slot = Column(t, "slot"), remove = Column(t, "remove"), reference = Column(t, "ref"), was = Column(t, "was");
        for (int i = 0; i < t.Rows.Count; i++)
        {
            string[] row = t.Rows[i];
            string origin = $"{t.Name} row {t.RowNumbers[i]}";
            string sheet = Cell(row, slot).Trim();
            string mark = Cell(row, remove).Trim();
            bool removing = string.Equals(mark, NarrativeWorkbook.RemoveMark, StringComparison.OrdinalIgnoreCase);
            if (mark.Length > 0 && !removing)
            {
                Errors.Add($"{origin}: remove holds '{mark}'; leave it blank or choose '{NarrativeWorkbook.RemoveMark}'.");
                continue;
            }

            string r = Cell(row, reference).Trim();
            if (r.Length == 0)
            {
                bool any = NarrativeWorkbook.LineContentHeaders.Any(h => Cell(row, Column(t, h)).Length > 0);
                if (any || sheet.Length > 0)
                    AddLine(origin, t, row, sheet, removing);
                continue;
            }

            Match m = RefPattern.Match(r);
            if (!m.Success || m.Groups["column"].Success || !NarrativeWorkbook.LineSlots.Contains(m.Groups["sheet"].Value))
            {
                Errors.Add($"{origin}: the ref '{r}' is damaged (export a new workbook).");
                continue;
            }
            string target = m.Groups["sheet"].Value;
            if (sheet != target)
            {
                Errors.Add($"{origin}: the slot of an existing line cannot change ('{target}' to '{sheet}'); add a new row with the new slot and remove this one.");
                continue;
            }
            if (!_content.TryGetValue(target, out RowTable table))
            {
                Errors.Add($"{origin}: the content has no sheet '{target}' (export a new workbook).");
                continue;
            }
            if (!Applicable(origin, t, row, table))
                continue;
            var values = NarrativeWorkbook.LineContentHeaders.Where(table.Headers.Contains).Select(h => (h, Cell(row, Column(t, h)))).ToArray();
            Bind(origin, target, int.Parse(m.Groups["row"].Value, CultureInfo.InvariantCulture), Cell(row, was), values, removing);
        }
    }

    /// <summary>False (with an error) when the row fills a column its slot's sheet does not have.</summary>
    private bool Applicable(string origin, RowTable t, string[] row, RowTable table)
    {
        List<string> stray = NarrativeWorkbook.LineContentHeaders.Where(h => !table.Headers.Contains(h) && Cell(row, Column(t, h)).Length > 0).ToList();
        if (stray.Count == 0)
            return true;
        Errors.Add($"{origin}: {string.Join(", ", stray)} {(stray.Count == 1 ? "does" : "do")} not apply to slot {table.Name} (its columns: {string.Join(", ", table.Headers)}); clear {(stray.Count == 1 ? "it" : "them")}.");
        return false;
    }

    private void AddLine(string origin, RowTable t, string[] row, string sheet, bool removing)
    {
        if (!NarrativeWorkbook.LineSlots.Contains(sheet))
        {
            Errors.Add($"{origin}: a new line needs its slot (one of: {string.Join(", ", NarrativeWorkbook.LineSlots)}); it has '{sheet}'.");
            return;
        }
        if (removing)
        {
            Errors.Add($"{origin}: a new line cannot be removed; delete the row instead.");
            return;
        }
        if (!_content.TryGetValue(sheet, out RowTable table))
        {
            Errors.Add($"{origin}: the content has no sheet '{sheet}' (export a new workbook).");
            return;
        }
        if (!Applicable(origin, t, row, table))
            return;
        string[] cells = table.Headers.Select(h => NarrativeWorkbook.LineContentHeaders.Contains(h) ? Cell(row, Column(t, h)) : string.Empty).ToArray();
        _additions.Add((sheet, cells, origin));
    }

    /// <summary>Records a bound row's changed cells (and its removal) against its content row.</summary>
    private void Bind(string origin, string sheet, int rowNumber, string wasJson, IReadOnlyList<(string header, string value)> values, bool remove)
    {
        BoundRows++;
        if (!_content.TryGetValue(sheet, out RowTable table))
        {
            Errors.Add($"{origin}: the content has no sheet '{sheet}' (export a new workbook).");
            return;
        }
        Dictionary<string, string> was = ParseWas(wasJson);
        if (was == null)
        {
            Errors.Add($"{origin}: the hidden 'was' cell is damaged (export a new workbook).");
            return;
        }

        int at = Locate(table, rowNumber - 2, was);
        bool stale = false;
        foreach ((string header, string value) in values)
        {
            string old = was.TryGetValue(header, out string w) ? w : string.Empty;
            if (value == old)
            {
                stale |= at < 0;
                continue;
            }
            if (at < 0)
            {
                Errors.Add($"{origin}: conflict: {sheet} row {rowNumber} changed in world_source.json since the export (or is gone), and this row edits its {header}. Export again and redo the edit.");
                return;
            }
            int column = table.Headers.IndexOf(header);
            if (column < 0)
            {
                Errors.Add($"{origin}: {sheet} has no column '{header}' any more (export a new workbook).");
                return;
            }
            var key = (sheet, at, column);
            if (_edits.TryGetValue(key, out Edit other))
            {
                if (other.Value != value)
                    Errors.Add($"{origin}: {sheet} row {at + 2} {header} is also edited at {other.Origin}, differently; keep one.");
                continue;
            }
            _edits[key] = new Edit { Value = value, Origin = origin };
        }

        if (remove)
        {
            if (at < 0)
                Errors.Add($"{origin}: conflict: {sheet} row {rowNumber} changed in world_source.json since the export (or is gone), so it cannot be removed here. Export again.");
            else
                _removals[(sheet, at)] = origin;
        }
        else if (stale)
            Stale++;
    }

    private static Dictionary<string, string> ParseWas(string json)
    {
        try
        {
            ContentNode node = ContentJson.Parse(json);
            if (node.Kind != ContentNodeKind.Object || node.Members.Any(m => m.Value.Kind != ContentNodeKind.String))
                return null;
            return node.Members.ToDictionary(m => m.Key, m => m.Value.Text);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    /// <summary>The content row the copy describes: the row at <paramref name="hint"/> when it still equals it, else the one row that does; -1 when none (or several) does.</summary>
    private static int Locate(RowTable table, int hint, Dictionary<string, string> was)
    {
        List<int> columns = Enumerable.Range(0, table.Headers.Count).Where(c => was.ContainsKey(table.Headers[c])).ToList();
        if (columns.Count == 0)
            return -1;
        bool Same(int r) => columns.All(c => Cell(table.Rows[r], c) == was[table.Headers[c]]);
        if (hint >= 0 && hint < table.Rows.Count && Same(hint))
            return hint;
        List<int> matches = Enumerable.Range(0, table.Rows.Count).Where(Same).Take(2).ToList();
        return matches.Count == 1 ? matches[0] : -1;
    }

    /// <summary>The content tables with the edits, removals and additions applied.</summary>
    private List<RowTable> Build(IReadOnlyList<RowTable> content)
    {
        var result = new List<RowTable>();
        foreach (RowTable t in content)
        {
            var copy = new RowTable(t.Name, t.Headers) { Kinds = t.Kinds };
            List<(string sheet, string[] cells, string origin)> added = _additions.Where(a => a.sheet == t.Name).ToList();
            var after = new Dictionary<int, List<(string[] cells, string origin)>>();
            foreach ((string _, string[] cells, string origin) in added)
            {
                int anchor = Anchor(t, cells);
                if (!after.TryGetValue(anchor, out List<(string[], string)> list))
                    after[anchor] = list = new List<(string[], string)>();
                list.Add((cells, origin));
            }

            int next = AddedRowNumbers;
            void Insert(int anchor)
            {
                if (!after.TryGetValue(anchor, out List<(string[] cells, string origin)> list))
                    return;
                foreach ((string[] cells, string origin) in list)
                {
                    copy.Add(cells, next);
                    _origins[(t.Name, next)] = origin;
                    Changes.Add($"{t.Name}: new line \"{Short(cells[Math.Max(0, t.Headers.IndexOf("text"))])}\" ({origin})");
                    Added++;
                    next++;
                }
            }

            Insert(-1);
            for (int r = 0; r < t.Rows.Count; r++)
            {
                int number = t.RowNumbers.Count > r ? t.RowNumbers[r] : r + 2;
                if (_removals.TryGetValue((t.Name, r), out string removedAt))
                {
                    Changes.Add($"{t.Name} row {r + 2}: removed \"{Short(Cell(t.Rows[r], t.Headers.IndexOf("text")))}\" ({removedAt})");
                    Removed++;
                }
                else
                {
                    string[] cells = (string[])t.Rows[r].Clone();
                    for (int c = 0; c < cells.Length; c++)
                    {
                        if (!_edits.TryGetValue((t.Name, r, c), out Edit edit) || edit.Value == cells[c])
                            continue;
                        Changes.Add($"{t.Name} row {r + 2} {t.Headers[c]}: \"{Short(cells[c])}\" -> \"{Short(edit.Value)}\" ({edit.Origin})");
                        cells[c] = edit.Value;
                        _origins[(t.Name, number)] = edit.Origin;
                        Edited++;
                    }
                    copy.Add(cells, number);
                }
                Insert(r);
            }
            result.Add(copy);
        }
        return result;
    }

    /// <summary>Where a new line goes: after the last row of its voice (the same personality and premade) in its slot; -1 (the top) never, the end when the voice has none.</summary>
    private static int Anchor(RowTable t, string[] cells)
    {
        int personality = t.Headers.IndexOf("personality"), premade = t.Headers.IndexOf("premade");
        for (int r = t.Rows.Count - 1; r >= 0; r--)
            if (Cell(t.Rows[r], personality) == Cell(cells, personality) && Cell(t.Rows[r], premade) == Cell(cells, premade))
                return r;
        return t.Rows.Count - 1;
    }

    private static string Short(string s)
    {
        s = (s ?? string.Empty).Replace("\n", " / ");
        return s.Length > 90 ? s.Substring(0, 87) + "..." : s;
    }
}
