using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Builds runtime CaseInstance objects from your data:
/// DayPlanSO -> picks the place the traveller CLAIMS as home (era by weight,
/// nation among today's places) -> the registered identity every traveller
/// carries (names and birth years of the claimed place) -> documents whose
/// fields come from today's FactTable for the claim -> maybe a lie: a liar
/// really comes from another of today's places and leaks tells carrying that
/// true home's values, on the papers, in their answers or in their dress
/// (Lies) -> the traveller's answers to today's questions, the desk's opener
/// and the claim sentence (the content library's interview wording), a
/// small-talk line, and how they look (Looks: layers from the claimed place's
/// wardrobe; a dress tell is one garment of the true home). Premades (named,
/// drawn whole) stand in forced slots or roll from the day's pool. Every draw
/// comes from seeded streams (per traveller: the case, lie, dialog, look and
/// premade streams; plus the day's rule-violator stream), so
/// the same run, day and met premades always produce the same travellers. A
/// displaced person's agency file (their Displacement No., incident, found
/// date and certificate's Valid Until, AgencyNumbers) is drawn on their
/// account stream (Seeds.ForAccount), counting from today's date on the
/// agency calendar, and their forms and registry entry print it. The day's
/// kinds (DayPlanSO kinds) decide the blueprint; a 2150 citizen (a tourist)
/// comes from the present: a name from the Future places' lists together and
/// a birth date in the present's years (the same case-stream draws), English
/// speech, the destination's dress over their family country's looks, and a
/// Citizen Account (AccountMaker) on their account stream, which their forms
/// and their record print. Who lies is one roll on the lie stream
/// (Lies.Roll, among the day's lies that fit the kind, LieKinds.For; K5,
/// FaultOrder: never a premade authored honest, a closure's violator, a
/// traveller drawn from an honest kind entry, or one
/// without papers): the displaced's false origin is the place lie above; a
/// smuggler (LieKind.Smuggling, from day 4) claims honestly but carries the
/// present's currency or technology, planned by the same Lies.Plan with the
/// present (TodaysWorld.Present) as the only candidate, so the manifest, the
/// declaration or an answer gives 2150's value; a
/// citizen's record lie (RecordLies: poor posing as rich, a doctored
/// identity, a debtor posing as a tourist, a forged contract) forges fields
/// of their papers that their own account and, where two papers disagree,
/// each other disprove. The Directives (Directives.Fault over the finished
/// papers and account, no evidence needed) give the directive fault: a
/// closed destination, an incomplete paper set, a frozen account, a
/// departure dated another day or an expired paper; each closure active
/// today and each procedure with a maker on its first day plans one faulty
/// traveller in the first half of the queue (PlanViolators: a closure's
/// place, the return home's or no 2150 goods' planned lie, or the rule a
/// procedure's slot breaks, PlanViolation), and on later days an honest
/// traveller may break a rolled procedure (Directives.Roll on their fault
/// stream, Seeds.ForFaults, before the costume roll; the makers of §5.4: an
/// Economy unit on the account, a form left out or unsigned, a Frozen
/// standing, a falsified date). A 2150 citizen with no other fault may wear
/// a costume error (CostumeErrors, on the same fault stream): another
/// place's item, the present's clothes or a 2150 accessory, dressed by
/// Looks.Compose like a dress tell.
/// </summary>
public sealed class CaseFactory
{
    /// <summary>Content library used as the source of eras, places, legendaries, etc.</summary>
    private readonly ContentLibrarySO _lib;

    /// <summary>Today's facts (the same snapshot the reference books show).</summary>
    private readonly FactTable _facts;

    /// <summary>Today's visitor names (unique per generated day; see NameRoster).</summary>
    private NameRoster _roster = new NameRoster();

    /// <summary>Today's places, the destinations (eras x allowed nations, never the Future), in book order (TodaysWorld).</summary>
    private readonly List<NationEraProfileSO> _todays;

    /// <summary>The present (TodaysWorld.Present): a 2150 citizen's birth years and the year their age is counted from; null when the content has none.</summary>
    private readonly PresentPlace _present;

    /// <summary>The names 2150 citizens are drawn from (the Future places' lists together, ContentLibrarySO.CitizenNames).</summary>
    private readonly CitizenNames _citizenNames;

    /// <summary>Every past place's label, in library order: where a citizen's past trips went (AccountRequest.TripPlaces).</summary>
    private readonly List<string> _pastPlaces;

    /// <summary>The current traveller's random stream (reset per case).</summary>
    private IRandomSource _rng = new SeededRandom(0);

    /// <summary>The current traveller's lie stream (Seeds.ForLies), apart from <see cref="_rng"/> so lie tuning never changes who travellers are.</summary>
    private IRandomSource _lieRng = new SeededRandom(0);

    /// <summary>The current traveller's dialog seed (Seeds.ForDialog): a value every line pick reads (Voices.Pick), never a stream.</summary>
    private int _dialogSeed;

    /// <summary>The current traveller's personality stream (Seeds.ForPersonality): one draw for a generated traveller, which reads nothing else.</summary>
    private IRandomSource _personalityRng = new SeededRandom(0);

    /// <summary>The current traveller's slip stream (Seeds.ForSlip): one roll for a generated liar, which reads nothing but the chance.</summary>
    private IRandomSource _slipRng = new SeededRandom(0);

    /// <summary>The current traveller's waiver-signing stream (Seeds.ForWaiverSign): their one answer to the desk's pad, which reads nothing but the chance.</summary>
    private IRandomSource _waiverSignRng = new SeededRandom(0);

    /// <summary>The current traveller's look stream (Seeds.ForLooks): gender when unknown, skin, face, hair colour.</summary>
    private IRandomSource _looksRng = new SeededRandom(0);

    /// <summary>The current slot's premade stream (Seeds.ForLegendary): the premade roll and pick.</summary>
    private IRandomSource _legendaryRng = new SeededRandom(0);

    /// <summary>The current traveller's account stream (Seeds.ForAccount): their agency numbers and dates, apart from every other stream.</summary>
    private IRandomSource _accountRng = new SeededRandom(0);

    /// <summary>The current traveller's fault stream (Seeds.ForFaults): the violation roll, its rule and variant (a planned procedure's maker draws here too), then the costume roll, its variant and its source.</summary>
    private IRandomSource _faultRng = new SeededRandom(0);

    /// <summary>The transponder models a unit may be drawn from today (Directives.Unrecalled: the agency's, minus every model a recall standing today grounds; days 7-15 §6).</summary>
    private List<TransponderModel> _transponders = new List<TransponderModel>();

    /// <summary>Whether a garment can be looked at and compared today (a costume error, like a dress tell, needs it).</summary>
    private bool _appearanceReachable;

    /// <summary>Today's date on the agency calendar (AgencyCalendar.TryToday for the world's day); null when the agency block's first date is unreadable.</summary>
    private System.DateTime? _today;

    /// <summary>Today's day number (GenerateDayCases): which document fields are introduced (ShowsField).</summary>
    private int _day;

    /// <summary>The agency numbers handed out today (a number belongs to one traveller a day, AgencyNumbers.TakeUnique).</summary>
    private HashSet<string> _agencyNumbers = new HashSet<string>();

    /// <summary>The current traveller's forms seed (Seeds.ForForms): a value their papers' serials come from, never a stream.</summary>
    private int _formsSeed;

    /// <summary>Today's tell channels: the plan's, minus Appearance where no garment can be looked at.</summary>
    private IReadOnlyList<TellChannel> _channels = System.Array.Empty<TellChannel>();

    /// <summary>Today's interview (the same questions for every traveller); null where nothing spoken can be read.</summary>
    private InterviewDay _interview;

    /// <summary>Today's askable question categories; every traveller answers each (InterviewDay.AskableCategories).</summary>
    private IReadOnlyList<ClueCategory> _askable = System.Array.Empty<ClueCategory>();

    /// <summary>Today's question categories that may carry an Answer tell (the day-gated questions; InterviewDay.AnswerTellCategories).</summary>
    private IReadOnlyList<ClueCategory> _answerTellCategories = System.Array.Empty<ClueCategory>();

    /// <summary>Categories with a reference book (only these can carry a place-fact tell).</summary>
    private readonly HashSet<ClueCategory> _bookCategories;

    /// <summary>The forced appearance standing in each slot today (days 7-15 B9: a slot's entries tried in order at the day's start, the first standing wins), by 1-based slot.</summary>
    private Dictionary<int, ForcedCaseSlot> _appearances = new Dictionary<int, ForcedCaseSlot>();

    /// <summary>Guaranteed rule violators for the day being generated (a closure's: the place they are bound for), by 1-based slot.</summary>
    private Dictionary<int, NationEraProfileSO> _violators = new Dictionary<int, NationEraProfileSO>();

    /// <summary>The closure each guaranteed violator breaks, by 1-based slot: its kinds are the only ones the slot draws (a closure listing kinds, the Economy range limit).</summary>
    private Dictionary<int, TravelRuleSO> _violatorRules = new Dictionary<int, TravelRuleSO>();

    /// <summary>Guaranteed procedure breakers for the day being generated (the paper set, the debt standing, the papers' dates or dress on its first day: the rule they break, PlanViolation), by 1-based slot.</summary>
    private Dictionary<int, TravelRuleSO> _plannedRules = new Dictionary<int, TravelRuleSO>();

    /// <summary>Guaranteed liars for the day being generated (the displaced's return home on its first day: the place lie they tell), by 1-based slot.</summary>
    private Dictionary<int, LieKind> _plannedLiars = new Dictionary<int, LieKind>();

    /// <summary>The denied travellers who come back today (wave 5, lesson 9; PlanReturns), by 1-based slot.</summary>
    private Dictionary<int, ReturningTraveller> _returning = new Dictionary<int, ReturningTraveller>();

    /// <summary>The current slot's case seed (Seeds.ForCase), kept on the traveller for their return.</summary>
    private int _caseSeed;

    /// <summary>
    /// Construct a factory over a content library and today's world: its
    /// places and their facts, history applied (ContentLibrarySO.BuildToday
    /// for the same day plan).
    /// </summary>
    public CaseFactory(ContentLibrarySO lib, TodaysWorld today)
    {
        _lib = lib;
        _facts = today?.Facts ?? new FactTable();
        _todays = today != null ? new List<NationEraProfileSO>(today.Places) : new List<NationEraProfileSO>();
        _present = today?.Present;
        _bookCategories = lib != null ? lib.ReferenceBookCategories() : new HashSet<ClueCategory>();
        _citizenNames = lib != null ? lib.CitizenNames() : new CitizenNames(null);
        _pastPlaces = lib != null
            ? lib.Profiles.Where(p => p != null && p.era != null && !p.era.isFuture).Select(p => p.OriginLabel).ToList()
            : new List<string>();
    }

    /// <summary>
    /// Generates the full list of cases for a day, based on the DayPlan.
    /// Each forced slot's appearance is picked once at the day's start
    /// (Appearances: its entries tried in order, the first whose conditions
    /// pass on the world as it stands and whose premade is not met wins); its
    /// blueprint overrides the day's pick and its premade stands. Each slot
    /// draws from its own streams (Seeds.ForCase and its salted streams), so
    /// one traveller's draws never shift the next one's. Every traveller
    /// answers each of today's askable questions
    /// (<paramref name="interview"/>, InterviewDay.AskableCategories: the same
    /// for everyone); only the day-gated ones (InterviewDay.AnswerTellCategories)
    /// may carry a spoken tell (a smuggler's only among Currency and Technology,
    /// Lies.SmuggledCategories); a null interview asks nothing (nothing spoken can
    /// be read). A dress tell needs <paramref name="appearanceReachable"/>
    /// (a garment can be looked at and compared).
    /// </summary>
    public List<CaseInstance> GenerateDayCases(DayPlanSO plan, WorldState state, int daySeed, InterviewDay interview, bool appearanceReachable)
    {
        Debug.Log($"[CaseFactory] >>> Entering GenerateDayCases (day {state?.day}, plan='{plan?.name}', daySeed={daySeed}).");

        var results = new List<CaseInstance>();

        if (plan == null || state == null || _lib == null)
        {
            Debug.LogWarning("[CaseFactory] <<< Exiting GenerateDayCases early — null plan/state/library.");
            return results;
        }

        int total = Mathf.Max(1, plan.VisitorsCount);
        _interview = interview;

        // The opener and the claim are content: one warning a day when Generate World has not written them.
        InterviewLines wording = _lib.Interview;
        if (wording == null || string.IsNullOrWhiteSpace(wording.opener?.text) || string.IsNullOrWhiteSpace(wording.openerLegendary?.text))
            Debug.LogWarning($"[CaseFactory] Day {plan.DayNumber}: the content library's interview opener or legendary opener is blank, so a transcript may start with the claim. Run Tools > TimeDesk > Generate World.");
        foreach (TravellerKind kind in plan.PossibleBlueprints.Concat(plan.ForcedBlueprints).Where(b => b != null).Select(b => b.Kind).Distinct())
            if (string.IsNullOrWhiteSpace(Interview.ClaimLine(wording, kind)?.text))
                Debug.LogWarning($"[CaseFactory] Day {plan.DayNumber}: the content library has no claim line for {kind} travellers, so they say the bare place label as their claim. Run Tools > TimeDesk > Generate World.");

        _channels = appearanceReachable ? plan.TellChannels : plan.TellChannels.Where(c => c != TellChannel.Appearance).ToList();
        _appearanceReachable = appearanceReachable;

        // Fresh roster: names are unique within the day (records use first
        // match). The forced premades who will stand today are reserved first,
        // which also keeps them out of the day's random roll.
        _roster = new NameRoster();
        _appearances = Appearances(plan, state);
        _transponders = Directives.Unrecalled(_lib.Agency.transponders,
            plan.ActiveTravelRules.Where(r => r != null && r.type == TravelRuleType.TransponderRecall && !string.IsNullOrWhiteSpace(r.transponder)).Select(r => r.transponder).ToList());
        _agencyNumbers = new HashSet<string>();
        string clerkId = _lib.Agency.clerk != null ? _lib.Agency.clerk.citizenId : null;
        if (!string.IsNullOrWhiteSpace(clerkId))
            _agencyNumbers.Add(clerkId.Trim()); // no traveller is ever given the clerk's own Citizen ID
        foreach (ForcedCaseSlot forced in _appearances.Values)
            if (forced.legendary != null && !string.IsNullOrWhiteSpace(forced.legendary.citizenId))
                _agencyNumbers.Add(forced.legendary.citizenId.Trim()); // a story character's own ID, reserved before slot 1 (days 7-15 B3)
        _day = state.day;
        _today = AgencyCalendar.TryToday(_lib.Agency.firstDate, state.day, out System.DateTime today) ? today : (System.DateTime?)null;
        if (_today == null)
            Debug.LogError($"[CaseFactory] Day {state.day}: the agency calendar cannot count from agency.firstDate '{_lib.Agency.firstDate}', so the displaced's numbers and dates print placeholders. Run Tools > TimeDesk > Generate World.");
        foreach (ForcedCaseSlot forced in _appearances.Values)
            if (forced.legendary != null && !_roster.Reserve(forced.legendary.displayName))
                Debug.LogError($"[CaseFactory] Day {plan.DayNumber}: premade '{forced.legendary.displayName}' stands in two forced slots today, or shares a name with another forced premade. Check world_source.json days[].forced.");

        if (_todays.Count == 0)
            Debug.LogWarning($"[CaseFactory] Day {plan.DayNumber} has no places (its eras x allowed nations match no profile). Run Tools > TimeDesk > Generate World.");

        _violators = PlanViolators(plan, total, daySeed, out _plannedLiars, out _plannedRules, out _violatorRules);
        _returning = PlanReturns(plan, state, total, daySeed);

        Debug.Log($"[CaseFactory] Generating {total} case(s) for day {state.day} from {_todays.Count} place(s); guaranteed violators in slot(s) [{string.Join(", ", _violators.Keys)}], guaranteed liars in slot(s) [{string.Join(", ", _plannedLiars.Select(l => $"{l.Key}:{l.Value}"))}], guaranteed breakers in slot(s) [{string.Join(", ", _plannedRules.Select(r => $"{r.Key}:{r.Value.name}"))}].");

        for (int i = 0; i < total; i++)
        {
            int caseIndex1Based = i + 1;
            int caseSeed = Seeds.ForCase(daySeed, caseIndex1Based);
            _caseSeed = caseSeed;
            _rng = new SeededRandom(caseSeed);
            _lieRng = new SeededRandom(Seeds.ForLies(caseSeed));
            _dialogSeed = Seeds.ForDialog(caseSeed);
            _personalityRng = new SeededRandom(Seeds.ForPersonality(caseSeed));
            _slipRng = new SeededRandom(Seeds.ForSlip(caseSeed));
            _waiverSignRng = new SeededRandom(Seeds.ForWaiverSign(caseSeed));
            _looksRng = new SeededRandom(Seeds.ForLooks(caseSeed));
            _legendaryRng = new SeededRandom(Seeds.ForLegendary(caseSeed));
            _accountRng = new SeededRandom(Seeds.ForAccount(caseSeed));
            _faultRng = new SeededRandom(Seeds.ForFaults(caseSeed));
            _formsSeed = Seeds.ForForms(caseSeed);
            results.Add(GenerateSingleCase(plan, state, i, caseIndex1Based));
        }

        Debug.Log($"[CaseFactory] <<< Exiting GenerateDayCases ({results.Count} case(s) generated for day {state.day}).");

        return results;
    }

