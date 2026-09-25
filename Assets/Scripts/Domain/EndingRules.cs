using System.Collections.Generic;

/// <summary>
/// Condition type for an ending. Unused fields are ignored per type
/// (see EndingService for evaluation logic). Serialized as ints: append only.
/// </summary>
public enum EndingConditionType
{
    /// <summary>Timeline stability has dropped to GameConfigSO.firedAtStability or below.</summary>
    Fired,

    /// <summary>Player money has dropped to GameConfigSO.bankruptcyMoneyThreshold or below.</summary>
    Bankrupt,

    /// <summary>A global attribute total (TimelineKeys.GlobalAttr) is >= threshold. Uses attribute + threshold.</summary>
    AttrTotalAtLeast,

    /// <summary>Current day is >= threshold. Uses threshold.</summary>
    DayAtLeast
}

/// <summary>What an ending is to the run.</summary>
public enum EndingKind
{
    /// <summary>The run fails (fired, bankrupt): checked at every moment.</summary>
    Failure,

    /// <summary>The run is complete (Retirement): checked only at the day boundary.</summary>
    Milestone,

    /// <summary>An attribute ending that replaces a reached milestone at the day boundary.</summary>
    Epilogue
}

/// <summary>When an ending check runs.</summary>
public enum EndingMoment
{
    /// <summary>After a verdict, or at the end of a shift that applied dialog consequences.</summary>
    Immediate,

    /// <summary>At sleep, before the nightly resolve.</summary>
    DayBoundary
}

/// <summary>One ending as the selection rule sees it.</summary>
public readonly struct EndingCandidate
{
    /// <summary>The ending's kind (EndingRules.KindOf).</summary>
    public readonly EndingKind Kind;

    /// <summary>Its priority (higher wins).</summary>
    public readonly int Priority;

    /// <summary>Whether its condition holds now.</summary>
    public readonly bool Met;

    /// <summary>Creates a candidate.</summary>
    public EndingCandidate(EndingKind kind, int priority, bool met)
    {
        Kind = kind;
        Priority = priority;
        Met = met;
    }
}

/// <summary>
/// Which ending a check picks, pure so it is tested headless: failures end a
/// run at any moment; the milestone (Retirement) only at the day boundary,
/// where a reached attribute epilogue replaces it.
/// </summary>
public static class EndingRules
{
    /// <summary>Fired and Bankrupt are failures, DayAtLeast a milestone, AttrTotalAtLeast an epilogue; any other value a failure.</summary>
    public static EndingKind KindOf(EndingConditionType type)
    {
        switch (type)
        {
            case EndingConditionType.DayAtLeast: return EndingKind.Milestone;
            case EndingConditionType.AttrTotalAtLeast: return EndingKind.Epilogue;
            default: return EndingKind.Failure;
        }
    }

    /// <summary>
    /// The index of the winning candidate, or -1. Only met candidates count:
    /// at an Immediate check only failures; at the day boundary failures and
    /// milestones, and epilogues only when some milestone is met. The highest
    /// priority wins; ties go to the lowest index (library order).
    /// </summary>
    public static int Select(IReadOnlyList<EndingCandidate> candidates, EndingMoment moment)
    {
        if (candidates == null)
            return -1;

        bool milestoneMet = false;
        foreach (EndingCandidate c in candidates)
            milestoneMet |= c.Met && c.Kind == EndingKind.Milestone;

        int best = -1;
        for (int i = 0; i < candidates.Count; i++)
        {
            EndingCandidate c = candidates[i];
            bool counts = c.Met && (c.Kind == EndingKind.Failure ||
                                    (moment == EndingMoment.DayBoundary && (c.Kind == EndingKind.Milestone || (c.Kind == EndingKind.Epilogue && milestoneMet))));
            if (counts && (best < 0 || c.Priority > candidates[best].Priority))
                best = i;
        }

        return best;
    }
}
