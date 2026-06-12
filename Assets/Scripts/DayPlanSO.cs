// ReSharper disable InconsistentNaming
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Designer-authored plan for a single day.
/// Owns:
/// - Day identity (dayNumber)
/// - How many cases happen that day (visitorsCount)
/// - Procedural generation knobs (blueprints, eras, legendary chance)
/// - Forced cases (e.g., "3rd case on day 2 is X")
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

    /// <summary>Total number of cases/visitors for this day.</summary>
    [SerializeField, Min(1)] private int visitorsCount = 6;

    // -----------------------------
    // Procedural generation
    // -----------------------------

    /// <summary>Blueprints available for procedural cases.</summary>
    [SerializeField] private CaseBlueprintSO[] possibleBlueprints;

    /// <summary>Weighted set of eras to pick the TRUE era from (optional).</summary>
    [SerializeField] private EraWeight[] eraWeights;

    /// <summary>Base chance per case to become legendary (0..1).</summary>
    [SerializeField, Range(0f, 1f)] private float legendaryBaseChance = 0.05f;

    /// <summary>Legendary candidates available this day (filtered by min/max day).</summary>
    [SerializeField] private LegendarySO[] availableLegendaries;

    // -----------------------------
    // Scripted overrides
    // -----------------------------

    /// <summary>Forced case blueprints by slot index (1-based).</summary>
    [SerializeField] private List<ForcedCaseSlot> forcedCases = new();

    /// <summary>Event rules (fixed or random placement).</summary>
    [SerializeField] private List<DayEventRule> eventRules = new();

    /// <summary>Public read-only day number.</summary>
    public int DayNumber => dayNumber;

    /// <summary>Public read-only number of visitors/cases.</summary>
    public int VisitorsCount => visitorsCount;

    /// <summary>Public read-only blueprint pool.</summary>
    public IReadOnlyList<CaseBlueprintSO> PossibleBlueprints => possibleBlueprints;

    /// <summary>Public read-only era weights.</summary>
    public IReadOnlyList<EraWeight> EraWeights => eraWeights;

    /// <summary>Public read-only legendary chance.</summary>
    public float LegendaryBaseChance => legendaryBaseChance;

    /// <summary>Public read-only legendaries list.</summary>
    public IReadOnlyList<LegendarySO> AvailableLegendaries => availableLegendaries;

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
/// Forces a specific blueprint into a specific case slot (1-based).
/// Example: "case 3 is tutorial blueprint".
/// </summary>
[Serializable]
public sealed class ForcedCaseSlot
{
    /// <summary>1-based case slot index.</summary>
    [Min(1)] public int caseIndex1Based = 1;

    /// <summary>The case blueprint that must appear in this slot.</summary>
    public CaseBlueprintSO caseBlueprint;
}

/// <summary>
/// Weighted era entry used by DayPlanSO to pick the TRUE era.
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

    /// <summary>Random index in [1..VisitorsCount].</summary>
    RandomAny,

    /// <summary>Random index in [minCasesBefore+1..VisitorsCount].</summary>
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
