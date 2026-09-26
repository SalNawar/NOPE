using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// Arabic for TextMeshPro, which neither shapes nor reorders right-to-left
/// text (piece 6 U9, R6): contextual shaping into Presentation Forms-B (with
/// the four lam-alef ligatures) and a simplified bidi reorder into visual
/// order, rendered left to right. Spaces inside a right-to-left run become
/// no-break spaces, so TMP never breaks a line inside an Arabic phrase.
/// Numbers and Latin inside keep their order, and a bracket pair around Latin
/// that follows Latin reads with it (a simplified paired-bracket rule, audit
/// R2-022: a place label such as "Abbasid Baghdad (Medieval)" stays whole in
/// an Arabic line). The reference model is
/// docs/superpowers/drafts/piece6-support/shaper.py.
/// </summary>
public static class ArabicShaper
{
    /// <summary>Isolated, final, initial and medial forms per letter (0 = no such form: a right-joining letter).</summary>
    private static readonly Dictionary<char, (char iso, char fin, char ini, char med)> Forms = new Dictionary<char, (char, char, char, char)>
    {
        { 'ء', ('ﺀ', '\0', '\0', '\0') },
        { 'آ', ('ﺁ', 'ﺂ', '\0', '\0') }, { 'أ', ('ﺃ', 'ﺄ', '\0', '\0') }, { 'ؤ', ('ﺅ', 'ﺆ', '\0', '\0') },
        { 'إ', ('ﺇ', 'ﺈ', '\0', '\0') }, { 'ئ', ('ﺉ', 'ﺊ', 'ﺋ', 'ﺌ') }, { 'ا', ('ﺍ', 'ﺎ', '\0', '\0') },
        { 'ب', ('ﺏ', 'ﺐ', 'ﺑ', 'ﺒ') }, { 'ة', ('ﺓ', 'ﺔ', '\0', '\0') }, { 'ت', ('ﺕ', 'ﺖ', 'ﺗ', 'ﺘ') },
        { 'ث', ('ﺙ', 'ﺚ', 'ﺛ', 'ﺜ') }, { 'ج', ('ﺝ', 'ﺞ', 'ﺟ', 'ﺠ') }, { 'ح', ('ﺡ', 'ﺢ', 'ﺣ', 'ﺤ') },
        { 'خ', ('ﺥ', 'ﺦ', 'ﺧ', 'ﺨ') }, { 'د', ('ﺩ', 'ﺪ', '\0', '\0') }, { 'ذ', ('ﺫ', 'ﺬ', '\0', '\0') },
        { 'ر', ('ﺭ', 'ﺮ', '\0', '\0') }, { 'ز', ('ﺯ', 'ﺰ', '\0', '\0') }, { 'س', ('ﺱ', 'ﺲ', 'ﺳ', 'ﺴ') },
        { 'ش', ('ﺵ', 'ﺶ', 'ﺷ', 'ﺸ') }, { 'ص', ('ﺹ', 'ﺺ', 'ﺻ', 'ﺼ') }, { 'ض', ('ﺽ', 'ﺾ', 'ﺿ', 'ﻀ') },
        { 'ط', ('ﻁ', 'ﻂ', 'ﻃ', 'ﻄ') }, { 'ظ', ('ﻅ', 'ﻆ', 'ﻇ', 'ﻈ') }, { 'ع', ('ﻉ', 'ﻊ', 'ﻋ', 'ﻌ') },
        { 'غ', ('ﻍ', 'ﻎ', 'ﻏ', 'ﻐ') }, { 'ف', ('ﻑ', 'ﻒ', 'ﻓ', 'ﻔ') }, { 'ق', ('ﻕ', 'ﻖ', 'ﻗ', 'ﻘ') },
        { 'ك', ('ﻙ', 'ﻚ', 'ﻛ', 'ﻜ') }, { 'ل', ('ﻝ', 'ﻞ', 'ﻟ', 'ﻠ') }, { 'م', ('ﻡ', 'ﻢ', 'ﻣ', 'ﻤ') },
        { 'ن', ('ﻥ', 'ﻦ', 'ﻧ', 'ﻨ') }, { 'ه', ('ﻩ', 'ﻪ', 'ﻫ', 'ﻬ') }, { 'و', ('ﻭ', 'ﻮ', '\0', '\0') },
        { 'ى', ('ﻯ', 'ﻰ', '\0', '\0') }, { 'ي', ('ﻱ', 'ﻲ', 'ﻳ', 'ﻴ') },
    };

    /// <summary>Lam followed by these alefs becomes one ligature (isolated, final).</summary>
    private static readonly Dictionary<char, (char iso, char fin)> LamAlef = new Dictionary<char, (char, char)>
    {
        { 'آ', ('ﻵ', 'ﻶ') }, { 'أ', ('ﻷ', 'ﻸ') }, { 'إ', ('ﻹ', 'ﻺ') }, { 'ا', ('ﻻ', 'ﻼ') },
    };

