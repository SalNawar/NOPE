using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

/// <summary>
/// A pasted foreign clip as search reads it (the PC redesign CP3, SE5): the
/// tongue of the untranslated transcript line it was copied from and that
/// line's canonical text. The canonical text is matched only against
/// untranslated lines of the same tongue; it is never shown and never matched
/// against anything typed.
/// </summary>
public readonly struct SearchChip
{
    /// <summary>A chip of <paramref name="tongueId"/> holding <paramref name="canonical"/>.</summary>
    public SearchChip(string tongueId, string canonical)
    {
        TongueId = tongueId;
        Canonical = canonical;
    }

    /// <summary>The copied line's tongue id.</summary>
    public string TongueId { get; }

    /// <summary>The copied line's canonical text (for matching only).</summary>
    public string Canonical { get; }
}

/// <summary>
/// What the player searches for (the PC redesign SE1, SE3): the typed words
/// and "quoted phrases", folded (TextMatch.Fold), and an optional chip (a
/// pasted foreign clip). A quote left open reads its words as words.
/// </summary>
public sealed class SearchQuery
{
    private SearchQuery(List<string> words, List<string> phrases, SearchChip? chip, string whole, int typedCharacters, bool digit)
    {
        Words = words;
        Phrases = phrases;
        Chip = chip;
        Whole = whole;
        IsEmpty = words.Count == 0 && phrases.Count == 0 && !chip.HasValue;
        IsSearchable = chip.HasValue || typedCharacters >= 2 || digit;
    }

    /// <summary>The query <paramref name="typed"/> (null reads as blank) with an optional <paramref name="chip"/>.</summary>
    public static SearchQuery Parse(string typed, SearchChip? chip = null)
    {
        var words = new List<string>();
        var phrases = new List<string>();
        string text = typed ?? string.Empty;
        int typedCharacters = 0;
        bool digit = false;
        bool quoted = false;
        var part = new StringBuilder();
        for (int i = 0; i <= text.Length; i++)
        {
            bool end = i == text.Length;
            if (!end && text[i] != '"')
            {
                part.Append(text[i]);
                continue;
            }

            string folded = TextMatch.Fold(part.ToString(), null);
            List<string> partWords = TextMatch.Words(folded);
            foreach (string w in partWords)
            {
                typedCharacters += w.Length;
                foreach (char c in w)
                    digit |= char.IsDigit(c);
            }
            if (quoted && !end && partWords.Count > 0)
                phrases.Add(string.Join(" ", partWords));
            else
                words.AddRange(partWords);
            part.Clear();
            quoted = !quoted;
        }

        return new SearchQuery(words, phrases, chip, string.Join(" ", TextMatch.Words(TextMatch.Fold(text, null))), typedCharacters, digit);
    }

    /// <summary>The typed words outside quotes, folded, in order.</summary>
    public IReadOnlyList<string> Words { get; }

    /// <summary>The quoted phrases, folded, their words joined by single spaces.</summary>
    public IReadOnlyList<string> Phrases { get; }

    /// <summary>The pasted foreign clip, if any.</summary>
    public SearchChip? Chip { get; }

    /// <summary>True with no word, no phrase and no chip.</summary>
    public bool IsEmpty { get; }

    /// <summary>
    /// True when the query is long enough to search (SE1): a chip, at least
    /// two typed letters or digits, or one digit. A shorter query gives no
    /// results.
    /// </summary>
    public bool IsSearchable { get; }

    /// <summary>All the typed words, folded, in order and joined by single spaces (the whole-value rank).</summary>
    internal string Whole { get; }
}

/// <summary>A marked part of a shown text: where a query matched (its start and length in the text as shown).</summary>
public readonly struct Mark
{
    /// <summary>A mark from <paramref name="start"/>, <paramref name="length"/> characters long.</summary>
    public Mark(int start, int length)
    {
        Start = start;
        Length = length;
    }

    /// <summary>The first marked character.</summary>
    public int Start { get; }

    /// <summary>How many characters are marked.</summary>
    public int Length { get; }
}

