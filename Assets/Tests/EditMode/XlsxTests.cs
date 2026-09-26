using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using NUnit.Framework;

/// <summary>The workbook layer: an .xlsx written and read with System.IO.Compression and XML only (no third-party package).</summary>
public class XlsxTests
{
    [Test]
    public void WriteThenRead_KeepsSheetsCellsAndOrder()
    {
        var people = new RowTable("people", new[] { "id", "age", "vip", "note" });
        people.Kinds = new[] { CellKind.Text, CellKind.Number, CellKind.Bool, CellKind.Text };
        people.Add(new[] { "ann", "34", "true", "line one\nline two" });
        people.Add(new[] { "bob", "1.0", "false", " lead & <tag> _x0041_ \r \U0001310F" });
        people.Add(new[] { "cy", "", "", "" });
        var places = new RowTable("places", new[] { "place" });
        places.Add(new[] { "egypt_ancient" });

        byte[] bytes = Xlsx.Write(new[] { people, places });
        var errors = new List<string>();
        List<RowTable> back = Xlsx.Read(bytes, errors);

        CollectionAssert.IsEmpty(errors);
        Assert.AreEqual(2, back.Count);
        Assert.AreEqual("people", back[0].Name);
        Assert.AreEqual("places", back[1].Name);
        Assert.IsTrue(RowTableTests.SameCells(people, back[0]), string.Join(" / ", back[0].Rows[1]));
        Assert.IsTrue(RowTableTests.SameCells(places, back[1]));
        CollectionAssert.AreEqual(new[] { 2, 3, 4 }, back[0].RowNumbers);
    }

    [Test]
    public void Write_IsDeterministic()
    {
        var t = new RowTable("s", new[] { "a" });
        t.Add(new[] { "x" });
        CollectionAssert.AreEqual(Xlsx.Write(new[] { t }), Xlsx.Write(new[] { t }));
    }

    [Test]
    public void Read_SharedStringsRichTextBooleansAndGaps()
    {
        const string ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        byte[] bytes = Zip(new Dictionary<string, string>
        {
            ["xl/workbook.xml"] = $"<workbook xmlns=\"{ns}\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Data\" sheetId=\"7\" r:id=\"rId3\"/></sheets></workbook>",
            ["xl/_rels/workbook.xml.rels"] = "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId3\" Type=\"x\" Target=\"/xl/worksheets/data.xml\"/></Relationships>",
            ["xl/sharedStrings.xml"] = $"<sst xmlns=\"{ns}\"><si><t>id</t></si><si><r><t>fl</t></r><r><rPr><b/></rPr><t>ag</t></r><rPh><t>x</t></rPh></si><si><t>a_x000D_b</t></si></sst>",
            ["xl/worksheets/data.xml"] = $"<worksheet xmlns=\"{ns}\"><sheetData>" +
                "<row r=\"1\"><c r=\"A1\" t=\"s\"><v>0</v></c><c r=\"C1\" t=\"s\"><v>1</v></c></row>" +
                "<row r=\"3\"><c r=\"A3\" t=\"s\"><v>2</v></c><c r=\"C3\" t=\"b\"><v>1</v></c></row>" +
                "<row><c t=\"str\"><f>A1</f><v>x</v></c><c><v>2.5</v></c><c t=\"b\"><v>0</v></c></row>" +
                "</sheetData></worksheet>"
        });

        var errors = new List<string>();
        List<RowTable> sheets = Xlsx.Read(bytes, errors);
        CollectionAssert.IsEmpty(errors);
        RowTable t = sheets[0];
        Assert.AreEqual("Data", t.Name);
        CollectionAssert.AreEqual(new[] { "id", "", "flag" }, t.Headers);
        CollectionAssert.AreEqual(new[] { "a\rb", "", "true" }, t.Rows[0]);
        CollectionAssert.AreEqual(new[] { "x", "2.5", "false" }, t.Rows[1]);
        CollectionAssert.AreEqual(new[] { 3, 4 }, t.RowNumbers);
    }

    [Test]
    public void Read_NotAWorkbook_IsAnError()
    {
        var errors = new List<string>();
        List<RowTable> sheets = Xlsx.Read(Encoding.UTF8.GetBytes("id,name\n"), errors);
        Assert.AreEqual(0, sheets.Count);
        Assert.AreEqual(1, errors.Count);
    }

    private static byte[] Zip(Dictionary<string, string> files)
    {
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, true))
            foreach (KeyValuePair<string, string> f in files)
                using (var w = new StreamWriter(zip.CreateEntry(f.Key).Open(), new UTF8Encoding(false)))
                    w.Write(f.Value);
        return stream.ToArray();
    }
}
