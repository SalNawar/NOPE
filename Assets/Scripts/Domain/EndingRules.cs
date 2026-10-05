using System.Collections.Generic;

/// <summary>
/// Condition type for an ending. Unused fields are ignored per type
/// (EndingRules.Met decides each). Serialized as ints: append only; a retired
/// value keeps its number (EndingRules.IsRetired).
/// </summary>
public enum EndingConditionType
{
    /// <summary>Timeline stability has dropped to GameConfigSO.firedAtStability or below.</summary>
    Fired,

    /// <summary>Player money has dropped to GameConfigSO.bankruptcyMoneyThreshold or below.</summary>
    Bankrupt,

    /// <summary>
    /// Retired (2026-09-29, Saleh: "we dont make judgements"): was an attribute
    /// epilogue that replaced the day-15 ending when a global attribute total
    /// reached its threshold. Kept for its number; never met, and the content
    /// validator rejects an ending that still uses it.
    /// </summary>
    AttrTotalAtLeast,

    /// <summary>Current day is >= threshold. Uses threshold.</summary>
    DayAtLeast,

    /// <summary>The Animal Welfare Office took the pet (PetState.taken; the Home pet spec PS6): the failure that replaces the family's.</summary>
    PetTaken
}

/// <summary>What an ending is to the run.</summary>
public enum EndingKind
{
    /// <summary>The run fails (fired, bankrupt, the pet taken): checked at every moment.</summary>
    Failure,

    /// <summary>The run reaches its last day (the neutral "world you made" ending, which shows the world summary): checked only at the day boundary.</summary>
    Milestone
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

    /// <summary>The Welfare Office has taken the pet (PetState.taken).</summary>
    public readonly bool PetTaken;

    /// <summary>Creates a check's numbers.</summary>
    public EndingCheck(float stability, int money, int day, float firedAtStability, int bankruptAtMoney, bool petTaken = false)
    {
        Stability = stability;
        Money = money;
        Day = day;
        FiredAtStability = firedAtStability;
        BankruptAtMoney = bankruptAtMoney;
        PetTaken = petTaken;
    }
}

/// <summary>
/// The ending rules, pure so they are tested headless: whether each condition
/// holds (Met), and which ending a check picks (Select): failures end a run at
/// any moment; the run's last day (the neutral "world you made" ending) only
/// at the day boundary. No ending judges the world the run made (Saleh,
/// 2026-09-29): the attribute epilogues are retired (IsRetired).
/// </summary>
public static class EndingRules
{
    /// <summary>The firing rule: stability at or below the firing line (ShiftScoring's firedNow is the same rule).</summary>
    public static bool IsFired(float stability, float firedAtStability) => stability <= firedAtStability;

    /// <summary>True for a retired condition (AttrTotalAtLeast, the attribute epilogues): never met, and an ending using it is a content error.</summary>
    public static bool IsRetired(EndingConditionType type) => type == EndingConditionType.AttrTotalAtLeast;

    /// <summary>
    /// Whether an ending's condition holds now (audit R2-007, R2-021): Fired
    /// at or below the firing line; Bankrupt at or below the bankruptcy line;
    /// DayAtLeast from day <paramref name="threshold"/>; PetTaken once the
    /// Welfare Office took the pet; a retired type (AttrTotalAtLeast) and any
    /// other type never.
    /// </summary>
    public static bool Met(EndingConditionType type, float threshold, EndingCheck now)
    {
        switch (type)
        {
            case EndingConditionType.Fired: return IsFired(now.Stability, now.FiredAtStability);
            case EndingConditionType.Bankrupt: return now.Money <= now.BankruptAtMoney;
            case EndingConditionType.DayAtLeast: return now.Day >= threshold;
            case EndingConditionType.PetTaken: return now.PetTaken;
            default: return false;
        }
    }

    /// <summary>DayAtLeast is the run's last day (a milestone); every other value a failure (a retired type is never met, so its kind never counts).</summary>
    public static EndingKind KindOf(EndingConditionType type) =>
        type == EndingConditionType.DayAtLeast ? EndingKind.Milestone : EndingKind.Failure;

    /// <summary>
    /// The index of the winning candidate, or -1. Only met candidates count:
    /// at an Immediate check only failures; at the day boundary failures and
    /// milestones. The highest priority wins; ties go to the lowest index
    /// (library order).
    /// </summary>
    public static int Select(IReadOnlyList<EndingCandidate> candidates, EndingMoment moment)
    {
        if (candidates == null)
            return -1;

        int best = -1;
        for (int i = 0; i < candidates.Count; i++)
        {
            EndingCandidate c = candidates[i];
            bool counts = c.Met && (c.Kind == EndingKind.Failure || (moment == EndingMoment.DayBoundary && c.Kind == EndingKind.Milestone));
            if (counts && (best < 0 || c.Priority > candidates[best].Priority))
                best = i;
        }

        return best;
    }
}
