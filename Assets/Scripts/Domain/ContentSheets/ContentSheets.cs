using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// The content spreadsheet engine, driven only by a declarative map (a tree of
/// <see cref="SheetSpec"/>): the content JSON to one table per sheet (export) and the
/// tables back to the JSON (import), so a new JSON section is a new map entry, not new
/// code. Export then import gives the same bytes back. The import checks everything it
/// can name by sheet, row and column: headers (an unknown or missing column is an
/// error), sheets (missing or unknown), cell types, required cells, allowed values,
/// duplicate row keys, child rows whose parent row does not exist, and references to
/// other sheets' rows. The export refuses JSON the sheets cannot hold (an unmapped key,
/// a field the map always writes but the JSON lacks, a default the map leaves out) with
/// the JSON path, so the round trip never loses or invents anything.
/// </summary>
public static class ContentSheets
{
    /// <summary>The documentation sheet a workbook starts with; the import skips it.</summary>
    public const string ReadmeSheet = "README";

    /// <summary>How many rows of each top-level table the template keeps as examples (with all their child rows).</summary>
    public const int TemplateExamples = 2;

    private const char Sep = '\u001f';
    private static readonly Regex WholeNumber = new Regex("^-?[0-9]+$");

    // =====================================================================
    // The map
    // =====================================================================

    /// <summary>What is wrong with a map (empty when it is sound): names, headers, keys, shapes, references, colliding paths.</summary>
    public static List<string> MapProblems(SheetSpec map)
    {
        var problems = new List<string>();
        if (map.Shape != SheetShape.Single || map.Path != string.Empty)
            problems.Add($"The map's root '{map.Name}' must be Single(name, \"\", ...), the document itself.");

        List<Node> nodes = Flatten(map);
        var byName = new Dictionary<string, Node>(StringComparer.OrdinalIgnoreCase);
        foreach (Node n in nodes)
        {
            string name = n.Spec.Name;
            if (byName.ContainsKey(name))
                problems.Add($"The sheet name '{name}' is used twice (sheet names ignore case).");
            else
                byName[name] = n;
            if (string.IsNullOrEmpty(name) || name.Length > 31)
                problems.Add($"The sheet name '{name}' must have 1 to 31 characters (Excel's limit).");
            if (name.IndexOfAny(new[] { '[', ']', ':', '*', '?', '/', '\\' }) >= 0 || string.Equals(name, ReadmeSheet, StringComparison.OrdinalIgnoreCase))
                problems.Add($"The sheet name '{name}' is not allowed.");
        }

        foreach (Node n in nodes)
        {
            SheetSpec s = n.Spec;
            foreach (string dup in n.Headers.GroupBy(h => h).Where(g => g.Count() > 1).Select(g => g.Key))
                problems.Add($"Sheet '{s.Name}': the header '{dup}' appears twice (a column named like an ancestor's key column?).");
            if (n.Headers.Any(string.IsNullOrEmpty))
                problems.Add($"Sheet '{s.Name}' has a column without a header.");

            if (s.Shape == SheetShape.Single && n.Parent != null && n.Parent.Spec.Shape != SheetShape.Single)
                problems.Add($"Sheet '{s.Name}' is a one-row sheet under '{n.Parent.Spec.Name}', which has many rows: use dotted column paths instead.");
            if (s.Shape == SheetShape.Keyed && (s.KeyColumn == null || s.KeyColumn.Type != CellType.Text))
                problems.Add($"Keyed sheet '{s.Name}' needs a text key column.");
            if (s.Shape == SheetShape.Keyed && s.Path == string.Empty && n.Parent == null)
                problems.Add($"Keyed sheet '{s.Name}' with the path \"\" needs a parent sheet.");
            if (s.Shape == SheetShape.Values && (s.Fields.Count != 1 || !(s.Fields[0] is ColumnSpec)))
                problems.Add($"Values sheet '{s.Name}' holds exactly one column.");
            if (s.RowKey != null)
                foreach (string c in s.RowKey.Columns.Where(c => s.Columns.All(col => col.Header != c)))
                    problems.Add($"Sheet '{s.Name}': the key column '{c}' is not one of its columns.");
            if (s.Shape != SheetShape.Single && s.KeyHeader == null)
                foreach (SheetSpec child in s.Children)
                    problems.Add($"Sheet '{child.Name}' sits under '{s.Name}', whose rows have no key: give '{s.Name}' a Key(column, childHeader).");

            var paths = s.Fields.Where(f => !(f is SheetSpec k && k.Shape == SheetShape.Keyed && k.Path == string.Empty)).Select(f => f.Path).ToList();
            if (s.Shape != SheetShape.Values)
                for (int i = 0; i < paths.Count; i++)
                    for (int j = 0; j < paths.Count; j++)
                        if (i != j && (paths[i] == paths[j] ? i < j : paths[j].StartsWith(paths[i] + ".", StringComparison.Ordinal)))
                            problems.Add($"Sheet '{s.Name}': the paths '{paths[i]}' and '{paths[j]}' collide.");

            foreach (ColumnSpec col in AllColumns(s))
            {
                if (col.RefSheet != null)
                {
                    Node target = nodes.FirstOrDefault(x => x.Spec.Name == col.RefSheet);
                    if (target == null)
                        problems.Add($"Sheet '{s.Name}', column '{col.Header}': it refers to sheet '{col.RefSheet}', which is not in the map.");
                    else if (target.Spec.RowKey == null && target.Spec.Shape != SheetShape.Keyed)
                        problems.Add($"Sheet '{s.Name}', column '{col.Header}': it refers to sheet '{col.RefSheet}', whose rows have no key.");
                }
            }
        }
        return problems;
    }

