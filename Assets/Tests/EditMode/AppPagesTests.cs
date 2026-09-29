using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The Investigation app's page kinds as data (redesign phase 16, the PC spec
/// FO9, §2.5-§2.9): the Record Extract's groups and the way back to a row,
/// the Register's rows and numbers, the Interview Record's rows, the
/// Deviation Report's rows and links, and the Directive Memo's rows.
/// </summary>
public class AppPagesTests
{
    private static CitizenRecord Oren() => new CitizenRecord("Oren Hale", "552-1804-33", new[]
    {
        new RecordGroup("Records", new[] { new RecordRow("Name", "Oren Hale", ClueCategory.Name), new RecordRow("Citizen ID", "552-1804-33", ClueCategory.CitizenId), new RecordRow("Lineage", "Hale") }),
        new RecordGroup("Travel", new[] { new RecordRow("Booked departure", "Periclean Athens (Ancient)", ClueCategory.Destination), new RecordRow("Trips", "") })
    });

    // ---------------- The Record Extract ----------------

    [Test]
    public void RecordExtract_AFlatIndexFindsItsRowAcrossTheGroups()
    {
        Assert.IsTrue(RecordExtractPage.TryRow(Oren(), 0, out RecordRow name) && name.Label == "Name");
        Assert.IsTrue(RecordExtractPage.TryRow(Oren(), 3, out RecordRow booked) && booked.Label == "Booked departure", "the flat index runs across the groups");
        Assert.IsFalse(RecordExtractPage.TryRow(Oren(), 5, out _), "past the last row");
        Assert.IsFalse(RecordExtractPage.TryRow(Oren(), -1, out _));
        Assert.IsFalse(RecordExtractPage.TryRow(null, 0, out _));
    }

    [Test]
    public void RecordExtract_ALinksRow_IsTheFirstEvidenceRowOfItsCategory_AndOnlyEvidenceWithAValueIsPickable()
    {
        Assert.AreEqual(3, RecordExtractPage.RowOf(Oren(), ClueCategory.Destination));
        Assert.AreEqual(-1, RecordExtractPage.RowOf(Oren(), ClueCategory.BirthDate), "no such row");
        Assert.AreEqual(-1, RecordExtractPage.RowOf(null, ClueCategory.Name));
        Assert.IsTrue(RecordExtractPage.IsPickable(new RecordRow("Name", "Oren Hale", ClueCategory.Name)));
        Assert.IsFalse(RecordExtractPage.IsPickable(new RecordRow("Lineage", "Hale")), "a plain row is shown only");
        Assert.IsFalse(RecordExtractPage.IsPickable(new RecordRow("Trips", "", ClueCategory.Destination)), "an empty value picks nothing");
    }

    // ---------------- The Register ----------------

    private static readonly FactRow Athens = new FactRow("greece", "ancient", "Periclean Athens (Ancient)", ClueCategory.Currency, "Silver drachma");
    private static readonly FactRow Florence = new FactRow("italy", "medieval", "Florence (Medieval)", ClueCategory.Currency, "Florin");

    private static string EraName(string id) => id == "ancient" ? "Ancient" : id == "medieval" ? "Medieval" : null;

    [Test]
    public void Register_FormNumbers_CountOnFromTheAssets()
    {
        Assert.AreEqual("TC-911", RegisterPage.FormNumber("TC-911", 0));
        Assert.AreEqual("TC-916", RegisterPage.FormNumber("TC-911", 5));
        Assert.AreEqual("TC-9", RegisterPage.FormNumber("TC-9", 0));
        Assert.AreEqual("TC-A", RegisterPage.FormNumber("TC-A", 3), "no trailing digits: the asset's own");
        Assert.AreEqual(string.Empty, RegisterPage.FormNumber(null, 2));
    }

