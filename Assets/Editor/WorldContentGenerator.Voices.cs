using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Generate World's voices (the personalities spec's §4.2, §9): the cast
/// (world_source.json "personalities"), the small-talk weights, the kinds'
/// small talk and the personalities' and premades' own lines
/// (interview.smallTalkWeights, kindSmallTalk, voices), checked by the rules
/// the validator shares (Personalities.Problems, VoiceChecks.Problems) and
/// written into the library (ContentLibrarySO.personalities,
/// InterviewLines.smallTalkWeights, kindSmallTalk, voices) with generated
/// line ids: "interview.voices.{list}.{personality or premade}.{n}",
/// "interview.kindSmallTalk.{n}".
/// </summary>
public static partial class WorldContentGenerator
{
    /// <summary>
    /// The cast's and the voices' checks: the cast's rules; every row's kinds
    /// and variant known; VoiceChecks over the rows (its errors stop the
    /// generator, its warnings and each personality's coverage are logged);
    /// each line's id unique (<paramref name="id"/>) and ASCII (<paramref name="ascii"/>).
    /// <paramref name="kindForms"/> are the kinds in play on each day with that
    /// day's papers menu (a refusal's request must be one a menu offers).
    /// </summary>
    private static void CheckVoices(WorldSource src, Authored authored, List<KindForms> kindForms, List<string> errors, Action<string, string> id, Action<string, string> ascii)
    {
        InterviewData iv = src.interview;
        errors.AddRange(Personalities.Problems(BuildCast(src.personalities)));

        void Names(string owner, string[] kinds, string variant, VoiceRowData row = null, bool reaction = false)
        {
            foreach (string kind in kinds ?? Array.Empty<string>())
                if (!ParseEnum(kind, out TravellerKind _))
                    errors.Add($"{owner} names '{kind}' in \"kinds\", which is not a traveller kind ({string.Join(", ", Enum.GetNames(typeof(TravellerKind)))}).");
            if (!string.IsNullOrEmpty(variant) && !ParseEnum(variant, out MissingFormVariant _))
                errors.Add($"{owner} names the variant '{variant}' ({string.Join(", ", Enum.GetNames(typeof(MissingFormVariant)))}).");
            if (reaction && row != null)
            {
                if (!ParseEnum(row.verdict, out ReactionVerdict _))
                    errors.Add($"{owner} needs a \"verdict\" ({string.Join(", ", Enum.GetNames(typeof(ReactionVerdict)))}), not '{row.verdict}'.");
                if (!ParseEnum(row.intent, out ReactionIntent _))
                    errors.Add($"{owner} needs an \"intent\" ({string.Join(", ", Enum.GetNames(typeof(ReactionIntent)))}), not '{row.intent}'.");
            }
        }

        VoicesData voices = iv.voices ?? new VoicesData();
        foreach ((string list, VoiceRowData[] rows) in VoiceLists(voices))
            for (int i = 0; i < rows.Length; i++)
                if (rows[i] != null)
                    Names($"interview.voices.{list} row {i + 1}", rows[i].kinds, rows[i].variant, rows[i], list == "reactions");
        foreach ((string list, VoiceRowData[] rows) in new[] { ("reactions", iv.reactions ?? Array.Empty<VoiceRowData>()), ("slips", iv.slips ?? Array.Empty<VoiceRowData>()), ("waiverPad.replies", iv.waiverPad?.replies ?? Array.Empty<VoiceRowData>()), ("confront.replies", iv.confront?.replies ?? Array.Empty<VoiceRowData>()) })
            for (int i = 0; i < rows.Length; i++)
                if (rows[i] != null)
                    Names($"interview.{list} row {i + 1}", rows[i].kinds, null, rows[i], list == "reactions");
        foreach ((string list, VoiceRowData[] rows) in new[] { ("interview.voices.confront", voices.confront ?? Array.Empty<VoiceRowData>()), ("interview.confront.replies", iv.confront?.replies ?? Array.Empty<VoiceRowData>()) })
            for (int i = 0; i < rows.Length; i++)
                if (rows[i] != null && !ParseEnum(rows[i].outcome, out ConfrontOutcome _))
                    errors.Add($"{list} row {i + 1} needs an \"outcome\" ({string.Join(", ", Enum.GetNames(typeof(ConfrontOutcome)))}), not '{rows[i].outcome}'.");
        ConfrontPromptData[] promptRows = iv.confront?.prompts ?? Array.Empty<ConfrontPromptData>();
        for (int i = 0; i < promptRows.Length; i++)
            if (promptRows[i] != null && (!ParseEnum(promptRows[i].proof, out DiscrepancyProof _) || !ParseEnum(promptRows[i].source, out EvidenceKind _)))
                errors.Add($"interview.confront.prompts row {i + 1} needs a \"proof\" ({string.Join(", ", Enum.GetNames(typeof(DiscrepancyProof)))}) and a \"source\" (DocumentField, Answer, Appearance).");
        KindTalkData[] kindTalk = iv.kindSmallTalk ?? Array.Empty<KindTalkData>();
        for (int i = 0; i < kindTalk.Length; i++)
            if (kindTalk[i] != null)
                Names($"interview.kindSmallTalk row {i + 1}", kindTalk[i].kinds, null);

        InterviewLines built = BuildLines(iv);
        foreach ((string list, List<VoiceLine> rows) in BookLists(built.voices).Concat(new[] { ("default reactions", built.reactions), ("default slips", built.slips), ("default waiver-pad replies", built.waiverPad.replies), ("default replies to a difference", built.confront.replies) }))
            foreach (VoiceLine row in rows.Where(r => r != null))
            {
                id(row.line.id, $"the {list} row of '{VoiceOf(row)}'");
                ascii(row.line.id, row.line.text);
                if (!string.IsNullOrEmpty(row.then?.text))
                {
                    id(row.then.id, $"the {list} row of '{VoiceOf(row)}' (then)");
                    ascii(row.then.id, row.then.text);
                }
            }
        foreach (VoiceLine row in built.kindSmallTalk.Where(r => r != null))
        {
            id(row.line.id, "the kinds' small talk");
            ascii(row.line.id, row.line.text);
        }
        foreach (ConfrontPrompt prompt in built.confront.prompts.Where(p => p != null))
        {
            id(prompt.line.id, "a question about a difference");
            ascii(prompt.line.id, prompt.line.text);
            if (!string.IsNullOrEmpty(prompt.then?.text))
            {
                id(prompt.then.id, "a question about a difference (then)");
                ascii(prompt.then.id, prompt.then.text);
            }
        }
        ascii(InterviewLineId("confront.entryLabel"), built.confront.entryLabel);
        id(built.openerReturning.id, "the returning traveller's opener");
        ascii(built.openerReturning.id, built.openerReturning.text);
        if (string.IsNullOrWhiteSpace(built.openerReturning.text))
            errors.Add("interview.openerReturning is blank: the desk's opener for a traveller back after a denial ({honorific}, {day}).");
        else if (!Interview.HoldsToken(built.openerReturning.text, Returns.DeniedDayToken))
            errors.Add("interview.openerReturning must hold {day}: the day the traveller was turned away.");
        if (iv.waiverPad != null)
        {
            id(built.waiverPad.prompt.id, "the waiver pad's prompt");
            ascii(built.waiverPad.prompt.id, built.waiverPad.prompt.text);
            ascii(InterviewLineId("waiverPad.label"), built.waiverPad.label);
            if (string.IsNullOrWhiteSpace(built.waiverPad.label) || string.IsNullOrWhiteSpace(built.waiverPad.prompt.text))
                errors.Add("interview.waiverPad needs a label (the entry) and a prompt (the desk's words).");
        }

        var eraNames = src.eras.ToDictionary(e => e.id, e => e.displayName);
        ClueCategory[] asked = (src.questions ?? Array.Empty<QuestionData>()).Select(q => ParseEnum(q.category, out ClueCategory c) ? c : (ClueCategory?)null)
                                                                          .Where(c => c.HasValue).Select(c => c.Value).Distinct().ToArray();
        var input = new VoiceCheckInput
        {
            Voices = built.voices,
            KindSmallTalk = built.kindSmallTalk,
            Weights = built.smallTalkWeights,
            Cast = BuildCast(src.personalities),
            Premades = (src.premades ?? Array.Empty<PremadeData>()).Select(m => m.id).ToList(),
            Eras = src.eras.Select(e => e.id).ToList(),
            Questions = (src.questions ?? Array.Empty<QuestionData>()).Select(q => q.id).ToList(),
            SpokenRequests = (iv.requests ?? Array.Empty<RequestData>()).Select(r => r.id).ToList(),
            Requests = kindForms.SelectMany(k => k.Askable ?? Array.Empty<AskableForm>()).Select(f => FormRequests.IdOf(f.AskGroup, f.FormNumber)).Distinct().ToList(),
            KindsInPlay = kindForms.Select(k => k.Kind).Distinct().ToList(),
            MaxLineChars = iv.maxLineChars,
            LongestPlace = src.places.Select(p => OriginLabels.Format(p.displayName, eraNames.TryGetValue(p.era ?? string.Empty, out string era) ? era : null).Length).DefaultIfEmpty(0).Max(),
            LongestValue = asked.Select(c => LongestValue(src, c)).DefaultIfEmpty(0).Max(),
            LongestDocument = DocumentTemplates(authored).Select(t => (t.displayName ?? string.Empty).Length)
                              .Concat((iv.askGroups ?? Array.Empty<AskGroupData>()).Select(g => (g.label ?? string.Empty).Length)).DefaultIfEmpty(0).Max(),
            FactValues = src.places.SelectMany(p => p.facts ?? Array.Empty<FactData>()).Concat(src.present?.facts ?? Array.Empty<FactData>())
                            .Select(f => f.value).Concat((src.history?.rules ?? Array.Empty<HistoryRuleData>()).SelectMany(r => r.edits ?? Array.Empty<EditData>()).Select(e => e.value))
                            .Where(v => !string.IsNullOrWhiteSpace(v)).Distinct().ToList(),
            TransponderModels = (src.agency?.transponders ?? Array.Empty<TransponderData>()).Select(t => t.model).ToList(),
            Employers = (src.agency?.employers ?? Array.Empty<Employer>()).Where(e => e != null).Select(e => e.name).ToList(),
            DefaultReactions = built.reactions,
            DefaultSlips = built.slips,
            DefaultPadReplies = built.waiverPad.replies,
            Confront = built.confront,
            PadOffered = InterviewScript.OffersPad(kindForms.SelectMany(k => k.Askable ?? Array.Empty<AskableForm>()).ToList(), built),
            SlipChances = (src.days ?? Array.Empty<DayData>()).Where(d => d != null).Select(d => (d.day, d.slipChance)).ToList(),
            PremadeIntents = PremadeIntents(src),
            DisplacedPremades = (src.premades ?? Array.Empty<PremadeData>()).Where(m => m != null && (string.IsNullOrEmpty(m.kind) || m.kind == "None" || m.kind == nameof(TravellerKind.Displaced))).Select(m => m.id).ToList()
        };
        VoiceCheckResult result = VoiceChecks.Problems(input);
        errors.AddRange(result.Errors);
        foreach (string warning in result.Warnings)
            Debug.LogWarning($"[WorldContentGenerator] {warning}");
        foreach (string info in result.Info)
            Debug.Log($"[WorldContentGenerator] {info}");
    }

