using System;

/// <summary>
/// The Home phase's family rules (audit R2-021), pure so they are tested
/// headless; HomeEconomy applies them to the run's family, wallet and config.
/// </summary>
public static class HomeRules
{
    /// <summary>A member's points for the medical drain: their condition, never below 0.</summary>
    public static int DrainPoints(int condition) => Math.Max(0, condition);

    /// <summary>Whether a member can be treated: they have a condition to treat and the wallet covers the care cost.</summary>
    public static bool CanTreat(int condition, int money, int careCost) => condition > 0 && money >= careCost;

    /// <summary>A treated member's condition: one point better, never below 0.</summary>
    public static int Treated(int condition) => Math.Max(0, condition - 1);

    /// <summary>A worsened member's condition: one point worse, capped at <paramref name="cap"/>.</summary>
    public static int Worsened(int condition, int cap) => Math.Min(cap, condition + 1);

    /// <summary>
    /// Whether the member at <paramref name="memberIndex"/> worsens tonight:
    /// the first Value() of the member's seed (Seeds.ForFamily of the day seed,
    /// mixed with the member's place in the family, index + 1) below
    /// <paramref name="chance"/>. Deterministic for a run and night, and apart
    /// from every other stream (audit R2-008: the drift used its own hash over
    /// System.Random); each member draws alone, so the family's size never
    /// moves another member's roll.
    /// </summary>
    public static bool Worsens(int daySeed, int memberIndex, float chance) =>
        new SeededRandom(Seeds.Mix(Seeds.ForFamily(daySeed), memberIndex + 1)).Value() < chance;
}
