using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Tools > TimeDesk > Generate World. The one authoritative world generator:
/// reads the hand-maintained world source (Assets/Data/World/world_source.json)
/// and creates or updates the eras, nations, places (NationEraProfileSO with
/// facts, names, birth years and small talk; the eight Future places), travel
/// rules and day plans (tell count and tell channels), the interview (its
/// wording and menu capacity, questions, narrative dialogs, and a one-shot
/// unlock-announcement trigger for every gated question), the history (one
/// leader effect per country, one one-shot trigger and SetFact effect per
/// history rule, the templated history lines), points the case blueprint at
/// the listed archetypes, then sets every world array of the content library
/// explicitly. Idempotent: re-running converges to the source file. It owns
/// the Eras/Nations/Places/Rules/Interview/History folders under
/// Assets/Data/World (assets there that the source no longer lists go to the
/// OS trash), merges its generated triggers and effects after the
/// hand-authored ones and only drops missing references elsewhere, so
/// hand-authored content (legendaries, effects, triggers) survives a re-run.
/// The authored assets the source points at (library, blueprint, attributes,
/// archetypes, books) must already exist; every reference, id and line is
/// checked before anything is written.
/// </summary>
public static class WorldContentGenerator
{
    /// <summary>The researched world data.</summary>
    private const string SourcePath = "Assets/Data/World/world_source.json";

    /// <summary>Folder for generated world assets.</summary>
    private const string WorldRoot = "Assets/Data/World";

    /// <summary>Generator-owned folders (under <see cref="WorldRoot"/>).</summary>
    private static readonly string[] OwnedFolders = { "Eras", "Nations", "Places", "Rules", "Interview", "History" };

    /// <summary>Folder of the generated interview assets (questions, dialogs, unlock triggers).</summary>
    private const string InterviewFolder = WorldRoot + "/Interview";

    /// <summary>Folder of the generated history assets (leader effects, history-rule triggers and effects).</summary>
    private const string HistoryFolder = WorldRoot + "/History";

    /// <summary>Reads the source, checks every reference, then writes the world. Aborts (writing nothing) on any error.</summary>
    [MenuItem("Tools/TimeDesk/Generate World")]
    public static void Generate()
    {
        WorldSource src = LoadSource();
        if (src == null)
            return;

        var errors = new List<string>();
        Authored authored = LoadAuthored(src.content, errors);
        CheckReferences(src, authored, errors);
        CheckHistory(src, authored, errors);
        CheckInterview(src, authored, errors);
        if (errors.Count > 0)
        {
            foreach (string e in errors)
                Debug.LogError($"[WorldContentGenerator] {e}");
            Debug.LogError($"[WorldContentGenerator] Aborted with {errors.Count} error(s); nothing was changed.");
            return;
        }

        foreach (string folder in OwnedFolders)
            EnsureFolder($"{WorldRoot}/{folder}");

        var written = new HashSet<string>();

        // --- Eras, leader effects and nations ---
        var eras = src.eras.ToDictionary(e => e.id, e => MakeEra(e, written));
        EffectSO[] leaderEffects = src.countries.Select(c => MakeLeaderEffect(c, written)).ToArray();
        var nations = src.countries.Select((c, i) => (c, i)).ToDictionary(x => x.c.id, x => MakeNation(x.c, leaderEffects[x.i], written));

        // --- Places ---
        var places = src.places
            .Select(p => MakePlace(p, nations[p.country], eras[p.era], src.countries.First(c => c.id == p.country),
                                   authored.attributes, src.travellerAgeMin, src.travellerAgeMax, written))
            .ToArray();
        var refs = new ConditionRefs(places.ToDictionary(p => p.id), authored.attributes, nations);

        // --- History rules: one SetFact effect and one one-shot trigger each ---
        HistoryRuleData[] ruleData = src.history?.rules ?? Array.Empty<HistoryRuleData>();
        EffectSO[] historyEffects = ruleData.Select(r => MakeHistoryEffect(r, refs, written)).ToArray();
        TimelineTriggerSO[] historyTriggers = ruleData.Select((r, i) => MakeHistoryTrigger(r, historyEffects[i], refs, written)).ToArray();

        // --- Rules, blueprint, day plans ---
        var rules = src.rules.ToDictionary(r => r.asset, r => MakeRule(r, nations, eras, written));

        var soBlueprint = new SerializedObject(authored.blueprint);
        SetArray(soBlueprint, "archetypePool", authored.archetypes);
        soBlueprint.ApplyModifiedProperties();
        EditorUtility.SetDirty(authored.blueprint);

        DayPlanSO[] days = src.days.Select(d => MakeDay(d, src.content.dayPlanFolder, authored.blueprint, eras, nations, rules)).ToArray();

        // --- Interview: questions, dialogs, unlock announcements ---
        QuestionData[] questionData = src.questions ?? Array.Empty<QuestionData>();
        QuestionSO[] questions = questionData.Select(q => MakeQuestion(q, refs, written)).ToArray();
        DialogSO[] dialogs = (src.dialogs ?? Array.Empty<DialogData>()).Select(d => MakeDialog(d, refs, written)).ToArray();
        TimelineTriggerSO[] unlocks = questionData.Where(IsGated).Select(q => MakeUnlockTrigger(q, refs, written)).ToArray();

        // Re-saving the book covers keeps their YAML in the current shape.
        foreach (ReferenceBookSO book in authored.books)
            EditorUtility.SetDirty(book);

        WireLibrary(authored.library, days, src.eras.Select(e => eras[e.id]).ToArray(), src.countries.Select(c => nations[c.id]).ToArray(),
                    places, authored.archetypes, src.content.attributes.Select(a => authored.attributes[a.id]).ToArray(), authored.books,
                    BuildLines(src.interview), questions, dialogs, unlocks, BuildHistoryLines(src.history?.lines),
                    historyTriggers, historyEffects, leaderEffects);

        int pruned = PruneOwnedFolders(written);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        int futurePlaces = src.places.Count(p => src.eras.Any(e => e.future && e.id == p.era));
        Debug.Log($"[WorldContentGenerator] World generated: {eras.Count} eras, {nations.Count} nations, {places.Length} places ({futurePlaces} Future), {rules.Count} rules, {days.Length} day plans, {questions.Length} questions, {dialogs.Length} dialogs, {unlocks.Length} unlock triggers, {historyTriggers.Length} history rules, {leaderEffects.Length} leader effects; {pruned} unlisted generated asset(s) moved to the trash.");
    }

    // -----------------------------
    // Checks (nothing is written until these pass)
    // -----------------------------

    /// <summary>The authored assets the source points at.</summary>
    private sealed class Authored
    {
        public ContentLibrarySO library;
        public CaseBlueprintSO blueprint;
        public Dictionary<string, AttributeSO> attributes = new Dictionary<string, AttributeSO>();
        public ArchetypeSO[] archetypes;
        public ReferenceBookSO[] books;
    }

    private static Authored LoadAuthored(ContentData content, List<string> errors)
    {
        var a = new Authored();
        if (content == null)
        {
            errors.Add($"'{SourcePath}' has no \"content\" section (library, blueprint, dayPlanFolder, attributes, archetypes, books).");
            return a;
        }

        a.library = Require<ContentLibrarySO>(content.library, "content library", errors);
        a.blueprint = Require<CaseBlueprintSO>(content.blueprint, "case blueprint", errors);
        if (!AssetDatabase.IsValidFolder(content.dayPlanFolder ?? string.Empty))
            errors.Add($"Day plan folder '{content.dayPlanFolder}' does not exist.");

        foreach (AttributeData attr in content.attributes ?? Array.Empty<AttributeData>())
        {
            var so = Require<AttributeSO>(attr.asset, $"attribute '{attr.id}'", errors);
            if (so != null)
                a.attributes[attr.id] = so;
        }

        a.archetypes = (content.archetypes ?? Array.Empty<string>()).Select(p => Require<ArchetypeSO>(p, "archetype", errors)).Where(x => x != null).ToArray();
        a.books = (content.books ?? Array.Empty<string>()).Select(p => Require<ReferenceBookSO>(p, "reference book", errors)).Where(x => x != null).ToArray();
        return a;
    }

