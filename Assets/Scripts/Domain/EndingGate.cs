/// <summary>
/// Pure rule for when run-ending evaluation is allowed at all.
/// Days below the configured minimum are cushioned: no ending (fired,
/// bankrupt, score) may fire, no matter how badly the shift went. Kept as
/// pure logic so the decision table is unit-testable without ScriptableObjects.
/// </summary>
public static class EndingGate
{
    /// <summary>
    /// True when ending evaluation may run on the given day.
    /// endingsMinDay defaults to 1 in spirit (day 1 always allowed) when the
    /// knob is unset/zero, so a missing config never bricks the run.
    /// </summary>
    public static bool EvaluationAllowed(int currentDay, int endingsMinDay)
    {
        if (endingsMinDay <= 1)
            return true;

        return currentDay >= endingsMinDay;
    }
}
