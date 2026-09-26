using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using NUnit.Framework;

/// <summary>
/// The real content map against today's world_source.json: the map is sound, every
/// key of the source is mapped, and export-then-import gives the source back byte for
/// byte (straight, through CSV and through an .xlsx workbook). A new section in the
/// source fails here until ContentSheetMap maps it.
/// </summary>
public class ContentSheetMapTests
{
    private const string SourcePath = "Assets/Data/World/world_source.json";

    /// <summary>Today's source with LF line endings (git may check it out with CRLF).</summary>
    private static string Source([CallerFilePath] string here = "")
    {
        string path = File.Exists(SourcePath)
            ? SourcePath
            : Path.Combine(Path.GetDirectoryName(here), "..", "..", "..", SourcePath);
        return File.ReadAllText(path).Replace("\r\n", "\n");
    }

    private static List<RowTable> ExportToday()
    {
        var problems = new List<string>();
        List<RowTable> tables = ContentSheets.Export(ContentSheetMap.World, ContentJson.Parse(Source()), problems);
        Assert.IsEmpty(problems, "world_source.json holds what the sheets cannot (map it in ContentSheetMap):\n" + string.Join("\n", problems.Take(40)));
        return tables;
    }

    private static string ImportBack(List<RowTable> tables)
    {
        var errors = new List<string>();
        ContentNode root = ContentSheets.Import(ContentSheetMap.World, tables, errors);
        Assert.IsEmpty(errors, string.Join("\n", errors.Take(40)));
        return ContentJson.Write(root);
    }

    [Test]
    public void TheMapIsSound()
    {
        CollectionAssert.IsEmpty(ContentSheets.MapProblems(ContentSheetMap.World));
    }

    [Test]
    public void TodaysSource_IsInThePythonLayoutTheWriterKeeps()
    {
        Assert.AreEqual(Source(), ContentJson.Write(ContentJson.Parse(Source())));
    }

    [Test]
    public void TodaysSource_ExportThenImport_IsByteIdentical()
    {
        Assert.AreEqual(Source(), ImportBack(ExportToday()));
    }

    [Test]
    public void TodaysSource_ThroughCsv_IsByteIdentical()
    {
        var errors = new List<string>();
        List<RowTable> back = ExportToday().Select(t => Csv.Read(t.Name, Csv.Write(t), errors)).ToList();
        CollectionAssert.IsEmpty(errors);
        Assert.AreEqual(Source(), ImportBack(back));
    }

    [Test]
    public void TodaysSource_ThroughTheWorkbook_IsByteIdentical()
    {
        var problems = new List<string>();
        List<RowTable> book = ContentSheets.Workbook(ContentSheetMap.World, ContentJson.Parse(Source()), problems);
        CollectionAssert.IsEmpty(problems);
        var errors = new List<string>();
        List<RowTable> back = Xlsx.Read(Xlsx.Write(book), errors);
        CollectionAssert.IsEmpty(errors);
        Assert.AreEqual(Source(), ImportBack(back));
    }

    [Test]
    public void TheTemplate_HasEverySheet_WithAFewExampleRows()
    {
        var problems = new List<string>();
        List<RowTable> template = ContentSheets.Workbook(ContentSheetMap.World, ContentJson.Parse(Source()), problems, ContentSheets.TemplateExamples);
        CollectionAssert.IsEmpty(problems);
        CollectionAssert.AreEqual(new[] { ContentSheets.ReadmeSheet }.Concat(ContentSheets.SheetNames(ContentSheetMap.World)), template.Select(t => t.Name));
        Assert.AreEqual(ContentSheets.TemplateExamples, template.Single(t => t.Name == "places").Rows.Count);
        Assert.AreEqual(ContentSheets.TemplateExamples, template.Single(t => t.Name == "premades").Rows.Count);
        Assert.IsTrue(template.Single(t => t.Name == "dialogLines").Rows.Count > 0);
    }

    [TestCase("places", "place")]
    [TestCase("premades", "premade")]
    [TestCase("dialogs", "dialog")]
    [TestCase("dialogNodes", "node")]
    [TestCase("questions", "question")]
    [TestCase("historyRules", "rule")]
    public void TheStoryTables_NameTheirRowsForTheirChildSheets(string sheet, string keyHeader)
    {
        SheetSpec spec = ContentSheets.Find(ContentSheetMap.World, sheet);
        Assert.IsNotNull(spec, sheet);
        Assert.AreEqual(keyHeader, spec.KeyHeader);
    }
}