    private static T Require<T>(string path, string what, List<string> errors) where T : Object
    {
        T asset = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
            errors.Add($"Missing {what} at '{path}'.");
        return asset;
    }

    /// <summary>Every id the source uses must resolve; enum strings must parse.</summary>
    private static void CheckReferences(WorldSource src, Authored authored, List<string> errors)
    {
        var eraIds = new HashSet<string>(src.eras.Select(e => e.id));
        var countryIds = new HashSet<string>(src.countries.Select(c => c.id));
        var ruleIds = new HashSet<string>(src.rules.Select(r => r.asset));

        if (eraIds.Count != src.eras.Length || eraIds.Any(string.IsNullOrWhiteSpace))
            errors.Add("Era ids must be unique and non-blank.");
        if (countryIds.Count != src.countries.Length || countryIds.Any(string.IsNullOrWhiteSpace))
            errors.Add("Country ids must be unique and non-blank.");
        if (ruleIds.Count != src.rules.Length || ruleIds.Any(string.IsNullOrWhiteSpace))
            errors.Add("Rule asset names must be unique and non-blank.");

        foreach (CountryData c in src.countries)
            foreach (BaselineData b in c.baselines ?? Array.Empty<BaselineData>())
                if (!authored.attributes.ContainsKey(b.attribute))
                    errors.Add($"Country '{c.id}' has a baseline for unknown attribute '{b.attribute}'.");

        var placeKeys = new HashSet<string>();
        foreach (PlaceData p in src.places)
        {
            if (!countryIds.Contains(p.country) || !eraIds.Contains(p.era))
                errors.Add($"Place '{p.displayName}' references unknown country '{p.country}' or era '{p.era}'.");
            if (!placeKeys.Add(PlaceId(p)))
                errors.Add($"Place '{PlaceId(p)}' is listed twice.");
            foreach (FactData f in p.facts ?? Array.Empty<FactData>())
                if (!Enum.TryParse(f.category, out ClueCategory _))
                    errors.Add($"Place '{p.displayName}' has unknown fact category '{f.category}'.");
        }

        foreach (RuleData r in src.rules)
        {
            if (!Enum.TryParse(r.type, out TravelRuleType _))
                errors.Add($"Rule '{r.asset}' has unknown type '{r.type}'.");
            if (!string.IsNullOrEmpty(r.country) && !countryIds.Contains(r.country))
                errors.Add($"Rule '{r.asset}' references unknown country '{r.country}'.");
            if (!string.IsNullOrEmpty(r.era) && !eraIds.Contains(r.era))
                errors.Add($"Rule '{r.asset}' references unknown era '{r.era}'.");
        }

        foreach (DayData d in src.days)
        {
            foreach (EraWeightData w in d.eras ?? Array.Empty<EraWeightData>())
                if (!eraIds.Contains(w.era))
                    errors.Add($"Day '{d.asset}' weights unknown era '{w.era}'.");
            foreach (string c in d.countries ?? Array.Empty<string>())
                if (!countryIds.Contains(c))
                    errors.Add($"Day '{d.asset}' allows unknown country '{c}'.");
            foreach (string r in d.rules ?? Array.Empty<string>())
                if (!ruleIds.Contains(r))
                    errors.Add($"Day '{d.asset}' uses unknown rule '{r}'.");
            if (d.tells < 1)
                errors.Add($"Day '{d.asset}' needs \"tells\" of at least 1.");
        }
    }

    /// <summary>
    /// Checks the Future and the history section before anything is written:
    /// at most one Future era, and then exactly one place of every country in
    /// it; every place fact fits a book row (FactTable.MaxValueLength); the
    /// history lines are ASCII and hold their tokens; history rules have
    /// unique lower-case ids, ASCII text, conditions whose references resolve,
    /// edits of known places and categories, and values HistoryChecks proves
    /// unique against every place's facts and every other rule.
    /// </summary>
    private static void CheckHistory(WorldSource src, Authored authored, List<string> errors)
    {
        EraData[] futures = src.eras.Where(e => e.future).ToArray();
        if (futures.Length > 1)
            errors.Add($"Only one era may be the Future; {string.Join(", ", futures.Select(e => e.id))} all set \"future\".");
        if (futures.Length == 1)
            foreach (CountryData c in src.countries)
            {
                int count = src.places.Count(p => p.country == c.id && p.era == futures[0].id);
                if (count != 1)
                    errors.Add($"Country '{c.id}' needs exactly one place in the Future era '{futures[0].id}' (it has {count}): the leader's Future place is in the world while it leads.");
            }

        var eraNames = src.eras.ToDictionary(e => e.id, e => e.displayName);
        var baseWorld = new FactTable();
        foreach (PlaceData p in src.places)
        {
            foreach (FactData f in p.facts ?? Array.Empty<FactData>())
            {
                if ((f.value ?? string.Empty).Length > FactTable.MaxValueLength)
                    errors.Add($"Place '{PlaceId(p)}' {f.category} '{f.value}' is {f.value.Length} characters long; a book row holds at most {FactTable.MaxValueLength}.");
                if (ParseEnum(f.category, out ClueCategory category) && !string.IsNullOrWhiteSpace(p.country) && !string.IsNullOrWhiteSpace(p.era))
                    baseWorld.Add(p.country, p.era, OriginLabels.Format(p.displayName, eraNames.TryGetValue(p.era, out string era) ? era : null), category, f.value);
            }
        }

        HistoryData h = src.history;
        if (h == null)
            return;

        foreach ((string field, string text, string[] tokens) in new[]
                 {
                     ("leaderGained", h.lines?.leaderGained, new[] { History.NationToken, Interview.PlaceToken }),
                     ("leaderLost", h.lines?.leaderLost, new[] { History.NationToken }),
                     ("carry", h.lines?.carry, new[] { Interview.ValueToken, Interview.PlaceToken })
                 })
        {
            if (string.IsNullOrWhiteSpace(text))
                errors.Add($"history.lines.{field} is blank.");
            CheckAscii($"history.{field}", text, errors);
            foreach (string token in tokens)
                if (!string.IsNullOrWhiteSpace(text) && !Interview.HoldsToken(text, token))
                    errors.Add($"history.lines.{field} must hold {Interview.Placeholder(token)}.");
        }

        var ruleIds = new HashSet<string>();
        var placeIds = new HashSet<string>(src.places.Select(PlaceId));
        var edits = new List<FactEdit>();
        foreach (HistoryRuleData r in h.rules ?? Array.Empty<HistoryRuleData>())
        {
            string owner = $"History rule '{r.id}'";
            if (string.IsNullOrWhiteSpace(r.id) || r.id.Any(ch => !(ch >= 'a' && ch <= 'z') && !(ch >= '0' && ch <= '9') && ch != '_'))
                errors.Add($"{owner} needs an id of lower-case letters, digits and '_' (its fired flag and asset names come from it).");
            else if (!ruleIds.Add(r.id))
                errors.Add($"History rule id '{r.id}' is listed twice.");

            foreach ((string field, string text) in new[] { ("name", r.name), ("news", r.news) })
            {
                if (string.IsNullOrWhiteSpace(text))
                    errors.Add($"{owner} has a blank {field}.");
                CheckAscii($"history.{r.id}.{field}", text, errors);
            }

            if (r.conditions == null || r.conditions.Length == 0)
                errors.Add($"{owner} needs at least one condition (it would fire on the first night).");
            CheckConditions(r.conditions, owner, false, authored, src, errors);

            if (r.edits == null || r.edits.Length == 0)
                errors.Add($"{owner} needs at least one edit.");
            foreach (EditData e in r.edits ?? Array.Empty<EditData>())
            {
                CheckAscii($"history.{r.id}.edit", e.value, errors);
                if (!placeIds.Contains(e.place ?? string.Empty))
                    errors.Add($"{owner} edits unknown place '{e.place}' (a place id is \"{{country}}_{{era}}\").");
                else if (!ParseEnum(e.category, out ClueCategory category))
                    errors.Add($"{owner} edits unknown category '{e.category}'.");
                else
                {
                    PlaceData p = src.places.First(x => PlaceId(x) == e.place);
                    edits.Add(new FactEdit(p.country, p.era, category, e.value, 0, EditCause.Rule, r.id));
                }
            }
        }

        foreach (string problem in HistoryChecks.Problems(edits, baseWorld))
            errors.Add(problem);
    }