    /// <summary>
    /// Places one faulty traveller per guaranteeing rule (Directives.Guarantees:
    /// each active closure; the displaced's return home, no 2150 goods and
    /// the papers' dates each on its first day) in the first half of the
    /// queue (DayPlanSO.GuaranteeRuleViolators), never in the slot of a forced
    /// appearance that stands today with a premade or an authored fault (a met
    /// premade's slot, or one whose appearance failed its conditions, is free),
    /// drawn from the day's own violator stream (the slots, then each
    /// rule's maker in plan order) so the travellers' streams are untouched.
    /// A closure's maker draws a place it forbids; the return home's draws the
    /// place lie (Lies.Roll among the day's place lies the displaced may
    /// carry, at a chance of 1) that the slot's displaced traveller then tells
    /// without a roll of their own (<paramref name="liars"/>); no 2150 goods
    /// plans a smuggler the same way (no draw); the papers' dates, the paper
    /// set, the debt standing and dress mark the slot with their rule
    /// (<paramref name="rules"/>), whose traveller is drawn among the kinds
    /// the rule reads and its maker can break, skips the lie roll, and gets
    /// the maker's fault on their own fault stream (PlanViolation, FalsifyDate,
    /// the costume roll planned). A closure that forbids none of today's
    /// places, a return home with no place lie enabled, a no-2150-goods rule
    /// with smuggling disabled or a procedure none of the day's kinds can
    /// break cannot be tested and is skipped with a warning.
    /// </summary>
    private Dictionary<int, NationEraProfileSO> PlanViolators(DayPlanSO plan, int total, int daySeed, out Dictionary<int, LieKind> liars, out Dictionary<int, TravelRuleSO> rules,
                                                             out Dictionary<int, TravelRuleSO> closures)
    {
        var violators = new Dictionary<int, NationEraProfileSO>();
        liars = new Dictionary<int, LieKind>();
        rules = new Dictionary<int, TravelRuleSO>();
        closures = new Dictionary<int, TravelRuleSO>();
        if (!plan.GuaranteeRuleViolators)
            return violators;

        List<LieKind> placeLies = LieKinds.For(plan.EnabledLies, TravellerKind.Displaced).Where(l => l != LieKind.Smuggling && LieKinds.IsPlaceLie(l)).ToList();

        // Each guaranteeing rule's breakers: the places a closure forbids, or null for a procedure (its maker plans a lie or marks the slot with the rule: planned).
        var breakersPerRule = new List<List<NationEraProfileSO>>();
        var planned = new List<TravelRuleSO>();
        foreach (TravelRuleSO rule in plan.ActiveTravelRules)
        {
            if (rule == null || !Directives.Guarantees(rule.type, plan.DayNumber, _lib.FirstDayOf(rule)))
                continue;

            if (rule.IsClosure)
            {
                List<NationEraProfileSO> breakers = _todays.Where(p => !rule.Allows(p.nation, p.era)).ToList();
                if (breakers.Count == 0)
                {
                    Debug.LogWarning($"[CaseFactory] Rule '{rule.name}' forbids none of day {plan.DayNumber}'s places, so no traveller can break it.");
                    continue;
                }
                if (!plan.Kinds.Any(k => k != null && k.blueprint != null && k.weight > 0f && rule.AppliesTo(k.blueprint.Kind)))
                {
                    Debug.LogWarning($"[CaseFactory] Rule '{rule.name}' closes for kinds day {plan.DayNumber} does not weight, so no traveller can break it. Check world_source.json days[].kinds and rules[].kinds.");
                    continue;
                }

                breakersPerRule.Add(breakers);
                planned.Add(rule);
            }
            else if (rule.type == TravelRuleType.ReturnHome && (placeLies.Count == 0 || !plan.Kinds.Any(k => k != null && k.blueprint != null && k.blueprint.Kind == TravellerKind.Displaced && k.weight > 0f)))
            {
                Debug.LogWarning($"[CaseFactory] Rule '{rule.name}' guarantees a liar on day {plan.DayNumber}, but the day enables no place lie for the displaced (or weights no displaced kind), so none can be planned. Check world_source.json days[].lies and days[].kinds.");
            }
            else if (rule.type == TravelRuleType.NoPresentGoods && !plan.EnabledLies.Contains(LieKind.Smuggling))
            {
                Debug.LogWarning($"[CaseFactory] Rule '{rule.name}' guarantees a smuggler on day {plan.DayNumber}, but the day does not enable Smuggling, so none can be planned. Check world_source.json days[].lies.");
            }
            else if (rule.type != TravelRuleType.ReturnHome && rule.type != TravelRuleType.NoPresentGoods && !plan.Kinds.Any(k => k != null && k.blueprint != null && k.weight > 0f && CanBreak(plan, rule, k.blueprint)))
            {
                Debug.LogWarning($"[CaseFactory] Rule '{rule.name}' ({rule.type}) is guaranteed a breaker on day {plan.DayNumber}, but none of the day's kinds can break it, so none can be planned. Check world_source.json days[].kinds and the kinds' templates.");
            }
            else
            {
                breakersPerRule.Add(null);
                planned.Add(rule);
            }
        }

        if (breakersPerRule.Count == 0)
            return violators;

        var standingSlots = new HashSet<int>(_appearances.Where(a => a.Value.legendary != null || HasAuthoredFault(a.Value)).Select(a => a.Key));
        var rng = new SeededRandom(Seeds.ForViolators(daySeed));
        int[] slots = ViolatorSlots.Pick(total, breakersPerRule.Count, rng, standingSlots);
        for (int i = 0; i < slots.Length; i++)
        {
            List<NationEraProfileSO> breakers = breakersPerRule[i];
            if (breakers != null)
            {
                violators[slots[i]] = breakers[rng.Range(0, breakers.Count)];
                closures[slots[i]] = planned[i];
            }
            else if (planned[i].type == TravelRuleType.ReturnHome)
                liars[slots[i]] = Lies.Roll(1f, placeLies, rng).Value;
            else if (planned[i].type == TravelRuleType.NoPresentGoods)
                liars[slots[i]] = LieKind.Smuggling;
            else
                rules[slots[i]] = planned[i];
        }

        if (slots.Length < breakersPerRule.Count)
            Debug.LogWarning($"[CaseFactory] Day {plan.DayNumber}: the first half of the queue ({ViolatorSlots.Window(total)} slots, the forced premades' excluded) holds {slots.Length} of the {breakersPerRule.Count} planned faulty travellers; the rest are dropped.");

        return violators;
    }

