using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using static ColumnSpec;
using static SheetSpec;

/// <summary>
/// The sheet engine on a small declarative map: JSON to sheets and back byte for byte
/// (through CSV and XLSX too), blank cells as defaults, strict headers and sheets,
/// row and column error messages, the map's own checks, examples and the README.
/// </summary>
public class ContentSheetsTests
{
    private static SheetSpec Map() =>
        Single("world", "",
            Int("ageMin"),
            Rows("people", "people", Key("id", "person"),
                Text("id").Required(),
                Text("name"),
                Bool("vip").Omit(),
                Text("home").Ref("places"),
                List("tags").Omit(),
                Values("personLines", "lines", Text("text")).OmitEmpty(),
                Keyed("looks", "look", Text("slot").OneOf("hat", "coat"),
                    Text("label").Required(),
                    Bool("worn").Omit()),
                Nums("stats.skin").Omit(),
                Rows("personHair", "stats.hair", Num("weight")).OmitEmpty()),
            Rows("places", "places", Key("{country}_{era}", "place"),
                Text("country").Required(),
                Text("era").Required(),
                Float("score")),
            Single("settings", "settings",
                Text("title"),
                Float("rate"),
                Num("alpha")));

    private const string Fixture =
        "{\n" +
        "  \"ageMin\": 18,\n" +
        "  \"people\": [\n" +
        "    {\n" +
        "      \"id\": \"ann\",\n" +
        "      \"name\": \"Ann \\\"the Bold\\\"\\nof Ur\",\n" +
        "      \"vip\": true,\n" +
        "      \"home\": \"egypt_ancient\",\n" +
        "      \"tags\": [\n" +
        "        \"a\",\n" +
        "        \"b\"\n" +
        "      ],\n" +
        "      \"lines\": [\n" +
        "        \"Hello, you.\"\n" +
        "      ],\n" +
        "      \"look\": {\n" +
        "        \"hat\": {\n" +
        "          \"label\": \"fez\",\n" +
        "          \"worn\": true\n" +
        "        },\n" +
        "        \"coat\": {\n" +
        "          \"label\": \"robe\"\n" +
        "        }\n" +
        "      },\n" +
        "      \"stats\": {\n" +
        "        \"skin\": [\n" +
        "          1,\n" +
        "          0.5\n" +
        "        ],\n" +
        "        \"hair\": [\n" +
        "          {\n" +
        "            \"weight\": 2\n" +
        "          }\n" +
        "        ]\n" +
        "      }\n" +
        "    },\n" +
        "    {\n" +
        "      \"id\": \"bob\",\n" +
        "      \"name\": \"Bob\",\n" +
        "      \"home\": \"\",\n" +
        "      \"look\": {}\n" +
        "    }\n" +
        "  ],\n" +
        "  \"places\": [\n" +
        "    {\n" +
        "      \"country\": \"egypt\",\n" +
        "      \"era\": \"ancient\",\n" +
        "      \"score\": 1.0\n" +
        "    }\n" +
        "  ],\n" +
        "  \"settings\": {\n" +
        "    \"title\": \"\U0001310F Ω\",\n" +
        "    \"rate\": 0.05,\n" +
        "    \"alpha\": 1\n" +
        "  }\n" +
        "}\n";

    private static List<RowTable> Export(string json = Fixture, int examples = 0)
    {
        var problems = new List<string>();
        List<RowTable> tables = ContentSheets.Export(Map(), ContentJson.Parse(json), problems, examples);
        CollectionAssert.IsEmpty(problems);
        return tables;
    }

    private static List<string> ImportErrors(List<RowTable> tables)
    {
        var errors = new List<string>();
        Assert.IsNull(ContentSheets.Import(Map(), tables, errors));
        Assert.IsNotEmpty(errors);
        return errors;
    }

    private static string Import(List<RowTable> tables)
    {
        var errors = new List<string>();
        ContentNode root = ContentSheets.Import(Map(), tables, errors);
        CollectionAssert.IsEmpty(errors);
        return ContentJson.Write(root);
    }

    private static RowTable Sheet(List<RowTable> tables, string name) => tables.Single(t => t.Name == name);

    // ---- round trips ----

    [Test]
    public void ExportThenImport_IsByteIdentical()
    {
        Assert.AreEqual(Fixture, Import(Export()));
    }

    [Test]
    public void ExportThenImport_ThroughCsv_IsByteIdentical()
    {
        var errors = new List<string>();
        List<RowTable> back = Export().Select(t => Csv.Read(t.Name, Csv.Write(t), errors)).ToList();
        CollectionAssert.IsEmpty(errors);
        Assert.AreEqual(Fixture, Import(back));
    }