    /// <summary>
    /// Checks each day's tell channels and the interview sections (wording,
    /// questions, dialogs, small talk) before anything is written: tokens,
    /// provable categories, gates and announcements, dialog structure and
    /// effects, one set of unique line ids, ASCII text, the worst-case length
    /// of every line the transcript can show, and menu sizes.
    /// </summary>
    private static void CheckInterview(WorldSource src, Authored authored, List<string> errors)
    {
        foreach (DayData d in src.days)
        {
            if (d.channels == null || d.channels.Length == 0)
                errors.Add($"Day '{d.asset}' needs \"channels\" (Papers and/or Answer).");
            else
                foreach (string c in d.channels)
                    if (!ParseEnum(c, out TellChannel _))
                        errors.Add($"Day '{d.asset}' has unknown tell channel '{c}' (Papers or Answer).");
        }

        InterviewData iv = src.interview;
        if (iv == null)
        {
            errors.Add($"'{SourcePath}' has no \"interview\" section (the interview's wording, menuCapacity and maxLineChars).");
            return;
        }

        // One id set for every line, generated or authored; it starts with the runtime ids.
        var ids = new Dictionary<string, string>
        {
            [InterviewScript.IntroLineId] = "the desk's opener at runtime", [InterviewScript.ClaimLineId] = "the traveller's claim at runtime"
        };
        void Id(string id, string owner)
        {
            if (string.IsNullOrWhiteSpace(id))
                errors.Add($"A line of {owner} has a blank id.");
            else if (ids.TryGetValue(id, out string first))
                errors.Add($"Line id '{id}' is used by {first} and by {owner}.");
            else
                ids.Add(id, owner);
        }

        void Ascii(string id, string text) => CheckAscii(id, text, errors);

        // --- The interview's wording and limits ---
        var wording = new (string field, string text)[]
        {
            ("deskName", iv.deskName), ("opener", iv.opener), ("openerLegendary", iv.openerLegendary), ("claim", iv.claim),
            ("honorificMale", iv.honorificMale), ("honorificFemale", iv.honorificFemale), ("honorificUnknown", iv.honorificUnknown),
            ("requestLabel", iv.requestLabel), ("requestPrompt", iv.requestPrompt), ("requestReply", iv.requestReply),
            ("askLabel", iv.askLabel), ("backLabel", iv.backLabel), ("smallTalkLabel", iv.smallTalkLabel), ("smallTalkPrompt", iv.smallTalkPrompt)
        };
        foreach ((string field, string text) in wording)
        {
            if (string.IsNullOrWhiteSpace(text))
                errors.Add($"interview.{field} is blank.");
            Ascii(InterviewLineId(field), text);
        }

        foreach ((string field, string text, string token) in new[]
                 {
                     ("opener", iv.opener, Interview.HonorificToken), ("openerLegendary", iv.openerLegendary, Interview.NameToken),
                     ("claim", iv.claim, Interview.PlaceToken), ("requestLabel", iv.requestLabel, Interview.DocumentToken),
                     ("requestPrompt", iv.requestPrompt, Interview.DocumentToken)
                 })
            if (!string.IsNullOrWhiteSpace(text) && !Interview.HoldsToken(text, token))
                errors.Add($"interview.{field} must hold {Interview.Placeholder(token)}.");

        if (iv.menuCapacity < 1)
            errors.Add("interview.menuCapacity must be at least 1 (a missing value reads 0).");
        if (iv.maxLineChars < 1)
            errors.Add("interview.maxLineChars must be at least 1 (a missing value reads 0).");

        foreach (string field in new[] { "opener", "openerLegendary", "claim", "requestPrompt", "requestReply", "smallTalkPrompt" })
            Id(InterviewLineId(field), $"interview.{field}");

        // --- Questions ---
        var bookCategories = new HashSet<ClueCategory>((authored.books ?? Array.Empty<ReferenceBookSO>()).Select(b => b.category));
        var eraIds = new HashSet<string>(src.eras.Select(e => e.id));
        var questionIds = new HashSet<string>();
        var askedCategories = new HashSet<ClueCategory>();
        QuestionData[] questions = src.questions ?? Array.Empty<QuestionData>();
        foreach (QuestionData q in questions)
        {
            string owner = $"Question '{q.id}'";
            if (string.IsNullOrWhiteSpace(q.id))
                errors.Add("A question has a blank id.");
            else if (!questionIds.Add(q.id))
                errors.Add($"Question id '{q.id}' is listed twice.");

            if (!ParseEnum(q.category, out ClueCategory category))
            {
                errors.Add($"{owner} has unknown category '{q.category}'.");
            }
            else
            {
                if (!Forgery.IsProvableCategory(category, bookCategories))
                    errors.Add($"{owner} asks about {category}, which no reference book (or, for a birth date, the Citizen Record) can prove.");
                if (!askedCategories.Add(category))
                    errors.Add($"{owner} asks about {category} again (one question per category).");
            }

            if (string.IsNullOrWhiteSpace(q.label))
                errors.Add($"{owner} has a blank label.");
            if (string.IsNullOrWhiteSpace(q.prompt))
                errors.Add($"{owner} has a blank prompt.");
            if (!Interview.HoldsToken(q.answer, Interview.ValueToken))
                errors.Add($"{owner}: its answer must hold {Interview.Placeholder(Interview.ValueToken)}.");
            if (q.fromDay < 1)
                errors.Add($"{owner} needs \"fromDay\" of at least 1 (a missing fromDay reads 0).");

            bool gated = IsGated(q);
            if (gated && string.IsNullOrWhiteSpace(q.announce))
                errors.Add($"{owner} is gated (from day {q.fromDay}, {(q.conditions != null ? q.conditions.Length : 0)} condition(s)) and needs an \"announce\" line for the morning paper.");
            else if (!gated && !string.IsNullOrWhiteSpace(q.announce))
                errors.Add($"{owner} is askable from day 1 without conditions, so nothing announces it; drop its \"announce\" line.");

            CheckConditions(q.conditions, owner, true, authored, src, errors);

            Ascii($"{q.id}.label", q.label);
            Ascii(QuestionLineId(q.id, PromptPart), q.prompt);
            Ascii(QuestionLineId(q.id, AnswerPart), q.answer);
            Ascii($"{q.id}.announce", q.announce);
            Id(QuestionLineId(q.id, PromptPart), $"question '{q.id}'");
            Id(QuestionLineId(q.id, AnswerPart), $"question '{q.id}'");

            var overridden = new HashSet<string>();
            foreach (OverrideData o in q.overrides ?? Array.Empty<OverrideData>())
            {
                string oOwner = $"{owner} override '{o.era}'";
                if (!eraIds.Contains(o.era ?? string.Empty))
                    errors.Add($"{oOwner} names an unknown era.");
                else if (!overridden.Add(o.era))
                    errors.Add($"{owner} overrides era '{o.era}' twice.");
                if (string.IsNullOrWhiteSpace(o.prompt))
                    errors.Add($"{oOwner} has a blank prompt.");
                if (!Interview.HoldsToken(o.answer, Interview.ValueToken))
                    errors.Add($"{oOwner}: its answer must hold {Interview.Placeholder(Interview.ValueToken)}.");
                Ascii(OverrideLineId(q.id, o.era, PromptPart), o.prompt);
                Ascii(OverrideLineId(q.id, o.era, AnswerPart), o.answer);
                Id(OverrideLineId(q.id, o.era, PromptPart), $"question '{q.id}' override '{o.era}'");
                Id(OverrideLineId(q.id, o.era, AnswerPart), $"question '{q.id}' override '{o.era}'");
            }
        }

        // --- Dialogs ---
        var dialogIds = new HashSet<string>();
        DialogData[] dialogs = src.dialogs ?? Array.Empty<DialogData>();
        foreach (DialogData d in dialogs)
        {
            string owner = $"Dialog '{d.id}'";
            if (string.IsNullOrWhiteSpace(d.id))
                errors.Add("A dialog has a blank id.");
            else if (!dialogIds.Add(d.id))
                errors.Add($"Dialog id '{d.id}' is listed twice.");
            if (string.IsNullOrWhiteSpace(d.label))
                errors.Add($"{owner} has a blank label.");
            Ascii($"{d.id}.label", d.label);
            CheckConditions(d.conditions, owner, false, authored, src, errors);

            void Lines(LineData[] lines, string lineOwner)
            {
                foreach (LineData line in lines ?? Array.Empty<LineData>())
                {
                    if (!ParseEnum(line.speaker, out DialogSpeaker _))
                        errors.Add($"{owner} line '{line.id}' has unknown speaker '{line.speaker}' (Desk or Traveller).");
                    if (line.id == null || !line.id.StartsWith(d.id + "."))
                        errors.Add($"{owner} line id '{line.id}' must start with '{d.id}.'.");
                    if (string.IsNullOrWhiteSpace(line.text))
                        errors.Add($"{owner} line '{line.id}' is blank.");
                    Ascii(line.id, line.text);
                    Id(line.id, lineOwner);
                }
            }

            foreach (NodeData n in d.nodes ?? Array.Empty<NodeData>())
            {
                Lines(n.lines, $"dialog '{d.id}' node '{n.id}'");
                foreach (ChoiceData c in n.choices ?? Array.Empty<ChoiceData>())
                {
                    string choiceLineId = InterviewScript.ChoiceLineId(d.id, c.id);
                    Id(choiceLineId, $"choice '{c.id}' of dialog '{d.id}'");
                    Ascii(choiceLineId, c.label);
                    Lines(c.lines, $"a line of choice '{c.id}' of dialog '{d.id}'");

                    if (string.IsNullOrWhiteSpace(c.effect))
                        continue;

                    EffectSO fx = authored.library != null ? authored.library.GetEffectByAssetName(c.effect) : null;
                    if (fx == null)
                    {
                        errors.Add($"{owner} choice '{c.id}' names effect '{c.effect}', which ContentLibrary_Main does not list; add it to the library's effects.");
                        continue;
                    }

                    foreach (EffectOp op in fx.ops)
                    {
                        if (op != null && EffectOps.ActsWhileActive(op.type))
                            errors.Add(ContentLibraryValidator.DialogEffectOpError(d.id, c.id, c.effect, op.type) + ".");
                        if (op != null && EffectOps.HistoryOnly(op.type))
                            errors.Add(ContentLibraryValidator.DialogHistoryOpError(d.id, c.id, c.effect, op.type) + ".");
                    }
                }
            }

            foreach (string problem in DialogChecks.Problems(BuildDialog(d), iv.menuCapacity))
                errors.Add($"{owner}: {problem}.");
        }

        // --- Small talk ---
        void SmallTalkLines(string ownerId, string[] lines, string owner)
        {
            for (int i = 0; lines != null && i < lines.Length; i++)
            {
                string id = SmallTalkId(ownerId, i);
                if (string.IsNullOrWhiteSpace(lines[i]))
                    errors.Add($"{owner} has a blank small-talk line ('{id}').");
                Ascii(id, lines[i]);
                Id(id, $"the small talk of {owner}");
            }
        }

        foreach (EraData e in src.eras)
            SmallTalkLines(e.id, e.smallTalk, $"era '{e.id}'");
        foreach (PlaceData p in src.places)
            SmallTalkLines(PlaceId(p), p.smallTalk, $"place '{PlaceId(p)}'");

        // --- Menus: the traveller wheel must show every choice ---
        bool anySmallTalk = src.eras.Any(e => e.smallTalk != null && e.smallTalk.Length > 0) ||
                            src.places.Any(p => p.smallTalk != null && p.smallTalk.Length > 0);
        foreach (string problem in DialogChecks.MenuProblems(questions.Length, anySmallTalk, ContentLibraryValidator.MaxRequestedDocuments(Blueprints(authored)), dialogs.Length, iv.menuCapacity))
            errors.Add(problem);

        // --- Line length: every line the transcript can show fits two lines of a row ---
        int max = iv.maxLineChars;
        if (max < 1)
            return;

        void Fits(string id, string template, string token, int longestValue)
        {
            int length = Interview.WorstCaseLength(template, token, longestValue);
            if (length > max)
                errors.Add($"Line '{id}' can render {length} characters; the transcript holds at most {max} (interview.maxLineChars).");
        }

        int longestHonorific = new[] { iv.honorificMale, iv.honorificFemale, iv.honorificUnknown }.Max(h => (h ?? string.Empty).Length);
        int longestName = authored.library != null ? authored.library.Legendaries.Where(l => l != null).Select(l => (l.displayName ?? string.Empty).Length).DefaultIfEmpty(0).Max() : 0;
        int longestDocument = DocumentTemplates(authored).Select(t => (t.displayName ?? string.Empty).Length).DefaultIfEmpty(0).Max();
        var eraNames = src.eras.ToDictionary(e => e.id, e => e.displayName);
        int longestPlace = src.places.Select(p => OriginLabels.Format(p.displayName, eraNames.TryGetValue(p.era ?? string.Empty, out string era) ? era : null).Length)
                              .DefaultIfEmpty(0).Max();

        Fits(InterviewLineId("opener"), iv.opener, Interview.HonorificToken, longestHonorific);
        Fits(InterviewLineId("openerLegendary"), iv.openerLegendary, Interview.NameToken, longestName);
        Fits(InterviewLineId("claim"), iv.claim, Interview.PlaceToken, longestPlace);
        Fits(InterviewLineId("requestPrompt"), iv.requestPrompt, Interview.DocumentToken, longestDocument);
        Fits(InterviewLineId("requestReply"), iv.requestReply, Interview.ValueToken, 0);
        Fits(InterviewLineId("smallTalkPrompt"), iv.smallTalkPrompt, Interview.ValueToken, 0);

        foreach (QuestionData q in questions)
        {
            int longestValue = ParseEnum(q.category, out ClueCategory category) ? LongestValue(src, category) : 0;
            Fits(QuestionLineId(q.id, PromptPart), q.prompt, Interview.ValueToken, 0);
            Fits(QuestionLineId(q.id, AnswerPart), q.answer, Interview.ValueToken, longestValue);
            foreach (OverrideData o in q.overrides ?? Array.Empty<OverrideData>())
            {
                Fits(OverrideLineId(q.id, o.era, PromptPart), o.prompt, Interview.ValueToken, 0);
                Fits(OverrideLineId(q.id, o.era, AnswerPart), o.answer, Interview.ValueToken, longestValue);
            }
        }

        foreach (DialogData d in dialogs)
        {
            foreach (NodeData n in d.nodes ?? Array.Empty<NodeData>())
            {
                foreach (LineData line in n.lines ?? Array.Empty<LineData>())
                    Fits(line.id, line.text, Interview.ValueToken, 0);
                foreach (ChoiceData c in n.choices ?? Array.Empty<ChoiceData>())
                {
                    Fits(InterviewScript.ChoiceLineId(d.id, c.id), c.label, Interview.ValueToken, 0);
                    foreach (LineData line in c.lines ?? Array.Empty<LineData>())
                        Fits(line.id, line.text, Interview.ValueToken, 0);
                }
            }
        }

        foreach (EraData e in src.eras)
            for (int i = 0; e.smallTalk != null && i < e.smallTalk.Length; i++)
                Fits(SmallTalkId(e.id, i), e.smallTalk[i], Interview.ValueToken, 0);
        foreach (PlaceData p in src.places)
            for (int i = 0; p.smallTalk != null && i < p.smallTalk.Length; i++)
                Fits(SmallTalkId(PlaceId(p), i), p.smallTalk[i], Interview.ValueToken, 0);
    }

