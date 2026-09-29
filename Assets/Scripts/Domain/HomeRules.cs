using System;

/// <summary>
/// The Home phase's family rules (audit R2-021) and the house upgrades'
/// (the adjusted knobs, recovery and the break-in), pure so they are tested
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

    // ---- The house upgrades (docs/superpowers/specs/2026-09-30-home-upgrades-design.md HU4, HU6, HU7) ----

    /// <summary>A household knob with the house upgrades' summed effect ops added (the worsen chance, a break-in's chance or share): <paramref name="value"/> + <paramref name="bonus"/>, never below 0.</summary>
    public static float Adjusted(float value, float bonus) => Math.Max(0f, value + bonus);

    /// <summary>A household cost in cr with the house upgrades' summed effect ops added (rent and utilities, care, the medical drain per point, upkeep from 0): <see cref="Adjusted"/>, rounded half to even.</summary>
    public static int Cost(int cost, float bonus) => (int)Math.Round(Adjusted(cost, bonus));

    /// <summary>A sick member's nightly chance to get one point better: the household's <paramref name="mood"/> times <paramref name="perPoint"/>, from 0 up to <paramref name="cap"/> (a negative cap is none).</summary>
    public static float RecoveryChance(float mood, float perPoint, float cap) =>
        Math.Min(Math.Max(0f, cap), Math.Max(0f, mood * perPoint));

    /// <summary>
    /// Whether the member at <paramref name="memberIndex"/> gets better tonight:
    /// the first Value() of the member's recovery seed (Seeds.ForRecovery of
    /// the day seed, mixed with index + 1, as the drift's is) below
    /// <paramref name="chance"/>; its own stream, so a recovery never moves the drift.
    /// </summary>
    public static bool Recovers(int daySeed, int memberIndex, float chance) =>
        new SeededRandom(Seeds.Mix(Seeds.ForRecovery(daySeed), memberIndex + 1)).Value() < chance;

    /// <summary>A member's condition after the night: one point worse (up to <paramref name="cap"/>) when they worsen, else one point better when they recover, else unchanged.</summary>
    public static int Night(int condition, bool worsens, bool recovers, int cap) =>
        worsens ? Worsened(condition, cap) : recovers ? Treated(condition) : condition;

    /// <summary>
    /// Whether someone broke in while the clerk was at work on
    /// <paramref name="day"/>: never before <paramref name="fromDay"/>, else the
    /// first Value() of the night's break-in stream (Seeds.ForBreakIns) below
    /// <paramref name="chance"/>. Deterministic, so a reload or Continue replays it.
    /// </summary>
    public static bool BreakIn(int day, int fromDay, int daySeed, float chance) =>
        day >= fromDay && new SeededRandom(Seeds.ForBreakIns(daySeed)).Value() < chance;

    /// <summary>What a break-in takes: <paramref name="share"/> of a positive wallet, rounded half to even, at most <paramref name="maxLoss"/>; nothing from an empty wallet or one in debt.</summary>
    public static int BreakInLoss(int money, float share, int maxLoss) =>
        money <= 0 ? 0 : Math.Min(Math.Max(0, maxLoss), (int)Math.Round(money * Math.Max(0f, share)));
}
