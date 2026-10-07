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

    /// <summary>The Helix River's CRT starts glitching this many points above the firing line, and glitches fully at it (HelixRiver).</summary>
    [Min(0f)]
    public float stabilityWarningMargin = 10f;

    /// <summary>The Helix River's screen flickers within this many points above the firing line (StabilityRules.Band's critical band).</summary>
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

    [Header("Recurring faces (wave 5, lesson 9)")]
    /// <summary>The chance a generated traveller the clerk denies comes back on a later day, same name and face (Returns.Plan; a premade's returns are the day plans'; a traveller comes back once at most; days[].returns caps a day's returning travellers).</summary>
    [Range(0f, 1f)]
    public float returnChance = 0.4f;

    /// <summary>The fewest days after the denial a traveller may come back (1: the next day).</summary>
    [Min(1)]
    public int returnAfterDaysMin = 1;

    /// <summary>The most days after the denial a traveller may come back; they give up after it (read as at least returnAfterDaysMin).</summary>
    [Min(1)]
    public int returnAfterDaysMax = 3;

    /// <summary>The share of returning travellers who come back with corrected papers (honest this time); the rest come back with a new story (every roll made again).</summary>
    [Range(0f, 1f)]
    public float returnCorrectedChance = 0.5f;

    [Header("World (the endings spec §4, §8.2)")]
    /// <summary>The "as you found it" outcome's head start on every factor answered by pulls: the world's inertia (WorldPulls.Lead).</summary>
    [Min(0f)]
    public float worldStatusQuoWeight = DefaultWorldStatusQuoWeight;

    /// <summary>How far ahead an outcome must be to lead its factor (else the factor is split between the top two), and how far another must pass a held lead to take it (WorldPulls.Lead).</summary>
    [Min(0f)]
    public float worldLeadMargin = DefaultWorldLeadMargin;

    /// <summary>A denial's pull toward each factor's "as you found it" outcome: the past stays untouched (WorldPulls.ForDenial). Small: a shift denies many, and at the spec's first cut (0.5) no simulated run ever left 2150 as it found it (balance 2026-09-30).</summary>
    [Min(0f)]
    public float worldDenialPull = 0.05f;

    /// <summary>A rich tourist's scale on their role's pull (a holiday leaves a lighter mark; WorldPulls.KindScale).</summary>
    [Min(0f)]
    public float worldKindScaleRich = 0.5f;

    /// <summary>A poor tourist's scale on their role's pull.</summary>
    [Min(0f)]
    public float worldKindScalePoor = 0.5f;

    /// <summary>A labourer's scale on their role's pull.</summary>
    [Min(0f)]
    public float worldKindScaleLabourer = 1f;

    /// <summary>A displaced person's scale on their role's pull (they go home for good).</summary>
    [Min(0f)]
    public float worldKindScaleDisplaced = 1f;

    /// <summary>The default of <see cref="worldStatusQuoWeight"/> (the endings spec's first cut), also used without a config.</summary>
    public const float DefaultWorldStatusQuoWeight = 6f;

    /// <summary>The default of <see cref="worldLeadMargin"/> (the endings spec's first cut), also used without a config.</summary>
    public const float DefaultWorldLeadMargin = 2f;

    /// <summary>A traveller kind's scale on their role's pull (WorldPulls.KindScale over the four knobs).</summary>
    public float WorldKindScale(TravellerKind kind) =>
        WorldPulls.KindScale(kind, worldKindScaleRich, worldKindScalePoor, worldKindScaleLabourer, worldKindScaleDisplaced);

    [Header("Home / Expenses")]
    /// <summary>Base daily living expense (rent/utilities) deducted at Home.</summary>
    [Min(0)]
    public int baseDailyExpense = 10;

    /// <summary>The sick pet's extra care a night per step of its sickness (the bill's drain line; the house's MedicalDrain ops lower it). The night's optional bills (food, heating, electricity, TV, medicine) are priced in world_source.json home.bills.</summary>
    [Min(0)]
    public int expensePerConditionPoint = 1;

    [Header("Home / Pet (the Home pet spec)")]
    /// <summary>The worst level of each of the pet's needs (hunger, cold, boredom, sickness: 0 well); the player only ever reads them in words (home.pet).</summary>
    [Min(1)]
    public int petNeedMax = 3;

    /// <summary>The pet's nightly chance (0..1) to get a step sicker when well cared for, before hunger, cold, the house and the mood.</summary>
    [Range(0f, 1f)]
    public float conditionWorsenChance = 0.15f;

    /// <summary>What each step of hunger and of cold after tonight's care adds to the pet's chance to get sicker (PetRules.SicknessChance).</summary>
    [Range(0f, 1f)]
    public float sicknessPerNeed = 0.1f;

    /// <summary>The nights in a row the pet may end at the worst of hunger, cold or sickness: the first brings the Animal Welfare Office's notice, this many take the pet (the failure ending; 0: never).</summary>
    [Min(0)]
    public int welfareNights = 2;

    /// <summary>A sick pet's nightly chance to get one step better per point of its mood (the house's and the toys' Mood ops less its boredom; PetRules.RecoveryChance). Saleh's Q3 (2026-09-29): mood both heals and keeps the household well; never shown to the player as a number.</summary>
    [Range(0f, 1f)]
    public float recoveryPerMood = 0.03f;

    /// <summary>The highest nightly recovery chance any mood gives.</summary>
    [Range(0f, 1f)]
    public float maxRecoveryChance = 0.4f;

    /// <summary>How much each point of the pet's mood lowers its nightly chance to get sicker (HomeRules.WorsenChance; Saleh's Q3).</summary>
    [Range(0f, 1f)]
    public float sicknessPerMood = 0.01f;

    /// <summary>The most the mood takes off the pet's nightly chance to get sicker.</summary>
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
        if (returnAfterDaysMax < returnAfterDaysMin)
            Debug.LogWarning($"[GameConfigSO] '{name}': returnAfterDaysMax {returnAfterDaysMax} is below returnAfterDaysMin {returnAfterDaysMin}, so a denied traveller may come back only on the first day of the window.", this);
    }
}