    /// <summary>Authored text must be ASCII: new glyphs dirty the TMP fallback atlas (interview and history text alike).</summary>
    private static void CheckAscii(string id, string text, List<string> errors)
    {
        char bad = (text ?? string.Empty).FirstOrDefault(ch => ch > 127);
        if (bad != default)
            errors.Add($"'{id}' holds the non-ASCII character '{bad}'; authored text must be ASCII (new glyphs dirty the TMP fallback atlas).");
    }

    /// <summary>
    /// A condition list's problems: an unknown type; a reference the type
    /// needs that is missing or unknown (a place and an attribute for the
    /// place-attribute types, a nation for NationScoreAtLeast and
    /// NationIsLeader, an attribute for the global totals); DayAtLeast inside
    /// a question (its day is "fromDay"); an unknown upgrade; a blank flag or
    /// counter key.
    /// </summary>
    private static void CheckConditions(ConditionData[] conditions, string owner, bool isQuestion, Authored authored, WorldSource src, List<string> errors)
    {
        var placeIds = new HashSet<string>(src.places.Select(PlaceId));
        var countryIds = new HashSet<string>(src.countries.Select(c => c.id));
        void Needs(bool ok, string type, string field, string value)
        {
            if (!ok)
                errors.Add($"{owner} has a {type} condition whose \"{field}\" ('{value}') names nothing in world_source.json.");
        }

        foreach (ConditionData c in conditions ?? Array.Empty<ConditionData>())
        {
            if (!ParseEnum(c.type, out TriggerConditionType type))
            {
                errors.Add($"{owner} has unknown condition type '{c.type}'.");
                continue;
            }

            switch (type)
            {
                case TriggerConditionType.AttributeScoreAtLeast:
                case TriggerConditionType.AttributeScoreAtMost:
                case TriggerConditionType.AttributeIsDominant:
                case TriggerConditionType.AttributeIsSupporting:
                    Needs(c.place != null && placeIds.Contains(c.place), c.type, "place", c.place);
                    Needs(c.attribute != null && authored.attributes.ContainsKey(c.attribute), c.type, "attribute", c.attribute);
                    break;
                case TriggerConditionType.NationScoreAtLeast:
                case TriggerConditionType.NationIsLeader:
                    Needs(c.nation != null && countryIds.Contains(c.nation), c.type, "nation", c.nation);
                    break;
                case TriggerConditionType.GlobalAttrAtLeast:
                case TriggerConditionType.GlobalAttrAtMost:
                    Needs(c.attribute != null && authored.attributes.ContainsKey(c.attribute), c.type, "attribute", c.attribute);
                    break;
                case TriggerConditionType.DayAtLeast:
                    if (isQuestion)
                        errors.Add($"{owner} gates on DayAtLeast in \"conditions\"; use \"fromDay\" (one day value per question).");
                    break;
                case TriggerConditionType.UpgradeOwned:
                    if (authored.library == null || authored.library.GetUpgradeById(c.key) == null)
                        errors.Add($"{owner} requires unknown upgrade '{c.key}' (not in the content library's upgrades).");
                    break;
                case TriggerConditionType.FlagSet:
                case TriggerConditionType.FlagNotSet:
                case TriggerConditionType.CounterAtLeast:
                    if (string.IsNullOrWhiteSpace(c.key))
                        errors.Add($"{owner} has a {type} condition with a blank key.");
                    break;
            }
        }
    }