    // =====================================================================
    // Export: JSON to tables
    // =====================================================================

    /// <summary>
    /// The JSON as one table per sheet, in map order. <paramref name="examples"/> above 0
    /// keeps only that many rows of each top-level table (and their child rows): the
    /// template. What the sheets cannot hold goes to <paramref name="problems"/>.
    /// </summary>
    public static List<RowTable> Export(SheetSpec map, ContentNode world, List<string> problems, int examples = 0)
    {
        List<Node> nodes = Flatten(map);
        var ctx = new ExportContext(nodes, problems, examples);
        if (world == null || world.Kind != ContentNodeKind.Object)
            problems.Add("$ must be an object.");
        else
            ExportRow(ctx, nodes[0], world, Array.Empty<string>(), "$", null);
        return nodes.Select(n => ctx.Tables[n.Spec]).ToList();
    }

    /// <summary>The workbook for the JSON: the README sheet, then <see cref="Export"/>'s tables.</summary>
    public static List<RowTable> Workbook(SheetSpec map, ContentNode world, List<string> problems, int examples = 0) =>
        new[] { Readme(map) }.Concat(Export(map, world, problems, examples)).ToList();

    private sealed class ExportContext
    {
        public ExportContext(List<Node> nodes, List<string> problems, int examples)
        {
            Nodes = nodes.ToDictionary(n => n.Spec);
            Problems = problems;
            Examples = examples;
            Tables = nodes.ToDictionary(n => n.Spec, n => new RowTable(n.Spec.Name, n.Headers) { Kinds = n.Kinds });
        }

        public Dictionary<SheetSpec, Node> Nodes { get; }
        public Dictionary<SheetSpec, RowTable> Tables { get; }
        public List<string> Problems { get; }
        public int Examples { get; }
    }

    private static void ExportRow(ExportContext ctx, Node node, ContentNode obj, string[] ancestors, string path, string memberName)
    {
        SheetSpec s = node.Spec;
        var cells = new List<string>(ancestors);
        if (s.Shape == SheetShape.Keyed)
            cells.Add(memberName);
        foreach (ColumnSpec col in s.Columns)
            cells.Add(CellOf(ctx, node, col, Lookup(obj, col.Path), path + "." + col.Path));
        ctx.Tables[s].Add(cells.ToArray());

        string own = s.Shape == SheetShape.Keyed ? memberName : s.RowKey?.Of(h => cells[node.Headers.IndexOf(h)]);
        string[] childAncestors = s.Shape != SheetShape.Single && s.KeyHeader != null ? ancestors.Concat(new[] { own }).ToArray() : ancestors;

        CheckCovered(ctx, s, obj, path);
        foreach (SheetSpec child in s.Children)
            ExportChild(ctx, ctx.Nodes[child], obj, childAncestors, path);
    }

    private static void ExportChild(ExportContext ctx, Node node, ContentNode parent, string[] ancestors, string parentPath)
    {
        SheetSpec s = node.Spec;
        int limit = ctx.Examples > 0 && node.TopLevel ? ctx.Examples : int.MaxValue;

        if (s.Shape == SheetShape.Keyed && s.Path == string.Empty)
        {
            HashSet<string> taken = FirstSegments(node.Parent.Spec, except: s);
            foreach (KeyValuePair<string, ContentNode> m in parent.Members.Where(m => !taken.Contains(m.Key)).Take(limit))
                ExportMember(ctx, node, m, ancestors, parentPath);
            return;
        }

        string path = parentPath + "." + s.Path;
        ContentNode value = Lookup(parent, s.Path);
        if (value == null)
        {
            if (!s.OmitWhenEmpty)
                ctx.Problems.Add($"{path} is missing, but sheet '{s.Name}' always writes it (mark the sheet .OmitEmpty() in the map if it may be left out).");
            return;
        }

        ContentNodeKind expected = s.Shape == SheetShape.Single || s.Shape == SheetShape.Keyed ? ContentNodeKind.Object : ContentNodeKind.Array;
        if (value.Kind != expected)
        {
            ctx.Problems.Add($"{path} is {Describe(value)}, but sheet '{s.Name}' holds {(expected == ContentNodeKind.Object ? "an object" : "a list")}.");
            return;
        }
        int count = expected == ContentNodeKind.Object ? value.Members.Count : value.Items.Count;
        if (count == 0 && s.OmitWhenEmpty && s.Shape != SheetShape.Single)
            ctx.Problems.Add($"{path} is empty, but sheet '{s.Name}' leaves it out of the JSON when it has no rows: drop it from the JSON.");

        switch (s.Shape)
        {
            case SheetShape.Single:
                ExportRow(ctx, node, value, ancestors, path, null);
                break;
            case SheetShape.Keyed:
                foreach (KeyValuePair<string, ContentNode> m in value.Members.Take(limit))
                    ExportMember(ctx, node, m, ancestors, path);
                break;
            case SheetShape.Rows:
                for (int i = 0; i < value.Items.Count && i < limit; i++)
                {
                    ContentNode item = value.Items[i];
                    if (item.Kind == ContentNodeKind.Object)
                        ExportRow(ctx, node, item, ancestors, $"{path}[{i}]", null);
                    else
                        ctx.Problems.Add($"{path}[{i}] is {Describe(item)}, but sheet '{s.Name}' holds objects.");
                }
                break;
            case SheetShape.Values:
                var col = (ColumnSpec)s.Fields[0];
                for (int i = 0; i < value.Items.Count && i < limit; i++)
                {
                    string cell = CellOf(ctx, node, col, value.Items[i], $"{path}[{i}]");
                    if (cell.Trim().Length == 0)
                        ctx.Problems.Add($"{path}[{i}] is blank, which a row of sheet '{s.Name}' cannot hold.");
                    ctx.Tables[s].Add(ancestors.Concat(new[] { cell }).ToArray());
                }
                break;
        }
    }

