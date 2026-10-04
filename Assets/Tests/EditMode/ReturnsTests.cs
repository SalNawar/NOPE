using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// Wave 5, Papers, Please lesson 9: a traveller the clerk denies may come
/// back on a later day, same name and face, with corrected papers or a new
/// story; seeded, deterministic, once at most; the paper's desk section tells
/// their second visit.
/// </summary>
public class ReturnsTests
{
    private static ReturningTraveller Back(string name, int from, int until, bool returned = false) =>
        new ReturningTraveller { name = name, displayName = name + " (Merchant)", fromDay = from, untilDay = until, deniedDay = from - 1, returned = returned };

    [Test]
    public void Plan_NoneForAPremadeOrASecondVisit_OrWhenTheRollSaysNo()
    {
        Assert.IsNull(Returns.Plan(false, 1f, 1, 3, 0.5f, 2, 99), "a premade (or a traveller back already) never enters");
        Assert.IsNull(Returns.Plan(true, 0f, 1, 3, 0.5f, 2, 99), "a chance of 0: nobody comes back");
    }

    [Test]
    public void Plan_TheWindowFromTheDenial_TheStoryByItsShare()
    {
        ReturningTraveller r = Returns.Plan(true, 1f, 2, 4, 1f, 5, 1234);
        Assert.AreEqual(5, r.deniedDay);
        Assert.AreEqual(7, r.fromDay);
        Assert.AreEqual(9, r.untilDay);
        Assert.AreEqual(ReturnStory.Corrected, r.story);
        Assert.AreEqual(ReturnStory.NewStory, Returns.Plan(true, 1f, 1, 1, 0f, 5, 1234).story);

        ReturningTraveller tight = Returns.Plan(true, 1f, 0, 0, 0.5f, 3, 1);
        Assert.AreEqual(4, tight.fromDay, "the next day at the earliest");
        Assert.AreEqual(4, tight.untilDay, "the window never ends before it starts");
    }

    [Test]
    public void Plan_IsSeeded_OnTheTravellersOwnReturnStream()
    {
        int back = 0;
        for (int c = 1; c <= 200; c++)
        {
            int seed = Seeds.ForReturn(Seeds.ForCase(Seeds.Day(12345, 3), c));
            ReturningTraveller a = Returns.Plan(true, 0.4f, 1, 3, 0.5f, 3, seed), b = Returns.Plan(true, 0.4f, 1, 3, 0.5f, 3, seed);
            Assert.AreEqual(a == null, b == null, "the same traveller, the same answer");
            if (a == null)
                continue;
            back++;
            Assert.AreEqual(a.story, b.story);
        }
        Assert.That(back, Is.InRange(50, 110), "a chance of 0.4 brings back about 80 of 200");
    }

    [Test]
    public void Due_TheWindowHoldsTheDay_TheyFit_NotBackYet_InOrder_UpToTheCap()
    {
        var records = new List<ReturningTraveller> { Back("Iset", 3, 5), Back("Omar", 2, 3), Back("Nakht", 4, 6, returned: true), Back("Meryt", 3, 3), null };
        CollectionAssert.AreEqual(new[] { "Iset", "Omar", "Meryt" }, Returns.Due(records, 3, null, 5).Select(r => r.name).ToArray());
        CollectionAssert.AreEqual(new[] { "Iset" }, Returns.Due(records, 3, null, 1).Select(r => r.name).ToArray(), "the day's cap, earliest denied first");
        CollectionAssert.AreEqual(new[] { "Omar", "Meryt" }, Returns.Due(records, 3, r => r.name != "Iset", 5).Select(r => r.name).ToArray(), "one who does not fit today waits for another day");
        CollectionAssert.AreEqual(new[] { "Iset" }, Returns.Due(records, 5, null, 5).Select(r => r.name).ToArray(), "out of their window, or back already: none");
        CollectionAssert.IsEmpty(Returns.Due(records, 3, null, 0), "a day of 0: none");
        CollectionAssert.IsEmpty(Returns.Due(null, 3, null, 5));
    }

    [Test]
    public void Slots_FreeSlotsOnly_Distinct_Seeded()
    {
        var taken = new HashSet<int> { 1, 3, 5 };
        List<int> a = Returns.Slots(6, taken, 2, new SeededRandom(Seeds.ForReturnSlots(Seeds.Day(12345, 4))));
        CollectionAssert.AreEqual(a, Returns.Slots(6, taken, 2, new SeededRandom(Seeds.ForReturnSlots(Seeds.Day(12345, 4)))));
        Assert.AreEqual(2, a.Count);
        CollectionAssert.AllItemsAreUnique(a);
        CollectionAssert.IsSubsetOf(a, new[] { 2, 4, 6 });
        CollectionAssert.AreEquivalent(new[] { 2, 4, 6 }, Returns.Slots(6, taken, 5, new SeededRandom(1)), "no more than are free");
        CollectionAssert.IsEmpty(Returns.Slots(3, new HashSet<int> { 1, 2, 3 }, 1, new SeededRandom(1)));
    }

    [Test]
    public void Lines_TheSecondVisitsOfTheDay_Once_TheRightTemplate()
    {
        ReturningTraveller let = Back("Iset", 3, 5), again = Back("Omar", 3, 5), later = Back("Nakht", 3, 5);
        let.backDay = 4;
        let.acceptedBack = true;
        again.backDay = 4;
        later.backDay = 5;
        var records = new List<ReturningTraveller> { let, again, later };
        List<string> lines = Returns.Lines(records, 4, "Second time lucky: {name}, turned away on day {day}, through on day {back}.", "{name}, turned away on day {day}, and again on day {back}.");
        CollectionAssert.AreEqual(new[] { "Second time lucky: Iset (Merchant), turned away on day 2, through on day 4.", "Omar (Merchant), turned away on day 2, and again on day 4." }, lines);
        Assert.IsTrue(let.reported && again.reported);
        Assert.IsFalse(later.reported, "another day's second visit waits for its own paper");
        CollectionAssert.IsEmpty(Returns.Lines(records, 4, "{name}", "{name}"), "told once");
        later.displayName = string.Empty;
        CollectionAssert.AreEqual(new[] { "Nakht" }, Returns.Lines(records, 5, "{name}", "{name}"), "no display name: the name");
        CollectionAssert.IsEmpty(Returns.Lines(new List<ReturningTraveller> { Back("X", 1, 2) }, 0, "{name}", "{name}"), "no second verdict yet");
    }

    [Test]
    public void ReturningOpener_TheHonorificAndTheDay_BlankIsNone()
    {
        var lines = new InterviewLines { honorificFemale = "madam", openerReturning = new LineText("interview.openerReturning", "Back again, {honorific}? Day {day}, wasn't it?") };
        Assert.AreEqual("Back again, madam? Day 3, wasn't it?", Interview.ReturningOpener(lines, TravellerGender.Female, 3));
        Assert.IsNull(Interview.ReturningOpener(new InterviewLines(), TravellerGender.Female, 3));
        Assert.IsNull(Interview.ReturningOpener(null, TravellerGender.Female, 3));
    }
}
