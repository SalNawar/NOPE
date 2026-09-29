using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The Analysis Scanner's rule (the PC redesign SC4, SC5; traveller types L4):
/// among the scanned papers, the first pair of fields of one compared category
/// on two documents whose values differ (PaperChecks.Contradictions, the one
/// rule the cross proof reads too), in document order then field order,
/// skipping the categories the Deviation Report holds; papers against papers
/// only, one pair per pass, and never a directive-only date or a name.
/// </summary>
public class PaperAnalysisTests
{
    private static DocumentField F(ClueCategory category, string value) => new DocumentField { category = category, label = category.ToString(), value = value };

    private static CaseDocument Doc(string name, params DocumentField[] fields) => new CaseDocument { name = name, fields = fields, handOver = DocumentHandOver.OnArrival };

    /// <summary>A case of <paramref name="count"/> papers with the given ones scanned.</summary>
    private static CasePapers Scanned(int count, params int[] scanned)
    {
        var papers = new CasePapers(count);
        foreach (int i in scanned)
            Assert.IsTrue(papers.Scan(i));
        return papers;
    }

    private static readonly HashSet<ClueCategory> Nothing = new HashSet<ClueCategory>();

    /// <summary>
    /// A rich tourist's set: the visa and the manifest agree on the Citizen ID;
    /// the waiver's Transponder disagrees with the manifest's (a traveller
    /// types L3 fake waiver, the spec's §2.4 sample) and its debt is printed
    /// nowhere else.
    /// </summary>
    private static CaseDocument[] Set() => new[]
    {
        Doc("Leisure Departure Visa", F(ClueCategory.CitizenId, "552-1804-33"), F(ClueCategory.Destination, "Periclean Athens"), F(ClueCategory.AccountStatus, "Premium"), F(ClueCategory.Expiry, "2 Apr 2150")),
        Doc("Departure Manifest", F(ClueCategory.CitizenId, "552-1804-33"), F(ClueCategory.DepartureDate, "14 Mar 2150"), F(ClueCategory.TransponderId, "Hopper Mk II · HP-40718"), F(ClueCategory.TransponderClass, "Premium")),
        Doc("Stranding Waiver", F(ClueCategory.CitizenId, "552-1804-33"), F(ClueCategory.TransponderId, "Hopper Mk I · HP-11952"), F(ClueCategory.Debt, "9,800 cr"))
    };

    private static void AssertMark(AnalysisMark? mark, int docA, int fieldA, int docB, int fieldB)
    {
        Assert.IsTrue(mark.HasValue, "a pair is marked");
        Assert.AreEqual((docA, fieldA, docB, fieldB), (mark.Value.DocA, mark.Value.FieldA, mark.Value.DocB, mark.Value.FieldB));
    }

    [Test]
    public void First_MarksTheDisagreeingPair_ByTheDocumentsFieldIndices()
    {
        AssertMark(PaperAnalysis.First(Set(), Scanned(3, 0, 1, 2), Nothing), 1, 2, 2, 1);
    }

    [Test]
    public void First_ReadsOnlyTheScannedPapers()
    {
        Assert.IsNull(PaperAnalysis.First(Set(), Scanned(3, 0, 1), Nothing), "the visa and the manifest agree");
        Assert.IsNull(PaperAnalysis.First(Set(), Scanned(3, 2), Nothing), "one paper contradicts nothing");
        Assert.IsNull(PaperAnalysis.First(Set(), Scanned(3), Nothing), "nothing scanned");
        Assert.IsNull(PaperAnalysis.First(Set(), null, Nothing), "no papers: nothing is scanned");
        AssertMark(PaperAnalysis.First(Set(), Scanned(3, 1, 2), Nothing), 1, 2, 2, 1);
    }

    [Test]
    public void First_SkipsADocumentedCategory_ForTheNextUndocumentedPair()
    {
        CaseDocument[] docs = Set();
        docs[2].fields[0].value = "552-1804-34";
        AssertMark(PaperAnalysis.First(docs, Scanned(3, 0, 1, 2), Nothing), 0, 0, 2, 0);
        AssertMark(PaperAnalysis.First(docs, Scanned(3, 0, 1, 2), new HashSet<ClueCategory> { ClueCategory.CitizenId }), 1, 2, 2, 1);
        Assert.IsNull(PaperAnalysis.First(docs, Scanned(3, 0, 1, 2), new HashSet<ClueCategory> { ClueCategory.CitizenId, ClueCategory.TransponderId }));
        AssertMark(PaperAnalysis.First(docs, Scanned(3, 0, 1, 2), null), 0, 0, 2, 0);
    }

    [Test]
    public void First_MarksOnePair_TheFirst_InDocumentOrderThenFieldOrder()
    {
        var docs = new[]
        {
            Doc("A", F(ClueCategory.AccountStatus, "Premium"), F(ClueCategory.CitizenId, "1")),
            Doc("B", F(ClueCategory.CitizenId, "2"), F(ClueCategory.AccountStatus, "Standard")),
            Doc("C", F(ClueCategory.CitizenId, "3"))
        };
        List<AnalysisMark> all = PaperAnalysis.Contradictions(docs, null);
        CollectionAssert.AreEqual(new[] { (0, 0, 1, 1), (0, 1, 1, 0), (0, 1, 2, 0), (1, 0, 2, 0) },
                                  all.Select(m => (m.DocA, m.FieldA, m.DocB, m.FieldB)).ToList(),
                                  "document A's fields in order, each against the later documents; then B against C");
        AssertMark(PaperAnalysis.First(docs, Scanned(3, 0, 1, 2), Nothing), 0, 0, 1, 1);
    }

