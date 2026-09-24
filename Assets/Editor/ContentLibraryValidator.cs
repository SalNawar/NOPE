using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Phase 6 editor tool: scans every ContentLibrarySO asset in the project and
/// reports data issues to the console — null array entries, duplicate or
/// missing IDs, duplicate day numbers, dangling cross-references, and
/// interview content the office could not use (questions, dialogs, menus).
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
        issues += CheckNullEntries(lib.Questions, "Questions", lib);
        issues += CheckNullEntries(lib.Dialogs, "Dialogs", lib);

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
        issues += CheckDuplicateIds(Ids(lib.Questions, q => q.question != null ? q.question.id : null), "Questions", lib);
        issues += CheckDuplicateIds(Ids(lib.Dialogs, d => d.dialog != null ? d.dialog.id : null), "Dialogs", lib);

        // --- Day plans ---
        issues += CheckDuplicateDayNumbers(lib);
        issues += CheckDayPlanLegendaryRanges(lib);

        // --- Cross references ---
        issues += CheckLegendaryReferences(lib);
        issues += CheckNationEraProfiles(lib);

        // --- World model (places and the days that use them) ---
        issues += CheckPlaces(lib);
        issues += CheckDayPlanPlaces(lib);

        // --- Interview (wording, questions, dialogs, menus), upgrade ids, tell channels, small talk ---
        issues += CheckInterview(lib);
        issues += CheckUpgradeIds(lib);
        issues += CheckTellChannels(lib);
        issues += CheckSmallTalk(lib);

        return issues;
    }

    /// <summary>The error for a dialog effect op that would act while active (Generate World reports the same text).</summary>
    public static string DialogEffectOpError(string dialogId, string choiceId, string effectName, EffectOpType op) =>
        $"Dialog '{dialogId}' choice '{choiceId}' names effect '{effectName}', whose {op} op would already act this evening and in a replay of the day; " +
        "dialog effects may only hold instant ops and briefing/news lines (timed modifiers from dialogs are piece 4/5 work)";

    /// <summary>
    /// Reports interview content the office could not use: blank wording or
    /// menu capacity; a question whose answers could never be proven, a second
    /// question for one category, an answer template without {value}; a
    /// structurally broken dialog; a dialog effect that is missing or holds an
    /// op that acts while active (and a warning for a permanent one with a
    /// briefing or news line); menus fuller than the intercom shows.
    /// </summary>
    private static int CheckInterview(ContentLibrarySO lib)
    {
        int issues = 0;
        void Error(string message, UnityEngine.Object context)
        {
            Debug.LogError($"[ContentLibraryValidator] {message} ('{lib.name}')", context);
            issues++;
        }

        InterviewLines lines = lib.Interview ?? new InterviewLines();
        var wording = new (string field, string text)[]
        {
            ("deskName", lines.deskName), ("opener", lines.opener?.text), ("openerLegendary", lines.openerLegendary?.text),
            ("claim", lines.claim?.text), ("honorificMale", lines.honorificMale), ("honorificFemale", lines.honorificFemale),
            ("honorificUnknown", lines.honorificUnknown), ("requestLabel", lines.requestLabel), ("requestPrompt", lines.requestPrompt?.text),
            ("requestReply", lines.requestReply?.text), ("askLabel", lines.askLabel), ("backLabel", lines.backLabel),
            ("smallTalkLabel", lines.smallTalkLabel), ("smallTalkPrompt", lines.smallTalkPrompt?.text)
        };
        foreach ((string field, string text) in wording)
            if (string.IsNullOrWhiteSpace(text))
                Error($"Interview line '{field}' is blank (run Tools > TimeDesk > Generate World).", lib);

        if (lines.menuCapacity < 1)
            Error("Interview menu capacity is below 1 (run Tools > TimeDesk > Generate World).", lib);

        HashSet<ClueCategory> books = lib.ReferenceBookCategories();
        var asked = new HashSet<ClueCategory>();
        foreach (QuestionSO q in lib.Questions)
        {
            if (q == null || q.question == null)
                continue;

            InterviewQuestion question = q.question;
            if (!Forgery.IsProvableCategory(question.category, books))
                Error($"Question '{question.id}' asks about {question.category}: answers in this category can never be proven (no reference book covers it, and it is not a birth date).", q);
            if (!asked.Add(question.category))
                Error($"Question '{question.id}' asks about {question.category} again (one question per category).", q);
            if (!Interview.HoldsToken(question.answer?.text, Interview.ValueToken))
                Error($"Question '{question.id}': its answer template must hold {{value}}.", q);
            foreach (WordingOverride o in question.overrides ?? new List<WordingOverride>())
                if (o != null && !Interview.HoldsToken(o.answer?.text, Interview.ValueToken))
                    Error($"Question '{question.id}' override '{o.eraId}': its answer template must hold {{value}}.", q);
        }

        foreach (DialogSO d in lib.Dialogs)
        {
            if (d == null || d.dialog == null)
                continue;

            AuthoredDialog dialog = d.dialog;
            foreach (string problem in DialogChecks.Problems(dialog, lines.menuCapacity))
                Error($"Dialog '{dialog.id}': {problem}.", d);

            foreach (ScriptNode node in dialog.nodes ?? new List<ScriptNode>())
            {
                foreach (ScriptChoice choice in node?.choices ?? new List<ScriptChoice>())
                {
                    if (choice == null || string.IsNullOrWhiteSpace(choice.effect))
                        continue;

                    EffectSO fx = lib.GetEffectByAssetName(choice.effect);
                    if (fx == null)
                    {
                        Error($"Dialog '{dialog.id}' choice '{choice.id}' names effect '{choice.effect}', which the library does not list; add it to the library's effects.", d);
                        continue;
                    }

                    foreach (EffectOp op in fx.ops)
                        if (op != null && EffectOps.ActsWhileActive(op.type))
                            Error(DialogEffectOpError(dialog.id, choice.id, choice.effect, op.type) + ".", d);

                    if (fx.defaultDurationDays < 0 && fx.ops.Any(op => op != null && (op.type == EffectOpType.BriefingLine || op.type == EffectOpType.NewsLine)))
                    {
                        Debug.LogWarning($"[ContentLibraryValidator] Dialog '{dialog.id}' choice '{choice.id}' names effect '{choice.effect}', which is permanent and carries a briefing or news line: the line would repeat every morning ('{lib.name}').", d);
                        issues++;
                    }
                }
            }
        }

        bool smallTalk = (lib.Eras ?? Array.Empty<EraSO>()).Any(e => e != null && e.smallTalk != null && e.smallTalk.Count > 0) ||
                         lib.Profiles.Any(p => p != null && p.smallTalk != null && p.smallTalk.Count > 0);
        foreach (string problem in DialogChecks.MenuProblems(lib.Questions.Count(q => q != null), smallTalk, MaxDocuments(TravellerBlueprints(lib)),
                                                             lib.Dialogs.Count(d => d != null), lines.menuCapacity))
            Error(problem, lib);

        return issues;
    }

    /// <summary>
    /// The most documents one traveller carries among these blueprints (a
    /// request each; null blueprints and templates are skipped). Generate
    /// World counts its source's blueprints with the same rule.
    /// </summary>
    public static int MaxDocuments(IEnumerable<CaseBlueprintSO> blueprints) =>
        blueprints.Where(b => b != null && b.DocumentTemplates != null)
                  .Select(b => b.DocumentTemplates.Count(t => t != null))
                  .DefaultIfEmpty(0)
                  .Max();

    /// <summary>Every blueprint a traveller can come from: the day plans' possible and forced ones and the legendaries' overrides (nulls included).</summary>
    private static IEnumerable<CaseBlueprintSO> TravellerBlueprints(ContentLibrarySO lib)
    {
        foreach (DayPlanSO plan in lib.DayPlans)
        {
            if (plan == null)
                continue;

            if (plan.PossibleBlueprints != null)
                foreach (CaseBlueprintSO blueprint in plan.PossibleBlueprints)
                    yield return blueprint;

            foreach (CaseBlueprintSO blueprint in plan.ForcedBlueprints)
                yield return blueprint;
        }

        foreach (LegendarySO legend in lib.Legendaries)
            if (legend != null)
                yield return legend.blueprintOverride;
    }

    /// <summary>
    /// Reports every upgrade id that names no library upgrade: UpgradeOwned
    /// keys in questions, dialogs and triggers, and UnlockUpgrade or
    /// (non-empty) ShopDiscountPercent parameters in effects.
    /// </summary>
    private static int CheckUpgradeIds(ContentLibrarySO lib)
    {
        int issues = 0;
        void Check(string upgradeId, string owner, UnityEngine.Object context)
        {
            if (lib.GetUpgradeById(upgradeId) != null)
                return;

            Debug.LogError($"[ContentLibraryValidator] {owner} names unknown upgrade '{upgradeId}' (not in '{lib.name}' upgrades).", context);
            issues++;
        }

        void CheckConditions(IEnumerable<TriggerCondition> conditions, string owner, UnityEngine.Object context)
        {
            foreach (TriggerCondition c in conditions ?? Array.Empty<TriggerCondition>())
                if (c != null && c.type == TriggerConditionType.UpgradeOwned)
                    Check(c.key, owner, context);
        }

        foreach (QuestionSO q in lib.Questions)
            if (q != null)
                CheckConditions(q.conditions, $"Question '{q.name}'", q);
        foreach (DialogSO d in lib.Dialogs)
            if (d != null)
                CheckConditions(d.conditions, $"Dialog '{d.name}'", d);
        foreach (TimelineTriggerSO t in lib.Triggers)
            if (t != null)
                CheckConditions(t.conditions, $"Trigger '{t.name}'", t);

        foreach (EffectSO fx in lib.Effects)
        {
            if (fx == null)
                continue;

            foreach (EffectOp op in fx.ops)
                if (op != null && (op.type == EffectOpType.UnlockUpgrade || (op.type == EffectOpType.ShopDiscountPercent && !string.IsNullOrEmpty(op.stringParam))))
                    Check(op.stringParam, $"Effect '{fx.name}' ({op.type})", fx);
        }

        return issues;
    }

    /// <summary>Reports day plans with no tell channel (their liars could leak nothing).</summary>
    private static int CheckTellChannels(ContentLibrarySO lib)
    {
        int issues = 0;
        foreach (DayPlanSO plan in lib.DayPlans)
        {
            if (plan != null && plan.TellChannels.Count == 0)
            {
                Debug.LogError($"[ContentLibraryValidator] Day plan '{plan.name}' has no tell channel, so its liars can leak no tell (world_source.json days[].channels) in '{lib.name}'.", plan);
                issues++;
            }
        }

        return issues;
    }

    /// <summary>Warns about an era a day plan uses that has no small talk while some of its places that day have none either.</summary>
    private static int CheckSmallTalk(ContentLibrarySO lib)
    {
        int issues = 0;
        var warned = new HashSet<EraSO>();
        foreach (DayPlanSO plan in lib.DayPlans)
        {
            if (plan == null)
                continue;

            List<NationEraProfileSO> today = lib.TodaysProfiles(plan);
            foreach (EraWeight w in plan.EraWeights ?? Array.Empty<EraWeight>())
            {
                if (w.era == null || w.weight <= 0f || (w.era.smallTalk != null && w.era.smallTalk.Count > 0) || warned.Contains(w.era))
                    continue;

                if (today.Any(p => p.era == w.era && (p.smallTalk == null || p.smallTalk.Count == 0)))
                {
                    Debug.LogWarning($"[ContentLibraryValidator] Era '{w.era.id}' (day plan '{plan.name}') has no small talk, and some of its places have none either; their travellers have nothing to say when asked in '{lib.name}'.", w.era);
                    warned.Add(w.era);
                    issues++;
                }
            }
        }

        return issues;
    }

    /// <summary>Fact categories every place must have (papers + books + questions).</summary>
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
                    Debug.LogError($"[ContentLibraryValidator] Place '{place.name}' has no {category} fact in '{lib.name}' (papers and answers would use a placeholder).", place);
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
                Debug.LogError($"[ContentLibraryValidator] Place '{place.name}' has no birth years (0..0); its visitors are born 'Unknown' and their birth dates can never carry a birth-date tell.", place);
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