/// <summary>
/// Search's one matcher (the PC redesign SE3, SE5, SE6): case- and
/// accent-insensitive (lower case, Unicode decomposition with the marks
/// dropped, anything but a letter or a digit read as a space); each typed word
/// must start a word of the entry ("drach" finds "Drachma"), each quoted
/// phrase must appear as written (its words whole and in order). Typed words
/// read an entry's label and text only; an untranslated line's text is its
/// key words, so its hidden English is never read, and a chip matches only an
/// untranslated line of the same tongue whose canonical text is equal. The
/// rank: the whole value equal to the query (100), then +40 per word or
/// phrase also in the title, +20 in the text, +10 in the label. No fuzzy
/// matching. Pure; the key-word rule and the Lineage Archive fold and match
/// through it too.
/// </summary>
public static class TextMatch
{
    /// <summary>The rank of a whole value equal to the query (a record's number or name: SE6).</summary>
    public const int WholeValueScore = 100;

    /// <summary>The rank of a word or phrase also in the title.</summary>
    public const int TitleScore = 40;

    /// <summary>The rank of a word or phrase in the text.</summary>
    public const int TextScore = 20;

    /// <summary>The rank of a word or phrase in the label.</summary>
    public const int LabelScore = 10;

    /// <summary>
    /// The text folded for matching ("" for null): each character in lower
    /// case as its first letter in canonical decomposition ("Ḥ" to "h"),
    /// combining marks dropped, and anything that is not then a letter or a
    /// digit read as a space. <paramref name="map"/> (null skips it) is
    /// cleared and receives, per folded character, the index in
    /// <paramref name="text"/> of the character it folds.
    /// </summary>
    public static string Fold(string text, List<int> map)
    {
        map?.Clear();
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var sb = new StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category == UnicodeCategory.NonSpacingMark || category == UnicodeCategory.SpacingCombiningMark || category == UnicodeCategory.EnclosingMark)
                continue;

