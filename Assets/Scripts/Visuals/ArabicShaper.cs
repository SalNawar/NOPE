using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// Arabic for TextMeshPro, which neither shapes nor reorders right-to-left
/// text (piece 6 U9, R6): contextual shaping into Presentation Forms-B (with
/// the four lam-alef ligatures) and a simplified bidi reorder into visual
/// order, rendered left to right. Spaces inside a right-to-left run become
/// no-break spaces, so TMP never breaks a line inside an Arabic phrase.
/// Numbers and Latin inside keep their order. The reference model is
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

    /// <summary>
    /// Logical Arabic text in visual order for left-to-right rendering: shaped
    /// letters, right-to-left runs reversed (brackets mirrored, spaces made
    /// no-break), runs in reverse order; digits and Latin keep their order.
    /// Text with no Arabic letter comes back unchanged; null gives "".
    /// </summary>
    public static string ToVisual(string logical)
    {
        if (string.IsNullOrEmpty(logical))
            return string.Empty;
        if (!logical.Any(Forms.ContainsKey))
            return logical;

        List<char> shaped = Shape(logical);
        var dir = new char[shaped.Count];
        for (int i = 0; i < shaped.Count; i++)
            dir[i] = Class(shaped[i]);

        // % after a digit, + - before a digit, and . , : between two digits join the digit run.
        for (int i = 0; i < shaped.Count; i++)
        {
            if (dir[i] != 'N')
                continue;
            char c = shaped[i];
            bool left = i > 0 && dir[i - 1] == 'L' && char.IsDigit(shaped[i - 1]);
            bool right = i + 1 < shaped.Count && dir[i + 1] == 'L' && char.IsDigit(shaped[i + 1]);
            if ((".,:".IndexOf(c) >= 0 && left && right) || (c == '%' && left) || ("+-".IndexOf(c) >= 0 && right))
                dir[i] = 'L';
        }

        // A neutral between two left-to-right runs is L, otherwise R (a right-to-left paragraph).
        var resolved = (char[])dir.Clone();
        for (int i = 0; i < dir.Length; i++)
        {
            if (dir[i] != 'N')
                continue;
            char l = Neighbour(dir, i, -1), r = Neighbour(dir, i, +1);
            resolved[i] = l == 'L' && r == 'L' ? 'L' : 'R';
        }

        var runs = new List<(char dir, List<char> chars)>();
        for (int i = 0; i < shaped.Count; i++)
        {
            if (runs.Count > 0 && runs[runs.Count - 1].dir == resolved[i])
                runs[runs.Count - 1].chars.Add(shaped[i]);
            else
                runs.Add((resolved[i], new List<char> { shaped[i] }));
        }

        var sb = new StringBuilder(shaped.Count);
        for (int r = runs.Count - 1; r >= 0; r--)
        {
            List<char> chars = runs[r].chars;
            if (runs[r].dir == 'R')
            {
                for (int i = chars.Count - 1; i >= 0; i--)
                    sb.Append(chars[i] == ' ' ? ' ' : Mirror.TryGetValue(chars[i], out char m) ? m : chars[i]);
            }
            else
            {
                foreach (char c in chars)
                    sb.Append(c);
            }
        }
        return sb.ToString();
    }

    /// <summary>False when the text holds a character of the Arabic block (U+0600–U+06FF) the tables do not cover; <paramref name="unsupported"/> lists them.</summary>
    public static bool CanShape(string text, out string unsupported)
    {
        string bad = new string((text ?? string.Empty).Where(c => c >= '؀' && c <= 'ۿ' && !Forms.ContainsKey(c)).Distinct().ToArray());
        unsupported = string.Join(", ", bad.Select(c => $"U+{(int)c:X4}"));
        return bad.Length == 0;
    }

    /// <summary>Contextual shaping in logical order (lam-alef ligatures included).</summary>
    private static List<char> Shape(string s)
    {
        var output = new List<char>(s.Length);
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
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