    [Test]
    public void Register_Rows_AHeadingAlone_ARowAsPlaceEraValueNote_TheClaimedAndRevisedNoted()
    {
        var lines = new List<ReferenceLine> { ReferenceLine.ForRow(Athens, true), ReferenceLine.Heading("medieval"), ReferenceLine.ForRow(Florence, false) };
        List<string[]> rows = RegisterPage.Rows(lines, EraName, f => f.NationId == "italy", "Claimed", "Revised");
        Assert.AreEqual(3, rows.Count);
        CollectionAssert.AreEqual(new[] { "Periclean Athens", "Ancient", "Silver drachma", "Claimed" }, rows[0]);
        CollectionAssert.AreEqual(new[] { "Medieval" }, rows[1], "a heading is one cell across");
        CollectionAssert.AreEqual(new[] { "Florence", "Medieval", "Florin", "Revised" }, rows[2]);
        List<string[]> both = RegisterPage.Rows(new[] { ReferenceLine.ForRow(Athens, true) }, EraName, _ => true, "Claimed", "Revised");
        Assert.AreEqual("Claimed · Revised", both[0][3]);
        List<string[]> unknown = RegisterPage.Rows(new[] { ReferenceLine.Heading("future") }, EraName, null, "Claimed", "Revised");
        Assert.AreEqual("future", unknown[0][0], "an era the names do not know reads its id");
        Assert.AreEqual(0, RegisterPage.Rows(null, EraName, null, "", "").Count);
    }

    [Test]
    public void Register_ARowKeepsItsLinesIndex_SoALinkFindsItsRow()
    {
        var lines = new List<ReferenceLine> { ReferenceLine.ForRow(Athens, true), ReferenceLine.Heading("medieval"), ReferenceLine.ForRow(Florence, false) };
        Assert.AreEqual(2, RegisterPage.LineOf(lines, PickKeys.BookRow(ClueCategory.Currency, "italy", "medieval")));
        Assert.AreEqual(0, RegisterPage.LineOf(lines, PickKeys.BookRow(ClueCategory.Currency, "greece", "ancient")));
        Assert.AreEqual(-1, RegisterPage.LineOf(lines, PickKeys.BookRow(ClueCategory.Language, "greece", "ancient")), "another book's row");
        Assert.AreEqual(-1, RegisterPage.LineOf(lines, null));
    }

    [Test]
    public void OriginLabels_Place_StripsTheEraFormatWrote()
    {
        Assert.AreEqual("Abbasid Baghdad", OriginLabels.Place(OriginLabels.Format("Abbasid Baghdad", "Medieval"), "Medieval"));
        Assert.AreEqual("Abbasid Baghdad (Medieval)", OriginLabels.Place("Abbasid Baghdad (Medieval)", "Ancient"), "another era: the label whole");
        Assert.AreEqual("Temporal Customs Zone", OriginLabels.Place("Temporal Customs Zone", null));
        Assert.AreEqual(string.Empty, OriginLabels.Place(null, "Medieval"));
    }

    [Test]
    public void SmartLinks_ForPlace_GoesToTheFirstBooksRowOfThePlace_NoneWithoutBothIdsOrABook()
    {
        Assert.AreEqual(LinkTarget.ToRow(AppTab.Reference, PickKeys.BookRow(ClueCategory.Currency, "greece", "ancient")), SmartLinks.ForPlace("greece", "ancient", ClueCategory.Currency));
        Assert.IsTrue(SmartLinks.ForPlace("greece", null, ClueCategory.Currency).IsNone, "a whole nation closed names no row");
        Assert.IsTrue(SmartLinks.ForPlace(null, "ancient", ClueCategory.Currency).IsNone, "a whole era closed names no row");
        Assert.IsTrue(SmartLinks.ForPlace("greece", "ancient", null).IsNone, "no books built");
    }

    // ---------------- The Interview Record ----------------

    private static IReadOnlyList<DialogLine> Lines() => new[]
    {
        new DialogLine("open", DialogSpeaker.Desk, "Papers, please."),
        new DialogLine("claim", DialogSpeaker.Traveller, "One departure to Athens."),
        DialogLine.Answer("a1", "We pay in drachma.", new InterviewAnswer { category = ClueCategory.Currency, value = "Silver drachma" })
    };

