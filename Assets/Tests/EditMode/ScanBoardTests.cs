using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The scanner app spec §2.1-§2.3: the auto record lookup (number, then
/// name, else NO RECORD), the rules check's verdicts (the destination against
/// today's closures, each date against today, each required paper) and the
/// cross-check table's mismatches (only values that are not the same value
/// glow; an honest set never does).
/// </summary>
public class ScanBoardTests
{
    private static DocumentField F(ClueCategory c, string value, string label = null) => new DocumentField { category = c, value = value, label = label ?? c.ToString() };

    private static readonly DateTime Today = new DateTime(2150, 3, 14);

    private static BoardPaper Passport(string expiry = "2 Jan 2151", string number = "NHA-512", string name = "Pell Quimby") =>
        new BoardPaper(0, "Travel Passport", new[] { F(ClueCategory.Name, name), F(ClueCategory.CitizenId, number), F(ClueCategory.BirthDate, "4 Jul 2124"), F(ClueCategory.Expiry, expiry) });

    private static BoardPaper Ticket(string departure = "14 Mar 2150", string destination = "Periclean Athens (Ancient)", string born = "4 Jul 2124") =>
        new BoardPaper(1, "Entry Ticket", new[] { F(ClueCategory.Name, "Pell Quimby"), F(ClueCategory.Destination, destination), F(ClueCategory.DepartureDate, departure), F(ClueCategory.BirthDate, born) });

    private static CitizenRecord Record(string born = "4 Jul 2124", string number = "NHA-512") =>
        new CitizenRecord("Pell Quimby", number, new[]
        {
            new RecordGroup("RECORDS", new[]
            {
                new RecordRow("Name", "Pell Quimby", ClueCategory.Name), new RecordRow("Citizen ID", number, ClueCategory.CitizenId),
                new RecordRow("Born", born, ClueCategory.BirthDate), new RecordRow("Status", "Standard", ClueCategory.AccountStatus)
            }),
            new RecordGroup("TRAVEL", new[] { new RecordRow("Booked departure", "Periclean Athens (Ancient)", ClueCategory.Destination), new RecordRow("Note", "No remarks.") })
        });

    private static Directive Closure(string nation, string era) => new Directive(TravelRuleType.NationEraForbidden, Array.Empty<TravellerKind>(), nation, era);

    [Test]
    public void Lookup_ByNumberFirst_ThenName_ElseNoRecord_NothingWithoutAnIdOrName()
    {
        var registry = new[] { Record(number: "AAA-001"), new CitizenRecord("Someone Else", "NHA-512", null) };
        (CitizenRecord rec, LookupBy by, string query) = RecordLookup.Find(registry, new[] { Passport() });
        Assert.AreEqual("Someone Else", rec.FullName, "the number wins");
        Assert.AreEqual(LookupBy.Number, by);
        Assert.AreEqual("NHA-512", query);

        (rec, by, _) = RecordLookup.Find(new[] { Record(number: "AAA-001") }, new[] { Passport() });
        Assert.AreEqual(LookupBy.Name, by, "no record holds the number: the name");
        Assert.AreEqual("Pell Quimby", rec.FullName);

        (rec, by, query) = RecordLookup.Find(new[] { new CitizenRecord("Other", "X-1", null) }, new[] { Passport() });
        Assert.IsNull(rec);
        Assert.AreEqual(LookupBy.NoRecord, by);
        Assert.AreEqual("NHA-512", query, "the plate names the number looked up");

        Assert.AreEqual(LookupBy.Nothing, RecordLookup.Find(registry, new[] { new BoardPaper(0, "Stub", new[] { F(ClueCategory.Expiry, "1 Jan 2151") }) }).By);
        Assert.AreEqual(LookupBy.Nothing, RecordLookup.Find(registry, null).By);
    }

    [Test]
    public void RulesCheck_Destination_ValidOrClosed_TheClosingRule_TheScannedBox()
    {
        var rules = new[] { new Directive(TravelRuleType.PaperDates, Array.Empty<TravellerKind>()), Closure("egypt", "ancient"), Closure("greece", "ancient") };
        List<RuleCheckRow> rows = RulesCheck.Rows(rules, TravellerKind.PoorTourist, "greece", "ancient", "Periclean Athens (Ancient)", new[] { Passport(), Ticket() }, null, Today);
        RuleCheckRow dest = rows.Single(r => r.Kind == RuleCheckKind.Destination);
        Assert.AreEqual(RuleChip.Closed, dest.Chip);
        Assert.AreEqual(2, dest.Rule, "the rule that closes it");
        Assert.AreEqual(1, dest.Document);
        Assert.AreEqual(1, dest.Field);
        Assert.IsTrue(dest.Loggable);

        dest = RulesCheck.Rows(rules, TravellerKind.PoorTourist, "italy", "ancient", "Republican Rome (Ancient)", new[] { Passport() }, null, Today).Single(r => r.Kind == RuleCheckKind.Destination);
        Assert.AreEqual(RuleChip.Valid, dest.Chip);
        Assert.AreEqual(1, dest.Rule, "the first closure read");
        Assert.AreEqual(-1, dest.Document, "no scanned box names the destination");
        Assert.AreEqual("Republican Rome (Ancient)", dest.Value, "the claim is shown");
        Assert.IsFalse(dest.Loggable);

        var labourOnly = new[] { new Directive(TravelRuleType.NationEraForbidden, new[] { TravellerKind.Labourer }, "greece", "ancient") };
        Assert.IsFalse(RulesCheck.Rows(labourOnly, TravellerKind.Displaced, "greece", "ancient", "x", null, null, Today).Any(), "a closure not read for the kind says nothing");
    }

    [Test]
    public void RulesCheck_Dates_ValidExpiredOrWrongDate_OnlyWhileTheDatesRuleStands()
    {
        var rules = new[] { new Directive(TravelRuleType.PaperDates, Array.Empty<TravellerKind>()) };
        List<RuleCheckRow> rows = RulesCheck.Rows(rules, TravellerKind.RichTourist, "greece", "ancient", "x", new[] { Passport(expiry: "13 Mar 2150"), Ticket(departure: "15 Mar 2150") }, null, Today);
        Assert.AreEqual(2, rows.Count);
        Assert.AreEqual(RuleChip.Expired, rows[0].Chip);
        Assert.AreEqual(ClueCategory.Expiry, rows[0].Category);
        Assert.AreEqual(RuleChip.WrongDate, rows[1].Chip, "a departure on another day");
        Assert.IsTrue(rows.All(r => r.Loggable && r.Rule == 0));

        rows = RulesCheck.Rows(rules, TravellerKind.RichTourist, "greece", "ancient", "x", new[] { Passport(expiry: "14 Mar 2150"), Ticket() }, null, Today);
        Assert.IsTrue(rows.All(r => r.Chip == RuleChip.Valid), "valid through today, departing today");

        Assert.IsEmpty(RulesCheck.Rows(new Directive[0], TravellerKind.RichTourist, "greece", "ancient", "x", new[] { Passport(expiry: "1 Jan 2100") }, null, Today), "no dates rule today: no date is judged");
        Assert.IsEmpty(RulesCheck.Rows(rules, TravellerKind.RichTourist, "greece", "ancient", "x", new[] { Passport(expiry: "1 Jan 2100") }, null, null), "no calendar: nothing judged");
    }

    [Test]
    public void RulesCheck_RequiredPapers_ValidOrMissing_WithTheRequestToFlag()
    {
        var rules = new[] { new Directive(TravelRuleType.PaperSet, new[] { TravellerKind.PoorTourist }) };
        List<RuleCheckRow> rows = RulesCheck.Rows(rules, TravellerKind.PoorTourist, "greece", "ancient", "x", null,
                                                  new[] { new RequiredPaper("TC-230", "Entry Ticket", true), new RequiredPaper("proof", "Proof of means", false) }, Today);
        Assert.AreEqual(new[] { RuleChip.Valid, RuleChip.Missing }, rows.Select(r => r.Chip).ToArray());
        Assert.AreEqual("proof", rows[1].RequestId);
        Assert.AreEqual(0, rows[1].Rule, "the paper-set rule");
        Assert.IsTrue(rows[1].Failing);
        Assert.IsFalse(rows[1].Loggable, "a missing paper is flagged, not held against a value");
        Assert.AreEqual(-1, RulesCheck.Rows(null, TravellerKind.Labourer, "a", "b", "x", null, new[] { new RequiredPaper("TC-230", "Entry Ticket", false) }, Today)[0].Rule);
    }

    private static CrossColumn Column(BoardPaper paper) =>
        new CrossColumn(paper.Name, false, paper.Document, paper.Fields.Select((f, i) => new CrossValue(f.category, i, f.label, f.value)));

    [Test]
    public void CrossCheck_AnHonestSetNeverGlows_RowsOnlyForSharedComparedDetails()
    {
        List<CrossRow> rows = CrossCheck.Rows(new[] { Column(Passport()), Column(Ticket()), new CrossColumn("Record", true, -1, RecordLookup.RecordValues(Record())) });
        CollectionAssert.AreEqual(new[] { ClueCategory.Name, ClueCategory.CitizenId, ClueCategory.BirthDate, ClueCategory.Destination }, rows.Select(r => r.Category).ToArray(),
                                  "in order of first appearance; the dates are read against today, never across; a detail one source states has no row");
        Assert.IsFalse(rows.Any(r => r.Mismatch));
        Assert.IsNull(rows.Single(r => r.Category == ClueCategory.CitizenId).Cells[1].Value, "the ticket states no Citizen ID");
    }

    [Test]
    public void CrossCheck_APaperAgainstTheRecord_GlowsWithTheRecordAsPartner()
    {
        List<CrossRow> rows = CrossCheck.Rows(new[] { Column(Passport()), Column(Ticket(born: "4 Jul 2120")), new CrossColumn("Record", true, -1, RecordLookup.RecordValues(Record())) });
        CrossRow born = rows.Single(r => r.Category == ClueCategory.BirthDate);
        Assert.IsTrue(born.Mismatch);
        Assert.IsFalse(born.Cells[0].Glows, "the passport agrees with the record");
        Assert.IsTrue(born.Cells[1].Glows);
        Assert.AreEqual(2, born.Cells[1].Partner, "logged against the record");
        Assert.IsTrue(born.Cells[2].Glows, "the record's cell glows too");
        Assert.AreEqual(1, born.Cells[2].Partner, "against the first paper that differs");
        Assert.AreEqual(2, born.Cells[2].Value.Value.Index, "the record's flat row index");
        Assert.IsFalse(rows.Where(r => r.Category != ClueCategory.BirthDate).Any(r => r.Mismatch), "only the real difference");
    }

    [Test]
    public void CrossCheck_WithoutTheRecord_BothPapersGlow_EachAgainstTheOther_CaseAndSpaceNeverDiffer()
    {
        List<CrossRow> rows = CrossCheck.Rows(new[] { Column(Passport()), Column(Ticket(born: "4 jul 2124 ")) });
        Assert.IsFalse(rows.Any(r => r.Mismatch), "the same value in another case or with a space is the same value");

        rows = CrossCheck.Rows(new[] { Column(Passport()), Column(Ticket(born: "5 Jul 2124")) });
        CrossRow born = rows.Single(r => r.Category == ClueCategory.BirthDate);
        Assert.IsTrue(born.Cells[0].Glows && born.Cells[1].Glows);
        Assert.AreEqual(1, born.Cells[0].Partner);
        Assert.AreEqual(0, born.Cells[1].Partner);
        Assert.IsEmpty(CrossCheck.Rows(null));
    }
}
