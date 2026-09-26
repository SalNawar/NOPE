using NUnit.Framework;

/// <summary>
/// Which ending a check picks: failures any time; the Retirement milestone and
/// the attribute epilogues only at the day boundary, where a reached epilogue
/// replaces the milestone.
/// </summary>
public class EndingRulesTests
{
    private static EndingCandidate C(EndingKind kind, int priority, bool met) => new EndingCandidate(kind, priority, met);

    [TestCase(EndingConditionType.Fired, EndingKind.Failure)]
    [TestCase(EndingConditionType.Bankrupt, EndingKind.Failure)]
    [TestCase(EndingConditionType.DayAtLeast, EndingKind.Milestone)]
    [TestCase(EndingConditionType.AttrTotalAtLeast, EndingKind.Epilogue)]
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
    }

    [Test]
    public void Immediate_OnlyFailuresCount()
    {
        var candidates = new[] { C(EndingKind.Milestone, 10, true), C(EndingKind.Epilogue, 50, true), C(EndingKind.Failure, 1, true), C(EndingKind.Failure, 5, true) };
        Assert.AreEqual(3, EndingRules.Select(candidates, EndingMoment.Immediate), "the highest-priority failure");
        Assert.AreEqual(-1, EndingRules.Select(new[] { C(EndingKind.Milestone, 10, true), C(EndingKind.Epilogue, 50, true) }, EndingMoment.Immediate));
    }

    [Test]
    public void DayBoundary_MilestonesAndTheirEpilogues()
    {
        Assert.AreEqual(0, EndingRules.Select(new[] { C(EndingKind.Milestone, 10, true) }, EndingMoment.DayBoundary), "a milestone alone");
        Assert.AreEqual(-1, EndingRules.Select(new[] { C(EndingKind.Milestone, 10, false), C(EndingKind.Epilogue, 50, true) }, EndingMoment.DayBoundary),
                        "an epilogue without a met milestone is ignored");
        Assert.AreEqual(1, EndingRules.Select(new[] { C(EndingKind.Milestone, 10, true), C(EndingKind.Epilogue, 50, true) }, EndingMoment.DayBoundary),
                        "an epilogue replaces the milestone");
        Assert.AreEqual(2, EndingRules.Select(new[] { C(EndingKind.Milestone, 10, true), C(EndingKind.Epilogue, 50, true), C(EndingKind.Failure, 100, true) }, EndingMoment.DayBoundary),
                        "a failure beats an epilogue");
    }

    [Test]
    public void EqualPriorities_PickTheLowestIndex()
    {
        var candidates = new[] { C(EndingKind.Milestone, 10, true), C(EndingKind.Epilogue, 50, true), C(EndingKind.Epilogue, 50, true) };
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
        Assert.AreEqual(expected, EndingRules.Met(EndingConditionType.Fired, 0f, null, Now(stability: stability)));
        Assert.AreEqual(expected, EndingRules.IsFired(stability, 0f));
    }

    [TestCase(-99, false)]
    [TestCase(-100, true, Description = "at the line is bankrupt")]
    [TestCase(-250, true)]
    public void Met_Bankrupt_AtOrBelowTheBankruptcyLine(int money, bool expected)
    {
        Assert.AreEqual(expected, EndingRules.Met(EndingConditionType.Bankrupt, 0f, null, Now(money: money)));
    }

    [TestCase(40.9f, false)]
    [TestCase(41f, true, Description = "reaching the threshold counts")]
    [TestCase(55f, true)]
    public void Met_AttrTotalAtLeast_ReachesTheThreshold(float total, bool expected)
    {
        Assert.AreEqual(expected, EndingRules.Met(EndingConditionType.AttrTotalAtLeast, 41f, total, Now()));
    }

    [Test]
    public void Met_AttrTotalAtLeast_WithoutAnAttribute_IsNeverMet()
    {
        Assert.IsFalse(EndingRules.Met(EndingConditionType.AttrTotalAtLeast, 0f, null, Now()));
    }

    [TestCase(14, false)]
    [TestCase(15, true, Description = "day 15 is Retirement's day")]
    [TestCase(16, true)]
    public void Met_DayAtLeast_ReachesTheDay(int day, bool expected)
    {
        Assert.AreEqual(expected, EndingRules.Met(EndingConditionType.DayAtLeast, 15f, null, Now(day: day)));
    }

    [Test]
    public void Met_ReadsOnlyItsOwnNumbers()
    {
        var broke = new EndingCheck(0f, -500, 99, firedAtStability: 0f, bankruptAtMoney: -100);
        Assert.IsTrue(EndingRules.Met(EndingConditionType.Fired, 0f, null, broke));
        Assert.IsTrue(EndingRules.Met(EndingConditionType.Bankrupt, 0f, null, broke));
        Assert.IsFalse(EndingRules.Met(EndingConditionType.AttrTotalAtLeast, 41f, 0f, broke), "the attribute total alone decides");
        Assert.IsFalse(EndingRules.Met(EndingConditionType.DayAtLeast, 100f, null, broke), "the day alone decides");
        Assert.IsFalse(EndingRules.Met((EndingConditionType)99, 0f, 1000f, broke), "an unknown type is never met");
    }

    [Test]
    public void Met_UsesTheConfiguredLines()
    {
        var check = new EndingCheck(20f, -40, 1, firedAtStability: 25f, bankruptAtMoney: -50);
        Assert.IsTrue(EndingRules.Met(EndingConditionType.Fired, 0f, null, check), "20 <= 25");
        Assert.IsFalse(EndingRules.Met(EndingConditionType.Bankrupt, 0f, null, check), "-40 > -50");
    }
}