    private static void ExportMember(ExportContext ctx, Node node, KeyValuePair<string, ContentNode> m, string[] ancestors, string path)
    {
        if (m.Value.Kind == ContentNodeKind.Object)
            ExportRow(ctx, node, m.Value, ancestors, path + "." + m.Key, m.Key);
        else
            ctx.Problems.Add($"{path}.{m.Key} is {Describe(m.Value)}, but sheet '{node.Spec.Name}' holds objects.");
    }

    /// <summary>A cell's text for a JSON value, reporting what the round trip could not give back.</summary>
    private static string CellOf(ExportContext ctx, Node node, ColumnSpec col, ContentNode v, string path)
    {
        string where = $"column '{col.Header}' of sheet '{node.Spec.Name}'";
        if (v == null)
        {
            if (!col.OmitDefault && node.Spec.Shape != SheetShape.Values)
                ctx.Problems.Add($"{path} is missing, but {where} always writes it (mark the column .Omit() in the map if it may be left out).");
            return string.Empty;
        }
        if (col.OmitDefault && ContentNode.Same(v, DefaultNode(col)))
            ctx.Problems.Add($"{path} holds its default, which {where} leaves out of the JSON: drop it from the JSON.");

        bool ok;
        string text = v.Text ?? string.Empty;
        switch (col.Type)
        {
            case CellType.Text:
                ok = v.Kind == ContentNodeKind.String;
                break;
            case CellType.Int:
                ok = v.IsWholeNumber;
                break;
            case CellType.Float:
                ok = v.Kind == ContentNodeKind.Number;
                if (ok && v.IsWholeNumber)
                    ctx.Problems.Add($"{path} is the whole number {v.Text}, but {where} holds decimals: write {v.Text}.0.");
                break;
            case CellType.Number:
                ok = v.Kind == ContentNodeKind.Number;
                break;
            case CellType.Bool:
                ok = v.Kind == ContentNodeKind.Bool;
                break;
            default:
                bool texts = col.Type == CellType.TextList;
                ok = v.Kind == ContentNodeKind.Array && v.Items.All(i => i.Kind == (texts ? ContentNodeKind.String : ContentNodeKind.Number));
                if (ok && texts)
                    foreach (ContentNode item in v.Items.Where(i => i.Text.Length == 0 || i.Text.Contains("|") || i.Text.Trim() != i.Text))
                        ctx.Problems.Add($"{path} has the item \"{item.Text}\", which a '|' list cannot hold (empty, a '|', or spaces at an end).");
                text = ok ? string.Join("|", v.Items.Select(i => i.Text)) : string.Empty;
                break;
        }
        if (!ok)
            ctx.Problems.Add($"{path} is {Describe(v)}, but {where} holds {TypeName(col.Type)}.");
        return text;
    }

    /// <summary>Reports the members of a row object that no field of the sheet maps.</summary>
    private static void CheckCovered(ExportContext ctx, SheetSpec s, ContentNode obj, string path)
    {
        bool rest = s.Children.Any(c => c.Shape == SheetShape.Keyed && c.Path == string.Empty);
        var tree = new Claim();
        foreach (SheetField f in s.Fields)
        {
            if (f is SheetSpec k && k.Shape == SheetShape.Keyed && k.Path == string.Empty)
                continue;
            Claim at = tree;
            foreach (string seg in f.Path.Split('.'))
            {
                if (!at.Children.TryGetValue(seg, out Claim next))
                    at.Children[seg] = next = new Claim();
                at = next;
            }
            at.Leaf = true;
        }
        CheckCovered(ctx, tree, rest, obj, path);
    }

    private static void CheckCovered(ExportContext ctx, Claim claim, bool rest, ContentNode obj, string path)
    {
        foreach (KeyValuePair<string, ContentNode> m in obj.Members)
        {
            if (claim.Children.TryGetValue(m.Key, out Claim c))
            {
                if (c.Leaf)
                    continue;
                if (m.Value.Kind != ContentNodeKind.Object)
                    ctx.Problems.Add($"{path}.{m.Key} is {Describe(m.Value)}, but the map reads it as an object.");
                else
                    CheckCovered(ctx, c, false, m.Value, path + "." + m.Key);
            }
            else if (!rest)
                ctx.Problems.Add($"{path}.{m.Key} is not mapped to any sheet: add a column or a sheet for it in ContentSheetMap.");
        }
    }

