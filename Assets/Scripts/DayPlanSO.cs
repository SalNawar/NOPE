// ReSharper disable InconsistentNaming
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Designer-authored plan for a single day.
/// Owns:
/// - Day identity (dayNumber)
/// - How many travellers queue that day (visitorsCount; the shift clock may close first)
/// - Procedural generation knobs (the kinds' blueprints and weights, eras, the premade pool and chance)
/// - Where today's liars may leak tells (tell count and tell channels)
/// - Forced slots (a blueprint, a premade or both: "3rd case on day 1 is Senenmut")
/// - Event rules (fixed or random placement, including "random but after N cases")
/// </summary>
[CreateAssetMenu(menuName = "TimeDesk/Day/Day Plan", fileName = "DayPlan_")]
public sealed class DayPlanSO : ScriptableObject
{
    // -----------------------------
    // Identity
    // -----------------------------

    /// <summary>Which day this plan represents (1-based).</summary>
    [SerializeField, Min(1)] private int dayNumber = 1;

    // -----------------------------
    // Day flow
    // -----------------------------

    /// <summary>Queue size: most travellers this day can hold. The shift clock usually closes the booth first.</summary>
    [SerializeField, Min(1)] private int visitorsCount = 6;

    // -----------------------------
    // Procedural generation
    // -----------------------------

    /// <summary>The day's traveller mix (traveller types K1): each kind's blueprint and weight, one weighted draw on the case stream (written by Generate World from days[].kinds).</summary>
    [SerializeField] private KindWeight[] kinds;

    /// <summary>Weighted set of eras to pick the claimed (home) era from (optional).</summary>
    [SerializeField] private EraWeight[] eraWeights;

    /// <summary>Countries travellers may come from today (empty = every country with a place in today's eras).</summary>
    [SerializeField] private NationSO[] allowedNations;

    /// <summary>Base chance per slot to hold a premade from the day's pool (0..1; written by Generate World).</summary>
    [SerializeField, Range(0f, 1f)] private float legendaryBaseChance = 0.05f;

    /// <summary>The premades that may roll this day (written by Generate World from days[].premades).</summary>
    [SerializeField] private LegendarySO[] availableLegendaries;

    /// <summary>
    /// How many tells each liar's disguise leaks today (at least 1; capped per
    /// liar at the categories that can carry a tell). Written by
    /// Tools > TimeDesk > Generate World from world_source.json.
    /// </summary>
    [SerializeField, Min(1)] private int tellCount = 1;

    /// <summary>
    /// Where today's liars may leak tells: Papers (their documents) and/or
    /// Answer (their answers to today's questions). Written by
    /// Tools > TimeDesk > Generate World from world_source.json days[].channels;
    /// the default keeps a day plan that does not set it on papers-only tells.
    /// </summary>
    [SerializeField] private TellChannel[] tellChannels = { TellChannel.Papers };

    // -----------------------------
    // Scripted overrides
    // -----------------------------

    /// <summary>Travel restrictions active this day (announced in the briefing).</summary>
    [SerializeField] private TravelRuleSO[] activeTravelRules;

    /// <summary>
    /// Each active rule sends at least one violator, placed in the first half
    /// of the queue, so the day's directives are always tested.
    /// </summary>
    [SerializeField] private bool guaranteeRuleViolators = true;

    /// <summary>Forced slots (1-based): a blueprint, a premade or both (written by Generate World from days[].forced).</summary>
    [SerializeField] private List<ForcedCaseSlot> forcedCases = new();

    /// <summary>Event rules (fixed or random placement).</summary>
    [SerializeField] private List<DayEventRule> eventRules = new();

    /// <summary>Public read-only day number.</summary>
    public int DayNumber => dayNumber;

    /// <summary>Public read-only number of visitors/cases.</summary>
    public int VisitorsCount => visitorsCount;

    /// <summary>The day's traveller mix: each kind's blueprint and weight, in authored order.</summary>
    public IReadOnlyList<KindWeight> Kinds => kinds ?? Array.Empty<KindWeight>();

    /// <summary>The blueprints the day's mix draws from (set entries only, in authored order).</summary>
    public IEnumerable<CaseBlueprintSO> PossibleBlueprints
    {
        get
        {
            foreach (KindWeight k in Kinds)
                if (k != null && k.blueprint != null)
                    yield return k.blueprint;
        }
    }

    /// <summary>Public read-only era weights.</summary>
    public IReadOnlyList<EraWeight> EraWeights => eraWeights;

    /// <summary>The chance per slot to roll a premade from the pool.</summary>
    public float LegendaryBaseChance => legendaryBaseChance;

    /// <summary>The premades that may roll this day.</summary>
    public IReadOnlyList<LegendarySO> AvailableLegendaries => availableLegendaries;