            char folded = c;
            if (c > '\u007f')
            {
                string decomposed = c.ToString().Normalize(NormalizationForm.FormD);
                if (decomposed.Length > 0)
                    folded = decomposed[0];
            }
            folded = char.ToLowerInvariant(folded);
            sb.Append(char.IsLetterOrDigit(folded) ? folded : ' ');
            map?.Add(i);
        }
        return sb.ToString();
    }

    /// <summary>
    /// True when <paramref name="q"/> finds <paramref name="entry"/>: its chip
    /// (if any) is this untranslated line (same tongue, equal canonical text,
    /// folded), and each typed word and phrase is in the entry's label or
    /// text. <paramref name="score"/> is the rank (0 when not found);
    /// <paramref name="marks"/> (cleared) receives the matched parts of the
    /// text as shown, sorted and merged (none for an untranslated line, whose
    /// snippet is its glyphs). An empty query finds nothing.
    /// </summary>
    public static bool Matches(SearchQuery q, IndexEntry entry, out int score, List<Mark> marks)
    {
        score = 0;
        marks?.Clear();
        if (q == null || entry == null || q.IsEmpty)
            return false;

        FoldedEntry f = entry.Folded();
        if (q.Chip.HasValue)
        {
            SearchChip chip = q.Chip.Value;
            if (!entry.Foreign || entry.TongueId != chip.TongueId || f.Canonical != string.Join(" ", Words(Fold(chip.Canonical, null))))
                return false;
            score += WholeValueScore;
        }

        var found = new List<(int start, int length)>();
        foreach (string word in q.Words)
            if (!Score(f, word, false, found, ref score))
                return false;
        foreach (string phrase in q.Phrases)
            if (!Score(f, phrase, true, found, ref score))
                return false;

        if (q.Whole.Length > 0 && f.TextWords == q.Whole)
            score += WholeValueScore;

        if (marks != null && !entry.Foreign)
            marks.AddRange(ToMarks(found, f.TextMap, entry.Text ?? string.Empty));
        return true;
    }

    /// <summary>True when each typed word and phrase of <paramref name="q"/> is in <paramref name="text"/> (a blank query finds every text; a chip is not read): the Lineage Archive's name search.</summary>
    public static bool MatchesText(SearchQuery q, string text)
    {
        if (q == null || (q.Words.Count == 0 && q.Phrases.Count == 0))
            return true;
        string folded = Fold(text, null);
        foreach (string word in q.Words)
            if (Find(folded, word, false, null) < 0)
                return false;
        foreach (string phrase in q.Phrases)
            if (Find(folded, phrase, true, null) < 0)
                return false;
        return true;
    }

    /// <summary>The words of a folded text (its runs of non-spaces), in order.</summary>
    internal static List<string> Words(string folded) =>
        new List<string>((folded ?? string.Empty).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));

    /// <summary>
    /// Scores one word or phrase against the entry: false when it is in
    /// neither the label nor the text; else adds its title, text and label
    /// ranks, and its places in the text to <paramref name="found"/>.
    /// </summary>
    private static bool Score(FoldedEntry f, string part, bool phrase, List<(int start, int length)> found, ref int score)
    {
        bool inText = Find(f.Text, part, phrase, found) >= 0;
        bool inLabel = Find(f.Label, part, phrase, null) >= 0;
        if (!inText && !inLabel)
            return false;
        if (Find(f.Title, part, phrase, null) >= 0)
            score += TitleScore;
        if (inText)
            score += TextScore;
        if (inLabel)
            score += LabelScore;
        return true;
    }

    /// <summary>
    /// The first place in <paramref name="folded"/> where <paramref name="part"/>
    /// starts a word (a word: its start; a phrase: its words whole, in order,
    /// any spacing between them), or -1. <paramref name="found"/> (null skips
    /// it) receives every such place (start, length in the folded text).
    /// </summary>
    private static int Find(string folded, string part, bool phrase, List<(int start, int length)> found)
    {
        if (string.IsNullOrEmpty(folded) || string.IsNullOrEmpty(part))
            return -1;

        List<string> phraseWords = phrase ? Words(part) : null;
        int first = -1;
        for (int at = 0; at < folded.Length; at++)
        {
            if (folded[at] == ' ' || (at > 0 && folded[at - 1] != ' '))
                continue;
            int length = phrase ? PhraseAt(folded, at, phraseWords) : WordAt(folded, at, part);
            if (length <= 0)
                continue;
            if (first < 0)
                first = at;
            if (found == null)
                return first;
            found.Add((at, length));
        }
        return first;
    }

    /// <summary>The word's length when <paramref name="folded"/> has it at <paramref name="at"/> (a word start), else 0.</summary>
    private static int WordAt(string folded, int at, string word) =>
        at + word.Length <= folded.Length && string.CompareOrdinal(folded, at, word, 0, word.Length) == 0 ? word.Length : 0;

    /// <summary>The matched length when the phrase's words follow each other whole from <paramref name="at"/> (a word start), else 0.</summary>
    private static int PhraseAt(string folded, int at, List<string> words)
    {
        int i = at;
        for (int w = 0; w < words.Count; w++)
        {
            if (w > 0)
            {
                int gap = i;
                while (i < folded.Length && folded[i] == ' ')
                    i++;
                if (i == gap)
                    return 0;
            }
            string word = words[w];
            if (i + word.Length > folded.Length || string.CompareOrdinal(folded, i, word, 0, word.Length) != 0)
                return 0;
            i += word.Length;
            if (i < folded.Length && folded[i] != ' ')
                return 0;
        }
        return i - at;
    }

    /// <summary>The folded places mapped back to <paramref name="shown"/> (a mark keeps the combining marks inside it), sorted, overlapping ones merged.</summary>
    private static List<Mark> ToMarks(List<(int start, int length)> found, List<int> map, string shown)
    {
        var spans = new List<(int start, int end)>(found.Count);
        foreach ((int start, int length) f in found)
        {
            int from = map[f.start];
            int to = f.start + f.length < map.Count ? map[f.start + f.length] : shown.Length;
            while (to > from && char.IsWhiteSpace(shown[to - 1]))
                to--;
            spans.Add((from, to));
        }
        spans.Sort((a, b) => a.start.CompareTo(b.start));

        var marks = new List<Mark>(spans.Count);
        int s = -1, e = -1;
        foreach ((int start, int end) span in spans)
        {
            if (s >= 0 && span.start <= e)
            {
                e = Math.Max(e, span.end);
                continue;
            }
            if (s >= 0)
                marks.Add(new Mark(s, e - s));
            s = span.start;
            e = span.end;
        }
        if (s >= 0)
            marks.Add(new Mark(s, e - s));
        return marks;
    }
}

/// <summary>An entry's texts folded once (IndexEntry.Folded): what the matcher reads.</summary>
internal sealed class FoldedEntry
{
    /// <summary>The folded title (ranked only).</summary>
    public string Title;

    /// <summary>The folded label.</summary>
    public string Label;

    /// <summary>The folded text.</summary>
    public string Text;

    /// <summary>The folded text's map back to the text as shown.</summary>
    public List<int> TextMap;

    /// <summary>The text's folded words joined by single spaces (the whole-value rank).</summary>
    public string TextWords;

    /// <summary>The canonical text's folded words joined by single spaces (a chip's match; untranslated lines only).</summary>
    public string Canonical;
}