    /// <summary>
    /// Generates one case:
    /// - The slot's standing forced appearance (Appearances): its blueprint, if it names one
    /// - A premade: the appearance's (a slot a premade is forced into with no standing appearance holds an ordinary traveller), else maybe one from the pool (never on a violator's slot)
    /// - A planned faulty traveller's place (a closure's) or rule (a procedure's) for this slot
    /// - Otherwise pick the claimed era from day weights and a place in it
    /// - If blueprint not forced, pick from the day's kinds (a planned procedure's among the kinds it reads)
    /// - Build documents, fill their fields from the claim, maybe disguise a liar or break a directive, then compose the look
    /// - Read the finished papers and account against today's Directives
    /// </summary>
    private CaseInstance GenerateSingleCase(DayPlanSO plan, WorldState state, int index0Based, int caseIndex1Based)
    {
        // 1) The slot's standing forced appearance (its blueprint, if it names one).
        _appearances.TryGetValue(caseIndex1Based, out ForcedCaseSlot appearance);
        CaseBlueprintSO forcedBlueprint = appearance != null ? appearance.caseBlueprint : null;

        // 1.5) A denied traveller who comes back (wave 5, lesson 9): the same person (name, kind, role, claim, birth date,
        //      personality, Citizen ID) with their first visit's look and account streams, so the same face comes back;
        //      corrected papers are an honest entry (no roll), a new story rolls everything again.
        _returning.TryGetValue(caseIndex1Based, out ReturningTraveller back);
        NationEraProfileSO backPlace = back != null ? PlaceOf(back) : null;
        bool corrected = back != null && back.story == ReturnStory.Corrected;
        if (back != null)
        {
            _looksRng = new SeededRandom(Seeds.ForLooks(back.caseSeed));
            _accountRng = new SeededRandom(Seeds.ForAccount(back.caseSeed));
        }

        // 2) A premade (the appearance's, or rolled from the day's pool on the premade stream); none in a returning traveller's slot.
        bool forcedPremade = false;
        LegendarySO legendary = back != null ? null : ResolvePremade(plan, state, caseIndex1Based, appearance, out forcedPremade);

        // 2.5) A planned faulty traveller stands in this slot (never a premade's: see ResolvePremade): a closure's violator
        //      claims its place; a planned liar's slot is read at the lie roll; a planned procedure's breaker is made below.
        _violators.TryGetValue(caseIndex1Based, out NationEraProfileSO violatorPlace);
        _violatorRules.TryGetValue(caseIndex1Based, out TravelRuleSO violatorRule);
        _plannedRules.TryGetValue(caseIndex1Based, out TravelRuleSO plannedRule);

        // 3) Decide the claimed era (the traveller's stated home and destination).
        EraSO claimedEra = backPlace != null ? backPlace.era
            : legendary != null ? legendary.trueEra
            : violatorPlace != null ? violatorPlace.era
            : PickEraFromPlan(plan);

        // 4) Decide blueprint: forced > one weighted pick of the day's kinds
        //    (active-effect weight multipliers applied; a premade's slot draws only
        //    the premade's kind and a planned liar's slot only the displaced, TravellerKinds.PickWeight;
        //    a planned procedure's slot only the kinds its rule reads and its maker
        //    can break, never an honest entry). A traveller drawn from an honest
        //    entry rolls no fault (K5, FaultOrder).
        TravellerKind? onlyKind = back != null ? back.kind
            : legendary != null ? legendary.kind
            : _plannedLiars.TryGetValue(caseIndex1Based, out LieKind plannedLie) && plannedLie != LieKind.Smuggling ? TravellerKind.Displaced
            : (TravellerKind?)null;
        KindWeight entry = forcedBlueprint != null ? null :
            WeightedRandom.Pick(plan.Kinds, k => k != null && k.blueprint != null && (plannedRule == null || (!k.honest && CanBreak(plan, plannedRule, k.blueprint)))
                                                 && (violatorRule == null || violatorRule.AppliesTo(k.blueprint.Kind))
                ? TravellerKinds.PickWeight(k.blueprint.Kind, k.weight, onlyKind) * TimelineEffects.GetBlueprintWeightMultiplier(state, _lib, k.blueprint.name)
                : 0f, _rng);
        CaseBlueprintSO blueprint = forcedBlueprint != null ? forcedBlueprint : entry?.blueprint;
        bool honest = (entry != null && entry.honest) || corrected;

        // A 2150 citizen (traveller types K2, K4) comes from the present: a drawn one, or a story character (a citizen premade, days 7-15 B2).
        bool citizen = blueprint != null && TravellerKinds.IsCitizen(blueprint.Kind);

        // 4.5) Timeline identity: archetype, place, visitor identity.
        ArchetypeSO archetype = back != null && _lib.Archetypes.Any(a => a != null && a.id == back.archetypeId)
            ? _lib.Archetypes.First(a => a != null && a.id == back.archetypeId)
            : PickArchetype(blueprint, legendary, state);
        NationEraProfileSO place = backPlace != null ? backPlace
            : violatorPlace != null ? violatorPlace
            : legendary == null && blueprint != null && plan.OpensOnly(blueprint.Kind) ? PickOpenPlace(plan, claimedEra, blueprint.Kind)
            : PickPlace(legendary, claimedEra, _plannedLiars.ContainsKey(caseIndex1Based) || plannedRule != null ? plan : null, blueprint != null ? blueprint.Kind : default);
        if (place != null && legendary == null)
            claimedEra = place.era; // an open place may be of another era than the weights' draw
        NationSO nation = legendary != null && legendary.nation != null ? legendary.nation : place != null ? place.nation : null;
        string originLabel = place != null ? PlaceLabel(place) : FallbackOriginLabel(nation, claimedEra);
        string givenName = back != null ? back.name : ResolveGivenName(legendary, forcedPremade, citizen ? _citizenNames.All : place != null ? place.AllNames : null, caseIndex1Based);
        TravellerGender gender = back != null ? back.gender
            : legendary != null ? legendary.gender
            : citizen ? _citizenNames.GenderOf(givenName)
            : place == null ? TravellerGender.Unknown
            : TravellerGenders.FromNameLists(givenName, place.maleNames, place.femaleNames);
        string role = archetype != null ? archetype.displayName : UiText.Get("case.roleUnknown");
        string visitorName = legendary != null ? givenName : $"{givenName} ({role})";
        string birthDate = back != null ? back.birthDate
            : legendary != null ? legendary.birthDate
            : citizen ? GenerateBirthDate(_present != null ? _present.BirthYearMin : 0, _present != null ? _present.BirthYearMax : 0)
            : GenerateBirthDate(place != null ? place.birthYearMin : 0, place != null ? place.birthYearMax : 0);
        NationEraProfileSO family = !citizen ? null : legendary != null ? PremadeFamily(legendary) : FamilyOf(givenName);
        string intro = Interview.Opener(_lib.Interview, gender, legendary != null && Premades.IsFamous(legendary.kind) ? legendary.displayName : null,
                                        back != null ? Interview.ReturningOpener(_lib.Interview, gender, back.deniedDay)
                                        : Premades.Voice(appearance != null ? appearance.introLine : null, legendary != null ? legendary.introLine : null));

        var inst = new CaseInstance
        {
            caseIndex = index0Based,
            claimedNation = nation,
            passportNation = family != null && family.nation != null ? family.nation : nation,
            claimedEra = claimedEra,
            isLegendary = legendary != null,
            legendarySource = legendary,
            forcedAppearance = appearance,
            premadeDialogId = Premades.Voice(appearance != null ? appearance.dialogId : null, legendary != null ? legendary.dialogId : null) ?? string.Empty,
            archetype = archetype,
            originLabel = originLabel,
            tongueId = !citizen && place != null && place.tongue != null ? place.tongue : string.Empty,
            visitorDisplayName = visitorName,
            visitorGivenName = givenName,
            caseSeed = _caseSeed,
            returning = back,
            trueBirthDate = birthDate,
            gender = gender,
            introLine = intro
        };

        // 4.55) The voice (the personalities spec's PS2-PS5): a generated traveller's personality is one draw on its own stream,
        //       which reads nothing else (never the kind, a lie or a fault); a premade draws nothing and speaks its own lines. The
        //       debug panel's force overrides after the draw, so no stream moves.
        inst.dialogSeed = _dialogSeed;
        if (legendary == null)
        {
            Personality drawn = Personalities.Pick(_lib.Personalities, _personalityRng);
            inst.personality = !string.IsNullOrEmpty(DevToolsState.ForcedPersonality) ? DevToolsState.ForcedPersonality
                : back != null ? back.personality ?? string.Empty
                : drawn != null ? drawn.id : string.Empty;
        }

        if (blueprint == null)
        {
            Debug.LogError($"CaseFactory generated a case with a null blueprint (Day {plan.DayNumber}, slot {caseIndex1Based}). Check DayPlanSO.possibleBlueprints / forcedCases.");
            return inst;
        }

        inst.kind = blueprint.Kind;
        inst.waiverIssued = plan.Issues(Directives.Waiver);
        _askable = _interview != null ? _interview.AskableCategories : System.Array.Empty<ClueCategory>();
        _answerTellCategories = _interview != null ? _interview.AnswerTellCategories : System.Array.Empty<ClueCategory>();

        // 4.6) The directive fault (traveller types P1): a closed destination, read against today's Directives.
        inst.directiveFault = plan.ClaimAllowed(nation, claimedEra, inst.kind) ? DirectiveFault.None : DirectiveFault.ClosedDestination;

        // 4.65) The slot's authored fault (days 7-15 B6), the appearance's authoring: a lie, or a directive fault whose
        //       rule is active today for the kind (its maker's variant pinned). Either skips every roll (K5, FaultOrder).
        LieKind? authoredLie = appearance != null && appearance.hasLie ? appearance.lie : (LieKind?)null;
        DirectivePlan authoredPlan = Directives.Plan(appearance != null ? appearance.directive : PlannedDirective.None);
        TravelRuleSO authoredRule = authoredPlan.IsFault ? AuthoredRule(plan, authoredPlan.Rule, inst.kind, caseIndex1Based) : null;
        bool authoredFault = authoredLie != null || authoredRule != null;

        // 4.7) The lie roll (K5: after the premade's authoring, the planned slot and the honest entry; before the account,
        //      so a poor citizen posing as rich holds the Standard account their papers must be checked against).
        //      A planned procedure's slot draws nothing: its smuggler is planned, its breaker is made below.
        //      A costume error forced from the debug panel is a planned fault: it stands in for the roll.
        bool forcedCostume = DevToolsState.ForcedCostumeError != CostumeError.None && legendary == null && !inst.HasDirectiveFault && !honest && place != null
                             && !_plannedLiars.ContainsKey(caseIndex1Based) && plannedRule == null && !authoredFault;
        LieKind? lieKind = forcedCostume ? null : RollLie(inst, plan, blueprint, state, legendary, honest, caseIndex1Based, plannedRule != null || authoredRule != null, authoredLie);

        // 4.8) The agency's file, on the account stream (the forms and the record print it):
        //      a displaced person's registry numbers, or a 2150 citizen's Citizen Account (LieKinds.TrueStatus: the truth).
        if (inst.kind == TravellerKind.Displaced && _today != null)
            inst.displacement = AgencyNumbers.Displaced(_today.Value, _lib.Agency.displaced, _agencyNumbers, _accountRng);
        else if (citizen && _today != null && AccountMaker.StatusOf(inst.kind, out CitizenStatus status))
            inst.account = AccountMaker.Make(AccountRequestFor(LieKinds.TrueStatus(lieKind, status), family, blueprint, claimedEra, lieKind == LieKind.DebtorPosingAsTourist, legendary, back?.citizenId),
                                             _lib.Agency.accounts, _transponders, _lib.Agency.proofs, _today.Value, _agencyNumbers, _accountRng);

        // 4.9) The violation (K5: after the lie roll, before the costume roll): the slot's authored directive fault or a
        //      guaranteed procedure's maker, or the roll on the fault stream for an honest traveller (never a premade's);
        //      the account side of the maker runs before the papers print.
        PaperSetBreak paperBreak = PaperSetBreak.None;
        TravelRuleSO broken = lieKind == null && !forcedCostume && !inst.HasDirectiveFault && (legendary == null || authoredRule != null)
            ? PlanViolation(inst, plan, blueprint, authoredRule ?? plannedRule, authoredPlan, honest, caseIndex1Based, out paperBreak)
            : null;

        // 5) Merge authored timeline impacts (blueprint + legendary).
        if (blueprint.AuthoredImpacts != null)
            inst.authoredImpacts.AddRange(blueprint.AuthoredImpacts);

        if (legendary != null && legendary.authoredImpacts != null)
            inst.authoredImpacts.AddRange(legendary.authoredImpacts);

        // 6) Build the documents the traveller carries today (their fields are filled below).
        BuildDocuments(inst, plan, blueprint);

        // 7) Investigation layer: structured fields (the claim is only spoken: InterviewScript.Opening), then the rolled
        //    lie planned and printed, or the paper side of a broken directive (a form left out or unsigned, a date falsified).
        PopulateDocumentFields(inst);
        LiePlan lie = lieKind == null || LieKinds.IsVisualLie(lieKind.Value) ? null
            : !LieKinds.IsPlaceLie(lieKind.Value) ? Forge(inst, lieKind.Value, plan, place, caseIndex1Based)
            : lieKind == LieKind.Smuggling ? Smuggle(inst, plan, caseIndex1Based)
            : Disguise(inst, plan, caseIndex1Based, place, legendary, lieKind.Value);
        BreakPapers(inst, paperBreak);
        if (broken != null && broken.type == TravelRuleType.PaperDates)
            FalsifyDate(inst, caseIndex1Based, authoredPlan.DateFault);
        AddAnswers(inst, lie);

        // Small talk (the personalities spec's V5): the personality's, the home's (a displaced person's claimed place, else its
        // era; a 2150 citizen's present, else the Future era) or the kind's lines, by the weights, as values of the dialog seed
        // (glue: only resolves the lists).
        EraSO talkEra = place != null ? place.era : claimedEra;
        NationEraProfileSO present = _present != null ? _lib.Profiles.FirstOrDefault(p => p != null && p.nation != null && p.era != null && p.nation.id == _present.NationId && p.era.id == _present.EraId) : null;
        inst.smallTalk = Voices.SmallTalk(_lib.Interview, inst.Voice, new VoiceContext(inst.kind, claimedEra != null ? claimedEra.id : null),
                                          Voices.Home(inst.kind, place != null ? place.smallTalk : null, talkEra != null ? talkEra.smallTalk : null,
                                                      present != null ? present.smallTalk : null, _lib.FutureEra != null ? _lib.FutureEra.smallTalk : null));

        bool plannedDress = plannedRule != null && plannedRule.type == TravelRuleType.DressForDestination;
        (LookSource source, bool whole) costume = broken != null ? (null, false) : PlanCostume(inst, place, legendary, forcedCostume, honest, plannedDress, authoredFault, plan, caseIndex1Based);
        inst.look = ComposeLook(inst, place, lie, legendary, family, costume, caseIndex1Based);

        // 7.5) The photos show who stands at the desk; a visual lie (the document design spec, D4, D8) is printed now, on the lie
        //      stream after the look: a forged seal on one paper, or someone else's photo.
        PrintPhotos(inst);
        if (lieKind != null && LieKinds.IsVisualLie(lieKind.Value))
            ForgeVisual(inst, lieKind.Value, caseIndex1Based);

        // 8) The Directives read the finished papers and account (traveller types P1, P3): the first rule broken is the fault.
        inst.facts = Facts(inst, plan);
        inst.directiveFault = Directives.Fault(plan.ActiveTravelRules.Where(r => r != null).Select(r => r.Directive).ToList(), inst.facts);
        if (broken != null && !inst.HasDirectiveFault)
            Debug.LogWarning($"[CaseFactory] Case {caseIndex1Based}: was to break '{broken.name}' ({broken.type}), but the finished papers read no fault. Check the kind's templates and the account ranges.");

        // 8.2) The desk's waiver pad (the endings and strandings spec §7.3): the waiver their kind carries, filled from their
        //      account and signed in their hand, and the fault a carried, signed waiver leaves (a missing or unsigned one cured,
        //      a forged one not: the pad never touches the papers they carry); their answer, one draw on their own stream.
        DocumentTemplateSO waiverForm = plan.TemplatesOf(blueprint).FirstOrDefault(t => t.formNumber == Directives.Waiver);
        if (waiverForm != null && inst.account != null)
        {
            inst.deskWaiver = DeskWaiver(inst, waiverForm);
            CaseFacts signed = Facts(inst, plan);
            signed.Forms = signed.Forms.Append(Directives.Waiver).ToList();
            signed.WaiverSigned = true;
            inst.faultWithDeskWaiver = Directives.Fault(plan.ActiveTravelRules.Where(r => r != null).Select(r => r.Directive).ToList(), signed);
            inst.factsWithDeskWaiver = signed;
        }
        Personality voice = _lib.Personalities.FirstOrDefault(p => p != null && p.id == inst.personality);
        inst.confess = voice != null ? voice.confess : 0f;
        bool carriesSigned = inst.documents.Any(d => d != null && d.template != null && d.template.formNumber == Directives.Waiver && Directives.IsSigned(d.fields));
        inst.waiverPadReply = Waivers.PadReply(inst.deskWaiver != null, carriesSigned, voice != null ? voice.waiverRefusal : 0f, _waiverSignRng);

        // 8.3) What a citation slip would name (lesson 6): the rule and the exact values of their fault, or the line of a wrong denial.
        inst.citation = CitationOf(inst, plan, lie);

        // 8.5) The slip (the personalities spec's T9-T10): a generated liar rolls once on their own stream against the day's
        //      slipChance, after the lie is planned; a liar premade slips when their line is authored; the honest never do.
        ReactionIntent intent = ReactionIntents.Of(inst.IsLiar, inst.IsForger);
        var slipContext = new VoiceContext(inst.kind, claimedEra != null ? claimedEra.id : null);
        bool slips = legendary != null
            ? Slips.PremadeSlips(intent, _lib.Interview.voices.slips.Exists(r => r != null && r.premade == legendary.id))
            : Slips.Rolls(intent, false) && Slips.Roll(plan.SlipChance, _slipRng);
        inst.slip = slips ? Voices.Slip(_lib.Interview, inst.Voice, slipContext, inst.lie) : null;

        // 8.6) The citizen file (the scanner app spec §3): the premade's lines or the random traveller's, drawn on their own
        //      file stream (a returning traveller's first visit's, so it comes back the same), and a line per earlier visit
        //      this run (WorldState.visits, kept under their record's identity). Last: it reads the finished case and moves no draw.
        inst.seenBefore = Visits.Before(state.visits, inst.RecordKey, state.day);
        inst.file = CitizenFile.Lines(_lib.Lore, LoreSubjectOf(inst, role), Seeds.ForLore(back != null ? back.caseSeed : _caseSeed), inst.seenBefore, state.day);

        string archetypeName = archetype != null ? archetype.displayName : string.Empty;
        string tells = lie != null ? string.Join(", ", lie.Tells.Select(t => $"{t}/{lie.ChannelOf(t)}")) : string.Empty;
        string look = inst.look != null ? inst.look.Describe() : "none";
        string recordTells = string.Join(", ", inst.recordTells.Select(t => $"{t.Category}@{t.Document}"));
        Debug.Log($"[CaseFactory] Case {caseIndex1Based}: blueprint='{blueprint.name}', kind={inst.kind}, honestEntry={honest}, account={inst.account?.CitizenId ?? "none"}, place='{originLabel}', archetype='{archetypeName}', premade={(legendary != null ? legendary.id : "none")}, personality={(string.IsNullOrEmpty(inst.personality) ? "none" : inst.personality)}, slip={(inst.slip != null ? inst.slip.id : "none")}, visitor='{visitorName}', born='{birthDate}', lie={(lieKind.HasValue ? lieKind.Value.ToString() : "none")}, liar={inst.IsLiar}, home='{inst.HomeLabel}', tells=[{tells}], recordTells=[{recordTells}], answers={inst.answers.Count}, gender={inst.gender}, look={look}, costume={inst.costumeFault}, broken={(broken != null ? broken.name : "none")}, paperSet={paperBreak}, pad={inst.waiverPadReply}, standing={inst.account?.Standing.ToString() ?? "none"}, directive={inst.directiveFault}, shouldAccept={inst.ShouldAccept}.");

        return inst;
    }

    /// <summary>The form numbers of a blueprint's templates issued on <paramref name="plan"/>'s day (DayPlanSO.TemplatesOf; null templates skipped).</summary>
    private static List<string> FormNumbers(DayPlanSO plan, CaseBlueprintSO blueprint) =>
        plan.TemplatesOf(blueprint).Select(t => t.formNumber).ToList();

    /// <summary>True when a traveller of <paramref name="blueprint"/>'s kind, carrying today's issued forms, can break <paramref name="rule"/> through its maker (Directives.CanBreak): the kinds a planned procedure's slot draws from.</summary>
    private static bool CanBreak(DayPlanSO plan, TravelRuleSO rule, CaseBlueprintSO blueprint) =>
        rule.AppliesTo(blueprint.Kind) && Directives.CanBreak(rule.type, blueprint.Kind, FormNumbers(plan, blueprint));

