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
/// agency calendar, and their forms and registry entry print it. A 2150
/// citizen with no other fault may wear a costume error (CostumeErrors, on
/// their fault stream, Seeds.ForFaults): another place's item, the present's
/// clothes or a 2150 accessory, dressed by Looks.Compose like a dress tell.
/// </summary>
public sealed class CaseFactory
{
    /// <summary>Content library used as the source of eras, places, legendaries, etc.</summary>
    private readonly ContentLibrarySO _lib;

    /// <summary>Today's facts (the same snapshot the reference books show).</summary>
    private readonly FactTable _facts;

    /// <summary>Today's visitor names (unique per generated day; see NameRoster).</summary>
    private NameRoster _roster = new NameRoster();

    /// <summary>Today's places (eras x allowed nations, at most one Future place), in book order (TodaysWorld).</summary>
    private readonly List<NationEraProfileSO> _todays;

    /// <summary>The current traveller's random stream (reset per case).</summary>
    private IRandomSource _rng = new SeededRandom(0);

    /// <summary>The current traveller's lie stream (Seeds.ForLies), apart from <see cref="_rng"/> so lie tuning never changes who travellers are.</summary>
    private IRandomSource _lieRng = new SeededRandom(0);

    /// <summary>The current traveller's dialog stream (Seeds.ForDialog): the small-talk pick, apart from the case and lie streams.</summary>
    private IRandomSource _dialogRng = new SeededRandom(0);

    /// <summary>The current traveller's look stream (Seeds.ForLooks): gender when unknown, skin, face, hair colour.</summary>
    private IRandomSource _looksRng = new SeededRandom(0);

    /// <summary>The current slot's premade stream (Seeds.ForLegendary): the premade roll and pick.</summary>
    private IRandomSource _legendaryRng = new SeededRandom(0);

    /// <summary>The current traveller's account stream (Seeds.ForAccount): their agency numbers and dates, apart from every other stream.</summary>
    private IRandomSource _accountRng = new SeededRandom(0);

    /// <summary>The current traveller's fault stream (Seeds.ForFaults): the costume roll, its variant and its source.</summary>
    private IRandomSource _faultRng = new SeededRandom(0);

    /// <summary>Whether a garment can be looked at and compared today (a costume error, like a dress tell, needs it).</summary>
    private bool _appearanceReachable;

    /// <summary>Today's date on the agency calendar (AgencyCalendar.TryToday for the world's day); null when the agency block's first date is unreadable.</summary>
    private System.DateTime? _today;

    /// <summary>The agency numbers handed out today (a number belongs to one traveller a day, AgencyNumbers.TakeUnique).</summary>
    private HashSet<string> _agencyNumbers = new HashSet<string>();

    /// <summary>The current traveller's forms seed (Seeds.ForForms): a value their papers' serials come from, never a stream.</summary>
    private int _formsSeed;

    /// <summary>Today's tell channels: the plan's, minus Appearance where no garment can be looked at.</summary>
    private IReadOnlyList<TellChannel> _channels = System.Array.Empty<TellChannel>();

    /// <summary>Today's askable question categories; every traveller answers each (InterviewDay.AskableCategories).</summary>
    private IReadOnlyList<ClueCategory> _askable = System.Array.Empty<ClueCategory>();

    /// <summary>Today's question categories that may carry an Answer tell (day-gated questions only; InterviewDay.AnswerTellCategories).</summary>
    private IReadOnlyList<ClueCategory> _answerTellCategories = System.Array.Empty<ClueCategory>();

    /// <summary>Categories with a reference book (only these can carry a place-fact tell).</summary>
    private readonly HashSet<ClueCategory> _bookCategories;