    /// <summary>
    /// Each premade's intents (VoiceCheckInput.PremadeIntents): Lying for one
    /// with a true place (every appearance lies) or a forced appearance that
    /// tells an authored lie; Honest for a pooled appearance or a forced one
    /// without a lie. A premade no day brings is left out.
    /// </summary>
    private static Dictionary<string, IReadOnlyCollection<ReactionIntent>> PremadeIntents(WorldSource src)
    {
        var intents = new Dictionary<string, HashSet<ReactionIntent>>();
        var liars = new HashSet<string>((src.premades ?? Array.Empty<PremadeData>()).Where(m => m != null && !string.IsNullOrEmpty(m.truePlace)).Select(m => m.id));
        void Add(string id, bool lie)
        {
            if (string.IsNullOrEmpty(id))
                return;
            if (!intents.TryGetValue(id, out HashSet<ReactionIntent> set))
                intents[id] = set = new HashSet<ReactionIntent>();
            set.Add(lie || liars.Contains(id) ? ReactionIntent.Lying : ReactionIntent.Honest);
        }
        foreach (DayData d in (src.days ?? Array.Empty<DayData>()).Where(d => d != null))
        {
            foreach (string id in d.premades ?? Array.Empty<string>())
                Add(id, false);
            foreach (ForcedData f in d.forced ?? Array.Empty<ForcedData>())
                if (f != null)
                    Add(f.premade, !string.IsNullOrEmpty(f.lie));
        }
        return intents.ToDictionary(kv => kv.Key, kv => (IReadOnlyCollection<ReactionIntent>)kv.Value.OrderBy(i => i).ToList());
    }

