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
/// true home's values, on the papers or in their answers (Lies) -> the
/// traveller's answers to today's questions, the desk's opener and the claim
/// sentence (the content library's interview wording) and a small-talk line.
/// Every draw comes from seeded streams (per traveller: the case, legacy
/// clue, lie and dialog streams; plus the day's rule-violator stream), so the
/// same run and day always produce the same travellers.
/// </summary>
public sealed class CaseFactory
{
    /// <summary>Content library used as the source of eras, places, legendaries, etc.</summary>
    private readonly ContentLibrarySO _lib;

    /// <summary>Today's facts (the same snapshot the reference books show).</summary>
    private readonly FactTable _facts;

    /// <summary>Today's visitor names (unique per generated day; see NameRoster).</summary>
    private NameRoster _roster = new NameRoster();

    /// <summary>Today's places (eras x allowed nations), in book order.</summary>
    private List<NationEraProfileSO> _todays = new List<NationEraProfileSO>();

    /// <summary>The current traveller's random stream (reset per case).</summary>
    private IRandomSource _rng = new SeededRandom(0);

    /// <summary>The current traveller's legacy clue stream, apart from <see cref="_rng"/> so clue settings never change who lies.</summary>
    private IRandomSource _clueRng = new SeededRandom(0);

    /// <summary>The current traveller's lie stream (Seeds.ForLies), apart from <see cref="_rng"/> so lie tuning never changes who travellers are.</summary>
    private IRandomSource _lieRng = new SeededRandom(0);

    /// <summary>The current traveller's dialog stream (Seeds.ForDialog): the small-talk pick, apart from the case and lie streams.</summary>
    private IRandomSource _dialogRng = new SeededRandom(0);

    /// <summary>Today's askable question categories; every traveller answers each (InterviewDay.AskableCategories).</summary>
    private IReadOnlyList<ClueCategory> _askable = System.Array.Empty<ClueCategory>();

    /// <summary>Today's question categories that may carry an Answer tell (day-gated questions only; InterviewDay.AnswerTellCategories).</summary>
    private IReadOnlyList<ClueCategory> _answerTellCategories = System.Array.Empty<ClueCategory>();

    /// <summary>Categories with a reference book (only these can carry a place-fact tell).</summary>
    private readonly HashSet<ClueCategory> _bookCategories;

    /// <summary>Guaranteed rule violators for the day being generated, by 1-based slot.</summary>
    private Dictionary<int, NationEraProfileSO> _violators = new Dictionary<int, NationEraProfileSO>();

    /// <summary>
    /// Construct a factory over a content library and today's fact snapshot
    /// (ContentLibrarySO.BuildFactTable for the same day plan).
    /// </summary>
    public CaseFactory(ContentLibrarySO lib, FactTable facts)
    {
        _lib = lib;
        _facts = facts ?? new FactTable();
        _bookCategories = lib != null ? lib.ReferenceBookCategories() : new HashSet<ClueCategory>();
    }

    /// <summary>
    /// Generates the full list of cases for a day, based on the DayPlan.
    /// Forced cases override procedural blueprint selection per slot. Each slot
    /// draws from its own stream (Seeds.ForCase), so one traveller's draws never
    /// shift the next one's. Every traveller answers each of
    /// <paramref name="askable"/> (InterviewDay.AskableCategories); only
    /// <paramref name="answerTellCategories"/> (InterviewDay.AnswerTellCategories)
    /// may carry a spoken tell. Null lists count as empty.
    /// </summary>
    public List<CaseInstance> GenerateDayCases(DayPlanSO plan, WorldState state, int daySeed,
                                               IReadOnlyList<ClueCategory> askable, IReadOnlyList<ClueCategory> answerTellCategories)
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
        if (wording == null || string.IsNullOrWhiteSpace(wording.claim?.text))
            Debug.LogWarning($"[CaseFactory] Day {plan.DayNumber}: the content library has no interview claim line, so the banner shows the bare place label. Run Tools > TimeDesk > Generate World.");

