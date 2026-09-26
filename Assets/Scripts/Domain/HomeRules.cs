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
    /// one draw from System.Random seeded by the day seed and the member's
    /// place in the family (seed × 397 XOR (index + 1) × 104729), below
    /// <paramref name="chance"/>. Deterministic for a run and day; its own
    /// hash, not a Seeds stream (audit R2-008 would move it to one, a
    /// deliberate change of every night's drift).
    /// </summary>
    public static bool Worsens(int daySeed, int memberIndex, float chance)
    {
        unchecked
        {
            int memberSeed = daySeed * 397 ^ (memberIndex + 1) * 104729;
            return new Random(memberSeed).NextDouble() < chance;
        }
    }
}
