using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Phase 6 editor tool: scans every ContentLibrarySO asset in the project and
/// reports data issues to the console — null array entries, duplicate or
/// missing IDs, duplicate day numbers, and dangling cross-references.
/// Access via Tools &gt; TimeDesk &gt; Validate Content Library.
/// </summary>
public static class ContentLibraryValidator
{
    /// <summary>Runs validation across all ContentLibrarySO assets in the project.</summary>
    [MenuItem("Tools/TimeDesk/Validate Content Library")]
    public static void Validate()
    {
        Debug.Log("[ContentLibraryValidator] >>> Entering Validate.");

        string[] guids = AssetDatabase.FindAssets("t:ContentLibrarySO");

        if (guids.Length == 0)
        {
            Debug.LogWarning("[ContentLibraryValidator] <<< Exiting Validate — no ContentLibrarySO assets found in the project.");
            return;
        }

        int totalIssues = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var lib = AssetDatabase.LoadAssetAtPath<ContentLibrarySO>(path);

            if (lib == null)
                continue;

            Debug.Log($"[ContentLibraryValidator] Validating '{path}'...");
            totalIssues += ValidateLibrary(lib);
        }

        if (totalIssues == 0)
            Debug.Log("[ContentLibraryValidator] <<< Exiting Validate — no issues found.");
        else
            Debug.LogWarning($"[ContentLibraryValidator] <<< Exiting Validate — {totalIssues} issue(s) found (see warnings/errors above).");
    }

    /// <summary>Runs every check against a single library asset and returns the issue count.</summary>
    private static int ValidateLibrary(ContentLibrarySO lib)
    {
        int issues = 0;

        // --- Null entries in every authored array ---
        issues += CheckNullEntries(lib.DayPlans, "DayPlans", lib);
        issues += CheckNullEntries(lib.Eras, "Eras", lib);
        issues += CheckNullEntries(lib.Clues, "Clues", lib);
        issues += CheckNullEntries(lib.Legendaries, "Legendaries", lib);
        issues += CheckNullEntries(lib.Effects, "Effects", lib);
        issues += CheckNullEntries(lib.Upgrades, "Upgrades", lib);
        issues += CheckNullEntries(lib.Attributes, "Attributes", lib);
        issues += CheckNullEntries(lib.Nations, "Nations", lib);
        issues += CheckNullEntries(lib.Profiles, "Profiles", lib);
        issues += CheckNullEntries(lib.Archetypes, "Archetypes", lib);
        issues += CheckNullEntries(lib.Triggers, "Triggers", lib);
        issues += CheckNullEntries(lib.SlotOutcomes, "SlotOutcomes", lib);
        issues += CheckNullEntries(lib.Endings, "Endings", lib);

        // --- Duplicate / missing IDs ---
        issues += CheckDuplicateIds(Ids(lib.Eras, e => e.id), "Eras", lib);
        issues += CheckDuplicateIds(Ids(lib.Upgrades, u => u.id), "Upgrades", lib);
        issues += CheckDuplicateIds(Ids(lib.Endings, e => e.id), "Endings", lib);
        issues += CheckDuplicateIds(Ids(lib.Attributes, a => a.id), "Attributes", lib);
        issues += CheckDuplicateIds(Ids(lib.Nations, n => n.id), "Nations", lib);
        issues += CheckDuplicateIds(Ids(lib.Archetypes, a => a.id), "Archetypes", lib);
        issues += CheckDuplicateIds(Ids(lib.Triggers, t => t.id), "Triggers", lib);
        issues += CheckDuplicateIds(Ids(lib.SlotOutcomes, s => s.id), "SlotOutcomes", lib);
        issues += CheckDuplicateIds(Ids(lib.Profiles, p => p.id), "Profiles", lib);

        // --- Day plans ---
        issues += CheckDuplicateDayNumbers(lib);
        issues += CheckDayPlanLegendaryRanges(lib);

        // --- Cross references ---
        issues += CheckLegendaryReferences(lib);
        issues += CheckNationEraProfiles(lib);

        // --- World model (places and the days that use them) ---
        issues += CheckPlaces(lib);
        issues += CheckDayPlanPlaces(lib);

        return issues;
    }

    /// <summary>Fact categories every place must have (papers + books + planned questions).</summary>
    private static readonly ClueCategory[] RequiredFacts =
        { ClueCategory.Currency, ClueCategory.Language, ClueCategory.Technology, ClueCategory.Geography, ClueCategory.Politics };

    /// <summary>Reports places with missing or duplicate facts, no names, or unset / inverted birth years.</summary>
    private static int CheckPlaces(ContentLibrarySO lib)
    {
        int issues = 0;

        foreach (NationEraProfileSO place in lib.Profiles)
        {
            if (place == null)
                continue;

            foreach (ClueCategory category in RequiredFacts)
            {
                ProfileFact fact = place.facts?.FirstOrDefault(f => f != null && f.category == category);
                if (fact == null || string.IsNullOrWhiteSpace(fact.value))
                {
                    Debug.LogError($"[ContentLibraryValidator] Place '{place.name}' has no {category} fact in '{lib.name}' (papers would print a placeholder).", place);
                    issues++;
                }
            }

            if (place.facts != null && place.facts.Where(f => f != null).GroupBy(f => f.category).Any(g => g.Count() > 1))
            {
                Debug.LogError($"[ContentLibraryValidator] Place '{place.name}' lists a fact category twice in '{lib.name}'.", place);
                issues++;
            }

            if (place.AllNames.Count == 0)
            {
                Debug.LogWarning($"[ContentLibraryValidator] Place '{place.name}' has no names; visitors from there are called 'Subject #n'.", place);
                issues++;
            }

            if (place.birthYearMin == 0 && place.birthYearMax == 0)
            {
                Debug.LogError($"[ContentLibraryValidator] Place '{place.name}' has no birth years (0..0); its visitors are born 'Unknown' and their birth dates are never forged.", place);
                issues++;
            }
            else if (place.birthYearMin > place.birthYearMax)
            {
                Debug.LogError($"[ContentLibraryValidator] Place '{place.name}' has birthYearMin {place.birthYearMin} > birthYearMax {place.birthYearMax}.", place);
                issues++;
            }
        }

        return issues;
    }

    /// <summary>
    /// Reports day plans whose weighted eras have no place today (eras x allowed
    /// nations), rules no place of the day can break, and legendaries whose
    /// place is outside the day's world (their papers would print placeholders).
    /// </summary>
    private static int CheckDayPlanPlaces(ContentLibrarySO lib)
    {
        int issues = 0;

        foreach (DayPlanSO plan in lib.DayPlans)
        {
            if (plan == null)
                continue;

            List<NationEraProfileSO> today = lib.TodaysProfiles(plan);
            if (today.Count == 0)
            {
                Debug.LogError($"[ContentLibraryValidator] Day plan '{plan.name}' has no places (its eras x allowed nations match no place).", plan);
                issues++;
                continue;
            }

            foreach (EraWeight w in plan.EraWeights ?? Array.Empty<EraWeight>())
            {
                if (w.era != null && w.weight > 0f && today.All(p => p.era != w.era))
                {
                    Debug.LogError($"[ContentLibraryValidator] Day plan '{plan.name}' weights era '{w.era.id}' but none of its allowed nations has a place there.", plan);
                    issues++;
                }
            }

            foreach (TravelRuleSO rule in plan.ActiveTravelRules)
            {
                if (rule != null && today.All(p => rule.Allows(p.nation, p.era)))
                {
                    Debug.LogWarning($"[ContentLibraryValidator] Day plan '{plan.name}' uses rule '{rule.name}', which forbids none of the day's places (no traveller can break it).", plan);
                    issues++;
                }
            }

            foreach (LegendarySO legend in plan.AvailableLegendaries ?? Array.Empty<LegendarySO>())
            {
                if (legend == null || legend.nation == null || legend.trueEra == null)
                    continue;

                if (!today.Any(p => p.nation == legend.nation && p.era == legend.trueEra))
                {
                    Debug.LogWarning($"[ContentLibraryValidator] Day plan '{plan.name}' lists legendary '{legend.displayName}' whose place ({legend.nation.id}, {legend.trueEra.id}) is not in the day's world; their papers would print placeholders.", plan);
                    issues++;
                }
            }
        }

        return issues;
    }

    /// <summary>Yields the id of every non-null item in <paramref name="items"/>.</summary>
    private static IEnumerable<string> Ids<T>(IEnumerable<T> items, Func<T, string> getId) where T : UnityEngine.Object
    {
        foreach (T item in items)
            if (item != null)
                yield return getId(item);
    }

    /// <summary>Reports any null entries in an authored array (broken/missing asset references).</summary>
    private static int CheckNullEntries<T>(IReadOnlyList<T> list, string fieldName, ContentLibrarySO lib) where T : UnityEngine.Object
    {
        int issues = 0;

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == null)
            {
                Debug.LogError($"[ContentLibraryValidator] {fieldName}[{i}] is null in '{lib.name}'.", lib);
                issues++;
            }
        }

        return issues;
    }

    /// <summary>Reports null/empty ids and duplicate ids (case-insensitive) within a single field.</summary>
    private static int CheckDuplicateIds(IEnumerable<string> ids, string fieldName, ContentLibrarySO lib)
    {
        int issues = 0;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var reported = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string id in ids)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                Debug.LogError($"[ContentLibraryValidator] {fieldName} contains an entry with a null/empty id in '{lib.name}'.", lib);
                issues++;
                continue;
            }

            if (!seen.Add(id) && reported.Add(id))
            {
                Debug.LogError($"[ContentLibraryValidator] Duplicate {fieldName} id '{id}' in '{lib.name}'.", lib);
                issues++;
            }
        }

        return issues;
    }

    /// <summary>Reports duplicate DayPlan.DayNumber values and gaps in the day sequence.</summary>
    private static int CheckDuplicateDayNumbers(ContentLibrarySO lib)
    {
        int issues = 0;
        var seen = new HashSet<int>();
        var reported = new HashSet<int>();

        foreach (DayPlanSO plan in lib.DayPlans)
        {
            if (plan == null)
                continue;

            int day = plan.DayNumber;

            if (!seen.Add(day) && reported.Add(day))
            {
                Debug.LogError($"[ContentLibraryValidator] Duplicate DayPlan for day {day} in '{lib.name}' (asset '{plan.name}').", plan);
                issues++;
            }
        }

        List<int> days = lib.DayPlans
            .Where(p => p != null)
            .Select(p => p.DayNumber)
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        for (int i = 1; i < days.Count; i++)
        {
            if (days[i] != days[i - 1] + 1)
                Debug.LogWarning($"[ContentLibraryValidator] DayPlans gap in '{lib.name}': day {days[i - 1]} is followed by day {days[i]} (day(s) {days[i - 1] + 1}..{days[i] - 1} have no plan).");
        }

        return issues;
    }

    /// <summary>
    /// Reports DayPlan.AvailableLegendaries entries that are null, or whose
    /// legendary's [minDay, maxDay] range can never include that day.
    /// </summary>
    private static int CheckDayPlanLegendaryRanges(ContentLibrarySO lib)
    {
        int issues = 0;

        foreach (DayPlanSO plan in lib.DayPlans)
        {
            if (plan == null || plan.AvailableLegendaries == null)
                continue;

            for (int i = 0; i < plan.AvailableLegendaries.Count; i++)
            {
                LegendarySO legend = plan.AvailableLegendaries[i];

                if (legend == null)
                {
                    Debug.LogError($"[ContentLibraryValidator] DayPlan {plan.DayNumber} ('{plan.name}') AvailableLegendaries[{i}] is null in '{lib.name}'.", plan);
                    issues++;
                    continue;
                }

                if (plan.DayNumber < legend.minDay || plan.DayNumber > legend.maxDay)
                {
                    Debug.LogWarning($"[ContentLibraryValidator] DayPlan {plan.DayNumber} ('{plan.name}') lists legendary '{legend.displayName}' but its valid range is {legend.minDay}-{legend.maxDay} — it can never be rolled on this day.", plan);
                    issues++;
                }
            }
        }

        return issues;
    }

    /// <summary>Reports legendaries with missing era/archetype/nation references or an inverted day range.</summary>
    private static int CheckLegendaryReferences(ContentLibrarySO lib)
    {
        int issues = 0;

        foreach (LegendarySO legend in lib.Legendaries)
        {
            if (legend == null)
                continue;

            string label = $"Legendary '{legend.name}' ({legend.displayName})";

            if (legend.trueEra == null)
            {
                Debug.LogWarning($"[ContentLibraryValidator] {label} has no trueEra assigned in '{lib.name}'.", legend);
                issues++;
            }

            if (legend.archetype == null)
            {
                Debug.LogWarning($"[ContentLibraryValidator] {label} has no archetype assigned in '{lib.name}'.", legend);
                issues++;
            }

            if (legend.nation == null)
            {
                Debug.LogWarning($"[ContentLibraryValidator] {label} has no nation assigned in '{lib.name}'.", legend);
                issues++;
            }

            if (legend.minDay > legend.maxDay)
            {
                Debug.LogError($"[ContentLibraryValidator] {label} has minDay ({legend.minDay}) > maxDay ({legend.maxDay}) in '{lib.name}'.", legend);
                issues++;
            }
        }

        return issues;
    }

    /// <summary>Reports nation/era profiles with a missing nation or era, or duplicate (nation, era) pairs.</summary>
    private static int CheckNationEraProfiles(ContentLibrarySO lib)
    {
        int issues = 0;
        var seen = new HashSet<(NationSO nation, EraSO era)>();

        foreach (NationEraProfileSO profile in lib.Profiles)
        {
            if (profile == null)
                continue;

            if (profile.nation == null || profile.era == null)
            {
                Debug.LogError($"[ContentLibraryValidator] Profile '{profile.name}' is missing its nation or era reference in '{lib.name}'.", profile);
                issues++;
                continue;
            }

            var key = (profile.nation, profile.era);

            if (!seen.Add(key))
            {
                Debug.LogError($"[ContentLibraryValidator] Duplicate profile for nation '{profile.nation.id}' / era '{profile.era.id}' in '{lib.name}' (asset '{profile.name}').", profile);
                issues++;
            }
        }

        return issues;
    }
}
