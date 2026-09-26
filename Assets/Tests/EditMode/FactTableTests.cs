using System.Linq;
using NUnit.Framework;

/// <summary>
/// Today's facts: two Ancient places. Egypt uses "Deben", Babylonia "Silver shekel";
/// both list their languages. The claim under test is Egypt / Ancient.
/// </summary>
public class FactTableTests
{
    /// <summary>The traveller at the desk (a book row proves a lie whoever the traveller is).</summary>
    private const string Traveller = "Ahmose";

    private static FactTable Today()
    {
        var t = new FactTable();
        t.Add("egypt", "ancient", "New Kingdom Egypt (Ancient)", ClueCategory.Currency, "Deben");
        t.Add("egypt", "ancient", "New Kingdom Egypt (Ancient)", ClueCategory.Language, "Middle Egyptian");
        t.Add("iraq", "ancient", "Babylonia (Ancient)", ClueCategory.Currency, "Silver shekel");
        t.Add("iraq", "ancient", "Babylonia (Ancient)", ClueCategory.Language, "Old Babylonian");
        return t;
    }

    [Test]
    public void Get_ReturnsTheFact_OrNullWhenMissing()
    {
        FactTable t = Today();
        Assert.AreEqual("Deben", t.Get("egypt", "ancient", ClueCategory.Currency));
        Assert.IsNull(t.Get("china", "ancient", ClueCategory.Currency));
        Assert.IsNull(t.Get("egypt", "ancient", ClueCategory.Technology));
    }

    [Test]
    public void Rows_KeepInsertionOrder_AndCarryOriginLabels()
    {
        var rows = Today().Rows(ClueCategory.Currency);
        CollectionAssert.AreEqual(new[] { "Deben", "Silver shekel" }, rows.Select(r => r.Value).ToArray());
        Assert.AreEqual("Babylonia (Ancient)", rows[1].OriginLabel);
        Assert.AreEqual("iraq", rows[1].NationId);
        Assert.AreEqual("ancient", rows[1].EraId);
        Assert.AreEqual(0, Today().Rows(ClueCategory.Politics).Count);
    }

    [Test]
    public void OriginLabel_IsPerPlace()
    {
        Assert.AreEqual("New Kingdom Egypt (Ancient)", Today().OriginLabel("egypt", "ancient"));
        Assert.IsNull(Today().OriginLabel("egypt", "modern"));
    }

    [Test]
    public void Add_IgnoresDuplicatesAndBlankValues()
    {
        FactTable t = Today();
        Assert.IsFalse(t.Add("egypt", "ancient", "x", ClueCategory.Currency, "Other"));
        Assert.IsFalse(t.Add("egypt", "ancient", "x", ClueCategory.Technology, "  "));
        Assert.AreEqual("Deben", t.Get("egypt", "ancient", ClueCategory.Currency));
        Assert.AreEqual(2, t.Rows(ClueCategory.Currency).Count);
    }

    [Test]
    public void ATellFromAnotherPlace_RegistersAsOriginProof_NamingThatPlace()
    {
        FactTable t = Today();
        var tell = new CompareEvidence { kind = EvidenceKind.DocumentField, category = ClueCategory.Currency, value = "Silver shekel", isAnachronism = true };
        FactRow babylon = t.Rows(ClueCategory.Currency)[1];
        Discrepancy d = DiscrepancyLog.Prove(tell, babylon.ToEvidence(), "egypt", "ancient", Traveller);
        Assert.NotNull(d);
        Assert.AreEqual(DiscrepancyProof.ForeignOrigin, d.provedBy);
        Assert.AreEqual("Babylonia (Ancient)", d.actualOrigin);
    }

    [Test]
    public void ATell_RegistersAsMismatch_AgainstTheClaimedPlacesRow()
    {
        FactTable t = Today();
        var tell = new CompareEvidence { kind = EvidenceKind.DocumentField, category = ClueCategory.Currency, value = "Silver shekel", isAnachronism = true };
        FactRow egypt = t.Rows(ClueCategory.Currency)[0];
        Discrepancy d = DiscrepancyLog.Prove(tell, egypt.ToEvidence(), "egypt", "ancient", Traveller);
        Assert.NotNull(d);
        Assert.AreEqual(DiscrepancyProof.ClaimMismatch, d.provedBy);
    }

