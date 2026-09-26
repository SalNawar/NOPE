using System;
using System.Collections.Generic;
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
    public void IsRevised_WhenHistoryGivesTheCellADifferentValue()
    {
        Assert.IsFalse(History.IsRevised(null, "egypt", "ancient", ClueCategory.Currency, "Deben"), "a null state");
        var h = new HistoryState();
        Assert.IsFalse(History.IsRevised(h, "egypt", "ancient", ClueCategory.Currency, "Deben"), "no edits");

        h.factEdits.Add(Edit("egypt", "ancient", ClueCategory.Currency, "Sterling"));
        Assert.IsTrue(History.IsRevised(h, "egypt", "ancient", ClueCategory.Currency, "Deben"));
        Assert.IsFalse(History.IsRevised(h, "egypt", "ancient", ClueCategory.Technology, "Papyrus"), "another category");
        Assert.IsFalse(History.IsRevised(h, "greece", "ancient", ClueCategory.Currency, "Obol"), "another place");
    }

    [TestCase("Deben", false, Description = "an edit equal to the base")]
    [TestCase("  deben ", false, Description = "case and surrounding spaces do not count (ValuesMatch)")]
    [TestCase(" ", false, Description = "a blank edit is ignored")]
    [TestCase("Sterling", true)]
    public void IsRevised_OneEdit(string value, bool expected)
    {
        var h = new HistoryState();
        h.factEdits.Add(Edit("egypt", "ancient", ClueCategory.Currency, value));
        Assert.AreEqual(expected, History.IsRevised(h, "egypt", "ancient", ClueCategory.Currency, "Deben"));
    }

    [Test]
    public void IsRevised_FollowsTheNewestEdit()
    {
        var back = new HistoryState();
        back.factEdits.Add(Edit("egypt", "ancient", ClueCategory.Currency, "Sterling"));
        back.factEdits.Add(Edit("egypt", "ancient", ClueCategory.Currency, "Deben", 3));
        Assert.IsFalse(History.IsRevised(back, "egypt", "ancient", ClueCategory.Currency, "Deben"), "changed back");

        var away = new HistoryState();
        away.factEdits.Add(Edit("egypt", "ancient", ClueCategory.Currency, "Deben"));
        away.factEdits.Add(Edit("egypt", "ancient", ClueCategory.Currency, "Sterling", 3));
        Assert.IsTrue(History.IsRevised(away, "egypt", "ancient", ClueCategory.Currency, "Deben"));
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
    public void LatestEdit_IsTheEditResolveReads()
    {
        var h = new HistoryState();
        Assert.IsNull(History.LatestEdit(h, "egypt", "ancient", ClueCategory.Currency));
        Assert.IsNull(History.LatestEdit(null, "egypt", "ancient", ClueCategory.Currency));

        FactEdit sterling = Edit("egypt", "ancient", ClueCategory.Currency, "Sterling", 2);
        h.factEdits.Add(sterling);
        h.factEdits.Add(Edit("egypt", "ancient", ClueCategory.Currency, " ", 4));
        h.factEdits.Add(Edit("greece", "ancient", ClueCategory.Currency, "Florin", 5));
        Assert.AreSame(sterling, History.LatestEdit(h, "egypt", "ancient", ClueCategory.Currency), "the newest non-blank edit of the place and category");
        Assert.IsNull(History.LatestEdit(h, "egypt", "ancient", ClueCategory.Language));
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

    [Test]
    public void PanicLines_OnePerAcceptedCostumeError_InAcceptOrder()
    {
        var panics = new List<PanicRecord>
        {
            new PanicRecord { placeLabel = "Periclean Athens (Ancient)", item = "transit badge", day = 2 },
            null,
            new PanicRecord { placeLabel = "Abbasid Baghdad (Medieval)", item = "tech jacket", day = 2 }
        };
        CollectionAssert.AreEqual(new[]
        {
            "PANIC in Periclean Athens (Ancient): \"transit badge\".",
            "PANIC in Abbasid Baghdad (Medieval): \"tech jacket\"."
        }, History.PanicLines("PANIC in {place}: \"{value}\".", panics));
        CollectionAssert.IsEmpty(History.PanicLines(" ", panics), "a blank template");
        CollectionAssert.IsEmpty(History.PanicLines("PANIC in {place}.", null));
    }
}
