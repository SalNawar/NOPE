using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// Smart links (the PC redesign LK1, CM2, §4.3): every mapped row of the
/// table, the claimed row's key, the record lookup (the paper's Citizen ID,
/// else its Name), the answers' case lookup, the directive-only dates, the
/// categories with no target, and ForKey of each pick-key kind (a paper held
/// at the desk links nowhere). The pick keys read back as they were written.
/// </summary>
public class SmartLinksTests
{
    private static readonly CaseClaim Athens = new CaseClaim("greece", "ancient");

    private static DocumentField F(ClueCategory c, string value) => new DocumentField { category = c, label = c.ToString(), value = value };

    private static readonly ClueCategory[] PlaceFacts =
    {
        ClueCategory.Currency, ClueCategory.Language, ClueCategory.Technology, ClueCategory.Geography, ClueCategory.Politics,
        ClueCategory.Material, ClueCategory.Culture
    };

    private static readonly ClueCategory[] RecordRows =
    {
        ClueCategory.Name, ClueCategory.BirthDate, ClueCategory.CitizenId, ClueCategory.Destination, ClueCategory.Incident
    };

    private static readonly ClueCategory[] DirectiveOnly = { ClueCategory.DepartureDate, ClueCategory.Expiry };

    [TestCase(ClueCategory.Currency)]
    [TestCase(ClueCategory.Language)]
    [TestCase(ClueCategory.Technology)]
    [TestCase(ClueCategory.Geography)]
    [TestCase(ClueCategory.Politics)]
    [TestCase(ClueCategory.Material)]
    [TestCase(ClueCategory.Culture)]
    public void PlaceFactField_GoesToItsBooksClaimedRow(ClueCategory c)
    {
        DocumentField field = F(c, "Drachma");
        LinkTarget link = SmartLinks.ForField(field, new[] { field }, Athens);
        Assert.AreEqual(LinkTarget.ToRow(AppTab.Reference, PickKeys.BookRow(c, "greece", "ancient")), link);
    }

    [Test]
    public void CultureField_GoesToTheCostumeGuidesClaimedRow()
    {
        DocumentField field = F(Looks.EvidenceCategory, "Chiton");
        Assert.AreEqual("book:Culture:greece:ancient", SmartLinks.ForField(field, new[] { field }, Athens).Key);
    }

    [Test]
    public void PlaceFact_WithoutAClaim_LinksNowhere()
    {
        DocumentField field = F(ClueCategory.Currency, "Drachma");
        Assert.IsTrue(SmartLinks.ForField(field, new[] { field }, new CaseClaim(null, "ancient")).IsNone);
        Assert.IsTrue(SmartLinks.ForField(field, new[] { field }, default).IsNone);
    }

    [TestCase(ClueCategory.Name)]
    [TestCase(ClueCategory.BirthDate)]
    [TestCase(ClueCategory.CitizenId)]
    [TestCase(ClueCategory.Destination)]
    [TestCase(ClueCategory.Incident)]
    public void RecordField_GoesToTheRecordOfThePapersCitizenId_AtItsRow(ClueCategory c)
    {
        DocumentField field = F(c, "value");
        var paper = new[] { F(ClueCategory.Name, "Oren Hale"), F(ClueCategory.CitizenId, " 552-1804-33 "), field };
        Assert.AreEqual(LinkTarget.ToRecords("552-1804-33", c), SmartLinks.ForField(field, paper, Athens));
    }

    [Test]
    public void NameField_WithoutACitizenId_LooksUpTheName()
    {
        DocumentField name = F(ClueCategory.Name, "Oren Hale");
        DocumentField born = F(ClueCategory.BirthDate, "2 Feb 2117");
        var paper = new[] { F(ClueCategory.CitizenId, "  "), name, born };
        Assert.AreEqual(LinkTarget.ToRecords("Oren Hale", ClueCategory.Name), SmartLinks.ForField(name, paper, Athens));
        Assert.AreEqual(LinkTarget.ToRecords("Oren Hale", ClueCategory.BirthDate), SmartLinks.ForField(born, paper, Athens),
                        "Date of Birth uses the paper's Name");
    }

    [Test]
    public void RecordField_OnAPaperWithNoIdAndNoName_LinksNowhere()
    {
        DocumentField born = F(ClueCategory.BirthDate, "2 Feb 2117");
        Assert.IsTrue(SmartLinks.ForField(born, new[] { born }, Athens).IsNone);
    }

    [TestCase(ClueCategory.DepartureDate)]
    [TestCase(ClueCategory.Expiry)]
    public void DirectiveOnlyDates_GoToTheRules(ClueCategory c)
    {
        DocumentField field = F(c, "9 Jun 2150");
        Assert.AreEqual(LinkTarget.ToTab(AppTab.Rules), SmartLinks.ForField(field, new[] { field }, Athens));
    }

