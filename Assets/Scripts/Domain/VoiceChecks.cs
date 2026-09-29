using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>What the voice lines' content rules read: the rows, the cast and every list a row may name, and the fills the length check assumes. Generate World fills it from the source, the validator from the library.</summary>
public sealed class VoiceCheckInput
{
    /// <summary>The personalities' and premades' rows (interview.voices).</summary>
    public VoiceBook Voices = new VoiceBook();

    /// <summary>The kinds' small talk (interview.kindSmallTalk).</summary>
    public List<VoiceLine> KindSmallTalk = new List<VoiceLine>();

    /// <summary>How small talk picks its source (interview.smallTalkWeights).</summary>
    public SmallTalkWeights Weights = new SmallTalkWeights();

    /// <summary>The cast (personalities).</summary>
    public IReadOnlyList<Personality> Cast = Array.Empty<Personality>();

    /// <summary>The premades' ids.</summary>
    public IReadOnlyCollection<string> Premades = Array.Empty<string>();

    /// <summary>The eras' ids.</summary>
    public IReadOnlyCollection<string> Eras = Array.Empty<string>();

    /// <summary>The questions' ids.</summary>
    public IReadOnlyCollection<string> Questions = Array.Empty<string>();

    /// <summary>The spoken requests' ids (interview.requests).</summary>
    public IReadOnlyCollection<string> SpokenRequests = Array.Empty<string>();

    /// <summary>The requests some day's papers menu offers (FormRequests.IdOf: a form number or a group id).</summary>
    public IReadOnlyCollection<string> Requests = Array.Empty<string>();

    /// <summary>The kinds some day's blueprints make.</summary>
    public IEnumerable<TravellerKind> KindsInPlay = Array.Empty<TravellerKind>();

    /// <summary>The longest line a transcript row holds (interview.maxLineChars; 0 or less skips the length check).</summary>
    public int MaxLineChars;

    /// <summary>The longest {place} fill (the longest origin label).</summary>
    public int LongestPlace;

    /// <summary>The longest {value} fill (the longest fact value of any category).</summary>
    public int LongestValue;

    /// <summary>The longest {document} fill (the longest request label).</summary>
    public int LongestDocument;

    /// <summary>Every checkable fact value (every place's and the present's, and the history edits').</summary>
    public IReadOnlyCollection<string> FactValues = Array.Empty<string>();

    /// <summary>The transponder models (agency.transponders).</summary>
    public IReadOnlyCollection<string> TransponderModels = Array.Empty<string>();

    /// <summary>The employers' names (agency.employers).</summary>
    public IReadOnlyCollection<string> Employers = Array.Empty<string>();

    /// <summary>The default reactions (interview.reactions).</summary>
    public List<VoiceLine> DefaultReactions = new List<VoiceLine>();

    /// <summary>The default slips (interview.slips).</summary>
    public List<VoiceLine> DefaultSlips = new List<VoiceLine>();

    /// <summary>Each day's slip chance (days[].slipChance), by day number.</summary>
    public IReadOnlyList<(int day, float chance)> SlipChances = Array.Empty<(int, float)>();

    /// <summary>
    /// The premades whose lines must cover every slot they can reach (the
    /// personalities spec's PS3, §4.5), with the intents they can stand with
    /// (Lying for a premade with a true place or an appearance that tells an
    /// authored lie; Honest for any other appearance). A premade not listed is
    /// not checked.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyCollection<ReactionIntent>> PremadeIntents = new Dictionary<string, IReadOnlyCollection<ReactionIntent>>();

    /// <summary>The premades who are displaced (they refuse the manifest, the waiver and the proof of means in their own words).</summary>
    public IReadOnlyCollection<string> DisplacedPremades = Array.Empty<string>();
}

/// <summary>What the voice lines' rules found: errors (Generate World writes nothing, the validator fails), warnings and info lines (each personality's coverage).</summary>
public sealed class VoiceCheckResult
{
    /// <summary>Problems that stop Generate World.</summary>
    public readonly List<string> Errors = new List<string>();

    /// <summary>Lines an author should look at (the fact guard, duplicates).</summary>
    public readonly List<string> Warnings = new List<string>();

