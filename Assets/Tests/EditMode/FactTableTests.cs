using System.Linq;
using NUnit.Framework;

/// <summary>
/// Today's facts: two Ancient places. Egypt uses "Deben", Babylonia "Silver shekel";
/// both list their languages. The claim under test is Egypt / Ancient.
/// </summary>
public class FactTableTests
{
    private static FactTable Today()
    {
        var t = new FactTable();
        t.Add("egypt", "ancient", "New Kingdom Egypt (Ancient)", ClueCategory.Currency, "Deben");
        t.Add("egypt", "ancient", "New Kingdom Egypt (Ancient)", ClueCategory.Language, "Middle Egyptian");
        t.Add("iraq", "ancient", "Babylonia (Ancient)", ClueCategory.Currency, "Silver shekel");
        t.Add("iraq", "ancient", "Babylonia (Ancient)", ClueCategory.Language, "Old Babylonian");
        return t;
    }

    private static int First(int count) => 0;

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
    public void PickOtherValue_ExcludesTheTruth_CaseAndSpaceInsensitive()
    {
        Assert.AreEqual("Silver shekel", Today().PickOtherValue(ClueCategory.Currency, " deben ", First));
    }

    [Test]
    public void PickOtherValue_OffersEachDistinctValueOnce()
    {
        FactTable t = Today();
        t.Add("greece", "ancient", "Periclean Athens (Ancient)", ClueCategory.Currency, "silver shekel");
        t.Add("italy", "ancient", "Republican Rome (Ancient)", ClueCategory.Currency, "Denarius");
        var seen = Enumerable.Range(0, 5).Select(i => t.PickOtherValue(ClueCategory.Currency, "Deben", n => i)).Distinct().ToArray();
        CollectionAssert.AreEquivalent(new[] { "Silver shekel", "Denarius" }, seen);
    }

    [Test]
    public void PickOtherValue_WithNothingElse_ReturnsNull()
    {
        var t = new FactTable();
        t.Add("egypt", "ancient", "E", ClueCategory.Currency, "Deben");
        Assert.IsNull(t.PickOtherValue(ClueCategory.Currency, "Deben", First));
        Assert.IsNull(t.PickOtherValue(ClueCategory.Technology, "Deben", First));
    }

    [Test]
    public void ForgedValueFromAnotherPlace_RegistersAsOriginProof_AgainstThatPlacesRow()
    {
        FactTable t = Today();
        var forged = new CompareEvidence { kind = EvidenceKind.DocumentField, category = ClueCategory.Currency, value = "Silver shekel", isAnachronism = true };
        FactRow babylon = t.Rows(ClueCategory.Currency)[1];
        Discrepancy d = new DiscrepancyLog().TryRegister(forged, babylon.ToEvidence(), "egypt", "ancient");
        Assert.NotNull(d);
        Assert.AreEqual(DiscrepancyProof.ForeignOrigin, d.provedBy);
    }

    [Test]
    public void ForgedValue_RegistersAsMismatch_AgainstTheClaimedPlacesRow()
    {
        FactTable t = Today();
        var forged = new CompareEvidence { kind = EvidenceKind.DocumentField, category = ClueCategory.Currency, value = "Silver shekel", isAnachronism = true };
        FactRow egypt = t.Rows(ClueCategory.Currency)[0];
        Discrepancy d = new DiscrepancyLog().TryRegister(forged, egypt.ToEvidence(), "egypt", "ancient");
        Assert.NotNull(d);
        Assert.AreEqual(DiscrepancyProof.ClaimMismatch, d.provedBy);
    }
}
