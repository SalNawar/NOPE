using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Phase 6 editor tool: scans every ContentLibrarySO asset in the project and
/// reports data issues to the console — null array entries, duplicate or
/// missing IDs, duplicate day numbers, dangling cross-references, interview
/// content the office could not use (questions, dialogs, menus), the Future
/// and history (Future places, leader effects, history values, SetFact only
/// in one-shot history-rule triggers), trigger, effect and ending fields, the
/// culture themes and UI string tables (contrast included), and the
/// translation (tongues, tables, every place's tongue, the translators, the
/// notice), and the agency block (its name, programme line and first date).
/// Access via Tools &gt; TimeDesk &gt; Validate Content Library.
/// </summary>
public static partial class ContentLibraryValidator
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
            ReportCharacterArt(lib);
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
        issues += CheckDuplicateIds(Ids(lib.Legendaries, l => l.id), "Legendaries", lib);

        // --- Day plans ---
        issues += CheckDayPlanEntries(lib);
        issues += CheckDayPlanLegendaries(lib);
        issues += CheckForcedEntries(lib);
        issues += CheckPremadeRows(lib);
        issues += CheckPacing(lib);

        // --- Cross references ---
        issues += CheckLegendaryReferences(lib);
        issues += CheckNationEraProfiles(lib);

        // --- World model (places, dress and the days that use them) ---
        issues += CheckPlaces(lib);
        issues += CheckCultureUnique(lib);
        issues += CheckLookRules(lib);
        issues += CheckDayPlanPlaces(lib);

        // --- Interview (wording, questions, dialogs, menus), upgrade ids, tell channels, small talk ---
        issues += CheckInterview(lib);
        issues += CheckKinds(lib);
        issues += CheckVoices(lib);
        issues += CheckDeskFit(lib);
        issues += CheckUpgradeIds(lib);
        issues += CheckUpgradeTree(lib);
        issues += CheckTellChannels(lib);
        issues += CheckSmallTalk(lib);

        // --- The Future and history; trigger, effect and ending fields ---
        issues += CheckFuture(lib);
        issues += CheckTriggers(lib);
        issues += CheckEffects(lib);
        issues += CheckHistoryValues(lib);
        issues += CheckEndings(lib);

        // --- The culture themes and UI string tables (piece 6) ---
        issues += CheckCulture(lib);

        // --- The authored mail (redesign phase 25; ContentLibraryValidator.Mail.cs) ---
        issues += CheckMail(lib);

        // --- Translation (piece 9) ---
        issues += CheckTranslation(lib);

        // --- The agency block (redesign phase 2) ---
        issues += CheckAgency(lib);

        // --- The hall's portals and the Directorate's routes (the portals spec v3; ContentLibraryValidator.Portals.cs) ---
        issues += CheckPortals(lib);

        // --- The morning paper's debt lines (redesign phase 13; ContentLibraryValidator.News.cs) ---
        issues += CheckNews(lib);

        // --- The PC block: the Internet's sites, pages and people (ContentLibraryValidator.Pc.cs) ---
        issues += CheckPc(lib);

        return issues;
    }

    /// <summary>The error for a dialog effect op that would act while active (Generate World reports the same text).</summary>
    public static string DialogEffectOpError(string dialogId, string choiceId, string effectName, EffectOpType op) =>
        $"Dialog '{dialogId}' choice '{choiceId}' names effect '{effectName}', whose {op} op would already act this evening and in a replay of the day; " +
        "dialog effects may only hold instant ops and briefing/news lines (timed modifiers from dialogs are piece 4/5 work)";

    /// <summary>The error for a dialog effect holding a history-only op (EffectOps.HistoryOnly; Generate World reports the same text).</summary>
    public static string DialogHistoryOpError(string dialogId, string choiceId, string effectName, EffectOpType op) =>
        $"Dialog '{dialogId}' choice '{choiceId}' names effect '{effectName}', whose {op} op may only run in a history rule at night";

    /// <summary>
    /// Reports interview content the office could not use: blank wording or
    /// menu capacity; a spoken request that is empty, has a blank or repeated id,
    /// or a blank label, prompt or reply; a question whose answers could never be proven, a second
    /// question for one category, an answer template without {value}; a
    /// structurally broken dialog; a dialog effect that is missing or holds an
    /// op that acts while active (and a warning for a permanent one with a
    /// briefing or news line); menus fuller than the traveller wheel shows.
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
            ("honorificMale", lines.honorificMale), ("honorificFemale", lines.honorificFemale),
            ("honorificUnknown", lines.honorificUnknown), ("requestLabel", lines.requestLabel), ("papersLabel", lines.papersLabel), ("requestPrompt", lines.requestPrompt?.text),
            ("requestReply", lines.requestReply?.text), ("askLabel", lines.askLabel), ("backLabel", lines.backLabel),
            ("smallTalkLabel", lines.smallTalkLabel), ("smallTalkPrompt", lines.smallTalkPrompt?.text), ("lookLabel", lines.lookLabel), ("faceLabel", lines.faceLabel)
        };
        foreach ((string field, string text) in wording)
            if (string.IsNullOrWhiteSpace(text))
                Error($"Interview line '{field}' is blank (run Tools > TimeDesk > Generate World).", lib);

        if (lines.menuCapacity < 1)
            Error("Interview menu capacity is below 1 (run Tools > TimeDesk > Generate World).", lib);

        var requestIds = new HashSet<string>();
        foreach (InterviewRequest r in lines.requests ?? new List<InterviewRequest>())
        {
            if (r == null)
            {
                Error("A spoken request is empty (run Tools > TimeDesk > Generate World).", lib);
                continue;
            }

            if (string.IsNullOrWhiteSpace(r.id) || !requestIds.Add(r.id))
                Error($"Spoken request '{r.id}' has a blank or repeated id (run Tools > TimeDesk > Generate World).", lib);
            if (string.IsNullOrWhiteSpace(r.label) || string.IsNullOrWhiteSpace(r.prompt?.text) || string.IsNullOrWhiteSpace(r.reply?.text))
                Error($"Spoken request '{r.id}' has a blank label, prompt or reply (run Tools > TimeDesk > Generate World).", lib);
        }

        HashSet<ClueCategory> books = lib.ReferenceBookCategories();
        List<InterviewQuestion> questions = lib.Questions.Where(q => q != null && q.question != null).Select(q => q.question).ToList();
        foreach (string problem in InterviewQuestions.Problems(questions))
            Error(problem, lib);
        foreach (QuestionSO q in lib.Questions)
        {
            if (q == null || q.question == null)
                continue;

            InterviewQuestion question = q.question;
            if (!Forgery.IsProvableCategory(question.category, books))
                Error($"Question '{question.id}' asks about {question.category}: answers in this category can never be proven (no reference book covers it, and it is not a birth date).", q);
            if (question.category == Looks.EvidenceCategory)
                Error($"Question '{question.id}' asks about Culture; dress is looked at on the traveller wheel, never asked.", q);
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
                    {
                        if (op != null && EffectOps.ActsWhileActive(op.type))
                            Error(DialogEffectOpError(dialog.id, choice.id, choice.effect, op.type) + ".", d);
                        if (op != null && EffectOps.HistoryOnly(op.type))
                            Error(DialogHistoryOpError(dialog.id, choice.id, choice.effect, op.type) + ".", d);
                    }

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
        var premadeDialogs = new HashSet<string>(TimelineService.PremadeDialogIds(lib));
        int bound = lib.Dialogs.Count(d => d != null && d.dialog != null && premadeDialogs.Contains(d.dialog.id));
        List<KindForms> kindForms = KindForms(lib, out List<AskableForm> forms, out int maxRequests);
        foreach (string problem in FormRequests.GroupProblems(forms, lines.askGroups))
            Error($"{problem} (run Tools > TimeDesk > Generate World)", lib);
        foreach (string problem in FormRequests.ReplyProblems(lines.missingFormReplies, forms, kindForms))
            Error($"{problem} (run Tools > TimeDesk > Generate World)", lib);

        foreach (string problem in DialogChecks.MenuProblems(InterviewQuestions.Count(questions), smallTalk, maxRequests,
                                                             (lines.requests ?? new List<InterviewRequest>()).Count(r => r != null),
                                                             lib.Dialogs.Count(d => d != null) - bound, bound, lines.menuCapacity, InterviewScript.OffersPad(forms, lines)))
            Error(problem, lib);

        return issues;
    }

    /// <summary>
    /// Each kind in play on each day of the library's plans (the plan's kinds'
    /// and forced blueprints) with that day's papers menu, the same for every
    /// traveller (FormRequests.MetSoFar over TimelineService.DayForms, as
    /// TimelineService.AgencyForms gives the office), and its blueprints'
    /// carried forms: FormRequests.ReplyProblems' input (the generator
    /// computes the same from its source). <paramref name="forms"/> is every
    /// form of the days; <paramref name="maxRequests"/> the most request
    /// entries any day's menu holds (FormRequests.Count), the papers menu's size.
    /// </summary>
    public static List<KindForms> KindForms(ContentLibrarySO lib, out List<AskableForm> forms, out int maxRequests)
    {
        int lastDay = lib.DayPlans.Where(p => p != null).Select(p => p.DayNumber).DefaultIfEmpty(0).Max();
        List<IReadOnlyList<AskableForm>> days = TimelineService.DayForms(lib, lastDay);
        forms = days.SelectMany(d => d).Distinct().ToList();
        var byNumber = forms.GroupBy(f => f.FormNumber ?? string.Empty).ToDictionary(g => g.Key, g => g.First());
        var kinds = new List<KindForms>();
        maxRequests = 0;
        for (int day = 1; day <= lastDay; day++)
        {
            List<AskableForm> menu = FormRequests.MetSoFar(days, day);
            maxRequests = Math.Max(maxRequests, FormRequests.Count(menu));
            DayPlanSO plan = lib.GetDayPlan(day);
            if (plan == null)
                continue;
            foreach (IGrouping<TravellerKind, CaseBlueprintSO> g in plan.PossibleBlueprints.Concat(plan.ForcedBlueprints).Where(b => b != null).Distinct().GroupBy(b => b.Kind))
                kinds.Add(new KindForms
                {
                    Kind = g.Key,
                    Askable = menu,
                    Carried = g.SelectMany(plan.TemplatesOf)
                               .Select(t => byNumber.TryGetValue(t.formNumber ?? string.Empty, out AskableForm f) ? f : new AskableForm(t.formNumber, t.displayName, t.askGroup, DocumentHandOvers.IsRequested(t.handOver)))
                               .ToList()
                });
        }
        return kinds;
    }

    /// <summary>
    /// The most papers one traveller carries among these blueprints on the
    /// day of <paramref name="plan"/>: a blueprint's templates the day issues
    /// (DayPlanSO.TemplatesOf; a form no day issues, the cut proofs of means,
    /// is never carried) outside a request group, plus one per group (a
    /// traveller carries one form of a group, FormRequests.CarriedCount; null
    /// blueprints and templates are skipped); 0 for no blueprints. The desk's
    /// paper spawn slots are checked against the most of any day.
    /// </summary>
    public static int MaxDocuments(IEnumerable<CaseBlueprintSO> blueprints, DayPlanSO plan) =>
        blueprints.Where(b => b != null && b.DocumentTemplates != null)
                  .Select(b => FormRequests.CarriedCount(plan.TemplatesOf(b).Where(t => t != null).Select(t => t.askGroup).ToList()))
                  .DefaultIfEmpty(0)
                  .Max();

    /// <summary>Every blueprint a traveller can come from: the day plans' possible and forced ones (nulls included); premades use the day's. The office builder counts the same blueprints.</summary>
    public static IEnumerable<CaseBlueprintSO> TravellerBlueprints(ContentLibrarySO lib)
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

    /// <summary>
    /// The Future: at most one era is the Future, and then every nation has a
    /// place in it; no premade claims (trueEra) or comes from (truePlace) the
    /// Future, which is in the world only while its nation leads (errors);
    /// every nation's leader effect is a UI-channel effect whose Cue op is its
    /// culture cue (CultureCue), and no Future place has baselines, which
    /// would put it in the dominance tiers (warnings).
    /// </summary>
    private static int CheckFuture(ContentLibrarySO lib)
    {
        int issues = 0;
        EraSO[] futures = (lib.Eras ?? Array.Empty<EraSO>()).Where(e => e != null && e.isFuture).ToArray();
        if (futures.Length > 1)
        {
            Debug.LogError($"[ContentLibraryValidator] More than one era is the Future ({string.Join(", ", futures.Select(e => e.id))}) in '{lib.name}'.", lib);
            issues++;
        }

        foreach (NationSO nation in lib.Nations)
        {
            if (nation == null)
                continue;

            if (futures.Length == 1 && lib.GetProfile(nation, futures[0]) == null)
            {
                Debug.LogError($"[ContentLibraryValidator] Nation '{nation.id}' has no Future place, so it could lead with no Future to open in '{lib.name}'.", nation);
                issues++;
            }

            EffectSO fx = nation.leaderEffect;
            bool cue = fx != null && fx.channel == EffectChannel.UI &&
                       fx.ops.Any(op => op != null && op.type == EffectOpType.Cue && CultureCue.TryParse(op.stringParam, out string id) && id == nation.id);
            if (!cue)
            {
                Debug.LogWarning($"[ContentLibraryValidator] Nation '{nation.id}' has no leader effect broadcasting '{CultureCue.Format(nation.id)}' on the UI channel, so its lead shows no culture (run Tools > TimeDesk > Generate World) in '{lib.name}'.", nation);
                issues++;
            }
        }

        foreach (NationEraProfileSO place in lib.Profiles)
        {
            if (place != null && place.era != null && place.era.isFuture && place.baselines != null && place.baselines.Count > 0)
            {
                Debug.LogWarning($"[ContentLibraryValidator] Future place '{place.name}' has baselines, so it would join the dominance tiers and their news in '{lib.name}'.", place);
                issues++;
            }
        }

        foreach (LegendarySO legend in lib.Legendaries)
        {
            if (legend == null)
                continue;

            if (legend.trueEra != null && legend.trueEra.isFuture)
            {
                Debug.LogError($"[ContentLibraryValidator] Premade '{legend.name}' ({legend.displayName}) claims the Future, whose place is in the world only while its nation leads; no premade may claim or come from the Future in '{lib.name}'.", legend);
                issues++;
            }

            if (legend.truePlace != null && legend.truePlace.era != null && legend.truePlace.era.isFuture)
            {
                Debug.LogError($"[ContentLibraryValidator] Premade '{legend.name}' ({legend.displayName}) comes from the Future place '{legend.truePlace.name}', which is in the world only while its nation leads; no premade may claim or come from the Future in '{lib.name}'.", legend);
                issues++;
            }
        }

        return issues;
    }

    /// <summary>
    /// Every trigger condition names what its type needs (a profile and an
    /// attribute, an attribute, a nation, a key): without it the condition can
    /// never pass. A trigger whose outcome effect holds a history-only op must
    /// be one-shot (a repeatable one would latch an edit every night). Each
    /// trigger is read as a history rule (HistoryChecks.RuleProblems, the
    /// generator's rule): a condition, a news line or an outcome, verdict
    /// flags naming known premades, a stability change within a hundred.
    /// </summary>
    private static int CheckTriggers(ContentLibrarySO lib)
    {
        int issues = 0;
        IEnumerable<string> forcedKeys = lib.DayPlans.Where(p => p != null).SelectMany(p => p.ForcedCases).Where(f => f != null)
            .SelectMany(f => f.conditions ?? new List<TriggerCondition>()).Where(c => c != null).Select(c => c.key);
        var historyRules = lib.Triggers.Where(t => t != null && t.id != null && t.id.StartsWith(FlagKeys.HistoryRuleTriggerId(string.Empty), StringComparison.Ordinal))
            .Select(t => (t.id.Substring(FlagKeys.HistoryRuleTriggerId(string.Empty).Length), t.section));
        foreach (string warning in HistoryChecks.ReturnProblems(historyRules, forcedKeys))
        {
            Debug.LogWarning($"[ContentLibraryValidator] {warning}", lib);
            issues++;
        }
        var premadeIds = new HashSet<string>(lib.Legendaries.Where(l => l != null).Select(l => l.id));
        foreach (TimelineTriggerSO t in lib.Triggers)
        {
            if (t == null)
                continue;

            List<EffectOp> ops = (t.outcomes ?? new List<TriggerOutcome>()).Where(o => o != null && o.effect != null).SelectMany(o => o.effect.ops ?? new List<EffectOp>()).Where(op => op != null).ToList();
            List<TriggerCondition> conditions = (t.conditions ?? new List<TriggerCondition>()).Where(c => c != null).ToList();
            foreach (string problem in HistoryChecks.RuleProblems(t.name, ops.Count, !string.IsNullOrWhiteSpace(t.newsLineOnFire), conditions.Count,
                                                                  conditions.Select(c => c.key), ops.Where(op => op.type == EffectOpType.AddStability).Sum(op => op.floatParam), premadeIds))
            {
                Debug.LogError($"[ContentLibraryValidator] {problem} (in '{lib.name}'; run Tools > TimeDesk > Generate World)", t);
                issues++;
            }

            foreach (TriggerCondition c in t.conditions ?? new List<TriggerCondition>())
            {
                string missing = c == null ? null : MissingReference(c);
                if (missing != null)
                {
                    Debug.LogError($"[ContentLibraryValidator] Trigger '{t.name}' has a {c.type} condition without {missing}, so it can never pass in '{lib.name}'.", t);
                    issues++;
                }
            }

            bool history = (t.outcomes ?? new List<TriggerOutcome>()).Any(o => o != null && o.effect != null && o.effect.ops.Any(op => op != null && EffectOps.HistoryOnly(op.type)));
            if (history && !t.oneShot)
            {
                Debug.LogError($"[ContentLibraryValidator] Trigger '{t.name}' latches history (SetFact) but is not one-shot, so it would latch an edit and print its line every night in '{lib.name}'.", t);
                issues++;
            }
        }

        return issues;
    }

    /// <summary>What a condition of this type needs and lacks ("a profile and an attribute", "a nation", ...), or null when it has it.</summary>
    private static string MissingReference(TriggerCondition c)
    {
        switch (c.type)
        {
            case TriggerConditionType.AttributeScoreAtLeast:
            case TriggerConditionType.AttributeScoreAtMost:
            case TriggerConditionType.AttributeIsDominant:
            case TriggerConditionType.AttributeIsSupporting:
                return c.profile != null && c.attribute != null ? null : "a profile and an attribute";
            case TriggerConditionType.NationScoreAtLeast:
            case TriggerConditionType.NationIsLeader:
                return c.nation != null ? null : "a nation";
            case TriggerConditionType.GlobalAttrAtLeast:
            case TriggerConditionType.GlobalAttrAtMost:
                return c.attribute != null ? null : "an attribute";
            case TriggerConditionType.CounterAtLeast:
            case TriggerConditionType.FlagSet:
            case TriggerConditionType.FlagNotSet:
            case TriggerConditionType.UpgradeOwned:
                return !string.IsNullOrWhiteSpace(c.key) ? null : "a key";
            default:
                return null;
        }
    }

    /// <summary>
    /// Effects: duplicate asset names (lookups are by name); AddAttributeScore
    /// needs a profile and an attribute, AddNationScore a nation, SetFact a
    /// profile, an editable category and a value. An effect with a history-only
    /// op must be a trigger outcome and never a slot outcome, upgrade unlock,
    /// tier, leader or dialog effect (those run by day or at Home, not latched
    /// at night).
    /// </summary>
    private static int CheckEffects(ContentLibrarySO lib)
    {
        int issues = 0;
        void Error(string message, UnityEngine.Object context)
        {
            Debug.LogError($"[ContentLibraryValidator] {message} in '{lib.name}'.", context);
            issues++;
        }

        foreach (IGrouping<string, EffectSO> dup in lib.Effects.Where(e => e != null).GroupBy(e => e.name, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
            Error($"Effect asset name '{dup.Key}' is listed {dup.Count()} times (effects are looked up by name)", dup.First());

        var triggerEffects = new HashSet<EffectSO>(lib.Triggers.Where(t => t != null).SelectMany(t => t.outcomes ?? new List<TriggerOutcome>()).Where(o => o != null && o.effect != null).Select(o => o.effect));
        var dayEffects = new Dictionary<EffectSO, string>();
        void Day(EffectSO fx, string owner)
        {
            if (fx != null && !dayEffects.ContainsKey(fx))
                dayEffects.Add(fx, owner);
        }
        foreach (SlotOutcomeSO s in lib.SlotOutcomes)
            if (s != null) Day(s.effect, $"slot outcome '{s.name}'");
        foreach (UpgradeSO u in lib.Upgrades)
            if (u != null) Day(u.unlockEffect, $"upgrade '{u.name}'");
        foreach (NationEraProfileSO p in lib.Profiles)
            foreach (AttributeBaseline b in p?.baselines ?? new List<AttributeBaseline>())
                if (b != null)
                {
                    Day(b.dominantEffect, $"a tier of '{p.name}'");
                    Day(b.supportingEffect, $"a tier of '{p.name}'");
                }
        foreach (NationSO n in lib.Nations)
            if (n != null) Day(n.leaderEffect, $"the leader of '{n.name}'");
        foreach (DialogSO d in lib.Dialogs)
            foreach (ScriptChoice choice in (d?.dialog?.nodes ?? new List<ScriptNode>()).Where(n => n != null).SelectMany(n => n.choices ?? new List<ScriptChoice>()))
                if (choice != null) Day(lib.GetEffectByAssetName(choice.effect), $"dialog '{d.name}'");

        foreach (EffectSO fx in lib.Effects)
        {
            if (fx == null)
                continue;

            foreach (EffectOp op in fx.ops)
            {
                if (op == null)
                    continue;
                if (op.type == EffectOpType.AddAttributeScore && (op.profile == null || op.attribute == null))
                    Error($"Effect '{fx.name}' has an AddAttributeScore op without a profile and an attribute", fx);
                if (op.type == EffectOpType.AddNationScore && op.nation == null)
                    Error($"Effect '{fx.name}' has an AddNationScore op without a nation", fx);
                if (op.type == EffectOpType.SetFact && (op.profile == null || !History.IsEditable(op.category) || string.IsNullOrWhiteSpace(op.stringParam)))
                    Error($"Effect '{fx.name}' has a SetFact op without a profile, an editable category and a value", fx);
            }

            if (!fx.ops.Any(op => op != null && EffectOps.HistoryOnly(op.type)))
                continue;
            if (!triggerEffects.Contains(fx))
                Error($"Effect '{fx.name}' holds a history-only op (SetFact) but no trigger fires it", fx);
            if (dayEffects.TryGetValue(fx, out string owner))
                Error($"Effect '{fx.name}' holds a history-only op (SetFact) but is also {owner}'s effect, which does not latch at night", fx);
        }

        return issues;
    }

    /// <summary>History values keep every book value unique: HistoryChecks over every SetFact op against the authored facts (no history).</summary>
    private static int CheckHistoryValues(ContentLibrarySO lib)
    {
        var edits = new List<FactEdit>();
        foreach (EffectSO fx in lib.Effects)
            foreach (EffectOp op in fx?.ops ?? new List<EffectOp>())
                if (op != null && op.type == EffectOpType.SetFact && op.profile != null && op.profile.nation != null && op.profile.era != null)
                    edits.Add(new FactEdit(op.profile.nation.id, op.profile.era.id, op.category, op.stringParam, 0, EditCause.Rule, fx.name));

        List<string> problems = HistoryChecks.Problems(edits, lib.BuildWorldFacts(null));
        foreach (string problem in problems)
            Debug.LogError($"[ContentLibraryValidator] {problem} ('{lib.name}')", lib);
        return problems.Count;
    }

    /// <summary>
    /// Endings: a retired condition (EndingRules.IsRetired: the attribute
    /// epilogues, retired 2026-09-29) is an error, since it never ends a run;
    /// a day ending needs a threshold of at least 1; and the world's outcomes
    /// every ending lists (the library's world block, WorldContent.Problems).
    /// </summary>
    private static int CheckEndings(ContentLibrarySO lib)
    {
        int issues = 0;
        foreach (EndingSO e in lib.Endings)
        {
            if (e == null)
                continue;
            if (EndingRules.IsRetired(e.conditionType))
            {
                Debug.LogError($"[ContentLibraryValidator] Ending '{e.name}' uses the retired condition {e.conditionType} (the attribute epilogues, retired 2026-09-29: no ending judges the world); it never ends a run, so take it out of '{lib.name}'.", e);
                issues++;
            }
            if (e.conditionType == EndingConditionType.DayAtLeast && e.threshold < 1f)
            {
                Debug.LogError($"[ContentLibraryValidator] Ending '{e.name}' (DayAtLeast) needs a threshold of at least 1 in '{lib.name}'.", e);
                issues++;
            }
        }

        foreach (string problem in lib.World.Problems())
        {
            Debug.LogError($"[ContentLibraryValidator] World: {problem} ('{lib.name}'; edit world_source.json \"world\" and Generate World).", lib);
            issues++;
        }

        return issues + CheckWorldRefs(lib);
    }

    /// <summary>
    /// The world's references in the assets (the endings spec §8.1, the rule
    /// Generate World applies to the source): every place's leaning, every
    /// premade's pull and every PullOutcome op (a history rule's or a dialog
    /// effect's, EffectOpType.PullOutcome) names a factor answered by pulls
    /// and one of its outcomes, a pull by more than 0; every role names an
    /// archetype of the library. Returns the number of problems.
    /// </summary>
    private static int CheckWorldRefs(ContentLibrarySO lib)
    {
        WorldContent world = lib.World;
        var problems = new List<(string text, UnityEngine.Object where)>();
        foreach (NationEraProfileSO place in lib.Profiles.Where(p => p != null))
            foreach (OutcomeRef l in place.leanings ?? new List<OutcomeRef>())
                problems.AddRange(world.RefProblems($"Place '{place.id}' leaning", l?.factor, l?.outcome, null).Select(p => (p, (UnityEngine.Object)place)));
        foreach (LegendarySO premade in lib.Legendaries.Where(m => m != null))
            foreach (OutcomePull pull in premade.pulls ?? new List<OutcomePull>())
                problems.AddRange(world.RefProblems($"Premade '{premade.id}' pull", pull?.factor, pull?.outcome, pull?.amount ?? 0f).Select(p => (p, (UnityEngine.Object)premade)));
        IEnumerable<EffectSO> effects = lib.Effects.Concat(lib.Triggers.Where(t => t != null).SelectMany(t => t.outcomes ?? new List<TriggerOutcome>()).Where(o => o != null).Select(o => o.effect));
        foreach (EffectSO fx in effects.Where(e => e != null).Distinct())
            foreach (EffectOp op in (fx.ops ?? new List<EffectOp>()).Where(op => op != null && op.type == EffectOpType.PullOutcome))
            {
                if (!WorldPulls.TryParseOpKey(op.stringParam, out string factor, out string outcome))
                    problems.Add(($"Effect '{fx.name}' has a PullOutcome op whose stringParam '{op.stringParam}' is not \"factor/outcome\" (WorldPulls.OpKey).", fx));
                else
                    problems.AddRange(world.RefProblems($"Effect '{fx.name}' PullOutcome op", factor, outcome, op.floatParam).Select(p => (p, (UnityEngine.Object)fx)));
            }
        var archetypes = new HashSet<string>(lib.Archetypes.Where(a => a != null).Select(a => a.id));
        foreach (WorldRole role in world.roles.Where(r => r != null && !archetypes.Contains(r.archetype)))
            problems.Add(($"world.roles '{role.archetype}' is no archetype of the library.", lib));

        foreach ((string text, UnityEngine.Object where) in problems)
            Debug.LogError($"[ContentLibraryValidator] World: {text} ('{lib.name}'; edit world_source.json and Generate World, or the effect asset).", where);
        return problems.Count;
    }

    /// <summary>
    /// The culture themes and UI string tables (piece 6): the neutral theme, the
    /// reading table and the Latin fallback font exist; every nation has a theme
    /// (a missing one is a warning: that culture stays neutral); no culture id
    /// twice; every theme's language has a table and every culture table passes
    /// UiStrings.TableProblems; every theme has a colour for every role and
    /// passes the contrast check with its stored palette and rings (the same
    /// pairs as Generate World); a theme whose labels need an OS font names one;
    /// a missing wallpaper is a warning.
    /// </summary>
    private static int CheckCulture(ContentLibrarySO lib)
    {
        int issues = 0;
        void Error(string message)
        {
            Debug.LogError($"[ContentLibraryValidator] {message} in '{lib.name}' (Tools > TimeDesk > Generate World writes the culture assets).", lib);
            issues++;
        }

        CultureUiSettings ui = lib.CultureUi;
        ThemeSO neutral = lib.NeutralTheme;
        UiStringTableSO reading = lib.GetStringTable(ui.readingLanguage);
        if (ui.latinFallbackFont == null)
            Error("No Latin fallback font (cultureUi.latinFallbackFont)");
        if (neutral == null)
            Error("No neutral theme");
        if (reading == null)
            Error($"No UI string table for the reading language '{ui.readingLanguage}'");
        if (neutral == null || reading == null)
            return issues;

        foreach (string problem in UiStrings.TableProblems(reading.entries, null, false))
            Error($"UI string table '{reading.language}': {problem}");

        foreach (NationSO nation in lib.Nations)
        {
            if (nation != null && lib.GetThemeByCultureId(nation.id) == null)
            {
                Debug.LogWarning($"[ContentLibraryValidator] Nation '{nation.id}' has no theme in '{lib.name}': its culture stays neutral.", lib);
                issues++;
            }
        }

        List<ThemeSO> themes = lib.Themes.Prepend(neutral).Where(t => t != null).ToList();
        foreach (string id in themes.GroupBy(t => t.cultureId).Where(g => g.Count() > 1).Select(g => g.Key))
            Error($"Culture id '{id}' has more than one theme");

        List<ResolvedRole> neutralRoles = Roles(neutral);
        foreach (ThemeSO theme in themes)
        {
            bool isNeutral = theme == neutral;
            UiStringTableSO table = lib.GetStringTable(theme.language);
            if (table == null)
                Error($"Theme '{theme.cultureId}' has no UI string table for its language '{theme.language}'");
            else if (table != reading)
                foreach (string problem in UiStrings.TableProblems(reading.entries, table.entries, table.rightToLeft))
                    Error($"UI string table '{table.language}': {problem}");

            List<ResolvedRole> roles = Roles(theme);
            foreach (ThemeRoleId missing in Palette.Missing(roles, isNeutral))
                Error($"Theme '{theme.cultureId}' has no colour for role '{missing}'");
            if (!isNeutral)
                foreach (ResolvedRole r in roles.Where(r => ThemeRoles.IsDiegetic(r.Role)))
                    Error($"Theme '{theme.cultureId}' colours the diegetic role '{r.Role}' (only the neutral theme may)");

            List<ContrastPair> pairs = Palette.Pairs(roles).Concat(Palette.DiegeticPairs(roles, neutralRoles)).ToList();
            foreach (string problem in Contrast.Problems(pairs, ToRgba(theme.ringDark), ToRgba(theme.ringLight), ui.contrast ?? new ContrastRules()))
                Error($"Theme '{theme.cultureId}': {problem}");

            if (theme.runtimeFont && table != null && table.entries.Any(e => CultureChoice.NeedsOsFont(e.text)) && theme.fonts.Count == 0)
                Error($"Theme '{theme.cultureId}' has labels the Latin fallback cannot draw but no font candidate");

            if (theme.wallpaper == null)
            {
                Debug.LogWarning($"[ContentLibraryValidator] Theme '{theme.cultureId}' has no wallpaper in '{lib.name}': the desktop shows its plain colour.", theme);
                issues++;
            }
        }

        return issues;
    }

    /// <summary>
    /// The Orders app's upgrade tree (Saleh 2026-09-29): what Generate World
    /// also checks (UpgradeTree.Problems over every library upgrade: unknown
    /// or cyclic prerequisites, a prerequisite at another venue, a branch of
    /// the other venue, a negative cost), an install slot only on an Orders
    /// upgrade (Home's are owned at once and never swapped), and Home's radio
    /// (HomeContent.Problems: its lines, its upgrade among Home's).
    /// </summary>
    private static int CheckUpgradeTree(ContentLibrarySO lib)
    {
        int issues = 0;
        foreach (string problem in UpgradeTree.Problems(OrderBook.Nodes(lib)))
        {
            Debug.LogError($"[ContentLibraryValidator] Upgrades: {problem} ('{lib.name}'; edit the upgrade's Inspector fields, or translation.packs[].requires and Generate World for a translator).", lib);
            issues++;
        }
        foreach (UpgradeSO u in lib.Upgrades)
            if (u != null && u.venue == UpgradeVenue.Home && !string.IsNullOrEmpty(u.installSlot))
            {
                Debug.LogError($"[ContentLibraryValidator] Upgrades: '{u.id}' is sold at Home but has the install slot '{u.installSlot}'; only an Orders upgrade can be swapped.", u);
                issues++;
            }
        foreach (string problem in lib.Home.Problems(lib.Upgrades.Where(u => u != null && u.venue == UpgradeVenue.Home).Select(u => u.id)))
        {
            Debug.LogError($"[ContentLibraryValidator] Home: {problem} ('{lib.name}'; edit world_source.json \"home\" and Generate World).", lib);
            issues++;
        }
        return issues;
    }

    /// <summary>
    /// Translation (piece 9, speech only): the rules Generate World also
    /// checks, over the library's settings and every place's tongue
    /// (TranslationSettings.Problems), every pack's Speech translator among
    /// the upgrades, and the notice trigger when foreign speech starts after
    /// day 1. A library with no translation data is one error (every tongue
    /// would read as English).
    /// </summary>
    private static int CheckTranslation(ContentLibrarySO lib)
    {
        int issues = 0;
        void Error(string message)
        {
            Debug.LogError($"[ContentLibraryValidator] Translation: {message.TrimEnd('.')} in '{lib.name}' (Tools > TimeDesk > Generate World writes the translation).", lib);
            issues++;
        }

        TranslationSettings translation = lib.Translation;
        if (!translation.HasData)
        {
            Error("no translation data, so every tongue reads as English");
            return issues;
        }

        foreach (string problem in translation.Problems(lib.Profiles.Where(p => p != null).Select(p => new KeyValuePair<string, string>(p.id, p.tongue))))
            Error(problem);

        foreach (TranslatorPack pack in (translation.rules.packs ?? new List<TranslatorPack>()).Where(p => p != null && !string.IsNullOrWhiteSpace(p.id)))
            if (lib.GetUpgradeById(Translation.UpgradeId(pack.id)) == null)
                Error($"pack '{pack.id}' has no '{Translation.UpgradeId(pack.id)}' upgrade");

        if (translation.rules.fromDay > 1 && !lib.Triggers.Any(t => t != null && t.id == WorldContentGenerator.TranslationNoticeId))
            Error($"foreign speech starts on day {translation.rules.fromDay} but no '{WorldContentGenerator.TranslationNoticeId}' trigger announces it");
        return issues;
    }

    /// <summary>A stored theme's palette as resolved roles (for the shared contrast pairs).</summary>
    private static List<ResolvedRole> Roles(ThemeSO theme) =>
        theme.palette.Where(e => e != null).Select(e => new ResolvedRole
        {
            Role = e.role,
            Fill = e.hasFill ? ToRgba(e.fill) : (Rgba?)null,
            Ink = e.hasInk ? ToRgba(e.ink) : (Rgba?)null,
            TextClass = e.textClass
        }).ToList();

    /// <summary>A theme colour from an engine colour.</summary>
    private static Rgba ToRgba(Color c) => new Rgba(c.r, c.g, c.b, c.a);

    /// <summary>Fact categories every place must have (papers + books + questions + dress).</summary>
    private static readonly ClueCategory[] RequiredFacts =
        { ClueCategory.Currency, ClueCategory.Language, ClueCategory.Technology, ClueCategory.Geography, ClueCategory.Politics, ClueCategory.Culture };

    /// <summary>
    /// Reports places with missing or duplicate facts, no names, unset or
    /// inverted birth years, no year, a Culture fact that is not the one the
    /// wardrobe gives (or wider than a book row), a gender look without outfit,
    /// hair or signature item, an item art nation that is not a key token,
    /// look weights with no positive sum, and (a warning) a signature that is
    /// the whole outfit, which can never leak (except on a Future place, whose
    /// culture-shaped outfit is its signature by design).
    /// </summary>
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

            if (!BirthDates.HasYears(place.birthYearMin, place.birthYearMax))
            {
                Debug.LogError($"[ContentLibraryValidator] Place '{place.name}' has no birth years (0..0); its visitors are born 'Unknown' and their birth dates can never carry a birth-date tell.", place);
                issues++;
            }
            else if (place.birthYearMin > place.birthYearMax)
            {
                Debug.LogError($"[ContentLibraryValidator] Place '{place.name}' has birthYearMin {place.birthYearMin} > birthYearMax {place.birthYearMax}.", place);
                issues++;
            }

            issues += CheckPlaceLook(place, lib);
        }

        return issues;
    }

    /// <summary>A place's look: year, the derived Culture fact, both genders' looks, their items' art nations and the weights.</summary>
    private static int CheckPlaceLook(NationEraProfileSO place, ContentLibrarySO lib)
    {
        int issues = 0;
        void Error(string message)
        {
            Debug.LogError($"[ContentLibraryValidator] Place '{place.name}' {message} in '{lib.name}'.", place);
            issues++;
        }

        if (place.year == 0)
            Error("has no year (travellers' ages are measured against it; run Tools > TimeDesk > Generate World)");

        string culture = place.facts?.FirstOrDefault(f => f != null && f.category == Looks.EvidenceCategory)?.value;
        string derived = Looks.CultureValue(place.wardrobe);
        if (derived == null || culture != derived)
            Error($"has Culture '{culture}', but its wardrobe gives '{derived}' (run Tools > TimeDesk > Generate World)");
        else if (culture.Length > FactTable.MaxValueLength)
            Error($"has Culture '{culture}', wider than a book row ({FactTable.MaxValueLength} characters)");

        foreach (TravellerGender gender in new[] { TravellerGender.Male, TravellerGender.Female })
        {
            GenderLook look = place.wardrobe?.For(gender);
            if (look == null || !look.outfit.IsPresent || !look.hair.IsPresent || look.Signature == null || !look.Signature.IsPresent)
            {
                Error($"has no {gender} outfit, hair or signature item");
                continue;
            }

            // A Future place's signature is its outfit by design, so a Future home never leaks dress (characters spec R27).
            if (look.signature == LookSlot.Outfit && (place.era == null || !place.era.isFuture))
            {
                Debug.LogWarning($"[ContentLibraryValidator] Place '{place.name}' has the whole outfit as its {gender} signature, so it can never leak as a dress tell ('{lib.name}').", place);
                issues++;
            }

            foreach (LookSlot slot in Looks.Slots)
            {
                LookItem item = look.Item(slot);
                if (item != null && item.IsPresent && !string.IsNullOrEmpty(item.artNation) && !LookKeys.IsToken(item.artNation))
                    Error($"files its {gender} {Looks.SlotLabel(slot)} '{item.label}' under art nation '{item.artNation}', which is not a key token (lowercase letters and digits)");
            }
        }

        if (place.looks == null || place.looks.skin == null || place.looks.skin.Sum() <= 0f || place.looks.hair == null || place.looks.hair.Sum(h => h != null ? h.weight : 0f) <= 0f)
            Error("has look weights (skin or hair) with no positive sum");

        return issues;
    }

    /// <summary>
    /// Reports day plans whose weighted eras have no place today (eras x allowed
    /// nations), a day weighting the Future era (never a destination, traveller
    /// types H2) or a rule naming it (it would forbid nothing), rules no place
    /// of the day can break, premades (pooled or forced) whose claim or true
    /// place is outside the day's world, forced slots beyond the queue, a
    /// premade forced twice, the first half's room (a warning when the forced
    /// slots leave fewer free slots than the day's guarantees,
    /// ViolatorSlots.RoomProblems: a violator would be dropped), and
    /// a day allowing dress tells without a Costume Guide.
    /// </summary>
    private static int CheckDayPlanPlaces(ContentLibrarySO lib)
    {
        int issues = 0;
        EraSO future = lib.FutureEra;

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
                if (w.era == null || w.weight <= 0f)
                    continue;

                if (w.era == future)
                {
                    Debug.LogError($"[ContentLibraryValidator] Day plan '{plan.name}' weights the Future era: the Future is the present, never a destination (traveller types H2), so the weight draws nobody.", plan);
                    issues++;
                }
                else if (today.All(p => p.era != w.era))
                {
                    Debug.LogError($"[ContentLibraryValidator] Day plan '{plan.name}' weights era '{w.era.id}' but none of its allowed nations has a place there.", plan);
                    issues++;
                }
            }

            foreach (TravelRuleSO rule in plan.ActiveTravelRules)
            {
                if (rule != null && future != null && rule.era == future)
                {
                    Debug.LogError($"[ContentLibraryValidator] Day plan '{plan.name}' uses rule '{rule.name}', which names the Future era: the Future is never a destination, so the rule forbids nothing.", plan);
                    issues++;
                }
            }

            foreach (TravelRuleSO rule in plan.ActiveTravelRules)
            {
                if (rule != null && rule.IsClosure && today.All(p => rule.Allows(p.nation, p.era)))
                {
                    Debug.LogWarning($"[ContentLibraryValidator] Day plan '{plan.name}' uses rule '{rule.name}', which forbids none of the day's places (no traveller can break it).", plan);
                    issues++;
                }
            }

            // The Directives (phase 9; the rule Generate World checks its source with): each rule's shape, and the day's
            // rolled procedures and guarantees against the kinds of the day (Directives.RuleProblems, DayProblems).
            foreach (TravelRuleSO rule in plan.ActiveTravelRules.Where(r => r != null).Distinct())
                foreach (string problem in Directives.RuleProblems(rule.name, rule.type, rule.kinds, rule.nation != null || rule.era != null, !string.IsNullOrWhiteSpace(rule.description),
                                                                   rule.transponder, lib.Agency.transponders, rule.openPlaces))
                {
                    Debug.LogError($"[ContentLibraryValidator] {problem} (run Tools > TimeDesk > Generate World)", rule);
                    issues++;
                }
            List<Directives.RuleEntry> active = plan.ActiveTravelRules.Where(r => r != null).Select(r => new Directives.RuleEntry(r.name, r.type, r.kinds, lib.FirstDayOf(r), r.transponder)).ToList();
            var kinds = plan.Kinds.Where(k => k != null && k.blueprint != null && k.weight > 0f)
                .Select(k => (k.blueprint.Kind, (IReadOnlyCollection<string>)plan.TemplatesOf(k.blueprint).Select(t => t.formNumber).ToList()))
                .ToList();
            foreach (string problem in Directives.DayProblems(plan.name, plan.DayNumber, active, kinds, lib.Agency.transponders))
            {
                Debug.LogError($"[ContentLibraryValidator] {problem} (run Tools > TimeDesk > Generate World)", plan);
                issues++;
            }

            var forced = plan.ForcedCases.Where(f => f != null && f.legendary != null).ToList();
            foreach (LegendarySO legend in (plan.AvailableLegendaries ?? Array.Empty<LegendarySO>()).Concat(forced.Select(f => f.legendary)))
            {
                if (legend == null || legend.nation == null || legend.trueEra == null)
                    continue;

                if (!today.Any(p => p.nation == legend.nation && p.era == legend.trueEra))
                {
                    Debug.LogWarning($"[ContentLibraryValidator] Day plan '{plan.name}' lists premade '{legend.displayName}' whose place ({legend.nation.id}, {legend.trueEra.id}) is not in the day's world; their papers would print placeholders.", plan);
                    issues++;
                }

                if (legend.truePlace != null && !today.Contains(legend.truePlace))
                {
                    Debug.LogWarning($"[ContentLibraryValidator] Day plan '{plan.name}' lists premade '{legend.displayName}', authored as a liar from '{legend.truePlace.name}', which is not in the day's world; they would stay honest.", plan);
                    issues++;
                }
            }

            foreach (ForcedCaseSlot slot in forced)
            {
                if (slot.caseIndex1Based < 1 || slot.caseIndex1Based > plan.VisitorsCount)
                {
                    Debug.LogError($"[ContentLibraryValidator] Day plan '{plan.name}' forces premade '{slot.legendary.displayName}' into slot {slot.caseIndex1Based}, outside its queue of {plan.VisitorsCount}.", plan);
                    issues++;
                }

            }

            if (plan.GuaranteeRuleViolators)
            {
                IEnumerable<int> standing = plan.ForcedCases.Where(f => f != null && (f.legendary != null || f.hasLie || f.directive != PlannedDirective.None)).Select(f => f.caseIndex1Based);
                int guarantees = plan.ActiveTravelRules.Where(r => r != null).Distinct().Count(r => Directives.Guarantees(r.type, plan.DayNumber, lib.FirstDayOf(r)));
                foreach (string problem in ViolatorSlots.RoomProblems(plan.name, plan.VisitorsCount, standing, guarantees))
                {
                    Debug.LogWarning($"[ContentLibraryValidator] {problem}", plan);
                    issues++;
                }
            }


            if (plan.TellChannels.Contains(TellChannel.Appearance) && !lib.ReferenceBookCategories().Contains(Looks.EvidenceCategory))
            {
                Debug.LogWarning($"[ContentLibraryValidator] Day plan '{plan.name}' allows dress tells, but no reference book covers Culture (the Costume Guide), so none can be proven or generated.", plan);
                issues++;
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

    /// <summary>
    /// Reports the day plans' identity and size problems (DayPlans.Problems,
    /// the rule Generate World checks its source with: a blank or repeated
    /// asset name, a day below 1 or planned twice, a queue below 1), gaps in
    /// the day sequence and an unplanned tail up to the run's last day
    /// (DayPlans.Unplanned, days 7-15 V1).
    /// </summary>
    private static int CheckDayPlanEntries(ContentLibrarySO lib)
    {
        List<string> problems = DayPlans.Problems(lib.DayPlans
            .Where(p => p != null)
            .Select(p => new DayPlanEntry(p.name, p.DayNumber, p.VisitorsCount))
            .ToList());
        foreach (string problem in problems)
            Debug.LogError($"[ContentLibraryValidator] {problem} ('{lib.name}')", lib);
        int issues = problems.Count;

        foreach (string gap in DayPlans.Gaps(lib.DayPlans.Where(p => p != null).Select(p => p.DayNumber))
                         .Concat(DayPlans.Unplanned(lib.DayPlans.Where(p => p != null).Select(p => p.DayNumber), lib.LastDay)))
        {
            Debug.LogWarning($"[ContentLibraryValidator] {gap} ('{lib.name}')", lib);
            issues++;
        }

        return issues;
    }

    /// <summary>
    /// The forced entries' faults, ids, alternatives, dialogs and conditions
    /// (days 7-15 V2-V5, Premades.ForcedProblems: the rule Generate World
    /// checks its source with), over every day plan: errors, then warnings.
    /// </summary>
    private static int CheckForcedEntries(ContentLibrarySO lib)
    {
        var days = new List<ForcedDayCheck>();
        foreach (DayPlanSO plan in lib.DayPlans.Where(p => p != null))
        {
            var forced = new List<ForcedCheck>();
            foreach (ForcedCaseSlot f in plan.ForcedCases.Where(f => f != null))
            {
                TravellerKind kind = f.legendary != null ? f.legendary.kind : f.caseBlueprint != null ? f.caseBlueprint.Kind : TravellerKind.Displaced;
                CaseBlueprintSO blueprint = f.caseBlueprint ?? plan.Kinds.Where(k => k != null && k.blueprint != null && k.blueprint.Kind == kind).Select(k => k.blueprint).FirstOrDefault();
                forced.Add(new ForcedCheck
                {
                    Slot = f.caseIndex1Based,
                    Id = f.id,
                    Premade = f.legendary != null ? f.legendary.id : null,
                    Kind = kind,
                    Forms = plan.TemplatesOf(blueprint).Select(t => t.formNumber).ToList(),
                    HasTruePlace = f.legendary != null && f.legendary.truePlace != null,
                    OncePerRun = f.legendary != null && f.legendary.oncePerRun,
                    ClosedPlace = f.legendary != null && !plan.ClaimAllowed(f.legendary.nation, f.legendary.trueEra, kind),
                    Lie = f.hasLie ? f.lie : (LieKind?)null,
                    Directive = f.directive,
                    Dialog = f.dialogId,
                    ConditionKeys = (f.conditions ?? new List<TriggerCondition>()).Where(c => c != null && !string.IsNullOrEmpty(c.key)).Select(c => c.key).ToList(),
                    Conditions = (f.conditions ?? new List<TriggerCondition>()).Count(c => c != null)
                });
            }

            days.Add(new ForcedDayCheck
            {
                Asset = plan.name,
                Day = plan.DayNumber,
                Lies = plan.EnabledLies,
                Rules = plan.ActiveTravelRules.Where(r => r != null).Select(r => r.Directive).ToList(),
                Pooled = (plan.AvailableLegendaries ?? Array.Empty<LegendarySO>()).Where(l => l != null).Select(l => l.id).ToList(),
                Forced = forced
            });
        }

        var errors = new List<string>();
        var warnings = new List<string>();
        Premades.ForcedProblems(days, lib.Legendaries.Where(l => l != null).Select(l => l.id).ToList(),
                                lib.Dialogs.Where(d => d != null && d.dialog != null).Select(d => d.dialog.id).ToList(), errors, warnings);
        foreach (string e in errors)
            Debug.LogError($"[ContentLibraryValidator] {e} (run Tools > TimeDesk > Generate World)", lib);
        foreach (string w in warnings)
            Debug.LogWarning($"[ContentLibraryValidator] {w}", lib);
        return errors.Count + warnings.Count;
    }

    /// <summary>
    /// The premades' rows (days 7-15 V6, Premades.Problems: the rule Generate
    /// World checks its source with): the famous hold no account; a story
    /// character's birth years, family, Citizen ID, debt, employer and that no
    /// day pools it.
    /// </summary>
    private static int CheckPremadeRows(ContentLibrarySO lib)
    {
        var employers = (lib.Agency.employers ?? new List<Employer>()).Where(e => e != null && !string.IsNullOrEmpty(e.id)).GroupBy(e => e.id).ToDictionary(g => g.Key, g => g.First());
        var pooled = new HashSet<LegendarySO>(lib.DayPlans.Where(p => p != null).SelectMany(p => p.AvailableLegendaries ?? Array.Empty<LegendarySO>()).Where(l => l != null));
        List<PremadeCheck> checks = lib.Legendaries.Where(l => l != null).Select(l => new PremadeCheck
        {
            Id = l.id,
            Kind = l.kind,
            HasTruePlace = l.truePlace != null,
            PlaceEraId = l.trueEra != null ? l.trueEra.id : null,
            BirthYear = BirthDates.TryParse(l.birthDate, out _, out _, out int born) ? born : (int?)null,
            HasFamily = l.family != null,
            FamilyKnown = true,
            CitizenId = l.citizenId,
            Debt = l.debt,
            Employer = l.employer,
            EmployerEraId = !string.IsNullOrEmpty(l.employer) && employers.TryGetValue(l.employer, out Employer e) ? e.era : null,
            Pooled = pooled.Contains(l)
        }).ToList();

        PresentPlace present = lib.BuildPresent(null);
        var errors = new List<string>();
        var warnings = new List<string>();
        Premades.Problems(checks, present != null ? present.BirthYearMin : 0, present != null ? present.BirthYearMax : 0,
                          lib.Agency.clerk != null ? lib.Agency.clerk.citizenId : null, lib.Agency.accounts, errors, warnings);
        foreach (string problem in errors)
            Debug.LogError($"[ContentLibraryValidator] {problem} (run Tools > TimeDesk > Generate World)", lib);
        foreach (string problem in warnings)
            Debug.LogWarning($"[ContentLibraryValidator] {problem}", lib);
        return errors.Count + warnings.Count;
    }

    /// <summary>Reports DayPlan.AvailableLegendaries entries that are null.</summary>
    private static int CheckDayPlanLegendaries(ContentLibrarySO lib)
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
                }
            }
        }

        return issues;
    }

    /// <summary>
    /// Reports premades with missing era, archetype or nation references, an
    /// id that is not a key token, a blank name, an unreadable birth date or,
    /// for the famous, one outside the claimed place's birth years (a 2150
    /// story character is born in the present's, CheckPremadeRows, days 7-15
    /// V6), a true place equal to the
    /// claim, or a record note too long for the Records box.
    /// </summary>
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

            void Error(string message)
            {
                Debug.LogError($"[ContentLibraryValidator] {label} {message} in '{lib.name}'.", legend);
                issues++;
            }

            if (!LookKeys.IsToken(legend.id))
                Error($"has id '{legend.id}', which is not a key token (lowercase letters and digits)");
            if (string.IsNullOrWhiteSpace(legend.displayName))
                Error("has a blank name");

            NationEraProfileSO claim = legend.nation != null && legend.trueEra != null ? lib.GetProfile(legend.nation, legend.trueEra) : null;
            if (!BirthDates.TryParse(legend.birthDate, out _, out _, out int year))
                Error($"has an unreadable birth date '{legend.birthDate}'");
            else if (claim != null && Premades.IsFamous(legend.kind) && (year < claim.birthYearMin || year > claim.birthYearMax))
                Error($"is born in {year}, outside '{claim.name}''s birth years {claim.birthYearMin}..{claim.birthYearMax}");

            if (legend.truePlace != null && legend.truePlace == claim)
                Error("has its claimed place as its true place");
            if ((legend.recordNote ?? string.Empty).Length > Premades.MaxNoteLength)
                Error($"has a record note longer than {Premades.MaxNoteLength} characters");
        }

        return issues;
    }

    /// <summary>
    /// Reports what makes dress tells unfair or impossible: two places sharing
    /// a Culture value (a dress proof must name one place) and item labels that
    /// read as another place's signature for the same gender (Looks.LabelProblems).
    /// </summary>
    private static int CheckCultureUnique(ContentLibrarySO lib)
    {
        int issues = 0;
        var seen = new List<(NationEraProfileSO place, string value)>();
        foreach (NationEraProfileSO place in lib.Profiles)
        {
            string value = place?.facts?.FirstOrDefault(f => f != null && f.category == Looks.EvidenceCategory)?.value;
            if (string.IsNullOrWhiteSpace(value))
                continue;

            foreach ((NationEraProfileSO other, string otherValue) in seen)
            {
                if (Values.Match(value, otherValue))
                {
                    Debug.LogError($"[ContentLibraryValidator] Places '{other.name}' and '{place.name}' share the Culture value '{value}' in '{lib.name}'; a dress proof must name one place.", place);
                    issues++;
                }
            }

            seen.Add((place, value));
        }

        var wardrobes = lib.Profiles.Where(p => p != null).Select(p => (p.id, p.wardrobe)).ToList();
        foreach (string problem in Looks.LabelProblems(wardrobes))
        {
            Debug.LogError($"[ContentLibraryValidator] Wardrobe labels in '{lib.name}': {problem}", lib);
            issues++;
        }

        return issues;
    }

    /// <summary>
    /// Logs (never counted as an issue) how many character art keys have art
    /// at CharacterArt.AssetFolder: the bases, every place's garments, the
    /// present's clothes and 2150 accessory kit, and every premade's
    /// expressions, each distinct name once (a drawing places share through an
    /// item's artNation counts once), with the first 20 missing names (at
    /// runtime each is drawn with its nearest stand-in, CharacterArtFallbackSO,
    /// or not at all); and warns when that table is missing or leaves a
    /// nation without neighbours.
    /// </summary>
    private static void ReportCharacterArt(ContentLibrarySO lib)
    {
        var keys = new List<string>(LookKeys.Bases(lib.LookRules));
        foreach (NationEraProfileSO place in lib.Profiles)
            if (place != null && place.nation != null && place.era != null)
                keys.AddRange(LookKeys.Required(place.nation.id, place.era.id, place.wardrobe));
        if (lib.FutureEra != null)
            keys.AddRange(LookKeys.PresentRequired(Present.NeutralNationId, lib.FutureEra.id, lib.PresentLook));
        foreach (LegendarySO premade in lib.Legendaries)
            if (premade != null)
                keys.AddRange(LookKeys.PremadeSet(premade.id));

        List<string> distinct = keys.Distinct().ToList();
        List<string> missing = distinct.Where(k => !System.IO.File.Exists($"{CharacterArt.AssetFolder}/{k}.png")).ToList();
        int total = distinct.Count;
        Debug.Log($"[ContentLibraryValidator] Character art: {total - missing.Count}/{total} key(s) have art in {CharacterArt.AssetFolder}; the rest are drawn with their nearest stand-in (CharacterArtFallback) or not at all{(missing.Count > 0 ? $" (first missing: {string.Join(", ", missing.Take(20))})" : string.Empty)}.");

        LookArtFallbackTable table = Resources.Load<CharacterArtFallbackSO>(CharacterArtFallbackSO.ResourcePath)?.table;
        if (table == null)
            Debug.LogWarning($"[ContentLibraryValidator] No character art fallback table at Resources/{CharacterArtFallbackSO.ResourcePath}: a layer with no art is not drawn.");
        else
            foreach (NationSO nation in lib.Nations)
                if (nation != null && table.NeighboursOf(nation.id).Count == 0)
                    Debug.LogWarning($"[ContentLibraryValidator] The character art fallback table lists no neighbours for '{nation.id}': its garments without art only borrow from its own other eras (and AnyPlace).");
    }

    /// <summary>Reports look rules a traveller's look cannot be composed from: a face band with no face, no grey age, no premade garment label.</summary>
    private static int CheckLookRules(ContentLibrarySO lib)
    {
        int issues = 0;
        void Error(string message)
        {
            Debug.LogError($"[ContentLibraryValidator] {message} in '{lib.name}' (run Tools > TimeDesk > Generate World).", lib);
            issues++;
        }

        LookRules rules = lib.LookRules ?? new LookRules();
        if (rules.faceBands == null || rules.faceBands.Count == 0)
            Error("Look rules have no face bands");
        else if (rules.faceBands.Any(b => b == null || b.faces == null || b.faces.Count == 0))
            Error("A look-rule face band has no face");
        if (rules.greyFromAge <= 0)
            Error("Look rules have no grey age (greyFromAge must be above 0)");
        if (string.IsNullOrWhiteSpace(rules.wholeFigureLabel))
            Error("Look rules have no whole-figure label (a premade's garment)");
        CostumeErrorWeights w = rules.costumeErrors ?? new CostumeErrorWeights();
        if (w.otherPlace + w.presentClothes + w.presentAccessory <= 0f)
            Error("Look rules have no costume error weights (looks.costumeErrors), so a rolled costume error can never show");
        PresentLook present = lib.PresentLook;
        if (present.wardrobe == null || !present.wardrobe.male.Signature.IsPresent || !present.wardrobe.female.Signature.IsPresent)
            Error("The present has no clothes (present.wardrobe), so no 2150 citizen can wear them by mistake");
        if (present.Kit(TravellerGender.Male).Count == 0 || present.Kit(TravellerGender.Female).Count == 0)
            Error("The present's 2150 accessory kit (present.kit) is missing a gender");
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
