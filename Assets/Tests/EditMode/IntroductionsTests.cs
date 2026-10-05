using System.Collections.Generic;
using NUnit.Framework;

/// <summary>The one introduction registry (the desk-first redesign, items 2, 9, 10): a feature is introduced on the first day any plan lists it, and stays.</summary>
public class IntroductionsTests
{
    private static Introductions Ramp() => new Introductions(new (int, IEnumerable<string>)[]
    {
        (1, Introductions.DayKeys(new[] { "TC-101" }, new[] { "Rule_OpenD1", "Rule_PaperDates" }, new[] { LieKind.SwappedPhoto }, new[] { "q_born" },
                                  new[] { Feature.Calendar, Feature.Field(ClueCategory.Name) })),
        (2, Introductions.DayKeys(new[] { "TC-101", "TC-230" }, new[] { "Rule_OpenD1", "Rule_PaperDates" }, new[] { LieKind.SwappedPhoto }, null, null)),
        (5, Introductions.DayKeys(new[] { "TC-101", "TC-230" }, null, null, null, new[] { Feature.Scanner, Feature.Records })),
        (4, Introductions.DayKeys(null, null, new[] { LieKind.ForgedSeal }, null, new[] { Feature.Book(ClueCategory.Seal), Feature.Scanner })),
    });

    [Test]
    public void FirstDay_TheEarliestDayListingIt_InAnyOrder()
    {
        Introductions r = Ramp();
        Assert.AreEqual(1, r.FirstDay(Feature.Paper("TC-101")));
        Assert.AreEqual(2, r.FirstDay(Feature.Paper("TC-230")));
        Assert.AreEqual(1, r.FirstDay(Feature.Rule("Rule_PaperDates")));
        Assert.AreEqual(4, r.FirstDay(Feature.Lie(LieKind.ForgedSeal)));
        Assert.AreEqual(4, r.FirstDay(Feature.Scanner), "day 4 lists it too, before day 5");
        Assert.AreEqual(1, r.FirstDay(Feature.Question("q_born")));
        Assert.AreEqual(0, r.FirstDay(Feature.Board), "never introduced");
        Assert.AreEqual(0, r.FirstDay(null));
    }

    [Test]
    public void Has_FromItsFirstDayOn_NeverBefore_NeverWhenUnlisted()
    {
        Introductions r = Ramp();
        Assert.IsFalse(r.Has(1, Feature.Paper("TC-230")));
        Assert.IsTrue(r.Has(2, Feature.Paper("TC-230")));
        Assert.IsTrue(r.Has(15, Feature.Paper("TC-230")), "nothing is withdrawn");
        Assert.IsTrue(r.Has(5, Feature.Book(ClueCategory.Seal)), "a later day need not list it again");
        Assert.IsFalse(r.Has(15, Feature.Board), "a feature no day lists stays hidden");
        Assert.IsFalse(Introductions.None.Has(1, Feature.Calendar));
    }

    [Test]
    public void NewOn_TheDaysNewThings()
    {
        Introductions r = Ramp();
        CollectionAssert.AreEquivalent(new[] { Feature.Paper("TC-230") }, r.NewOn(2));
        CollectionAssert.AreEquivalent(new[] { Feature.Lie(LieKind.ForgedSeal), Feature.Book(ClueCategory.Seal), Feature.Scanner }, r.NewOn(4));
        CollectionAssert.AreEquivalent(new[] { Feature.Records }, r.NewOn(5));
        Assert.IsTrue(r.IsNew(4, Feature.Scanner));
        Assert.IsFalse(r.IsNew(5, Feature.Scanner));
    }

    [Test]
    public void ShowsField_TheCategoryOrTheFormsOwnKey()
    {
        var r = new Introductions(new (int, IEnumerable<string>)[]
        {
            (1, new[] { Feature.Field(ClueCategory.Name) }),
            (14, new[] { Feature.Field("TC-620", ClueCategory.Currency) }),
        });
        Assert.IsTrue(r.ShowsField(1, "TC-101", ClueCategory.Name));
        Assert.IsTrue(r.ShowsField(1, "TC-230", ClueCategory.Name), "a category key shows it on every form");
        Assert.IsFalse(r.ShowsField(13, "TC-620", ClueCategory.Currency));
        Assert.IsTrue(r.ShowsField(14, "TC-620", ClueCategory.Currency));
        Assert.IsFalse(r.ShowsField(15, "TC-230", ClueCategory.Currency), "a form's key shows it on that form only");
    }

    [TestCase("tool:scanner", true)]
    [TestCase("tool:board", true)]
    [TestCase("tool:calendar", true)]
    [TestCase("tool:rulebook", true)]
    [TestCase("wheel:clothes", true)]
    [TestCase("pc:records", true)]
    [TestCase("pc:standing", true)]
    [TestCase("app:orders", true)]
    [TestCase("app:nowhere", false)]
    [TestCase("book:Seal", true)]
    [TestCase("book:Sealz", false)]
    [TestCase("field:Expiry", true)]
    [TestCase("field:TC-620/Currency", true)]
    [TestCase("field:TC-620/", false)]
    [TestCase("field:7", false)]
    [TestCase("tool:hammer", false)]
    [TestCase("paper:TC-101", false)]
    [TestCase("", false)]
    public void IsNamedKey_OnlyTheRegistrysKeys(string key, bool expected) => Assert.AreEqual(expected, Introductions.IsNamedKey(key));

    [Test]
    public void Problems_AnUnknownKey_AndATwice()
    {
        List<string> problems = Introductions.Problems("DayPlan_Inv_Day1", new[] { "tool:scanner", "tool:hammer", "tool:scanner" });
        Assert.AreEqual(2, problems.Count);
        StringAssert.Contains("'tool:hammer', which is no feature key", problems[0]);
        StringAssert.Contains("twice", problems[1]);
        CollectionAssert.IsEmpty(Introductions.Problems("D", null));
    }
}