    private sealed class Claim
    {
        public Dictionary<string, Claim> Children { get; } = new Dictionary<string, Claim>();
        public bool Leaf { get; set; }
    }

    // =====================================================================
    // Import: tables to JSON
    // =====================================================================

    /// <summary>
    /// The JSON the tables describe; null when any check fails (the errors, each naming
    /// its sheet, row and column, go to <paramref name="errors"/>). The README sheet is
    /// skipped; every other sheet must be one of the map's, and every map sheet present.
    /// </summary>
    public static ContentNode Import(SheetSpec map, IEnumerable<RowTable> sheets, List<string> errors)
    {
        int before = errors.Count;
        List<Node> nodes = Flatten(map);
        var byName = new Dictionary<string, RowTable>();
        foreach (RowTable t in sheets)
        {
            if (string.Equals(t.Name, ReadmeSheet, StringComparison.OrdinalIgnoreCase))
                continue;
            if (nodes.All(n => n.Spec.Name != t.Name))
                errors.Add($"{t.Name}: there is no such sheet in the content map (a renamed sheet, or a table the map does not have yet).");
            else if (byName.ContainsKey(t.Name))
                errors.Add($"{t.Name}: the sheet appears twice.");
            else
                byName[t.Name] = t;
        }

        var data = new Dictionary<SheetSpec, SheetRows>();
        foreach (Node n in nodes)
        {
            if (!byName.TryGetValue(n.Spec.Name, out RowTable table))
            {
                errors.Add($"{n.Spec.Name}: the sheet is missing (export the content to get every sheet, then copy your rows over).");
                continue;
            }
            SheetRows rows = ReadRows(n, table, n.Parent != null && data.TryGetValue(n.Parent.Spec, out SheetRows p) ? p : null, errors);
            if (rows != null)
                data[n.Spec] = rows;
        }
        CheckRefs(nodes, data, errors);

        if (errors.Count > before || !data.TryGetValue(map, out SheetRows root) || root.Rows.Count != 1)
            return null;
        return BuildRow(nodes.ToDictionary(n => n.Spec), data, nodes[0], root.Rows[0]);
    }

    private sealed class ParsedRow
    {
        public int Number;
        public string[] Raw;
        public ContentNode[] Values;
        public string ParentKey;
        public string OwnKey;
        public string PathKey;
    }

    private sealed class SheetRows
    {
        public Node Node;
        public int[] TableColumn;
        public List<ParsedRow> Rows = new List<ParsedRow>();
        public Dictionary<string, List<ParsedRow>> ByParent = new Dictionary<string, List<ParsedRow>>();
        public HashSet<string> PathKeys = new HashSet<string>();
        public HashSet<string> OwnKeys = new HashSet<string>();

        public string Where(ParsedRow row, int header) =>
            $"{Node.Spec.Name}: row {row.Number}, column {RowTable.ColumnLetter(TableColumn[header])} ({Node.Headers[header]})";
    }

