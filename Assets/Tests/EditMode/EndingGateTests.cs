using NUnit.Framework;

/// <summary>
/// Decision table for the endings cushioning gate (GameConfigSO.endingsMinDay).
/// The rule itself lives in TimeDesk.Domain (EndingGate); EndingService consults
/// it at its single entry point. EndingService itself is still in
/// Assembly-CSharp (known extraction debt), so the table is expressed against
/// the pure rule the service delegates to.
/// </summary>
public class EndingGateTests
{
    [Test]
    public void Day1_WithDefaultMinDay2_IsCushioned()
    {
        Assert.IsFalse(EndingGate.EvaluationAllowed(1, 2));
    }

    [Test]
    public void Day1_BankruptcyLevelConditions_StillCushioned()
    {
        // Worst Day 1: 6 wrong verdicts. The gate must hold regardless of
        // money/stability — those live on WorldState, the day is what gates.
        Assert.IsFalse(EndingGate.EvaluationAllowed(1, 2));
    }

    [Test]
    public void Day2_WithMinDay2_AllowsEvaluation()
    {
        Assert.IsTrue(EndingGate.EvaluationAllowed(2, 2));
    }

    [Test]
    public void MinDay1_RestoresOldBehavior_OnDay1()
    {
        Assert.IsTrue(EndingGate.EvaluationAllowed(1, 1));
    }

    [Test]
    public void LaterDays_AlwaysAllow()
    {
        Assert.IsTrue(EndingGate.EvaluationAllowed(5, 2));
        Assert.IsTrue(EndingGate.EvaluationAllowed(3, 3));
    }

    [Test]
    public void UnsetKnob_NeverBricksTheRun()
    {
        // A zero/unset knob must behave like endingsMinDay = 1.
        Assert.IsTrue(EndingGate.EvaluationAllowed(1, 0));
    }
}
