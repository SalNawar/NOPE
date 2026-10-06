using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

/// <summary>One entry of a culture language's lens glossary (world_source.json ui.languages[].words): a word as the culture's labels write it and its English.</summary>
[Serializable]
public sealed class LensWord
{
    /// <summary>The word in the culture's language, as written in its labels (logical order; matched ignoring case and accents).</summary>
    public string native;

    /// <summary>Its English, as it reads inside the label's English ("Briefing"; upper-cased when the label's English is all capitals).</summary>
    public string english;
}

/// <summary>One stretch of a phrase in logical order: a word (with its English) or what lies between words (spaces, digits, punctuation: never translated).</summary>
public readonly struct LensSegment
{
    /// <summary>The stretch from <paramref name="start"/>, <paramref name="length"/> characters long; <paramref name="isWord"/> with its English (null when the glossary has none).</summary>
    public LensSegment(int start, int length, bool isWord, string english)
    {
        Start = start;
        Length = length;
        IsWord = isWord;
        English = english;
    }

    /// <summary>Its first character's index in the phrase's logical text.</summary>
    public int Start { get; }

    /// <summary>Its length in characters.</summary>
    public int Length { get; }

    /// <summary>True for a word (letters); false for the rest.</summary>
    public bool IsWord { get; }

    /// <summary>A word's English, or null (no English known: the word never flips; Generate World refuses such a label).</summary>
    public string English { get; }
}

/// <summary>
/// A culture label as the office shows it (the Translation Lens, Saleh
/// 2026-10-06): its text in logical order with its placeholders filled, its
/// English, its direction, the text as drawn (shaped and reordered when right
/// to left) and its segments (LensWords.Split). Built once per distinct label
/// text by UiStrings, never per frame.
/// </summary>
public sealed class LensPhrase
{
    private readonly List<int> _words = new List<int>();

    /// <summary>The phrase <paramref name="logical"/> whose English is <paramref name="english"/>, its words' English from <paramref name="glossary"/> (folded native to English; LensWords.Glossary).</summary>
    public LensPhrase(string logical, string english, bool rightToLeft, IReadOnlyDictionary<string, string> glossary)
    {
        Logical = logical ?? string.Empty;
        English = english ?? string.Empty;
        RightToLeft = rightToLeft;
        Visual = rightToLeft ? ArabicShaper.ToVisual(Logical) : Logical;
        Segments = LensWords.Split(Logical, English, glossary);
        for (int i = 0; i < Segments.Count; i++)
            if (Segments[i].IsWord)
                _words.Add(i);
    }

    /// <summary>The label in logical order (placeholders filled).</summary>
    public string Logical { get; }

    /// <summary>The label's English (placeholders filled).</summary>
    public string English { get; }

    /// <summary>True for a right-to-left label (shaped by ArabicShaper when drawn).</summary>
    public bool RightToLeft { get; }

    /// <summary>The label as drawn with no word translated.</summary>
    public string Visual { get; }

    /// <summary>Its segments in logical order.</summary>
    public IReadOnlyList<LensSegment> Segments { get; }

    /// <summary>How many words it has.</summary>
    public int WordCount => _words.Count;

    /// <summary>The segment index of word <paramref name="rank"/> (0 first, in logical order).</summary>
    public int WordSegment(int rank) => _words[rank];

    /// <summary>The word rank of segment <paramref name="segment"/>, or -1 when it is not a word.</summary>
    public int WordRank(int segment) => _words.IndexOf(segment);

    /// <summary>The native text of word <paramref name="rank"/>.</summary>
    public string NativeWord(int rank)
    {
        LensSegment s = Segments[_words[rank]];
        return Logical.Substring(s.Start, s.Length);
    }

    /// <summary>The English of word <paramref name="rank"/> (null: unknown).</summary>
    public string EnglishWord(int rank) => Segments[_words[rank]].English;

    /// <summary>
    /// The phrase as drawn with some words shown otherwise:
    /// <paramref name="shown"/>[rank] replaces word rank's native text (null
    /// keeps it). Composed in logical order, then shaped and reordered when
    /// right to left. <paramref name="segmentOf"/> (cleared; null skips it)
    /// receives, per drawn character, the segment it belongs to.
    /// </summary>
    public string Compose(IReadOnlyList<string> shown, List<int> segmentOf)
    {
        var logical = new StringBuilder(Logical.Length + 16);
        var owner = new List<int>(Logical.Length + 16);
        int rank = 0;
        for (int s = 0; s < Segments.Count; s++)
        {
            LensSegment seg = Segments[s];
            string replacement = seg.IsWord && shown != null && rank < shown.Count ? shown[rank] : null;
            if (seg.IsWord)
                rank++;
            string text = replacement ?? Logical.Substring(seg.Start, seg.Length);
            logical.Append(text);
            for (int k = 0; k < text.Length; k++)
                owner.Add(s);
        }

        segmentOf?.Clear();
        string composed = logical.ToString();
        if (!RightToLeft)
        {
            segmentOf?.AddRange(owner);
            return composed;
        }

        var sources = new List<int>(composed.Length);
        string visual = ArabicShaper.ToVisual(composed, sources);
        if (segmentOf != null)
            foreach (int source in sources)
                segmentOf.Add(source >= 0 && source < owner.Count ? owner[source] : -1);
        return visual;
    }