    /// <summary>
    /// The traveller's violation (traveller types P4, §5.4): the rule of a
    /// planned procedure's slot, or of the slot's authored directive fault
    /// (days 7-15 B6, <paramref name="authored"/>: its variant pinned), with
    /// a predicate (the paper set, the debt standing, the papers' dates; dress
    /// is the costume roll's), or for an honest traveller one of today's
    /// rolled procedures they can break, at the day's violation chance
    /// (Directives.Roll on the fault stream). Then the maker's account side,
    /// on the same stream: a Premium citizen's Economy unit
    /// (RecordLies.FalseTransponder, the paper set's <paramref name="paperBreak"/>
    /// drawn first, Directives.PickPaperSetBreak, or pinned), a Frozen
    /// standing since a day within agency.accounts.frozenWithinDays
    /// (AgencyNumbers.DaysAgo), or a recall's grounded model with a fresh
    /// serial (Directives.RecalledUnit, days 7-15 §6). The paper side follows the printing
    /// (BreakPapers, FalsifyDate). Null when honest (no draw for a traveller
    /// who can break nothing, or who rolls no fault: an honest kind entry,
    /// FaultOrder.MayRoll).
    /// </summary>
    private TravelRuleSO PlanViolation(CaseInstance inst, DayPlanSO plan, CaseBlueprintSO blueprint, TravelRuleSO plannedRule, DirectivePlan authored, bool honestEntry, int caseIndex1Based, out PaperSetBreak paperBreak)
    {
        paperBreak = PaperSetBreak.None;
        List<string> forms = FormNumbers(plan, blueprint);
        TravelRuleSO broken;
        if (plannedRule != null)
            broken = Directives.IsRolled(plannedRule.type) ? plannedRule : null;
        else if (!FaultOrder.MayRoll(FaultRoll.Violation, false, inst.HasDirectiveFault, honestEntry, inst.lie != null))
            return null;
        else
        {
            List<TravelRuleSO> breakable = plan.ActiveTravelRules.Where(r => r != null && Directives.IsRolled(r.type) && CanBreak(plan, r, blueprint)).ToList();
            int pick = Directives.Roll(plan.ViolationChance, breakable.Count, _faultRng);
            broken = pick < 0 ? null : breakable[pick];
        }

        if (broken == null)
            return null;

        switch (broken.type)
        {
            case TravelRuleType.PaperSet:
                if (inst.account == null)
                    return null;
                paperBreak = Directives.PickPaperSetBreak(inst.kind, forms, _faultRng, authored.PaperBreak);
                if (paperBreak == PaperSetBreak.None && authored.PaperBreak != PaperSetBreak.None)
                    Debug.LogWarning($"[CaseFactory] Case {caseIndex1Based}: the slot's authored {authored.PaperBreak} cannot show on a {inst.kind}'s papers, so the traveller stays honest. Check world_source.json days[].forced[].directive.");
                if (paperBreak == PaperSetBreak.EconomyManifest)
                {
                    string unit = RecordLies.FalseTransponder(TransponderClass.Economy, inst.account.Transponder, _transponders, _agencyNumbers, _faultRng);
                    if (unit == null)
                        Debug.LogWarning($"[CaseFactory] Case {caseIndex1Based}: was to travel on an Economy unit, but agency.transponders has no Economy model. Run Tools > TimeDesk > Generate World.");
                    else
                    {
                        inst.account.Transponder = unit;
                        inst.account.TransponderClass = TransponderClass.Economy;
                    }
                }
                break;
            case TravelRuleType.DebtStanding:
                if (inst.account == null)
                    return null;
                inst.account.Standing = AccountStanding.Frozen;
                inst.account.FrozenSince = _today != null ? AgencyCalendar.Write(AgencyNumbers.DaysAgo(_today.Value, _lib.Agency.accounts.frozenWithinDays, _faultRng)) : null;
                break;
            case TravelRuleType.TransponderRecall:
                TransponderModel recalled = (_lib.Agency.transponders ?? new List<TransponderModel>()).FirstOrDefault(t => t != null && t.id == broken.transponder);
                if (inst.account == null || recalled == null)
                {
                    if (recalled == null)
                        Debug.LogWarning($"[CaseFactory] Case {caseIndex1Based}: was to travel on the model '{broken.name}' recalls ('{broken.transponder}'), which agency.transponders does not have. Run Tools > TimeDesk > Generate World.");
                    return null;
                }
                inst.account.Transponder = Directives.RecalledUnit(recalled, _agencyNumbers, _faultRng);
                inst.account.TransponderClass = recalled.transponderClass;
                break;
        }

        return broken;
    }

    /// <summary>
    /// The paper side of a broken paper set (§5.4), after the papers print:
    /// the Stranding Waiver or the proof of means left out (the traveller
    /// never hands it over and answers the request with their kind's Missing
    /// line, MissingFormVariant.Missing; the account keeps it on file), or the
    /// waiver's Signature box reading UNSIGNED (Directives.UnsignedMark).
    /// </summary>
    private static void BreakPapers(CaseInstance inst, PaperSetBreak paperBreak)
    {
        switch (paperBreak)
        {
            case PaperSetBreak.WaiverMissing:
                inst.documents.RemoveAll(d => d.template != null && d.template.formNumber == Directives.Waiver);
                inst.missingFormVariant = MissingFormVariant.Missing;
                break;
            case PaperSetBreak.ProofMissing:
                inst.documents.RemoveAll(d => d.template != null && Directives.Proofs.Contains(d.template.formNumber));
                inst.missingFormVariant = MissingFormVariant.Missing;
                break;
            case PaperSetBreak.WaiverUnsigned:
                DocumentField signature = inst.documents.Where(d => d.template != null && d.template.formNumber == Directives.Waiver)
                    .SelectMany(d => d.fields).FirstOrDefault(f => f != null && f.category == ClueCategory.Signature);
                if (signature != null)
                    signature.value = Directives.UnsignedMark;
                break;
        }
    }

    /// <summary>
    /// What the Directives read of the finished traveller (CaseFacts): the
    /// kind; whether today's closures forbid the claim; the class the visa
    /// and the manifest print (unreadable: none); the forms carried; whether
    /// the waiver is carried and signed (Directives.IsSigned; a number the
    /// account never registered is a forgery, L3, not a paper-set fault); the
    /// account's standing; and the papers' dates against today.
    /// </summary>
    private CaseFacts Facts(CaseInstance inst, DayPlanSO plan)
    {
        DocumentInstance waiver = inst.documents.FirstOrDefault(d => d.template != null && d.template.formNumber == Directives.Waiver);
        List<DocumentField> fields = inst.documents.SelectMany(d => d.fields).Where(f => f != null).ToList();
        return new CaseFacts
        {
            Kind = inst.kind,
            ClosedDestination = !plan.ClaimAllowed(inst.claimedNation, inst.claimedEra, inst.kind),
            VisaClass = System.Enum.TryParse(FieldValue(inst, Directives.Visa, ClueCategory.AccountStatus), out CitizenStatus visa) ? visa : (CitizenStatus?)null,
            ManifestClass = System.Enum.TryParse(FieldValue(inst, Directives.Manifest, ClueCategory.TransponderClass), out TransponderClass manifest) ? manifest : (TransponderClass?)null,
            ManifestModelId = RecordLies.ModelIdOf(FieldValue(inst, Directives.Manifest, ClueCategory.TransponderId), _lib.Agency.transponders),
            Forms = inst.documents.Where(d => d.template != null).Select(d => d.template.formNumber).ToList(),
            WaiverSigned = waiver != null && Directives.IsSigned(waiver.fields),
            Frozen = inst.account != null && inst.account.Standing == AccountStanding.Frozen,
            Departures = fields.Where(f => f.category == ClueCategory.DepartureDate).Select(f => f.value).ToList(),
            ValidUntils = fields.Where(f => f.category == ClueCategory.Expiry).Select(f => f.value).ToList(),
            Today = _today,
            Issued = plan.Papers.ToList()
        };
    }

    /// <summary>
    /// What a citation slip names about the finished traveller (Papers Please
    /// lesson 6, Citations; no draw): for a directive fault, the first of
    /// today's rules that finds it (its memo row), its line (a closure's or a
    /// recall's own summary; a paper set's broken condition, the frozen
    /// standing or the papers' dates as a check's line) and the values that
    /// break it (the destination, the box that is wrong, the date today);
    /// for a costume error the dress rule, the garment and the destination;
    /// for a forger the record check (the kind's procedure line's row), the
    /// first forged box and what the record holds; for a place liar the
    /// return-home or no-2150-goods rule, the first tell (a paper's box, an
    /// answer or a garment) and the claimed place's value; for an honest
    /// traveller the line a wrong denial prints (the destination open).
    /// </summary>
    private CitationFacts CitationOf(CaseInstance inst, DayPlanSO plan, LiePlan lie)
    {
        List<TravelRuleSO> rules = plan.ActiveTravelRules.Where(r => r != null).ToList();
        List<string> lines = rules.Select(r => r.Summary()).ToList();
        var c = new CitationFacts();
        void Rule(TravelRuleSO rule, string key)
        {
            c.DirectiveNumber = rule != null ? Citations.MemoNumber(lines, rules.IndexOf(rule)) : 0;
            c.RuleText = rule != null && key == null ? rule.Summary() : null;
            c.RuleKey = key;
        }
        void Add(string label, string value) => c.Values.Add(new CitationValue(label, value));
        TravelRuleSO Of(TravelRuleType type) => rules.FirstOrDefault(r => r.type == type && r.AppliesTo(inst.kind));
        string record = UiText.Get(inst.kind == TravellerKind.Displaced ? "citation.label.registry" : "citation.label.account");
        string today = _today != null ? AgencyCalendar.Write(_today.Value) : null;

        if (inst.HasDirectiveFault)
        {
            CaseFacts facts = Facts(inst, plan);
            // A closed destination cites the closure that closes it (the open destinations, or the range limit beside them).
            TravelRuleSO broken = rules.FirstOrDefault(r => r.AppliesTo(inst.kind) && Directives.FaultOf(r.Directive, facts) == inst.directiveFault
                                                            && (!r.IsClosure || !r.Allows(inst.claimedNation, inst.claimedEra)));
            switch (inst.directiveFault)
            {
                case DirectiveFault.ClosedDestination:
                    Rule(broken, null);
                    Add(UiText.Get("citation.label.destination"), inst.originLabel);
                    break;
                case DirectiveFault.RecalledTransponder:
                    Rule(broken, null);
                    AddBox(c, inst, d => d.template.formNumber == Directives.Manifest, f => f.category == ClueCategory.TransponderId);
                    break;
                case DirectiveFault.IncompletePapers:
                    string breach = Citations.PaperSetBreach(facts);
                    Rule(broken, breach != null ? "citation.rule.paperSet." + breach : null);
                    if (breach == Citations.EconomyManifest || breach == Citations.PremiumManifest)
                    {
                        AddBox(c, inst, d => d.template.formNumber == Directives.Visa, f => f.category == ClueCategory.AccountStatus);
                        AddBox(c, inst, d => d.template.formNumber == Directives.Manifest, f => f.category == ClueCategory.TransponderClass);
                    }
                    else if (breach == Citations.WaiverUnsigned)
                        AddBox(c, inst, d => d.template.formNumber == Directives.Waiver, f => f.category == ClueCategory.Signature);
                    else if (breach != null)
                        Add(UiText.Get("citation.label.missing." + breach), UiText.Get("citation.value.notHandedOver"));
                    break;
                case DirectiveFault.FrozenAccount:
                    Rule(broken, "citation.rule.frozen");
                    Add(UiText.Format("citation.label.standing", record), UiText.Format("citation.value.frozen", inst.account != null ? inst.account.FrozenSince : null));
                    break;
                case DirectiveFault.WrongDepartureDate:
                    Rule(broken, "citation.rule.departure");
                    AddBox(c, inst, d => true, f => f.category == ClueCategory.DepartureDate && f.value != today);
                    Add(UiText.Get("citation.label.today"), today);
                    break;
                case DirectiveFault.ExpiredPaper:
                    Rule(broken, "citation.rule.expired");
                    AddBox(c, inst, d => true, f => f.category == ClueCategory.Expiry && _today != null
                                                    && Directives.PaperDates(new string[0], new[] { f.value }, _today.Value) == DirectiveFault.ExpiredPaper);
                    Add(UiText.Get("citation.label.today"), today);
                    break;
            }
        }
        else if (inst.costumeFault != CostumeError.None)
        {
            TravelRuleSO dress = Of(TravelRuleType.DressForDestination);
            Rule(dress, dress != null ? null : "citation.rule.dress");
            Add(UiText.Get("citation.label.garment"), inst.CostumeItem);
            Add(UiText.Get("citation.label.destination"), inst.originLabel);
        }
        else if (inst.IsForger)
        {
            Rule(Of(TravelRuleType.Procedure), "citation.rule.record");
            RecordTell tell = inst.recordTells[0];
            AddBox(c, inst, d => inst.documents.IndexOf(d) == tell.Document, f => f.category == tell.Category);
            if (tell.Category == ClueCategory.Seal)
            {
                // A forged seal is held against its issuing office's seal in the Seal Register, never a place's fact.
                string form = inst.documents[tell.Document].template != null ? inst.documents[tell.Document].template.formNumber : null;
                AgencyOffice office = Seals.OfficeOf(_lib.Agency.offices, form);
                Add(UiText.Format("citation.label.book", UiText.Category(ClueCategory.Seal), office != null ? office.name : form), SealValue(office, form));
            }
            else
                Add(record, ResolveFieldValue(tell.Category, inst));
        }
        else if (inst.IsLiar && lie != null && lie.Tells.Count > 0)
        {
            bool smuggler = inst.lie == LieKind.Smuggling;
            TravelRuleSO rule = Of(smuggler ? TravelRuleType.NoPresentGoods : TravelRuleType.ReturnHome);
            Rule(rule, rule != null ? null : smuggler ? "citation.rule.goods" : "citation.rule.origin");
            ClueCategory tell = lie.Tells[0];
            TellChannel? channel = lie.ChannelOf(tell);
            if (channel == TellChannel.Papers)
                AddBox(c, inst, d => true, f => f.category == tell && f.isAnachronism);
            else if (channel == TellChannel.Answer)
                Add(UiText.Format("citation.label.answer", UiText.Category(tell)), lie.TellValue(tell));
            else
                Add(UiText.Get("citation.label.garment"), inst.look?.Garments.FirstOrDefault(g => g.IsTell)?.Label);
            if (channel != TellChannel.Appearance)
                Add(OnFile(tell) ? record : UiText.Format("citation.label.book", UiText.Category(tell), inst.originLabel), ResolveFieldValue(tell, inst));
            else
                Add(UiText.Get("citation.label.destination"), inst.originLabel);
        }
        else
        {
            Rule(null, "citation.rule.none");
            Add(UiText.Get("citation.label.destination"), UiText.Format("citation.value.open", inst.originLabel));
        }
        return c;
    }

    /// <summary>Adds the first box of the traveller's papers that <paramref name="field"/> picks on a paper <paramref name="paper"/> picks, as "{box} on the {paper}" with what it prints (nothing when no box matches).</summary>
    private static void AddBox(CitationFacts c, CaseInstance inst, System.Func<DocumentInstance, bool> paper, System.Func<DocumentField, bool> field)
    {
        foreach (DocumentInstance d in inst.documents)
        {
            if (d == null || d.template == null || !paper(d))
                continue;
            DocumentField box = d.fields.FirstOrDefault(f => f != null && field(f));
            if (box == null)
                continue;
            c.Values.Add(new CitationValue(UiText.Format("citation.label.onPaper", box.label, d.DisplayName), box.value));
            return;
        }
    }

    /// <summary>True for a category the agency's file holds (the record, the registry or the calendar), not a place's fact: what ResolveFieldValue reads from the traveller, never from the books.</summary>
    private static bool OnFile(ClueCategory category)
    {
        switch (category)
        {
            case ClueCategory.Name:
            case ClueCategory.BirthDate:
            case ClueCategory.CitizenId:
            case ClueCategory.Incident:
            case ClueCategory.Expiry:
            case ClueCategory.DepartureDate:
            case ClueCategory.AccountStatus:
            case ClueCategory.TransponderId:
            case ClueCategory.TransponderClass:
            case ClueCategory.Debt:
            case ClueCategory.WaiverNo:
            case ClueCategory.Credit:
            case ClueCategory.Funds:
            case ClueCategory.PolicyNo:
            case ClueCategory.Signature:
            case ClueCategory.Employer:
            case ClueCategory.Term:
            case ClueCategory.Wage:
                return true;
            default:
                return false;
        }
    }

    /// <summary>The value the form numbered <paramref name="formNumber"/> prints for <paramref name="category"/>; null without the form or the box.</summary>
    private static string FieldValue(CaseInstance inst, string formNumber, ClueCategory category) =>
        inst.documents.FirstOrDefault(d => d.template != null && d.template.formNumber == formNumber)?.fields.FirstOrDefault(f => f != null && f.category == category)?.value;

    /// <summary>Origin label when no place is authored for a nation+era (content gap).</summary>
    private static string FallbackOriginLabel(NationSO nation, EraSO era) =>
        OriginLabels.Format(nation != null ? nation.displayName : UiText.Get("case.unlistedLand"), era != null ? era.displayName : null);