    private static SheetRows ReadRows(Node node, RowTable table, SheetRows parent, List<string> errors)
    {
        SheetSpec s = node.Spec;
        string sheet = s.Name;
        int before = errors.Count;

        // Headers: every column known, none twice, none missing.
        var at = new int[node.Headers.Count];
        for (int i = 0; i < at.Length; i++)
            at[i] = -1;
        for (int c = 0; c < table.Headers.Count; c++)
        {
            string h = table.Headers[c];
            string letter = RowTable.ColumnLetter(c);
            if (h.Length == 0)
            {
                if (table.Rows.Any(r => c < r.Length && r[c].Length > 0))
                    errors.Add($"{sheet}: column {letter} has values but no header.");
                continue;
            }
            int idx = node.Headers.IndexOf(h);
            if (idx < 0)
            {
                string near = node.Headers.FirstOrDefault(x => string.Equals(x, h, StringComparison.OrdinalIgnoreCase) || string.Equals(x, h.Replace(" ", ""), StringComparison.OrdinalIgnoreCase));
                errors.Add(near != null
                    ? $"{sheet}: column {letter} '{h}' is not a column of this sheet; did you mean '{near}'?"
                    : $"{sheet}: column {letter} '{h}' is not a column of this sheet (its columns: {string.Join(", ", node.Headers)}).");
            }
            else if (at[idx] >= 0)
                errors.Add($"{sheet}: column {letter} '{h}' appears twice (also column {RowTable.ColumnLetter(at[idx])}).");
            else
                at[idx] = c;
        }
        for (int i = 0; i < at.Length; i++)
            if (at[i] < 0)
                errors.Add($"{sheet}: the column '{node.Headers[i]}' is missing (add it even if empty: a blank cell takes the column's default).");
        if (errors.Count > before)
            return null;

        var rows = new SheetRows { Node = node, TableColumn = at };
        int anc = node.AncestorHeaders.Count;
        List<ColumnSpec> columns = node.ValueColumns;
        int firstColumn = anc + (s.Shape == SheetShape.Keyed ? 1 : 0);
        HashSet<string> taken = s.Shape == SheetShape.Keyed && s.Path == string.Empty ? FirstSegments(node.Parent.Spec, except: s) : null;
        var firstRowOfKey = new Dictionary<string, int>();

        for (int r = 0; r < table.Rows.Count; r++)
        {
            string[] cells = table.Rows[r];
            var row = new ParsedRow
            {
                Number = table.RowNumbers[r],
                Raw = at.Select(c => c < cells.Length ? cells[c] ?? string.Empty : string.Empty).ToArray(),
                Values = new ContentNode[columns.Count]
            };
            row.ParentKey = string.Join(Sep.ToString(), row.Raw.Take(anc));

            if (s.Shape == SheetShape.Keyed)
            {
                row.OwnKey = row.Raw[anc];
                if (row.OwnKey.Length == 0)
                    errors.Add($"{rows.Where(row, anc)}: the member's name is required.");
                else if (s.KeyColumn.Allowed != null && !s.KeyColumn.Allowed.Contains(row.OwnKey))
                    errors.Add($"{rows.Where(row, anc)}: '{row.OwnKey}' must be one of: {string.Join(", ", s.KeyColumn.Allowed)}.");
                else if (taken != null && taken.Contains(row.OwnKey))
                    errors.Add($"{rows.Where(row, anc)}: '{row.OwnKey}' is the name of a column of '{node.Parent.Spec.Name}'.");
            }

            for (int j = 0; j < columns.Count; j++)
            {
                ColumnSpec col = columns[j];
                int h = firstColumn + j;
                string cell = row.Raw[h];
                bool blank = col.Type == CellType.Text ? cell.Length == 0 : cell.Trim().Length == 0;
                if (blank && (col.IsRequired || s.Shape == SheetShape.Values))
                {
                    errors.Add($"{rows.Where(row, h)}: a value is required.");
                    continue;
                }
                ContentNode value = ParseCell(col, cell, out string error);
                if (error != null)
                {
                    errors.Add($"{rows.Where(row, h)}: {error}");
                    continue;
                }
                if (col.Allowed != null && !blank)
                    foreach (string v in ItemsOf(col, value).Where(v => !col.Allowed.Contains(v)))
                        errors.Add($"{rows.Where(row, h)}: '{v}' must be one of: {string.Join(", ", col.Allowed)}.");
                row.Values[j] = col.OmitDefault && ContentNode.Same(value, DefaultNode(col)) ? null : value;
            }

            if (s.RowKey != null)
            {
                List<int> keyCols = s.RowKey.Columns.Select(c => node.Headers.IndexOf(c)).ToList();
                foreach (int k in keyCols.Where(k => row.Raw[k].Trim().Length == 0 && !columns[k - firstColumn].IsRequired))
                    errors.Add($"{rows.Where(row, k)}: a value is required (it names the row).");
                row.OwnKey = s.RowKey.Of(c => row.Raw[node.Headers.IndexOf(c)].Trim());
            }
            if (row.OwnKey != null)
            {
                string scoped = row.ParentKey + Sep + row.OwnKey;
                int keyHeader = s.Shape == SheetShape.Keyed ? anc : node.Headers.IndexOf(s.RowKey.Columns.First());
                if (firstRowOfKey.TryGetValue(scoped, out int first))
                    errors.Add($"{rows.Where(row, keyHeader)}: duplicate '{row.OwnKey}' (also on row {first}).");
                else
                    firstRowOfKey[scoped] = row.Number;
                rows.OwnKeys.Add(row.OwnKey);
            }
            row.PathKey = row.OwnKey == null || s.Shape == SheetShape.Single ? row.ParentKey
                : anc == 0 ? row.OwnKey : row.ParentKey + Sep + row.OwnKey;

            if (parent != null && !parent.PathKeys.Contains(row.ParentKey))
            {
                string names = string.Join(", ", node.AncestorHeaders.Select((a, i) => $"{a} '{row.Raw[i]}'"));
                errors.Add($"{rows.Where(row, anc - 1)}: no row of '{parent.Node.Spec.Name}' has {names}.");
            }

            rows.Rows.Add(row);
            rows.PathKeys.Add(row.PathKey);
            if (!rows.ByParent.TryGetValue(row.ParentKey, out List<ParsedRow> group))
                rows.ByParent[row.ParentKey] = group = new List<ParsedRow>();
            group.Add(row);
        }

        if (s.Shape == SheetShape.Single && rows.Rows.Count != 1)
            errors.Add($"{sheet}: this sheet holds exactly one row; it has {rows.Rows.Count}.");
        return rows;
    }