    /// <summary>
    /// The phrase as drawn while it turns whole (levels 2 and 3):
    /// <paramref name="logical"/> (a frame of its flip into its English, in
    /// logical order) shaped and reordered when right to left, so an English
    /// sentence reads left to right. <paramref name="segmentOf"/> (cleared;
    /// null skips it) receives -1 per drawn character: no single word is
    /// under the pointer.
    /// </summary>
    public string ComposeWhole(string logical, List<int> segmentOf)
    {
        string visual = RightToLeft ? ArabicShaper.ToVisual(logical ?? string.Empty) : logical ?? string.Empty;
        segmentOf?.Clear();
        for (int i = 0; segmentOf != null && i < visual.Length; i++)
            segmentOf.Add(-1);
        return visual;
    }
}

/// <summary>
/// Splits a culture label into words for the Translation Lens: letters make
/// words, everything else (spaces, digits, punctuation, placeholders) lies
/// between them and never translates. A run of Chinese or Japanese (no spaces
/// between words) is cut at the glossary's words, longest first; letters no
/// glossary word starts with stay together as one word. A word's English is
/// its glossary entry (matched ignoring case and accents), upper-cased when
/// the label's English is all capitals; a label with a single word and
/// nothing else to read takes the label's English. Pure; tested
/// (LensTextTests).
/// </summary>
public static class LensWords
{
    /// <summary>The glossary by folded native word (<see cref="Fold"/>; the first entry of a word wins).</summary>
    public static Dictionary<string, string> Glossary(IReadOnlyList<LensWord> words)
    {
        var glossary = new Dictionary<string, string>(StringComparer.Ordinal);
        if (words == null)
            return glossary;
        foreach (LensWord w in words)
        {
            if (w == null || string.IsNullOrWhiteSpace(w.native) || w.english == null)
                continue;
            string key = Fold(w.native);
            if (!glossary.ContainsKey(key))
                glossary.Add(key, w.english);
        }
        return glossary;
    }

    /// <summary>The segments of <paramref name="logical"/>, whose English is <paramref name="english"/>, with each word's English from <paramref name="glossary"/>.</summary>
    public static List<LensSegment> Split(string logical, string english, IReadOnlyDictionary<string, string> glossary)
    {
        var segments = new List<LensSegment>();
        string text = logical ?? string.Empty;
        int i = 0;
        while (i < text.Length)
        {
            int start = i;
            if (!IsLetter(text, i))
            {
                while (i < text.Length && !IsLetter(text, i))
                    i++;
                segments.Add(new LensSegment(start, i - start, false, null));
                continue;
            }

            while (i < text.Length && IsLetter(text, i))
                i++;
            if (IsUnspaced(text, start, i))
                SplitUnspaced(text, start, i, glossary, segments);
            else
                segments.Add(new LensSegment(start, i - start, true, null));
        }

        bool capitals = IsAllCapitals(english);
        int wordCount = 0, readable = 0;
        foreach (LensSegment s in segments)
        {
            if (s.IsWord)
                wordCount++;
            else if (HasLetterOrDigit(text, s.Start, s.Length))
                readable++;
        }

        for (int k = 0; k < segments.Count; k++)
        {
            LensSegment s = segments[k];
            if (!s.IsWord)
                continue;
            string found = Lookup(glossary, text.Substring(s.Start, s.Length));
            if (found == null && wordCount == 1 && readable == 0 && !string.IsNullOrEmpty(english))
                found = english.Trim();
            else if (found != null && capitals)
                found = found.ToUpperInvariant();
            segments[k] = new LensSegment(s.Start, s.Length, true, found);
        }
        return segments;
    }