    /// <summary>
    /// Fills each document's structured fields from today's facts for the
    /// case's claimed place (identity fields from the registered identity;
    /// a citizen's Valid Until is their account's date for that form, the
    /// expiring forms counted in paper order).
    /// </summary>
    private void PopulateDocumentFields(CaseInstance inst)
    {
        if (inst == null || _lib == null)
            return;

        int expiring = 0;
        foreach (DocumentInstance doc in inst.documents)
        {
            if (doc == null || doc.template == null || doc.template.fieldSpecs == null)
                continue;

            int expiryIndex = Expires(doc.template) ? expiring++ : 0;
            doc.serial = FormSerials.Make(doc.template.formNumber, _formsSeed, inst.documents.IndexOf(doc));
            foreach (DocumentFieldSpec spec in doc.template.fieldSpecs)
            {
                if (spec == null)
                    continue;

                DocumentField field = NewField(spec, inst, doc.template, expiryIndex);
                field.page = doc.template.form != null ? Mathf.Max(0, doc.template.form.PageOf(doc.fields.Count)) : 0;

                doc.fields.Add(field);
            }
        }
    }

    /// <summary>
    /// One field of <paramref name="template"/> as the traveller's file fills
    /// it (ResolveFieldValue; the <paramref name="expiryIndex"/>th expiring
    /// form's Valid Until); a Seal field prints the true seal of the office
    /// that issues the form, which it names as its issuer (the document design
    /// spec, D4).
    /// </summary>
    private DocumentField NewField(DocumentFieldSpec spec, CaseInstance inst, DocumentTemplateSO template, int expiryIndex = 0)
    {
        AgencyOffice office = spec.category == ClueCategory.Seal ? Seals.OfficeOf(_lib.Agency.offices, template.formNumber) : null;
        return new DocumentField
        {
            category = spec.category,
            label = string.IsNullOrEmpty(spec.label) ? spec.category.ToString() : spec.label,
            value = spec.category == ClueCategory.Seal ? SealValue(office, template.formNumber) : ResolveFieldValue(spec.category, inst, expiryIndex),
            issuer = office != null ? office.id : string.Empty
        };
    }

    /// <summary>
    /// A paper's true seal (the document design spec, D4): the description of
    /// the seal of the office that issues <paramref name="formNumber"/>
    /// (agency.offices), or a stable placeholder with a warning when no office
    /// issues it (Generate World refuses that content).
    /// </summary>
    private static string SealValue(AgencyOffice office, string formNumber)
    {
        if (office != null && office.TryGetSeal(out Seal seal))
            return Seals.Describe(seal);
        Debug.LogWarning($"[CaseFactory] No office issues form {formNumber} with a valid seal (agency.offices); its seal prints a placeholder. Run Tools > TimeDesk > Generate World.");
        return AgencyValue(null, ClueCategory.Seal);
    }

    /// <summary>Every Photo field shows who stands at the desk (Looks.IdentityKey of the traveller's look; the document design spec, D8); ForgeVisual may then swap one for a stranger's.</summary>
    private static void PrintPhotos(CaseInstance inst)
    {
        string who = Looks.IdentityKey(inst.look);
        foreach (DocumentInstance doc in inst.documents)
            foreach (DocumentField f in doc.fields)
                if (f != null && f.category == ClueCategory.Photo)
                    f.value = who;
    }

    /// <summary>
    /// The traveller's papers as the lie makers see them: each paper's fields
    /// that are introduced today (Introductions.ShowsField; the desk-first
    /// redesign, item 3), so a lie forges only what the player can read and
    /// a variant whose field is still hidden cannot show.
    /// </summary>
    private List<RecordForm> ShownForms(CaseInstance inst)
    {
        Introductions intro = _lib.Introductions;
        return inst.documents.Select(d =>
        {
            string form = d.template != null ? d.template.formNumber : null;
            return new RecordForm(form, d.fields.Where(f => f != null && intro.ShowsField(_day, form, f.category)).ToList());
        }).ToList();
    }

    /// <summary>
    /// Prints a rolled visual lie (the document design spec, D4, D8), drawing
    /// only from the published canon (agency.faults) on the lie stream after
    /// the look: a forged seal on one paper (VisualLies.PlanSeal) or someone
    /// else's photo (VisualLies.PlanPhoto; the stranger's look is the one the
    /// photo draws, CaseInstance.PhotoLook). Each printed value is a record
    /// tell (the traveller is a forger: HasDeviationFault). Nothing to print
    /// (no canon row, no office seal, a premade's whole picture) leaves the
    /// traveller honest with a warning.
    /// </summary>
    private void ForgeVisual(CaseInstance inst, LieKind kind, int caseIndex1Based)
    {
        List<RecordForm> forms = ShownForms(inst);
        TravellerLook stranger = null;
        List<RecordTell> tells = kind == LieKind.ForgedSeal
            ? VisualLies.PlanSeal(_lib.Agency.faults, forms, _lib.Agency.offices, _lieRng)
            : VisualLies.PlanPhoto(_lib.Agency.faults, forms, inst.look, _lieRng, out stranger);
        if (tells.Count == 0)
        {
            Debug.LogWarning($"[CaseFactory] Case {caseIndex1Based}: rolled {kind}, but no paper of theirs can carry it (the canon's rows, the offices' seals, a photo that can be swapped), so the traveller stays honest. Check agency.faults and agency.offices.");
            return;
        }

        foreach (RecordTell tell in tells)
            foreach (DocumentField f in inst.documents[tell.Document].fields)
                if (f != null && f.category == tell.Category)
                {
                    f.value = tell.Value;
                    f.isAnachronism = true;
                }
        inst.strangerPhoto = stranger;
        inst.recordTells = tells;
        inst.lie = kind;
    }

    /// <summary>A place's label as today's FactTable (and so the scanner) prints it; the profile's own label when the place is not in today's table.</summary>
    private string PlaceLabel(NationEraProfileSO p) => _facts.OriginLabel(p.nation.id, p.era.id) ?? p.OriginLabel;

    /// <summary>
    /// Rolls the traveller's lie on their lie stream (Lies.Roll; traveller
    /// types K5, §6.2): at the liar chance, one of the day's lies that fit
    /// the kind (LieKinds.For). A planned liar's slot (PlanViolators: the
    /// displaced's return home on its first day) tells its planned lie with
    /// no draw; a forced slot's authored lie (<paramref name="authoredLie"/>,
    /// days 7-15 B6) is told surely (Lies.Roll at a chance of 1, as a premade
    /// authored a liar); otherwise a premade's authoring decides: a premade
    /// authored as a liar lies surely (their false origin), an honest one
    /// never (Lies.MayLie). Exempt travellers (a closure's violator, a
    /// guaranteed procedure's breaker or an authored directive fault,
    /// <paramref name="plannedFault"/>; an honest kind entry:
    /// FaultOrder.MayRoll; no papers: Lies.MayLie) draw nothing. Null: honest.
    /// </summary>
    private LieKind? RollLie(CaseInstance inst, DayPlanSO plan, CaseBlueprintSO blueprint, WorldState state, LegendarySO legendary, bool honestEntry, int caseIndex1Based, bool plannedFault, LieKind? authoredLie)
    {
        if (_plannedLiars.TryGetValue(caseIndex1Based, out LieKind planned))
            return planned;
        if (plannedFault)
            return null;

        bool hasPapers = blueprint.DocumentTemplates != null && blueprint.DocumentTemplates.Any(t => t != null && t.fieldSpecs != null && t.fieldSpecs.Length > 0);
        if (authoredLie != null)
            return Lies.MayLie(false, !inst.HasDirectiveFault, hasPapers) ? Lies.Roll(1f, new[] { authoredLie.Value }, _lieRng) : null;
        if (legendary != null)
            return Lies.MayLie(legendary.truePlace == null, !inst.HasDirectiveFault, hasPapers) ? Lies.Roll(1f, new[] { LieKind.FalseOrigin }, _lieRng) : null;
        if (!FaultOrder.MayRoll(FaultRoll.Lie, false, inst.HasDirectiveFault, honestEntry, false) || !Lies.MayLie(false, true, hasPapers))
            return null;

        return Lies.Roll(LiarChance(blueprint, state), LieKinds.For(plan.EnabledLies, inst.kind), _lieRng);
    }

    /// <summary>The traveller's papers as the lie plans rewrite them: each paper's fields, in case order.</summary>
    private static List<IReadOnlyList<DocumentField>> Papers(CaseInstance inst) =>
        inst.documents.Select(d => (IReadOnlyList<DocumentField>)d.fields).ToList();

    /// <summary>
    /// Plans a rolled place lie and applies it (Lies.Plan): a false origin
    /// (L7) gets a true home among today's other places; the fake displaced
    /// (L8, <paramref name="kind"/>) come from the present, the one candidate
    /// (TodaysWorld.Present, whose row today's facts hold, so the origin
    /// proof names 2150). The tells are drawn from the fields the published
    /// canon names for the lie (CanonFields; the document design spec, D9).
    /// Every field of each Papers-tell category is
    /// rewritten with the source's value, while an Answer or Appearance tell
    /// leaves the papers on the cover (AddAnswers speaks an answer;
    /// ComposeLook dresses the garment). A home's dress may leak only when
    /// Looks.CanLeak holds for the claim and the traveller's gender (the
    /// present's whole outfit never does). A premade authored as a liar lies
    /// from their true place, and a premade authored a fake displaced person
    /// (a forced slot's lie) from the present, through papers and answers
    /// only (a premade's look never leaks). Returns the plan, or null for a
    /// premade whose true place is not in today's world, or a fake displaced
    /// person without a present (they stay honest).
    /// </summary>
    private LiePlan Disguise(CaseInstance inst, DayPlanSO plan, int caseIndex1Based, NationEraProfileSO place, LegendarySO legendary, LieKind kind)
    {
        // The homes the lie may come from: today's places (L7), the present (L8), or a lying premade's own true place.
        List<NationEraProfileSO> homes = _todays;
        IReadOnlyList<TellChannel> channels = legendary != null ? _channels.Where(c => c != TellChannel.Appearance).ToList() : _channels;
        if (legendary != null && legendary.truePlace != null)
        {
            if (!_todays.Contains(legendary.truePlace))
            {
                Debug.LogWarning($"[CaseFactory] Case {caseIndex1Based}: premade '{legendary.displayName}' is authored as a liar from '{legendary.truePlace.OriginLabel}', which is not in today's world, so they stay honest. List them only on days that include their true place.");
                return null;
            }

            homes = new List<NationEraProfileSO> { legendary.truePlace };
        }
        else if (kind == LieKind.FakeDisplaced)
        {
            if (_present == null)
            {
                Debug.LogWarning($"[CaseFactory] Case {caseIndex1Based}: rolled a fake displaced person, but today has no present to come from, so they stay honest. Run Tools > TimeDesk > Generate World.");
                return null;
            }

            homes = new List<NationEraProfileSO>();
        }

        // The homes as the lie rules see them, in order (HomeIndex indexes both): today's places, or the present alone.
        LookSource claim = place != null ? SourceOf(place) : null;
        var todays = new List<HomeCandidate>(homes.Count);
        var labels = new List<string>(homes.Count);
        foreach (NationEraProfileSO p in homes)
        {
            todays.Add(new HomeCandidate(p.nation.id, p.era.id, p.birthYearMin, p.birthYearMax,
                                         Looks.CanLeak(claim, SourceOf(p), inst.gender, _lib.LookRules)));
            labels.Add(PlaceLabel(p));
        }
        if (kind == LieKind.FakeDisplaced)
        {
            LookSource present = PresentSource();
            todays.Add(new HomeCandidate(_present.NationId, _present.EraId, _present.BirthYearMin, _present.BirthYearMax,
                                         present != null && Looks.CanLeak(claim, present, inst.gender, _lib.LookRules)));
            labels.Add(_present.Label);
        }

        LiePlan lie = Lies.Plan(
            plan.TellCount,
            inst.claimedNation != null ? inst.claimedNation.id : null,
            inst.claimedEra != null ? inst.claimedEra.id : null,
            inst.trueBirthDate,
            todays,
            CanonFields(inst, kind),
            _answerTellCategories,
            channels,
            _facts,
            _bookCategories,
            _lieRng,
            kind);

        if (lie.Outcome == LieOutcome.NoPossibleLie)
        {
            Debug.LogWarning($"[CaseFactory] Case {caseIndex1Based}: rolled {kind}, but no candidate home today differs from '{inst.originLabel}' in a fact its papers print, today's questions ask or its dress could leak (with a reference book), or in birth year, so the traveller stays honest. Widen the day's eras or countries, add a reference book, or allow more tell channels.");
        }
        else if (lie.Outcome == LieOutcome.Liar)
        {
            HomeCandidate home = todays[lie.HomeIndex];
            inst.tellSourceNationId = home.NationId;
            inst.tellSourceEraId = home.EraId;
            inst.trueHomeLabel = labels[lie.HomeIndex];
            inst.lie = kind;
            lie.ApplyTo(Papers(inst));
        }

        return lie;
    }

    /// <summary>
    /// Plans rolled smuggling and applies it (traveller types L1, L6;
    /// Lies.Plan on the lie stream with the present as the only candidate and
    /// Lies.SmuggledCategories as the only options, on the fields the published
    /// canon names for smuggling, CanonFields): the traveller's claim is
    /// honest, but every Currency or Technology field of a Papers tell reads
    /// the present's value (the manifest's currency carried and declared
    /// effects, a displaced person's coin of home and effects carried), or an
    /// Answer tell speaks it to the trip's question; never dress. The tell
    /// source is the present, whose Technology an accepted smuggler carries
    /// into the destination (HistoryService.RecordCarry). Without a present, or
    /// when neither category can show today, the traveller stays honest with a
    /// warning.
    /// </summary>
    private LiePlan Smuggle(CaseInstance inst, DayPlanSO plan, int caseIndex1Based)
    {
        if (_present == null)
        {
            Debug.LogWarning($"[CaseFactory] Case {caseIndex1Based}: rolled smuggling, but today has no present (no 2150 row), so the traveller stays honest. Run Tools > TimeDesk > Generate World.");
            return null;
        }

        var present = new[] { new HomeCandidate(_present.NationId, _present.EraId, _present.BirthYearMin, _present.BirthYearMax) };
        LiePlan lie = Lies.Plan(
            plan.TellCount,
            inst.claimedNation != null ? inst.claimedNation.id : null,
            inst.claimedEra != null ? inst.claimedEra.id : null,
            inst.trueBirthDate,
            present,
            CanonFields(inst, LieKind.Smuggling),
            _answerTellCategories,
            _channels,
            _facts,
            _bookCategories,
            _lieRng,
            LieKind.Smuggling,
            Lies.SmuggledCategories);

        if (lie.Outcome == LieOutcome.NoPossibleLie)
        {
            Debug.LogWarning($"[CaseFactory] Case {caseIndex1Based}: rolled smuggling, but neither the currency nor the technology of '{_present.Label}' can show against '{inst.originLabel}' today (no paper prints them, no question asks them, or the values match), so the traveller stays honest. Check the present's facts and the day's channels.");
        }
        else if (lie.Outcome == LieOutcome.Liar)
        {
            inst.tellSourceNationId = _present.NationId;
            inst.tellSourceEraId = _present.EraId;
            inst.trueHomeLabel = _facts.OriginLabel(_present.NationId, _present.EraId) ?? _present.Label;
            inst.lie = LieKind.Smuggling;
            lie.ApplyTo(Papers(inst));
        }

        return lie;
    }

    /// <summary>The fields of the traveller's papers the published canon lets <paramref name="lie"/> print a tell on (agency.faults; the document design spec, D9), in paper and field order.</summary>
    private List<DocumentField> CanonFields(CaseInstance inst, LieKind lie) => CanonFields(inst, FaultCanon.Of(lie));

