using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using NUnit.Framework;

/// <summary>
/// The narrative workbook against today's world_source.json: it is built on the content
/// spreadsheet's own tables, an export read back with no edits changes nothing (byte for
/// byte, through a real .xlsx), an edited Pell line lands on exactly that line, and the
/// import refuses what it cannot place (a conflict, a stray row, a damaged ref, a slot
/// change) instead of dropping it. Lines add and remove rows; the views say when
/// things fire.
/// </summary>
public class NarrativeWorkbookTests
{
    private const string SourcePath = "Assets/Data/World/world_source.json";

    private static string Source([CallerFilePath] string here = "")
    {
        string path = File.Exists(SourcePath)
            ? SourcePath
            : Path.Combine(Path.GetDirectoryName(here), "..", "..", "..", SourcePath);
        return File.ReadAllText(path).Replace("\r\n", "\n");
    }

    private static List<RowTable> Content()
    {
        var problems = new List<string>();
        List<RowTable> tables = ContentSheets.Export(ContentSheetMap.World, ContentJson.Parse(Source()), problems);
        Assert.IsEmpty(problems);
        return tables;
    }

    /// <summary>The workbook as a spreadsheet would give it back: written to .xlsx bytes and read again.</summary>
    private static List<RowTable> ThroughXlsx(List<RowTable> book)
    {
        var errors = new List<string>();
        List<RowTable> back = Xlsx.Read(Xlsx.Write(book), errors);
        CollectionAssert.IsEmpty(errors);
        return back;
    }

    private static string Import(List<RowTable> content, List<RowTable> workbook, out NarrativeImport result)
    {
        result = NarrativeImport.Apply(content, workbook);
        Assert.IsEmpty(result.Errors, string.Join("\n", result.Errors.Take(20)));
        var errors = new List<string>();
        ContentNode root = ContentSheets.Import(ContentSheetMap.World, result.Tables, errors);
        Assert.IsEmpty(errors, string.Join("\n", result.Explain(errors).Take(20)));
        return ContentJson.Write(root);
    }

    private static RowTable Sheet(List<RowTable> book, string name) => book.Single(t => t.Name == name);

    private static int Col(RowTable t, string header) => t.Headers.IndexOf(header);

    /// <summary>The Narrative row bound to <paramref name="refPrefix"/> (a content sheet, row and column).</summary>
    private static string[] Bound(RowTable narrative, string reference) =>
        narrative.Rows.First(r => r[Col(narrative, "ref")] == reference);

    private static string RefOfLine(List<RowTable> content, string sheet, string header, string value)
    {
        RowTable t = content.Single(x => x.Name == sheet);
        int row = t.Rows.FindIndex(r => r[t.Headers.IndexOf(header)] == value);
        Assert.GreaterOrEqual(row, 0, $"{sheet} has no {header} '{value}'");
        return NarrativeWorkbook.Ref(sheet, row);
    }

    [Test]
    public void Build_HasEverySheet_ReadmeFirst_ListsHidden()
    {
        List<RowTable> book = NarrativeWorkbook.Build(ContentSheetMap.World, Content(), null);
        CollectionAssert.AreEqual(new[] { "README", "Days", "Cases", "Narrative", "Lines", "Triggers", "Faults", "Lists" }, book.Select(t => t.Name));
        RowTable faults = book.Single(t => t.Name == "Faults");
        Assert.IsTrue(faults.Rows.Any(r => r[0] == "any paper" && r[2] == "ForgedSeal"), "the canon's view: a forged seal on any paper");
        Assert.IsTrue(faults.Rows.Any(r => r[0] == "TC-101" && r[1] == "Photo" && r[4] == "the traveller at the desk"));
        Assert.IsTrue(Sheet(book, "Lists").Look.Hidden);
        Assert.IsFalse(book[0].Look.Hidden);
        Assert.AreEqual(15, Sheet(book, "Days").Rows.Count, "one row per day");
    }

