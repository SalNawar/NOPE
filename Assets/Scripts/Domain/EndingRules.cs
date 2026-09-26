using System.Collections.Generic;

/// <summary>
/// Condition type for an ending. Unused fields are ignored per type
/// (EndingRules.Met decides each). Serialized as ints: append only.
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

/// <summary>The run's numbers an ending condition reads, taken when a check runs (EndingRules.Met).</summary>
public readonly struct EndingCheck
{
    /// <summary>Timeline stability now.</summary>
    public readonly float Stability;

    /// <summary>The wallet now.</summary>
    public readonly int Money;

    /// <summary>The run's day now.</summary>
    public readonly int Day;

    /// <summary>The firing line: stability at or below it is Fired (GameConfigSO.firedAtStability).</summary>
    public readonly float FiredAtStability;

    /// <summary>The bankruptcy line: money at or below it is Bankrupt (GameConfigSO.bankruptcyMoneyThreshold).</summary>
    public readonly int BankruptAtMoney;

    /// <summary>Creates a check's numbers.</summary>
    public EndingCheck(float stability, int money, int day, float firedAtStability, int bankruptAtMoney)
    {
        Stability = stability;
        Money = money;
        Day = day;
        FiredAtStability = firedAtStability;
        BankruptAtMoney = bankruptAtMoney;
    }
}

/// <summary>
/// The ending rules, pure so they are tested headless: whether each condition
/// holds (Met), and which ending a check picks (Select): failures end a run at
/// any moment; the milestone (Retirement) only at the day boundary, where a
/// reached attribute epilogue replaces it.
/// </summary>
public static class EndingRules
{
    /// <summary>The firing rule: stability at or below the firing line (ShiftScoring's firedNow is the same rule).</summary>
    public static bool IsFired(float stability, float firedAtStability) => stability <= firedAtStability;

    /// <summary>
    /// Whether an ending's condition holds now (audit R2-007, R2-021): Fired
    /// at or below the firing line; Bankrupt at or below the bankruptcy line;
    /// AttrTotalAtLeast when the ending's attribute total reaches
    /// <paramref name="threshold"/> (<paramref name="attributeTotal"/> is null
    /// when the ending names no attribute: never met); DayAtLeast from day
    /// <paramref name="threshold"/>; any other type never.
    /// </summary>
    public static bool Met(EndingConditionType type, float threshold, float? attributeTotal, EndingCheck now)
    {
        switch (type)
        {
            case EndingConditionType.Fired: return IsFired(now.Stability, now.FiredAtStability);
            case EndingConditionType.Bankrupt: return now.Money <= now.BankruptAtMoney;
            case EndingConditionType.AttrTotalAtLeast: return attributeTotal.HasValue && attributeTotal.Value >= threshold;
            case EndingConditionType.DayAtLeast: return now.Day >= threshold;
            default: return false;
        }
    }

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
