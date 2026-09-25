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
    /// A slot's source: a forced premade not met yet stands there; a forced
    /// premade already met leaves an ordinary traveller; a slot planned for a
    /// rule violator never rolls; every other slot rolls.
    /// </summary>
    public static PremadeSlot SlotSource(bool forcedHere, bool forcedMet, bool violatorSlot)
    {
        if (forcedHere)
            return forcedMet ? PremadeSlot.None : PremadeSlot.Forced;
        return violatorSlot ? PremadeSlot.None : PremadeSlot.Roll;
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
