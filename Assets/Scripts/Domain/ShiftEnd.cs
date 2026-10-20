/// <summary>
/// How a shift ends (FLOW: BUG-28b, BUG-32a, BUG-56a; FIX_PLAN_v2's D12 and
/// TIMELINE-FAIL's Q4 = A). Pure; GameManager follows it.
/// <list type="bullet">
/// <item>Closing never abandons a decided traveller: while their verdict
/// (the ribbon, a citation slip) is still showing, the slot is finished first,
/// so the report is never drawn over the slip.</item>
/// <item>The report waits for the last traveller's payoff: their reaction and
/// walk-off play first, up to a cap.</item>
/// <item>A run that reached an ending during the shift ends at the day's end:
/// the report first, then the ending (never Home).</item>
/// </list>
/// </summary>
public static class ShiftEnd
{
    /// <summary>What closing does now: finish the slot when a traveller stands at the desk (<paramref name="travellerAtDesk"/>) or a decided one's verdict is still showing (<paramref name="verdictShowing"/>), else close at once.</summary>
    public static ClosingAction OnClosing(bool travellerAtDesk, bool verdictShowing) =>
        ShiftFlow.OnClosing(travellerAtDesk || verdictShowing);

    /// <summary>True when the shift report may show: the last traveller has left (<paramref name="departing"/> false), or the wait reached <paramref name="maxSeconds"/>.</summary>
    public static bool PayoffDone(bool departing, float waitedSeconds, float maxSeconds) =>
        !departing || !(waitedSeconds < maxSeconds);

    /// <summary>True when the day ends in the run's ending (<paramref name="endingId"/> set during the shift or at its close), not at Home.</summary>
    public static bool EndsTheRun(string endingId) => !string.IsNullOrEmpty(endingId);
}