    private static void CheckRefs(List<Node> nodes, Dictionary<SheetSpec, SheetRows> data, List<string> errors)
    {
        foreach (SheetRows rows in data.Values)
        {
            Node node = rows.Node;
            int first = node.AncestorHeaders.Count + (node.Spec.Shape == SheetShape.Keyed ? 1 : 0);
            List<ColumnSpec> columns = node.ValueColumns;
            for (int j = 0; j < columns.Count; j++)
            {
                ColumnSpec col = columns[j];
                if (col.RefSheet == null)
                    continue;
                Node target = nodes.FirstOrDefault(n => n.Spec.Name == col.RefSheet);
                if (target == null || !data.TryGetValue(target.Spec, out SheetRows keys))
                    continue;
                foreach (ParsedRow row in rows.Rows)
                {
                    if (row.Raw[first + j].Trim().Length == 0)
                        continue;
                    ContentNode value = ParseCell(col, row.Raw[first + j], out string error);
                    if (error != null)
                        continue;
                    foreach (string v in ItemsOf(col, value).Where(v => !keys.OwnKeys.Contains(v)))
                        errors.Add($"{rows.Where(row, first + j)}: '{v}' is not a row of sheet '{col.RefSheet}'.");
                }
            }
        }
    }

    private static ContentNode BuildRow(Dictionary<SheetSpec, Node> nodes, Dictionary<SheetSpec, SheetRows> data, Node node, ParsedRow row)
    {
        var obj = ContentNode.NewObject();
        int j = 0;
        foreach (SheetField f in node.Spec.Fields)
        {
            if (f is ColumnSpec)
            {
                ContentNode v = row.Values[j++];
                if (v != null)
                    SetPath(obj, f.Path, v);
                continue;
            }

            var child = (SheetSpec)f;
            Node cn = nodes[child];
            List<ParsedRow> kids = data[child].ByParent.TryGetValue(row.PathKey, out List<ParsedRow> g) ? g : new List<ParsedRow>();
            switch (child.Shape)
            {
                case SheetShape.Single:
                    SetPath(obj, child.Path, BuildRow(nodes, data, cn, kids[0]));
                    break;
                case SheetShape.Rows:
                case SheetShape.Values:
                    if (kids.Count == 0 && child.OmitWhenEmpty)
                        break;
                    var arr = ContentNode.NewArray();
                    foreach (ParsedRow k in kids)
                        arr.Add(child.Shape == SheetShape.Values ? k.Values[0] ?? DefaultNode((ColumnSpec)child.Fields[0]) : BuildRow(nodes, data, cn, k));
                    SetPath(obj, child.Path, arr);
                    break;
                case SheetShape.Keyed:
                    bool inline = child.Path == string.Empty;
                    if (!inline && kids.Count == 0 && child.OmitWhenEmpty)
                        break;
                    ContentNode target = inline ? obj : ContentNode.NewObject();
                    foreach (ParsedRow k in kids)
                        target.Add(k.OwnKey, BuildRow(nodes, data, cn, k));
                    if (!inline)
                        SetPath(obj, child.Path, target);
                    break;
            }
        }
        return obj;
    }

    // =====================================================================
    // The README sheet
    // =====================================================================

    /// <summary>The README sheet: how the workbook works, then a row per sheet (its shape, JSON place and key) and a row per column (type, what a blank means, rules, note).</summary>
    private static RowTable Readme(SheetSpec map)
    {
        var t = new RowTable(ReadmeSheet, new[] { "sheet", "column", "type", "blank means", "rules", "note" });
        foreach (string line in Guide)
            t.Add(new[] { string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, line });
        List<Node> nodes = Flatten(map);
        foreach (Node n in nodes)
        {
            SheetSpec s = n.Spec;
            t.Add(new[] { s.Name, string.Empty, s.Shape.ToString(), string.Empty, DescribeSheet(n), s.Doc ?? string.Empty });
            for (int i = 0; i < n.AncestorHeaders.Count; i++)
                t.Add(new[] { s.Name, n.AncestorHeaders[i], "row name", "(required)", $"names the row of '{n.KeyedAncestors[i].Spec.Name}' this row belongs to", string.Empty });
            if (s.Shape == SheetShape.Keyed)
                t.Add(new[] { s.Name, s.KeyColumn.Header, "text", "(required)", Rules(s.KeyColumn, "the member's name, unique per parent row"), s.KeyColumn.Doc ?? string.Empty });
            foreach (ColumnSpec col in n.ValueColumns)
            {
                bool required = col.IsRequired || s.Shape == SheetShape.Values;
                string blank = required ? "(required)" : DefaultDisplay(col) + (col.OmitDefault ? " (left out of the JSON)" : string.Empty);
                string key = s.RowKey != null && s.RowKey.Columns.Contains(col.Header) ? "part of the row's name" : null;
                t.Add(new[] { s.Name, col.Header, TypeName(col.Type), blank, Rules(col, key), col.Doc ?? string.Empty });
            }
        }
        return t;
    }

    /// <summary>The README's opening lines: how the workbook works.</summary>
    private static readonly string[] Guide =
    {
        "Each sheet below is one table of Assets/Data/World/world_source.json: one row per record, one column per field. This README is skipped by the import.",
        "Tools > TimeDesk > Import Content Spreadsheet reads ContentSheets/TimeDesk_Content.xlsx, writes world_source.json in its usual layout, then runs Generate World (Import Content Sheets (CSV) reads ContentSheets/csv/ instead: one UTF-8 CSV per sheet, named after it).",
        "A blank cell takes the column's default ('blank means'). Lists go in one cell separated by |. A child sheet's first columns name its parent row (a place is {country}_{era}).",
        "An unknown or missing column, a missing sheet, a bad value, a duplicate row name or a name no row has stops the import, naming the sheet, row and column; nothing is written.",
        "Start from Tools > TimeDesk > Export Content Spreadsheet (today's content); keep sheet and column names as they are."
    };

