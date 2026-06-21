// ReSharper disable InconsistentNaming
using UnityEngine;

/// <summary>
/// Central gameplay tuning (economy, citations, stability).
/// Referenced by RunConfigSO so all systems read one source of truth.
/// </summary>
[CreateAssetMenu(fileName = "GameConfig_", menuName = "TimeDesk/Game Config", order = 2)]
public sealed class GameConfigSO : ScriptableObject
{
    [Header("Pay")]
    /// <summary>Base pay for a correct send (multiplied by WorldState.payRateMultiplier).</summary>
    [Min(0)]
    public int basePayPerCorrect = 10;

    /// <summary>Extra pay for correctly handling a legendary case.</summary>
    [Min(0)]
    public int legendaryBonusPay = 15;

    [Header("Citations (per day)")]
    /// <summary>Wrong sends per day forgiven with a warning only (no money penalty).</summary>
    [Min(0)]
    public int freeWarningsPerDay = 1;

    /// <summary>
    /// Escalating money penalties after free warnings are used.
    /// Index 0 = first penalized citation of the day; past the end, the last value repeats.
    /// </summary>
    public int[] citationPenalties = { 5, 10, 20 };

    [Header("Timeline stability")]
    /// <summary>Stability lost per wrong send (0..100 scale).</summary>
    [Min(0f)]
    public float stabilityLossPerWrong = 5f;

    /// <summary>Extra stability lost when a wrong send involves a legendary.</summary>
    [Min(0f)]
    public float extraStabilityLossLegendary = 10f;

    /// <summary>Stability regained per correct send (usually small or 0).</summary>
    [Min(0f)]
    public float stabilityGainPerCorrect = 0f;

    /// <summary>At or below this stability, the player is fired (run over).</summary>
    public float firedAtStability = 0f;

    [Header("Endings")]
    /// <summary>At or below this money total, the player goes bankrupt (run over).</summary>
    public int bankruptcyMoneyThreshold = -100;

    [Header("Timeline dominance")]
    /// <summary>Top N attributes per profile counted as DOMINANT (big effects).</summary>
    [Min(0)]
    public int dominantPerProfile = 1;

    /// <summary>Next N attributes per profile counted as SUPPORTING (small effects).</summary>
    [Min(0)]
    public int supportingPerProfile = 2;

    [Header("Home / Expenses")]
    /// <summary>Base daily living expense (rent/utilities) deducted at Home.</summary>
    [Min(0)]
    public int baseDailyExpense = 10;

    /// <summary>Additional daily expense per family member.</summary>
    [Min(0)]
    public int expensePerFamilyMember = 5;

    /// <summary>Ongoing medical drain per point of a family member's condition.</summary>
    [Min(0)]
    public int expensePerConditionPoint = 1;

    /// <summary>Credits cost to treat one point of a family member's condition.</summary>
    [Min(0)]
    public int conditionCareCost = 8;

    /// <summary>Chance (0..1) an untreated family member's condition worsens by 1 overnight.</summary>
    [Range(0f, 1f)]
    public float conditionWorsenChance = 0.25f;

    /// <summary>Maximum value a family member's condition can reach.</summary>
    [Min(0)]
    public int maxFamilyCondition = 10;

    [Header("Home / Slot Machine")]
    /// <summary>Credits cost to spin the slot machine once.</summary>
    [Min(0)]
    public int slotSpinCost = 10;

    /// <summary>
    /// Returns the money penalty for the Nth penalized citation of the day (1-based).
    /// </summary>
    public int GetCitationPenalty(int penalizedCitationIndex1Based)
    {
        if (citationPenalties == null || citationPenalties.Length == 0)
            return 0;

        int idx = Mathf.Clamp(penalizedCitationIndex1Based - 1, 0, citationPenalties.Length - 1);
        return Mathf.Max(0, citationPenalties[idx]);
    }
}
