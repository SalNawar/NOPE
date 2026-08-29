using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Builds runtime CaseInstance objects from your data:
/// DayPlanSO -> picks blueprint + picks true era -> selects docs -> injects clues.
/// </summary>
public sealed class CaseFactory
{
    /// <summary>Content library used as the source of eras, clues, legendaries, etc.</summary>
    private readonly ContentLibrarySO _lib;

    /// <summary>
    /// Construct a factory that uses a specific ContentLibrary as its source.
    /// </summary>
    public CaseFactory(ContentLibrarySO lib)
    {
        _lib = lib;
    }

    /// <summary>
    /// Generates the full list of cases for a day, based on the DayPlan.
    /// Forced cases override procedural blueprint selection per slot.
    /// </summary>
    public List<CaseInstance> GenerateDayCases(DayPlanSO plan, WorldState state)
    {
        Debug.Log($"[CaseFactory] >>> Entering GenerateDayCases (day {state?.day}, plan='{plan?.name}').");

        var results = new List<CaseInstance>();

        if (plan == null || state == null || _lib == null)
        {
            Debug.LogWarning("[CaseFactory] <<< Exiting GenerateDayCases early — null plan/state/library.");
            return results;
        }

        int total = Mathf.Max(1, plan.VisitorsCount);

        Debug.Log($"[CaseFactory] Generating {total} case(s) for day {state.day}.");

        for (int i = 0; i < total; i++)
        {
            int caseIndex1Based = i + 1;
            results.Add(GenerateSingleCase(plan, state, i, caseIndex1Based));
        }

        Debug.Log($"[CaseFactory] <<< Exiting GenerateDayCases ({results.Count} case(s) generated for day {state.day}).");

        return results;
    }

