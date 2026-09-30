using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// Papers Please lessons 4 and D7 (wave 5 track C): the papers in circulation
/// on a day (DayPapers) and what each day brings for the first time, at most
/// one paper and one directive, named in its bulletin (DayPacing).
/// </summary>
public class DayPacingTests
{
    private static readonly string[] Known = { "TC-101", "TC-230", "TC-310", "TC-415" };

    [Test]
    public void Issued_AnEmptyListIssuesEveryForm()
    {
        Assert.IsTrue(DayPapers.Issued(null, "TC-310"));
        Assert.IsTrue(DayPapers.Issued(new string[0], "TC-310"));
        Assert.IsTrue(DayPapers.Issued(new[] { "TC-101" }, "TC-101"));
        Assert.IsFalse(DayPapers.Issued(new[] { "TC-101" }, "TC-230"));
        Assert.IsFalse(DayPapers.Issued(new[] { "TC-101" }, null));
    }

    [Test]
    public void Carried_KeepsTheIssuedFormsInOrder()
    {
        CollectionAssert.AreEqual(new[] { "TC-101", "TC-310" }, DayPapers.Carried(new[] { "TC-101", "TC-230", null, "TC-310" }, new[] { "TC-310", "TC-101" }));
        CollectionAssert.AreEqual(new[] { "TC-101", "TC-230" }, DayPapers.Carried(new[] { "TC-101", "TC-230" }, new string[0]));
    }

    [Test]
    public void Problems_AnUnknownFormATwiceListedFormAndAWithdrawnForm()
    {
        CollectionAssert.IsEmpty(DayPapers.Problems("d2", new[] { "TC-101", "TC-230" }, Known, new[] { "TC-101" }));
        CollectionAssert.IsEmpty(DayPapers.Problems("d9", new string[0], Known, new[] { "TC-101" }), "an empty list issues every form");

        List<string> problems = DayPapers.Problems("d3", new[] { "TC-101", "TC-999", "TC-101" }, Known, new[] { "TC-101", "TC-230" });
        Assert.AreEqual(3, problems.Count, string.Join(" | ", problems));
        StringAssert.Contains("TC-999", problems[0]);
        StringAssert.Contains("twice", problems[1]);
        StringAssert.Contains("no longer issues 'TC-230'", problems[2]);
    }

    [Test]
    public void Keys_AProofGroupIsOnePaper_EveryClosureOfEveryKindIsOneCheck()
    {
        Assert.AreEqual("proof", DayPacing.PaperKey("proof", "TC-416"));
        Assert.AreEqual("TC-310", DayPacing.PaperKey("", "TC-310"));
        Assert.AreEqual(DayPacing.Closures, DayPacing.RuleKey("Rule_NoAncientEgypt", true, false));
        Assert.AreEqual("Rule_NoEconomyAncient", DayPacing.RuleKey("Rule_NoEconomyAncient", true, true), "a closure for some kinds is a check of its own");
        Assert.AreEqual("Rule_PaperDates", DayPacing.RuleKey("Rule_PaperDates", false, false));
    }

    [Test]
    public void NewByDay_CountsAKeyOnTheFirstDayOnly()
    {
        List<List<string>> fresh = DayPacing.NewByDay(new List<IEnumerable<string>>
        {
            new[] { "TC-101" },
            new[] { "TC-101", "TC-230", "TC-230" },
            null,
            new[] { "closures", "TC-101" },
            new[] { "closures" }
        });
        CollectionAssert.AreEqual(new[] { "TC-101" }, fresh[0]);
        CollectionAssert.AreEqual(new[] { "TC-230" }, fresh[1]);
        CollectionAssert.IsEmpty(fresh[2]);
        CollectionAssert.AreEqual(new[] { "closures" }, fresh[3]);
        CollectionAssert.IsEmpty(fresh[4], "the closures change daily; the check is new once");
    }

    [Test]
    public void Problems_AtMostOnePaperAndOneDirective_AndEveryNewThingNamedInTheBulletin()
    {
        CollectionAssert.IsEmpty(DayPacing.Problems("d4", new[] { "TC-310" }, new[] { "Rule_TouristWaiverSet" }, "NEW: the Stranding Waiver."),
                                 "a paper and the procedure that asks for it arrive together");
        CollectionAssert.IsEmpty(DayPacing.Problems("d15", new string[0], new string[0], ""), "nothing new, no bulletin needed");
        CollectionAssert.IsEmpty(DayPacing.Problems("d15", new string[0], new string[0], "A notice."), "a bulletin with nothing new is a notice");

        List<string> two = DayPacing.Problems("d2", new[] { "TC-230", "TC-310" }, new[] { "closures", "Rule_DressForDestination" }, "NEW: two things.");
        Assert.AreEqual(2, two.Count, string.Join(" | ", two));
        StringAssert.Contains("2 new papers", two[0]);
        StringAssert.Contains("2 new directives", two[1]);

        List<string> silent = DayPacing.Problems("d3", new string[0], new[] { "closures" }, " ");
        Assert.AreEqual(1, silent.Count);
        StringAssert.Contains("bulletin", silent[0]);
    }
}
