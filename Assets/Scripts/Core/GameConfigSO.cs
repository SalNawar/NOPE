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

    [Header("Citations (one penalty per wrong decision)")]
    /// <summary>Wrong decisions per day forgiven with a warning only (no money penalty; the slip says so); 0 fines every mistake (Saleh: "one free warning but configurable and tunable").</summary>
    [Min(0)]
    public int freeWarningsPerDay = 1;

    /// <summary>
    /// The one money penalty for a wrong decision past the free warnings: a
    /// wrong accept, a wrong deny or an unproven denial, whatever the fault,
    /// the same every time (VerdictRules.WrongDecisionPenalty; redesign phase 23).
    /// </summary>
    [Min(0)]
    public int wrongDecisionPenalty = 10;

    [Header("Strandings and the waiver (the endings and strandings spec, Saleh's answers 2026-09-30)")]
    /// <summary>
    /// The stranding fine the agency's failure report charges the desk for a
    /// stranded traveller who had no valid signed waiver on file (Saleh's Q10 =
    /// D: a separate amount, chosen knowingly as the one exception to the
    /// one-penalty rule; no free warning applies). 0 turns it off.
    /// </summary>
    [Min(0)]
    public int strandingFine = 100;

    /// <summary>What a personality's tilt multiplies its stranding fate's weight by (Personality.strandingFate; the spec's §6.2: 2 doubles it; 1 tilts nothing).</summary>
    [Min(0f)]
    public float strandingFateTilt = 2f;

    /// <summary>The shift minutes a waiver signed from the desk's pad takes (Saleh's Q15 = A: time only, about one question's worth; ShiftClock.Spend).</summary>
    [Min(0f)]
    public float waiverSignMinutes = 5f;

    [Header("Evidence (deny gating)")]
    /// <summary>
    /// When true, denying a liar without documented scanner evidence earns a
    /// citation + deduction even though the visitor really was lying.
    /// </summary>
    public bool requireEvidenceToDeny = true;

    [Header("Shift clock")]
    /// <summary>Hour the booth opens (0-23). The clock shows this during the briefing and starts at Start Shift.</summary>
    [Range(0, 23)]
    public int shiftStartHour = 9;

    /// <summary>Hour the booth closes (1-24, after the opening hour). No new traveller is called after it.</summary>
    [Range(1, 24)]
    public int shiftEndHour = 17;

    /// <summary>Real seconds a whole shift lasts (the Papers, Please-style time pressure).</summary>
    [Min(10f)]
    public float shiftRealSeconds = 480f;

    [Header("Timeline stability (x.xx, changes compound)")]
    /// <summary>
    /// The share of stability one point of change moves (StabilityRules.Apply;
    /// redesign phase 23 part 1b): a loss takes rate x points of the current
    /// value, a gain closes rate x points of the gap to 100, so changes are
    /// small and compound. 0.005 = 0.5 % a point.
    /// </summary>
    [Range(0f, 0.05f)]
    public float stabilityChangeRate = 0.005f;

    /// <summary>Points of change a wrong decision costs (5 at 0.5 % a point: 2.5 % of the current stability).</summary>
    [Min(0f)]
    public float stabilityLossPerWrong = 5f;

    /// <summary>Extra points a wrong decision on a premade costs (Saleh: "this is a different consequence").</summary>
    [Min(0f)]
    public float extraStabilityLossLegendary = 10f;

    /// <summary>Points of change a correct decision gains, closing a share of the gap to 100 (usually 0).</summary>
    [Min(0f)]
    public float stabilityGainPerCorrect = 0f;

    /// <summary>At or below this stability, the player is fired (run over). Compounding losses never reach 0, so the line sits above it (set with the balance simulation).</summary>
    [Range(0f, 100f)]
    public float firedAtStability = 60f;

    /// <summary>The office's stability readout turns amber within this many points above the firing line (StabilityRules.Band).</summary>
    [Min(0f)]
    public float stabilityWarningMargin = 10f;

    /// <summary>The office's stability readout turns red within this many points above the firing line.</summary>
    [Min(0f)]
    public float stabilityCriticalMargin = 3f;

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

    [Header("History")]
    /// <summary>Influence a nation needs, strictly above this, to start leading the timeline (and open its Future).</summary>
    public float leaderFloor = 4f;

    /// <summary>A leader loses the lead when its influence falls to this or below (kept below leaderFloor, so a one-send dip does not flip the Future).</summary>
    public float leaderKeepFloor = 2f;

    /// <summary>A challenger replaces the leader only when its influence exceeds the leader's by more than this.</summary>
    [Min(0f)]
    public float leaderMargin = 2f;

    /// <summary>How often the same (true home, claimed place) pair must be accepted before its carry latches.</summary>
    [Min(1)]
    public int carryThreshold = 1;

    /// <summary>The fact an accepted liar carries from their true home into the claimed place (one of the five editable categories).</summary>
    public ClueCategory carryCategory = ClueCategory.Technology;

    /// <summary>Most templated history news lines a night (the leader's first, then carries).</summary>
    [Min(1)]
    public int maxHistoryNewsPerNight = 3;

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

    /// <summary>A sick member's nightly chance to get one point better per point of the household's mood (the house upgrades' Mood ops; HomeRules.MoodShare). Saleh's Q3 (2026-09-29): mood both heals and keeps the family well, each a little weaker than recovery alone was (0.04); never shown to the player as a number.</summary>
    [Range(0f, 1f)]
    public float recoveryPerMood = 0.03f;

    /// <summary>The highest nightly recovery chance any mood gives.</summary>
    [Range(0f, 1f)]
    public float maxRecoveryChance = 0.4f;

    /// <summary>How much each point of the household's mood lowers a member's nightly chance to get worse (HomeRules.WorsenChance; Saleh's Q3).</summary>
    [Range(0f, 1f)]
    public float sicknessPerMood = 0.01f;

    /// <summary>The most the mood takes off a member's nightly chance to get worse.</summary>
    [Range(0f, 1f)]
    public float maxMoodSicknessCut = 0.1f;

    [Header("Home / Break-ins")]
    /// <summary>The first night someone may break in while the clerk is at work (HomeRules.BreakIn). Saleh's Q2 (2026-09-29): "it will start mid second week": day 11, the middle of days 8-14 when week one is days 1-7.</summary>
    [Min(1)]
    public int breakInFromDay = 11;

    /// <summary>The nightly chance of a break-in, before the house's locks and alarm (BreakInChance ops).</summary>
    [Range(0f, 1f)]
    public float breakInChance = 0.08f;

    /// <summary>The share of a positive wallet a break-in takes, before the strongbox (BreakInShare ops).</summary>
    [Range(0f, 1f)]
    public float breakInShare = 0.25f;

    /// <summary>The most a break-in takes, in credits.</summary>
    [Min(0)]
    public int breakInMaxLoss = 60;

    [Header("Home / Slot Machine")]
    /// <summary>Credits cost to spin the slot machine once.</summary>
    [Min(0)]
    public int slotSpinCost = 10;

    [Header("Citizen Account (redesign phase 25)")]
    /// <summary>The most days the clerk's statement keeps (WorldState.accountDays); the oldest go first.</summary>
    [Min(1)]
    public int accountDaysKept = 60;

    /// <summary>Warns about history knobs that would silently disable a rule.</summary>
    private void OnValidate()
    {
        if (!History.IsEditable(carryCategory))
            Debug.LogWarning($"[GameConfigSO] '{name}': carryCategory {carryCategory} is not a category history may edit, so carries would silently never record.", this);
        if (leaderKeepFloor > leaderFloor)
            Debug.LogWarning($"[GameConfigSO] '{name}': leaderKeepFloor {leaderKeepFloor} is above leaderFloor {leaderFloor} and counts as the floor (no hysteresis at the floor).", this);
    }
}
