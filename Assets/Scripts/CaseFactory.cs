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

        // 4) Decide blueprint (forced > legendary override > weighted pick).
        CaseBlueprintSO blueprint =
            forcedBlueprint != null ? forcedBlueprint :
            legendary != null && legendary.blueprintOverride != null ? legendary.blueprintOverride :
            WeightedRandom.Pick(plan.PossibleBlueprints, b => b != null ? b.Difficulty : 0f);

                if (blueprint == null)
        {
            Debug.LogError($"CaseFactory generated a case with a null blueprint (Day {plan.DayNumber}, slot {caseIndex1Based}). Check DayPlanSO.possibleBlueprints / forcedCases.");
            return new CaseInstance
            {
                caseIndex = index0Based,
                trueEra = trueEra,
                isLegendary = legendary != null,
                legendarySource = legendary,
                visitorDisplayName = legendary != null ? legendary.displayName : $"Subject #{caseIndex1Based}",
                introLine = legendary != null ? $"Priority arrival: {legendary.displayName}." : "Next subject for reassignment."
            };
        }

// 5) Build instance.
        var inst = new CaseInstance
        {
            caseIndex = index0Based,
            trueEra = trueEra,
            isLegendary = legendary != null,
            legendarySource = legendary,
            visitorDisplayName = legendary != null ? legendary.displayName : $"Subject #{caseIndex1Based}",
            introLine = legendary != null ? $"Priority arrival: {legendary.displayName}." : "Next subject for reassignment."
        };

        // 6) Build documents + inject clues.
        BuildDocumentsAndClues(inst, trueEra, blueprint, state);
        return inst;
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

        float chance = Mathf.Clamp01(plan.LegendaryBaseChance + state.legendaryChanceBonus);

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

        // Decide counts: how many contradictions and red herrings to inject.
        int contradictions = 0;
        for (int i = 0; i < totalCluesTarget; i++)
            if (Random.value < blueprint.ContradictionChance) contradictions++;

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