    /// <summary>
    /// Generates one case:
    /// - Forced blueprint for this slot (if defined)
    /// - Maybe legendary (based on chance)
    /// - Otherwise pick true era from day weights
    /// - If blueprint not forced, pick from possibleBlueprints
    /// - Build documents and inject clues
    /// </summary>
    private CaseInstance GenerateSingleCase(DayPlanSO plan, WorldState state, int index0Based, int caseIndex1Based)
    {
        // 1) Forced blueprint if present.
        CaseBlueprintSO forcedBlueprint;
        plan.TryGetForcedCase(caseIndex1Based, out forcedBlueprint);

        // 2) Legendary roll.
        LegendarySO legendary = TryRollLegendary(plan, state);

        // 3) Decide blueprint (forced > legendary override > weighted pick,
        //    with active-effect weight multipliers applied). Resolved before
        //    the era so pinned eras on the blueprint can drive the case.
        CaseBlueprintSO blueprint =
            forcedBlueprint != null ? forcedBlueprint :
            legendary != null && legendary.blueprintOverride != null ? legendary.blueprintOverride :
            WeightedRandom.Pick(plan.PossibleBlueprints, b => b != null
                ? b.Difficulty * TimelineEffects.GetBlueprintWeightMultiplier(state, _lib, b.name)
                : 0f);

        // 4) Decide true era: legendary truth > blueprint pinned pool > day weights.
        EraSO trueEra;
        if (legendary != null && legendary.trueEra != null)
        {
            trueEra = legendary.trueEra;
        }
        else if (blueprint != null && blueprint.PinnedEras != null && blueprint.PinnedEras.Length > 0)
        {
            trueEra = blueprint.PinnedEras[Random.Range(0, blueprint.PinnedEras.Length)];
        }
        else
        {
            trueEra = PickEraFromPlan(plan);
        }

        // 4.5) Timeline identity: archetype, destination nation, visitor identity.
        // Pinned fields on the blueprint override the procedural picks so a
        // forced-case anchor is exactly the visitor it was authored to be.
        ArchetypeSO archetype = PickArchetype(blueprint, legendary, state);
        NationSO nation = blueprint != null && blueprint.PinnedNation != null
            ? blueprint.PinnedNation
            : PickNation(legendary, trueEra);
        string givenName = blueprint != null && !string.IsNullOrEmpty(blueprint.PinnedGivenName)
            ? blueprint.PinnedGivenName
            : ResolveGivenName(legendary, archetype, nation, caseIndex1Based);
        string role = archetype != null ? archetype.displayName : "Traveler";
        string visitorName = legendary != null ? givenName : $"{givenName} ({role})";
        string birthDate = blueprint != null && !string.IsNullOrEmpty(blueprint.PinnedBirthDate)
            ? blueprint.PinnedBirthDate
            : GenerateBirthDate(trueEra);
        string intro = blueprint != null && !string.IsNullOrEmpty(blueprint.PinnedIntroLine)
            ? blueprint.PinnedIntroLine
            : legendary != null ? $"Priority arrival: {legendary.displayName}." : "Next subject for reassignment.";

        if (blueprint == null)
        {
            Debug.LogError($"CaseFactory generated a case with a null blueprint (Day {plan.DayNumber}, slot {caseIndex1Based}). Check DayPlanSO.possibleBlueprints / forcedCases.");
            return new CaseInstance
            {
                caseIndex = index0Based,
                trueEra = trueEra,
                isLegendary = legendary != null,
                legendarySource = legendary,
                archetype = archetype,
                nation = nation,
                visitorDisplayName = visitorName,
                visitorGivenName = givenName,
                trueBirthDate = birthDate,
                introLine = intro
            };
        }

        // 5) Build instance.
        var inst = new CaseInstance
        {
            caseIndex = index0Based,
            trueEra = trueEra,
            isLegendary = legendary != null,
            legendarySource = legendary,
            archetype = archetype,
            nation = nation,
            visitorDisplayName = visitorName,
            visitorGivenName = givenName,
            trueBirthDate = birthDate,
            introLine = intro
        };

        // 5.5) Merge authored timeline impacts (blueprint + legendary).
        if (blueprint.AuthoredImpacts != null)
            inst.authoredImpacts.AddRange(blueprint.AuthoredImpacts);

        if (legendary != null && legendary.authoredImpacts != null)
            inst.authoredImpacts.AddRange(legendary.authoredImpacts);

        // 6) Build documents + inject clues.
        BuildDocumentsAndClues(inst, trueEra, blueprint, state);

        // 7) Investigation layer: stated claim, structured fields + forgery, daily rules.
        inst.claimedNation = nation;
        inst.claimedEra = trueEra;
        inst.claimLine = BuildClaimLine(nation, trueEra);
        inst.claimAllowedByRules = plan.ClaimAllowed(nation, trueEra);
        PopulateDocumentFields(inst, blueprint, state);

        Debug.Log($"[CaseFactory] Case {caseIndex1Based}: blueprint='{blueprint.name}', trueEra='{trueEra?.id}', archetype='{archetype?.displayName}', nation='{nation?.displayName}', legendary={legendary != null}, visitor='{visitorName}', forged={inst.isForged}, claimAllowed={inst.claimAllowedByRules}, shouldAccept={inst.ShouldAccept}.");

        return inst;
    }

    /// <summary>Builds the visitor's stated travel claim line for the UI banner.</summary>
    private static string BuildClaimLine(NationSO nation, EraSO era)
    {
        string when = era != null ? era.displayName : "an unlisted era";

        return nation != null
            ? $"I request passage to {nation.displayName} during {when}."
            : $"I request passage to {when}.";
    }

    /// <summary>
    /// Fills each document's structured fields from the reference data for the
    /// case's claimed nation+era, then (with the blueprint's contradiction
    /// chance) forges exactly one field into an anachronism, flagging the case.
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

        // Authored forgery: exactly one field, exactly this value, no roll.
        if (blueprint != null && blueprint.ForceForgery)
        {
            ApplyForcedForgery(inst, blueprint, allFields);
            return;
        }