    [Test]
    public void Narrative_PellBlock_HoldsHerDialogLineAndAppearance_AsEditableBoundRows()
    {
        List<RowTable> content = Content();
        RowTable narrative = Sheet(NarrativeWorkbook.Build(ContentSheetMap.World, content, null), "Narrative");
        string lineRef = RefOfLine(content, "dialogLines", "id", "dlg_pell_1.start.1") + ":text";
        string[] line = Bound(narrative, lineRef);
        Assert.AreEqual("pell", line[Col(narrative, "narrative")]);
        StringAssert.StartsWith("Periclean Athens!", line[Col(narrative, "text")]);
        StringAssert.StartsWith("Traveller", line[Col(narrative, "speaker")]);

        string[] banner = narrative.Rows.First(r => r[Col(narrative, "narrative")] == "pell");
        Assert.AreEqual("", banner[Col(narrative, "ref")], "the banner row is not bound");
        StringAssert.Contains("day 7 slot 5", banner[Col(narrative, "when")]);
        Assert.IsTrue(narrative.Rows.Any(r => r[Col(narrative, "narrative")] == "pell" && r[Col(narrative, "part")] == "story beat pell_departed" && r[Col(narrative, "field")] == "news"),
            "Pell's beat (its condition names her verdict) sits in her block");
        Assert.AreEqual(CellLook.Editable, narrative.Look.Columns[Col(narrative, "text")]);
        Assert.AreEqual(CellLook.Binding, narrative.Look.Columns[Col(narrative, "ref")]);
        Assert.AreEqual(CellLook.Locked, narrative.Look.Columns[Col(narrative, "when")]);
    }

    [Test]
    public void EveryBoundRow_NamesAContentCell()
    {
        List<RowTable> content = Content();
        List<RowTable> book = NarrativeWorkbook.Build(ContentSheetMap.World, content, null);
        foreach (RowTable sheet in new[] { Sheet(book, "Narrative"), Sheet(book, "Lines") })
            foreach (string[] row in sheet.Rows.Where(r => r[Col(sheet, "ref")].Length > 0))
            {
                string reference = row[Col(sheet, "ref")];
                string name = reference.Substring(0, reference.IndexOf('#'));
                Assert.IsTrue(content.Any(t => t.Name == name), reference);
            }
    }

    [Test]
    public void ExportThenImport_WithNoEdits_IsByteIdentical()
    {
        List<RowTable> content = Content();
        List<RowTable> back = ThroughXlsx(NarrativeWorkbook.Build(ContentSheetMap.World, content, null));
        Assert.AreEqual(Source(), Import(content, back, out NarrativeImport result));
        Assert.AreEqual(0, result.Edited + result.Added + result.Removed);
        CollectionAssert.IsEmpty(result.Changes);
        Assert.Greater(result.BoundRows, 1000);
    }

    [Test]
    public void EditingAPellLine_ChangesThatLineOnly()
    {
        List<RowTable> content = Content();
        List<RowTable> book = ThroughXlsx(NarrativeWorkbook.Build(ContentSheetMap.World, content, null));
        RowTable narrative = Sheet(book, "Narrative");
        string[] line = Bound(narrative, RefOfLine(content, "dialogLines", "id", "dlg_pell_1.start.1") + ":text");
        line[Col(narrative, "text")] = "Periclean Athens! The owls have been briefed.";

        string json = Import(content, book, out NarrativeImport result);
        Assert.AreEqual(1, result.Edited);
        string[] before = Source().Split('\n'), after = json.Split('\n');
        Assert.AreEqual(before.Length, after.Length);
        List<int> changed = Enumerable.Range(0, before.Length).Where(i => before[i] != after[i]).ToList();
        Assert.AreEqual(1, changed.Count);
        StringAssert.Contains("\"text\": \"Periclean Athens! The owls have been briefed.\"", after[changed[0]]);
    }