    /// <summary>The fields of the traveller's papers the published canon's rows (<paramref name="fault"/>) name, in paper and field order: the only fields a maker may rewrite (D9).</summary>
    private List<DocumentField> CanonFields(CaseInstance inst, System.Func<FaultEntry, bool> fault) =>
        inst.documents.Where(d => d != null && d.template != null)
            .SelectMany(d => d.fields.Where(f => f != null && FaultCanon.Allows(_lib.Agency.faults, fault, d.template.formNumber, f.category)))
            .ToList();

    /// <summary>
    /// The PaperDates maker (traveller types §5.4; Directives), for a planned
    /// slot: one Range draw on the fault stream picks the date (the departure,
    /// or one of the expiring forms' Valid Until, Directives.PlanDateFault;
    /// an authored fault's <paramref name="pinned"/> variant skips the choice),
    /// then one draw the false date (1-3 days off, or 1-30 days past), among the
    /// dates the published canon names (CanonFields; the document design spec, D9); every
    /// field of that date is rewritten (a departure is printed once per
    /// traveller, the invariant of one value per category); the directive
    /// fault is what Directives.Fault reads back from the finished papers
    /// (Directives.PaperDates), so the verdict and the desk agree. No calendar
    /// or nothing printed: honest, with a warning.
    /// </summary>
    private void FalsifyDate(CaseInstance inst, int caseIndex1Based, PaperDateFault pinned)
    {
        List<DocumentField> departures = CanonFields(inst, FaultCanon.Of(DirectiveFault.WrongDepartureDate)).Where(f => f.category == ClueCategory.DepartureDate).ToList();
        List<DocumentField> expiries = CanonFields(inst, FaultCanon.Of(DirectiveFault.ExpiredPaper)).Where(f => f.category == ClueCategory.Expiry).ToList();
        PaperDatePlan dates = _today != null ? Directives.PlanDateFault(departures.Count > 0, expiries.Count, _faultRng, pinned) : new PaperDatePlan(PaperDateFault.None, -1);
        if (dates.Fault == PaperDateFault.None)
        {
            Debug.LogWarning($"[CaseFactory] Case {caseIndex1Based}: planned a paper-dates fault, but the traveller's forms print no departure or Valid Until (or the agency calendar cannot count today), so they stay honest. Check the kind's templates and agency.firstDate.");
            return;
        }

        if (dates.Fault == PaperDateFault.Departure)
        {
            string wrongDay = AgencyCalendar.Write(Directives.OffsetDeparture(_today.Value, _faultRng));
            foreach (DocumentField field in departures)
                field.value = wrongDay;
        }
        else
        {
            expiries[dates.ExpiryIndex].value = AgencyCalendar.Write(Directives.ExpiredValidUntil(_today.Value, _faultRng));
        }
    }

    /// <summary>
    /// Plans a rolled record lie and applies it (RecordLies.Plan, on the lie
    /// stream): the forged fields of the named forms are rewritten with the
    /// false values their Citizen Account disproves (the birth year from the
    /// present's years; fresh numbers from today's, which they join; a
    /// transponder from the agency's models; a debtor's classes from the
    /// status their kind poses as; a forged contract's employer from the
    /// era's, its worksite from today's other open places; a fake waiver's
    /// number with the waiver prefix; a forged policy with the policy's
    /// prefix), and become the traveller's record tells. A lie none of whose variants can show
    /// (nothing printed to forge) leaves the traveller honest with a warning.
    /// </summary>
    private LiePlan Forge(CaseInstance inst, LieKind kind, DayPlanSO plan, NationEraProfileSO place, int caseIndex1Based)
    {
        List<RecordForm> forms = ShownForms(inst);
        var context = new RecordLieContext
        {
            CoverBirthDate = inst.trueBirthDate,
            BirthYearMin = _present != null ? _present.BirthYearMin : 0,
            BirthYearMax = _present != null ? _present.BirthYearMax : 0,
            Transponders = _transponders,
            TakenToday = _agencyNumbers,
            PosedStatus = kind == LieKind.DebtorPosingAsTourist && AccountMaker.StatusOf(inst.kind, out CitizenStatus posed) ? posed : (CitizenStatus?)null,
            Employers = _lib.Agency.EmployersOf(inst.claimedEra != null ? inst.claimedEra.id : null),
            OpenPlaces = _todays.Where(p => p != place && plan.ClaimAllowed(p.nation, p.era, inst.kind)).Select(PlaceLabel).ToList(),
            WaiverPrefix = _lib.Agency.accounts != null ? _lib.Agency.accounts.waiverPrefix : null,
            Proofs = _lib.Agency.proofs,
            Canon = _lib.Agency.faults
        };
        LiePlan lie = RecordLies.Plan(kind, forms, inst.account, context, _lieRng);

        if (lie.Outcome == LieOutcome.NoPossibleLie)
        {
            Debug.LogWarning($"[CaseFactory] Case {caseIndex1Based}: rolled {kind}, but none of its forged fields is printed on the traveller's forms or can differ from their account, so the traveller stays honest. Check the kind's templates (TC-101, TC-230, TC-310, TC-415 to TC-417, TC-520), the account ranges, agency.proofs and agency.employers.");
        }
        else if (lie.Outcome == LieOutcome.Forger)
        {
            inst.recordTells = lie.RecordTells;
            inst.lie = kind;
            lie.ApplyTo(Papers(inst));
        }

        return lie;
    }

    /// <summary>
    /// The costume roll (traveller types C2, K5), for a 2150 citizen with no
    /// other fault (FaultOrder.MayRoll: not a premade, a closure's violator,
    /// an honest kind entry, a liar, a forger or a directive breaker) whose
    /// garments can be looked at today; no draw otherwise. Its candidates: today's other places whose signature item
    /// can leak onto the claim (Looks.CanLeak) and whose Costume Guide row
    /// differs from the claim's (per place, C5), the present's clothes, and
    /// the kit accessories that can leak (Looks.KitSource). It draws on the
    /// fault stream (CostumeErrors.Plan) at the day's costume error chance;
    /// the dress rule's guaranteed breaker (<paramref name="planned"/>, P4)
    /// skips the roll. A costume error forced from the debug panel skips the
    /// roll and the kind check (a dev cheat for testing before 2150 citizens
    /// reach a day) and is consumed here. Sets inst.costumeFault; returns the
    /// leak source and whether it is worn whole (the present's clothes), or
    /// no source.
    /// </summary>
    private (LookSource source, bool whole) PlanCostume(CaseInstance inst, NationEraProfileSO place, LegendarySO legendary, bool forced, bool honestEntry, bool planned,
                                                        bool authoredFault, DayPlanSO plan, int caseIndex1Based)
    {
        if (!(planned || FaultOrder.MayRoll(FaultRoll.Costume, legendary != null, inst.HasDirectiveFault || authoredFault, honestEntry, inst.IsLiar || inst.IsForger)) ||
            place == null || !_appearanceReachable || inst.gender == TravellerGender.Unknown || (!forced && !CostumeErrors.MayErr(inst.kind)))
            return (null, false);

        LookSource claim = SourceOf(place);
        List<LookSource> others = _todays
            .Where(p => p != place)
            .Select(SourceOf)
            .Where(p => Looks.CanLeak(claim, p, inst.gender, _lib.LookRules) && !Values.Match(p.CultureValue, claim.CultureValue))
            .ToList();
        LookSource present = PresentSource();
        GenderLook presentLook = present?.Wardrobe?.For(inst.gender);
        bool clothes = presentLook != null && presentLook.Signature.IsPresent && !Values.Match(present.CultureValue, claim.CultureValue);
        LookSource kitOwner = KitOwner(present);
        List<LookSource> kit = kitOwner == null ? new List<LookSource>()
            : _lib.PresentLook.Kit(inst.gender).Select(item => Looks.KitSource(kitOwner, item))
                  .Where(k => Looks.CanLeak(claim, k, inst.gender, _lib.LookRules)).ToList();

        CostumeError pinned = forced ? DevToolsState.ForcedCostumeError : CostumeError.None;
        CostumePlan costume = CostumeErrors.Plan(plan.CostumeErrorChance, forced || planned, pinned, _lib.LookRules.costumeErrors, others.Count, clothes, kit.Count, _faultRng);
        if (forced)
        {
            Debug.Log($"[CaseFactory] ForcedCostumeError '{pinned}' consumed by case {caseIndex1Based} ({inst.kind}): {costume.Error}.");
            DevToolsState.ForcedCostumeError = CostumeError.None;
        }

        if (costume.Error == CostumeError.None)
        {
            if (costume.Rolled)
                Debug.LogWarning($"[CaseFactory] Case {caseIndex1Based}: {(planned ? "was planned" : "rolled")} a costume error, but nothing wrong can show on '{inst.originLabel}' for a {inst.gender} traveller (no leakable item of another place today, no present's clothes or kit that differ), so they are dressed right. Check the wardrobes, the present and looks.costumeErrors.");
            return (null, false);
        }

        inst.costumeFault = costume.Error;
        switch (costume.Error)
        {
            case CostumeError.OtherPlace:
                return (others[costume.SourceIndex], false);
            case CostumeError.PresentClothes:
                return (present, true);
            default:
                return (kit[costume.SourceIndex], false);
        }
    }

    /// <summary>
    /// The present as the look rules see it (traveller types H1): today's
    /// present (TodaysWorld.Present: the leader's Future place and its outfit,
    /// or the neutral present and its clothes), under its own nation and era,
    /// valued with its Culture fact (its row in the Costume Guide, so a 2150
    /// garment's origin proof names 2150). Null without a present or clothes.
    /// </summary>
    private LookSource PresentSource()
    {
        if (_present == null || _present.Wardrobe == null)
            return null;

        return new LookSource
        {
            NationId = _present.NationId,
            EraId = _present.EraId,
            PlaceId = _present.NationId + "_" + _present.EraId,
            Wardrobe = _present.Wardrobe,
            CultureValue = _present.Fact(Looks.EvidenceCategory)
        };
    }

    /// <summary>
    /// Whose accessories the 2150 kit is (traveller types §7.3): the neutral
    /// present's, filed under its token and the present's era whoever leads,
    /// and valued with the present's Culture fact, so a kit accessory proves
    /// against today's present row. Null without a present.
    /// </summary>
    private static LookSource KitOwner(LookSource present) =>
        present == null ? null : new LookSource
        {
            NationId = Present.NeutralNationId,
            EraId = present.EraId,
            PlaceId = Present.NeutralNationId + "_" + present.EraId,
            CultureValue = present.CultureValue
        };

    /// <summary>
    /// How the traveller looks: a premade's whole picture once its neutral
    /// art is delivered (CharacterArt.HasFinalArt), else its generated
    /// stand-in (days 7-15 B4, PremadeStandIn); otherwise the
    /// claimed place's layers (Looks.Compose on the look stream), with one
    /// garment of the tell source (TellSource, a false origin's true home)
    /// for a dress tell, or the costume error's
    /// source: its signature item, or its whole look for the present's
    /// clothes. A 2150 citizen wears the destination's dress (traveller types
    /// C1) with their family country's skin and hair weights
    /// (<paramref name="family"/>: the Future place whose list gave the name),
    /// their age counted from the present's year. A missing place draws the
    /// minimal look and an unknown gender is drawn on the look stream, each
    /// with a warning.
    /// </summary>
    private TravellerLook ComposeLook(CaseInstance inst, NationEraProfileSO place, LiePlan lie, LegendarySO legendary, NationEraProfileSO family,
                                      (LookSource source, bool whole) costume, int caseIndex1Based)
    {
        if (legendary != null)
            return CharacterArt.HasFinalArt(LookKeys.Premade(legendary.id, LookKeys.NeutralExpression).Name)
                ? Looks.Whole(legendary.id, place != null ? SourceOf(place) : null, _lib.LookRules)
                : PremadeStandIn(inst, place, legendary, family);

        if (place == null)
        {
            Debug.LogWarning($"[CaseFactory] Case {caseIndex1Based}: '{inst.originLabel}' has no place today, so the traveller gets a body and head only. Run Tools > TimeDesk > Generate World.");
            return Looks.Compose(null, null, inst.gender, inst.trueBirthDate, 0, null, _lib.LookRules, _looksRng);
        }

        if (inst.gender == TravellerGender.Unknown)
            Debug.LogWarning($"[CaseFactory] Case {caseIndex1Based}: '{inst.visitorGivenName}' has no known gender (not on the place's name lists); the look draws one. Check the place's names.");

        LookSource leak = costume.source ?? (lie != null && lie.ChannelOf(Looks.EvidenceCategory) == TellChannel.Appearance ? TellSource(inst) : null);
        int year = family != null && _present != null ? _present.Year : place.year;
        LookWeights weights = family != null ? family.looks : place.looks;
        return Looks.Compose(SourceOf(place), leak, inst.gender, inst.trueBirthDate, year, weights, _lib.LookRules, _looksRng, costume.whole);
    }

    /// <summary>
    /// A premade's generated stand-in until its art lands (days 7-15 B4, Saleh:
    /// "premades with drawn art", a generated look meanwhile): its claimed
    /// place's dress (Looks.Compose, no leak: premades roll no costume error)
    /// over a story character's family country's looks and the present's year
    /// (<paramref name="family"/>), or the place's own looks and year for the
    /// famous, drawn on the premade's own stream (Seeds.ForPremadeLook), so the
    /// same face comes back at every appearance and no other draw moves.
    /// </summary>
    private TravellerLook PremadeStandIn(CaseInstance inst, NationEraProfileSO place, LegendarySO legendary, NationEraProfileSO family)
    {
        var rng = new SeededRandom(Seeds.ForPremadeLook(legendary.id));
        int year = family != null && _present != null ? _present.Year : place != null ? place.year : 0;
        LookWeights weights = family != null ? family.looks : place != null ? place.looks : null;
        return Looks.Compose(place != null ? SourceOf(place) : null, null, inst.gender, inst.trueBirthDate, year, weights, _lib.LookRules, rng);
    }

    /// <summary>
    /// A liar's tell source as the look rules see it (a dress tell's
    /// garment): today's place with the tell source's ids, or the present
    /// (a fake displaced person); null for an honest traveller or a source
    /// today's world does not hold.
    /// </summary>
    private LookSource TellSource(CaseInstance inst)
    {
        if (!inst.IsLiar)
            return null;

        NationEraProfileSO home = _todays.FirstOrDefault(p => p.nation != null && p.era != null && p.nation.id == inst.tellSourceNationId && p.era.id == inst.tellSourceEraId);
        if (home != null)
            return SourceOf(home);

        return _present != null && _present.NationId == inst.tellSourceNationId && _present.EraId == inst.tellSourceEraId ? PresentSource() : null;
    }

    /// <summary>A place as the look rules see it: its ids, wardrobe and today's Culture fact.</summary>
    private LookSource SourceOf(NationEraProfileSO p) => new LookSource
    {
        NationId = p.nation != null ? p.nation.id : null,
        EraId = p.era != null ? p.era.id : null,
        PlaceId = p.id,
        Wardrobe = p.wardrobe,
        CultureValue = _facts.Get(p.nation != null ? p.nation.id : null, p.era != null ? p.era.id : null, Looks.EvidenceCategory)
    };

    /// <summary>
    /// The traveller's answer to each of today's askable questions, in
    /// question order: the cover value ResolveFieldValue gives the papers (the
    /// registered birth date, the claim's fact or its placeholder), or an
    /// Answer tell's true-home value (Interview.Answer). Reads the claim, never
    /// the fields, so it does not matter that Disguise already applied the plan.
    /// </summary>
    private void AddAnswers(CaseInstance inst, LiePlan lie)
    {
        foreach (ClueCategory category in _askable)
            inst.answers.Add(Interview.Answer(category, ResolveFieldValue(category, inst), lie));
    }