    /// <summary>The cast as the library holds it (null rows kept, so the rules report them).</summary>
    private static List<Personality> BuildCast(PersonalityData[] cast) =>
        (cast ?? Array.Empty<PersonalityData>()).Select(p => p == null ? null : new Personality
        {
            id = p.id, name = p.name, weight = p.weight, note = p.note ?? string.Empty, waiverRefusal = p.waiverRefusal, strandingFate = p.strandingFate ?? string.Empty,
            confess = p.confess
        }).ToList();

    /// <summary>Writes the cast into the library's personalities (field by field: the list holds plain rows).</summary>
    private static void WireCast(ContentLibrarySO lib, PersonalityData[] cast)
    {
        List<Personality> built = BuildCast(cast).Where(p => p != null).ToList();
        var so = new SerializedObject(lib);
        SerializedProperty list = so.FindProperty("personalities");
        list.arraySize = built.Count;
        for (int i = 0; i < built.Count; i++)
        {
            SerializedProperty row = list.GetArrayElementAtIndex(i);
            row.FindPropertyRelative("id").stringValue = built[i].id;
            row.FindPropertyRelative("name").stringValue = built[i].name;
            row.FindPropertyRelative("weight").floatValue = built[i].weight;
            row.FindPropertyRelative("note").stringValue = built[i].note;
            row.FindPropertyRelative("waiverRefusal").floatValue = built[i].waiverRefusal;
            row.FindPropertyRelative("confess").floatValue = built[i].confess;
            row.FindPropertyRelative("strandingFate").stringValue = built[i].strandingFate;
        }
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(lib);
    }

