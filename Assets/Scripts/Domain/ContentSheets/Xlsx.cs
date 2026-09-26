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
/// booleans as "true"/"false"). Excel's "_xHHHH_" escapes are written and read.
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
                book.Append($"<sheet name=\"{Attr(sheets[i].Name)}\" sheetId=\"{i + 1}\" r:id=\"rId{i + 1}\"/>");
                rels.Append($"<Relationship Id=\"rId{i + 1}\" Type=\"{RelNs}/worksheet\" Target=\"worksheets/sheet{i + 1}.xml\"/>");
            }
            book.Append("</sheets></workbook>");
            rels.Append($"<Relationship Id=\"rId{sheets.Count + 1}\" Type=\"{RelNs}/styles\" Target=\"styles.xml\"/></Relationships>");
            Put(zip, "xl/workbook.xml", book.ToString());
            Put(zip, "xl/_rels/workbook.xml.rels", rels.ToString());
            Put(zip, "xl/styles.xml", Styles);

            for (int i = 0; i < sheets.Count; i++)
                Put(zip, $"xl/worksheets/sheet{i + 1}.xml", Sheet(sheets[i]));
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