    /// <summary>
    /// The chance a traveller lies: the blueprint's contradiction chance plus
    /// tomorrow's slot modifier and active ForgeryChanceBonus effects, clamped
    /// to 0..1.
    /// </summary>
    private float LiarChance(CaseBlueprintSO blueprint, WorldState state) =>
        Mathf.Clamp01(
            blueprint.ContradictionChance +
            (state != null ? state.forgeryChanceModifier : 0f) +
            TimelineEffects.SumFloat(state, _lib, EffectOpType.ForgeryChanceBonus));

    /// <summary>
    /// Resolves a field's value for the claim, one value per category per
    /// traveller (the traveller-types spec's F4): identity fields come from
    /// the registered identity (a liar's cover); the destination is the
    /// claimed place's label; the agency's numbers and dates from the
    /// traveller's file (a departure is dated today); place fields come from
    /// today's facts for the claimed place, with a readable placeholder (and
    /// a warning) when content is missing; also each spoken answer's cover
    /// value (AddAnswers). A 2150 citizen's account values come from their
    /// Citizen Account (the ID, the status, the transponder and its class,
    /// the debt, the waiver number, the proof of means the account holds,
    /// and the Valid Until of their <paramref name="expiryIndex"/>th
    /// expiring form); a waiver's signature is the traveller's own hand (their
    /// given name). A liar's Papers tells overwrite the printed values
    /// afterwards (Disguise); an Answer tell replaces only the spoken value
    /// (Interview.Answer).
    /// </summary>
    private string ResolveFieldValue(ClueCategory category, CaseInstance inst, int expiryIndex = 0)
    {
        CitizenAccount account = inst.account;
        switch (category)
        {
            case ClueCategory.Name: return inst.visitorGivenName;
            case ClueCategory.BirthDate: return inst.trueBirthDate;
            case ClueCategory.Destination: return inst.originLabel;
            case ClueCategory.CitizenId: return AgencyValue(account != null ? account.CitizenId : inst.displacement?.Number, category);
            case ClueCategory.Incident: return AgencyValue(inst.displacement?.Incident, category);
            case ClueCategory.Expiry:
                return AgencyValue(account != null ? account.ValidUntil.ElementAtOrDefault(expiryIndex) : inst.displacement?.ValidUntil, category);
            case ClueCategory.DepartureDate: return AgencyValue(_today != null ? AgencyCalendar.Write(_today.Value) : null, category);
            case ClueCategory.AccountStatus: return AgencyValue(account?.Status.ToString(), category);
            case ClueCategory.TransponderId: return AgencyValue(account?.Transponder, category);
            case ClueCategory.TransponderClass: return AgencyValue(account?.TransponderClass.ToString(), category);
            case ClueCategory.Debt: return AgencyValue(account != null ? AccountMaker.Credits(account.Debt) : null, category);
            case ClueCategory.WaiverNo: return AgencyValue(account?.WaiverNo, category);
            case ClueCategory.Credit:
            case ClueCategory.Funds:
            case ClueCategory.PolicyNo:
                return AgencyValue(account != null && account.ProofForm != null && account.ProofCategory == category ? account.ProofValue : null, category);
            case ClueCategory.Signature: return inst.visitorGivenName;
            case ClueCategory.Photo: return Looks.IdentityKey(inst.look);
            case ClueCategory.Employer: return AgencyValue(account?.Employer, category);
            case ClueCategory.Term: return AgencyValue(account != null && account.HasContract ? AccountMaker.Term(account.TermDays) : null, category);
            case ClueCategory.Wage: return AgencyValue(account != null && account.HasContract ? AccountMaker.Credits(account.Wage) : null, category);
        }

        string value = _facts.Get(inst.claimedNation != null ? inst.claimedNation.id : null,
                                  inst.claimedEra != null ? inst.claimedEra.id : null, category);

        if (!string.IsNullOrEmpty(value))
            return value;

        // No authored fact: a stable placeholder keeps the field internally
        // consistent (never a tell: a tell needs the claim's fact) and the gap visible.
        string e = inst.claimedEra != null ? inst.claimedEra.id : "unknown";
        Debug.LogWarning($"[CaseFactory] '{inst.originLabel}' has no {category} fact today; using a placeholder on the papers and in answers. Check the place's facts (Tools > TimeDesk > Validate Content Library).");
        return $"{category}:{e}";
    }

    /// <summary>An agency number or date, or a stable placeholder when the traveller has none (the day's calendar error names the cause).</summary>
    private static string AgencyValue(string value, ClueCategory category) => value ?? $"{category}:none";

    /// <summary>
    /// Picks the visitor archetype: the premade's > blueprint pool > library pool.
    /// Weights = baseWeight * active VisitorTagWeight effect multipliers, so
    /// "more scientists for 3 days" style effects bias generation automatically.
    /// </summary>
    private ArchetypeSO PickArchetype(CaseBlueprintSO blueprint, LegendarySO legendary, WorldState state)
    {
        if (legendary != null && legendary.archetype != null)
            return legendary.archetype;

        IReadOnlyList<ArchetypeSO> pool =
            blueprint != null && blueprint.ArchetypePool != null && blueprint.ArchetypePool.Length > 0
                ? blueprint.ArchetypePool
                : _lib.Archetypes;

        if (pool == null || pool.Count == 0)
            return null;

        return WeightedRandom.Pick(pool, a => a != null
            ? Mathf.Max(0f, a.baseWeight) * TimelineEffects.GetVisitorTagWeightMultiplier(state, _lib, a.tags)
            : 0f, _rng);
    }

    /// <summary>
    /// Picks the traveller's place: the premade's claimed place (if authored) >
    /// uniform pick among today's places in the claimed era > null (no place).
    /// A planned faulty traveller's slot (a planned liar or procedure breaker,
    /// <paramref name="openOn"/>) picks among the places open on that plan
    /// (K5: one fault per traveller, so no closure beside the planned fault),
    /// all of the era's when none is open; the same one draw either way.
    /// </summary>
    /// <summary>
    /// The destination of a traveller an open-destinations rule reads (the
    /// desk-first ramp: "all other travellers want that destination"): one of
    /// today's places every rule allows them, of <paramref name="claimedEra"/>
    /// when one is (the era weights still lean the draw), else of any era;
    /// with none open, the era's draw as before (PickPlace).
    /// </summary>
    private NationEraProfileSO PickOpenPlace(DayPlanSO plan, EraSO claimedEra, TravellerKind kind)
    {
        List<NationEraProfileSO> open = _todays.Where(p => plan.ClaimAllowed(p.nation, p.era, kind)).ToList();
        if (open.Count == 0)
            return PickPlace(null, claimedEra, plan, kind);
        List<NationEraProfileSO> ofEra = open.Where(p => p.era == claimedEra).ToList();
        List<NationEraProfileSO> candidates = ofEra.Count > 0 ? ofEra : open;
        return candidates[_rng.Range(0, candidates.Count)];
    }

    private NationEraProfileSO PickPlace(LegendarySO legendary, EraSO claimedEra, DayPlanSO openOn, TravellerKind kind)
    {
        if (legendary != null && legendary.nation != null)
        {
            NationEraProfileSO own = _lib.GetProfile(legendary.nation, claimedEra);
            if (own != null && !_todays.Contains(own))
                Debug.LogWarning($"[CaseFactory] Premade '{legendary.displayName}' claims '{own.OriginLabel}', which is not in today's world, so their papers print placeholders. List them only on days that include their place.");
            return own;
        }

        if (claimedEra == null)
            return null;

        var candidates = _todays.Where(p => p.era == claimedEra).ToList();
        if (openOn != null && candidates.Any(p => openOn.ClaimAllowed(p.nation, p.era, kind)))
            candidates = candidates.Where(p => openOn.ClaimAllowed(p.nation, p.era, kind)).ToList();
        return candidates.Count == 0 ? null : candidates[_rng.Range(0, candidates.Count)];
    }

    /// <summary>
    /// The visitor's given name (no role suffix; records lookup key), unique
    /// within the day: premade name (a forced premade's is reserved before slot
    /// 1) > a name of <paramref name="pool"/> (the claimed place's period names,
    /// or a 2150 citizen's: the Future places' lists together) > generic
    /// subject (only when the pool is empty; the validator flags that).
    /// </summary>
    private string ResolveGivenName(LegendarySO legendary, bool forcedPremade, IReadOnlyList<string> pool, int caseIndex1Based)
    {
        if (legendary != null)
        {
            // The roll skips taken names, so a clash means that filter was bypassed.
            if (!forcedPremade && !_roster.Reserve(legendary.displayName))
                Debug.LogError($"[CaseFactory] Premade '{legendary.displayName}' shares a name with an earlier visitor today; Citizen Records will return the first match.");
            return legendary.displayName;
        }

        string picked = _roster.Take(pool, n => _rng.Range(0, n));
        if (picked != null)
            return picked;

        string fallback = UiText.Format("case.subject", caseIndex1Based);
        _roster.Reserve(fallback);
        return fallback;
    }

    /// <summary>A birth date within a birth-year range: a place's, or the present's for a 2150 citizen ("Unknown" when none is authored, BirthDates.HasYears).</summary>
    private string GenerateBirthDate(int yearMin, int yearMax)
    {
        if (!BirthDates.HasYears(yearMin, yearMax))
            return "Unknown";

        return BirthDates.Generate(yearMin, yearMax, _rng);
    }

    /// <summary>A story character's family country's Future place (LegendarySO.family, else its claimed nation): their lineage and their generated look's weights; null, with a warning, when that country has no Future place.</summary>
    private NationEraProfileSO PremadeFamily(LegendarySO premade)
    {
        NationSO nation = premade.family != null ? premade.family : premade.nation;
        NationEraProfileSO family = _lib.Profiles.FirstOrDefault(p => p != null && p.nation == nation && p.era != null && p.era.isFuture);
        if (family == null)
            Debug.LogWarning($"[CaseFactory] Premade '{premade.displayName}' has no family country with a Future place ('{(nation != null ? nation.id : "none")}'), so they have no lineage. Check world_source.json premades[].family.");
        return family;
    }

    /// <summary>The Future place whose name list gave a 2150 citizen's name (CitizenNames.SourceOf): their family's country; null, with a warning, when no list holds it.</summary>
    private NationEraProfileSO FamilyOf(string givenName)
    {
        int source = _citizenNames.SourceOf(givenName);
        NationEraProfileSO family = source >= 0 ? _lib.GetProfileById(_citizenNames.Lists[source].Id) : null;
        if (family == null)
            Debug.LogWarning($"[CaseFactory] The 2150 citizen '{givenName}' is on no Future place's name list, so they have no family country (looks from the destination, no lineage). Check the Future places' names.");
        return family;
    }

    /// <summary>
    /// What the account maker needs for a citizen of <paramref name="status"/>
    /// (explicit inputs, audit R3-025): their family country's past places as
    /// lineages (in era order), every past place for their trips, their
    /// blueprint's forms (number, request group, whether it prints a Valid
    /// Until), of which the account decides the carried ones, and for a
    /// labourer the registered contract with the worksite's era's employers
    /// (agency.employers); a debtor posing as a tourist
    /// (<paramref name="debtorPosing"/>) from an entry that carries a proof of
    /// means draws one they do not hold (L4's poor variant); a story
    /// character (<paramref name="premade"/>, days 7-15 B3) brings its
    /// authored Citizen ID, debt and employer (by its agency.employers id);
    /// a returning traveller (wave 5, lesson 9) brings their own Citizen ID
    /// (<paramref name="citizenId"/>).
    /// </summary>
    private AccountRequest AccountRequestFor(CitizenStatus status, NationEraProfileSO family, CaseBlueprintSO blueprint, EraSO worksiteEra, bool debtorPosing, LegendarySO premade,
                                             string citizenId = null) => new AccountRequest
    {
        CitizenId = premade != null ? premade.citizenId : !string.IsNullOrWhiteSpace(citizenId) ? citizenId : null,
        Debt = premade != null ? premade.debt : 0,
        Employer = premade != null && !string.IsNullOrWhiteSpace(premade.employer)
            ? (_lib.Agency.employers ?? new List<Employer>()).FirstOrDefault(e => e != null && e.id == premade.employer)?.name
            : null,
        Status = status,
        Contract = blueprint.Kind == TravellerKind.Labourer,
        ForgedProof = debtorPosing && !AccountMaker.HoldsProof(status) &&
                      (blueprint.DocumentTemplates ?? System.Array.Empty<DocumentTemplateSO>()).Any(t => t != null && t.askGroup == AccountMaker.ProofGroup),
        Employers = _lib.Agency.EmployersOf(worksiteEra != null ? worksiteEra.id : null),
        Lineages = family != null && family.nation != null
            ? _lib.Profiles.Where(p => p != null && p.nation == family.nation && p.era != null && !p.era.isFuture)
                           .OrderBy(p => p.era.order)
                           .Select(p => p.OriginLabel)
                           .ToList()
            : new List<string>(),
        TripPlaces = _pastPlaces,
        Forms = (blueprint.DocumentTemplates ?? System.Array.Empty<DocumentTemplateSO>()).Where(t => t != null)
            .Select(t => new FormEntry(t.formNumber, t.askGroup, Expires(t))).ToList()
    };

    /// <summary>Whom the citizen file is about (CitizenFile): the case's registered identity, its account or agency file, its role and its fault (a clue must agree with it).</summary>
    private static LoreSubject LoreSubjectOf(CaseInstance inst, string role) => new LoreSubject
    {
        Kind = inst.kind,
        Premade = inst.legendarySource != null ? inst.legendarySource.id : string.Empty,
        Personality = inst.personality ?? string.Empty,
        Name = inst.visitorGivenName ?? string.Empty,
        Place = inst.originLabel ?? string.Empty,
        Era = inst.claimedEra != null ? inst.claimedEra.displayName : string.Empty,
        Role = role ?? string.Empty,
        Debt = inst.account != null ? inst.account.Debt : 0,
        Status = inst.account != null ? inst.account.Status.ToString() : string.Empty,
        Frozen = inst.account != null && inst.account.Standing == AccountStanding.Frozen,
        Trips = inst.account != null ? inst.account.Trips.Count : 0,
        Employer = inst.account != null && inst.account.HasContract ? inst.account.Employer : string.Empty,
        Wage = inst.account != null && inst.account.HasContract ? AccountMaker.Credits(inst.account.Wage) : string.Empty,
        Term = inst.account != null && inst.account.HasContract ? AccountMaker.Term(inst.account.TermDays) : string.Empty,
        Transponder = inst.account != null ? inst.account.Transponder ?? string.Empty : string.Empty,
        Incident = inst.displacement != null ? inst.displacement.Incident : string.Empty,
        Found = inst.displacement != null ? inst.displacement.Found : string.Empty,
        Fault = inst.directiveFault,
        Lie = inst.lie
    };

    /// <summary>True when the form prints a Valid Until (an Expiry field).</summary>
    private static bool Expires(DocumentTemplateSO template) =>
        template.fieldSpecs != null && template.fieldSpecs.Any(s => s != null && s.category == ClueCategory.Expiry);