    /// <summary>Guaranteed rule violators for the day being generated, by 1-based slot.</summary>
    private Dictionary<int, NationEraProfileSO> _violators = new Dictionary<int, NationEraProfileSO>();

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
        _bookCategories = lib != null ? lib.ReferenceBookCategories() : new HashSet<ClueCategory>();
    }

    /// <summary>
    /// Generates the full list of cases for a day, based on the DayPlan.
    /// Forced cases override procedural blueprint selection per slot, and a
    /// forced premade (not met yet this run) stands in its slot. Each slot
    /// draws from its own streams (Seeds.ForCase and its salted streams), so
    /// one traveller's draws never shift the next one's. Every traveller
    /// answers each of <paramref name="askable"/> (InterviewDay.AskableCategories);
    /// only <paramref name="answerTellCategories"/> (InterviewDay.AnswerTellCategories)
    /// may carry a spoken tell; a dress tell needs <paramref name="appearanceReachable"/>
    /// (a garment can be looked at and compared). Null lists count as empty.
    /// </summary>
    public List<CaseInstance> GenerateDayCases(DayPlanSO plan, WorldState state, int daySeed,
                                               IReadOnlyList<ClueCategory> askable, IReadOnlyList<ClueCategory> answerTellCategories,
                                               bool appearanceReachable)
    {
        Debug.Log($"[CaseFactory] >>> Entering GenerateDayCases (day {state?.day}, plan='{plan?.name}', daySeed={daySeed}).");

        var results = new List<CaseInstance>();

        if (plan == null || state == null || _lib == null)
        {
            Debug.LogWarning("[CaseFactory] <<< Exiting GenerateDayCases early — null plan/state/library.");
            return results;
        }

        int total = Mathf.Max(1, plan.VisitorsCount);
        _askable = askable ?? System.Array.Empty<ClueCategory>();
        _answerTellCategories = answerTellCategories ?? System.Array.Empty<ClueCategory>();

        // The opener and the claim are content: one warning a day when Generate World has not written them.
        InterviewLines wording = _lib.Interview;
        if (wording == null || string.IsNullOrWhiteSpace(wording.opener?.text) || string.IsNullOrWhiteSpace(wording.openerLegendary?.text))
            Debug.LogWarning($"[CaseFactory] Day {plan.DayNumber}: the content library's interview opener or legendary opener is blank, so a transcript may start with the claim. Run Tools > TimeDesk > Generate World.");
        foreach (TravellerKind kind in plan.PossibleBlueprints.Concat(plan.ForcedBlueprints).Where(b => b != null).Select(b => b.Kind).Distinct())
            if (string.IsNullOrWhiteSpace(Interview.ClaimLine(wording, kind)?.text))
                Debug.LogWarning($"[CaseFactory] Day {plan.DayNumber}: the content library has no claim line for {kind} travellers, so their banner shows the bare place label. Run Tools > TimeDesk > Generate World.");

        _channels = appearanceReachable ? plan.TellChannels : plan.TellChannels.Where(c => c != TellChannel.Appearance).ToList();
        _appearanceReachable = appearanceReachable;

        // Fresh roster: names are unique within the day (records use first
        // match). The forced premades who will stand today are reserved first,
        // which also keeps them out of the day's random roll.
        _roster = new NameRoster();
        _agencyNumbers = new HashSet<string>();
        _today = AgencyCalendar.TryToday(_lib.Agency.firstDate, state.day, out System.DateTime today) ? today : (System.DateTime?)null;
        if (_today == null)
            Debug.LogError($"[CaseFactory] Day {state.day}: the agency calendar cannot count from agency.firstDate '{_lib.Agency.firstDate}', so the displaced's numbers and dates print placeholders. Run Tools > TimeDesk > Generate World.");
        foreach (ForcedCaseSlot forced in plan.ForcedCases)
            if (forced != null && forced.legendary != null && !IsMet(state, forced.legendary) && !_roster.Reserve(forced.legendary.displayName))
                Debug.LogError($"[CaseFactory] Day {plan.DayNumber}: premade '{forced.legendary.displayName}' is forced twice, or shares a name with another forced premade. Check world_source.json days[].forced.");

        if (_todays.Count == 0)
            Debug.LogWarning($"[CaseFactory] Day {plan.DayNumber} has no places (its eras x allowed nations match no profile). Run Tools > TimeDesk > Generate World.");

        _violators = PlanViolators(plan, total, daySeed);

        Debug.Log($"[CaseFactory] Generating {total} case(s) for day {state.day} from {_todays.Count} place(s); guaranteed violators in slot(s) [{string.Join(", ", _violators.Keys)}].");

        for (int i = 0; i < total; i++)
        {
            int caseIndex1Based = i + 1;
            int caseSeed = Seeds.ForCase(daySeed, caseIndex1Based);
            _rng = new SeededRandom(caseSeed);
            _lieRng = new SeededRandom(Seeds.ForLies(caseSeed));
            _dialogRng = new SeededRandom(Seeds.ForDialog(caseSeed));
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
    /// Places one violator of each active closure in the first half of the
    /// queue (DayPlanSO.GuaranteeRuleViolators), never in a forced premade's
    /// slot, drawn from the day's own violator stream so the travellers'
    /// streams are untouched. A closure that forbids none of today's places
    /// cannot be tested and is skipped with a warning. A standing procedure
    /// (dress for the destination) plans no violator here: its costume errors
    /// come from the costume roll (the plan's phase 9 brings its first-day
    /// guarantee with the directives' makers).
    /// </summary>
    private Dictionary<int, NationEraProfileSO> PlanViolators(DayPlanSO plan, int total, int daySeed)
    {
        var violators = new Dictionary<int, NationEraProfileSO>();
        if (!plan.GuaranteeRuleViolators)
            return violators;

        var breakersPerRule = new List<List<NationEraProfileSO>>();
        foreach (TravelRuleSO rule in plan.ActiveTravelRules)
        {
            if (rule == null || !rule.IsClosure)
                continue;

            List<NationEraProfileSO> breakers = _todays.Where(p => !rule.Allows(p.nation, p.era)).ToList();
            if (breakers.Count == 0)
            {
                Debug.LogWarning($"[CaseFactory] Rule '{rule.name}' forbids none of day {plan.DayNumber}'s places, so no traveller can break it.");
                continue;
            }

            breakersPerRule.Add(breakers);
        }

        var premadeSlots = new HashSet<int>(plan.ForcedCases.Where(f => f != null && f.legendary != null).Select(f => f.caseIndex1Based));
        var rng = new SeededRandom(Seeds.ForViolators(daySeed));
        int[] slots = ViolatorSlots.Pick(total, breakersPerRule.Count, rng, premadeSlots);
        for (int i = 0; i < slots.Length; i++)
            violators[slots[i]] = breakersPerRule[i][rng.Range(0, breakersPerRule[i].Count)];

        return violators;
    }

    /// <summary>
    /// Generates one case:
    /// - Forced blueprint for this slot (if defined)
    /// - A premade: the slot's forced one (unless met), else maybe one from the pool (never on a violator's slot)
    /// - A guaranteed rule violator's place for this slot (if planned)
    /// - Otherwise pick the claimed era from day weights and a place in it
    /// - If blueprint not forced, pick from possibleBlueprints
    /// - Build documents, fill their fields from the claim, maybe disguise a liar, then compose the look
    /// </summary>
    private CaseInstance GenerateSingleCase(DayPlanSO plan, WorldState state, int index0Based, int caseIndex1Based)
    {
        // 1) Forced blueprint if present.
        plan.TryGetForcedCase(caseIndex1Based, out CaseBlueprintSO forcedBlueprint);

        // 2) A premade (forced here, or rolled from the day's pool on the premade stream).
        LegendarySO legendary = ResolvePremade(plan, state, caseIndex1Based, out bool forcedPremade);

        // 2.5) A planned rule violator stands in this slot (never a premade's: see ResolvePremade).
        _violators.TryGetValue(caseIndex1Based, out NationEraProfileSO violatorPlace);

        // 3) Decide the claimed era (the traveller's stated home and destination).
        EraSO claimedEra = legendary != null ? legendary.trueEra
            : violatorPlace != null ? violatorPlace.era
            : PickEraFromPlan(plan);

        // 4) Decide blueprint (forced > weighted pick, with active-effect
        //    weight multipliers applied).
        CaseBlueprintSO blueprint =
            forcedBlueprint != null ? forcedBlueprint :
            WeightedRandom.Pick(plan.PossibleBlueprints, b => b != null
                ? b.Difficulty * TimelineEffects.GetBlueprintWeightMultiplier(state, _lib, b.name)
                : 0f, _rng);

        // 4.5) Timeline identity: archetype, place, visitor identity.
        ArchetypeSO archetype = PickArchetype(blueprint, legendary, state);
        NationEraProfileSO place = violatorPlace != null ? violatorPlace : PickPlace(legendary, claimedEra);
        NationSO nation = legendary != null && legendary.nation != null ? legendary.nation : place != null ? place.nation : null;
        string originLabel = place != null ? PlaceLabel(place) : FallbackOriginLabel(nation, claimedEra);
        string givenName = ResolveGivenName(legendary, forcedPremade, place, caseIndex1Based);
        TravellerGender gender = legendary != null ? legendary.gender
            : place == null ? TravellerGender.Unknown
            : TravellerGenders.FromNameLists(givenName, place.maleNames, place.femaleNames);
        string role = archetype != null ? archetype.displayName : UiText.Get("case.roleUnknown");
        string visitorName = legendary != null ? givenName : $"{givenName} ({role})";
        string birthDate = legendary != null ? legendary.birthDate : GenerateBirthDate(place);
        string intro = Interview.Opener(_lib.Interview, gender, legendary != null ? legendary.displayName : null, legendary != null ? legendary.introLine : null);

        var inst = new CaseInstance
        {
            caseIndex = index0Based,
            claimedNation = nation,
            claimedEra = claimedEra,
            isLegendary = legendary != null,
            legendarySource = legendary,
            archetype = archetype,
            originLabel = originLabel,
            tongueId = place != null && place.tongue != null ? place.tongue : string.Empty,
            visitorDisplayName = visitorName,
            visitorGivenName = givenName,
            trueBirthDate = birthDate,
            gender = gender,
            introLine = intro
        };

        if (blueprint == null)
        {
            Debug.LogError($"CaseFactory generated a case with a null blueprint (Day {plan.DayNumber}, slot {caseIndex1Based}). Check DayPlanSO.possibleBlueprints / forcedCases.");
            return inst;
        }

        inst.kind = blueprint.Kind;

        // 4.8) A displaced person's agency file, on their account stream (the forms and the registry print it).
        if (inst.kind == TravellerKind.Displaced && _today != null)
            inst.displacement = AgencyNumbers.Displaced(_today.Value, _lib.Agency.displaced, _agencyNumbers, _accountRng);

        // 5) Merge authored timeline impacts (blueprint + legendary).
        if (blueprint.AuthoredImpacts != null)
            inst.authoredImpacts.AddRange(blueprint.AuthoredImpacts);

        if (legendary != null && legendary.authoredImpacts != null)
            inst.authoredImpacts.AddRange(legendary.authoredImpacts);

        // 6) Build the documents (their fields are filled below).
        BuildDocuments(inst, blueprint);

        // 7) Investigation layer: stated claim, structured fields, the lie (if any), daily rules.
        inst.claimLine = Interview.Claim(_lib.Interview, inst.kind, originLabel);
        inst.claimAllowedByRules = plan.ClaimAllowed(nation, claimedEra);
        List<DocumentField> fields = PopulateDocumentFields(inst);

        // A costume error forced from the debug panel is a planned fault: it stands in for the lie roll (K5).
        bool forcedCostume = DevToolsState.ForcedCostumeError != CostumeError.None && legendary == null && inst.claimAllowedByRules && place != null;
        LiePlan lie = forcedCostume ? null : Disguise(inst, fields, plan, blueprint, state, caseIndex1Based, place, legendary);
        AddAnswers(inst, lie);

        // Small talk: the claimed place's lines, else its era's (glue: only resolves the two lists).
        EraSO talkEra = place != null ? place.era : claimedEra;
        inst.smallTalk = Interview.PickSmallTalk(place != null ? place.smallTalk : null, talkEra != null ? talkEra.smallTalk : null, _dialogRng);

        (LookSource source, bool whole) costume = PlanCostume(inst, place, legendary, forcedCostume, plan, caseIndex1Based);
        inst.look = ComposeLook(inst, place, lie, legendary, costume, caseIndex1Based);

        string archetypeName = archetype != null ? archetype.displayName : string.Empty;
        string tells = lie != null ? string.Join(", ", lie.Tells.Select(t => $"{t}/{lie.ChannelOf(t)}")) : string.Empty;
        string look = inst.look != null ? inst.look.Describe() : "none";
        Debug.Log($"[CaseFactory] Case {caseIndex1Based}: blueprint='{blueprint.name}', place='{originLabel}', archetype='{archetypeName}', premade={(legendary != null ? legendary.id : "none")}, visitor='{visitorName}', born='{birthDate}', liar={inst.IsLiar}, home='{inst.HomeLabel}', tells=[{tells}], answers={inst.answers.Count}, gender={inst.gender}, look={look}, costume={inst.costumeFault}, claimAllowed={inst.claimAllowedByRules}, shouldAccept={inst.ShouldAccept}.");

        return inst;
    }

    /// <summary>Origin label when no place is authored for a nation+era (content gap).</summary>
    private static string FallbackOriginLabel(NationSO nation, EraSO era) =>
        OriginLabels.Format(nation != null ? nation.displayName : UiText.Get("case.unlistedLand"), era != null ? era.displayName : null);

    /// <summary>
    /// Fills each document's structured fields from today's facts for the
    /// case's claimed place (identity fields from the registered identity) and
    /// returns them in paper order. Never null: empty when there are no documents.
    /// </summary>
    private List<DocumentField> PopulateDocumentFields(CaseInstance inst)
    {
        var allFields = new List<DocumentField>();

        if (inst == null || _lib == null)
            return allFields;

        foreach (DocumentInstance doc in inst.documents)
        {
            if (doc == null || doc.template == null || doc.template.fieldSpecs == null)
                continue;

            doc.serial = FormSerials.Make(doc.template.formNumber, _formsSeed, inst.documents.IndexOf(doc));
            foreach (DocumentFieldSpec spec in doc.template.fieldSpecs)
            {
                if (spec == null)
                    continue;

                var field = new DocumentField
                {
                    category = spec.category,
                    label = string.IsNullOrEmpty(spec.label) ? spec.category.ToString() : spec.label,
                    value = ResolveFieldValue(spec.category, inst),
                    page = doc.template.form != null ? Mathf.Max(0, doc.template.form.PageOf(doc.fields.Count)) : 0
                };

                doc.fields.Add(field);
                allFields.Add(field);
            }
        }

        return allFields;
    }

    /// <summary>A place's label as today's FactTable (and so the scanner) prints it; the profile's own label when the place is not in today's table.</summary>
    private string PlaceLabel(NationEraProfileSO p) => _facts.OriginLabel(p.nation.id, p.era.id) ?? p.OriginLabel;

    /// <summary>
    /// Rolls the traveller's lie on their lie stream and applies it (Lies.Plan):
    /// a liar gets a true home among today's other places; every field of each
    /// Papers-tell category is rewritten with that home's value, while an Answer
    /// or Appearance tell leaves the papers on the cover (AddAnswers speaks an
    /// answer; ComposeLook dresses the garment). A home's dress may leak only
    /// when Looks.CanLeak holds for the claim and the traveller's gender. A
    /// premade authored as a liar lies surely, from their true place, through
    /// papers and answers only. Exempt travellers (honest premades, a claim a
    /// rule forbids, no papers) draw nothing. Returns the plan, or null when
    /// the traveller is exempt.
    /// </summary>
    private LiePlan Disguise(CaseInstance inst, List<DocumentField> fields, DayPlanSO plan, CaseBlueprintSO blueprint, WorldState state,
                             int caseIndex1Based, NationEraProfileSO place, LegendarySO legendary)
    {
        if (!Lies.MayLie(legendary != null && legendary.truePlace == null, inst.claimAllowedByRules, fields))
            return null;

        // The homes the lie may come from: today's places, or a lying premade's own true place.
        List<NationEraProfileSO> homes = _todays;
        IReadOnlyList<TellChannel> channels = _channels;
        float chance = LiarChance(blueprint, state);
        if (legendary != null)
        {
            if (!_todays.Contains(legendary.truePlace))
            {
                Debug.LogWarning($"[CaseFactory] Case {caseIndex1Based}: premade '{legendary.displayName}' is authored as a liar from '{legendary.truePlace.OriginLabel}', which is not in today's world, so they stay honest. List them only on days that include their true place.");
                return null;
            }

            homes = new List<NationEraProfileSO> { legendary.truePlace };
            channels = _channels.Where(c => c != TellChannel.Appearance).ToList();
            chance = 1f;
        }

        // The homes as the lie rules see them, in order (HomeIndex indexes both).
        LookSource claim = place != null ? SourceOf(place) : null;
        var todays = new List<HomeCandidate>(homes.Count);
        foreach (NationEraProfileSO p in homes)
            todays.Add(new HomeCandidate(p.nation.id, p.era.id, p.birthYearMin, p.birthYearMax,
                                         Looks.CanLeak(claim, SourceOf(p), inst.gender, _lib.LookRules)));

        LiePlan lie = Lies.Plan(
            chance,
            plan.TellCount,
            inst.claimedNation != null ? inst.claimedNation.id : null,
            inst.claimedEra != null ? inst.claimedEra.id : null,
            inst.trueBirthDate,
            todays,
            fields,
            _answerTellCategories,
            channels,
            _facts,
            _bookCategories,
            _lieRng);

        if (lie.Outcome == LieOutcome.NoPossibleLie)
        {
            Debug.LogWarning($"[CaseFactory] Case {caseIndex1Based}: rolled a liar, but no other place today differs from '{inst.originLabel}' in a fact its papers print, today's questions ask or its dress could leak (with a reference book), or in birth year, so the traveller stays honest. Widen the day's eras or countries, add a reference book, or allow more tell channels.");
        }
        else if (lie.Outcome == LieOutcome.Liar)
        {
            inst.trueHome = homes[lie.HomeIndex];
            inst.trueHomeLabel = PlaceLabel(inst.trueHome);
            lie.ApplyTo(fields);
        }

        return lie;
    }

    /// <summary>
    /// The costume roll (traveller types C2, K5), for a 2150 citizen with no
    /// other fault (not a premade, a liar or a closure's violator) whose
    /// garments can be looked at today; no draw otherwise. Its candidates:
    /// today's other places whose signature item can leak onto the claim
    /// (Looks.CanLeak) and whose Costume Guide row differs from the claim's
    /// (per place, C5), the present's clothes, and the kit accessories that
    /// can leak (Looks.KitSource). It draws on the fault stream
    /// (CostumeErrors.Plan) at the day's costume error chance. A costume error
    /// forced from the debug panel skips the roll and the kind check (a dev
    /// cheat for testing before 2150 citizens reach a day) and is consumed
    /// here. Sets inst.costumeFault; returns the leak source and whether it is
    /// worn whole (the present's clothes), or no source.
    /// </summary>
    private (LookSource source, bool whole) PlanCostume(CaseInstance inst, NationEraProfileSO place, LegendarySO legendary, bool forced,
                                                        DayPlanSO plan, int caseIndex1Based)
    {
        if (legendary != null || place == null || inst.IsLiar || !inst.claimAllowedByRules || !_appearanceReachable ||
            inst.gender == TravellerGender.Unknown || (!forced && !CostumeErrors.MayErr(inst.kind)))
            return (null, false);

        LookSource claim = SourceOf(place);
        List<LookSource> others = _todays
            .Where(p => p != place)
            .Select(SourceOf)
            .Where(p => Looks.CanLeak(claim, p, inst.gender, _lib.LookRules) && !DiscrepancyLog.ValuesMatch(p.CultureValue, claim.CultureValue))
            .ToList();
        LookSource present = PresentSource();
        GenderLook presentLook = present?.Wardrobe?.For(inst.gender);
        bool clothes = presentLook != null && presentLook.Signature.IsPresent && !DiscrepancyLog.ValuesMatch(present.CultureValue, claim.CultureValue);
        List<LookSource> kit = present == null ? new List<LookSource>()
            : _lib.PresentLook.Kit(inst.gender).Select(item => Looks.KitSource(present, item))
                  .Where(k => Looks.CanLeak(claim, k, inst.gender, _lib.LookRules)).ToList();

        CostumeError pinned = forced ? DevToolsState.ForcedCostumeError : CostumeError.None;
        CostumePlan costume = CostumeErrors.Plan(plan.CostumeErrorChance, forced, pinned, _lib.LookRules.costumeErrors, others.Count, clothes, kit.Count, _faultRng);
        if (forced)
        {
            Debug.Log($"[CaseFactory] ForcedCostumeError '{pinned}' consumed by case {caseIndex1Based} ({inst.kind}): {costume.Error}.");
            DevToolsState.ForcedCostumeError = CostumeError.None;
        }

        if (costume.Error == CostumeError.None)
        {
            if (costume.Rolled)
                Debug.LogWarning($"[CaseFactory] Case {caseIndex1Based}: rolled a costume error, but nothing wrong can show on '{inst.originLabel}' for a {inst.gender} traveller (no leakable item of another place today, no present's clothes or kit that differ), so they are dressed right. Check the wardrobes, the present and looks.costumeErrors.");
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
    /// The present as the look rules see it (traveller types H1): its clothes
    /// (ContentLibrarySO.PresentLook) under the present's nation token and the
    /// Future era, valued with the Culture value its wardrobe derives (as a
    /// place's). Null without a Future era or clothes. Seam: the plan's phase
    /// 6 brings TodaysWorld.Present (the leader's Future place, or the neutral
    /// present, with its row in every book); this then reads it.
    /// </summary>
    private LookSource PresentSource()
    {
        EraSO future = _lib.FutureEra;
        PlaceWardrobe wardrobe = _lib.PresentLook.wardrobe;
        if (future == null || wardrobe == null)
            return null;

        return new LookSource
        {
            NationId = PresentLook.NationToken,
            EraId = future.id,
            PlaceId = PresentLook.NationToken + "_" + future.id,
            Wardrobe = wardrobe,
            CultureValue = Looks.CultureValue(wardrobe)
        };
    }

    /// <summary>
    /// How the traveller looks: a premade's whole picture; otherwise the
    /// claimed place's layers (Looks.Compose on the look stream), with one
    /// garment of the true home for a dress tell, or the costume error's
    /// source: its signature item, or its whole look for the present's
    /// clothes. A missing place draws the minimal look and an unknown gender
    /// is drawn on the look stream, each with a warning.
    /// </summary>
    private TravellerLook ComposeLook(CaseInstance inst, NationEraProfileSO place, LiePlan lie, LegendarySO legendary,
                                      (LookSource source, bool whole) costume, int caseIndex1Based)
    {
        if (legendary != null)
            return Looks.Whole(legendary.id, place != null ? SourceOf(place) : null, _lib.LookRules);

        if (place == null)
        {
            Debug.LogWarning($"[CaseFactory] Case {caseIndex1Based}: '{inst.originLabel}' has no place today, so the traveller gets a body and head only. Run Tools > TimeDesk > Generate World.");
            return Looks.Compose(null, null, inst.gender, inst.trueBirthDate, 0, null, _lib.LookRules, _looksRng);
        }

        if (inst.gender == TravellerGender.Unknown)
            Debug.LogWarning($"[CaseFactory] Case {caseIndex1Based}: '{inst.visitorGivenName}' has no known gender (not on the place's name lists); the look draws one. Check the place's names.");

        LookSource leak = costume.source ?? (lie != null && lie.ChannelOf(Looks.EvidenceCategory) == TellChannel.Appearance && inst.trueHome != null
            ? SourceOf(inst.trueHome)
            : null);
        return Looks.Compose(SourceOf(place), leak, inst.gender, inst.trueBirthDate, place.year, place.looks, _lib.LookRules, _looksRng, costume.whole);
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
    /// value (AddAnswers). A liar's Papers tells overwrite the printed values
    /// afterwards (Disguise); an Answer tell replaces only the spoken value
    /// (Interview.Answer).
    /// </summary>
    private string ResolveFieldValue(ClueCategory category, CaseInstance inst)
    {
        switch (category)
        {
            case ClueCategory.Name: return inst.visitorGivenName;
            case ClueCategory.BirthDate: return inst.trueBirthDate;
            case ClueCategory.Destination: return inst.originLabel;
            case ClueCategory.CitizenId: return AgencyValue(inst.displacement?.Number, category);
            case ClueCategory.Incident: return AgencyValue(inst.displacement?.Incident, category);
            case ClueCategory.Expiry: return AgencyValue(inst.displacement?.ValidUntil, category);
            case ClueCategory.DepartureDate: return AgencyValue(_today != null ? AgencyCalendar.Write(_today.Value) : null, category);
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
    /// </summary>
    private NationEraProfileSO PickPlace(LegendarySO legendary, EraSO claimedEra)
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
        return candidates.Count == 0 ? null : candidates[_rng.Range(0, candidates.Count)];
    }

    /// <summary>
    /// The visitor's given name (no role suffix; records lookup key), unique
    /// within the day: premade name (a forced premade's is reserved before slot
    /// 1) > the place's period names > generic subject (only when a place has
    /// no names; the validator flags that).
    /// </summary>
    private string ResolveGivenName(LegendarySO legendary, bool forcedPremade, NationEraProfileSO place, int caseIndex1Based)
    {
        if (legendary != null)
        {
            // The roll skips taken names, so a clash means that filter was bypassed.
            if (!forcedPremade && !_roster.Reserve(legendary.displayName))
                Debug.LogError($"[CaseFactory] Premade '{legendary.displayName}' shares a name with an earlier visitor today; Citizen Records will return the first match.");
            return legendary.displayName;
        }

        string picked = _roster.Take(place != null ? place.AllNames : null, n => _rng.Range(0, n));
        if (picked != null)
            return picked;

        string fallback = UiText.Format("case.subject", caseIndex1Based);
        _roster.Reserve(fallback);
        return fallback;
    }

    /// <summary>A birth date within the place's birth-year range ("Unknown" when no place or birth years are authored).</summary>
    private string GenerateBirthDate(NationEraProfileSO place)
    {
        if (place == null || (place.birthYearMin == 0 && place.birthYearMax == 0))
            return "Unknown";

        return BirthDates.Generate(place.birthYearMin, place.birthYearMax, _rng);
    }

    /// <summary>
    /// Builds the agency's citizen master record for a day's visitors: one
    /// Displacement Registry entry each (traveller types §4.2), found by its
    /// Displacement No. or name, a group of rows under UI string labels: Name,
    /// Displacement No., Born, Origin and Incident (evidence of their
    /// categories), Found, Status ("Awaiting return") and the clerk's Note (not
    /// evidence); the number, incident and found rows only with an agency file.
    /// Records carry the registered identity: an honest traveller's, or a
    /// liar's cover (claimed origin). They never reveal a true home. (Future:
    /// deliberately missing/corrupted records + family history.)
    /// </summary>
    public static CitizenRegistry BuildRegistry(IReadOnlyList<CaseInstance> cases)
    {
        var registry = new CitizenRegistry();

        if (cases == null)
            return registry;

        foreach (CaseInstance inst in cases)
        {
            if (inst == null || string.IsNullOrWhiteSpace(inst.visitorGivenName))
                continue;

            string origin = !string.IsNullOrEmpty(inst.originLabel) ? inst.originLabel : FallbackOriginLabel(inst.claimedNation, inst.claimedEra);
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
            registry.Add(new CitizenRecord(inst.visitorGivenName, file?.Number, new[] { new RecordGroup(UiText.Get("records.group.registry"), rows) }));
        }

        return registry;
    }

    /// <summary>
    /// Picks the claimed era by the DayPlan weights; an era with no place
    /// today (the Future without a leader) is never drawn. With no weights (or
    /// bad data), picks uniformly among the eras that have a place today.
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
    /// The slot's premade (Premades.SlotSource): the forced premade when not
    /// met yet this run (<paramref name="forced"/> true); none when the forced
    /// one was met (an ordinary traveller stands there) or the slot is a
    /// planned violator's; otherwise a roll from the day's pool.
    /// </summary>
    private LegendarySO ResolvePremade(DayPlanSO plan, WorldState state, int caseIndex1Based, out bool forced)
    {
        forced = false;
        bool forcedHere = plan.TryGetForcedPremade(caseIndex1Based, out LegendarySO forcedPremade);
        bool forcedMet = forcedHere && IsMet(state, forcedPremade);

        switch (Premades.SlotSource(forcedHere, forcedMet, _violators.ContainsKey(caseIndex1Based)))
        {
            case PremadeSlot.Forced:
                forced = true;
                return forcedPremade;
            case PremadeSlot.None:
                if (forcedMet)
                    Debug.Log($"[CaseFactory] Case {caseIndex1Based}: forced premade '{forcedPremade.displayName}' was already met this run; the slot holds an ordinary traveller.");
                return null;
            default:
                return RollPremade(plan, state);
        }
    }

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

    /// <summary>True when the premade was presented earlier this run (FlagKeys.PremadeMet).</summary>
    private static bool IsMet(WorldState state, LegendarySO premade) =>
        state != null && premade != null && state.HasFlag(FlagKeys.PremadeMet(premade.id));

    /// <summary>
    /// Creates the traveller's runtime documents from the blueprint's
    /// templates, in paper order (null templates skipped); their fields are
    /// filled next (PopulateDocumentFields).
    /// </summary>
    private static void BuildDocuments(CaseInstance inst, CaseBlueprintSO blueprint)
    {
        if (inst == null || blueprint == null || blueprint.DocumentTemplates == null)
            return;

        foreach (DocumentTemplateSO dt in blueprint.DocumentTemplates)
            if (dt != null)
                inst.documents.Add(new DocumentInstance { template = dt });
    }
}
