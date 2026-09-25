using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// An accepted liar's carry: recorded at accept (their true home's value for
/// the claim) and promoted at night into a latched fact edit once the
/// (home, claim) pair reaches the threshold. World (Technology): New Kingdom
/// Egypt "Papyrus" (and Currency "Deben"), Periclean Athens "Klepsydra",
/// Babylonia "Clay tablet", Northern Song Kaifeng "Movable type".
/// </summary>
public class CarriesTests
{
    private static FactTable World()
    {
        var t = new FactTable();
        t.Add("egypt", "ancient", "New Kingdom Egypt (Ancient)", ClueCategory.Technology, "Papyrus");
        t.Add("egypt", "ancient", "New Kingdom Egypt (Ancient)", ClueCategory.Currency, "Deben");
        t.Add("greece", "ancient", "Periclean Athens (Ancient)", ClueCategory.Technology, "Klepsydra");
        t.Add("iraq", "ancient", "Babylonia (Ancient)", ClueCategory.Technology, "Clay tablet");
        t.Add("china", "medieval", "Northern Song Kaifeng (Medieval)", ClueCategory.Technology, "Movable type");
        return t;
    }

    private static CarryRecord Record(string fromNation, string toNation, string value, string fromEra = "ancient", string toEra = "ancient", int day = 1) =>
        new CarryRecord
        {
            fromNationId = fromNation, fromEraId = fromEra, toNationId = toNation, toEraId = toEra,
            category = ClueCategory.Technology, value = value, day = day
        };

    [Test]
    public void Make_TheHomesValueHeadedForTheClaim()
    {
        CarryRecord r = Carries.Make("egypt", "ancient", "greece", "ancient", ClueCategory.Technology, World(), 3);
        Assert.NotNull(r);
        Assert.AreEqual(("egypt", "ancient", "greece", "ancient"), (r.fromNationId, r.fromEraId, r.toNationId, r.toEraId));
        Assert.AreEqual(ClueCategory.Technology, r.category);
        Assert.AreEqual("Papyrus", r.value);
        Assert.AreEqual(3, r.day);
    }

    [Test]
    public void Make_NullWhenNothingWouldCarry()
    {
        FactTable w = World();
        Assert.IsNull(Carries.Make("egypt", "ancient", "egypt", "ancient", ClueCategory.Technology, w, 1), "an honest pair (from = to)");
        Assert.IsNull(Carries.Make("greece", "ancient", "egypt", "ancient", ClueCategory.Currency, w, 1), "the home has no value today");
        Assert.IsNull(Carries.Make("egypt", "ancient", "greece", "ancient", ClueCategory.Culture, w, 1), "not an editable category");
        Assert.IsNull(Carries.Make("egypt", "ancient", "greece", "ancient", ClueCategory.Technology, null, 1));
        Assert.IsNull(Carries.Make("", "ancient", "greece", "ancient", ClueCategory.Technology, w, 1));

        w.Add("japan", "ancient", "Yamato (Ancient)", ClueCategory.Technology, " PAPYRUS ");
        Assert.IsNull(Carries.Make("egypt", "ancient", "japan", "ancient", ClueCategory.Technology, w, 1), "the claim already has the value");
    }

    [Test]
    public void Promote_Threshold1_LatchesEveryPairInFirstRecordOrder()
    {
        var h = new HistoryState();
        h.pendingCarries.Add(Record("egypt", "greece", "Papyrus"));
        h.pendingCarries.Add(Record("china", "iraq", "Movable type", fromEra: "medieval"));

        List<FactEdit> edits = Carries.Promote(h, 1, 4, World());

        Assert.AreEqual(2, edits.Count);
        Assert.AreEqual(("greece", "ancient", "Papyrus"), (edits[0].nationId, edits[0].eraId, edits[0].value));
        Assert.AreEqual(("iraq", "ancient", "Movable type"), (edits[1].nationId, edits[1].eraId, edits[1].value));
        Assert.AreEqual(4, edits[0].sinceDay);
        Assert.AreEqual(EditCause.Carry, edits[0].cause);
        Assert.AreEqual("New Kingdom Egypt (Ancient)", edits[0].source);
        Assert.AreEqual("Northern Song Kaifeng (Medieval)", edits[1].source);
        CollectionAssert.AreEqual(edits, h.factEdits, "the edits are latched");
        Assert.AreEqual(0, h.pendingCarries.Count, "their records are consumed");
    }

    [Test]
    public void Promote_Threshold2_WaitsForTheSecondRecord_ThenTakesTheLatestValue()
    {
        var h = new HistoryState();
        h.pendingCarries.Add(Record("egypt", "greece", "Papyrus"));
        Assert.AreEqual(0, Carries.Promote(h, 2, 2, World()).Count);
        Assert.AreEqual(1, h.pendingCarries.Count, "below the threshold the record stays");

        h.pendingCarries.Add(Record("egypt", "greece", "Papyrus & reed", day: 2));
        List<FactEdit> edits = Carries.Promote(h, 2, 3, World());
        Assert.AreEqual(1, edits.Count);
        Assert.AreEqual("Papyrus & reed", edits[0].value);
        Assert.AreEqual(0, h.pendingCarries.Count);
    }

    [Test]
    public void Promote_ADuePairThatChangesNothing_IsConsumedWithoutAnEdit()
    {
        var h = new HistoryState();
        h.pendingCarries.Add(Record("egypt", "greece", "klepsydra "));
        Assert.AreEqual(0, Carries.Promote(h, 1, 2, World()).Count);
        Assert.AreEqual(0, h.pendingCarries.Count);
        Assert.AreEqual(0, h.factEdits.Count);
    }

    [Test]
    public void Promote_TwoHomesSameTargetSameValue_LatchOnce()
    {
        var h = new HistoryState();
        h.pendingCarries.Add(Record("egypt", "greece", "Papyrus"));
        h.pendingCarries.Add(Record("iraq", "greece", "Papyrus"));
        List<FactEdit> edits = Carries.Promote(h, 1, 2, World());
        Assert.AreEqual(1, edits.Count, "the second pair is consumed against the first's edit");
        Assert.AreEqual(0, h.pendingCarries.Count);
    }

    [Test]
    public void Promote_AThresholdOfZeroCountsAsOne()
    {
        var h = new HistoryState();
        h.pendingCarries.Add(Record("egypt", "greece", "Papyrus"));
        Assert.AreEqual(1, Carries.Promote(h, 0, 2, World()).Count);
    }

    [Test]
    public void Promote_NullInputs_ChangeNothing()
    {
        Assert.AreEqual(0, Carries.Promote(null, 1, 2, World()).Count);
        var h = new HistoryState();
        h.pendingCarries.Add(Record("egypt", "greece", "Papyrus"));
        Assert.AreEqual(0, Carries.Promote(h, 1, 2, null).Count);
        Assert.AreEqual(1, h.pendingCarries.Count);
    }
}