    [Test]
    public void AnEditToARowTheSourceChangedMeanwhile_IsAConflict_AndAnUneditedOneIsSkipped()
    {
        List<RowTable> content = Content();
        List<RowTable> book = ThroughXlsx(NarrativeWorkbook.Build(ContentSheetMap.World, content, null));
        RowTable lines = content.Single(t => t.Name == "dialogLines");
        int row = lines.Rows.FindIndex(r => r[lines.Headers.IndexOf("id")] == "dlg_pell_1.start.1");
        lines.Rows[row][lines.Headers.IndexOf("text")] = "Changed by another branch.";

        NarrativeImport untouched = NarrativeImport.Apply(content, book);
        CollectionAssert.IsEmpty(untouched.Errors);
        Assert.AreEqual(1, untouched.Stale);

        RowTable narrative = Sheet(book, "Narrative");
        Bound(narrative, NarrativeWorkbook.Ref("dialogLines", row) + ":text")[Col(narrative, "text")] = "My edit.";
        NarrativeImport edited = NarrativeImport.Apply(content, book);
        Assert.IsNull(edited.Tables);
        StringAssert.Contains("conflict", edited.Errors.Single());
        StringAssert.Contains("Narrative row", edited.Errors.Single());
    }

    [Test]
    public void ARowFoundElsewhere_StillTakesTheEdit()
    {
        List<RowTable> content = Content();
        List<RowTable> book = ThroughXlsx(NarrativeWorkbook.Build(ContentSheetMap.World, content, null));
        RowTable narrative = Sheet(book, "Narrative");
        string reference = RefOfLine(content, "dialogLines", "id", "dlg_pell_1.start.1") + ":text";
        Bound(narrative, reference)[Col(narrative, "text")] = "Moved but found.";

        // Another branch inserted a line above it: its row number moved by one.
        RowTable lines = content.Single(t => t.Name == "dialogLines");
        var moved = new RowTable(lines.Name, lines.Headers) { Kinds = lines.Kinds };
        moved.Add(lines.Headers.Select(h => h == "dialog" ? "dlg_rumour" : h == "node" ? lines.Rows[0][lines.Headers.IndexOf("node")] : h == "id" ? "x.new" : "").ToArray());
        foreach (string[] r in lines.Rows)
            moved.Add(r);
        content[content.IndexOf(lines)] = moved;

        NarrativeImport result = NarrativeImport.Apply(content, book);
        CollectionAssert.IsEmpty(result.Errors);
        Assert.AreEqual(1, result.Edited);
        Assert.IsTrue(result.Tables.Single(t => t.Name == "dialogLines").Rows.Any(r => r[lines.Headers.IndexOf("text")] == "Moved but found."));
    }

    [Test]
    public void AReadOnlyNote_IsShownLockedAndSkippedByTheImport()
    {
        var context = new NarrativeContext();
        context.Notes.Add(new NarrativeNote { Narrative = "strandings", Part = "waiver form", Field = "fine print", Text = "The Time Police may remove you.", Where = "DocTemplate_TC310.asset" });
        List<RowTable> content = Content();
        List<RowTable> book = ThroughXlsx(NarrativeWorkbook.Build(ContentSheetMap.World, content, context));
        RowTable narrative = Sheet(book, "Narrative");
        string[] note = narrative.Rows.Single(r => r[Col(narrative, "text")] == "The Time Police may remove you.");
        Assert.AreEqual(NarrativeWorkbook.ReadOnlyRef, note[Col(narrative, "ref")]);
        note[Col(narrative, "text")] = "Edited where it cannot be imported.";
        Assert.AreEqual(Source(), Import(content, book, out _));
    }

    [Test]
    public void TextInANarrativeRowWithoutARef_StopsTheImport()
    {
        List<RowTable> content = Content();
        List<RowTable> book = NarrativeWorkbook.Build(ContentSheetMap.World, content, null);
        RowTable narrative = Sheet(book, "Narrative");
        narrative.Add(narrative.Headers.Select(h => h == "text" ? "A new line nobody bound." : "").ToArray());
        NarrativeImport result = NarrativeImport.Apply(content, book);
        Assert.IsNull(result.Tables);
        StringAssert.Contains("no ref", result.Errors.Single());
    }