    /// <summary>The forced slots, in authored order.</summary>
    public IReadOnlyList<ForcedCaseSlot> ForcedCases => forcedCases;

    /// <summary>Tells each liar leaks today (at least 1).</summary>
    public int TellCount => tellCount;

    /// <summary>Where today's liars may leak tells (empty when unset).</summary>
    public IReadOnlyList<TellChannel> TellChannels => tellChannels ?? Array.Empty<TellChannel>();

    /// <summary>Public read-only travel rules active this day.</summary>
    public IReadOnlyList<TravelRuleSO> ActiveTravelRules => activeTravelRules ?? System.Array.Empty<TravelRuleSO>();

    /// <summary>Whether each active rule is guaranteed a violator in the first half of the queue.</summary>
    public bool GuaranteeRuleViolators => guaranteeRuleViolators;

    /// <summary>Every forced case's blueprint (set slots only, in authored order); the content validator counts their documents.</summary>
    public IEnumerable<CaseBlueprintSO> ForcedBlueprints
    {
        get
        {
            foreach (ForcedCaseSlot slot in forcedCases)
                if (slot != null && slot.caseBlueprint != null)
                    yield return slot.caseBlueprint;
        }
    }

    /// <summary>True when travellers may come from this country today.</summary>
    public bool AllowsNation(NationSO nation) =>
        nation != null && (allowedNations == null || allowedNations.Length == 0 || Array.IndexOf(allowedNations, nation) >= 0);

    /// <summary>True when this era can appear today (a positive era weight, or no weights at all).</summary>
    public bool IncludesEra(EraSO era)
    {
        if (era == null)
            return false;

        if (eraWeights == null || eraWeights.Length == 0)
            return true;

        foreach (EraWeight w in eraWeights)
            if (w.era == era && w.weight > 0f)
                return true;

        return false;
    }

    /// <summary>
    /// Returns true if every active rule permits travel to the claimed nation+era.
    /// </summary>
    public bool ClaimAllowed(NationSO claimNation, EraSO claimEra)
    {
        if (activeTravelRules == null)
            return true;

        foreach (TravelRuleSO rule in activeTravelRules)
            if (rule != null && !rule.Allows(claimNation, claimEra))
                return false;

        return true;
    }

    /// <summary>
    /// Tries to get a forced blueprint for the given case slot (1-based).
    /// Returns true if a forced case exists for that slot.
    /// </summary>
    public bool TryGetForcedCase(int caseIndex1Based, out CaseBlueprintSO blueprint)
    {
        foreach (ForcedCaseSlot slot in forcedCases)
        {
            if (slot == null)
                continue;

            if (slot.caseIndex1Based == caseIndex1Based && slot.caseBlueprint != null)
            {
                blueprint = slot.caseBlueprint;
                return true;
            }
        }

        blueprint = null;
        return false;
    }

    /// <summary>
    /// Tries to get the premade forced into the given case slot (1-based).
    /// Returns true if the slot names a premade.
    /// </summary>
    public bool TryGetForcedPremade(int caseIndex1Based, out LegendarySO premade)
    {
        foreach (ForcedCaseSlot slot in forcedCases)
        {
            if (slot != null && slot.caseIndex1Based == caseIndex1Based && slot.legendary != null)
            {
                premade = slot.legendary;
                return true;
            }
        }

        premade = null;
        return false;
    }

