using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The paper cross check (traveller types L4, §6.2): two papers that disagree
/// on one compared category. The fixture is a rich tourist's set, TC-101
/// (Name, Citizen ID, Date of Birth, Destination, Visa Class, Valid Until)
/// and TC-230 (Citizen ID, Transponder, Transponder Class, Currency Carried,
/// Declared Effects, Departure), honest unless a test forges a field.
/// </summary>
public class PaperChecksTests
{
    private static DocumentField F(ClueCategory category, string value) =>
        new DocumentField { category = category, label = category.ToString(), value = value };

    private static List<DocumentField> Visa(string citizenId = "418-0937-52", string status = "Premium", string expiry = "27 Mar 2150") => new List<DocumentField>
    {
        F(ClueCategory.Name, "Mara"),
        F(ClueCategory.CitizenId, citizenId),
        F(ClueCategory.BirthDate, "3 Jun 2101"),
        F(ClueCategory.Destination, "New Kingdom Egypt (Ancient)"),
        F(ClueCategory.AccountStatus, status),
        F(ClueCategory.Expiry, expiry)
    };

    private static List<DocumentField> Manifest(string citizenId = "418-0937-52", string currency = "Deben", string departure = "14 Mar 2150") => new List<DocumentField>
    {
        F(ClueCategory.CitizenId, citizenId),
        F(ClueCategory.TransponderId, "Hopper Mk II · HP-40718"),
        F(ClueCategory.TransponderClass, "Premium"),
        F(ClueCategory.Currency, currency),
        F(ClueCategory.Technology, "Papyrus"),
        F(ClueCategory.DepartureDate, departure)
    };

    private static List<PaperContradiction> Check(params IReadOnlyList<DocumentField>[] documents) => PaperChecks.Contradictions(documents);

    [Test]
    public void AnHonestSet_HasNoContradiction()
    {
        CollectionAssert.IsEmpty(Check(Visa(), Manifest()));
    }

    [Test]
    public void TheSameCategoryOnTwoDocuments_WithValuesThatDiffer_Contradicts()
    {
        List<PaperContradiction> found = Check(Visa(), Manifest(citizenId: "552-1804-33"));
        Assert.AreEqual(1, found.Count);
        PaperContradiction c = found[0];
        Assert.AreEqual(ClueCategory.CitizenId, c.Category);
        Assert.AreEqual(0, c.DocumentA);
        Assert.AreEqual(1, c.FieldA, "the visa's Citizen ID box");
        Assert.AreEqual(1, c.DocumentB);
        Assert.AreEqual(0, c.FieldB, "the manifest's Citizen ID box");
    }

    [Test]
    public void ValuesAreHeldEqual_AsEverywhere_TrimmedAndIgnoringCase()
    {
        CollectionAssert.IsEmpty(Check(Visa(citizenId: " 418-0937-52 "), Manifest(citizenId: "418-0937-52")));
        CollectionAssert.IsEmpty(Check(Visa(status: "PREMIUM"), new List<DocumentField> { F(ClueCategory.AccountStatus, "premium") }));
    }

    [Test]
    public void ADirectiveOnlyCategory_IsSkipped()
    {
        // A second dated paper whose dates differ: the calendar reads them, the papers never contradict each other.
        var dated = new List<DocumentField> { F(ClueCategory.Expiry, "1 Jan 2150"), F(ClueCategory.DepartureDate, "9 Sep 2149") };
        CollectionAssert.IsEmpty(Check(Visa(), Manifest(), dated));
    }

    [Test]
    public void AName_IsNeverCompared()
    {
        CollectionAssert.IsEmpty(Check(Visa(), new List<DocumentField> { F(ClueCategory.Name, "Somebody Else") }));
    }

    [Test]
    public void OneDocument_NeverContradictsItself()
    {
        var twice = new List<DocumentField> { F(ClueCategory.Currency, "Deben"), F(ClueCategory.Currency, "Denarius") };
        CollectionAssert.IsEmpty(Check(twice));
        CollectionAssert.IsEmpty(Check(twice, Manifest()).Where(c => c.DocumentA == c.DocumentB));
    }