    /// <summary>The document templates a traveller can carry: the wired blueprint's and every listed legendary's override's.</summary>
    private static List<DocumentTemplateSO> DocumentTemplates(Authored authored)
    {
        var templates = new List<DocumentTemplateSO>();
        foreach (CaseBlueprintSO blueprint in Blueprints(authored))
            if (blueprint.DocumentTemplates != null)
                templates.AddRange(blueprint.DocumentTemplates.Where(t => t != null));
        return templates;
    }

    /// <summary>The wired blueprint and the listed legendaries' overrides (non-null).</summary>
    private static List<CaseBlueprintSO> Blueprints(Authored authored)
    {
        var blueprints = new List<CaseBlueprintSO>();
        if (authored.blueprint != null)
            blueprints.Add(authored.blueprint);
        if (authored.library != null)
            foreach (LegendarySO legend in authored.library.Legendaries)
                if (legend != null && legend.blueprintOverride != null)
                    blueprints.Add(legend.blueprintOverride);
        return blueprints;
    }

    /// <summary>
    /// The longest value a {value} of this category can take: the longest fact
    /// of any place or history-rule edit, or for a birth date the longest
    /// registered date the places' birth years give.
    /// </summary>
    private static int LongestValue(WorldSource src, ClueCategory category)
    {
        int longest = 0;
        foreach (PlaceData p in src.places)
        {
            if (category == ClueCategory.BirthDate)
            {
                (int min, int max) = BirthYears(p, src.travellerAgeMin, src.travellerAgeMax);
                for (int year = min; year <= max; year++)
                {
                    // Only a date BirthDates can read back is ever printed (it owns "no year 0").
                    string date = BirthDates.Format(28, 0, year);
                    if (BirthDates.TryParse(date, out _, out _, out _))
                        longest = Math.Max(longest, date.Length);
                }
                continue;
            }

            foreach (FactData f in p.facts ?? Array.Empty<FactData>())
                if (f.category == category.ToString())
                    longest = Math.Max(longest, (f.value ?? string.Empty).Length);
        }

        foreach (HistoryRuleData r in src.history?.rules ?? Array.Empty<HistoryRuleData>())
            foreach (EditData e in r.edits ?? Array.Empty<EditData>())
                if (e.category == category.ToString())
                    longest = Math.Max(longest, (e.value ?? string.Empty).Length);

        return longest;
    }

    /// <summary>A place's birth-year range: its year minus the oldest and the youngest traveller age (MakePlace writes it, LongestValue measures it).</summary>
    private static (int min, int max) BirthYears(PlaceData p, int ageMin, int ageMax) => (p.year - ageMax, p.year - ageMin);

    // -----------------------------
    // Builders
    // -----------------------------

    private static EraSO MakeEra(EraData e, HashSet<string> written)
    {
        EraSO era = LoadOrCreate<EraSO>($"{WorldRoot}/Eras/Era_{e.id}.asset", written);
        era.id = e.id;
        era.displayName = e.displayName;
        era.order = e.order;
        era.isFuture = e.future;
        era.smallTalk = SmallTalk(e.id, e.smallTalk);
        EditorUtility.SetDirty(era);
        return era;
    }

    private static NationSO MakeNation(CountryData c, EffectSO leaderEffect, HashSet<string> written)
    {
        NationSO nation = LoadOrCreate<NationSO>($"{WorldRoot}/Nations/Nation_{c.id}.asset", written);
        nation.id = c.id;
        nation.displayName = c.displayName;
        nation.leaderEffect = leaderEffect;
        EditorUtility.SetDirty(nation);
        return nation;
    }

    /// <summary>Writes History/Effect_Leader_{id}.asset: the permanent UI-channel effect whose cue culture:{id} broadcasts the country as the present culture while it leads.</summary>
    private static EffectSO MakeLeaderEffect(CountryData c, HashSet<string> written)
    {
        EffectSO fx = LoadOrCreate<EffectSO>($"{HistoryFolder}/Effect_Leader_{c.id}.asset", written);
        fx.displayName = $"Leader: {c.displayName}";
        fx.channel = EffectChannel.UI;
        fx.defaultDurationDays = -1;
        fx.ops = new List<EffectOp> { new EffectOp { type = EffectOpType.Cue, stringParam = CultureCue.Format(c.id) } };
        EditorUtility.SetDirty(fx);
        return fx;
    }

    /// <summary>Writes History/Effect_History_{id}.asset: one SetFact op per edit of the rule (latched when its trigger fires).</summary>
    private static EffectSO MakeHistoryEffect(HistoryRuleData r, ConditionRefs refs, HashSet<string> written)
    {
        EffectSO fx = LoadOrCreate<EffectSO>($"{HistoryFolder}/Effect_History_{r.id}.asset", written);
        fx.displayName = r.name;
        fx.channel = EffectChannel.General;
        fx.defaultDurationDays = 1;
        fx.ops = (r.edits ?? Array.Empty<EditData>()).Select(e => new EffectOp
        {
            type = EffectOpType.SetFact,
            profile = refs.places[e.place],
            category = (ClueCategory)Enum.Parse(typeof(ClueCategory), e.category),
            stringParam = e.value
        }).ToList();
        EditorUtility.SetDirty(fx);
        return fx;
    }

