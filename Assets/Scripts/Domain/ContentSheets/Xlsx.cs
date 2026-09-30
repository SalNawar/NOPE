using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;

/// <summary>
/// Excel workbooks (.xlsx) without a third-party package: an .xlsx is a zip of XML
/// parts, read and written here with System.IO.Compression and System.Xml. Writes one
/// worksheet per table (a frozen, bold header row; text columns formatted as text so
/// the spreadsheet never turns an id or a date-like text into a number); reads every
/// worksheet's cells as text (shared, inline and formula strings, numbers as written,
/// booleans as "true"/"false"). Excel's "_xHHHH_" escapes are written and read. A table
/// with a <see cref="SheetLook"/> is drawn styled (Arial; editable cells pale yellow and
/// unlocked, the rest locked; banners, widths, hidden columns and sheets, frozen columns,
/// a header filter, password-less protection, drop-down lists): the narrative workbook.
/// A workbook of plain tables is written exactly as before.
/// </summary>
public static class Xlsx
{
    private const string Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private const string RelNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private const string PackageRelNs = "http://schemas.openxmlformats.org/package/2006/relationships";

    /// <summary>The fixed timestamp of every zip entry, so the same tables give the same bytes.</summary>
    private static readonly DateTimeOffset Stamp = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>Writes the tables as one worksheet each, in order.</summary>
    public static byte[] Write(IList<RowTable> sheets)
    {
        bool styled = sheets.Any(t => t.Look != null);
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            var types = new StringBuilder();
            types.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">");
            types.Append("<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>");
            types.Append("<Default Extension=\"xml\" ContentType=\"application/xml\"/>");
            types.Append("<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>");
            types.Append("<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>");
            for (int i = 0; i < sheets.Count; i++)
                types.Append($"<Override PartName=\"/xl/worksheets/sheet{i + 1}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>");
            types.Append("</Types>");
            Put(zip, "[Content_Types].xml", types.ToString());

            Put(zip, "_rels/.rels",
                $"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n<Relationships xmlns=\"{PackageRelNs}\"><Relationship Id=\"rId1\" Type=\"{RelNs}/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");

            var book = new StringBuilder();
            book.Append($"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n<workbook xmlns=\"{Main}\" xmlns:r=\"{RelNs}\"><sheets>");
            var rels = new StringBuilder();
            rels.Append($"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n<Relationships xmlns=\"{PackageRelNs}\">");
            for (int i = 0; i < sheets.Count; i++)
            {
                string state = sheets[i].Look != null && sheets[i].Look.Hidden ? " state=\"hidden\"" : string.Empty;
                book.Append($"<sheet name=\"{Attr(sheets[i].Name)}\" sheetId=\"{i + 1}\"{state} r:id=\"rId{i + 1}\"/>");
                rels.Append($"<Relationship Id=\"rId{i + 1}\" Type=\"{RelNs}/worksheet\" Target=\"worksheets/sheet{i + 1}.xml\"/>");
            }
            book.Append("</sheets>");
            string filters = string.Concat(sheets.Select((t, i) => t.Look != null && t.Look.Filter && t.Headers.Count > 0
                ? $"<definedName name=\"_xlnm._FilterDatabase\" localSheetId=\"{i}\" hidden=\"1\">{Attr(FilterRange(t, true))}</definedName>"
                : string.Empty));
            if (filters.Length > 0)
                book.Append("<definedNames>").Append(filters).Append("</definedNames>");
            book.Append("</workbook>");
            rels.Append($"<Relationship Id=\"rId{sheets.Count + 1}\" Type=\"{RelNs}/styles\" Target=\"styles.xml\"/></Relationships>");
            Put(zip, "xl/workbook.xml", book.ToString());
            Put(zip, "xl/_rels/workbook.xml.rels", rels.ToString());
            Put(zip, "xl/styles.xml", styled ? StyledStyles : Styles);