        // Chance for this case to carry a forged field (reuses the economy knobs).
        float forgeChance = Mathf.Clamp01(
            blueprint.ContradictionChance +
            (state != null ? state.forgeryChanceModifier : 0f) +
            TimelineEffects.SumFloat(state, _lib, EffectOpType.ForgeryChanceBonus));

        if (Random.value >= forgeChance)
            return;

        // Forge one PROVABLE field. Era fields are provable when the reference
        // book contains the truth for the claimed nation+era plus a different
        // value to forge with; birth dates are provable against the citizen
        // records (which always carry the true identity). Names stay honest
        // for now — forged names pair with the future missing-record mechanic.
        var provable = new List<DocumentField>();

        foreach (DocumentField f in allFields)
        {
            if (f.category == ClueCategory.Name)
                continue;

            if (f.category == ClueCategory.BirthDate)
            {
                if (!string.IsNullOrEmpty(inst.trueBirthDate))
                    provable.Add(f);
                continue;
            }

            ReferenceBookSO b = _lib.GetReferenceBook(f.category);
            if (b == null)
                continue;

            string truth = b.GetValue(inst.claimedNation, inst.claimedEra);
            if (string.IsNullOrEmpty(truth))
                continue;

            if (string.IsNullOrEmpty(b.GetAnyOtherValue(truth)))
                continue;

            provable.Add(f);
        }

        if (provable.Count == 0)
        {
            Debug.LogWarning($"[CaseFactory] No provable field to forge for claim '{inst.claimedNation?.displayName}/{inst.claimedEra?.id}' — case stays genuine. Author reference-book entries for this era to enable forgeries.");
            return;
        }

        DocumentField target = provable[Random.Range(0, provable.Count)];

        string wrong;
        if (target.category == ClueCategory.BirthDate)
        {
            wrong = ForgeBirthDate(inst.trueBirthDate);
        }
        else
        {
            ReferenceBookSO book = _lib.GetReferenceBook(target.category);
            wrong = book.GetAnyOtherValue(book.GetValue(inst.claimedNation, inst.claimedEra));
        }