    /// <summary>Writes History/Trigger_History_{id}.asset: a one-shot trigger with the rule's conditions, news line and effect ("history_{id}").</summary>
    private static TimelineTriggerSO MakeHistoryTrigger(HistoryRuleData r, EffectSO effect, ConditionRefs refs, HashSet<string> written)
    {
        TimelineTriggerSO t = LoadOrCreate<TimelineTriggerSO>($"{HistoryFolder}/Trigger_History_{r.id}.asset", written);
        t.id = $"history_{r.id}";
        t.displayName = r.name;
        t.description = "Generated from world_source.json history.rules.";
        t.oneShot = true;
        t.newsLineOnFire = r.news;
        t.conditions = Conditions(r.conditions, refs).ToList();
        t.outcomes = new List<TriggerOutcome> { new TriggerOutcome { effect = effect, durationDaysOverride = 0 } };
        EditorUtility.SetDirty(t);
        return t;
    }

    /// <summary>The templated history lines with their ids ("history.leaderGained", ...).</summary>
    private static HistoryLines BuildHistoryLines(HistoryLinesData l) => new HistoryLines
    {
        leaderGained = new LineText("history.leaderGained", l?.leaderGained),
        leaderLost = new LineText("history.leaderLost", l?.leaderLost),
        carry = new LineText("history.carry", l?.carry)
    };

    private static NationEraProfileSO MakePlace(PlaceData p, NationSO nation, EraSO era, CountryData country,
                                                Dictionary<string, AttributeSO> attributes, int ageMin, int ageMax,
                                                HashSet<string> written)
    {
        NationEraProfileSO place = LoadOrCreate<NationEraProfileSO>($"{WorldRoot}/Places/Place_{PlaceId(p)}.asset", written);
        place.id = PlaceId(p);
        place.displayName = p.displayName;
        place.nation = nation;
        place.era = era;
        (place.birthYearMin, place.birthYearMax) = BirthYears(p, ageMin, ageMax);
        place.maleNames = p.maleNames ?? Array.Empty<string>();
        place.femaleNames = p.femaleNames ?? Array.Empty<string>();
        place.smallTalk = SmallTalk(place.id, p.smallTalk);

        place.facts = (p.facts ?? Array.Empty<FactData>())
            .Select(f => new ProfileFact { category = (ClueCategory)Enum.Parse(typeof(ClueCategory), f.category), value = f.value })
            .ToList();

        // Starting attribute scores follow the country's theme. No tier effects:
        // 40 places x tiers would flood the morning paper. Future places get no
        // baselines, so they never join the tiers or their news (history ranks
        // nations by influence instead).
        place.baselines = era.isFuture
            ? new List<AttributeBaseline>()
            : (country.baselines ?? Array.Empty<BaselineData>())
                .Select(b => new AttributeBaseline { attribute = attributes[b.attribute], baseScore = b.score })
                .ToList();

        EditorUtility.SetDirty(place);
        return place;
    }

    private static TravelRuleSO MakeRule(RuleData r, Dictionary<string, NationSO> nations, Dictionary<string, EraSO> eras, HashSet<string> written)
    {
        TravelRuleSO rule = LoadOrCreate<TravelRuleSO>($"{WorldRoot}/Rules/{r.asset}.asset", written);
        rule.type = (TravelRuleType)Enum.Parse(typeof(TravelRuleType), r.type);
        rule.nation = !string.IsNullOrEmpty(r.country) ? nations[r.country] : null;
        rule.era = !string.IsNullOrEmpty(r.era) ? eras[r.era] : null;
        rule.description = r.description;
        EditorUtility.SetDirty(rule);
        return rule;
    }

