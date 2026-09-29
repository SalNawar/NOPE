/// <summary>The rolls that can give a traveller their one fault, in K5's order (FaultOrder.Order).</summary>
public enum FaultRoll
{
    /// <summary>The lie roll (Lies.Roll): a place lie or a record lie.</summary>
    Lie,

    /// <summary>The violation roll (the directives' makers; the plan's phase 9).</summary>
    Violation,

    /// <summary>The costume roll (CostumeErrors.Plan).</summary>
    Costume
}

/// <summary>
/// One fault source per traveller (traveller types K5), decided in this
/// order, the first that applies winning and every later roll skipped with
/// no draw: the premade's authoring (a premade never rolls: honest unless
/// authored a liar, CaseFactory); a planned slot (the day's guaranteed
/// violator, P4); a traveller drawn from an honest kind entry
/// (KindWeight.honest: no roll at all); then the rolls of
/// <see cref="Order"/>, each only while no earlier one won. Pure, so the
/// table is tested headless.
/// </summary>
public static class FaultOrder
{
    /// <summary>The rolls in K5's order: the lie roll, the violation roll, the costume roll.</summary>
    public static readonly FaultRoll[] Order = { FaultRoll.Lie, FaultRoll.Violation, FaultRoll.Costume };

    /// <summary>
    /// True when the traveller's fault comes from the rolls at all: not a
    /// premade (<paramref name="premade"/>: their authoring decides), not a
    /// planned slot (<paramref name="plannedSlot"/>: the day's guaranteed
    /// violator stands there) and not drawn from an honest kind entry
    /// (<paramref name="honestEntry"/>). False skips every roll with no draw.
    /// </summary>
    public static bool Rolls(bool premade, bool plannedSlot, bool honestEntry) => !premade && !plannedSlot && !honestEntry;

    /// <summary>
    /// True when <paramref name="roll"/> runs: the traveller rolls at all
    /// (<see cref="Rolls"/>) and no earlier roll of <see cref="Order"/> won
    /// (<paramref name="earlierWon"/>: a lie for the violation roll, a lie or a
    /// violation for the costume roll).
    /// </summary>
    public static bool MayRoll(FaultRoll roll, bool premade, bool plannedSlot, bool honestEntry, bool earlierWon) =>
        Rolls(premade, plannedSlot, honestEntry) && !(earlierWon && roll != FaultRoll.Lie);
}