    [Test]
    public void EveryCategory_IsMappedOrLinksNowhere()
    {
        // A later ClueCategory links nowhere until it is mapped (no ↗ is drawn); today every one is mapped.
        var mapped = new HashSet<ClueCategory>(PlaceFacts.Concat(RecordRows).Concat(DirectiveOnly));
        foreach (ClueCategory c in Enum.GetValues(typeof(ClueCategory)).Cast<ClueCategory>())
        {
            DocumentField field = F(c, "v");
            var paper = new[] { F(ClueCategory.CitizenId, "552-1804-33"), field };
            Assert.AreEqual(mapped.Contains(c), !SmartLinks.ForField(field, paper, Athens).IsNone, c.ToString());
        }
        Assert.IsTrue(SmartLinks.ForField(F((ClueCategory)999, "v"), Array.Empty<DocumentField>(), Athens).IsNone);
        Assert.IsTrue(SmartLinks.ForField(null, Array.Empty<DocumentField>(), Athens).IsNone);
    }

    [Test]
    public void Answer_GoesWhereAFieldOfItsCategoryGoes()
    {
        Assert.AreEqual(LinkTarget.ToRow(AppTab.Reference, "book:Currency:greece:ancient"), SmartLinks.ForAnswer(ClueCategory.Currency, Athens, "552-1804-33"));
        Assert.AreEqual(LinkTarget.ToRecords("552-1804-33", ClueCategory.Name), SmartLinks.ForAnswer(ClueCategory.Name, Athens, "552-1804-33"));
        Assert.AreEqual(LinkTarget.ToRecords("552-1804-33", ClueCategory.BirthDate), SmartLinks.ForAnswer(ClueCategory.BirthDate, Athens, "552-1804-33"));
        Assert.IsTrue(SmartLinks.ForAnswer(ClueCategory.BirthDate, Athens, null).IsNone);
    }

    [Test]
    public void CaseLookup_IsThePrimaryPapersCitizenId_ElseItsName_ElseTheTraveller()
    {
        var noIdentity = new[] { F(ClueCategory.Currency, "Drachma") };
        var named = new[] { F(ClueCategory.Name, "Oren Hale") };
        var identified = new[] { F(ClueCategory.Name, "Oren Hale"), F(ClueCategory.CitizenId, "552-1804-33") };
        Assert.AreEqual("552-1804-33", SmartLinks.CaseLookup(new[] { noIdentity, identified, named }, "Aster Vale"));
        Assert.AreEqual("Oren Hale", SmartLinks.CaseLookup(new[] { noIdentity, named, identified }, "Aster Vale"));
        Assert.AreEqual("Aster Vale", SmartLinks.CaseLookup(new[] { noIdentity }, " Aster Vale "));
        Assert.IsNull(SmartLinks.CaseLookup(null, " "));
    }

    [Test]
    public void ForKey_AScannedPapersField_GoesToThatRow_AHeldOneNowhere()
    {
        var papers = new CasePapers(2);
        papers.Scan(0);
        papers.HandOver(1);
        string scanned = PickKeys.Field(0, 3);
        Assert.AreEqual(LinkTarget.ToRow(AppTab.Documents, scanned, 0), SmartLinks.ForKey(scanned, papers));
        Assert.IsTrue(SmartLinks.ForKey(PickKeys.Field(1, 0), papers).IsNone, "a paper held at the desk, not scanned, links nowhere");
        Assert.IsTrue(SmartLinks.ForKey(PickKeys.Field(5, 0), papers).IsNone);
        Assert.IsTrue(SmartLinks.ForKey(scanned, null).IsNone);
    }

    [Test]
    public void ForKey_OfEachKind()
    {
        var papers = new CasePapers(1);
        Assert.AreEqual(LinkTarget.ToRow(AppTab.Transcript, "line:7"), SmartLinks.ForKey(PickKeys.Line(7), papers));
        Assert.AreEqual(LinkTarget.ToRow(AppTab.Reference, "book:Currency:greece:ancient"),
                        SmartLinks.ForKey(PickKeys.BookRow(ClueCategory.Currency, "greece", "ancient"), papers));
        Assert.AreEqual(LinkTarget.ToRecords("552-1804-33", ClueCategory.BirthDate),
                        SmartLinks.ForKey(PickKeys.Record(ClueCategory.BirthDate, "552-1804-33"), papers));
        Assert.AreEqual(LinkTarget.ToRecords("Oren Hale", ClueCategory.Name), SmartLinks.ForKey(PickKeys.Record(ClueCategory.Name, "Oren Hale"), papers),
                        "a record without a number is found by its name");
        Assert.IsTrue(SmartLinks.ForKey(PickKeys.Garment(1), papers).IsNone, "a garment is picked at the wheel, not in the app");
        Assert.IsTrue(SmartLinks.ForKey(null, papers).IsNone);
        Assert.IsTrue(SmartLinks.ForKey("", papers).IsNone);
        Assert.IsTrue(SmartLinks.ForKey("book:Nonsense:greece:ancient", papers).IsNone);
    }

