using System.Collections.Generic;

/// <summary>Where a slot's traveller comes from, as far as premades go.</summary>
public enum PremadeSlot
{
    /// <summary>The slot's forced premade stands here.</summary>
    Forced,

    /// <summary>The slot may roll a premade from the day's pool.</summary>
    Roll,

    /// <summary>An ordinary traveller stands here (no roll).</summary>
    None
}

/// <summary>
/// Premade scheduling rules: which slots hold a premade, who may still roll,
/// and the roll itself (moved from CaseFactory onto the premade stream,
/// Seeds.ForLegendary). Pure, so the decision tables are tested headless.
/// </summary>
public static class Premades
{
    /// <summary>The longest Citizen Records note a premade may carry (the note box holds about three lines of 60 characters).</summary>
    public const int MaxNoteLength = 120;

    /// <summary>
    /// A slot's source: where a premade is forced (<paramref name="forcedHere"/>),
    /// the slot's standing appearance's premade stands there
    /// (<paramref name="premadeStands"/>, <see cref="Appearance"/>), else an
    /// ordinary traveller does (a met premade, or conditions that failed at
    /// the day's start), never a roll; a slot planned for a rule violator
    /// never rolls; every other slot rolls. A slot nothing is forced into
    /// reads no condition.
    /// </summary>
    public static PremadeSlot SlotSource(bool forcedHere, bool premadeStands, bool violatorSlot)
    {
        if (forcedHere)
            return premadeStands ? PremadeSlot.Forced : PremadeSlot.None;
        return violatorSlot ? PremadeSlot.None : PremadeSlot.Roll;
    }

    /// <summary>
    /// Whether one forced entry stands today (days 7-15 B9): its conditions
    /// pass on the day-start snapshot, and its premade is not a once-per-run
    /// premade already met (a repeatable premade is never kept out by a met
    /// flag; an entry with no premade passes <paramref name="oncePerRun"/> false).
    /// </summary>
    public static bool Stands(bool oncePerRun, bool met, bool conditionsPass) => conditionsPass && !(oncePerRun && met);

    /// <summary>
    /// A slot's appearance among its forced entries (days 7-15 B9, the
    /// alternatives of a beat): the index of the first entry that stands
    /// (<see cref="Stands"/>, in the authored order), so an author writes
    /// if / else-if top to bottom; -1 when none stands (an ordinary traveller)
    /// or there are none.
    /// </summary>
    public static int Appearance(IReadOnlyList<bool> standing)
    {
        if (standing == null)
            return -1;
        for (int i = 0; i < standing.Count; i++)
            if (standing[i])
                return i;
        return -1;
    }

    /// <summary>
    /// Whether a pooled premade may roll: not met this run and their name free
    /// today. Forced premades' names are reserved before slot 1, which is what
    /// keeps them out of the day's roll.
    /// </summary>
    public static bool IsRollable(bool met, bool nameTaken) => !met && !nameTaken;

    /// <summary>
    /// The roll: -1 with no draw when there is no candidate; otherwise, unless
    /// <paramref name="forcedByCheat"/>, one Value draw and -1 when it is not
    /// below <paramref name="chance"/>; then one Range draw picks the candidate.
    /// </summary>
    public static int Roll(float chance, int candidates, bool forcedByCheat, IRandomSource rng)
    {
        if (candidates <= 0 || rng == null)
            return -1;

        if (!forcedByCheat && !(rng.Value() < chance))
            return -1;

        return rng.Range(0, candidates);
    }
}