        if (!string.IsNullOrEmpty(wrong) && wrong != target.value)
        {
            target.value = wrong;
            target.isAnachronism = true;
            inst.isForged = true;
        }
    }

    /// <summary>
    /// Applies a blueprint's authored forgery: finds the pinned category's
    /// field and stamps the pinned value. Fails loudly (manifesto rule) when
    /// the category is missing from the documents or the value is empty —
    /// a silent no-op would quietly turn an anchor case honest.
    /// </summary>
    private void ApplyForcedForgery(CaseInstance inst, CaseBlueprintSO blueprint, List<DocumentField> allFields)
    {
        ClueCategory category = blueprint.ForcedForgeryCategory;

        DocumentField target = null;
        foreach (DocumentField f in allFields)
        {
            if (f != null && f.category == category)
            {
                target = f;
                break;
            }
        }

        if (target == null)
        {
            Debug.LogWarning($"[CaseFactory] Blueprint '{blueprint.name}' forces a {category} forgery, but no document template presents a {category} field. Add it to a fieldSpec or the anchor silently stays honest.");
            return;
        }

        string wrong = blueprint.ForcedForgeryValue;

        if (string.IsNullOrEmpty(wrong))
        {
            if (category == ClueCategory.BirthDate)
            {
                wrong = ForgeBirthDate(inst.trueBirthDate);
            }
            else
            {
                ReferenceBookSO book = _lib.GetReferenceBook(category);
                wrong = book != null
                    ? book.GetAnyOtherValue(book.GetValue(inst.claimedNation, inst.claimedEra))
                    : null;
            }
        }

        if (string.IsNullOrEmpty(wrong))
        {
            Debug.LogWarning($"[CaseFactory] Blueprint '{blueprint.name}' forces a {category} forgery but no provable value exists for claim '{inst.claimedNation?.displayName}/{inst.claimedEra?.id}'. Author the reference-book entry (provable-only rule).");
            return;
        }

        if (wrong == target.value)
        {
            Debug.LogWarning($"[CaseFactory] Blueprint '{blueprint.name}' forged value '{wrong}' equals the honest {category} value — nothing to forge.");
            return;
        }

        target.value = wrong;
        target.isAnachronism = true;
        inst.isForged = true;
    }

    /// <summary>
    /// Resolves a field's true value: identity fields come from the visitor's
    /// identity; era fields come from the reference books for the claimed
    /// nation+era, with a readable placeholder fallback.
    /// </summary>
    private string ResolveFieldValue(ClueCategory category, CaseInstance inst)
    {
        if (category == ClueCategory.Name)
            return inst.visitorGivenName;

        if (category == ClueCategory.BirthDate)
            return inst.trueBirthDate;

        ReferenceBookSO book = _lib != null ? _lib.GetReferenceBook(category) : null;
        string value = book != null ? book.GetValue(inst.claimedNation, inst.claimedEra) : null;

        if (!string.IsNullOrEmpty(value))
            return value;

        // No authored reference: synthesize a stable placeholder so the field
        // still renders (and is internally consistent = not a forgery).
        string e = inst.claimedEra != null ? inst.claimedEra.id : "unknown";
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

        ArchetypeSO picked = WeightedRandom.Pick(pool, a => a != null
            ? Mathf.Max(0f, a.baseWeight) * TimelineEffects.GetVisitorTagWeightMultiplier(state, _lib, a.tags)
            : 0f);

        if (picked == null)
        {
            // A hand-authored pool whose weights are all zero (e.g. anchor-only
            // archetypes like the Guardian) must not silently degrade to a
            // nameless "Traveler" — fall back to a uniform pick. A single-entry
            // pool is a deliberate pin and stays quiet; larger pools warn.
            if (pool.Count > 1)
                Debug.LogWarning($"[CaseFactory] Blueprint archetype pool has {pool.Count} entries but no positive weight; falling back to a uniform pick.");

            var nonNull = new List<ArchetypeSO>();
            foreach (ArchetypeSO a in pool)
                if (a != null)
                    nonNull.Add(a);

            picked = nonNull.Count > 0 ? nonNull[Random.Range(0, nonNull.Count)] : null;
        }

        return picked;
    }

    /// <summary>
    /// Picks the destination nation: legendary override > uniform pick among
    /// authored profiles for the true era > null (era has no nations yet).
    /// </summary>
    private NationSO PickNation(LegendarySO legendary, EraSO trueEra)
    {
        if (legendary != null && legendary.nation != null)
            return legendary.nation;

        if (trueEra == null || _lib.Profiles == null)
            return null;

        var candidates = new List<NationEraProfileSO>();

        foreach (NationEraProfileSO p in _lib.Profiles)
            if (p != null && p.era == trueEra && p.nation != null)
                candidates.Add(p);

        if (candidates.Count == 0)
            return null;

        return candidates[Random.Range(0, candidates.Count)].nation;
    }

    /// <summary>
    /// Resolves the visitor display name: legendary name > nation name pool
    /// (era-appropriate) > archetype name pool > generic subject.
    /// </summary>
    /// <summary>The visitor's given name (no role suffix; records lookup key).</summary>
    private static string ResolveGivenName(LegendarySO legendary, ArchetypeSO archetype, NationSO nation, int caseIndex1Based)
    {
        if (legendary != null)
            return legendary.displayName;

        // Prefer a name themed to the visitor's nation/era.
        if (nation != null && nation.namePool != null && nation.namePool.Length > 0)
        {
            string picked = nation.namePool[Random.Range(0, nation.namePool.Length)];
            if (!string.IsNullOrWhiteSpace(picked))
                return picked;
        }

        if (archetype != null && archetype.namePool != null && archetype.namePool.Length > 0)
        {
            string picked = archetype.namePool[Random.Range(0, archetype.namePool.Length)];
            if (!string.IsNullOrWhiteSpace(picked))
                return picked;
        }

        return $"Subject #{caseIndex1Based}";
    }

    private static readonly string[] Months =
        { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

    /// <summary>
    /// A plausible birth date for the visitor's true era, drawn from the era's
    /// authored calendar (native month names, BCE epochs, plausible year span).
    /// Eras without calendar data fall back to 20th-century Gregorian. The
    /// calendar itself is the texture: an ancient Greek reads "Elaphebolion",
    /// an Egyptian reads "Choiak" — the clerk should believe the paper, not
    /// quiz it. The desktop Chrono Converter decodes the same data.
    /// </summary>
    private static string GenerateBirthDate(EraSO era)
    {
        string[] months = era != null && era.CalendarMonths.Length > 0 ? era.CalendarMonths : Months;

        int spanStart = 1900, spanEnd = 2000;
        bool bce = false;

        if (era != null && era.CalendarSpanEnd > 0)
        {
            spanStart = era.CalendarSpanStart;
            spanEnd = era.CalendarSpanEnd;
            bce = era.CalendarYearsAreBCE;
        }

        int modernYear = Random.Range(spanStart, spanEnd + 1);
        int eraYear = CalendarConverter.ToEraYear(modernYear, bce);

        return $"{Random.Range(1, 29)} {months[Random.Range(0, months.Length)]} {eraYear}";
    }

    /// <summary>
    /// Shifts a birth date's year so the forged value stays plausible but wrong.
    /// </summary>
    private static string ForgeBirthDate(string trueDate)
    {
        string[] parts = (trueDate ?? string.Empty).Split(' ');
        if (parts.Length == 3 && int.TryParse(parts[2], out int year))
        {
            int offset = Random.Range(2, 25) * (Random.value < 0.5f ? -1 : 1);
            return $"{parts[0]} {parts[1]} {year + offset}";
        }

        return trueDate + " (?)";
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

            string nation = inst.nation != null ? inst.nation.displayName : "Unregistered";
            string era = inst.trueEra != null ? inst.trueEra.displayName : "Unknown Era";

            // "Imperial Japan" already names the nation — don't read
            // "Japan — Imperial Japan" on an agency record.
            string origin = era.IndexOf(nation, System.StringComparison.OrdinalIgnoreCase) >= 0
                ? era
                : $"{nation} — {era}";

            registry.Add(new CitizenRecord
            {
                fullName = inst.visitorGivenName,
                birthDate = inst.trueBirthDate,
                origin = origin,
                note = inst.isLegendary
                    ? "Priority subject. Records sealed above your clearance."
                    : "No remarks on file."
            });
        }

        return registry;
    }

    /// <summary>
    /// Picks an era for the true destination using the DayPlan weights.
    /// If no weights are defined, falls back to library eras uniformly.
    /// </summary>
    private EraSO PickEraFromPlan(DayPlanSO plan)
    {
        // Era generation requires a non-empty library.
        if (_lib == null || _lib.Eras == null || _lib.Eras.Count == 0)
        {
            Debug.LogError("CaseFactory cannot pick an era because ContentLibrarySO has no eras assigned.");
            return null;
        }

        // If no weights are defined, pick uniformly from the library.
        if (plan.EraWeights == null || plan.EraWeights.Count == 0)
            return _lib.Eras[Random.Range(0, _lib.Eras.Count)];

        // Weighted pick; if the result is null (bad data), fall back to uniform.
        EraSO picked = WeightedRandom.Pick(plan.EraWeights, ew => ew.weight).era;
        return picked != null ? picked : _lib.Eras[Random.Range(0, _lib.Eras.Count)];
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

        // Only allow legendaries that are active within the day range.
        var valid = plan.AvailableLegendaries
            .Where(l => l != null && day >= l.minDay && day <= l.maxDay)
            .ToList();

        if (valid.Count == 0)
        {
            if (forced)
                Debug.LogWarning($"[CaseFactory] TryRollLegendary: ForceLegendaryNextCase is set but no legendary is valid for day {day} — flag left active.");

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
            if (Random.value > chance)
                return null;
        }

        // Uniform pick among valid legendaries (add weights later if needed).
        return valid[Random.Range(0, valid.Count)];
    }

    /// <summary>
    /// Creates runtime documents and fills them with clue text.
    /// This is where contradictions and red herrings are injected.
    /// </summary>
    private void BuildDocumentsAndClues(CaseInstance inst, EraSO trueEra, CaseBlueprintSO blueprint, WorldState state)
    {
        if (inst == null || trueEra == null || blueprint == null || _lib == null)
            return;

        if (blueprint.DocumentTemplates == null || blueprint.DocumentTemplates.Length == 0)
            return;

        // Decide how many total clue lines this case should contain.
        int totalCluesTarget = Random.Range(blueprint.TotalCluesMin, blueprint.TotalCluesMax + 1);

        // Create runtime document instances from templates.
        var docInstances = new List<DocumentInstance>();
        foreach (DocumentTemplateSO dt in blueprint.DocumentTemplates)
        {
            if (dt == null)
                continue;

            docInstances.Add(new DocumentInstance { template = dt });
        }

        // Validate clue library.
        if (_lib.Clues == null || _lib.Clues.Count == 0)
        {
            Debug.LogWarning("ContentLibrarySO has no clues assigned. Case documents will be empty.");
            // Still render empty documents for UI layout testing.
        }

        // Build clue pools from the library:
        // - supporting clues for the true era
        // - contradicting clues against the true era
        // - red herrings: irrelevant but plausible clues
        var clueSource = _lib.Clues != null ? _lib.Clues : System.Array.Empty<ClueSO>();

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
            if (Random.value < effectiveContradictionChance) contradictions++;

        int herrings = 0;
        for (int i = 0; i < totalCluesTarget; i++)
            if (Random.value < blueprint.RedHerringChance) herrings++;

        contradictions = Mathf.Min(contradictions, totalCluesTarget);
        herrings = Mathf.Min(herrings, totalCluesTarget - contradictions);

        int supports = totalCluesTarget - contradictions - herrings;

        // Pick clues from each pool without repeating.
        var picked = new List<ClueSO>();
        picked.AddRange(PickUnique(supportsTrueEra, supports));
        picked.AddRange(PickUnique(contradictsTrueEra, contradictions));
        picked.AddRange(PickUnique(redHerrings, herrings));

        Debug.Log($"[CaseFactory] Case {inst.caseIndex + 1}: clue selection — target={totalCluesTarget}, supports={supports}/{supportsTrueEra.Count} pool, contradicts={contradictions}/{contradictsTrueEra.Count} pool, redHerrings={herrings}/{redHerrings.Count} pool, picked={picked.Count}.");

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
    private static List<ClueSO> PickUnique(List<ClueSO> pool, int count)
    {
        var result = new List<ClueSO>();

        if (pool == null || pool.Count == 0 || count <= 0)
            return result;

        var temp = new List<ClueSO>(pool);

        for (int i = 0; i < count && temp.Count > 0; i++)
        {
            int idx = Random.Range(0, temp.Count);
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
    private static DocumentInstance PickDocForClue(List<DocumentInstance> docs, ClueSO clue)
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
            return preferred[Random.Range(0, preferred.Count)];

        // Otherwise choose any doc that still has room.
        var any = docs.Where(d =>
            d != null &&
            d.template != null &&
            d.cluesInDoc.Count < d.template.maxClues
        ).ToList();

        if (any.Count > 0)
            return any[Random.Range(0, any.Count)];

        // Worst case: all docs are "full" -> dump into a random doc anyway.
        return docs[Random.Range(0, docs.Count)];
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
