using NUnit.Framework;

/// <summary>The first traveller of the game is an accept (first-case-clean).</summary>
public class FirstCaseTests
{
    [Test]
    public void DayOneSlotOne_IsCleanForEverySeed_NoRollRuns_AndTheDecisionIsApprove()
    {
        for (int seed = 0; seed < 200; seed++)
        {
            bool honest = FirstCase.IsClean(1, 1, true);
            Assert.IsTrue(honest, $"seed {seed}");
            foreach (FaultRoll roll in FaultOrder.Order)
                Assert.IsFalse(FaultOrder.MayRoll(roll, false, false, honest, false), $"seed {seed}: {roll} must not roll");
            Assert.IsTrue(VerdictRules.ShouldAccept(false, false), $"seed {seed}: a traveller with no fault is an accept");
        }
    }

    [Test]
    public void KnobOff_OtherSlotsAndOtherDays_DrawAsBefore()
    {
        Assert.IsFalse(FirstCase.IsClean(1, 1, false));
        Assert.IsFalse(FirstCase.IsClean(1, 2, true));
        Assert.IsFalse(FirstCase.IsClean(2, 1, true));
    }
}
