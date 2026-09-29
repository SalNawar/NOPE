using System;
using System.Collections.Generic;
using System.Globalization;
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
            foreach (var slot in Slots)
                (RowsOf(book, slot.list).Exists(r => r != null && string.IsNullOrEmpty(r.premade) && r.personality == p.id) ? own : fallback).Add(slot.list);
            result.Info.Add(own.Count == 0 ? $"Personality '{p.id}' ({p.name}) says the defaults in every slot."
                          : fallback.Count == 0 ? $"Personality '{p.id}' ({p.name}) has its own lines for every slot."
                          : $"Personality '{p.id}' ({p.name}) has its own lines for {string.Join(", ", own)}; the defaults speak its {string.Join(", ", fallback)}.");
        }

        return result;
    }

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
    private static void CheckText(string text, string owner, string noun, string[] allowed, string required, VoiceCheckInput input, VoiceCheckResult result)
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

        Guard(text, input.FactValues, "the checkable value", owner, result);
        Guard(text, input.TransponderModels, "the transponder model", owner, result);
        Guard(text, input.Employers, "the employer", owner, result);
    }

    /// <summary>The fact guard (V8): a line naming one of <paramref name="values"/> warns (a voice line is never evidence, and a fact written into a line goes stale when history edits it).</summary>
    private static void Guard(string text, IReadOnlyCollection<string> values, string what, string owner, VoiceCheckResult result)
    {
        foreach (string value in values ?? Array.Empty<string>())
            if (!string.IsNullOrWhiteSpace(value) && value.Length >= 3 && text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                result.Warnings.Add($"{owner} names {what} '{value}'; a voice line is never evidence (the fact guard).");
                return;
            }
    }

    private static bool Same(VoiceLine a, VoiceLine b) =>
        a.personality == b.personality && a.premade == b.premade && (a.era ?? string.Empty) == (b.era ?? string.Empty) && (a.key ?? string.Empty) == (b.key ?? string.Empty) &&
        a.variant == b.variant && (a.line?.text ?? string.Empty) == (b.line?.text ?? string.Empty) && SameKinds(a.kinds, b.kinds);

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
            _ => book.smallTalk
        }) ?? new List<VoiceLine>();
}