    /// <summary>
    /// Builds the agency's citizen master record for a day's visitors: a 2150
    /// citizen's Citizen Account (AccountRecords.Record, traveller types §4.1:
    /// the art's three groups, found by Citizen ID or name; its Forms on file
    /// rows only for the forms issued on the case's day, CaseFacts.Issued), or one
    /// Displacement Registry entry (traveller types §4.2), found by its
    /// Displacement No. or name, a group of rows under UI string labels: Name,
    /// Displacement No., Born, Origin and Incident (evidence of their
    /// categories), Found, Status ("Awaiting return") and the clerk's Note (not
    /// evidence); the number, incident and found rows only with an agency file.
    /// Records carry the registered identity: an honest traveller's, or a
    /// liar's cover (claimed origin). They never reveal a true home. (Future:
    /// deliberately missing/corrupted records + family history.) Every
    /// record ends with the traveller's citizen file (FILE) and, when they
    /// were seen before this run, SEEN BEFORE with the flag of their latest
    /// verdict as of <paramref name="today"/> (CitizenFile.Groups; the
    /// scanner app spec §2.6, §3).
    /// </summary>
    public static CitizenRegistry BuildRegistry(IReadOnlyList<CaseInstance> cases, bool standing = true, int today = 0)
    {
        var registry = new CitizenRegistry();

        if (cases == null)
            return registry;

        foreach (CaseInstance inst in cases)
        {
            if (inst == null || string.IsNullOrWhiteSpace(inst.visitorGivenName))
                continue;

            string origin = !string.IsNullOrEmpty(inst.originLabel) ? inst.originLabel : FallbackOriginLabel(inst.claimedNation, inst.claimedEra);
            List<RecordGroup> fileGroups = CitizenFile.Groups(inst.file, inst.seenBefore, today, UiText.Get);
            if (inst.account != null)
            {
                CitizenRecord account = AccountRecords.Record(inst.visitorGivenName, inst.trueBirthDate, origin, inst.account, UiText.Get,
                                                              inst.isLegendary && inst.legendarySource != null ? inst.legendarySource.recordNote : null,
                                                              inst.facts != null ? inst.facts.Issued : null, standing);
                registry.Add(new CitizenRecord(account.FullName, account.Number, account.Groups.Concat(fileGroups)));
                continue;
            }

            string note = !inst.isLegendary ? UiText.Get("records.note.none")
                : inst.legendarySource != null && !string.IsNullOrWhiteSpace(inst.legendarySource.recordNote) ? inst.legendarySource.recordNote
                : UiText.Get("records.note.sealed");
            DisplacementFile file = inst.displacement;
            var rows = new List<RecordRow> { new RecordRow(UiText.Get("records.row.name"), inst.visitorGivenName, ClueCategory.Name) };
            if (file != null)
                rows.Add(new RecordRow(UiText.Get("records.row.number"), file.Number, ClueCategory.CitizenId));
            rows.Add(new RecordRow(UiText.Get("records.row.born"), inst.trueBirthDate, ClueCategory.BirthDate));
            rows.Add(new RecordRow(UiText.Get("records.row.origin"), origin, ClueCategory.Destination));
            if (file != null)
            {
                rows.Add(new RecordRow(UiText.Get("records.row.incident"), file.Incident, ClueCategory.Incident));
                rows.Add(new RecordRow(UiText.Get("records.row.found"), file.Found));
            }
            rows.Add(new RecordRow(UiText.Get("records.row.status"), UiText.Get("records.status.awaiting")));
            rows.Add(new RecordRow(UiText.Get("records.row.note"), note));
            registry.Add(new CitizenRecord(inst.visitorGivenName, file?.Number, new[] { new RecordGroup(UiText.Get("records.group.registry"), rows) }.Concat(fileGroups)));
        }

        return registry;
    }

    /// <summary>
    /// Picks the claimed era by the DayPlan weights; an era with no place
    /// today (the Future, never a destination) is never drawn. With no
    /// weights (or bad data), picks uniformly among the eras that have a
    /// place today.
    /// </summary>
    private EraSO PickEraFromPlan(DayPlanSO plan)
    {
        if (plan.EraWeights != null && plan.EraWeights.Count > 0)
        {
            EraSO picked = WeightedRandom.Pick(plan.EraWeights, ew => ew.era != null && _todays.Any(p => p.era == ew.era) ? ew.weight : 0f, _rng).era;
            if (picked != null)
                return picked;
        }

        var eras = _todays.Select(p => p.era).Distinct().ToList();
        if (eras.Count == 0)
        {
            Debug.LogError("CaseFactory cannot pick an era: today has no places. Check the day plan's era weights and allowed nations.");
            return null;
        }

        return eras[_rng.Range(0, eras.Count)];
    }

    /// <summary>
    /// The slot's premade (Premades.SlotSource): the standing appearance's
    /// premade (<paramref name="forced"/> true); none when a premade is forced
    /// here but no appearance with a premade stands (met, or its conditions
    /// failed: an ordinary traveller stands there) or the slot is a planned
    /// violator's or planned liar's; otherwise a roll from the day's pool.
    /// </summary>
    private LegendarySO ResolvePremade(DayPlanSO plan, WorldState state, int caseIndex1Based, ForcedCaseSlot appearance, out bool forced)
    {
        forced = false;
        bool forcedHere = (appearance != null && appearance.legendary != null) || plan.ForcedAt(caseIndex1Based).Any(f => f.legendary != null);
        LegendarySO standing = appearance != null ? appearance.legendary : null;

        switch (Premades.SlotSource(forcedHere, standing != null, _violators.ContainsKey(caseIndex1Based) || _plannedLiars.ContainsKey(caseIndex1Based) || _plannedRules.ContainsKey(caseIndex1Based)))
        {
            case PremadeSlot.Forced:
                forced = true;
                return standing;
            case PremadeSlot.None:
                if (forcedHere)
                    Debug.Log($"[CaseFactory] Case {caseIndex1Based}: no forced premade stands here today (met this run, or the conditions failed); the slot holds an ordinary traveller.");
                return null;
            default:
                return RollPremade(plan, state);
        }
    }

    /// <summary>
    /// The denied travellers who come back today (wave 5, lesson 9): those of
    /// the world's returns due today who fit it (Returns.Due, at most the day
    /// plan's ReturnsMax: the plan takes their kind, their place is in today's
    /// world and open to their kind, their name is free and so is their
    /// Citizen ID), each in a slot no forced appearance or planned faulty
    /// traveller holds (Returns.Slots on the day's own return stream, drawn
    /// after the violators, so a day with none draws as before). Their names
    /// and numbers are reserved before slot 1.
    /// </summary>
    private Dictionary<int, ReturningTraveller> PlanReturns(DayPlanSO plan, WorldState state, int total, int daySeed)
    {
        var planned = new Dictionary<int, ReturningTraveller>();
        List<ReturningTraveller> due = Returns.Due(state.returns, state.day, r => ReturnFits(plan, r), plan.ReturnsMax);
        if (due.Count == 0)
            return planned;

        var taken = new HashSet<int>(_appearances.Keys.Concat(_violators.Keys).Concat(_plannedLiars.Keys).Concat(_plannedRules.Keys));
        List<int> slots = Returns.Slots(total, taken, due.Count, new SeededRandom(Seeds.ForReturnSlots(daySeed)));
        for (int i = 0; i < slots.Count; i++)
        {
            ReturningTraveller r = due[i];
            planned[slots[i]] = r;
            _roster.Reserve(r.name);
            if (!string.IsNullOrWhiteSpace(r.citizenId))
                _agencyNumbers.Add(r.citizenId.Trim());
            Debug.Log($"[CaseFactory] Day {state.day} slot {slots[i]}: '{r.displayName}' comes back ({r.story}), turned away on day {r.deniedDay}.");
        }
        return planned;
    }

    /// <summary>True when a returning traveller fits today: the plan draws their kind, their place is among today's and open to them, and their name and Citizen ID are free.</summary>
    private bool ReturnFits(DayPlanSO plan, ReturningTraveller r)
    {
        NationEraProfileSO place = PlaceOf(r);
        return place != null && plan.Kinds.Any(k => k != null && k.blueprint != null && k.blueprint.Kind == r.kind && k.weight > 0f)
               && plan.ClaimAllowed(place.nation, place.era, r.kind)
               && !_roster.IsTaken(r.name)
               && (string.IsNullOrWhiteSpace(r.citizenId) || !_agencyNumbers.Contains(r.citizenId.Trim()));
    }

    /// <summary>A returning traveller's claimed place among today's places; null when today's world does not hold it.</summary>
    private NationEraProfileSO PlaceOf(ReturningTraveller r) =>
        _todays.FirstOrDefault(p => p != null && p.nation != null && p.era != null && p.nation.id == r.nationId && p.era.id == r.eraId);

    /// <summary>
    /// Each forced slot's appearance today (days 7-15 B9): its entries in the
    /// authored order (DayPlanSO.ForcedAt), the first that stands
    /// (Premades.Stands: its conditions pass on the world as it stands at the
    /// day's start, TimelineService.ConditionsPass, and its premade is not a
    /// once-per-run premade already met) wins (Premades.Appearance). A slot
    /// where none stands is left out, with a log line (a failed condition is
    /// the story's choice, never a warning). The cheat menu's forced
    /// appearance (DevToolsState.ForcedAppearance: a famous traveller or
    /// another day's story beat) takes slot 1 whatever its conditions, and is
    /// used up.
    /// </summary>
    private static Dictionary<int, ForcedCaseSlot> Appearances(DayPlanSO plan, WorldState state)
    {
        var appearances = new Dictionary<int, ForcedCaseSlot>();
        foreach (int slot in plan.ForcedCases.Where(f => f != null).Select(f => f.caseIndex1Based).Distinct())
        {
            List<ForcedCaseSlot> entries = plan.ForcedAt(slot).ToList();
            List<bool> standing = entries.Select(e => Premades.Stands(e.legendary != null && e.legendary.oncePerRun, IsMet(state, e.legendary),
                                                                      TimelineService.ConditionsPass(e.conditions, state))).ToList();
            int pick = Premades.Appearance(standing);
            if (pick >= 0)
                appearances[slot] = entries[pick];
            else
                Debug.Log($"[CaseFactory] Day {plan.DayNumber} slot {slot}: none of its forced entries stands today ([{string.Join(", ", entries.Select(Describe))}]: met this run, or their conditions failed); an ordinary traveller stands there.");
        }

        ForcedCaseSlot cheat = DevToolsState.ForcedAppearance;
        if (cheat != null)
        {
            // It stands once: the same premade or story beat leaves the slot the day gave it.
            foreach (int slot in appearances.Where(a => (cheat.legendary != null && a.Value.legendary == cheat.legendary) || (!string.IsNullOrWhiteSpace(cheat.id) && a.Value.id == cheat.id)).Select(a => a.Key).ToList())
                appearances.Remove(slot);
            Debug.Log($"[CaseFactory] Cheat: '{Describe(cheat)}' stands in slot 1 of day {plan.DayNumber} (the cheat menu's forced appearance, used up).");
            appearances[1] = cheat;
            DevToolsState.ForcedAppearance = null;
        }

        return appearances;
    }

    /// <summary>A forced entry as the logs name it: its id, else its premade's or blueprint's name.</summary>
    public static string Describe(ForcedCaseSlot entry) =>
        entry == null ? "none"
        : !string.IsNullOrWhiteSpace(entry.id) ? entry.id
        : entry.legendary != null ? entry.legendary.displayName
        : entry.caseBlueprint != null ? entry.caseBlueprint.name
        : "empty";

    /// <summary>
    /// Rolls the day's pool on the slot's premade stream (Premades.Roll): the
    /// chance is the day's plus WorldState.legendaryChanceBonus and active
    /// LegendaryChanceBonus effects; only premades not met this run whose name
    /// nobody has today may roll. The dev cheat forces the roll once.
    /// </summary>
    private LegendarySO RollPremade(DayPlanSO plan, WorldState state)
    {
        bool cheat = DevToolsState.ForceLegendaryNextCase;
        var rollable = (plan.AvailableLegendaries ?? System.Array.Empty<LegendarySO>())
            .Where(l => l != null && Premades.IsRollable(IsMet(state, l), _roster.IsTaken(l.displayName)))
            .ToList();

        if (rollable.Count == 0)
        {
            if (cheat)
                Debug.LogWarning("[CaseFactory] ForceLegendaryNextCase is set but no premade can roll in this slot (none in the day's pool, or all met or named today); flag left active.");
            return null;
        }

        float chance = Mathf.Clamp01(
            plan.LegendaryBaseChance +
            (state != null ? state.legendaryChanceBonus : 0f) +
            TimelineEffects.SumFloat(state, _lib, EffectOpType.LegendaryChanceBonus));

        int pick = Premades.Roll(chance, rollable.Count, cheat, _legendaryRng);
        if (cheat)
        {
            Debug.Log($"[CaseFactory] ForceLegendaryNextCase consumed ({rollable.Count} candidate(s)).");
            DevToolsState.ForceLegendaryNextCase = false;
        }

        return pick < 0 ? null : rollable[pick];
    }

    /// <summary>True when a forced entry names an authored fault (a lie or a directive fault; days 7-15 B6).</summary>
    private static bool HasAuthoredFault(ForcedCaseSlot entry) => entry != null && (entry.hasLie || entry.directive != PlannedDirective.None);

    /// <summary>
    /// The rule an authored directive fault breaks today: the first of the
    /// day's active rules of <paramref name="type"/> that applies to the
    /// traveller's <paramref name="kind"/>; null, with a warning, when none
    /// stands today (the traveller stays honest; the content checks refuse it).
    /// </summary>
    private static TravelRuleSO AuthoredRule(DayPlanSO plan, TravelRuleType type, TravellerKind kind, int caseIndex1Based)
    {
        TravelRuleSO rule = plan.ActiveTravelRules.FirstOrDefault(r => r != null && r.type == type && r.AppliesTo(kind));
        if (rule == null)
            Debug.LogWarning($"[CaseFactory] Day {plan.DayNumber} case {caseIndex1Based}: the slot's authored directive fault breaks a {type} rule, but none stands today for a {kind}, so the traveller stays honest. Check world_source.json days[].forced[].directive and days[].rules.");
        return rule;
    }

    /// <summary>True when the premade was presented earlier this run (WorldState.HasMetPremade).</summary>
    private static bool IsMet(WorldState state, LegendarySO premade) =>
        state != null && premade != null && state.HasMetPremade(premade.id);

    /// <summary>
    /// The waiver the desk's pad files when the traveller signs (the endings
    /// and strandings spec §7.3): the waiver form's fields filled from the
    /// traveller's file as the desk writes them (ResolveFieldValue: their
    /// account's registered number and unit, their own hand on the Signature),
    /// never their papers' printed (possibly forged) values.
    /// </summary>
    private DocumentInstance DeskWaiver(CaseInstance inst, DocumentTemplateSO form)
    {
        var doc = new DocumentInstance { template = form };
        foreach (DocumentFieldSpec spec in form.fieldSpecs ?? System.Array.Empty<DocumentFieldSpec>())
            if (spec != null)
                doc.fields.Add(NewField(spec, inst, form));
        return doc;
    }

    /// <summary>
    /// Creates the traveller's runtime documents from the blueprint's
    /// templates issued today (DayPlanSO.TemplatesOf, lesson D7) that the
    /// traveller carries (AccountMaker.Carries: every form outside a request
    /// group, and of the proof group the one form their account holds), in
    /// paper order (null templates skipped); their fields are filled next
    /// (PopulateDocumentFields).
    /// </summary>
    private static void BuildDocuments(CaseInstance inst, DayPlanSO plan, CaseBlueprintSO blueprint)
    {
        if (inst == null || plan == null || blueprint == null)
            return;

        foreach (DocumentTemplateSO dt in plan.TemplatesOf(blueprint))
            if (AccountMaker.Carries(dt.askGroup, dt.formNumber, inst.account))
                inst.documents.Add(new DocumentInstance { template = dt });
    }
}
