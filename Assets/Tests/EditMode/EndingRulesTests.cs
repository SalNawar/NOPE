using NUnit.Framework;

/// <summary>
/// Which ending a check picks: failures any time; the run's last day (the
/// neutral "world you made" ending) only at the day boundary. The attribute
/// epilogues are retired (2026-09-29): their condition keeps its number and is
/// never met.
/// </summary>
public class EndingRulesTests
{
    private static EndingCandidate C(EndingKind kind, int priority, bool met) => new EndingCandidate(kind, priority, met);

    [TestCase(EndingConditionType.Fired, EndingKind.Failure)]
    [TestCase(EndingConditionType.Bankrupt, EndingKind.Failure)]
    [TestCase(EndingConditionType.DayAtLeast, EndingKind.Milestone)]
    [TestCase(EndingConditionType.PetTaken, EndingKind.Failure)]
    [TestCase(EndingConditionType.AttrTotalAtLeast, EndingKind.Failure, Description = "retired: never met, so its kind never counts")]
    [TestCase((EndingConditionType)99, EndingKind.Failure)]
    public void KindOf(EndingConditionType type, EndingKind expected)
    {
        Assert.AreEqual(expected, EndingRules.KindOf(type));
    }

    [Test]
    public void EndingConditionType_KeepsItsSerializedInts()
    {
        Assert.AreEqual(0, (int)EndingConditionType.Fired);
        Assert.AreEqual(1, (int)EndingConditionType.Bankrupt);
        Assert.AreEqual(2, (int)EndingConditionType.AttrTotalAtLeast);
        Assert.AreEqual(3, (int)EndingConditionType.DayAtLeast);
        Assert.AreEqual(4, (int)EndingConditionType.PetTaken);
        Assert.AreEqual(5, System.Enum.GetValues(typeof(EndingConditionType)).Length, "a new member is appended here too");
    }

    /// <summary>The Welfare Office's ending (the Home pet spec PS6): met once the pet is taken, at any moment, whatever the threshold.</summary>
    [Test]
    public void Met_PetTaken_OnceTheWelfareOfficeTookThePet()
    {
        Assert.IsTrue(EndingRules.Met(EndingConditionType.PetTaken, 0f, new EndingCheck(80f, 100, 4, 0f, -100, petTaken: true)));
        Assert.IsFalse(EndingRules.Met(EndingConditionType.PetTaken, 0f, new EndingCheck(80f, 100, 4, 0f, -100)));
        Assert.IsFalse(EndingRules.IsRetired(EndingConditionType.PetTaken));
    }

    [Test]
    public void OnlyTheAttributeEpilogues_AreRetired()
    {
        Assert.IsTrue(EndingRules.IsRetired(EndingConditionType.AttrTotalAtLeast));
        Assert.IsFalse(EndingRules.IsRetired(EndingConditionType.Fired));
        Assert.IsFalse(EndingRules.IsRetired(EndingConditionType.Bankrupt));
        Assert.IsFalse(EndingRules.IsRetired(EndingConditionType.DayAtLeast));
    }

    [Test]
    public void EndingKind_HasNoEpilogue()
    {
        CollectionAssert.AreEqual(new[] { "Failure", "Milestone" }, System.Enum.GetNames(typeof(EndingKind)), "no ending ranks the world the run made");
    }

    [Test]
    public void Immediate_OnlyFailuresCount()
    {
        var candidates = new[] { C(EndingKind.Milestone, 10, true), C(EndingKind.Failure, 1, true), C(EndingKind.Failure, 5, true) };
        Assert.AreEqual(2, EndingRules.Select(candidates, EndingMoment.Immediate), "the highest-priority failure");
        Assert.AreEqual(-1, EndingRules.Select(new[] { C(EndingKind.Milestone, 10, true) }, EndingMoment.Immediate));
    }

    [Test]
    public void DayBoundary_TheLastDayAndFailures()
    {
        Assert.AreEqual(0, EndingRules.Select(new[] { C(EndingKind.Milestone, 10, true) }, EndingMoment.DayBoundary), "the last day alone");
        Assert.AreEqual(-1, EndingRules.Select(new[] { C(EndingKind.Milestone, 10, false) }, EndingMoment.DayBoundary), "before the last day, nothing");
        Assert.AreEqual(1, EndingRules.Select(new[] { C(EndingKind.Milestone, 10, true), C(EndingKind.Failure, 100, true) }, EndingMoment.DayBoundary),
                        "a failure beats the last day");
    }