    [Test]
    public void ADamagedRef_AndTwoDifferentEditsOfOneCell_AreErrors()
    {
        List<RowTable> content = Content();
        List<RowTable> book = NarrativeWorkbook.Build(ContentSheetMap.World, content, null);
        RowTable narrative = Sheet(book, "Narrative");
        string[] first = narrative.Rows.First(r => r[Col(narrative, "ref")].Length > 0);
        string[] twin = (string[])first.Clone();
        first[Col(narrative, "text")] = "one";
        twin[Col(narrative, "text")] = "two";
        narrative.Add(twin);
        string[] damaged = narrative.Rows.Last(r => r[Col(narrative, "ref")].Length > 0 && r != twin);
        damaged[Col(narrative, "ref")] = "nonsense";

        NarrativeImport result = NarrativeImport.Apply(content, book);
        Assert.IsNull(result.Tables);
        Assert.IsTrue(result.Errors.Any(e => e.Contains("damaged")));
        Assert.IsTrue(result.Errors.Any(e => e.Contains("differently")));
    }

    [Test]
    public void Lines_ANewRow_IsAddedAfterItsVoice_AndARemovedRowGoes()
    {
        List<RowTable> content = Content();
        List<RowTable> book = ThroughXlsx(NarrativeWorkbook.Build(ContentSheetMap.World, content, null));
        RowTable lines = Sheet(book, "Lines");
        lines.Add(lines.Headers.Select(h => h == "slot" ? "voiceClaims" : h == "personality" ? "chatty" : h == "text" ? "Off to {place}, and not a moment too soon!" : "").ToArray());
        string[] victim = lines.Rows.First(r => r[Col(lines, "slot")] == "voiceSmallTalk" && r[Col(lines, "ref")].Length > 0);
        victim[Col(lines, "remove")] = "remove";

        string json = Import(content, book, out NarrativeImport result);
        Assert.AreEqual(1, result.Added);
        Assert.AreEqual(1, result.Removed);
        StringAssert.Contains("Off to {place}, and not a moment too soon!", json);
        StringAssert.DoesNotContain("\"text\": " + Quote(victim[Col(lines, "text")]), json);

        RowTable claims = result.Tables.Single(t => t.Name == "voiceClaims");
        int added = claims.Rows.FindIndex(r => r[claims.Headers.IndexOf("text")] == "Off to {place}, and not a moment too soon!");
        Assert.AreEqual("chatty", claims.Rows[added - 1][claims.Headers.IndexOf("personality")], "after the chatty rows");
    }

    private static string Quote(string s) => "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

    [Test]
    public void Lines_ASlotChange_OrAColumnTheSlotLacks_IsAnError_AndAnEditedLineLands()
    {
        List<RowTable> content = Content();
        List<RowTable> book = NarrativeWorkbook.Build(ContentSheetMap.World, content, null);
        RowTable lines = Sheet(book, "Lines");
        string[] claim = lines.Rows.First(r => r[Col(lines, "slot")] == "voiceClaims");
        claim[Col(lines, "slot")] = "voiceSlips";
        string[] other = lines.Rows.First(r => r[Col(lines, "slot")] == "voiceSmallTalk");
        other[Col(lines, "question")] = "q_currency";
        NarrativeImport bad = NarrativeImport.Apply(content, book);
        Assert.IsTrue(bad.Errors.Any(e => e.Contains("slot of an existing line")));
        Assert.IsTrue(bad.Errors.Any(e => e.Contains("does not apply")));

        book = NarrativeWorkbook.Build(ContentSheetMap.World, content, null);
        lines = Sheet(book, "Lines");
        lines.Rows.First(r => r[Col(lines, "slot")] == "voiceClaims")[Col(lines, "text")] = "To {place}. Chop chop.";
        StringAssert.Contains("To {place}. Chop chop.", Import(content, book, out _));
    }

    [Test]
    public void AnEngineError_NamesTheWorkbookRowItCameFrom()
    {
        List<RowTable> content = Content();
        List<RowTable> book = NarrativeWorkbook.Build(ContentSheetMap.World, content, null);
        RowTable lines = Sheet(book, "Lines");
        lines.Add(lines.Headers.Select(h => h == "slot" ? "voiceClaims" : h == "personality" ? "nobody" : h == "text" ? "{place}!" : "").ToArray());
        NarrativeImport result = NarrativeImport.Apply(content, book);
        CollectionAssert.IsEmpty(result.Errors);
        var errors = new List<string>();
        Assert.IsNull(ContentSheets.Import(ContentSheetMap.World, result.Tables, errors));
        StringAssert.Contains($"typed at Lines row {lines.Rows.Count + 1}", result.Explain(errors).Single());
    }