    private static string DescribeSheet(Node n)
    {
        SheetSpec s = n.Spec;
        string place = n.JsonPath;
        string text;
        switch (s.Shape)
        {
            case SheetShape.Single:
                text = n.Parent == null ? "one row: the document's own values" : $"one row: the object {place}";
                break;
            case SheetShape.Keyed:
                text = $"one row per member of {place}, named in '{s.KeyColumn.Header}'";
                break;
            case SheetShape.Values:
                text = $"one row per item of the list {place}, in order";
                break;
            default:
                text = $"one row per item of the list {place}, in order";
                if (s.RowKey != null)
                    text += $"; a row's name is {s.RowKey.Template}, unique{(n.AncestorHeaders.Count > 0 ? " per parent row" : string.Empty)}";
                break;
        }
        if (n.AncestorHeaders.Count > 0)
            text += $"; the first column{(n.AncestorHeaders.Count > 1 ? "s name" : " names")} the parent row";
        if (s.OmitWhenEmpty)
            text += "; left out of the JSON when it has no rows";
        return text;
    }

    private static string Rules(ColumnSpec col, string extra)
    {
        var parts = new List<string>();
        if (extra != null)
            parts.Add(extra);
        if (col.Allowed != null)
            parts.Add("one of: " + string.Join(", ", col.Allowed));
        if (col.RefSheet != null)
            parts.Add((col.Type == CellType.TextList ? "each item names a row of '" : "names a row of '") + col.RefSheet + "'");
        if (col.Type == CellType.TextList || col.Type == CellType.NumberList)
            parts.Add("separate items with |");
        return string.Join("; ", parts);
    }

    private static string DefaultDisplay(ColumnSpec col)
    {
        string d = TypeDefault(col.Type);
        if (d.Length > 0)
            return d;
        return col.Type == CellType.Text ? "empty text" : "no items";
    }

    private static string TypeName(CellType type)
    {
        switch (type)
        {
            case CellType.Int: return "whole number";
            case CellType.Float: return "decimal";
            case CellType.Number: return "number";
            case CellType.Bool: return "true/false";
            case CellType.TextList: return "text list";
            case CellType.NumberList: return "number list";
            default: return "text";
        }
    }

    // =====================================================================
    // Cells
    // =====================================================================

    private static string TypeDefault(CellType type)
    {
        switch (type)
        {
            case CellType.Int:
            case CellType.Float:
            case CellType.Number:
                return "0";
            case CellType.Bool:
                return "false";
            default:
                return string.Empty;
        }
    }

    private static ContentNode DefaultNode(ColumnSpec col) => ParseCell(col, string.Empty, out _);

    /// <summary>A cell's JSON value (a blank cell is the column's default); <paramref name="error"/> says what is wrong with a bad cell.</summary>
    private static ContentNode ParseCell(ColumnSpec col, string cell, out string error)
    {
        error = null;
        string text = cell ?? string.Empty;
        bool blank = col.Type == CellType.Text ? text.Length == 0 : text.Trim().Length == 0;
        if (blank)
            text = TypeDefault(col.Type);
        string t = text.Trim();

        switch (col.Type)
        {
            case CellType.Text:
                return ContentNode.FromString(text.Replace("\r\n", "\n"));
            case CellType.Int:
                if (WholeNumber.IsMatch(t) && long.TryParse(t, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long whole))
                    return ContentNode.FromNumber(whole.ToString(CultureInfo.InvariantCulture));
                error = $"'{t}' is not a whole number.";
                return null;
            case CellType.Float:
                return Decimal(t, out error);
            case CellType.Number:
                return NumberAsTyped(t, out error);
            case CellType.Bool:
                if (string.Equals(t, "true", StringComparison.OrdinalIgnoreCase))
                    return ContentNode.FromBool(true);
                if (string.Equals(t, "false", StringComparison.OrdinalIgnoreCase))
                    return ContentNode.FromBool(false);
                error = $"'{t}' is not true or false.";
                return null;
            default:
                var list = ContentNode.NewArray();
                if (t.Length == 0)
                    return list;
                foreach (string raw in text.Split('|'))
                {
                    string item = raw.Trim();
                    if (item.Length == 0)
                    {
                        error = $"'{text}' has an empty item (two '|' in a row, or one at an end).";
                        return null;
                    }
                    if (col.Type == CellType.TextList)
                        list.Add(ContentNode.FromString(item));
                    else
                    {
                        ContentNode n = NumberAsTyped(item, out error);
                        if (error != null)
                        {
                            error = $"'{item}' in '{text}' is not a number.";
                            return null;
                        }
                        list.Add(n);
                    }
                }
                return list;
        }
    }