    [Test]
    public void EqualPriorities_PickTheLowestIndex()
    {
        var candidates = new[] { C(EndingKind.Milestone, 10, true), C(EndingKind.Failure, 50, true), C(EndingKind.Failure, 50, true) };
        Assert.AreEqual(1, EndingRules.Select(candidates, EndingMoment.DayBoundary));
    }

    [Test]
    public void NothingMet_OrNoCandidates()
    {
        Assert.AreEqual(-1, EndingRules.Select(new[] { C(EndingKind.Failure, 1, false) }, EndingMoment.Immediate));
        Assert.AreEqual(-1, EndingRules.Select(new EndingCandidate[0], EndingMoment.DayBoundary));
        Assert.AreEqual(-1, EndingRules.Select(null, EndingMoment.DayBoundary));
    }

    /// <summary>A check's numbers: stability, money and day, with the config's firing (0) and bankruptcy (-100) lines.</summary>
    private static EndingCheck Now(float stability = 50f, int money = 100, int day = 3) => new EndingCheck(stability, money, day, firedAtStability: 0f, bankruptAtMoney: -100);

    [TestCase(0.5f, false)]
    [TestCase(0f, true, Description = "at the line is fired")]
    [TestCase(-3f, true)]
    public void Met_Fired_AtOrBelowTheFiringLine(float stability, bool expected)
    {
        Assert.AreEqual(expected, EndingRules.Met(EndingConditionType.Fired, 0f, Now(stability: stability)));
        Assert.AreEqual(expected, EndingRules.IsFired(stability, 0f));
    }

    [TestCase(-99, false)]
    [TestCase(-100, true, Description = "at the line is bankrupt")]
    [TestCase(-250, true)]
    public void Met_Bankrupt_AtOrBelowTheBankruptcyLine(int money, bool expected)
    {
        Assert.AreEqual(expected, EndingRules.Met(EndingConditionType.Bankrupt, 0f, Now(money: money)));
    }

    [TestCase(0f)]
    [TestCase(41f)]
    public void Met_AttrTotalAtLeast_IsRetired_NeverMet(float threshold)
    {
        Assert.IsFalse(EndingRules.Met(EndingConditionType.AttrTotalAtLeast, threshold, Now()));
        Assert.IsFalse(EndingRules.Met(EndingConditionType.AttrTotalAtLeast, threshold, Now(day: 15)), "not even on the last day");
    }

    [TestCase(14, false)]
    [TestCase(15, true, Description = "day 15 is the last day")]
    [TestCase(16, true)]
    public void Met_DayAtLeast_ReachesTheDay(int day, bool expected)
    {
        Assert.AreEqual(expected, EndingRules.Met(EndingConditionType.DayAtLeast, 15f, Now(day: day)));
    }

    [Test]
    public void Met_ReadsOnlyItsOwnNumbers()
    {
        var broke = new EndingCheck(0f, -500, 99, firedAtStability: 0f, bankruptAtMoney: -100);
        Assert.IsTrue(EndingRules.Met(EndingConditionType.Fired, 0f, broke));
        Assert.IsTrue(EndingRules.Met(EndingConditionType.Bankrupt, 0f, broke));
        Assert.IsFalse(EndingRules.Met(EndingConditionType.DayAtLeast, 100f, broke), "the day alone decides");
        Assert.IsFalse(EndingRules.Met((EndingConditionType)99, 0f, broke), "an unknown type is never met");
    }

    [Test]
    public void Met_UsesTheConfiguredLines()
    {
        var check = new EndingCheck(20f, -40, 1, firedAtStability: 25f, bankruptAtMoney: -50);
        Assert.IsTrue(EndingRules.Met(EndingConditionType.Fired, 0f, check), "20 <= 25");
        Assert.IsFalse(EndingRules.Met(EndingConditionType.Bankrupt, 0f, check), "-40 > -50");
    }
}