    [Test]
    public void ExportThenImport_ThroughXlsx_IsByteIdentical()
    {
        var errors = new List<string>();
        List<RowTable> back = Xlsx.Read(Xlsx.Write(Export()), errors);
        CollectionAssert.IsEmpty(errors);
        Assert.AreEqual(Fixture, Import(back));
    }

    // ---- the sheets ----

    [Test]
    public void Export_OneSheetPerTable_InMapOrder_WithAncestorKeyColumns()
    {
        List<RowTable> tables = Export();
        CollectionAssert.AreEqual(new[] { "world", "people", "personLines", "looks", "personHair", "places", "settings" }, tables.Select(t => t.Name));
        CollectionAssert.AreEqual(new[] { "id", "name", "vip", "home", "tags", "stats.skin" }, Sheet(tables, "people").Headers);
        CollectionAssert.AreEqual(new[] { "ann", "Ann \"the Bold\"\nof Ur", "true", "egypt_ancient", "a|b", "1|0.5" }, Sheet(tables, "people").Rows[0]);
        CollectionAssert.AreEqual(new[] { "bob", "Bob", "", "", "", "" }, Sheet(tables, "people").Rows[1]);
        CollectionAssert.AreEqual(new[] { "person", "text" }, Sheet(tables, "personLines").Headers);
        CollectionAssert.AreEqual(new[] { "person", "slot", "label", "worn" }, Sheet(tables, "looks").Headers);
        CollectionAssert.AreEqual(new[] { "ann", "coat", "robe", "" }, Sheet(tables, "looks").Rows[1]);
        CollectionAssert.AreEqual(new[] { "person", "weight" }, Sheet(tables, "personHair").Headers);
        CollectionAssert.AreEqual(new[] { "1.0" }, Sheet(tables, "places").Rows[0].Skip(2));
        Assert.AreEqual(1, Sheet(tables, "settings").Rows.Count);
    }

    [Test]
    public void Export_Examples_KeepTheFirstRowsAndTheirChildren()
    {
        List<RowTable> tables = Export(examples: 1);
        Assert.AreEqual(1, Sheet(tables, "people").Rows.Count);
        Assert.AreEqual(2, Sheet(tables, "looks").Rows.Count);
        Assert.AreEqual(1, Sheet(tables, "settings").Rows.Count);
    }

    [Test]
    public void Import_BlankCellsTakeTheDocumentedDefault()
    {
        List<RowTable> tables = Export();
        RowTable people = Sheet(tables, "people");
        people.Rows[1][1] = "";          // name: blank is ""
        people.Rows[1][2] = "FALSE";     // vip: false is the default of an omitted column
        Sheet(tables, "places").Rows[0][2] = "1";   // a decimal column writes 1.0
        string json = Import(tables);
        StringAssert.Contains("\"id\": \"bob\",\n      \"name\": \"\",\n      \"home\": \"\"", json);
        StringAssert.Contains("\"score\": 1.0", json);
    }

    [Test]
    public void Import_RowsFollowTheSheetOrder_AndChildrenFindTheirParentAnywhere()
    {
        List<RowTable> tables = Export();
        RowTable looks = Sheet(tables, "looks");
        looks.Rows.Reverse();
        string json = Import(tables);
        Assert.Less(json.IndexOf("\"coat\""), json.IndexOf("\"hat\""));
    }

    // ---- errors, with the sheet, row and column ----

    [Test]
    public void Import_UnknownColumn_IsAnError()
    {
        List<RowTable> tables = Export();
        RowTable people = Sheet(tables, "people");
        people.Headers[1] = "Name";
        List<string> errors = ImportErrors(tables);
        Assert.IsTrue(errors.Any(e => e.Contains("people") && e.Contains("column B") && e.Contains("'Name'") && e.Contains("'name'")), string.Join("\n", errors));
    }

    [Test]
    public void Import_MissingSheet_AndUnknownSheet_AreErrors()
    {
        List<RowTable> tables = Export();
        tables.RemoveAll(t => t.Name == "personHair");
        tables.Add(new RowTable("extras", new[] { "a" }));
        tables.Add(new RowTable(ContentSheets.ReadmeSheet, new[] { "sheet" }));
        List<string> errors = ImportErrors(tables);
        Assert.IsTrue(errors.Any(e => e.Contains("personHair") && e.Contains("missing")), string.Join("\n", errors));
        Assert.IsTrue(errors.Any(e => e.Contains("extras")), string.Join("\n", errors));
        Assert.IsFalse(errors.Any(e => e.Contains(ContentSheets.ReadmeSheet)), string.Join("\n", errors));
    }