            for (int i = 0; i < sheets.Count; i++)
                Put(zip, $"xl/worksheets/sheet{i + 1}.xml", sheets[i].Look != null ? StyledSheet(sheets[i]) : Sheet(sheets[i]));
        }
        return stream.ToArray();
    }

    /// <summary>Reads every worksheet, in workbook order; a file that is not a workbook is one error.</summary>
    public static List<RowTable> Read(byte[] data, List<string> errors)
    {
        var tables = new List<RowTable>();
        try
        {
            using var zip = new ZipArchive(new MemoryStream(data), ZipArchiveMode.Read);
            XDocument book = Load(zip, "xl/workbook.xml") ?? throw new InvalidDataException("it has no xl/workbook.xml");
            XDocument rels = Load(zip, "xl/_rels/workbook.xml.rels");
            var targets = rels?.Root?.Elements(XName.Get("Relationship", PackageRelNs))
                .ToDictionary(r => (string)r.Attribute("Id"), r => (string)r.Attribute("Target")) ?? new Dictionary<string, string>();
            List<string> shared = SharedStrings(Load(zip, "xl/sharedStrings.xml"));

            foreach (XElement sheet in book.Root.Descendants(XName.Get("sheet", Main)))
            {
                string name = (string)sheet.Attribute("name");
                string id = (string)sheet.Attribute(XName.Get("id", RelNs));
                if (id == null || !targets.TryGetValue(id, out string target))
                {
                    errors.Add($"{name}: the workbook does not say where this sheet is.");
                    continue;
                }
                string part = target.StartsWith("/") ? target.Substring(1) : "xl/" + target;
                XDocument ws = Load(zip, part);
                if (ws == null)
                {
                    errors.Add($"{name}: the workbook has no part '{part}'.");
                    continue;
                }
                tables.Add(ReadSheet(name, ws, shared, errors));
            }
        }
        catch (Exception e) when (e is InvalidDataException || e is System.Xml.XmlException || e is IOException)
        {
            errors.Add($"Not a readable .xlsx workbook: {e.Message}");
        }
        return tables;
    }

    // ---- writing ----

    /// <summary>Cell styles: 0 general, 1 text-formatted, 2 the bold header on a light fill (text-formatted).</summary>
    private const string Styles =
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n<styleSheet xmlns=\"" + Main + "\">" +
        "<fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Calibri\"/><family val=\"2\"/></font><font><b/><sz val=\"11\"/><name val=\"Calibri\"/><family val=\"2\"/></font></fonts>" +
        "<fills count=\"3\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill>" +
        "<fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFDCE3EC\"/><bgColor indexed=\"64\"/></patternFill></fill></fills>" +
        "<borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders>" +
        "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>" +
        "<cellXfs count=\"3\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/>" +
        "<xf numFmtId=\"49\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyNumberFormat=\"1\"/>" +
        "<xf numFmtId=\"49\" fontId=\"1\" fillId=\"2\" borderId=\"0\" xfId=\"0\" applyNumberFormat=\"1\" applyFont=\"1\" applyFill=\"1\"/></cellXfs>" +
        "<cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles></styleSheet>";

    /// <summary>
    /// The styled workbook's cell styles: 0-2 as <see cref="Styles"/> (in Arial), then
    /// 3 editable (pale yellow, unlocked), 4 locked (grey text), 5 not applicable (shaded),
    /// 6 section banner, 7 an editable column's header (amber), 8 a locked column's header
    /// (slate, white text), 9 a locked number or true/false. Data cells wrap at the top.
    /// </summary>
    private static readonly string StyledStyles =
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n<styleSheet xmlns=\"" + Main + "\">" +
        "<fonts count=\"5\">" + Font(false, null) + Font(true, null) + Font(false, "FF595959") + Font(true, "FF1F3864") + Font(true, "FFFFFFFF") + "</fonts>" +
        "<fills count=\"8\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill>" +
        Fill("FFDCE3EC") + Fill("FFFFF2CC") + Fill("FFEDEDED") + Fill("FFDDEBF7") + Fill("FFFFC000") + Fill("FF44546A") + "</fills>" +
        "<borders count=\"2\"><border><left/><right/><top/><bottom/><diagonal/></border><border>" + Side("left") + Side("right") + Side("top") + Side("bottom") + "<diagonal/></border></borders>" +
        "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>" +
        "<cellXfs count=\"10\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/>" +
        "<xf numFmtId=\"49\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyNumberFormat=\"1\"/>" +
        "<xf numFmtId=\"49\" fontId=\"1\" fillId=\"2\" borderId=\"0\" xfId=\"0\" applyNumberFormat=\"1\" applyFont=\"1\" applyFill=\"1\"/>" +
        Xf(49, 0, 3, false) + Xf(49, 2, 0, true) + Xf(49, 2, 4, true) + Xf(49, 3, 5, true) + Xf(49, 1, 6, true) + Xf(49, 4, 7, true) + Xf(0, 2, 0, true) +
        "</cellXfs><cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles></styleSheet>";

    private static string Font(bool bold, string argb) =>
        "<font>" + (bold ? "<b/>" : string.Empty) + "<sz val=\"10\"/>" + (argb != null ? $"<color rgb=\"{argb}\"/>" : string.Empty) + "<name val=\"Arial\"/><family val=\"2\"/></font>";

    private static string Fill(string argb) => $"<fill><patternFill patternType=\"solid\"><fgColor rgb=\"{argb}\"/><bgColor indexed=\"64\"/></patternFill></fill>";

    private static string Side(string side) => $"<{side} style=\"thin\"><color rgb=\"FFD9D9D9\"/></{side}>";

    private static string Xf(int format, int font, int fill, bool locked) =>
        $"<xf numFmtId=\"{format}\" fontId=\"{font}\" fillId=\"{fill}\" borderId=\"1\" xfId=\"0\" applyNumberFormat=\"1\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\" applyAlignment=\"1\" applyProtection=\"1\">" +
        $"<alignment vertical=\"top\" wrapText=\"1\"/><protection locked=\"{(locked ? 1 : 0)}\"/></xf>";

    /// <summary>The style index of a look (<see cref="StyledStyles"/>).</summary>
    private static int StyleOf(CellLook look)
    {
        switch (look)
        {
            case CellLook.Editable: return 3;
            case CellLook.NotApplicable: return 5;
            case CellLook.Section: return 6;
            default: return 4;
        }
    }

    /// <summary>The header row and the data as a range ("A1:K40"; absolute and sheet-qualified for a defined name).</summary>
    private static string FilterRange(RowTable t, bool qualified)
    {
        string last = RowTable.ColumnLetter(Math.Max(0, t.Headers.Count - 1));
        int rows = t.Rows.Count + 1;
        return qualified ? $"'{t.Name.Replace("'", "''")}'!$A$1:${last}${rows}" : $"A1:{last}{rows}";
    }

    /// <summary>A sheet drawn with its <see cref="SheetLook"/>.</summary>
    private static string StyledSheet(RowTable t)
    {
        SheetLook look = t.Look;
        int width = t.Headers.Count;
        CellKind Kind(int col) => t.Kinds != null && col < t.Kinds.Length ? t.Kinds[col] : CellKind.Text;
        CellLook ColumnLook(int col) => col < look.Columns.Length ? look.Columns[col] : CellLook.Locked;

        var sb = new StringBuilder();
        sb.Append($"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n<worksheet xmlns=\"{Main}\">");
        if (!string.IsNullOrEmpty(look.TabColor))
            sb.Append($"<sheetPr><tabColor rgb=\"{Attr(look.TabColor)}\"/></sheetPr>");
        int freeze = Math.Max(0, Math.Min(look.FreezeColumns, width));
        sb.Append(freeze > 0
            ? $"<sheetViews><sheetView workbookViewId=\"0\"><pane xSplit=\"{freeze}\" ySplit=\"1\" topLeftCell=\"{RowTable.ColumnLetter(freeze)}2\" activePane=\"bottomRight\" state=\"frozen\"/></sheetView></sheetViews>"
            : "<sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews>");
        sb.Append("<sheetFormatPr defaultRowHeight=\"13\"/>");
        if (width > 0)
        {
            sb.Append("<cols>");
            for (int c = 0; c < width; c++)
            {
                int w = c < look.Widths.Length && look.Widths[c] > 0
                    ? look.Widths[c]
                    : Math.Max(8, Math.Min(60, new[] { t.Headers[c] }.Concat(t.Rows.Select(r => c < r.Length ? r[c] : null)).Max(s => (s ?? string.Empty).Split('\n').Max(line => line.Length)) + 2));
                string hidden = c < look.HiddenColumns.Length && look.HiddenColumns[c] ? " hidden=\"1\"" : string.Empty;
                sb.Append($"<col min=\"{c + 1}\" max=\"{c + 1}\" width=\"{w}\" style=\"{StyleOf(ColumnLook(c))}\"{hidden} customWidth=\"1\"/>");
            }
            sb.Append("</cols>");
        }

        sb.Append("<sheetData><row r=\"1\">");
        for (int c = 0; c < width; c++)
            sb.Append($"<c r=\"{RowTable.ColumnLetter(c)}1\" s=\"{(ColumnLook(c) == CellLook.Editable ? 7 : 8)}\" t=\"inlineStr\"><is><t xml:space=\"preserve\">{Text(t.Headers[c] ?? string.Empty)}</t></is></c>");
        sb.Append("</row>");
        for (int r = 0; r < t.Rows.Count; r++)
        {
            int number = r + 2;
            sb.Append($"<row r=\"{number}\">");
            string[] cells = t.Rows[r];
            for (int c = 0; c < width; c++)
            {
                string v = c < cells.Length ? cells[c] ?? string.Empty : string.Empty;
                CellLook cellLook = look.LookOf(r, c);
                string at = RowTable.ColumnLetter(c) + number.ToString(CultureInfo.InvariantCulture);
                int style = StyleOf(cellLook);
                if (v.Length == 0)
                {
                    if (cellLook != ColumnLook(c))
                        sb.Append($"<c r=\"{at}\" s=\"{style}\"/>");
                    continue;
                }
                CellKind k = Kind(c);
                int valueStyle = cellLook == CellLook.Locked ? 9 : style;
                if (k == CellKind.Number && IsNumberLiteral(v))
                    sb.Append($"<c r=\"{at}\" s=\"{valueStyle}\"><v>{v}</v></c>");
                else if (k == CellKind.Bool && (v == "true" || v == "false"))
                    sb.Append($"<c r=\"{at}\" s=\"{valueStyle}\" t=\"b\"><v>{(v == "true" ? 1 : 0)}</v></c>");
                else
                    sb.Append($"<c r=\"{at}\" s=\"{style}\" t=\"inlineStr\"><is><t xml:space=\"preserve\">{Text(v)}</t></is></c>");
            }
            sb.Append("</row>");
        }
        sb.Append("</sheetData>");

        if (look.Protect)
            sb.Append("<sheetProtection sheet=\"1\" objects=\"1\" scenarios=\"1\" formatCells=\"0\" formatColumns=\"0\" formatRows=\"0\" insertRows=\"0\" deleteRows=\"0\" sort=\"0\" autoFilter=\"0\"/>");
        if (look.Filter && width > 0)
            sb.Append($"<autoFilter ref=\"{FilterRange(t, false)}\"/>");
        List<ListRule> lists = look.Lists.Where(l => l.Spans.Count > 0).ToList();
        if (lists.Count > 0)
        {
            sb.Append($"<dataValidations count=\"{lists.Count}\">");
            foreach (ListRule rule in lists)
                sb.Append($"<dataValidation type=\"list\" allowBlank=\"1\" showInputMessage=\"1\" showErrorMessage=\"1\" sqref=\"{Sqref(rule)}\"><formula1>{Text(rule.Source)}</formula1></dataValidation>");
            sb.Append("</dataValidations>");
        }
        sb.Append("</worksheet>");
        return sb.ToString();
    }

    /// <summary>A list rule's cells as a space-separated reference list, each column's adjacent rows merged into one range.</summary>
    private static string Sqref(ListRule rule)
    {
        var parts = new List<string>();
        foreach (IGrouping<int, (int first, int last, int column)> column in rule.Spans.GroupBy(s => s.column).OrderBy(g => g.Key))
        {
            string letter = RowTable.ColumnLetter(column.Key);
            int start = -1, end = -2;
            foreach ((int first, int last, int _) in column.OrderBy(s => s.first))
            {
                if (start >= 0 && first <= end + 1)
                {
                    end = Math.Max(end, last);
                    continue;
                }
                if (start >= 0)
                    parts.Add(CellRange(letter, start, end));
                start = first;
                end = last;
            }
            if (start >= 0)
                parts.Add(CellRange(letter, start, end));
        }
        return string.Join(" ", parts);
    }

    private static string CellRange(string letter, int first, int last) =>
        first == last ? $"{letter}{first + 2}" : $"{letter}{first + 2}:{letter}{last + 2}";


    private static string Sheet(RowTable t)
    {
        int width = t.Headers.Count;
        CellKind Kind(int col) => t.Kinds != null && col < t.Kinds.Length ? t.Kinds[col] : CellKind.Text;

        var sb = new StringBuilder();
        sb.Append($"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n<worksheet xmlns=\"{Main}\">");
        sb.Append("<sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews>");
        sb.Append("<sheetFormatPr defaultRowHeight=\"15\"/>");
        if (width > 0)
        {
            sb.Append("<cols>");
            for (int c = 0; c < width; c++)
            {
                int longest = new[] { t.Headers[c] }.Concat(t.Rows.Select(r => r[c])).Max(s => (s ?? string.Empty).Split('\n').Max(line => line.Length));
                int w = Math.Max(8, Math.Min(60, longest + 2));
                string style = Kind(c) == CellKind.Text ? " style=\"1\"" : string.Empty;
                sb.Append($"<col min=\"{c + 1}\" max=\"{c + 1}\" width=\"{w}\"{style} customWidth=\"1\"/>");
            }
            sb.Append("</cols>");
        }
        sb.Append("<sheetData>");
        WriteRow(sb, 1, t.Headers, _ => CellKind.Text, 2);
        for (int r = 0; r < t.Rows.Count; r++)
            WriteRow(sb, r + 2, t.Rows[r], Kind, 1);
        sb.Append("</sheetData></worksheet>");
        return sb.ToString();
    }

    private static void WriteRow(StringBuilder sb, int number, IList<string> cells, Func<int, CellKind> kind, int textStyle)
    {
        sb.Append($"<row r=\"{number}\">");
        for (int c = 0; c < cells.Count; c++)
        {
            string v = cells[c] ?? string.Empty;
            if (v.Length == 0)
                continue;
            string at = RowTable.ColumnLetter(c) + number.ToString(CultureInfo.InvariantCulture);
            CellKind k = kind(c);
            if (k == CellKind.Number && IsNumberLiteral(v))
                sb.Append($"<c r=\"{at}\"><v>{v}</v></c>");
            else if (k == CellKind.Bool && (v == "true" || v == "false"))
                sb.Append($"<c r=\"{at}\" t=\"b\"><v>{(v == "true" ? 1 : 0)}</v></c>");
            else
                sb.Append($"<c r=\"{at}\" s=\"{textStyle}\" t=\"inlineStr\"><is><t xml:space=\"preserve\">{Text(v)}</t></is></c>");
        }
        sb.Append("</row>");
    }

    /// <summary>Text for an XML text node: markup escaped; characters XML cannot carry (and "\r", which XML reads as "\n") as Excel's "_xHHHH_"; a literal "_xHHHH_" keeps its underscore as "_x005F_".</summary>
    private static string Text(string s)
    {
        var sb = new StringBuilder(s.Length);
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c == '_' && IsEscapeAt(s, i))
                sb.Append("_x005F_");
            else if (c == '&')
                sb.Append("&amp;");
            else if (c == '<')
                sb.Append("&lt;");
            else if (c == '>')
                sb.Append("&gt;");
            else if ((c < 0x20 && c != '\t' && c != '\n') || c == '￾' || c == '￿'
                     || (char.IsHighSurrogate(c) && !(i + 1 < s.Length && char.IsLowSurrogate(s[i + 1])))
                     || (char.IsLowSurrogate(c) && !(i > 0 && char.IsHighSurrogate(s[i - 1]))))
                sb.Append("_x").Append(((int)c).ToString("X4", CultureInfo.InvariantCulture)).Append('_');
            else
                sb.Append(c);
        }
        return sb.ToString();
    }

    private static string Attr(string s) => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    /// <summary>A plain decimal literal ("12", "-0.5", "1e-05"): what a number cell may hold.</summary>
    private static bool IsNumberLiteral(string v) =>
        v.All(ch => (ch >= '0' && ch <= '9') || ch == '-' || ch == '+' || ch == '.' || ch == 'e' || ch == 'E')
        && double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out _);

    private static bool IsEscapeAt(string s, int i)
    {
        if (i + 7 > s.Length || s[i] != '_' || s[i + 1] != 'x' || s[i + 6] != '_')
            return false;
        for (int k = i + 2; k < i + 6; k++)
            if (!Uri.IsHexDigit(s[k]))
                return false;
        return true;
    }

    private static string Unescape(string s)
    {
        if (s.IndexOf("_x", StringComparison.Ordinal) < 0)
            return s;
        var sb = new StringBuilder(s.Length);
        for (int i = 0; i < s.Length; i++)
        {
            if (IsEscapeAt(s, i))
            {
                sb.Append((char)int.Parse(s.Substring(i + 2, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture));
                i += 6;
            }
            else
                sb.Append(s[i]);
        }
        return sb.ToString();
    }

    private static void Put(ZipArchive zip, string name, string content)
    {
        ZipArchiveEntry entry = zip.CreateEntry(name, CompressionLevel.Optimal);
        entry.LastWriteTime = Stamp;
        using Stream s = entry.Open();
        byte[] bytes = new UTF8Encoding(false).GetBytes(content);
        s.Write(bytes, 0, bytes.Length);
    }

    // ---- reading ----

    private static XDocument Load(ZipArchive zip, string part)
    {
        ZipArchiveEntry entry = zip.GetEntry(part) ?? zip.Entries.FirstOrDefault(e => string.Equals(e.FullName, part, StringComparison.OrdinalIgnoreCase));
        if (entry == null)
            return null;
        using Stream s = entry.Open();
        return XDocument.Load(s, LoadOptions.PreserveWhitespace);
    }

    private static List<string> SharedStrings(XDocument doc)
    {
        var list = new List<string>();
        if (doc?.Root == null)
            return list;
        foreach (XElement si in doc.Root.Elements(XName.Get("si", Main)))
            list.Add(Unescape(RunText(si)));
        return list;
    }

    /// <summary>A string item's text: its own &lt;t&gt; or its runs' &lt;t&gt;s (phonetic runs skipped).</summary>
    private static string RunText(XElement item)
    {
        XElement t = item.Element(XName.Get("t", Main));
        if (t != null)
            return t.Value;
        return string.Concat(item.Elements(XName.Get("r", Main)).Select(r => r.Element(XName.Get("t", Main))?.Value ?? string.Empty));
    }

    private static RowTable ReadSheet(string name, XDocument ws, List<string> shared, List<string> errors)
    {
        var records = new List<string[]>();
        var numbers = new List<int>();
        int lastRow = 0;
        XElement data = ws.Root?.Element(XName.Get("sheetData", Main));
        foreach (XElement row in data?.Elements(XName.Get("row", Main)) ?? Enumerable.Empty<XElement>())
        {
            int number = int.TryParse((string)row.Attribute("r"), NumberStyles.None, CultureInfo.InvariantCulture, out int n) ? n : lastRow + 1;
            lastRow = number;
            var cells = new List<string>();
            int next = 0;
            foreach (XElement c in row.Elements(XName.Get("c", Main)))
            {
                int col = ColumnOf((string)c.Attribute("r")) ?? next;
                next = col + 1;
                while (cells.Count <= col)
                    cells.Add(string.Empty);
                cells[col] = CellText(name, number, col, c, shared, errors);
            }
            records.Add(cells.ToArray());
            numbers.Add(number);
        }
        if (records.Count == 0 || numbers[0] != 1)
        {
            // The header must be row 1: rows above the first recorded row are blank.
            records.Insert(0, Array.Empty<string>());
            numbers.Insert(0, 1);
        }
        return RowTable.FromRecords(name, records, numbers);
    }

    private static string CellText(string sheet, int row, int col, XElement c, List<string> shared, List<string> errors)
    {
        string type = (string)c.Attribute("t") ?? "n";
        string v = c.Element(XName.Get("v", Main))?.Value;
        switch (type)
        {
            case "s":
                if (int.TryParse(v, NumberStyles.None, CultureInfo.InvariantCulture, out int i) && i < shared.Count)
                    return shared[i];
                errors.Add($"{sheet}: row {row}, column {RowTable.ColumnLetter(col)}: a shared string the workbook does not have.");
                return string.Empty;
            case "inlineStr":
                XElement inline = c.Element(XName.Get("is", Main));
                return inline == null ? string.Empty : Unescape(RunText(inline));
            case "b":
                return v == "1" ? "true" : "false";
            case "e":
                errors.Add($"{sheet}: row {row}, column {RowTable.ColumnLetter(col)}: the cell holds the spreadsheet error {v}.");
                return string.Empty;
            case "str":
                return Unescape(v ?? string.Empty);
            default:
                return v ?? string.Empty;
        }
    }

    /// <summary>The 0-based column of a cell reference like "AB12"; null when there is none.</summary>
    private static int? ColumnOf(string reference)
    {
        if (string.IsNullOrEmpty(reference))
            return null;
        int col = 0, k = 0;
        while (k < reference.Length && char.IsLetter(reference[k]))
            col = col * 26 + (char.ToUpperInvariant(reference[k++]) - 'A' + 1);
        return k == 0 ? (int?)null : col - 1;
    }
}