    /// <summary>Brackets mirrored inside a right-to-left run.</summary>
    private static readonly Dictionary<char, char> Mirror = new Dictionary<char, char>
    {
        { '(', ')' }, { ')', '(' }, { '[', ']' }, { ']', '[' }, { '{', '}' }, { '}', '{' }, { '<', '>' }, { '>', '<' }, { '«', '»' }, { '»', '«' },
    };

    /// <summary>The Lam letter.</summary>
    private const char Lam = 'ل';

    /// <summary>Opening paired brackets; each closes with the character at the same place in <see cref="Closers"/>.</summary>
    private const string Openers = "([{";

    /// <summary>Closing paired brackets.</summary>
    private const string Closers = ")]}";

    /// <summary>
    /// Logical Arabic text in visual order for left-to-right rendering: shaped
    /// letters, right-to-left runs reversed (brackets mirrored, spaces made
    /// no-break), runs in reverse order; digits and Latin keep their order.
    /// Text with no Arabic letter comes back unchanged; null gives "".
    /// </summary>
    public static string ToVisual(string logical) => ToVisual(logical, null);

    /// <summary>
    /// ToVisual, also saying where each visual character came from (audit
    /// R2-001: a typed line shows its characters in reading order):
    /// <paramref name="sources"/> (cleared first; null skips it) receives, per
    /// character of the result, the index in <paramref name="logical"/> of the
    /// character it shows (a lam-alef ligature: its lam's).
    /// </summary>
    public static string ToVisual(string logical, List<int> sources)
    {
        sources?.Clear();
        if (string.IsNullOrEmpty(logical))
            return string.Empty;
        if (!HasArabicLetter(logical))
        {
            for (int i = 0; sources != null && i < logical.Length; i++)
                sources.Add(i);
            return logical;
        }

        var from = new List<int>(logical.Length);
        List<char> shaped = Shape(logical, from);
        return Reorder(shaped, from, Directions(shaped), sources);
    }

    /// <summary>
    /// The shaped characters in visual order: right-to-left runs reversed
    /// (brackets mirrored, spaces made no-break), runs in reverse order;
    /// <paramref name="sources"/> (null skips it) receives each visual
    /// character's logical source (<paramref name="from"/>).
    /// </summary>
    private static string Reorder(List<char> shaped, List<int> from, char[] resolved, List<int> sources)
    {
        // Runs (start..end of one direction) are walked from the last to the first, so every visual character keeps its source.
        var sb = new StringBuilder(shaped.Count);
        for (int end = shaped.Count - 1; end >= 0;)
        {
            int start = end;
            while (start > 0 && resolved[start - 1] == resolved[end])
                start--;
            bool rtl = resolved[end] == 'R';
            for (int k = 0; k <= end - start; k++)
            {
                int i = rtl ? end - k : start + k;
                char c = shaped[i];
                sb.Append(!rtl ? c : c == ' ' ? ' ' : Mirror.TryGetValue(c, out char m) ? m : c);
                sources?.Add(from[i]);
            }
            end = start - 1;
        }
        return sb.ToString();
    }

    /// <summary>
    /// Each shaped character's direction for the reorder: R for Arabic, L for
    /// Latin and digits (and a % after a digit, a + or - before one, a . , or :
    /// between two, which join the digit run); a bracket pair takes a
    /// direction by what it holds (PairBrackets); a neutral between two
    /// left-to-right runs is L, any other R (a right-to-left paragraph).
    /// </summary>
    private static char[] Directions(List<char> shaped)
    {
        var dir = new char[shaped.Count];
        for (int i = 0; i < shaped.Count; i++)
            dir[i] = Class(shaped[i]);

        for (int i = 0; i < shaped.Count; i++)
            if (dir[i] == 'N' && JoinsDigitRun(shaped, dir, i))
                dir[i] = 'L';

        PairBrackets(shaped, dir);

        var resolved = (char[])dir.Clone();
        for (int i = 0; i < dir.Length; i++)
        {
            if (dir[i] != 'N')
                continue;
            char l = Neighbour(dir, i, -1), r = Neighbour(dir, i, +1);
            resolved[i] = l == 'L' && r == 'L' ? 'L' : 'R';
        }
        return resolved;
    }

