// ReSharper disable InconsistentNaming
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Designer-authored plan for a single day.
/// Owns:
/// - Day identity (dayNumber)
/// - How many travellers queue that day (visitorsCount; the shift clock may close first)
/// - What the day brings for the first time (bulletin, the briefing's first line) and the papers in circulation (papers; lessons 4 and D7, DayPacing, DayPapers)
/// - Procedural generation knobs (the kinds' blueprints and weights, eras, the premade pool and chance)
/// - Which lies today's liars may tell (lie kinds), and where a place lie may leak tells (tell count and tell channels)
/// - Forced slots (a blueprint, a premade or both: "3rd case on day 1 is Senenmut"; a slot may list alternatives, the first standing wins)
/// - Event rules (fixed or random placement, including "random but after N cases")
///
/// The portals seam (Saleh's portals feature, designed separately and not
/// built here; days 7-15 spec section 15): a day would list the portals it
/// opens beside its travel rules (a days[].portals list read here next to
/// activeTravelRules), and ContentLibrarySO.BuildToday(plan, history), the
/// one place a day's destinations are decided, would narrow them to the open
/// portals' places. Which portals are open on which day is the portal
/// design's to say (portal 01 from day 1, the others repaired from the PC);
/// no day plan assumes an opening.
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

    /// <summary>
    /// The day's bulletin (Papers Please lesson 4): one line naming the check
    /// or paper the day brings, the first line of the morning briefing; blank
    /// on a day that brings nothing new. Written by Generate World from
    /// world_source.json days[].bulletin (DayPacing checks that every day
    /// bringing something names it).
    /// </summary>
    [SerializeField, TextArea] private string bulletin = string.Empty;

    /// <summary>
    /// The papers in circulation today (lesson D7, "documents arrive one day
    /// at a time"): the form numbers a traveller may carry, the papers menu
    /// offers and a paper set may ask for (DayPapers); empty issues every
    /// form. Written by Generate World from world_source.json days[].papers.
    /// </summary>
    [SerializeField] private string[] papers;

    // -----------------------------
    // Procedural generation
    // -----------------------------

    /// <summary>The day's traveller mix (traveller types K1): each kind's blueprint, weight and whether its travellers are honest (K5), one weighted draw on the case stream (written by Generate World from days[].kinds).</summary>
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

    /// <summary>
    /// Chance per 2150 citizen of a costume error (traveller types C2 and P4;
    /// CostumeErrors.Plan): 0 before the dress rule's first day. Written by
    /// Tools > TimeDesk > Generate World from world_source.json days[].costumeErrorChance.
    /// </summary>
    [SerializeField, Range(0f, 1f)] private float costumeErrorChance;

    /// <summary>
    /// Chance per generated liar of a slip after their small talk (the
    /// personalities spec's T9; Slips.Roll on Seeds.ForSlip). Written by
    /// Tools > TimeDesk > Generate World from world_source.json days[].slipChance.
    /// </summary>
    [SerializeField, Range(0f, 1f)] private float slipChance;

    /// <summary>
    /// The most denied travellers who may come back today (wave 5, lesson 9;
    /// Returns.Due; 0: none). Written by Tools > TimeDesk > Generate World
    /// from world_source.json days[].returns.
    /// </summary>
    [SerializeField, Min(0)] private int returnsMax;

    /// <summary>
    /// The lies enabled today (traveller types §6.1; a traveller draws among
    /// those that fit their kind, LieKinds.For, on the lie roll). Written by
    /// Tools > TimeDesk > Generate World from world_source.json days[].lies.
    /// </summary>
    [SerializeField] private LieKind[] lieKinds;

    /// <summary>
    /// Chance per honest traveller of breaking one of today's rolled
    /// procedures (the paper set, the debt standing; traveller types P4;
    /// Directives.Roll on the fault stream): 0 before their first day.
    /// Written by Generate World from world_source.json days[].violationChance.
    /// </summary>
    [SerializeField, Range(0f, 1f)] private float violationChance;

    // -----------------------------
    // Scripted overrides
    // -----------------------------

    /// <summary>Travel restrictions active this day (announced in the briefing).</summary>
    [SerializeField] private TravelRuleSO[] activeTravelRules;

    /// <summary>
    /// Each guaranteeing rule (Directives.Guarantees: every active closure;
    /// a procedure on its first day) sends at least one faulty traveller,
    /// placed in the first half of the queue, so the day's directives are
    /// always tested.
    /// </summary>
    [SerializeField] private bool guaranteeRuleViolators = true;

    /// <summary>Forced slots (1-based): a blueprint, a premade or both, and a slot's alternatives in the order they are tried (written by Generate World from days[].forced).</summary>
    [SerializeField] private List<ForcedCaseSlot> forcedCases = new();

    /// <summary>Event rules (fixed or random placement).</summary>
    [SerializeField] private List<DayEventRule> eventRules = new();

    /// <summary>The Directorate's route for each departure portal today (the portals spec v3 RT2; written by Generate World from days[].portals, the place by reference so a renamed place cannot dangle).</summary>
    [SerializeField] private DirectorateRoute[] directorateRoutes;

    /// <summary>Public read-only day number.</summary>
    public int DayNumber => dayNumber;

    /// <summary>Public read-only number of visitors/cases.</summary>
    public int VisitorsCount => visitorsCount;

    /// <summary>The day's bulletin line (blank: nothing new today).</summary>
    public string Bulletin => bulletin ?? string.Empty;

    /// <summary>The form numbers issued today (empty: every form).</summary>
    public IReadOnlyList<string> Papers => papers ?? Array.Empty<string>();

    /// <summary>True when the form numbered <paramref name="formNumber"/> is issued today (DayPapers.Issued).</summary>
    public bool Issues(string formNumber) => DayPapers.Issued(Papers, formNumber);

    /// <summary>The templates of <paramref name="blueprint"/> issued today, in paper order (null templates skipped; none for a null blueprint): what a traveller of its kind may carry today.</summary>
    public IEnumerable<DocumentTemplateSO> TemplatesOf(CaseBlueprintSO blueprint)
    {
        foreach (DocumentTemplateSO t in blueprint != null && blueprint.DocumentTemplates != null ? blueprint.DocumentTemplates : Array.Empty<DocumentTemplateSO>())
            if (t != null && Issues(t.formNumber))
                yield return t;
    }

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

    /// <summary>Chance per 2150 citizen of a costume error today.</summary>
    public float CostumeErrorChance => costumeErrorChance;

    /// <summary>Chance per generated liar of a slip today (T9).</summary>
    public float SlipChance => slipChance;

    /// <summary>The most denied travellers who may come back today (wave 5, lesson 9).</summary>
    public int ReturnsMax => returnsMax;

    /// <summary>The lies enabled today, in authored order (empty when unset).</summary>
    public IReadOnlyList<LieKind> EnabledLies => lieKinds ?? Array.Empty<LieKind>();

    /// <summary>Chance per honest traveller of breaking one of today's rolled procedures.</summary>
    public float ViolationChance => violationChance;

    /// <summary>Public read-only travel rules active this day.</summary>
    public IReadOnlyList<TravelRuleSO> ActiveTravelRules => activeTravelRules ?? System.Array.Empty<TravelRuleSO>();

    /// <summary>Whether each guaranteeing rule is guaranteed a faulty traveller in the first half of the queue.</summary>
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
    /// Returns true if every active rule read for a traveller of
    /// <paramref name="kind"/> permits travel to the claimed nation+era (a
    /// closure listing kinds closes only for them: the Economy range limit,
    /// days 7-15 §6.1).
    /// </summary>
    public bool ClaimAllowed(NationSO claimNation, EraSO claimEra, TravellerKind kind)
    {
        if (activeTravelRules == null)
            return true;

        foreach (TravelRuleSO rule in activeTravelRules)
            if (rule != null && rule.AppliesTo(kind) && !rule.Allows(claimNation, claimEra))
                return false;

        return true;
    }

    /// <summary>The Directorate's routes as authored (the validator checks each names a place).</summary>
    public IReadOnlyList<DirectorateRoute> DirectorateRoutes => directorateRoutes ?? Array.Empty<DirectorateRoute>();

    /// <summary>The Directorate's routes as the schedule's requests (PortalSchedule.Resolve), in authored order; a route whose place is missing is left out.</summary>
    public List<PortalRequest> PortalRequests()
    {
        var requests = new List<PortalRequest>();
        foreach (DirectorateRoute route in directorateRoutes ?? Array.Empty<DirectorateRoute>())
            if (route != null && route.place != null && route.place.nation != null && route.place.era != null)
                requests.Add(new PortalRequest(route.portal, new PlaceRef(route.place.nation.id, route.place.era.id)));
        return requests;
    }

    /// <summary>
    /// The line of today's first closure that forbids <paramref name="nation"/>
    /// in <paramref name="era"/> for every traveller (a portal's route it
    /// forbids shows CLOSED; the portals spec v3 RT2), or null when none does.
    /// A closure listing kinds (the Economy range limit) closes no portal: it
    /// only turns some travellers away.
    /// </summary>
    public string ClosureOf(NationSO nation, EraSO era)
    {
        foreach (TravelRuleSO rule in ActiveTravelRules)
            if (rule != null && rule.IsClosure && (rule.kinds == null || rule.kinds.Length == 0) && !rule.Allows(nation, era))
                return rule.Summary();
        return null;
    }

    /// <summary>
    /// The forced entries of a case slot (1-based), in the authored order: a
    /// slot's alternatives (days 7-15 B9), tried in this order at the day's
    /// start (Premades.Appearance). Empty when nothing is forced there.
    /// </summary>
    public IEnumerable<ForcedCaseSlot> ForcedAt(int caseIndex1Based)
    {
        foreach (ForcedCaseSlot slot in forcedCases)
            if (slot != null && slot.caseIndex1Based == caseIndex1Based)
                yield return slot;
    }

    /// <summary>
    /// Resolves eventRules into a concrete schedule using a deterministic seed:
    /// the day's event stream (Seeds.ForEvents of <paramref name="seed"/>, the
    /// day's seed; audit R3-010), apart from every traveller's. Random
    /// placement happens here (once), so runtime lookups are fast and stable.
    /// </summary>
    public ResolvedDaySchedule ResolveSchedule(int seed)
    {
        var schedule = new ResolvedDaySchedule();

        int total = Mathf.Max(1, visitorsCount);
        var rng = new SeededRandom(Seeds.ForEvents(seed));

        // Tracks reserved slots for exclusive events to reduce collisions.
        var reservedExclusive = new HashSet<(DayEventTrigger trigger, int caseIndex1Based)>();

        // Picks an index inside an inclusive range and clamps safely.
        int PickIndexInclusive(int minInclusive, int maxInclusive)
        {
            minInclusive = Mathf.Clamp(minInclusive, 1, total);
            maxInclusive = Mathf.Clamp(maxInclusive, 1, total);

            if (minInclusive > maxInclusive)
                minInclusive = maxInclusive;

            return rng.Range(minInclusive, maxInclusive + 1);
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
/// of day 1 is Senenmut". One entry is one appearance (days 7-15 B9): it
/// stands when its conditions pass at the day's start and its premade is not
/// met; a slot may list several, the first standing wins.
/// </summary>
[Serializable]
public sealed class ForcedCaseSlot
{
    /// <summary>1-based case slot index.</summary>
    [Min(1)] public int caseIndex1Based = 1;

    /// <summary>The appearance's name (days[].forced[].id; blank: none), unique in the day: a slot's alternatives are told apart by it in the logs and the content sheet.</summary>
    public string id = string.Empty;

    /// <summary>The case blueprint that must appear in this slot (null = the day's pick).</summary>
    public CaseBlueprintSO caseBlueprint;

    /// <summary>A premade who stands in this slot (null = none); the slot is never a rule violator's.</summary>
    public LegendarySO legendary;

    /// <summary>True when the appearance tells an authored lie (<see cref="lie"/>; days 7-15 B6): the slot's authoring, never a roll.</summary>
    public bool hasLie;

    /// <summary>The appearance's authored lie (read only when <see cref="hasLie"/>): its variant and false values drawn on the traveller's lie stream, as a rolled lie's are.</summary>
    public LieKind lie;

    /// <summary>The appearance's authored directive fault (None: none; days 7-15 B6): the maker's variant pinned (Directives.Plan), its values drawn on the traveller's fault stream.</summary>
    public PlannedDirective directive;

    /// <summary>The appearance's dialog (blank: the premade's own; days 7-15 B7), offered only while it stands at the desk (Premades.Voice).</summary>
    public string dialogId = string.Empty;

    /// <summary>The desk's opener for the appearance (blank: the premade's own, else the interview's; days 7-15 B7).</summary>
    public string introLine = string.Empty;

    /// <summary>When this appearance stands (all must pass on the day-start snapshot; none: always): the verdict memory's flags, dialog flags, the day (days 7-15 B9).</summary>
    public List<TriggerCondition> conditions = new();
}

/// <summary>
/// One kind's share of a day's travellers (traveller types K1, days[].kinds):
/// the kind's blueprint and its weight in the day's one blueprint draw, and
/// whether its travellers are honest (K5).
/// </summary>
[Serializable]
public sealed class KindWeight
{
    /// <summary>The kind's blueprint (CaseBlueprintSO.Kind names the kind).</summary>
    public CaseBlueprintSO blueprint;

    /// <summary>Relative weight among the day's kinds (higher = more likely; 0 = never).</summary>
    [Min(0f)] public float weight = 1f;

    /// <summary>True when a traveller drawn from this entry is honest: every fault roll is skipped with no draw (traveller types K5; FaultOrder; day 1's poor tourists). Written by Generate World from days[].kinds[].honest.</summary>
    public bool honest;
}

/// <summary>The Directorate's route for one departure portal on a day (days[].portals[]).</summary>
[Serializable]
public sealed class DirectorateRoute
{
    /// <summary>The portal's number (agency.portals[].number).</summary>
    public int portal;

    /// <summary>The place it runs to today.</summary>
    public NationEraProfileSO place;
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