    private static ContentNode Decimal(string t, out string error)
    {
        error = null;
        if (double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) && !double.IsNaN(d) && !double.IsInfinity(d)
            && t.All(ch => (ch >= '0' && ch <= '9') || ch == '-' || ch == '+' || ch == '.' || ch == 'e' || ch == 'E'))
            return ContentNode.FromNumber(ContentJson.FloatRepr(d));
        error = $"'{t}' is not a number (use a dot for decimals).";
        return null;
    }

    private static ContentNode NumberAsTyped(string t, out string error)
    {
        error = null;
        if (WholeNumber.IsMatch(t) && long.TryParse(t, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long whole))
            return ContentNode.FromNumber(whole.ToString(CultureInfo.InvariantCulture));
        return Decimal(t, out error);
    }

    private static IEnumerable<string> ItemsOf(ColumnSpec col, ContentNode value) =>
        value.Kind == ContentNodeKind.Array ? value.Items.Select(i => i.Text) : new[] { value.Text };

    private static string Describe(ContentNode v)
    {
        switch (v.Kind)
        {
            case ContentNodeKind.Object: return "an object";
            case ContentNodeKind.Array: return "a list";
            case ContentNodeKind.String: return $"the text \"{(v.Text.Length > 40 ? v.Text.Substring(0, 40) + "..." : v.Text)}\"";
            case ContentNodeKind.Null: return "null";
            default: return v.Text;
        }
    }

    // =====================================================================
    // Plumbing
    // =====================================================================

    /// <summary>One sheet of the map with what its place in the tree implies.</summary>
    private sealed class Node
    {
        public SheetSpec Spec;
        public Node Parent;
        public List<Node> KeyedAncestors;
        public List<string> AncestorHeaders;
        public List<string> Headers;
        public CellKind[] Kinds;
        public List<ColumnSpec> ValueColumns;
        public string JsonPath;
        public bool TopLevel;
    }

    private static List<Node> Flatten(SheetSpec root)
    {
        var list = new List<Node>();
        Walk(root, null, "$", list);
        return list;
    }

    private static void Walk(SheetSpec s, Node parent, string parentBase, List<Node> list)
    {
        var n = new Node
        {
            Spec = s,
            Parent = parent,
            KeyedAncestors = parent == null ? new List<Node>() : parent.KeyedAncestors.Concat(parent.Spec.Shape != SheetShape.Single && parent.Spec.KeyHeader != null ? new[] { parent } : Array.Empty<Node>()).ToList(),
            ValueColumns = s.Columns.ToList(),
            TopLevel = parent != null && Ancestry(parent).All(a => a.Spec.Shape == SheetShape.Single)
        };
        n.AncestorHeaders = n.KeyedAncestors.Select(a => a.Spec.KeyHeader).ToList();
        var headers = new List<string>(n.AncestorHeaders);
        var kinds = new List<CellKind>(n.AncestorHeaders.Select(_ => CellKind.Text));
        if (s.Shape == SheetShape.Keyed && s.KeyColumn != null)
        {
            headers.Add(s.KeyColumn.Header);
            kinds.Add(CellKind.Text);
        }
        foreach (ColumnSpec c in n.ValueColumns)
        {
            headers.Add(c.Header);
            kinds.Add(c.Type == CellType.Bool ? CellKind.Bool : c.Type == CellType.Int || c.Type == CellType.Float || c.Type == CellType.Number ? CellKind.Number : CellKind.Text);
        }
        n.Headers = headers;
        n.Kinds = kinds.ToArray();

        string here = s.Path.Length == 0 ? parentBase : parentBase + "." + s.Path;
        n.JsonPath = here;
        list.Add(n);

        string childBase;
        switch (s.Shape)
        {
            case SheetShape.Rows: childBase = here + "[]"; break;
            case SheetShape.Keyed: childBase = here + ".<" + (s.KeyColumn?.Header ?? "key") + ">"; break;
            default: childBase = here; break;
        }
        foreach (SheetSpec child in s.Children)
            Walk(child, n, childBase, list);
    }

    private static IEnumerable<Node> Ancestry(Node n)
    {
        for (Node a = n; a != null; a = a.Parent)
            yield return a;
    }

    private static IEnumerable<ColumnSpec> AllColumns(SheetSpec s) =>
        s.Shape == SheetShape.Keyed && s.KeyColumn != null ? new[] { s.KeyColumn }.Concat(s.Columns) : s.Columns;

    /// <summary>The first path segments a sheet's fields use, except one field.</summary>
    private static HashSet<string> FirstSegments(SheetSpec s, SheetSpec except) =>
        new HashSet<string>(s.Fields.Where(f => !ReferenceEquals(f, except)).Select(f => f.Path.Split('.')[0]));

    private static ContentNode Lookup(ContentNode obj, string path)
    {
        ContentNode at = obj;
        foreach (string seg in path.Split('.'))
        {
            if (at == null || at.Kind != ContentNodeKind.Object)
                return null;
            at = at.Get(seg);
        }
        return at;
    }

    private static void SetPath(ContentNode obj, string path, ContentNode value)
    {
        string[] segs = path.Split('.');
        ContentNode at = obj;
        for (int i = 0; i < segs.Length - 1; i++)
        {
            ContentNode next = at.Get(segs[i]);
            if (next == null)
                at.Add(segs[i], next = ContentNode.NewObject());
            at = next;
        }
        at.Add(segs[segs.Length - 1], value);
    }
}