    [Test]
    public void TryFindOtherPlaceWith_MatchesLikeTheScanner_ButNeverThePlaceItself()
    {
        FactTable t = Today();
        Assert.IsTrue(t.TryFindOtherPlaceWith(ClueCategory.Currency, "egypt", "ancient", "  silver SHEKEL ", out FactRow row),
                      "another case and leading/trailing spaces still match (DiscrepancyLog.ValuesMatch)");
        Assert.AreEqual("iraq", row.NationId);
        Assert.AreEqual("Babylonia (Ancient)", row.OriginLabel);

        Assert.IsFalse(t.TryFindOtherPlaceWith(ClueCategory.Currency, "egypt", "ancient", "Deben", out _), "the place's own row never counts");
        Assert.IsTrue(t.TryFindOtherPlaceWith(ClueCategory.Currency, "iraq", "ancient", "Deben", out _));
    }

    [Test]
    public void TryFindOtherPlaceWith_FalseForInternalSpacing_AnotherCategory_AndBlank()
    {
        FactTable t = Today();
        Assert.IsFalse(t.TryFindOtherPlaceWith(ClueCategory.Currency, "egypt", "ancient", "Silver  shekel", out _), "ValuesMatch only trims and ignores case");
        Assert.IsFalse(t.TryFindOtherPlaceWith(ClueCategory.Language, "egypt", "ancient", "Silver shekel", out _), "another category's equal value");
        Assert.IsFalse(t.TryFindOtherPlaceWith(ClueCategory.Currency, "egypt", "ancient", " ", out FactRow none));
        Assert.IsNull(none.NationId);
    }

    [Test]
    public void MarkChanged_MarksOnlyACellInTheTable()
    {
        FactTable t = Today();
        Assert.IsTrue(t.MarkChanged("egypt", "ancient", ClueCategory.Currency));
        Assert.IsTrue(t.IsChanged("egypt", "ancient", ClueCategory.Currency));
        Assert.IsFalse(t.IsChanged("egypt", "ancient", ClueCategory.Language), "another category");
        Assert.IsFalse(t.IsChanged("iraq", "ancient", ClueCategory.Currency), "another place");

        Assert.IsFalse(t.MarkChanged("egypt", "ancient", ClueCategory.Technology), "not in the table");
        Assert.IsFalse(t.IsChanged("egypt", "ancient", ClueCategory.Technology));
        Assert.IsFalse(t.MarkChanged(null, "ancient", ClueCategory.Currency));
        Assert.IsFalse(t.IsChanged(null, "ancient", ClueCategory.Currency));
        Assert.IsFalse(t.IsChanged("egypt", null, ClueCategory.Currency));
    }

    [Test]
    public void Add_RejectsABlankOriginLabel_LikeABlankId()
    {
        var t = new FactTable();
        Assert.Throws<System.ArgumentException>(() => t.Add("egypt", "ancient", null, ClueCategory.Currency, "Deben"), "audit R1-017: a place without a label would count as absent");
        Assert.Throws<System.ArgumentException>(() => t.Add("egypt", "ancient", " ", ClueCategory.Currency, "Deben"));
        Assert.IsNull(t.Get("egypt", "ancient", ClueCategory.Currency), "nothing was stored");
    }

    [Test]
    public void HasPlace_IsTrueOnlyForAPlaceInTheTable()
    {
        FactTable t = Today();
        Assert.IsTrue(t.HasPlace("egypt", "ancient"));
        Assert.IsFalse(t.HasPlace("egypt", "medieval"));
        Assert.IsFalse(t.HasPlace(null, "ancient"));
        Assert.IsFalse(t.HasPlace("egypt", null));
    }

    [Test]
    public void MaxValueLength_Is28()
    {
        Assert.AreEqual(28, FactTable.MaxValueLength);
    }
}