    [Test]
    public void Triggers_SayWhatABeatDependsOn_AndDaysListTheAuthoredSlots()
    {
        List<RowTable> book = NarrativeWorkbook.Build(ContentSheetMap.World, Content(), null);
        RowTable triggers = Sheet(book, "Triggers");
        string[] beat = triggers.Rows.Single(r => r[Col(triggers, "kind")] == "story beat" && r[Col(triggers, "id")] == "pell_departed");
        Assert.AreEqual("pell", beat[Col(triggers, "narrative")]);
        StringAssert.Contains("the clerk's stamp on Pell Quimby", beat[Col(triggers, "depends on")]);

        RowTable days = Sheet(book, "Days");
        string[] day7 = days.Rows.Single(r => r[Col(days, "day")] == "7");
        StringAssert.Contains("5: Pell Quimby (pell_1", day7[Col(days, "authored slots")]);
    }

    [Test]
    public void Cases_LayOutTheReferenceRun()
    {
        var context = new NarrativeContext { RunNote = "seed 1" };
        context.Cases.Add(new NarrativeCase { Seed = 1, Day = 7, Slot = 5, Source = NarrativeCaseSource.Forced, Appearance = "pell_1", Premade = "pell", Name = "Pell Quimby", Claim = "Athens!", IfDenied = "Oh." });
        context.Events.Add(new NarrativeEvent(1, 7, NarrativeEvent.BeatFired, "pell_departed"));
        List<RowTable> book = NarrativeWorkbook.Build(ContentSheetMap.World, Content(), context);
        RowTable cases = Sheet(book, "Cases");
        string[] row = cases.Rows.Single();
        Assert.AreEqual("authored (forced slot)", row[Col(cases, "source")]);
        Assert.AreEqual("Oh.", row[Col(cases, "if denied")]);
        RowTable triggers = Sheet(book, "Triggers");
        StringAssert.Contains("seed 1: night 7", triggers.Rows.Single(r => r[Col(triggers, "id")] == "pell_departed")[Col(triggers, "reference run")]);
        StringAssert.Contains("seed 1: Pell Quimby", triggers.Rows.First(r => r[Col(triggers, "id")] == "pell_1")[Col(triggers, "reference run")]);
    }

    [Test]
    public void AStyledWorkbook_ReadsBackAsItsText_AndAPlainOneKeepsThePlainStyles()
    {
        var plain = new RowTable("people", new[] { "id", "n" }) { Kinds = new[] { CellKind.Text, CellKind.Number } };
        plain.Add(new[] { "ann", "3" });
        var styled = new RowTable("styled", new[] { "a", "b" }) { Look = new SheetLook { Columns = new[] { CellLook.Editable, CellLook.Locked }, Protect = true, Filter = true } };
        styled.Add(new[] { "x", "" });
        styled.Look.SectionRows.Add(0);
        styled.Look.Lists.Add(new ListRule("\"x,y\"").Add(0, 0).AddColumn(0, 1, 9));
        var errors = new List<string>();
        List<RowTable> back = Xlsx.Read(Xlsx.Write(new[] { plain, styled }), errors);
        CollectionAssert.IsEmpty(errors);
        CollectionAssert.AreEqual(new[] { "x", "" }, back[1].Rows[0]);
        CollectionAssert.AreEqual(new[] { "ann", "3" }, back[0].Rows[0]);
        StringAssert.Contains("Calibri", Part(Xlsx.Write(new[] { plain }), "xl/styles.xml"), "a workbook of plain tables keeps the content workbook's styles");
        string sheet = Part(Xlsx.Write(new[] { plain, styled }), "xl/worksheets/sheet2.xml");
        StringAssert.Contains("<sheetProtection", sheet);
        StringAssert.Contains("sqref=\"A2:A11\"", sheet, "the cell and the column run merge into one range");
    }

    private static string Part(byte[] xlsx, string name)
    {
        using var zip = new System.IO.Compression.ZipArchive(new MemoryStream(xlsx));
        using var reader = new StreamReader(zip.GetEntry(name).Open());
        return reader.ReadToEnd();
    }
}
