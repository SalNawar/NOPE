/// <summary>
/// Who slips (the personalities spec's T9): a generated traveller of Lying
/// intent (a place lie, smuggling included, or a record lie) rolls once
/// against the day's slipChance on their own stream (Seeds.ForSlip); an honest
/// traveller never rolls and never slips; a premade never rolls (rule 4: a
/// liar premade slips when their slip line is authored). Pure.
/// </summary>
public static class Slips
{
    /// <summary>True when the traveller rolls: Lying intent and not a premade.</summary>
    public static bool Rolls(ReactionIntent intent, bool premade) => intent == ReactionIntent.Lying && !premade;

    /// <summary>One draw of <paramref name="rng"/>: true below <paramref name="chance"/> (0 never, 1 always). No draw without a stream.</summary>
    public static bool Roll(float chance, IRandomSource rng) => rng != null && rng.Value() < chance;

    /// <summary>A premade liar slips when their slip line is authored (<paramref name="authored"/>); an honest premade never does.</summary>
    public static bool PremadeSlips(ReactionIntent intent, bool authored) => intent == ReactionIntent.Lying && authored;
}
