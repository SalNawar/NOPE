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
        var results = new List<CaseInstance>();

        if (plan == null || state == null || _lib == null)
            return results;

        int total = Mathf.Max(1, plan.VisitorsCount);

        for (int i = 0; i < total; i++)
        {
            int caseIndex1Based = i + 1;
            results.Add(GenerateSingleCase(plan, state, i, caseIndex1Based));
        }

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

        // 3) Decide true era.
        EraSO trueEra = legendary != null ? legendary.trueEra : PickEraFromPlan(plan);

        // 4) Decide blueprint (forced > legendary override > weighted pick,
        //    with active-effect weight multipliers applied).
        CaseBlueprintSO blueprint =
            forcedBlueprint != null ? forcedBlueprint :
            legendary != null && legendary.blueprintOverride != null ? legendary.blueprintOverride :
            WeightedRandom.Pick(plan.PossibleBlueprints, b => b != null
                ? b.Difficulty * TimelineEffects.GetBlueprintWeightMultiplier(state, _lib, b.name)
                : 0f);

        // 4.5) Timeline identity: archetype, destination nation, visitor name.
        ArchetypeSO archetype = PickArchetype(blueprint, legendary, state);
        NationSO nation = PickNation(legendary, trueEra);
        string visitorName = ResolveVisitorName(legendary, archetype, caseIndex1Based);
        string intro = legendary != null ? $"Priority arrival: {legendary.displayName}." : "Next subject for reassignment.";

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
            introLine = intro
        };

        // 5.5) Merge authored timeline impacts (blueprint + legendary).
        if (blueprint.AuthoredImpacts != null)
            inst.authoredImpacts.AddRange(blueprint.AuthoredImpacts);

        if (legendary != null && legendary.authoredImpacts != null)
            inst.authoredImpacts.AddRange(legendary.authoredImpacts);

        // 6) Build documents + inject clues.
        BuildDocumentsAndClues(inst, trueEra, blueprint, state);
        return inst;
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
            : 0f);
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
    /// Resolves the visitor display name: legendary name > archetype name pool > generic subject.
    /// </summary>
    private static string ResolveVisitorName(LegendarySO legendary, ArchetypeSO archetype, int caseIndex1Based)
    {
        if (legendary != null)
            return legendary.displayName;

        if (archetype != null && archetype.namePool != null && archetype.namePool.Length > 0)
        {
            string picked = archetype.namePool[Random.Range(0, archetype.namePool.Length)];

            if (!string.IsNullOrWhiteSpace(picked))
                return $"{picked} ({archetype.displayName})";
        }

        return archetype != null
            ? $"Subject #{caseIndex1Based} ({archetype.displayName})"
            : $"Subject #{caseIndex1Based}";
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
        if (plan.AvailableLegendaries == null || plan.AvailableLegendaries.Count == 0)
            return null;

        float chance = Mathf.Clamp01(
            plan.LegendaryBaseChance +
            state.legendaryChanceBonus +
            TimelineEffects.SumFloat(state, _lib, EffectOpType.LegendaryChanceBonus));

        // Random roll: if above chance, no legendary this case.
        if (Random.value > chance)
            return null;

        int day = state.day;

        // Only allow legendaries that are active within the day range.
        var valid = plan.AvailableLegendaries
            .Where(l => l != null && day >= l.minDay && day <= l.maxDay)
            .ToList();

        if (valid.Count == 0)
            return null;

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
        picked.AddRange(PickUnique(r