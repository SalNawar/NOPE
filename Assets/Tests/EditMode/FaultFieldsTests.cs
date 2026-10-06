using System.Collections.Generic;
using NUnit.Framework;

/// <summary>The cheat menu's "reveal faults": the boxes that show what is wrong with a traveller's papers.</summary>
public class FaultFieldsTests
{
    private static DocumentField F(ClueCategory category, string value, bool anachronism = false) =>
        new DocumentField { category = category, label = category.ToString(), value = value, isAnachronism = anachronism };

    private static IReadOnlyList<IReadOnlyList<DocumentField>> Papers() => new List<IReadOnlyList<DocumentField>>
    {
        new List<DocumentField> { F(ClueCategory.Name, "Ada Vey"), F(ClueCategory.CitizenId, "KTR-418"), F(ClueCategory.Currency, "Florin", true) },
        new List<DocumentField> { F(ClueCategory.CitizenId, "KTR-481"), F(ClueCategory.CitizenId, "KTR-999"), F(ClueCategory.Signature, "UNSIGNED") },
    };

    [Test]
    public void Of_MarksTheForgersBox_TheAnachronisms_AndTheCitedValues_InPaperOrder()
    {
        var tells = new[] { new RecordTell(1, ClueCategory.CitizenId, "KTR-999") };
        var cited = new[] { new CitationValue("Signature on the Stranding Waiver", "unsigned "), new CitationValue("Nothing", "  ") };
        List<(int, int)> found = FaultFields.Of(Papers(), tells, cited);
        CollectionAssert.AreEqual(new[] { (0, 2), (1, 1), (1, 2) }, found,
                                  "the anachronism, the forged box printing the tell's value (not the first of its category), the cited value (case and spaces ignored)");
    }

    [Test]
    public void Of_AnHonestTraveller_MarksNothing_AndBadInputIsSafe()
    {
        CollectionAssert.IsEmpty(FaultFields.Of(new List<IReadOnlyList<DocumentField>> { new List<DocumentField> { F(ClueCategory.Name, "Ada Vey") } }, null, null));
        CollectionAssert.IsEmpty(FaultFields.Of(null, null, null));
        var outside = new[] { new RecordTell(7, ClueCategory.CitizenId, "X"), new RecordTell(0, ClueCategory.Seal, "X") };
        CollectionAssert.IsEmpty(FaultFields.Of(new List<IReadOnlyList<DocumentField>> { new List<DocumentField> { F(ClueCategory.Name, "Ada Vey") } }, outside, null),
                                 "a tell on no paper, or of a category the paper does not print, marks nothing");
    }

    [Test]
    public void Of_ATellWhoseValueNoBoxPrints_MarksTheFirstBoxOfItsCategory()
    {
        List<(int, int)> found = FaultFields.Of(Papers(), new[] { new RecordTell(1, ClueCategory.CitizenId, "KTR-000") }, null);
        CollectionAssert.Contains(found, (1, 0));
    }
}