    [Test]
    public void Interview_Rows_NumberEveryLine_MarkTheAnswers_AndRememberTheirLines()
    {
        var lineOfRow = new List<int>();
        List<string[]> rows = InterviewPage.Rows(Lines(), false, "Desk officer", "Nikias", l => l.Text.ToUpperInvariant(), "►", lineOfRow);
        Assert.AreEqual(3, rows.Count);
        CollectionAssert.AreEqual(new[] { "1", "Desk officer", "PAPERS, PLEASE." }, rows[0]);
        CollectionAssert.AreEqual(new[] { "2", "Nikias", "ONE DEPARTURE TO ATHENS." }, rows[1]);
        CollectionAssert.AreEqual(new[] { "►3", "Nikias", "WE PAY IN DRACHMA." }, rows[2], "an answer carries the mark before its number");
        CollectionAssert.AreEqual(new[] { 0, 1, 2 }, lineOfRow);
        Assert.AreEqual(2, InterviewPage.RowOf(lineOfRow, 2));
        Assert.AreEqual(-1, InterviewPage.RowOf(lineOfRow, 7));
    }

    [Test]
    public void Interview_AnswersOnly_KeepsTheAnswers_AndTheirLineNumbers()
    {
        var lineOfRow = new List<int>();
        List<string[]> rows = InterviewPage.Rows(Lines(), true, "Desk officer", "Nikias", null, null, lineOfRow);
        Assert.AreEqual(1, rows.Count);
        CollectionAssert.AreEqual(new[] { "3", "Nikias", "We pay in drachma." }, rows[0], "no mark without one; the shown text is the line's without a writer");
        CollectionAssert.AreEqual(new[] { 2 }, lineOfRow, "the row remembers line 2");
        Assert.AreEqual(0, InterviewPage.RowOf(lineOfRow, 2));
        Assert.AreEqual(-1, InterviewPage.RowOf(lineOfRow, 0), "a hidden line has no row");
        Assert.AreEqual(0, InterviewPage.Rows(null, false, "", "", null, null, lineOfRow).Count);
        Assert.AreEqual(0, lineOfRow.Count);
    }

    // ---------------- The Deviation Report ----------------

    private static ComparePick Pick(string key, string label, CompareEvidence evidence) => new ComparePick(key, label, evidence.value, evidence);

    private static readonly ComparePick VisaClass = Pick("field:0:4", "Leisure Departure Visa · Visa Class", CompareEvidence.FromDocumentField(new DocumentField { category = ClueCategory.TransponderClass, label = "Visa Class", value = "Premium" }, 0));
    private static readonly ComparePick Account = Pick("record:418-0937-52:TransponderClass", "Records · Class", CompareEvidence.ForRecordField(ClueCategory.TransponderClass, "Standard", "Aster Vale"));
    private static readonly ComparePick Manifest = Pick("field:1:2", "Departure Manifest · Class", CompareEvidence.FromDocumentField(new DocumentField { category = ClueCategory.TransponderClass, label = "Class", value = "Standard" }, 1));

    private static Discrepancy RecordProof() => new Discrepancy
    {
        category = ClueCategory.TransponderClass, documentValue = "Premium", expectedValue = "Standard", provedBy = DiscrepancyProof.RecordMismatch, source = EvidenceKind.DocumentField
    };

    [Test]
    public void ReportEntry_From_PutsTheStatementFirst_WhicheverSideWasPickedFirst()
    {
        ReportEntry picked = ReportEntry.From(RecordProof(), VisaClass, Account);
        Assert.AreEqual((VisaClass.Key, Account.Key), (picked.Statement.Key, picked.Truth.Key));
        ReportEntry reversed = ReportEntry.From(RecordProof(), Account, VisaClass);
        Assert.AreEqual((VisaClass.Key, Account.Key), (reversed.Statement.Key, reversed.Truth.Key), "the truth picked first still comes second");
        var cross = new Discrepancy { category = ClueCategory.TransponderClass, documentValue = "Premium", expectedValue = "Standard", provedBy = DiscrepancyProof.CrossMismatch, source = EvidenceKind.DocumentField };
        ReportEntry papers = ReportEntry.From(cross, Manifest, VisaClass);
        Assert.AreEqual((Manifest.Key, VisaClass.Key), (papers.Statement.Key, papers.Truth.Key), "two papers: the first picked is the statement");
    }