    /// <summary>
    /// Resolves eventRules into a concrete schedule using a deterministic seed.
    /// Random placement happens here (once), so runtime lookups are fast and stable.
    /// </summary>
    public ResolvedDaySchedule ResolveSchedule(int seed)
    {
        var schedule = new ResolvedDaySchedule();

        int total = Mathf.Max(1, visitorsCount);
        var rng = new System.Random(seed);

        // Tracks reserved slots for exclusive events to reduce collisions.
        var reservedExclusive = new HashSet<(DayEventTrigger trigger, int caseIndex1Based)>();

        // Picks an index inside an inclusive range and clamps safely.
        int PickIndexInclusive(int minInclusive, int maxInclusive)
        {
            minInclusive = Mathf.Clamp(minInclusive, 1, total);
            maxInclusive = Mathf.Clamp(maxInclusive, 1, total);

            if (minInclusive > maxInclusive)
                minInclusive = maxInclusive;

            return rng.Next(minInclusive, maxInclusive + 1);
        }

        if (eventRules == null || eventRules.Count == 0)
            return schedule;

        foreach (DayEventRule rule in eventRules)
        {
            if (rule == null || rule.eventAsset == null)
                continue;

            int slotIndex = rule.placement switch
            {
                EventPlacement.FixedCaseIndex => Mathf.Clamp(rule.fixedCaseIndex1Based, 1, total),
                EventPlacement.RandomAny => PickIndexInclusive(1, total),

                // Your rule: "random but after 2nd case" -> minCasesBefore = 2
                EventPlacement.RandomAfterMinCases => PickIndexInclusive(rule.minCasesBefore + 1, total),
                _ => 1
            };

            if (rule.exclusiveSlot)
            {
                // If this is a fixed placement and the slot is already reserved by another exclusive rule,
                // keep the slot (designer intent) but emit a warning.
                if (rule.placement == EventPlacement.FixedCaseIndex)
                {
                    var fixedKey = (rule.trigger, slotIndex);
                    if (reservedExclusive.Contains(fixedKey))
                        Debug.LogWarning($"DayPlanSO has multiple exclusive events targeting the same fixed slot (Trigger={rule.trigger}, Case={slotIndex}). Both will run in insertion order.");
                }

                // Fixed events cannot move; random ones can retry.
                if (rule.placement != EventPlacement.FixedCaseIndex)
                {
                    const int maxRetries = 16;

                    for (int attempt = 0; attempt < maxRetries; attempt++)
                    {
                        var key = (rule.trigger, slotIndex);
                        if (!reservedExclusive.Contains(key))
                            break;

                        slotIndex = rule.placement switch
                        {
                            EventPlacement.RandomAny => PickIndexInclusive(1, total),
                            EventPlacement.RandomAfterMinCases => PickIndexInclusive(rule.minCasesBefore + 1, total),
                            _ => slotIndex
                        };
                    }
                }

                reservedExclusive.Add((rule.trigger, slotIndex));
            }

            schedule.Add(rule.trigger, slotIndex, rule.eventAsset);
        }

        return schedule;
    }
}

/// <summary>
/// Forces a blueprint, a premade or both into a case slot (1-based), written
/// by Generate World from world_source.json days[].forced. Example: "case 3
/// of day 1 is Senenmut".
/// </summary>
[Serializable]
public sealed class ForcedCaseSlot
{
    /// <summary>1-based case slot index.</summary>
    [Min(1)] public int caseIndex1Based = 1;

    /// <summary>The case blueprint that must appear in this slot (null = the day's pick).</summary>
    public CaseBlueprintSO caseBlueprint;

    /// <summary>A premade who stands in this slot (null = none); the slot is never a rule violator's.</summary>
    public LegendarySO legendary;
}

/// <summary>
/// One kind's share of a day's travellers (traveller types K1, days[].kinds):
/// the kind's blueprint and its weight in the day's one blueprint draw.
/// </summary>
[Serializable]
public sealed class KindWeight
{
    /// <summary>The kind's blueprint (CaseBlueprintSO.Kind names the kind).</summary>
    public CaseBlueprintSO blueprint;

    /// <summary>Relative weight among the day's kinds (higher = more likely; 0 = never).</summary>
    [Min(0f)] public float weight = 1f;
}

/// <summary>
/// Weighted era entry used by DayPlanSO to pick the claimed (home) era.
/// </summary>
[Serializable]
public struct EraWeight
{
    /// <summary>The era represented by this weight entry.</summary>
    public EraSO era;

    /// <summary>Relative selection weight (higher = more likely).</summary>
    [Min(0f)] public float weight;
}

/// <summary>
/// When to run an event relative to a case.
/// </summary>
public enum DayEventTrigger
{
    /// <summary>Runs before the case begins.</summary>
    BeforeCase,

    /// <summary>Runs after the case resolves.</summary>
    AfterCase
}

/// <summary>
/// How an event chooses its case slot.
/// </summary>
public enum EventPlacement
{
    /// <summary>Fixed index (e.g., before case #3).</summary>
    FixedCaseIndex,

    /// <summary>Random index in [1..VisitorsCount] (slots past closing time are never reached).</summary>
    RandomAny,

    /// <summary>Random index in [minCasesBefore+1..VisitorsCount] (slots past closing time are never reached).</summary>
    RandomAfterMinCases
}

/// <summary>
/// Designer-authored rule that turns into a concrete scheduled event at runtime.
/// </summary>
[Serializable]
public sealed class DayEventRule
{
    /// <summary>When to run relative to the case.</summary>
    public DayEventTrigger trigger = DayEventTrigger.BeforeCase;

    /// <summary>The event asset to execute.</summary>
    public DayEventSO eventAsset;

    /// <summary>Placement strategy for this event.</summary>
    public EventPlacement placement = EventPlacement.FixedCaseIndex;

    /// <summary>Used when placement is FixedCaseIndex.</summary>
    [Min(1)] public int fixedCaseIndex1Based = 1;

    /// <summary>Used when placement is RandomAfterMinCases.</summary>
    [Min(0)] public int minCasesBefore = 0;

    /// <summary>If true, tries to avoid sharing the same slot with other exclusive rules.</summary>
    public bool exclusiveSlot = true;
}