    /// <summary>The voices as the library holds them, with generated line ids ("interview.voices.{list}.{voice}.{n}", n counted per list and voice).</summary>
    private static VoiceBook BuildVoices(VoicesData v)
    {
        v = v ?? new VoicesData();
        return new VoiceBook
        {
            claims = VoiceRows("claims", v.claims, r => null),
            handOver = VoiceRows("handOver", v.handOver, r => r.request),
            missingForms = VoiceRows("missingForms", v.missingForms, r => r.request),
            spoken = VoiceRows("spoken", v.spoken, r => r.request),
            answers = VoiceRows("answers", v.answers, r => r.question),
            smallTalk = VoiceRows("smallTalk", v.smallTalk, r => null),
            reactions = VoiceRows("reactions", v.reactions, r => null),
            slips = VoiceRows("slips", v.slips, r => null),
            waiverPad = VoiceRows("waiverPad", v.waiverPad, r => r.reply),
            confront = VoiceRows("confront", v.confront, r => null)
        };
    }

    /// <summary>
    /// The wheel's questions about logged differences as the library holds
    /// them (wave 5, lesson 3): the entries' labels, the prompts
    /// ("interview.confront.prompts.{n}") and the default replies
    /// ("interview.confront.replies.{n}"). Unknown proofs or statement kinds
    /// read as the first value; CheckVoices reports them.
    /// </summary>
    private static ConfrontWording BuildConfront(ConfrontData c) => new ConfrontWording
    {
        entryLabel = c?.entryLabel ?? string.Empty,
        prompts = (c?.prompts ?? Array.Empty<ConfrontPromptData>()).Select((p, i) => p == null ? null : new ConfrontPrompt
        {
            proof = ParseEnum(p.proof, out DiscrepancyProof proof) ? proof : default,
            source = ParseEnum(p.source, out EvidenceKind source) ? source : default,
            category = p.category ?? string.Empty,
            line = new LineText(InterviewLineId($"confront.prompts.{i + 1}"), p.text),
            then = string.IsNullOrEmpty(p.then) ? new LineText() : new LineText(InterviewLineId($"confront.prompts.{i + 1}.then"), p.then)
        }).ToList(),
        replies = VoiceRows("confront", c?.replies, r => null, "confront.replies")
    };

