using System;
using NUnit.Framework;

/// <summary>
/// The run's history: which Future place is in the world, how a place's fact
/// resolves from the latched edits (newest wins), what may be latched, and the
/// history news cap.
/// </summary>
public class HistoryTests
{
    private static FactEdit Edit(string nation, string era, ClueCategory category, string value, int day = 2) =>
        new FactEdit(nation, era, category, value, day, EditCause.Rule, "test");

    [Test]
    public void FutureNation_IsTheLeader_OrNull()
    {
        Assert.IsNull(History.FutureNation(null));
        Assert.IsNull(History.FutureNation(new HistoryState()));
        Assert.IsNull(History.FutureNation(new HistoryState { leaderId = "  " }));
        Assert.AreEqual("china", History.FutureNation(new HistoryState { leaderId = "china" }));
    }

    [Test]
    public void InWorld_EveryPastPlace_AndOnlyTheLeadersFuture()
    {
        Assert.IsTrue(History.InWorld(false, "egypt", null));
        Assert.IsTrue(History.InWorld(false, "egypt", "china"));
        Assert.IsTrue(History.InWorld(true, "china", "china"));
        Assert.IsFalse(History.InWorld(true, "egypt", "china"));
        Assert.IsFalse(History.InWorld(true, "china", null));
        Assert.IsFalse(History.InWorld(true, "china", ""));
        Assert.IsFalse(History.InWorld(true, "China", "china"), "ids compare ordinally");
    }

    [Test]
    public void Resolve_NewestEditOfThePlaceAndCategoryWins()
    {
        var h = new HistoryState();
        Assert.AreEqual("Deben", History.Resolve(h, "egypt", "ancient", ClueCategory.Currency, "Deben"), "no edits");

        h.factEdits.Add(Edit("egypt", "ancient", ClueCategory.Currency, "Sterling"));
        Assert.AreEqual("Sterling", History.Resolve(h, "egypt", "ancient", ClueCategory.Currency, "Deben"));

        h.factEdits.Add(Edit("egypt", "ancient", ClueCategory.Currency, "Florin", 3));
        Assert.AreEqual("Florin", History.Resolve(h, "egypt", "ancient", ClueCategory.Currency, "Deben"), "the newest wins");

        Assert.AreEqual("Papyrus", History.Resolve(h, "egypt", "ancient", ClueCategory.Technology, "Papyrus"), "another category");
        Assert.AreEqual("Obol", History.Resolve(h, "greece", "ancient", ClueCategory.Currency, "Obol"), "another place");
        Assert.AreEqual("Deben", History.Resolve(null, "egypt", "ancient", ClueCategory.Currency, "Deben"), "a null state");
    }

    [Test]
    public void Resolve_IgnoresABlankEditValue()
    {
        var h = new HistoryState();
        h.factEdits.Add(Edit("egypt", "ancient", ClueCategory.Currency, "Sterling"));
        h.factEdits.Add(Edit("egypt", "ancient", ClueCategory.Currency, " "));
        Assert.AreEqual("Sterling", History.Resolve(h, "egypt", "ancient", ClueCategory.Currency, "Deben"));
    }

    [Test]
    public void Latch_AppendsEditableNonBlankEdits_Only()
    {
        var h = new HistoryState();
        Assert.IsTrue(History.Latch(h, Edit("egypt", "ancient", ClueCategory.Politics, "Elected Pharaoh")));
        Assert.AreEqual(1, h.factEdits.Count);

        Assert.IsFalse(History.Latch(h, null));
        Assert.IsFalse(History.Latch(null, Edit("egypt", "ancient", ClueCategory.Politics, "x")));
        Assert.IsFalse(History.Latch(h, Edit("egypt", "ancient", ClueCategory.Politics, " ")));
        foreach (ClueCategory c in new[] { ClueCategory.Culture, ClueCategory.Name, ClueCategory.BirthDate, ClueCategory.Material })
            Assert.IsFalse(History.Latch(h, Edit("egypt", "ancient", c, "x")), c.ToString());
        Assert.AreEqual(1, h.factEdits.Count);
    }

    [TestCase(0, 3, 5, 3)]
    [TestCase(1, 3, 5, 2, Description = "after the leader line")]
    [TestCase(3, 3, 1, 0)]
    [TestCase(0, 3, 1, 1)]
    [TestCase(4, 3, 2, 0)]
    [TestCase(-2, 3, 5, 3, Description = "negative inputs count as 0")]
    [TestCase(0, -1, 5, 0)]
    [TestCase(0, 3, -4, 0)]
    public void NewsSlots_TheCapCountsEveryTemplatedLine(int linesSoFar, int cap, int count, int expected)
    {
        Assert.AreEqual(expected, History.NewsSlots(linesSoFar, cap, count));
    }

    [Test]
    public void IsEditable_TheFiveBookCategories()
    {
        foreach (ClueCategory c in Enum.GetValues(typeof(ClueCategory)))
        {
            bool expected = c == ClueCategory.Currency || c == ClueCategory.Language || c == ClueCategory.Technology ||
                            c == ClueCategory.Geography || c == ClueCategory.Politics;
            Assert.AreEqual(expected, History.IsEditable(c), c.ToString());
        }
        CollectionAssert.AreEquivalent(History.EditableCategories, new[]
        {
            ClueCategory.Currency, ClueCategory.Language, ClueCategory.Technology, ClueCategory.Geography, ClueCategory.Politics
        });
    }

    [Test]
    public void EditCause_KeepsItsSerializedInts()
    {
        Assert.AreEqual(0, (int)EditCause.Rule);
        Assert.AreEqual(1, (int)EditCause.Carry);
    }
}