    /// <summary>
    /// Writes the day's queue, tell count, tell channels, eras, countries and
    /// rules. Legendary settings are left to their authors (missing legendaries
    /// are dropped).
    /// </summary>
    private static DayPlanSO MakeDay(DayData d, string folder, CaseBlueprintSO blueprint, Dictionary<string, EraSO> eras,
                                     Dictionary<string, NationSO> nations, Dictionary<string, TravelRuleSO> rules)
    {
        DayPlanSO plan = LoadOrCreate<DayPlanSO>($"{folder}/{d.asset}.asset", null);
        var so = new SerializedObject(plan);
        so.FindProperty("dayNumber").intValue = d.day;
        so.FindProperty("visitorsCount").intValue = d.queue;
        so.FindProperty("tellCount").intValue = d.tells;
        string[] channels = d.channels ?? Array.Empty<string>();
        SerializedProperty tellChannels = so.FindProperty("tellChannels");
        tellChannels.arraySize = channels.Length;
        for (int i = 0; i < channels.Length; i++)
            tellChannels.GetArrayElementAtIndex(i).enumValueIndex = (int)(TellChannel)Enum.Parse(typeof(TellChannel), channels[i]);
        SetArray(so, "possibleBlueprints", new Object[] { blueprint });
        DropMissing(so, "availableLegendaries");
        SetArray(so, "allowedNations", (d.countries ?? Array.Empty<string>()).Select(c => (Object)nations[c]).ToArray());
        SetArray(so, "activeTravelRules", (d.rules ?? Array.Empty<string>()).Select(r => (Object)rules[r]).ToArray());

        EraWeightData[] weightsData = d.eras ?? Array.Empty<EraWeightData>();
        SerializedProperty weights = so.FindProperty("eraWeights");
        weights.arraySize = weightsData.Length;
        for (int i = 0; i < weightsData.Length; i++)
        {
            SerializedProperty el = weights.GetArrayElementAtIndex(i);
            el.FindPropertyRelative("era").objectReferenceValue = eras[weightsData[i].era];
            el.FindPropertyRelative("weight").floatValue = weightsData[i].weight;
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(plan);
        return plan;
    }

    /// <summary>Small-talk lines with their generated ids ("{ownerId}.smalltalk.{n}", 1-based).</summary>
    private static List<LineText> SmallTalk(string ownerId, string[] lines) =>
        (lines ?? Array.Empty<string>()).Select((text, i) => new LineText(SmallTalkId(ownerId, i), text)).ToList();

    /// <summary>The id of an era's or place's small-talk line.</summary>
    private static string SmallTalkId(string ownerId, int index) => $"{ownerId}.smalltalk.{index + 1}";

    /// <summary>The id of an interview line ("interview.opener", ...).</summary>
    private static string InterviewLineId(string field) => $"interview.{field}";

    /// <summary>The parts of a question's (or an override's) two line ids: its prompt and its answer.</summary>
    private const string PromptPart = "prompt", AnswerPart = "answer";

    /// <summary>The id of a question's line, "{questionId}.{part}": BuildQuestion writes it, CheckInterview checks it.</summary>
    private static string QuestionLineId(string questionId, string part) => $"{questionId}.{part}";

    /// <summary>The id of an era override's line, "{questionId}.{eraId}.{part}": BuildQuestion writes it, CheckInterview checks it.</summary>
    private static string OverrideLineId(string questionId, string eraId, string part) => $"{questionId}.{eraId}.{part}";

    /// <summary>A place's id, "{country}_{era}": the profile's id, its asset name and its small-talk lines' owner id.</summary>
    private static string PlaceId(PlaceData p) => $"{p.country}_{p.era}";

    /// <summary>The interview's wording with generated line ids, and its menu capacity (the longest-line limit stays in the source: only CheckInterview reads it).</summary>
    private static InterviewLines BuildLines(InterviewData i) => new InterviewLines
    {
        deskName = i.deskName,
        opener = new LineText(InterviewLineId("opener"), i.opener),
        openerLegendary = new LineText(InterviewLineId("openerLegendary"), i.openerLegendary),
        claim = new LineText(InterviewLineId("claim"), i.claim),
        honorificMale = i.honorificMale,
        honorificFemale = i.honorificFemale,
        honorificUnknown = i.honorificUnknown,
        requestLabel = i.requestLabel,
        requestPrompt = new LineText(InterviewLineId("requestPrompt"), i.requestPrompt),
        requestReply = new LineText(InterviewLineId("requestReply"), i.requestReply),
        askLabel = i.askLabel,
        backLabel = i.backLabel,
        smallTalkLabel = i.smallTalkLabel,
        smallTalkPrompt = new LineText(InterviewLineId("smallTalkPrompt"), i.smallTalkPrompt),
        menuCapacity = i.menuCapacity
    };

    /// <summary>A question with generated line ids ("{id}.prompt", "{id}.{era}.answer", ...).</summary>
    private static InterviewQuestion BuildQuestion(QuestionData q) => new InterviewQuestion
    {
        id = q.id,
        category = (ClueCategory)Enum.Parse(typeof(ClueCategory), q.category),
        label = q.label,
        prompt = new LineText(QuestionLineId(q.id, PromptPart), q.prompt),
        answer = new LineText(QuestionLineId(q.id, AnswerPart), q.answer),
        overrides = (q.overrides ?? Array.Empty<OverrideData>()).Select(o => new WordingOverride
        {
            eraId = o.era,
            prompt = new LineText(OverrideLineId(q.id, o.era, PromptPart), o.prompt),
            answer = new LineText(OverrideLineId(q.id, o.era, AnswerPart), o.answer)
        }).ToList()
    };

    /// <summary>A dialog as the runner's data contract; one-shot unless repeatable. Never throws (an unknown speaker reads Traveller; the checks report it).</summary>
    private static AuthoredDialog BuildDialog(DialogData d) => new AuthoredDialog
    {
        id = d.id,
        label = d.label,
        oneShot = !d.repeatable,
        nodes = (d.nodes ?? Array.Empty<NodeData>()).Select(n => new ScriptNode
        {
            id = n.id,
            lines = ScriptLines(n.lines),
            choices = (n.choices ?? Array.Empty<ChoiceData>()).Select(c => new ScriptChoice
            {
                id = c.id,
                label = c.label,
                lines = ScriptLines(c.lines),
                next = c.next ?? string.Empty,
                effect = c.effect ?? string.Empty
            }).ToList()
        }).ToList()
    };

    private static List<ScriptLine> ScriptLines(LineData[] lines) =>
        (lines ?? Array.Empty<LineData>()).Select(l => new ScriptLine
        {
            id = l.id,
            speaker = ParseEnum(l.speaker, out DialogSpeaker speaker) ? speaker : DialogSpeaker.Traveller,
            text = l.text
        }).ToList();

    /// <summary>A day gate at <paramref name="threshold"/> when the question starts after day 1, else nothing.</summary>
    private static IEnumerable<TriggerCondition> DayGate(int fromDay, int threshold) =>
        fromDay > 1
            ? new[] { new TriggerCondition { type = TriggerConditionType.DayAtLeast, threshold = threshold } }
            : Array.Empty<TriggerCondition>();

    /// <summary>The authored conditions as trigger conditions, their place, attribute and nation ids resolved (questions, dialogs and history rules alike).</summary>
    private static IEnumerable<TriggerCondition> Conditions(ConditionData[] conditions, ConditionRefs refs) =>
        (conditions ?? Array.Empty<ConditionData>()).Select(c => new TriggerCondition
        {
            type = (TriggerConditionType)Enum.Parse(typeof(TriggerConditionType), c.type),
            key = c.key,
            threshold = c.threshold,
            profile = c.place != null && refs.places.TryGetValue(c.place, out NationEraProfileSO p) ? p : null,
            attribute = c.attribute != null && refs.attributes.TryGetValue(c.attribute, out AttributeSO a) ? a : null,
            nation = c.nation != null && refs.nations.TryGetValue(c.nation, out NationSO n) ? n : null
        });

    /// <summary>The generated places, the authored attributes and the generated nations by id: what condition and edit ids resolve to.</summary>
    private sealed class ConditionRefs
    {
        public readonly Dictionary<string, NationEraProfileSO> places;
        public readonly Dictionary<string, AttributeSO> attributes;
        public readonly Dictionary<string, NationSO> nations;

        public ConditionRefs(Dictionary<string, NationEraProfileSO> places, Dictionary<string, AttributeSO> attributes, Dictionary<string, NationSO> nations)
        {
            this.places = places;
            this.attributes = attributes;
            this.nations = nations;
        }
    }

    /// <summary>True when a question is gated (fromDay above 1 or any authored condition), so an unlock trigger announces it.</summary>
    private static bool IsGated(QuestionData q) => q.fromDay > 1 || (q.conditions != null && q.conditions.Length > 0);

    /// <summary>Writes Interview/Question_{id}.asset: the question and its day-start conditions (DayAtLeast fromDay when fromDay > 1, plus the authored ones).</summary>
    private static QuestionSO MakeQuestion(QuestionData q, ConditionRefs refs, HashSet<string> written)
    {
        QuestionSO so = LoadOrCreate<QuestionSO>($"{InterviewFolder}/Question_{q.id}.asset", written);
        so.question = BuildQuestion(q);
        so.conditions = DayGate(q.fromDay, q.fromDay).Concat(Conditions(q.conditions, refs)).ToList();
        EditorUtility.SetDirty(so);
        return so;
    }

    /// <summary>Writes Interview/Dialog_{id}.asset: the dialog and its conditions.</summary>
    private static DialogSO MakeDialog(DialogData d, ConditionRefs refs, HashSet<string> written)
    {
        DialogSO so = LoadOrCreate<DialogSO>($"{InterviewFolder}/Dialog_{d.id}.asset", written);
        so.dialog = BuildDialog(d);
        so.conditions = Conditions(d.conditions, refs).ToList();
        EditorUtility.SetDirty(so);
        return so;
    }

    /// <summary>
    /// Writes Interview/Trigger_Unlock_{questionId}.asset for a gated question:
    /// a one-shot trigger whose news line is the question's announcement. It
    /// fires the night before the first day the question is askable
    /// (DayAtLeast Gates.UnlockNight(fromDay) when fromDay > 1) and when the
    /// question's own conditions hold.
    /// </summary>
    private static TimelineTriggerSO MakeUnlockTrigger(QuestionData q, ConditionRefs refs, HashSet<string> written)
    {
        TimelineTriggerSO t = LoadOrCreate<TimelineTriggerSO>($"{InterviewFolder}/Trigger_Unlock_{q.id}.asset", written);
        t.id = $"unlock_{q.id}";
        t.displayName = $"Unlock: {q.label}";
        t.description = $"Generated by Generate World: announces the {q.label} question in the morning paper of the first day it can be asked.";
        t.oneShot = true;
        t.newsLineOnFire = q.announce;
        t.conditions = DayGate(q.fromDay, Gates.UnlockNight(q.fromDay)).Concat(Conditions(q.conditions, refs)).ToList();
        t.outcomes = new List<TriggerOutcome>();
        EditorUtility.SetDirty(t);
        return t;
    }

    /// <summary>An enum value by name that the enum defines (plain Enum.TryParse also accepts any number).</summary>
    private static bool ParseEnum<T>(string text, out T value) where T : struct =>
        Enum.TryParse(text, out value) && Enum.IsDefined(typeof(T), value);

    /// <summary>
    /// Sets every world array of the library, the interview and the history
    /// lines (authoritative), rewires the triggers (the hand-authored ones kept
    /// in order, then the generated unlock triggers, then the history-rule
    /// triggers) and the effects (the hand-authored ones kept in order, then
    /// the history-rule effects, then the leader effects), and drops missing
    /// references from the rest.
    /// </summary>
    private static void WireLibrary(ContentLibrarySO lib, DayPlanSO[] days, EraSO[] eras, NationSO[] nations, NationEraProfileSO[] places,
                                    ArchetypeSO[] archetypes, AttributeSO[] attributes, ReferenceBookSO[] books,
                                    InterviewLines interview, QuestionSO[] questions, DialogSO[] dialogs, TimelineTriggerSO[] unlocks,
                                    HistoryLines historyLines, TimelineTriggerSO[] historyTriggers, EffectSO[] historyEffects, EffectSO[] leaderEffects)
    {
        var so = new SerializedObject(lib);
        SetArray(so, "dayPlans", days);
        SetArray(so, "eras", eras);
        SetArray(so, "nations", nations);
        SetArray(so, "nationEraProfiles", places);
        SetArray(so, "archetypes", archetypes);
        SetArray(so, "attributes", attributes);
        SetArray(so, "referenceBooks", books);
        so.FindProperty("interview").boxedValue = interview;
        SetArray(so, "questions", questions);
        SetArray(so, "dialogs", dialogs);
        so.FindProperty("historyLines").boxedValue = historyLines;
        SetArray(so, "timelineTriggers", HandAuthored(so, "timelineTriggers").Concat(unlocks).Concat(historyTriggers).ToArray());
        SetArray(so, "effects", HandAuthored(so, "effects").Concat(historyEffects).Concat(leaderEffects).ToArray());
        DropMissing(so, "clues");
        DropMissing(so, "legendaries");
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(lib);
    }

    /// <summary>Moves generated assets the source no longer lists to the OS trash. Returns how many.</summary>
    private static int PruneOwnedFolders(HashSet<string> written)
    {
        int pruned = 0;
        foreach (string folder in OwnedFolders)
        {
            foreach (string guid in AssetDatabase.FindAssets(string.Empty, new[] { $"{WorldRoot}/{folder}" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(path) || written.Contains(path))
                    continue;

                Debug.LogWarning($"[WorldContentGenerator] '{path}' is no longer in the source; moving it to the trash.");
                if (AssetDatabase.MoveAssetToTrash(path))
                    pruned++;
            }
        }

        return pruned;
    }

    // -----------------------------
    // Helpers
    // -----------------------------

    private static WorldSource LoadSource()
    {
        string full = Path.Combine(Directory.GetParent(Application.dataPath).FullName, SourcePath);
        if (!File.Exists(full))
        {
            Debug.LogError($"[WorldContentGenerator] Missing '{SourcePath}'.");
            return null;
        }

        WorldSource src = JsonUtility.FromJson<WorldSource>(File.ReadAllText(full));
        if (src == null || src.eras == null || src.countries == null || src.places == null || src.days == null || src.rules == null)
        {
            Debug.LogError($"[WorldContentGenerator] '{SourcePath}' is malformed.");
            return null;
        }
        return src;
    }

    private static void SetArray(SerializedObject so, string prop, IReadOnlyList<Object> values)
    {
        SerializedProperty p = so.FindProperty(prop);
        if (p == null)
        {
            Debug.LogError($"[WorldContentGenerator] '{so.targetObject.name}' has no serialized field '{prop}'.");
            return;
        }
        p.arraySize = values.Count;
        for (int i = 0; i < values.Count; i++)
            p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

    /// <summary>Removes null (deleted) references from an object array property.</summary>
    private static void DropMissing(SerializedObject so, string prop)
    {
        SerializedProperty p = so.FindProperty(prop);
        if (p == null)
        {
            Debug.LogError($"[WorldContentGenerator] '{so.targetObject.name}' has no serialized field '{prop}'.");
            return;
        }

        var kept = new List<Object>();
        for (int i = 0; i < p.arraySize; i++)
        {
            Object o = p.GetArrayElementAtIndex(i).objectReferenceValue;
            if (o != null)
                kept.Add(o);
        }
        SetArray(so, prop, kept);
    }

    /// <summary>Loads or creates an asset; records its path in <paramref name="written"/> (when given) so pruning keeps it.</summary>
    private static T LoadOrCreate<T>(string path, HashSet<string> written) where T : ScriptableObject
    {
        written?.Add(path);
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null)
            return asset;

        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    /// <summary>The library array's current entries that are not generated here (non-null, outside the Interview and History folders), in order.</summary>
    private static List<Object> HandAuthored(SerializedObject so, string prop)
    {
        var kept = new List<Object>();
        SerializedProperty p = so.FindProperty(prop);
        for (int i = 0; i < p.arraySize; i++)
        {
            Object o = p.GetArrayElementAtIndex(i).objectReferenceValue;
            string path = o != null ? AssetDatabase.GetAssetPath(o) : null;
            if (o != null && !path.StartsWith(InterviewFolder + "/") && !path.StartsWith(HistoryFolder + "/"))
                kept.Add(o);
        }
        return kept;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    // -----------------------------
    // Source file shape (JsonUtility)
    // -----------------------------

    [Serializable] private sealed class WorldSource
    {
        public int travellerAgeMin = 18;
        public int travellerAgeMax = 70;
        public ContentData content;
        public EraData[] eras;
        public CountryData[] countries;
        public PlaceData[] places;
        public RuleData[] rules;
        public DayData[] days;
        public InterviewData interview;
        public QuestionData[] questions;
        public DialogData[] dialogs;
        public HistoryData history;
    }

    /// <summary>Authored assets the world is wired into (asset paths).</summary>
    [Serializable] private sealed class ContentData
    {
        public string library;
        public string blueprint;
        public string dayPlanFolder;
        public AttributeData[] attributes;
        public string[] archetypes;
        public string[] books;
    }

    [Serializable] private sealed class AttributeData { public string id; public string asset; }

    /// <summary>An era; "future" marks the office's own time (at most one).</summary>
    [Serializable] private sealed class EraData { public string id; public string displayName; public int order; public bool future; public string[] smallTalk; }

    [Serializable] private sealed class CountryData { public string id; public string displayName; public BaselineData[] baselines; }

    [Serializable] private sealed class BaselineData { public string attribute; public float score; }

    [Serializable] private sealed class FactData { public string category; public string value; }

    /// <summary>A place; "moment" in the source is research context only (not generated).</summary>
    [Serializable] private sealed class PlaceData
    {
        public string country;
        public string era;
        public string displayName;
        public int year;
        public FactData[] facts;
        public string[] maleNames;
        public string[] femaleNames;
        public string[] smallTalk;
    }

    [Serializable] private sealed class RuleData { public string asset; public string type; public string country; public string era; public string description; }

    [Serializable] private sealed class EraWeightData { public string era; public float weight; }

    [Serializable] private sealed class DayData
    {
        public string asset;
        public int day;
        public int queue;
        /// <summary>Tells each liar leaks this day (at least 1).</summary>
        public int tells;
        /// <summary>Where this day's tells may show ("Papers", "Answer").</summary>
        public string[] channels;
        public EraWeightData[] eras;
        public string[] countries;
        public string[] rules;
    }

    /// <summary>The interview's wording (plain strings; ids are generated) and its two layout limits (menuCapacity is written to the library; maxLineChars only bounds CheckInterview's line-length check).</summary>
    [Serializable] private sealed class InterviewData
    {
        public string deskName;
        public string opener;
        public string openerLegendary;
        public string claim;
        public string honorificMale;
        public string honorificFemale;
        public string honorificUnknown;
        public string requestLabel;
        public string requestPrompt;
        public string requestReply;
        public string askLabel;
        public string backLabel;
        public string smallTalkLabel;
        public string smallTalkPrompt;
        public int menuCapacity;
        public int maxLineChars;
    }

    /// <summary>A question; fromDay is required (0 = missing), announce is required exactly when the question is gated.</summary>
    [Serializable] private sealed class QuestionData
    {
        public string id;
        public string category;
        public string label;
        public string prompt;
        public string answer;
        public int fromDay;
        public string announce;
        public ConditionData[] conditions;
        public OverrideData[] overrides;
    }

    [Serializable] private sealed class OverrideData { public string era; public string prompt; public string answer; }

    /// <summary>A gate condition; place ("{country}_{era}"), attribute and nation are ids the generator resolves.</summary>
    [Serializable] private sealed class ConditionData { public string type; public string key; public float threshold; public string place; public string attribute; public string nation; }

    /// <summary>The history section: the templated news lines and the authored history rules.</summary>
    [Serializable] private sealed class HistoryData { public HistoryLinesData lines; public HistoryRuleData[] rules; }

    /// <summary>The templated history lines ({nation}, {place}, {value}).</summary>
    [Serializable] private sealed class HistoryLinesData { public string leaderGained; public string leaderLost; public string carry; }

    /// <summary>A history rule: when its conditions pass at night it fires once, latches its edits and prints its news line.</summary>
    [Serializable] private sealed class HistoryRuleData { public string id; public string name; public string news; public ConditionData[] conditions; public EditData[] edits; }

    /// <summary>A fact edit: place ("{country}_{era}"), category and the new value.</summary>
    [Serializable] private sealed class EditData { public string place; public string category; public string value; }

    /// <summary>A narrative dialog; one-shot unless "repeatable" is true.</summary>
    [Serializable] private sealed class DialogData
    {
        public string id;
        public string label;
        public bool repeatable;
        public ConditionData[] conditions;
        public NodeData[] nodes;
    }

    [Serializable] private sealed class NodeData { public string id; public LineData[] lines; public ChoiceData[] choices; }

    [Serializable] private sealed class LineData { public string id; public string speaker; public string text; }

    [Serializable] private sealed class ChoiceData { public string id; public string label; public LineData[] lines; public string next; public string effect; }
}