    /// <summary>
    /// One list's rows with generated line ids: a voice's "interview.voices.{list}.{voice}.{n}" (n per list and voice), or,
    /// with <paramref name="defaults"/> (interview.reactions, interview.slips), "interview.{defaults}.{n}"; a reaction's then
    /// line "{id}.then".
    /// </summary>
    private static List<VoiceLine> VoiceRows(string list, VoiceRowData[] rows, Func<VoiceRowData, string> key, string defaults = null)
    {
        var built = new List<VoiceLine>();
        var counts = new Dictionary<string, int>();
        foreach (VoiceRowData r in rows ?? Array.Empty<VoiceRowData>())
        {
            if (r == null)
            {
                built.Add(null);
                continue;
            }
            string voice = !string.IsNullOrEmpty(r.premade) && string.IsNullOrEmpty(r.personality) ? r.premade : r.personality ?? string.Empty;
            counts[voice] = counts.TryGetValue(voice, out int n) ? n + 1 : 1;
            built.Add(new VoiceLine
            {
                personality = r.personality ?? string.Empty,
                premade = r.premade ?? string.Empty,
                kinds = Kinds(r.kinds),
                era = r.era ?? string.Empty,
                key = key(r) ?? string.Empty,
                variant = ParseEnum(r.variant, out MissingFormVariant variant) ? variant : MissingFormVariant.Honest,
                verdict = ParseEnum(r.verdict, out ReactionVerdict verdict) ? verdict : ReactionVerdict.Accepted,
                intent = ParseEnum(r.intent, out ReactionIntent intent) ? intent : ReactionIntent.Honest,
                reason = r.reason ?? string.Empty,
                lie = r.lie ?? string.Empty,
                outcome = ParseEnum(r.outcome, out ConfrontOutcome outcome) ? outcome : ConfrontOutcome.Explain,
                line = new LineText(LineIdOf(list, voice, counts[voice], defaults, built.Count + 1), r.text),
                then = string.IsNullOrEmpty(r.then) ? new LineText() : new LineText(LineIdOf(list, voice, counts[voice], defaults, built.Count + 1) + ".then", r.then)
            });
        }
        return built;
    }

    private static string LineIdOf(string list, string voice, int n, string defaults, int index) =>
        defaults != null ? InterviewLineId($"{defaults}.{index}") : InterviewLineId($"voices.{list}.{voice}.{n}");

    /// <summary>The kinds' small talk with generated line ids ("interview.kindSmallTalk.{n}").</summary>
    private static List<VoiceLine> BuildKindTalk(KindTalkData[] rows) =>
        (rows ?? Array.Empty<KindTalkData>()).Select((r, i) => r == null ? null : new VoiceLine
        {
            kinds = Kinds(r.kinds),
            era = r.era ?? string.Empty,
            line = new LineText(InterviewLineId($"kindSmallTalk.{i + 1}"), r.text)
        }).ToList();

    /// <summary>The small-talk weights (1, 1, 1 when the source has none).</summary>
    private static SmallTalkWeights BuildWeights(SmallTalkWeightsData w) =>
        w == null ? new SmallTalkWeights() : new SmallTalkWeights { personality = w.personality, home = w.home, kind = w.kind };

    /// <summary>The kinds a row names (unknown names are left out; CheckVoices reports them).</summary>
    private static List<TravellerKind> Kinds(string[] names) =>
        (names ?? Array.Empty<string>()).Where(k => ParseEnum(k, out TravellerKind _)).Select(k => (TravellerKind)Enum.Parse(typeof(TravellerKind), k)).ToList();