        // Fresh roster: names are unique within the day (records use first match).
        _roster = new NameRoster();
        _todays = _lib.TodaysProfiles(plan);

        if (_todays.Count == 0)
            Debug.LogWarning($"[CaseFactory] Day {plan.DayNumber} has no places (its eras x allowed nations match no profile). Run Tools > TimeDesk > Generate World.");

        _violators = PlanViolators(plan, total, daySeed);

        Debug.Log($"[CaseFactory] Generating {total} case(s) for day {state.day} from {_todays.Count} place(s); guaranteed violators in slot(s) [{string.Join(", ", _violators.Keys)}].");

        for (int i = 0; i < total; i++)
        {
            int caseIndex1Based = i + 1;
            int caseSeed = Seeds.ForCase(daySeed, caseIndex1Based);
            _rng = new SeededRandom(caseSeed);
            _clueRng = new SeededRandom(Seeds.ForClues(caseSeed));
            _lieRng = new SeededRandom(Seeds.ForLies(caseSeed));
            _dialogRng = new SeededRandom(Seeds.ForDialog(caseSeed));
            results.Add(GenerateSingleCase(plan, state, i, caseIndex1Based));
        }

        Debug.Log($"[CaseFactory] <<< Exiting GenerateDayCases ({results.Count} case(s) generated for day {state.day}).");