    /// <summary>Each personality's coverage (the slots that fall back to the defaults), one line per personality (the personalities spec's C4).</summary>
    public readonly List<string> Info = new List<string>();
}

/// <summary>
/// The voice lines' content rules (the personalities spec's C3, C4, V7, V8,
/// §9.2), the one rule Generate World and the content validator share: every
/// row names exactly one voice of the cast or the premades, a known era and a
/// key its slot knows; its text is not blank, holds its slot's required token
/// and only its slot's tokens (V7), and fits interview.maxLineChars with the
/// longest fills; a line naming a checkable value (a fact value, a transponder
/// model, an employer) warns, as a row identical to an earlier one does; the
/// kinds' small talk names kinds and covers every kind in play; the
/// small-talk weights are not negative and one is above 0; and an info line
/// lists each personality's fallbacks. Pure.
/// </summary>
public static class VoiceChecks
{
    private static readonly Regex Token = new Regex(@"\{(\w+)\}");

    /// <summary>The slots' names as the JSON and the messages name them, their allowed tokens and their required one.</summary>
    private static readonly (string list, string noun, string[] allowed, string required, string key)[] Slots =
    {
        ("claims", "a claim", new[] { Interview.PlaceToken }, Interview.PlaceToken, null),
        ("handOver", "a hand-over", new[] { Interview.DocumentToken, Interview.PlaceToken }, null, "request?"),
        ("missingForms", "a refusal", new[] { Interview.DocumentToken, Interview.PlaceToken }, null, "request"),
        ("spoken", "a spoken request's reply", new[] { Interview.PlaceToken }, null, "spoken"),
        ("answers", "an answer", new[] { Interview.ValueToken, Interview.PlaceToken }, Interview.ValueToken, "question"),
        ("smallTalk", "small talk", new[] { Interview.PlaceToken }, null, null)
    };

