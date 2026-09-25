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
}