    [TestCase("people", 0, "vip", "maybe", "true or false")]
    [TestCase("world", 0, "ageMin", "12.5", "whole number")]
    [TestCase("places", 0, "score", "high", "number")]
    [TestCase("people", 0, "id", "", "required")]
    [TestCase("people", 1, "id", "ann", "duplicate")]
    [TestCase("people", 0, "home", "atlantis_ancient", "places")]
    [TestCase("people", 0, "tags", "a||b", "empty item")]
    [TestCase("looks", 0, "slot", "cape", "one of")]
    [TestCase("personLines", 0, "person", "zed", "people")]
    public void Import_BadCell_NamesSheetRowAndColumn(string sheet, int row, string header, string value, string expected)
    {
        List<RowTable> tables = Export();
        RowTable t = Sheet(tables, sheet);
        int col = t.Headers.IndexOf(header);
        t.Rows[row][col] = value;
        List<string> errors = ImportErrors(tables);
        string where = $"{sheet}: row {t.RowNumbers[row]}, column {(char)('A' + col)} ({header})";
        Assert.IsTrue(errors.Any(e => e.StartsWith(where) && e.Contains(expected)), $"expected '{where} ... {expected}' in:\n" + string.Join("\n", errors));
    }

    [Test]
    public void Import_ASingleSheetHoldsExactlyOneRow()
    {
        List<RowTable> tables = Export();
        RowTable settings = Sheet(tables, "settings");
        settings.Add(settings.Rows[0].ToArray());
        List<string> errors = ImportErrors(tables);
        Assert.IsTrue(errors.Any(e => e.StartsWith("settings") && e.Contains("one row")), string.Join("\n", errors));
    }

    [Test]
    public void Import_MissingColumn_IsAnError()
    {
        List<RowTable> tables = Export();
        RowTable places = Sheet(tables, "places");
        var cut = new RowTable("places", places.Headers.Take(2));
        foreach (string[] r in places.Rows)
            cut.Add(r.Take(2).ToArray());
        tables[tables.IndexOf(places)] = cut;
        List<string> errors = ImportErrors(tables);
        Assert.IsTrue(errors.Any(e => e.StartsWith("places") && e.Contains("'score'") && e.Contains("missing")), string.Join("\n", errors));
    }

    // ---- what the export refuses ----

    [TestCase("\"home\": \"\",", "\"home\": \"\", \"extra\": 1,", "$.people[1].extra")]
    [TestCase("\"name\": \"Bob\",", "", "$.people[1].name")]
    [TestCase("\"home\": \"\",", "\"home\": \"\", \"vip\": false,", "$.people[1].vip")]
    [TestCase("\"score\": 1.0", "\"score\": 1", "$.places[0].score")]
    [TestCase("\"tags\": [", "\"tags\": [\"a|b\",", "$.people[0].tags")]
    public void Export_ReportsWhatTheSheetsCannotHold(string from, string to, string path)
    {
        var problems = new List<string>();
        ContentSheets.Export(Map(), ContentJson.Parse(Fixture.Replace(from, to)), problems);
        Assert.IsTrue(problems.Any(p => p.Contains(path)), $"expected {path} in:\n" + string.Join("\n", problems));
    }

    // ---- the map's own checks and the README ----

    [Test]
    public void MapProblems_TheFixtureMapIsSound()
    {
        CollectionAssert.IsEmpty(ContentSheets.MapProblems(Map()));
    }

    [Test]
    public void MapProblems_CatchBadMaps()
    {
        SheetSpec bad =
            Single("world", "",
                Rows("items", "items",
                    Text("id"),
                    Rows("parts", "parts", Text("x"))),
                Rows("items", "more", Key("id", "id"), Text("id"), Text("other").Ref("nowhere"),
                    Values("tooLong_abcdefghijklmnopqrstuvwxyz", "v", Text("id"))));
        List<string> problems = ContentSheets.MapProblems(bad);
        Assert.IsTrue(problems.Any(p => p.Contains("'items'") && p.Contains("twice")), string.Join("\n", problems));
        Assert.IsTrue(problems.Any(p => p.Contains("'parts'") && p.Contains("key")), string.Join("\n", problems));
        Assert.IsTrue(problems.Any(p => p.Contains("nowhere")), string.Join("\n", problems));
        Assert.IsTrue(problems.Any(p => p.Contains("31")), string.Join("\n", problems));
        Assert.IsTrue(problems.Any(p => p.Contains("'id'") && p.Contains("header")), string.Join("\n", problems));
    }

    [Test]
    public void Workbook_PutsTheReadmeFirst_ListingEverySheetAndColumn()
    {
        var problems = new List<string>();
        List<RowTable> book = ContentSheets.Workbook(Map(), ContentJson.Parse(Fixture), problems);
        CollectionAssert.IsEmpty(problems);
        Assert.AreEqual(ContentSheets.ReadmeSheet, book[0].Name);
        RowTable readme = book[0];
        foreach (RowTable t in book.Skip(1))
            foreach (string header in t.Headers)
                Assert.IsTrue(readme.Rows.Any(r => r[0] == t.Name && r[1] == header), $"{t.Name}.{header} is not in the README");
    }
}
