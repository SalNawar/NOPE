using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Builds runtime CaseInstance objects from your data:
/// DayPlanSO -> picks the traveller's place (era by weight, nation among today's
/// places) -> identity (names and birth years of that place) -> documents whose
/// fields come from today's FactTable (one field may be forged with another
/// place's value). Every draw comes from a per-traveller seeded stream, so the
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

    /// <summary>
    /// Construct a factory over a content library and today's fact snapshot
    /// (ContentLibrarySO.BuildFactTable for the same day plan).
    /// </summary>
    public CaseFactory(ContentLibrarySO lib, FactTable facts)
    {
        _lib = lib;
        _facts = facts ?? new FactTable();
    }

    /// <summary>
    /// Generates the full list of cases for a day, based on the DayPlan.
    /// Forced cases override procedural blueprint selection per slot. Each slot
    /// draws from its own stream (Seeds.ForCase), so one traveller's draws never
    /// shift the next one's.
    /// </summary>
    public List<CaseInstance> GenerateDayCases(DayPlanSO plan, WorldState state, int daySeed)
    {
        Debug.Log($"[CaseFactory] >>> Entering GenerateDayCases (day {state?.day}, plan='{plan?.name}', daySeed={daySeed}).");

        var results = new List<CaseInstance>();

        if (plan == null || state == null || _lib == null)
        {
            Debug.LogWarning("[CaseFactory] <<< Exiting GenerateDayCases early — null plan/state/library.");
            return results;
        }

        int total = Mathf.Max(1, plan.VisitorsCount);

        // Fresh roster: names are unique within the day (records use first match).
        _roster = new NameRoster();
        _todays = _lib.TodaysProfiles(plan);

        if (_todays.Count == 0)
            Debug.LogWarning($"[CaseFactory] Day {plan.DayNumber} has no places (its eras x allowed nations match no profile). Run Tools > TimeDesk > Generate World.");

        Debug.Log($"[CaseFactory] Generating {total} case(s) for day {state.day} from {_todays.Count} place(s).");

        for (int i = 0; i < total; i++)
        {
            int caseIndex1Based = i + 1;
            _rng = new SeededRandom(Seeds.ForCase(daySeed, caseIndex1Based));
            results.Add(GenerateSingleCase(plan, state, i, caseIndex1Based));
        }

        Debug.Log($"[CaseFactory] <<< Exiting GenerateDayCases ({results.Count} case(s) generated for day {state.day}).");

        return results;
    }

    /// <summary>
    /// Generates one case:
    /// - Forced blueprint for this slot (if defined)
    /// - Maybe legendary (based on chance)
    /// - Otherwise pick the true era from day weights and a place in it
    /// - If blueprint not forced, pick from possibleBlueprints
    /// - Build documents, then fill and maybe forge their fields
    /// </summary>
    private CaseInstance GenerateSingleCase(DayPlanSO plan, WorldState state, int index0Based, int caseIndex1Based)
    {
        // 1) Forced blueprint if present.
        plan.TryGetForcedCase(caseIndex1Based, out CaseBlueprintSO forcedBlueprint);

        // 2) Legendary roll.
        LegendarySO legendary = TryRollLegendary(plan, state);

        // 3) Decide true era.
        EraSO trueEra = legendary != null ? legendary.trueEra : PickEraFromPlan(plan);

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
        NationEraProfileSO place = PickPlace(legendary, trueEra);
        NationSO nation = legendary != null && legendary.nation != null ? legendary.nation : place != null ? place.nation : null;
        string originLabel = place != null ? place.OriginLabel : FallbackOriginLabel(nation, trueEra);
        string givenName = ResolveGivenName(legendary, archetype, place, nation, caseIndex1Based);
        string role = archetype != null ? archetype.displayName : "Traveler";
        string visitorName = legendary != null ? givenName : $"{givenName} ({role})";
        string birthDate = GenerateBirthDate(place);
        string intro = legendary != null ? $"Priority arrival: {legendary.displayName}." : "Next subject for reassignment.";

        var inst = new CaseInstance
        {
            caseIndex = index0Based,
            trueEra = trueEra,
            isLegendary = legendary != null,
            legendarySource = legendary,
            archetype = archetype,
            nation = nation,
            place = place,
            originLabel = originLabel,
            visitorDisplayName = visitorName,
            visitorGivenName = givenName,
            trueBirthDate = birthDate,
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

        // 7) Investigation layer: stated claim, structured fields + forgery, daily rules.
        inst.claimedNation = nation;
        inst.claimedEra = trueEra;
        inst.claimLine = $"I request passage home to {originLabel}.";
        inst.claimAllowedByRules = plan.ClaimAllowed(nation, trueEra);
        PopulateDocumentFields(inst, blueprint, state);

        Debug.Log($"[CaseFactory] Case {caseIndex1Based}: blueprint='{blueprint.name}', place='{originLabel}', archetype='{archetype?.displayName}', legendary={legendary != null}, visitor='{visitorName}', born='{birthDate}', forged={inst.isForged}, claimAllowed={inst.claimAllowedByRules}, shouldAccept={inst.ShouldAccept}.");

        return inst;
    }

    /// <summary>Origin label when no place is authored for a nation+era (content gap).</summary>
    private static string FallbackOriginLabel(NationSO nation, EraSO era)
    {
        string where = nation != null ? nation.displayName : "an unlisted land";
        return era != null ? $"{where} ({era.displayName})" : where;
    }

    /// <summary>
    /// Fills each document's structured fields from today's facts for the
    /// case's claimed place, then (with the blueprint's contradiction chance)
    /// forges exactly one provable field with another place's value, flagging the case.
    /// </summary>
    private void PopulateDocumentFields(CaseInstance inst, CaseBlueprintSO blueprint, WorldState state)
    {
        if (inst == null || _lib == null)
            return;

        var allFields = new List<DocumentField>();

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

        if (allFields.Count == 0)
            return;

        // Chance for this case to carry a forged field (reuses the economy knobs).
        float forgeChance = Mathf.Clamp01(
            blueprint.ContradictionChance +
            (state != null ? state.forgeryChanceModifier : 0f) +
            TimelineEffects.SumFloat(state, _lib, EffectOpType.ForgeryChanceBonus));

        if (_rng.Value() >= forgeChance)
            return;

        // Forge one PROVABLE field. Place facts are provable when today's table
        // holds the claim's truth plus a different value to forge with (the
        // player can find both in the books); birth dates are provable against
        // the citizen records (which always carry the true identity). Names stay
        // honest for now — forged names pair with the future missing-record mechanic.
        string nationId = inst.claimedNation != null ? inst.claimedNation.id : null;
        string eraId = inst.claimedEra != null ? inst.claimedEra.id : null;
        var provable = new List<DocumentField>();

        foreach (DocumentField f in allFields)
        {
            if (f.category == ClueCategory.Name)
                continue;

            if (f.category == ClueCategory.BirthDate)
            {
                if (BirthDates.TryParse(inst.trueBirthDate, out _, out _, out _))
                    provable.Add(f);
                continue;
            }

            string truth = _facts.Get(nationId, eraId, f.category);
            if (string.IsNullOrEmpty(truth) || _facts.PickOtherValue(f.category, truth, n => 0) == null)
                continue;

            provable.Add(f);
        }

        if (provable.Count == 0)
        {
            Debug.LogWarning($"[CaseFactory] No provable field to forge for '{inst.originLabel}' — case stays genuine. Today's world needs at least two places with this fact.");
            return;
        }

        DocumentField target = provable[_rng.Range(0, provable.Count)];

        string wrong = target.category == ClueCategory.BirthDate
            ? BirthDates.Forge(inst.trueBirthDate, _rng)
            : _facts.PickOtherValue(target.category, _facts.Get(nationId, eraId, target.category), n => _rng.Range(0, n));

        if (!string.IsNullOrEmpty(wrong) && wrong != target.value)
        {
            target.value = wrong;
            target.isAnachronism = true;
            inst.isForged = true;
        }
    }

    /// <summary>
    /// Resolves a field's true value: identity fields come from the visitor's
    /// identity; place fields come from today's facts for the claimed place,
    /// with a readable placeholder (and a warning) when content is missing.
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
        // consistent (never a forgery) and the gap visible.
        string e = inst.claimedEra != null ? inst.claimedEra.id : "unknown";
        Debug.LogWarning($"[CaseFactory] '{inst.originLabel}' has no {category} fact today; printing a placeholder. Check the place's facts (Tools > TimeDesk > Validate Content Library).");
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
    /// uniform pick among today's places in the true era > null (no place).
    /// </summary>
    private NationEraProfileSO PickPlace(LegendarySO legendary, EraSO trueEra)
    {
        if (legendary != null && legendary.nation != null)
            return _lib.GetProfile(legendary.nation, trueEra);

        if (trueEra == null)
            return null;

        var candidates = _todays.Where(p => p.era == trueEra).ToList();
        return candidates.Count == 0 ? null : candidates[_rng.Range(0, candidates.Count)];
    }

    /// <summary>
    /// The visitor's given name (no role suffix; records lookup key), unique
    /// within the day: legendary name > the place's period names > the nation's
    /// pool > archetype name pool > generic subject.
    /// </summary>
    private string ResolveGivenName(LegendarySO legendary, ArchetypeSO archetype, NationEraProfileSO place, NationSO nation, int caseIndex1Based)
    {
        if (legendary != null)
        {
            // TryRollLegendary skips taken names, so a clash means that filter was bypassed.
            if (!_roster.Reserve(legendary.displayName))
                Debug.LogError($"[CaseFactory] Legendary '{legendary.displayName}' shares a name with an earlier visitor today; Citizen Records will return the first match.");
            return legendary.displayName;
        }

        string picked = _roster.Take(place != null ? place.AllNames : null, n => _rng.Range(0, n))
                        ?? _roster.Take(nation != null ? nation.namePool : null, n => _rng.Range(0, n))
                        ?? _roster.Take(archetype != null ? archetype.namePool : null, n => _rng.Range(0, n));
        if (picked != null)
            return picked;

        string fallback = $"Subject #{caseIndex1Based}";
        _roster.Reserve(fallback);
        return fallback;
    }

    /// <summary>A birth date within the place's birth-year range ("Unknown" when no place is authored).</summary>
    private string GenerateBirthDate(NationEraProfileSO place)
    {
        if (place == null)
            return "Unknown";

        return BirthDates.Generate(place.birthYearMin, place.birthYearMax, _rng);
    }

    /// <summary>
    /// Builds the agency's citizen master record for a day's visitors. Records
    /// always carry the TRUE identity, so forged papers can be caught against
    /// them. (Future: deliberately missing/corrupted records + family history.)
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
    /// Picks the true era by the DayPlan weights. With no weights (or bad
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
        int totalCluesTarget = _rng.Range(blueprint.TotalCluesMin, blueprint.TotalCluesMax + 1);

        // Create runtime document instances from templates.
        var docInstances = new List<DocumentInstance>();
        foreach (DocumentTemplateSO dt in blueprint.DocumentTemplates)
        {
            if (dt == null)
                continue;

            docInstances.Add(new DocumentInstance { template = dt });
        }

        // Build clue pools from the library:
        // - supporting clues for the true era
        // - contradicting clues against the true era
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

        // Effective contradiction chance: blueprint base + tomorrow modifier
        // (slot machine) + stacked ForgeryChanceBonus effects.
        float effectiveContradictionChance = Mathf.Clamp01(
            blueprint.ContradictionChance +
            state.forgeryChanceModifier +
            TimelineEffects.SumFloat(state, _lib, EffectOpType.ForgeryChanceBonus));

        // Decide counts: how many contradictions and red herrings to inject.
        int contradictions = 0;
        for (int i = 0; i < totalCluesTarget; i++)
            if (_rng.Value() < effectiveContradictionChance) contradictions++;

        int herrings = 0;
        for (int i = 0; i < totalCluesTarget; i++)
            if (_rng.Value() < blueprint.RedHerringChance) herrings++;

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
            int idx = _rng.Range(0, temp.Count);
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
            return preferred[_rng.Range(0, preferred.Count)];

        // Otherwise choose any doc that still has room.
        var any = docs.Where(d =>
            d != null &&
            d.template != null &&
            d.cluesInDoc.Count < d.template.maxClues
        ).ToList();

        if (any.Count > 0)
            return any[_rng.Range(0, any.Count)];

        // Worst case: all docs are "full" -> dump into a random doc anyway.
        return docs[_rng.Range(0, docs.Count)];
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