    /// <summary>
    /// A simplified paired-bracket rule (Unicode's N0 for a right-to-left
    /// paragraph): each closing bracket pairs with the nearest open bracket of
    /// its kind (brackets opened after that one are dropped). A pair holding
    /// Arabic reads right to left; a pair holding only Latin or digits reads
    /// left to right when the nearest strong character before it is Latin or a
    /// digit, else right to left; a pair holding neither stays neutral. Pairs
    /// are settled in the order they close, each seeing the ones settled before.
    /// </summary>
    private static void PairBrackets(List<char> shaped, char[] dir)
    {
        List<int> open = null;
        for (int i = 0; i < shaped.Count; i++)
        {
            if (dir[i] != 'N')
                continue;
            if (Openers.IndexOf(shaped[i]) >= 0)
            {
                (open ??= new List<int>()).Add(i);
                continue;
            }

            int kind = Closers.IndexOf(shaped[i]);
            for (int k = (open?.Count ?? 0) - 1; kind >= 0 && k >= 0; k--)
            {
                int start = open[k];
                if (Openers.IndexOf(shaped[start]) != kind)
                    continue;
                open.RemoveRange(k, open.Count - k);
                char inside = Holds(dir, start, i);
                if (inside != 'N')
                    dir[start] = dir[i] = inside == 'L' && Neighbour(dir, start, -1) == 'L' ? 'L' : 'R';
                break;
            }
        }
    }

    /// <summary>What a bracket pair holds between <paramref name="start"/> and <paramref name="end"/>: R when any Arabic, else L when any Latin or digit, else N.</summary>
    private static char Holds(char[] dir, int start, int end)
    {
        char held = 'N';
        for (int j = start + 1; j < end; j++)
        {
            if (dir[j] == 'R')
                return 'R';
            if (dir[j] == 'L')
                held = 'L';
        }
        return held;
    }

    /// <summary>A neutral that joins a digit run: % after a digit, + or - before one, and . , or : between two.</summary>
    private static bool JoinsDigitRun(List<char> shaped, char[] dir, int i)
    {
        char c = shaped[i];
        bool left = i > 0 && dir[i - 1] == 'L' && char.IsDigit(shaped[i - 1]);
        bool right = i + 1 < shaped.Count && dir[i + 1] == 'L' && char.IsDigit(shaped[i + 1]);
        return (".,:".IndexOf(c) >= 0 && left && right) || (c == '%' && left) || ("+-".IndexOf(c) >= 0 && right);
    }

    /// <summary>False when the text holds a character of the Arabic block (U+0600–U+06FF) the tables do not cover; <paramref name="unsupported"/> lists them.</summary>
    public static bool CanShape(string text, out string unsupported)
    {
        string bad = new string((text ?? string.Empty).Where(c => c >= '؀' && c <= 'ۿ' && !Forms.ContainsKey(c)).Distinct().ToArray());
        unsupported = string.Join(", ", bad.Select(c => $"U+{(int)c:X4}"));
        return bad.Length == 0;
    }

    /// <summary>Contextual shaping in logical order (lam-alef ligatures included); <paramref name="from"/> receives each shaped character's index in <paramref name="s"/> (a ligature's: its lam's).</summary>
    private static List<char> Shape(string s, List<int> from)
    {
        var output = new List<char>(s.Length);
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            from.Add(i);
            if (!Forms.TryGetValue(c, out var f))
            {
                output.Add(c);
                continue;
            }

            // A letter joins the one before only when both join (a hamza never does: it has no final form).
            bool prevJoins = i > 0 && DualJoining(s[i - 1]) && Joins(c);
            if (c == Lam && i + 1 < s.Length && LamAlef.TryGetValue(s[i + 1], out var ligature))
            {
                output.Add(prevJoins ? ligature.fin : ligature.iso);
                i++;
                continue;
            }

            bool nextJoins = DualJoining(c) && i + 1 < s.Length && Joins(s[i + 1]);
            if (prevJoins && nextJoins)
                output.Add(f.med);
            else if (prevJoins)
                output.Add(f.fin);
            else if (nextJoins)
                output.Add(f.ini);
            else
                output.Add(f.iso);
        }
        return output;
    }

    /// <summary>True when the text holds a letter the tables shape (a plain loop: ToVisual runs on each recompose of a right-to-left text, audit R2-019).</summary>
    private static bool HasArabicLetter(string text)
    {
        foreach (char c in text)
            if (Forms.ContainsKey(c))
                return true;
        return false;
    }

    /// <summary>A letter that connects to the next one.</summary>
    private static bool DualJoining(char c) => Forms.TryGetValue(c, out var f) && f.ini != '\0';

    /// <summary>A letter the previous one can connect to (every letter but hamza).</summary>
    private static bool Joins(char c) => Forms.ContainsKey(c) && c != 'ء';

    /// <summary>Direction class: R for Arabic, L for ASCII letters and digits, N for the rest.</summary>
    private static char Class(char c)
    {
        if ((c >= '؀' && c <= 'ۿ') || (c >= 'ﭐ' && c <= '﷿') || (c >= 'ﹰ' && c <= '﻿'))
            return 'R';
        if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
            return 'L';
        return 'N';
    }

    /// <summary>The nearest strong class in a direction (0 when none).</summary>
    private static char Neighbour(char[] dir, int i, int step)
    {
        for (int j = i + step; j >= 0 && j < dir.Length; j += step)
            if (dir[j] != 'N')
                return dir[j];
        return '\0';
    }
}