    /// <summary>Every problem of the voice lines in <paramref name="input"/>.</summary>
    public static VoiceCheckResult Problems(VoiceCheckInput input)
    {
        var result = new VoiceCheckResult();
        if (input == null)
            return result;
        VoiceBook book = input.Voices ?? new VoiceBook();
        var cast = new HashSet<string>(StringComparer.Ordinal);
        foreach (Personality p in input.Cast ?? Array.Empty<Personality>())
            if (p != null && !string.IsNullOrWhiteSpace(p.id))
                cast.Add(p.id);

        foreach ((string list, string noun, string[] allowed, string required, string key) in Slots)
        {
            List<VoiceLine> rows = RowsOf(book, list);
            for (int i = 0; i < rows.Count; i++)
            {
                VoiceLine row = rows[i];
                if (row == null)
                {
                    result.Errors.Add($"interview.voices.{list} row {i + 1} is empty.");
                    continue;
                }

                string at = $"interview.voices.{list} row {i + 1}";
                CheckVoice(row, at, cast, input, result);
                string owner = $"{at} ({Name(row)})";
                CheckEra(row.era, owner, input, result);
                CheckKey(row, key, owner, input, result);
                CheckText(row.line?.text, owner, noun, allowed, required, input, result);
                for (int j = 0; j < i; j++)
                    if (rows[j] != null && Same(rows[j], row))
                    {
                        result.Warnings.Add($"{owner} repeats row {j + 1} in every column.");
                        break;
                    }
            }
        }

        CheckAfterRows(book.reactions, "interview.voices.reactions", true, true, cast, input, result);
        CheckAfterRows(input.DefaultReactions, "interview.reactions", false, true, cast, input, result);
        CheckAfterRows(book.slips, "interview.voices.slips", true, false, cast, input, result);
        CheckAfterRows(input.DefaultSlips, "interview.slips", false, false, cast, input, result);

        foreach (ReactionVerdict verdict in new[] { ReactionVerdict.Accepted, ReactionVerdict.Denied })
            foreach (ReactionIntent intent in new[] { ReactionIntent.Honest, ReactionIntent.Lying })
            {
                if (!(input.DefaultReactions ?? new List<VoiceLine>()).Exists(r => IsBase(r, verdict, intent)))
                    result.Errors.Add($"interview.reactions has no base row for {verdict} · {intent} (blank reason, kinds and era): the four defaults are the safety net every voice falls back to.");
                foreach (Personality p in input.Cast ?? Array.Empty<Personality>())
                    if (p != null && !string.IsNullOrWhiteSpace(p.id) && p.weight > 0f &&
                        !(book.reactions ?? new List<VoiceLine>()).Exists(r => IsBase(r, verdict, intent) && string.IsNullOrEmpty(r.premade) && r.personality == p.id))
                        result.Errors.Add($"Personality '{p.id}' ({p.name}) has no base reaction for {verdict} · {intent} (blank reason, kinds and era); every personality in the draw authors its four (the personalities spec's R3).");
            }
        if (!(input.DefaultSlips ?? new List<VoiceLine>()).Exists(r => r != null && string.IsNullOrWhiteSpace(r.lie) && (r.kinds == null || r.kinds.Count == 0) && string.IsNullOrWhiteSpace(r.era)))
            result.Errors.Add("interview.slips has no row with a blank lie, kinds and era: every liar who slips needs a default to fall back to.");
        foreach ((int day, float chance) in input.SlipChances ?? Array.Empty<(int, float)>())
            if (!(chance >= 0f && chance <= 1f))
                result.Errors.Add($"days: day {day}'s slipChance is {chance.ToString("0.###", CultureInfo.InvariantCulture)}; it must be within 0 and 1.");

        CheckPremades(book, input, result);

        List<VoiceLine> kindTalk = input.KindSmallTalk ?? new List<VoiceLine>();
        var talked = new HashSet<TravellerKind>();
        for (int i = 0; i < kindTalk.Count; i++)
        {
            VoiceLine row = kindTalk[i];
            string owner = $"interview.kindSmallTalk row {i + 1}";
            if (row == null)
            {
                result.Errors.Add($"{owner} is empty.");
                continue;
            }
            if (row.kinds == null || row.kinds.Count == 0)
                result.Errors.Add($"{owner} names no kind; a kind's small talk names the kinds that say it.");
            else
                talked.UnionWith(row.kinds);
            CheckEra(row.era, owner, input, result);
            CheckText(row.line?.text, owner, "small talk", new[] { Interview.PlaceToken }, null, input, result);
        }
        foreach (TravellerKind kind in input.KindsInPlay ?? Array.Empty<TravellerKind>())
            if (!talked.Contains(kind))
                result.Errors.Add($"interview.kindSmallTalk: {kind} travellers are in play, but no row names them.");

        SmallTalkWeights w = input.Weights ?? new SmallTalkWeights();
        foreach ((string name, float weight) in new[] { ("personality", w.personality), ("home", w.home), ("kind", w.kind) })
            if (weight < 0f)
                result.Errors.Add($"interview.smallTalkWeights.{name} is negative ({weight.ToString("0.###", CultureInfo.InvariantCulture)}).");
        if (!(w.personality > 0f || w.home > 0f || w.kind > 0f))
            result.Errors.Add("interview.smallTalkWeights: no source has a weight above 0, so no traveller makes small talk.");

        foreach (Personality p in input.Cast ?? Array.Empty<Personality>())
        {
            if (p == null || string.IsNullOrWhiteSpace(p.id))
                continue;
            var own = new List<string>();
            var fallback = new List<string>();
            foreach (string list in Array.ConvertAll(Slots, s => s.list).Concat(new[] { "reactions", "slips" }))
                (RowsOf(book, list).Exists(r => r != null && string.IsNullOrEmpty(r.premade) && r.personality == p.id) ? own : fallback).Add(list);
            result.Info.Add(own.Count == 0 ? $"Personality '{p.id}' ({p.name}) says the defaults in every slot."
                          : fallback.Count == 0 ? $"Personality '{p.id}' ({p.name}) has its own lines for every slot."
                          : $"Personality '{p.id}' ({p.name}) has its own lines for {string.Join(", ", own)}; the defaults speak its {string.Join(", ", fallback)}.");
        }

        return result;
    }

    /// <summary>
    /// The reactions' or the slips' rows (<paramref name="reaction"/>): a voice
    /// row names one voice, a default none; a known era; a reaction's reason is
    /// blank or one of Faults.Reasons, a slip's lie blank or a lie kind; the
    /// line and a reaction's then line hold only {place} and fit; a slip
    /// naming a checkable value is refused (T11), any other line warns.
    /// </summary>
    private static void CheckAfterRows(List<VoiceLine> rows, string list, bool voiced, bool reaction, HashSet<string> cast, VoiceCheckInput input, VoiceCheckResult result)
    {
        rows = rows ?? new List<VoiceLine>();
        string noun = reaction ? "a reaction" : "a slip";
        for (int i = 0; i < rows.Count; i++)
        {
            VoiceLine row = rows[i];
            string at = $"{list} row {i + 1}";
            if (row == null)
            {
                result.Errors.Add($"{at} is empty.");
                continue;
            }
            if (voiced)
                CheckVoice(row, at, cast, input, result);
            else if (!string.IsNullOrWhiteSpace(row.personality) || !string.IsNullOrWhiteSpace(row.premade))
                result.Errors.Add($"{at} names a voice; a default row names none (voices' own rows go in interview.voices).");
            string owner = voiced ? $"{at} ({Name(row)})" : at;
            CheckEra(row.era, owner, input, result);
            if (reaction && !string.IsNullOrWhiteSpace(row.reason) && Array.IndexOf(Faults.Reasons, row.reason) < 0)
                result.Errors.Add($"{owner} names the reason '{row.reason}' ({string.Join(", ", Faults.Reasons)}).");
            if (!reaction && !string.IsNullOrWhiteSpace(row.lie) && !Enum.IsDefined(typeof(LieKind), row.lie))
                result.Errors.Add($"{owner} names the lie kind '{row.lie}' ({string.Join(", ", Enum.GetNames(typeof(LieKind)))}).");
            CheckText(row.line?.text, owner, noun, new[] { Interview.PlaceToken }, null, input, result, !reaction);
            if (reaction && row.then != null && !string.IsNullOrWhiteSpace(row.then.text))
                CheckText(row.then.text, owner + " (then)", noun, new[] { Interview.PlaceToken }, null, input, result, false);
            for (int j = 0; j < i; j++)
                if (rows[j] != null && Same(rows[j], row))
                {
                    result.Warnings.Add($"{owner} repeats row {j + 1} in every column.");
                    break;
                }
        }
    }

    /// <summary>
    /// Every listed premade speaks its own line in every slot it can reach
    /// (PS3, §4.5): a claim, a hand-over, each spoken request, each question,
    /// small talk, a displaced premade's Honest refusals of the manifest, the
    /// waiver and the proof of means (those some menu offers), the Accepted and
    /// Denied reactions of each intent it can stand with; a slip only for a
    /// premade that lies.
    /// </summary>
    private static void CheckPremades(VoiceBook book, VoiceCheckInput input, VoiceCheckResult result)
    {
        foreach (KeyValuePair<string, IReadOnlyCollection<ReactionIntent>> premade in input.PremadeIntents ?? new Dictionary<string, IReadOnlyCollection<ReactionIntent>>())
        {
            string id = premade.Key;
            var missing = new List<string>();
            bool Has(List<VoiceLine> rows, Func<VoiceLine, bool> match) =>
                (rows ?? new List<VoiceLine>()).Exists(r => r != null && r.premade == id && r.line != null && !string.IsNullOrWhiteSpace(r.line.text) && match(r));

            if (!Has(book.claims, _ => true)) missing.Add("the claim");
            if (!Has(book.handOver, _ => true)) missing.Add("the hand-over");
            foreach (string spoken in input.SpokenRequests ?? Array.Empty<string>())
                if (!Has(book.spoken, r => r.key == spoken)) missing.Add($"the spoken request '{spoken}'");
            foreach (string question in input.Questions ?? Array.Empty<string>())
                if (!Has(book.answers, r => r.key == question)) missing.Add($"the answer to '{question}'");
            if (!Has(book.smallTalk, _ => true)) missing.Add("small talk");
            if (Contains(input.DisplacedPremades, id))
                foreach (string request in new[] { "TC-230", "TC-310", "proof" })
                    if (Contains(input.Requests, request) && !Has(book.missingForms, r => r.key == request && r.variant == MissingFormVariant.Honest))
                        missing.Add($"the refusal of '{request}'");
            ICollection<ReactionIntent> intents = new List<ReactionIntent>(premade.Value ?? Array.Empty<ReactionIntent>());
            foreach (ReactionIntent intent in intents)
                foreach (ReactionVerdict verdict in new[] { ReactionVerdict.Accepted, ReactionVerdict.Denied })
                    if (!Has(book.reactions, r => r.verdict == verdict && r.intent == intent && string.IsNullOrWhiteSpace(r.reason)))
                        missing.Add($"the {verdict} · {intent} reaction");
            if (missing.Count > 0)
                result.Errors.Add($"The premade '{id}' has no line of its own for {string.Join(", ", missing)}; a premade speaks only its own lines (the personalities spec's PS3).");
            if (!intents.Contains(ReactionIntent.Lying) && Has(book.slips, _ => true))
                result.Errors.Add($"The premade '{id}' never lies, so it never slips; its slip row is never said.");
        }
    }

    /// <summary>A base reaction of <paramref name="verdict"/> and <paramref name="intent"/>: no reason, kinds or era.</summary>
    private static bool IsBase(VoiceLine r, ReactionVerdict verdict, ReactionIntent intent) =>
        r != null && r.verdict == verdict && r.intent == intent && string.IsNullOrWhiteSpace(r.reason) && (r.kinds == null || r.kinds.Count == 0) && string.IsNullOrWhiteSpace(r.era)
        && r.line != null && !string.IsNullOrWhiteSpace(r.line.text);

    private static void CheckVoice(VoiceLine row, string at, HashSet<string> cast, VoiceCheckInput input, VoiceCheckResult result)
    {
        bool personality = !string.IsNullOrWhiteSpace(row.personality), premade = !string.IsNullOrWhiteSpace(row.premade);
        if (!personality && !premade)
            result.Errors.Add($"{at} names no personality and no premade; a voice row names exactly one.");
        else if (personality && premade)
            result.Errors.Add($"{at} names both a personality and a premade; a voice row names exactly one.");
        else if (personality && !cast.Contains(row.personality))
            result.Errors.Add($"{at} names the personality '{row.personality}', which personalities does not list.");
        else if (premade && !Contains(input.Premades, row.premade))
            result.Errors.Add($"{at} names the premade '{row.premade}', which premades does not list.");
    }

    private static void CheckEra(string era, string owner, VoiceCheckInput input, VoiceCheckResult result)
    {
        if (!string.IsNullOrWhiteSpace(era) && !Contains(input.Eras, era))
            result.Errors.Add($"{owner} names the era '{era}', which eras does not list.");
    }

    private static void CheckKey(VoiceLine row, string key, string owner, VoiceCheckInput input, VoiceCheckResult result)
    {
        bool blank = string.IsNullOrWhiteSpace(row.key);
        switch (key)
        {
            case "request":
            case "request?":
                if (blank && key == "request")
                    result.Errors.Add($"{owner} names no request (a form number or a request group's id).");
                else if (!blank && !Contains(input.Requests, row.key))
                    result.Errors.Add($"{owner} names the request '{row.key}', which no day's papers menu offers.");
                return;
            case "spoken":
                if (blank)
                    result.Errors.Add($"{owner} names no spoken request.");
                else if (!Contains(input.SpokenRequests, row.key))
                    result.Errors.Add($"{owner} names the spoken request '{row.key}', which interview.requests does not list.");
                return;
            case "question":
                if (blank)
                    result.Errors.Add($"{owner} names no question.");
                else if (!Contains(input.Questions, row.key))
                    result.Errors.Add($"{owner} names the question '{row.key}', which questions does not list.");
                return;
        }
    }

    /// <summary>A line's text: not blank, its required token, only its slot's tokens, the worst case within the limit, and the fact guard.</summary>
    private static void CheckText(string text, string owner, string noun, string[] allowed, string required, VoiceCheckInput input, VoiceCheckResult result, bool refuseFacts = false)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            result.Errors.Add($"{owner} is blank.");
            return;
        }

        if (required != null && !Interview.HoldsToken(text, required))
            result.Errors.Add($"{owner} must hold {Interview.Placeholder(required)}.");
        foreach (Match m in Token.Matches(text))
        {
            string token = m.Groups[1].Value;
            if (token != Interview.PlaceToken && token != Interview.ValueToken && token != Interview.DocumentToken)
                result.Errors.Add($"{owner} holds the unknown token {m.Value}.");
            else if (Array.IndexOf(allowed, token) < 0)
                result.Errors.Add($"{owner} holds {m.Value}, which {noun} cannot fill (only {string.Join(", ", Array.ConvertAll(allowed, Interview.Placeholder))}).");
        }

        if (input.MaxLineChars > 0)
        {
            int length = text.Length
                + Interview.WorstCaseLength(text, Interview.PlaceToken, input.LongestPlace) - text.Length
                + Interview.WorstCaseLength(text, Interview.ValueToken, input.LongestValue) - text.Length
                + Interview.WorstCaseLength(text, Interview.DocumentToken, input.LongestDocument) - text.Length;
            if (length > input.MaxLineChars)
                result.Errors.Add($"{owner} can render {length} characters with the longest fills; the transcript holds at most {input.MaxLineChars} (interview.maxLineChars).");
        }

        Guard(text, input.FactValues, "the checkable value", owner, result, refuseFacts);
        Guard(text, input.TransponderModels, "the transponder model", owner, result, refuseFacts);
        Guard(text, input.Employers, "the employer", owner, result, refuseFacts);
    }