    [Test]
    public void AnHonestSet_HasNoContradiction()
    {
        CaseDocument[] docs = Set();
        docs[2].fields[1].value = "hopper mk ii · hp-40718 ";
        CollectionAssert.IsEmpty(PaperAnalysis.Contradictions(docs, null), "the same value, matched as every value is (ValuesMatch): case and spaces aside");
        Assert.IsNull(PaperAnalysis.First(docs, Scanned(3, 0, 1, 2), Nothing));
    }

    [Test]
    public void DirectiveOnlyDates_AndNames_AreNeverCompared_TheOneRule()
    {
        var docs = new[]
        {
            Doc("Visa", F(ClueCategory.Expiry, "2 Apr 2150"), F(ClueCategory.DepartureDate, "14 Mar 2150"), F(ClueCategory.Name, "Mara"), F(ClueCategory.Signature, "Mara")),
            Doc("Manifest", F(ClueCategory.Expiry, "9 Apr 2150"), F(ClueCategory.DepartureDate, "15 Mar 2150"), F(ClueCategory.Name, "Nebamun"), F(ClueCategory.Signature, ""))
        };
        CollectionAssert.IsEmpty(PaperAnalysis.Contradictions(docs, null));
        Assert.IsFalse(PaperChecks.IsCompared(ClueCategory.DepartureDate));
        Assert.IsFalse(PaperChecks.IsCompared(ClueCategory.Expiry));
        Assert.IsFalse(PaperChecks.IsCompared(ClueCategory.Signature), "a waiver's signature is read against the paper-set directive (phase 8)");
        Assert.IsFalse(PaperChecks.IsCompared(ClueCategory.Name), "never a tell (Forgery), so never a pair");
        foreach (ClueCategory category in System.Enum.GetValues(typeof(ClueCategory)))
            if (!Forgery.IsDirectiveOnly(category) && category != ClueCategory.Name)
                Assert.IsTrue(PaperChecks.IsCompared(category), category.ToString());
    }

    /// <summary>The scanner marks exactly the pairs the cross proof accepts: the same rule, the same pairs, in the same order.</summary>
    [Test]
    public void Contradictions_AreTheCrossProofsPairs()
    {
        CaseDocument[] docs = Set();
        docs[2].fields[0].value = "552-1804-34";
        List<PaperContradiction> checks = PaperChecks.Contradictions(docs.Select(d => d.fields).ToList());
        CollectionAssert.AreEqual(checks.Select(c => (c.DocumentA, c.FieldA, c.DocumentB, c.FieldB)).ToList(),
                                  PaperAnalysis.Contradictions(docs, null).Select(m => (m.DocA, m.FieldA, m.DocB, m.FieldB)).ToList());
        Assert.AreEqual(3, checks.Count, "the visa's and the manifest's IDs against the waiver's, and the two transponders");
    }

    [Test]
    public void ABlankValue_StatesNothing()
    {
        var docs = new[] { Doc("A", F(ClueCategory.CitizenId, "1")), Doc("B", F(ClueCategory.CitizenId, " ")), Doc("C", F(ClueCategory.CitizenId, null)) };
        CollectionAssert.IsEmpty(PaperAnalysis.Contradictions(docs, null));
    }

    [Test]
    public void DifferentCategories_AndOneDocument_ProveNothing()
    {
        var docs = new[]
        {
            Doc("A", F(ClueCategory.CitizenId, "1"), F(ClueCategory.CitizenId, "2")),
            Doc("B", F(ClueCategory.Destination, "1"))
        };
        CollectionAssert.IsEmpty(PaperAnalysis.Contradictions(docs, null), "two fields of one document are never a pair; a Citizen ID against a Destination is not compared");
    }

    [Test]
    public void MissingDocumentsAndFields_AreSkipped()
    {
        var docs = new[] { null, Doc("A", F(ClueCategory.CitizenId, "1"), null), new CaseDocument { name = "no fields" }, Doc("B", F(ClueCategory.CitizenId, "2")) };
        List<AnalysisMark> all = PaperAnalysis.Contradictions(docs, null);
        Assert.AreEqual(1, all.Count);
        Assert.AreEqual((1, 0, 3, 0), (all[0].DocA, all[0].FieldA, all[0].DocB, all[0].FieldB));
        CollectionAssert.IsEmpty(PaperAnalysis.Contradictions(null, null));
        Assert.IsNull(PaperAnalysis.First(null, Scanned(0), Nothing));
    }

    [Test]
    public void AMark_NamesEachDocumentsFields()
    {
        var mark = new AnalysisMark(1, 2, 2, 1);
        CollectionAssert.AreEqual(new[] { 2 }, mark.FieldsOf(1).ToList());
        CollectionAssert.AreEqual(new[] { 1 }, mark.FieldsOf(2).ToList());
        CollectionAssert.IsEmpty(mark.FieldsOf(0).ToList());
    }
}