    /// <summary>
    /// The lens's problems of a culture table (Generate World and the
    /// validator): a glossary entry with a blank word or English, a word
    /// listed twice (ignoring case and accents), and each label whose words
    /// the glossary leaves without English (so level 1 could not read them),
    /// naming the words.
    /// </summary>
    public static List<string> TableProblems(IReadOnlyList<UiStringEntry> reading, IReadOnlyList<UiStringEntry> culture, IReadOnlyList<LensWord> words)
    {
        var problems = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (LensWord w in words ?? Array.Empty<LensWord>())
        {
            if (w == null || string.IsNullOrWhiteSpace(w.native) || string.IsNullOrWhiteSpace(w.english))
                problems.Add($"words: an entry ('{w?.native}') has a blank word or English.");
            else if (!seen.Add(Fold(w.native)))
                problems.Add($"words: '{w.native}' is listed twice.");
        }

        Dictionary<string, string> glossary = Glossary(words);
        var englishByKey = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (UiStringEntry e in reading ?? Array.Empty<UiStringEntry>())
            if (e != null && !string.IsNullOrEmpty(e.key) && !englishByKey.ContainsKey(e.key))
                englishByKey.Add(e.key, e.text);
        foreach (UiStringEntry e in culture ?? Array.Empty<UiStringEntry>())
        {
            if (e == null || string.IsNullOrEmpty(e.text))
                continue;
            englishByKey.TryGetValue(e.key ?? string.Empty, out string english);
            List<string> missing = Missing(e.text, english, glossary);
            if (missing.Count > 0)
                problems.Add($"key '{e.key}': the Translation Lens has no English for {string.Join(", ", missing.ConvertAll(m => "'" + m + "'"))}; add them to the language's words.");
        }
        return problems;
    }

    /// <summary>The words of <paramref name="logical"/> with no English (Generate World's check of every culture label against its glossary).</summary>
    public static List<string> Missing(string logical, string english, IReadOnlyDictionary<string, string> glossary)
    {
        var missing = new List<string>();
        foreach (LensSegment s in Split(logical, english, glossary))
            if (s.IsWord && s.English == null)
                missing.Add(logical.Substring(s.Start, s.Length));
        return missing;
    }

    /// <summary>A word as the glossary matches it: lower case (invariant), accents and other combining marks removed, a Greek final sigma as a sigma.</summary>
    public static string Fold(string word)
    {
        if (string.IsNullOrEmpty(word))
            return string.Empty;
        string decomposed = word.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (char c in decomposed)
        {
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark && category != UnicodeCategory.SpacingCombiningMark && category != UnicodeCategory.EnclosingMark)
                sb.Append(c == 'ς' ? 'σ' : char.ToLowerInvariant(c)); // Greek final sigma as sigma: capitals have no final form
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>True for a character of a word: a letter or a combining mark, or an apostrophe between two letters.</summary>
    public static bool IsLetter(string text, int i)
    {
        char c = text[i];
        if (char.IsLetter(c))
            return true;
        UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);
        if (category == UnicodeCategory.NonSpacingMark || category == UnicodeCategory.SpacingCombiningMark)
            return i > 0 && char.IsLetter(text[i - 1]);
        return (c == '\'' || c == '’') && i > 0 && i + 1 < text.Length && char.IsLetter(text[i - 1]) && char.IsLetter(text[i + 1]);
    }

    /// <summary>True for a Chinese or Japanese character (Han, kana): a script written without spaces between words.</summary>
    public static bool IsUnspacedChar(char c) =>
        (c >= '぀' && c <= 'ヿ') || (c >= 'ㇰ' && c <= 'ㇿ') || (c >= '㐀' && c <= '䶿') || (c >= '一' && c <= '鿿') ||
        (c >= '豈' && c <= '﫿');

    /// <summary>True when the run [start, end) holds a character of an unspaced script.</summary>
    private static bool IsUnspaced(string text, int start, int end)
    {
        for (int i = start; i < end; i++)
            if (IsUnspacedChar(text[i]))
                return true;
        return false;
    }

    /// <summary>Cuts an unspaced run at the glossary's words (the longest that starts at each place); letters no glossary word starts with gather into one word.</summary>
    private static void SplitUnspaced(string text, int start, int end, IReadOnlyDictionary<string, string> glossary, List<LensSegment> segments)
    {
        int unknown = -1;
        int i = start;
        while (i < end)
        {
            int length = LongestMatch(text, i, end, glossary);
            if (length == 0)
            {
                if (unknown < 0)
                    unknown = i;
                i++;
                continue;
            }
            if (unknown >= 0)
            {
                segments.Add(new LensSegment(unknown, i - unknown, true, null));
                unknown = -1;
            }
            segments.Add(new LensSegment(i, length, true, null));
            i += length;
        }
        if (unknown >= 0)
            segments.Add(new LensSegment(unknown, end - unknown, true, null));
    }