    [Test]
    public void Report_Rows_AHeadingPerDeviation_OverItsTwoSides_AndTheSidesCellsLinkToTheirPicks()
    {
        var entries = new List<ReportEntry> { ReportEntry.From(RecordProof(), VisaClass, Account), ReportEntry.From(RecordProof(), Manifest, Account) };
        List<string[]> rows = ReportPage.Rows(entries, c => c.ToString().ToUpperInvariant(), p => p == DiscrepancyProof.RecordMismatch ? "Agency records" : "?");
        Assert.AreEqual(4, rows.Count, "a heading and a row of the two sides per deviation");
        CollectionAssert.AreEqual(new[] { "1  TRANSPONDERCLASS · Agency records" }, rows[0], "the heading: the number, the category and the proof, one cell across");
        CollectionAssert.AreEqual(new[] { "Leisure Departure Visa · Visa Class: Premium", "Records · Class: Standard" }, rows[1], "the sides: the statement, then what contradicts it");
        Assert.AreEqual("2  TRANSPONDERCLASS · Agency records", rows[2][0]);
        Assert.AreEqual(-1, ReportPage.EntryOfRow(0), "a heading is no entry's row");
        Assert.AreEqual(0, ReportPage.EntryOfRow(1));
        Assert.AreEqual(1, ReportPage.EntryOfRow(3));
        Assert.AreEqual(-1, ReportPage.EntryOfRow(-1));
        Assert.AreEqual(VisaClass.Key, ReportPage.LinkKey(entries[0], ReportPage.StatementCell));
        Assert.AreEqual(Account.Key, ReportPage.LinkKey(entries[0], ReportPage.TruthCell));
        Assert.IsNull(ReportPage.LinkKey(entries[0], 2), "no third cell");
        Assert.IsNull(ReportPage.LinkKey(null, ReportPage.StatementCell));
        Assert.AreEqual(0, ReportPage.Rows(null, null, null).Count);
    }

    [Test]
    public void Report_AForeignOriginProof_ShowsWhereTheValueBelongs()
    {
        var foreign = new Discrepancy { category = ClueCategory.Currency, documentValue = "Florin", actualOrigin = "Florence (Medieval)", provedBy = DiscrepancyProof.ForeignOrigin, source = EvidenceKind.Answer };
        var answer = Pick("line:4", "Traveller · CURRENCY", CompareEvidence.ForAnswer(ClueCategory.Currency, "Florin", true));
        var book = Pick("book:Currency:italy:medieval", "Currency Ledger: Florence (Medieval)", CompareEvidence.ForReferenceEntry(ClueCategory.Currency, "Florin", "italy", "medieval", "Florence (Medieval)"));
        List<string[]> rows = ReportPage.Rows(new[] { ReportEntry.From(foreign, book, answer) }, c => c.ToString(), p => p.ToString());
        CollectionAssert.AreEqual(new[] { "1  Currency · ForeignOrigin" }, rows[0]);
        CollectionAssert.AreEqual(new[] { "Traveller · CURRENCY: Florin", "Currency Ledger: Florence (Medieval): Florin" }, rows[1], "each side shows what the dock showed");
    }

    // ---------------- The Directive Memo ----------------

    [Test]
    public void DirectiveMemo_Rows_NumberTheDirectives_SkippingBlanks()
    {
        List<string[]> rows = DirectiveMemoPage.Rows(new[] { "No travel to the Medieval era today.", " ", null, " Dress for the destination. " });
        Assert.AreEqual(2, rows.Count);
        CollectionAssert.AreEqual(new[] { "1", "No travel to the Medieval era today." }, rows[0]);
        CollectionAssert.AreEqual(new[] { "2", "Dress for the destination." }, rows[1]);
        Assert.AreEqual(0, DirectiveMemoPage.Rows(null).Count);
    }
}
