/// <summary>What the booth does when the shift clock reaches closing time.</summary>
public enum ClosingAction
{
    /// <summary>A traveller is being processed: let the player finish, then end the day.</summary>
    FinishCurrent,

    /// <summary>Nobody at the desk: end the day now (a traveller behind READY is never called).</summary>
    CloseNow,
}

/// <summary>Shift closing rules (decision table covered by ShiftFlowTests).</summary>
public static class ShiftFlow
{
    /// <summary>Chooses the closing action from whether a traveller is at the desk.</summary>
    public static ClosingAction OnClosing(bool travellerAtDesk) =>
        travellerAtDesk ? ClosingAction.FinishCurrent : ClosingAction.CloseNow;
}