    /// <summary>The fact guard (V8): a line naming one of <paramref name="values"/> warns (a voice line is never evidence, and a fact written into a line goes stale when history edits it).</summary>
    private static void Guard(string text, IReadOnlyCollection<string> values, string what, string owner, VoiceCheckResult result, bool refuse)
    {
        foreach (string value in values ?? Array.Empty<string>())
            if (!string.IsNullOrWhiteSpace(value) && value.Length >= 3 && text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (refuse)
                    result.Errors.Add($"{owner} names {what} '{value}'; a slip hints and never names a value (the personalities spec's T11).");
                else
                    result.Warnings.Add($"{owner} names {what} '{value}'; a voice line is never evidence (the fact guard).");
                return;
            }
    }

    private static bool Same(VoiceLine a, VoiceLine b) =>
        a.personality == b.personality && a.premade == b.premade && (a.era ?? string.Empty) == (b.era ?? string.Empty) && (a.key ?? string.Empty) == (b.key ?? string.Empty) &&
        a.variant == b.variant && (a.line?.text ?? string.Empty) == (b.line?.text ?? string.Empty) && SameKinds(a.kinds, b.kinds) &&
        a.verdict == b.verdict && a.intent == b.intent && (a.reason ?? string.Empty) == (b.reason ?? string.Empty) && (a.lie ?? string.Empty) == (b.lie ?? string.Empty) &&
        (a.then?.text ?? string.Empty) == (b.then?.text ?? string.Empty);

    private static bool SameKinds(List<TravellerKind> a, List<TravellerKind> b)
    {
        var x = new HashSet<TravellerKind>(a ?? new List<TravellerKind>());
        return x.SetEquals(b ?? new List<TravellerKind>());
    }

    private static string Name(VoiceLine row) => !string.IsNullOrWhiteSpace(row.premade) && string.IsNullOrWhiteSpace(row.personality) ? row.premade : row.personality;

    private static bool Contains(IReadOnlyCollection<string> values, string value)
    {
        foreach (string v in values ?? Array.Empty<string>())
            if (v == value)
                return true;
        return false;
    }

    /// <summary>A slot's rows by its JSON name.</summary>
    private static List<VoiceLine> RowsOf(VoiceBook book, string list) =>
        (list switch
        {
            "claims" => book.claims,
            "handOver" => book.handOver,
            "missingForms" => book.missingForms,
            "spoken" => book.spoken,
            "answers" => book.answers,
            "reactions" => book.reactions,
            "slips" => book.slips,
            _ => book.smallTalk
        }) ?? new List<VoiceLine>();
}