    private static string VoiceOf(VoiceLine row) => !string.IsNullOrEmpty(row.premade) ? row.premade : row.personality;

    private static IEnumerable<(string list, VoiceRowData[] rows)> VoiceLists(VoicesData v)
    {
        yield return ("claims", v.claims ?? Array.Empty<VoiceRowData>());
        yield return ("handOver", v.handOver ?? Array.Empty<VoiceRowData>());
        yield return ("missingForms", v.missingForms ?? Array.Empty<VoiceRowData>());
        yield return ("spoken", v.spoken ?? Array.Empty<VoiceRowData>());
        yield return ("answers", v.answers ?? Array.Empty<VoiceRowData>());
        yield return ("smallTalk", v.smallTalk ?? Array.Empty<VoiceRowData>());
        yield return ("reactions", v.reactions ?? Array.Empty<VoiceRowData>());
        yield return ("slips", v.slips ?? Array.Empty<VoiceRowData>());
        yield return ("waiverPad", v.waiverPad ?? Array.Empty<VoiceRowData>());
        yield return ("confront", v.confront ?? Array.Empty<VoiceRowData>());
    }

    private static IEnumerable<(string list, List<VoiceLine> rows)> BookLists(VoiceBook b)
    {
        yield return ("claims", b.claims);
        yield return ("handOver", b.handOver);
        yield return ("missingForms", b.missingForms);
        yield return ("spoken", b.spoken);
        yield return ("answers", b.answers);
        yield return ("smallTalk", b.smallTalk);
        yield return ("reactions", b.reactions);
        yield return ("slips", b.slips);
        yield return ("waiverPad", b.waiverPad);
        yield return ("confront", b.confront);
    }

    // -----------------------------
    // Source file shape (JsonUtility)
    // -----------------------------

    /// <summary>A personality of the cast (world_source.json "personalities").</summary>
    [Serializable] private sealed class PersonalityData { public string id; public string name; public float weight; public string note; public float waiverRefusal; public string strandingFate; public float confess; }

    /// <summary>interview.smallTalkWeights.</summary>
    [Serializable] private sealed class SmallTalkWeightsData { public float personality; public float home; public float kind; }

    /// <summary>A kind's small-talk row (interview.kindSmallTalk): the kinds that say it, an optional era, the line.</summary>
    [Serializable] private sealed class KindTalkData { public string[] kinds; public string era; public string text; }

    /// <summary>A voice row: the voice (a personality or a premade), the slot's key (a request, a question), a refusal's variant, the kinds and era it is for, the line.</summary>
    [Serializable] private sealed class VoiceRowData
    {
        public string personality;
        public string premade;
        public string request;
        public string question;
        public string variant;
        public string verdict;
        public string intent;
        public string reason;
        public string lie;
        public string reply;
        /// <summary>A reply to a question about a difference: Explain, Crack or DoubleDown (wave 5, lesson 3).</summary>
        public string outcome;
        public string[] kinds;
        public string era;
        public string text;
        public string then;
    }

    /// <summary>interview.voices: one list per slot.</summary>
    [Serializable] private sealed class VoicesData
    {
        public VoiceRowData[] claims;
        public VoiceRowData[] handOver;
        public VoiceRowData[] missingForms;
        public VoiceRowData[] spoken;
        public VoiceRowData[] answers;
        public VoiceRowData[] smallTalk;
        public VoiceRowData[] reactions;
        public VoiceRowData[] slips;
        public VoiceRowData[] waiverPad;
        public VoiceRowData[] confront;
    }

    /// <summary>interview.confront (wave 5, lesson 3): the hub entry, a difference's entry, the desk's questions, the default replies.</summary>
    [Serializable] private sealed class ConfrontData
    {
        public string entryLabel;
        public ConfrontPromptData[] prompts;
        public VoiceRowData[] replies;
    }

    /// <summary>A question about a kind of difference: its proof, its statement kind, an optional category, the desk's words.</summary>
    [Serializable] private sealed class ConfrontPromptData { public string proof; public string source; public string category; public string text; public string then; }
}