    /// <summary>The length of the longest glossary word at <paramref name="at"/> inside [at, end), or 0.</summary>
    private static int LongestMatch(string text, int at, int end, IReadOnlyDictionary<string, string> glossary)
    {
        if (glossary == null || glossary.Count == 0)
            return 0;
        for (int length = end - at; length > 0; length--)
            if (glossary.ContainsKey(Fold(text.Substring(at, length))))
                return length;
        return 0;
    }

    /// <summary>The glossary's English for a word, or null.</summary>
    private static string Lookup(IReadOnlyDictionary<string, string> glossary, string word) =>
        glossary != null && glossary.TryGetValue(Fold(word), out string english) ? english : null;

    /// <summary>True when the text has letters and none of them is lower case.</summary>
    private static bool IsAllCapitals(string text)
    {
        bool letters = false;
        foreach (char c in text ?? string.Empty)
        {
            if (!char.IsLetter(c))
                continue;
            letters = true;
            if (char.IsLower(c))
                return false;
        }
        return letters;
    }

    /// <summary>True when the stretch holds a letter or a digit.</summary>
    private static bool HasLetterOrDigit(string text, int start, int length)
    {
        for (int i = start; i < start + length; i++)
            if (char.IsLetterOrDigit(text[i]))
                return true;
        return false;
    }
}

/// <summary>
/// The lens's letter flip (Saleh 2026-10-06: "when a word is being translated
/// the letters flip back to English"): a word turns from one text into
/// another cell by cell in reading order, cell k on FlipSequence's clock
/// (k × letterInterval after startDelay, landing letterSeconds later),
/// passing through scramble letters of the word it leaves; the two texts may
/// differ in length (a cell past a text's end shows nothing). Pure; tested
/// (LensTextTests).
/// </summary>
public static class LensFlip
{
    /// <summary>How many cells a flip between <paramref name="from"/> and <paramref name="to"/> has: the longer text's length.</summary>
    public static int Cells(string from, string to) => Math.Max(from?.Length ?? 0, to?.Length ?? 0);

    /// <summary>Seconds from the flip's start until its last cell lands.</summary>
    public static float Duration(string from, string to, FlipTiming timing) => FlipSequence.Duration(Cells(from, to), timing);

    /// <summary>
    /// The word <paramref name="elapsed"/> seconds into its flip from
    /// <paramref name="from"/> to <paramref name="to"/>, appended to
    /// <paramref name="into"/>: each cell shows its letter of
    /// <paramref name="from"/> until it starts, a scramble letter of
    /// <paramref name="from"/> while it flips, then its letter of
    /// <paramref name="to"/>. An elapsed time that is NaN or negative shows
    /// <paramref name="from"/>; infinity shows <paramref name="to"/> (reduced
    /// motion).
    /// </summary>
    public static void Frame(string from, string to, FlipTiming timing, float elapsed, StringBuilder into)
    {
        from = from ?? string.Empty;
        to = to ?? string.Empty;
        int cells = Cells(from, to);
        for (int k = 0; k < cells; k++)
        {
            CellState state = FlipSequence.StateAt(k, timing, elapsed, out int step);
            if (state == CellState.Foreign)
            {
                if (k < from.Length)
                    into.Append(from[k]);
            }
            else if (state == CellState.English)
            {
                if (k < to.Length)
                    into.Append(to[k]);
            }
            else
            {
                into.Append(Scramble(from, to, k, step));
            }
        }
    }

    /// <summary>The word as one string at <paramref name="elapsed"/> (Frame into a new builder).</summary>
    public static string Frame(string from, string to, FlipTiming timing, float elapsed)
    {
        var sb = new StringBuilder(Cells(from, to));
        Frame(from, to, timing, elapsed, sb);
        return sb.ToString();
    }

    /// <summary>A flipping cell's letter: a letter of the word it leaves (stepping by 7 each step), else of the word it reaches; never a space.</summary>
    private static char Scramble(string from, string to, int cell, int step)
    {
        string pool = HasLetter(from) ? from : to;
        if (pool.Length == 0)
            return ' ';
        for (int n = 0; n < pool.Length; n++)
        {
            char c = pool[((cell + 7 * (step + 1) + n) % pool.Length + pool.Length) % pool.Length];
            if (char.IsLetter(c))
                return c;
        }
        return pool[0];
    }

    /// <summary>True when the text has a letter.</summary>
    private static bool HasLetter(string text)
    {
        foreach (char c in text)
            if (char.IsLetter(c))
                return true;
        return false;
    }
}