    [Test]
    public void PickKeys_ReadBackAsWritten()
    {
        Assert.IsTrue(PickKeys.TryField(PickKeys.Field(2, 11), out int doc, out int field));
        Assert.AreEqual((2, 11), (doc, field));
        Assert.IsTrue(PickKeys.TryLine(PickKeys.Line(40), out int line));
        Assert.AreEqual(40, line);
        Assert.IsTrue(PickKeys.TryBookRow(PickKeys.BookRow(ClueCategory.Culture, "japan", "earlymodern"), out ClueCategory c, out string nation, out string era));
        Assert.AreEqual((ClueCategory.Culture, "japan", "earlymodern"), (c, nation, era));
        Assert.IsTrue(PickKeys.TryRecord(PickKeys.Record(ClueCategory.Incident, "DP-4471-02"), out string id, out ClueCategory rc));
        Assert.AreEqual(("DP-4471-02", ClueCategory.Incident), (id, rc));
    }

    [Test]
    public void PickKeys_RejectOtherKindsAndJunk()
    {
        Assert.IsFalse(PickKeys.TryField(PickKeys.Line(1), out _, out _));
        Assert.IsFalse(PickKeys.TryField("field:1", out _, out _));
        Assert.IsFalse(PickKeys.TryField("field:a:1", out _, out _));
        Assert.IsFalse(PickKeys.TryField("field:-1:1", out _, out _));
        Assert.IsFalse(PickKeys.TryLine("line:", out _));
        Assert.IsFalse(PickKeys.TryBookRow("book:Currency:greece", out _, out _, out _));
        Assert.IsFalse(PickKeys.TryBookRow("book:4:greece:ancient", out _, out _, out _), "a category is written by its name");
        Assert.IsFalse(PickKeys.TryRecord("record::Name", out _, out _));
        Assert.IsFalse(PickKeys.TryRecord("record:552-1804-33:Nonsense", out _, out _));
        Assert.IsFalse(PickKeys.TryRecord(null, out _, out _));
    }

    [Test]
    public void LinkTargets_AreEqualByWhereTheyGo()
    {
        Assert.AreEqual(LinkTarget.ToTab(AppTab.Rules), LinkTarget.ToTab(AppTab.Rules, -5));
        Assert.AreNotEqual(LinkTarget.ToTab(AppTab.Documents, 0), LinkTarget.ToTab(AppTab.Documents, 1));
        Assert.AreNotEqual(LinkTarget.ToTab(AppTab.Documents), LinkTarget.None, "None is not the first tab");
        Assert.AreEqual(LinkTarget.ToRecords(" Oren Hale "), LinkTarget.ToRecords("Oren Hale"));
        Assert.AreNotEqual(LinkTarget.ToRecords("Oren Hale", ClueCategory.Name), LinkTarget.ToRecords("Oren Hale"));
        Assert.IsTrue(LinkTarget.None.IsNone);
        Assert.IsFalse(LinkTarget.ToTab(AppTab.Documents).IsNone);
    }

    [Test]
    public void Split_NeedsTheSidebarAndTwoReadablePanes()
    {
        Assert.IsTrue(AppPanes.CanSplit(1440f, 272f, 520f), "maximised");
        Assert.IsFalse(AppPanes.CanSplit(1120f, 272f, 520f), "the restored window has one pane");
        Assert.IsTrue(AppPanes.CanSplit(1312f, 272f, 520f));
    }

    [Test]
    public void ScrollToMiddle_CentresTheItem_AsNearAsTheEndsAllow()
    {
        Assert.AreEqual(1f, AppPanes.ScrollToMiddle(500f, 700f, 300f, 40f), "content shorter than the window: the top");
        Assert.AreEqual(1f, AppPanes.ScrollToMiddle(1416f, 708f, 100f, 40f), 1e-4f, "near the top: the top");
        Assert.AreEqual(0f, AppPanes.ScrollToMiddle(1416f, 708f, 1380f, 30f), 1e-4f, "near the bottom: the bottom");
        Assert.AreEqual(0.5f, AppPanes.ScrollToMiddle(1416f, 708f, 688f, 40f), 1e-4f, "in the middle: half way");
    }

    [Test]
    public void ANarrowStrip_CollapsesItsInactiveTabs()
    {
        Assert.IsTrue(AppPanes.TabsNarrow(578f, 6, 110f), "a split pane");
        Assert.IsFalse(AppPanes.TabsNarrow(1162f, 6, 110f), "one maximised pane");
        Assert.IsFalse(AppPanes.TabsNarrow(660f, 6, 110f));
    }
}