        return results;
    }

    /// <summary>
    /// Places one violator of each active rule in the first half of the queue
    /// (DayPlanSO.GuaranteeRuleViolators), drawn from the day's own violator
    /// stream so the travellers' streams are untouched. A rule that forbids
    /// none of today's places cannot be tested and is skipped with a warning.
    /// </summary>
    private Dictionary<int, NationEraProfileSO> PlanViolators(DayPlanSO plan, int total, int daySeed)
    {
        var violators = new Dictionary<int, NationEraProfileSO>();
        if (!plan.GuaranteeRuleViolators)
            return violators;

        var breakersPerRule = new List<List<NationEraProfileSO>>();
        foreach (TravelRuleSO rule in plan.ActiveTravelRules)
        {
            if (rule == null)
                continue;

            List<NationEraProfileSO> breakers = _todays.Where(p => !rule.Allows(p.nation, p.era)).ToList();
            if (breakers.Count == 0)
            {
                Debug.LogWarning($"[CaseFactory] Rule '{rule.name}' forbids none of day {plan.DayNumber}'s places, so no traveller can break it.");
                continue;
            }

            breakersPerRule.Add(breakers);
        }

        var rng = new SeededRandom(Seeds.ForViolators(daySeed));
        int[] slots = ViolatorSlots.Pick(total, breakersPerRule.Count, rng);
        for (int i = 0; i < slots.Length; i++)
            violators[slots[i]] = breakersPerRule[i][rng.Range(0, breakersPerRule[i].Count)];

        return violators;
    }

    /// <summary>
    /// Generates one case:
    /// - Forced blueprint for this slot (if defined)
    /// - Maybe legendary (based on chance)
    /// - A guaranteed rule violator's place for this slot (if planned)
    /// - Otherwise pick the claimed era from day weights and a place in it
    /// - If blueprint not forced, pick from possibleBlueprints
    /// - Build documents, fill their fields from the claim, then maybe disguise a liar
    /// </summary>
    private CaseInstance GenerateSingleCase(DayPlanSO plan, WorldState state, int index0Based, int caseIndex1Based)
    {
        // 1) Forced blueprint if present.
        plan.TryGetForcedCase(caseIndex1Based, out CaseBlueprintSO forcedBlueprint);

        // 2) Legendary roll.
        LegendarySO legendary = TryRollLegendary(plan, state);

        // 2.5) A planned rule violator stands in this slot (a legendary keeps its own place).
        NationEraProfileSO violatorPlace = null;
        if (legendary == null)
            _violators.TryGetValue(caseIndex1Based, out violatorPlace);

        // 3) Decide the claimed era (the traveller's stated home and destination).
        EraSO trueEra = legendary != null ? legendary.trueEra
            : violatorPlace != null ? violatorPlace.era
            : PickEraFromPlan(plan);

        // 4) Decide blueprint (forced > legendary override > weighted pick,
        //    with active-effect weight multipliers applied).
        CaseBlueprintSO blueprint =
            forcedBlueprint != null ? forcedBlueprint :
            legendary != null && legendary.blueprintOverride != null ? legendary.blueprintOverride :
            WeightedRandom.Pick(plan.PossibleBlueprints, b => b != null
                ? b.Difficulty * TimelineEffects.GetBlueprintWeightMultiplier(state, _lib, b.name)
                : 0f, _rng);

        // 4.5) Timeline identity: archetype, place, visitor identity.
        ArchetypeSO archetype = PickArchetype(blueprint, legendary, state);
        NationEraProfileSO place = violatorPlace != null ? violatorPlace : PickPlace(legendary, trueEra);
        NationSO nation = legendary != null && legendary.nation != null ? legendary.nation : place != null ? place.nation : null;
        string originLabel = place != null ? PlaceLabel(place) : FallbackOriginLabel(nation, trueEra);
        string givenName = ResolveGivenName(legendary, place, caseIndex1Based);
        TravellerGender gender = legendary != null || place == null
            ? TravellerGender.Unknown
            : TravellerGenders.FromNameLists(givenName, place.maleNames, place.femaleNames);
        string role = archetype != null ? archetype.displayName : "Traveler";
        string visitorName = legendary != null ? givenName : $"{givenName} ({role})";
        string birthDate = GenerateBirthDate(place);
        string intro = Interview.Opener(_lib.Interview, gender, legendary != null ? legendary.displayName : null, null);

        var inst = new CaseInstance
        {
            caseIndex = index0Based,
            trueEra = trueEra,
            isLegendary = legendary != null,
            legendarySource = legendary,
            archetype = archetype,
            nation = nation,
            originLabel = originLabel,
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

        // 5) Merge authored timeline impacts (blueprint + legendary).
        if (blueprint.AuthoredImpacts != null)
            inst.authoredImpacts.AddRange(blueprint.AuthoredImpacts);

        if (legendary != null && legendary.authoredImpacts != null)
            inst.authoredImpacts.AddRange(legendary.authoredImpacts);

        // 6) Build documents + inject (legacy) clues.
        BuildDocumentsAndClues(inst, trueEra, blueprint, state);

        // 7) Investigation layer: stated claim, structured fields, the lie (if any), daily rules.
        inst.claimedNation = nation;
        inst.claimedEra = trueEra;
        inst.claimLine = Interview.Claim(_lib.Interview, originLabel);
        inst.claimAllowedByRules = plan.ClaimAllowed(nation, trueEra);
        List<DocumentField> fields = PopulateDocumentFields(inst);
        LiePlan lie = Disguise(inst, fields, plan, blueprint, state, caseIndex1Based);
        AddAnswers(inst, lie);

        // Small talk: the claimed place's lines, else its era's (glue: only resolves the two lists).
        EraSO talkEra = place != null ? place.era : trueEra;
        inst.smallTalk = Interview.PickSmallTalk(place != null ? place.smallTalk : null, talkEra != null ? talkEra.smallTalk : null, _dialogRng);

        string archetypeName = archetype != null ? archetype.displayName : string.Empty;
        string tells = lie != null ? string.Join(", ", lie.Tells.Select(t => $"{t}/{lie.ChannelOf(t)}")) : string.Empty;
        Debug.Log($"[CaseFactory] Case {caseIndex1Based}: blueprint='{blueprint.name}', place='{originLabel}', archetype='{archetypeName}', legendary={legendary != null}, visitor='{visitorName}', born='{birthDate}', liar={inst.IsLiar}, home='{inst.HomeLabel}', tells=[{tells}], answers={inst.answers.Count}, gender={inst.gender}, claimAllowed={inst.claimAllowedByRules}, shouldAccept={inst.ShouldAccept}.");

        return inst;
    }

    /// <summary>Origin label when no place is authored for a nation+era (content gap).</summary>
    private static string FallbackOriginLabel(NationSO nation, EraSO era) =>
        OriginLabels.Format(nation != null ? nation.displayName : "an unlisted land", era != null ? era.displayName : null);

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

            foreach (DocumentFieldSpec spec in doc.template.fieldSpecs)
            {
                if (spec == null)
                    continue;

                var field = new DocumentField
                {
                    category = spec.category,
                    label = string.IsNullOrEmpty(spec.label) ? spec.category.ToString() : spec.label,
                    value = ResolveFieldValue(spec.category, inst),
                    page = Mathf.Max(0, spec.page)
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
    /// Papers-tell category is rewritten with that home's value, and an Answer
    /// tell leaves the papers on the cover (AddAnswers speaks it). Exempt
    /// travellers (legendaries, a claim a rule forbids, no papers) draw
    /// nothing. Returns the plan, or null when the traveller is exempt.
    /// </summary>
    private LiePlan Disguise(CaseInstance inst, List<DocumentField> fields, DayPlanSO plan, CaseBlueprintSO blueprint, WorldState state, int caseIndex1Based)
    {
        if (!Lies.MayLie(inst.isLegendary, inst.claimAllowedByRules, fields))
            return null;

        // Today's places as the lie rules see them, in _todays order (HomeIndex indexes both).
        var todays = new List<HomeCandidate>(_todays.Count);
        foreach (NationEraProfileSO p in _todays)
            todays.Add(new HomeCandidate(p.nation.id, p.era.id, p.birthYearMin, p.birthYearMax));

        LiePlan lie = Lies.Plan(
            LiarChance(blueprint, state),
            plan.TellCount,
            inst.claimedNation != null ? inst.claimedNation.id : null,
            inst.claimedEra != null ? inst.claimedEra.id : null,
            inst.trueBirthDate,
            todays,
            fields,
            _answerTellCategories,
            plan.TellChannels,
            _facts,
            _bookCategories,
            _lieRng);

        if (lie.Outcome == LieOutcome.NoPossibleLie)
        {
            Debug.LogWarning($"[CaseFactory] Case {caseIndex1Based}: rolled a liar, but no other place today can carry a provable tell against '{inst.originLabel}' (no book-covered fact that its papers print or today's day-gated questions ask, that differs from the claim's and belongs to that place alone, and no birth year other than the record's), so the traveller stays honest. Widen the day's eras or countries, add a reference book, allow more tell channels, or give places that share a fact value distinct values.");
        }
        else if (lie.Outcome == LieOutcome.Liar)
        {
            inst.trueHome = _todays[lie.HomeIndex];
            inst.trueHomeLabel = PlaceLabel(inst.trueHome);
            lie.ApplyTo(fields);
        }

        return lie;
    }

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
    /// to 0..1. The legacy clue path reads the same knob as its per-clue
    /// contradiction chance.
    /// </summary>
    private float LiarChance(CaseBlueprintSO blueprint, WorldState state) =>
        Mathf.Clamp01(
            blueprint.ContradictionChance +
            (state != null ? state.forgeryChanceModifier : 0f) +
            TimelineEffects.SumFloat(state, _lib, EffectOpType.ForgeryChanceBonus));

    /// <summary>
    /// Resolves a field's value for the claim: identity fields come from the
    /// registered identity (a liar's cover); place fields come from today's
    /// facts for the claimed place, with a readable placeholder (and a warning)
    /// when content is missing; also each spoken answer's cover value
    /// (AddAnswers). A liar's Papers tells overwrite the printed values
    /// afterwards (Disguise); an Answer tell replaces only the spoken value
    /// (Interview.Answer).
    /// </summary>
    private string ResolveFieldValue(ClueCategory category, CaseInstance inst)
    {
        if (category == ClueCategory.Name)
            return inst.visitorGivenName;

        if (category == ClueCategory.BirthDate)
            return inst.trueBirthDate;

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

    /// <summary>
    /// Picks the visitor archetype: legendary override > blueprint pool > library pool.
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
    /// Picks the traveller's place: the legendary's own place (if authored) >
    /// uniform pick among today's places in the claimed era > null (no place).
    /// </summary>
    private NationEraProfileSO PickPlace(LegendarySO legendary, EraSO trueEra)
    {
        if (legendary != null && legendary.nation != null)
        {
            NationEraProfileSO own = _lib.GetProfile(legendary.nation, trueEra);
            if (own != null && !_todays.Contains(own))
                Debug.LogWarning($"[CaseFactory] Legendary '{legendary.displayName}' comes from '{own.OriginLabel}', which is not in today's world, so their papers print placeholders. List them only on days that include their place.");
            return own;
        }

        if (trueEra == null)
            return null;

        var candidates = _todays.Where(p => p.era == trueEra).ToList();
        return candidates.Count == 0 ? null : candidates[_rng.Range(0, candidates.Count)];
    }

    /// <summary>
    /// The visitor's given name (no role suffix; records lookup key), unique
    /// within the day: legendary name > the place's period names > generic
    /// subject (only when a place has no names; the validator flags that).
    /// </summary>
    private string ResolveGivenName(LegendarySO legendary, NationEraProfileSO place, int caseIndex1Based)
    {
        if (legendary != null)
        {
            // TryRollLegendary skips taken names, so a clash means that filter was bypassed.
            if (!_roster.Reserve(legendary.displayName))
                Debug.LogError($"[CaseFactory] Legendary '{legendary.displayName}' shares a name with an earlier visitor today; Citizen Records will return the first match.");
            return legendary.displayName;
        }

        string picked = _roster.Take(place != null ? place.AllNames : null, n => _rng.Range(0, n));
        if (picked != null)
            return picked;

        string fallback = $"Subject #{caseIndex1Based}";
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
    /// Builds the agency's citizen master record for a day's visitors. Records
    /// carry the registered identity: an honest traveller's, or a liar's cover
    /// (claimed origin). They never reveal a true home. (Future: deliberately
    /// missing/corrupted records + family history.)
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

            registry.Add(new CitizenRecord
            {
                fullName = inst.visitorGivenName,
                birthDate = inst.trueBirthDate,
                origin = !string.IsNullOrEmpty(inst.originLabel) ? inst.originLabel : FallbackOriginLabel(inst.nation, inst.trueEra),
                note = inst.isLegendary
                    ? "Priority subject. Records sealed above your clearance."
                    : "No remarks on file."
            });
        }

        return registry;
    }

    /// <summary>
    /// Picks the claimed era by the DayPlan weights. With no weights (or bad
    /// data), picks uniformly among the eras that have a place today.
    /// </summary>
    private EraSO PickEraFromPlan(DayPlanSO plan)
    {
        if (plan.EraWeights != null && plan.EraWeights.Count > 0)
        {
            EraSO picked = WeightedRandom.Pick(plan.EraWeights, ew => ew.weight, _rng).era;
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
    /// Rolls for a legendary encounter:
    /// - Uses DayPlan.legendaryBaseChance + WorldState.legendaryChanceBonus
    /// - Filters to legendaries allowed for the current day
    /// - Returns one legendary or null
    /// </summary>
    private LegendarySO TryRollLegendary(DayPlanSO plan, WorldState state)
    {
        bool forced = DevToolsState.ForceLegendaryNextCase;

        if (plan.AvailableLegendaries == null || plan.AvailableLegendaries.Count == 0)
        {
            if (forced)
                Debug.LogWarning("[CaseFactory] TryRollLegendary: ForceLegendaryNextCase is set but this day has no AvailableLegendaries — flag left active for a later day.");

            return null;
        }

        int day = state.day;

        // Only allow legendaries that are active within the day range and whose
        // name nobody has today (a repeat would duplicate a citizen record).
        var valid = plan.AvailableLegendaries
            .Where(l => l != null && day >= l.minDay && day <= l.maxDay && !_roster.IsTaken(l.displayName))
            .ToList();

        if (valid.Count == 0)
        {
            if (forced)
                Debug.LogWarning($"[CaseFactory] TryRollLegendary: ForceLegendaryNextCase is set but no unused legendary is valid for day {day} — flag left active.");

            return null;
        }

        if (forced)
        {
            Debug.Log($"[CaseFactory] TryRollLegendary: ForceLegendaryNextCase consumed (day {day}, {valid.Count} candidate(s)).");
            DevToolsState.ForceLegendaryNextCase = false;
        }
        else
        {
            float chance = Mathf.Clamp01(
                plan.LegendaryBaseChance +
                state.legendaryChanceBonus +
                TimelineEffects.SumFloat(state, _lib, EffectOpType.LegendaryChanceBonus));

            // Random roll: if above chance, no legendary this case.
            if (_rng.Value() >= chance)
                return null;
        }

        // Uniform pick among valid legendaries (add weights later if needed).
        return valid[_rng.Range(0, valid.Count)];
    }

    /// <summary>
    /// Creates runtime documents and fills them with (legacy) clue text.
    /// This is where contradictions and red herrings are injected.
    /// </summary>
    private void BuildDocumentsAndClues(CaseInstance inst, EraSO trueEra, CaseBlueprintSO blueprint, WorldState state)
    {
        if (inst == null || trueEra == null || blueprint == null || _lib == null)
            return;

        if (blueprint.DocumentTemplates == null || blueprint.DocumentTemplates.Length == 0)
            return;

        // Decide how many total clue lines this case should contain.
        int totalCluesTarget = _clueRng.Range(blueprint.TotalCluesMin, blueprint.TotalCluesMax + 1);

        // Create runtime document instances from templates.
        var docInstances = new List<DocumentInstance>();
        foreach (DocumentTemplateSO dt in blueprint.DocumentTemplates)
        {
            if (dt == null)
                continue;

            docInstances.Add(new DocumentInstance { template = dt });
        }

        // Build clue pools from the library:
        // - supporting clues for the claimed era
        // - contradicting clues against the claimed era
        // - red herrings: irrelevant but plausible clues
        IReadOnlyList<ClueSO> clueSource = _lib.Clues != null ? _lib.Clues : System.Array.Empty<ClueSO>();

        var supportsTrueEra = clueSource.Where(c =>
            c != null &&
            c.supports != null &&
            c.supports.Contains(trueEra) &&
            IsClueAllowedByUpgrades(c, state)).ToList();

        var contradictsTrueEra = clueSource.Where(c =>
            c != null &&
            c.contradicts != null &&
            c.contradicts.Contains(trueEra) &&
            IsClueAllowedByUpgrades(c, state)).ToList();

        var redHerrings = clueSource.Where(c =>
            c != null &&
            IsClueAllowedByUpgrades(c, state) &&
            (c.supports == null || !c.supports.Contains(trueEra)) &&
            (c.contradicts == null || !c.contradicts.Contains(trueEra))
        ).ToList();

        // Effective contradiction chance: the same knob as the liar chance
        // (blueprint base + tomorrow modifier + stacked ForgeryChanceBonus effects).
        float effectiveContradictionChance = LiarChance(blueprint, state);

        // Decide counts: how many contradictions and red herrings to inject.
        int contradictions = 0;
        for (int i = 0; i < totalCluesTarget; i++)
            if (_clueRng.Value() < effectiveContradictionChance) contradictions++;

        int herrings = 0;
        for (int i = 0; i < totalCluesTarget; i++)
            if (_clueRng.Value() < blueprint.RedHerringChance) herrings++;

        contradictions = Mathf.Min(contradictions, totalCluesTarget);
        herrings = Mathf.Min(herrings, totalCluesTarget - contradictions);

        int supports = totalCluesTarget - contradictions - herrings;

        // Pick clues from each pool without repeating.
        var picked = new List<ClueSO>();
        picked.AddRange(PickUnique(supportsTrueEra, supports));
        picked.AddRange(PickUnique(contradictsTrueEra, contradictions));
        picked.AddRange(PickUnique(redHerrings, herrings));

        // Store global clue list on the case.
        inst.usedClues.AddRange(picked);

        // Distribute each clue into an appropriate document.
        foreach (ClueSO clue in picked)
        {
            DocumentInstance doc = PickDocForClue(docInstances, clue);
            if (doc == null)
                continue;

            doc.cluesInDoc.Add(clue);
        }

        // Render simple text for each document (prototype-friendly).
        foreach (DocumentInstance doc in docInstances)
        {
            doc.renderedText = RenderDocText(doc);
            inst.documents.Add(doc);
        }
    }

    /// <summary>
    /// Determines whether a clue is allowed to appear based on unlocked upgrades.
    /// If a clue requires an upgrade (e.g., scanner), it won't be generated until unlocked.
    /// </summary>
    private static bool IsClueAllowedByUpgrades(ClueSO clue, WorldState state)
    {
        // If no upgrade is required, the clue is always eligible.
        if (clue.requiresUpgradeToReveal == null)
            return true;

        // If state is missing, treat gated clues as unavailable.
        if (state == null)
            return false;

        return state.unlockedUpgradeIds.Contains(clue.requiresUpgradeToReveal.id);
    }

    /// <summary>
    /// Randomly picks up to 'count' unique items from a pool.
    /// </summary>
    private List<ClueSO> PickUnique(List<ClueSO> pool, int count)
    {
        var result = new List<ClueSO>();

        if (pool == null || pool.Count == 0 || count <= 0)
            return result;

        var temp = new List<ClueSO>(pool);

        for (int i = 0; i < count && temp.Count > 0; i++)
        {
            int idx = _clueRng.Range(0, temp.Count);
            result.Add(temp[idx]);
            temp.RemoveAt(idx);
        }

        return result;
    }

    /// <summary>
    /// Chooses which document should contain a given clue.
    /// Prefers templates whose preferredCategories include the clue’s category,
    /// and respects each template’s maxClues limit when possible.
    /// </summary>
    private DocumentInstance PickDocForClue(List<DocumentInstance> docs, ClueSO clue)
    {
        if (docs == null || docs.Count == 0 || clue == null)
            return null;

        // Prefer docs that want this clue category and have room.
        var preferred = docs.Where(d =>
            d != null &&
            d.template != null &&
            d.template.preferredCategories != null &&
            d.template.preferredCategories.Contains(clue.category) &&
            d.cluesInDoc.Count < d.template.maxClues
        ).ToList();

        if (preferred.Count > 0)
            return preferred[_clueRng.Range(0, preferred.Count)];

        // Otherwise choose any doc that still has room.
        var any = docs.Where(d =>
            d != null &&
            d.template != null &&
            d.cluesInDoc.Count < d.template.maxClues
        ).ToList();

        if (any.Count > 0)
            return any[_clueRng.Range(0, any.Count)];

        // Worst case: all docs are "full" -> dump into a random doc anyway.
        return docs[_clueRng.Range(0, docs.Count)];
    }

    /// <summary>
    /// Produces a simple, readable document string from its clues.
    /// This keeps the prototype UI trivial (just show a block of text).
    /// </summary>
    private static string RenderDocText(DocumentInstance doc)
    {
        string header = doc.template != null ? doc.template.displayName : "Document";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine(header);
        sb.AppendLine("----------------");

        foreach (ClueSO clue in doc.cluesInDoc)
            sb.AppendLine("• " + clue.text);

        return sb.ToString();
    }
}
