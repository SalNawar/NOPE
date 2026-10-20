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
/// <item>A day is closed once: an office whose shift was closed elsewhere (a
/// cheat's route) or that a scene change is replacing closes nothing
/// again.</item>
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

    /// <summary>
    /// True when an office's shift is no longer its own to close, so its
    /// clock's closing and its day loop's end are ignored (GameManager): the
    /// shift is not running (<paramref name="shiftRunning"/> false: a cheat's
    /// route already closed the day on the run's world and moved it on, or the
    /// ending was shown at once), or a scene change is replacing the office
    /// (<paramref name="sceneChanging"/>: it stays alive behind the shutter
    /// until the new scene activates). FLOW round 4: a mid-shift Skip day closed
    /// the skipped day a second time on the landing morning and saved it.
    /// </summary>
    public static bool LeftBehind(bool shiftRunning, bool sceneChanging) => !shiftRunning || sceneChanging;
}
