using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// The key-word rule (the traveller-types spec's §8.1, decision I4): Saleh,
/// "if you dont have it most of the dialogue will be not in english but key
/// words will still be in english enough for the player to understand some
/// stuff in the dialogue. this we will tune later." A traveller's line is
/// formatted from a template and its slot fills; the spans that stay English
/// when it shows untranslated are the fill of each listed slot, each
/// whole-word match of a listed word (case- and accent-insensitive, anywhere
/// in the line, folded as search folds: TextMatch.Fold) and, when asked, each
/// run of digits. Data only
/// (world_source.json translation.keyWords), so tuning edits two lists.
/// </summary>
public static class KeyWords
{
    /// <summary>
    /// The character spans of the line <paramref name="template"/> gives with
    /// <paramref name="fills"/> (slot name to value; each "{slot}" replaced as
    /// Interview.Fill replaces it, other tokens left as text; fills hold no
    /// token) that stay English under <paramref name="rule"/>: each listed
    /// slot's fill, each listed word as a whole word, and digit runs when the
    /// rule asks. Sorted by start, overlapping or touching spans merged; empty
    /// for a null template or rule.
    /// </summary>
    public static IReadOnlyList<(int start, int length)> Spans(string template, IReadOnlyDictionary<string, string> fills, KeyWordRule rule)
    {
        var spans = new List<(int start, int length)>();
        if (template == null || rule == null)
            return spans;

        string text = Fill(template, fills, rule, spans);
        var map = new List<int>();
        string folded = TextMatch.Fold(text, map);
        foreach (string word in rule.words ?? new List<string>())
            if (!string.IsNullOrWhiteSpace(word))
                AddWholeWords(text, folded, map, TextMatch.Fold(word, null), spans);
        if (rule.digits)
            AddDigitRuns(text, spans);
        return Merge(spans);
    }

    /// <summary>
    /// A rule's problems (the generator and the validator report them through
    /// Translation.Problems), one message each: no rule; a slot that is blank
    /// or not one a line fills; a blank word; a word listed twice (ignoring
    /// case and accents, as matching does).
    /// </summary>
    public static List<string> Problems(KeyWordRule rule)
    {
        var problems = new List<string>();
        if (rule == null)
        {
            problems.Add("translation.keyWords: the section is missing.");
            return problems;
        }

        foreach (string slot in rule.slots ?? new List<string>())
            if (!IsSlot(slot))
                problems.Add($"translation.keyWords.slots: '{slot}' is not a slot a line fills ({Interview.PlaceToken}, {Interview.NameToken}, {Interview.DocumentToken}, {Interview.ValueToken}, {Interview.HonorificToken}).");

        var seen = new HashSet<string>();
        foreach (string word in rule.words ?? new List<string>())
        {
            if (string.IsNullOrWhiteSpace(word))
                problems.Add("translation.keyWords.words: a word is blank.");
            else if (!seen.Add(TextMatch.Fold(word, null)))
                problems.Add($"translation.keyWords.words: '{word}' is listed twice (matching ignores case and accents).");
        }
        return problems;
    }

    /// <summary>True for a slot a line can fill (Interview's tokens): the only names a rule may list.</summary>
    private static bool IsSlot(string slot) =>
        slot == Interview.PlaceToken || slot == Interview.NameToken || slot == Interview.DocumentToken ||
        slot == Interview.ValueToken || slot == Interview.HonorificToken;

    /// <summary>The template with its filled slots (one pass, left to right), adding each listed slot's non-empty fill as a span.</summary>
    private static string Fill(string template, IReadOnlyDictionary<string, string> fills, KeyWordRule rule, List<(int start, int length)> spans)
    {
        if (fills == null || fills.Count == 0)
            return template;

        var sb = new StringBuilder(template.Length);
        for (int i = 0; i < template.Length; i++)
        {
            int close = template[i] == '{' ? template.IndexOf('}', i + 1) : -1;
            string slot = close > i ? template.Substring(i + 1, close - i - 1) : null;
            if (slot == null || !fills.TryGetValue(slot, out string value))
            {
                sb.Append(template[i]);
                continue;
            }

            value = value ?? string.Empty;
            if (value.Length > 0 && rule.slots != null && rule.slots.Contains(slot))
                spans.Add((sb.Length, value.Length));
            sb.Append(value);
            i = close;
        }
        return sb.ToString();
    }

    /// <summary>Adds each whole-word match of <paramref name="word"/> (folded) in <paramref name="folded"/> (<paramref name="text"/> folded, <paramref name="map"/> its map back), as a span of the text.</summary>
    private static void AddWholeWords(string text, string folded, List<int> map, string word, List<(int start, int length)> spans)
    {
        if (word.Length == 0)
            return;
        for (int at = folded.IndexOf(word, StringComparison.Ordinal); at >= 0; at = folded.IndexOf(word, at + 1, StringComparison.Ordinal))
        {
            int end = at + word.Length;
            if ((at == 0 || !char.IsLetterOrDigit(folded[at - 1])) && (end == folded.Length || !char.IsLetterOrDigit(folded[end])))
            {
                int from = map[at];
                spans.Add((from, (end < map.Count ? map[end] : text.Length) - from));
            }
        }
    }

    /// <summary>Adds each run of the digits 0-9.</summary>
    private static void AddDigitRuns(string text, List<(int start, int length)> spans)
    {
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] < '0' || text[i] > '9')
                continue;
            int start = i;
            while (i + 1 < text.Length && text[i + 1] >= '0' && text[i + 1] <= '9')
                i++;
            spans.Add((start, i - start + 1));
        }
    }

    /// <summary>The spans sorted by start, overlapping or touching ones merged into one.</summary>
    private static List<(int start, int length)> Merge(List<(int start, int length)> spans)
    {
        spans.Sort((a, b) => a.start.CompareTo(b.start));
        var merged = new List<(int start, int length)>(spans.Count);
        foreach ((int start, int length) s in spans)
        {
            int last = merged.Count - 1;
            if (last >= 0 && s.start <= merged[last].start + merged[last].length)
            {
                int end = Math.Max(merged[last].start + merged[last].length, s.start + s.length);
                merged[last] = (merged[last].start, end - merged[last].start);
            }
            else
            {
                merged.Add(s);
            }
        }
        return merged;
    }
}