    [Test]
    public void Contradictions_ComeInDocumentOrder_ThenFieldOrder()
    {
        // A declaration (a third paper) whose coin and device differ from the manifest's, and a borrowed manifest.
        var declaration = new List<DocumentField> { F(ClueCategory.Currency, "Silver shekel"), F(ClueCategory.Technology, "Cylinder seal") };
        List<PaperContradiction> found = Check(Visa(), Manifest(citizenId: "552-1804-33"), declaration);

        CollectionAssert.AreEqual(
            new[] { (0, 1, 1, 0, ClueCategory.CitizenId), (1, 3, 2, 0, ClueCategory.Currency), (1, 4, 2, 1, ClueCategory.Technology) },
            found.Select(c => (c.DocumentA, c.FieldA, c.DocumentB, c.FieldB, c.Category)).ToArray());
    }

    [Test]
    public void EveryPairOfContradictingBoxes_IsListed()
    {
        // Three papers that all print the Citizen ID, one forged: the forged one contradicts each of the other two.
        var waiver = new List<DocumentField> { F(ClueCategory.CitizenId, "418-0937-52") };
        List<PaperContradiction> found = Check(Visa(), Manifest(citizenId: "552-1804-33"), waiver);
        CollectionAssert.AreEqual(new[] { (0, 1), (1, 2) }, found.Select(c => (c.DocumentA, c.DocumentB)).ToArray());
    }

    /// <summary>A blank box states nothing (the Analysis Scanner's rule, phase 22, now the one rule's).</summary>
    [Test]
    public void ABlankValue_StatesNothing()
    {
        var blank = new List<DocumentField> { F(ClueCategory.CitizenId, " "), F(ClueCategory.AccountStatus, null) };
        CollectionAssert.IsEmpty(Check(Visa(), blank));
    }

    [Test]
    public void NullDocumentsAndFields_AreSkipped()
    {
        CollectionAssert.IsEmpty(PaperChecks.Contradictions(null));
        var holed = new List<DocumentField> { null, F(ClueCategory.CitizenId, "552-1804-33") };
        List<PaperContradiction> found = PaperChecks.Contradictions(new IReadOnlyList<DocumentField>[] { Visa(), null, holed });
        Assert.AreEqual(1, found.Count);
        Assert.AreEqual((0, 1, 2, 1), (found[0].DocumentA, found[0].FieldA, found[0].DocumentB, found[0].FieldB));
    }

    [TestCase(ClueCategory.CitizenId, "a", ClueCategory.CitizenId, "b", true)]
    [TestCase(ClueCategory.CitizenId, "a", ClueCategory.CitizenId, " A ", false)]
    [TestCase(ClueCategory.CitizenId, "a", ClueCategory.CitizenId, " ", false)]
    [TestCase(ClueCategory.CitizenId, null, ClueCategory.CitizenId, "b", false)]
    [TestCase(ClueCategory.CitizenId, "a", ClueCategory.AccountStatus, "b", false)]
    [TestCase(ClueCategory.Expiry, "a", ClueCategory.Expiry, "b", false)]
    [TestCase(ClueCategory.DepartureDate, "a", ClueCategory.DepartureDate, "b", false)]
    [TestCase(ClueCategory.Name, "a", ClueCategory.Name, "b", false)]
    [TestCase(ClueCategory.Currency, "a", ClueCategory.Currency, "b", true)]
    [TestCase(ClueCategory.BirthDate, "a", ClueCategory.BirthDate, "b", true)]
    public void Contradict_IsTheOneRule_TheProofAndTheScannerRead(ClueCategory a, string x, ClueCategory b, string y, bool expected)
    {
        Assert.AreEqual(expected, PaperChecks.Contradict(a, x, b, y));
    }

    [Test]
    public void IsCompared_EverythingButANameTheDirectiveOnlyDatesAndTheVisualChecks()
    {
        foreach (ClueCategory category in (ClueCategory[])System.Enum.GetValues(typeof(ClueCategory)))
        {
            bool expected = category != ClueCategory.Name && !Forgery.IsDirectiveOnly(category) && category != ClueCategory.Seal && category != ClueCategory.Photo;
            Assert.AreEqual(expected, PaperChecks.IsCompared(category), category.ToString());
        }
    }
}
