using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// How a printed text fits its box without breaking a word (BUG-9, DOC-LOOK):
/// it wraps before it shrinks; a text that still has to shrink below
/// <see cref="Floor"/> of its size counts as shrunk (the cap-height gate
/// reports it, DocLookChecks), and it never shrinks below the absolute size
/// the old fit allowed (<see cref="Least"/>, <see cref="WordLeast"/> of the
/// art's own size: a grown text has more room to shrink, never less), so no
/// generated value ever overflows its place (the validator checks the
/// longest at that least size); a word still wider than its box there is cut
/// and marked with a full stop ("KONSTANTINOP.") rather than broken across
/// two lines. The words are measured by the caller (a text box's preferred
/// width, in the box's units, at the full size), so the rule is pure and
/// tested headless.
/// </summary>
public static class TextFit
{
    /// <summary>The legibility floor: a text fitted below this share of its size is a shrunk text (BUG-9: "the fit shrinks the most important values"; FIX_PLAN DOC-LOOK raises the floor from 0.5 to 0.85).</summary>
    public const float Floor = 0.85f;

    /// <summary>The least share of the art's own size a value or a label on a document's art shrinks to so that it fits its place (the old fit's floor, kept so the longest generated values still fit: the passport's 43-character origins need it).</summary>
    public const float Least = 0.5f;

    /// <summary>The least share of its size a text shrinks to so that its widest word fits its box (a form off the art; wave 5 A3).</summary>
    public const float WordLeast = 0.6f;

    /// <summary>The share of the width a fitted word may take (a hair of room for the box's rounding).</summary>
    public const float WidthSlack = 0.98f;

    /// <summary>Where a printed text's words part.</summary>
    private static readonly char[] WordBreaks = { ' ', '\n' };

    /// <summary>The text's words (split at spaces and line breaks, empties dropped).</summary>
    public static string[] Words(string text) => (text ?? string.Empty).Split(WordBreaks, StringSplitOptions.RemoveEmptyEntries);

    /// <summary>
    /// The share of its size a text whose widest word is
    /// <paramref name="widestWord"/> wide at full size prints at in a box
    /// <paramref name="width"/> wide: 1 when the word fits, else just small
    /// enough that it does, never under <see cref="WordLeast"/> (a word still
    /// too wide there is cut: <see cref="Unbroken"/>).
    /// </summary>
    public static float WordShare(float widestWord, float width)
    {
        if (width <= 0f || widestWord <= width)
            return 1f;
        return Math.Max(WordLeast, width / widestWord * WidthSlack);
    }

    /// <summary>The least share of a grown text's size it may shrink to: <paramref name="least"/> of the art's own size, so <paramref name="least"/> over the <paramref name="growth"/> (1 or less: as it is).</summary>
    public static float LeastOf(float least, float growth) => least / Math.Max(1f, growth);

    /// <summary>True when a text printed at <paramref name="share"/> of its size counts as shrunk (under <see cref="Floor"/>): the cap-height gate's shrunk text.</summary>
    public static bool Shrunk(float share) => share < Floor - 1e-4f;

    /// <summary>
    /// <paramref name="text"/> with every word that is wider than
    /// <paramref name="width"/> at <paramref name="share"/> of its size
    /// (<paramref name="measure"/>: a word's width at full size) cut to its
    /// longest start that fits with a full stop after it; every other word,
    /// space and line break kept, and a word holding a rich-text tag ('&lt;')
    /// never cut (the guide's sheets: a cut tag would print as text). So no
    /// word is ever broken across lines.
    /// </summary>
    public static string Unbroken(string text, float width, float share, Func<string, float> measure)
    {
        if (string.IsNullOrEmpty(text) || measure == null || width <= 0f || share <= 0f)
            return text ?? string.Empty;
        float room = width / share;
        var sb = new StringBuilder(text.Length);
        int at = 0;
        while (at < text.Length)
        {
            if (text[at] == ' ' || text[at] == '\n')
            {
                sb.Append(text[at++]);
                continue;
            }
            int end = at;
            while (end < text.Length && text[end] != ' ' && text[end] != '\n')
                end++;
            string word = text.Substring(at, end - at);
            sb.Append(word.IndexOf('<') >= 0 || measure(word) <= room ? word : Cut(word, room, measure));
            at = end;
        }
        return sb.ToString();
    }

    /// <summary>The longest start of <paramref name="word"/> that with a full stop after it fits <paramref name="room"/> (at least its first letter and the stop).</summary>
    public static string Cut(string word, float room, Func<string, float> measure)
    {
        for (int keep = word.Length - 1; keep > 1; keep--)
        {
            string cut = word.Substring(0, keep) + ".";
            if (measure(cut) <= room)
                return cut;
        }
        return word.Length > 0 ? word.Substring(0, 1) + "." : word;
    }

    /// <summary>
    /// <paramref name="text"/> broken into lines no wider than
    /// <paramref name="width"/> (<paramref name="measure"/>: a line's width),
    /// words whole, each paragraph (split at a line break) on a line of its
    /// own: the logical lines a right-to-left text goes into visual order by,
    /// one by one (ArabicShaper.ToVisualLines, BUG-37c). A word wider than the
    /// line has a line of its own (Unbroken cuts it first).
    /// </summary>
    public static List<string> WrapWords(string text, float width, Func<string, float> measure)
    {
        var lines = new List<string>();
        foreach (string paragraph in (text ?? string.Empty).Split('\n'))
        {
            string line = null;
            foreach (string word in paragraph.Split(' '))
            {
                if (word.Length == 0)
                    continue;
                string candidate = line == null ? word : line + " " + word;
                if (line == null || measure == null || width <= 0f || measure(candidate) <= width)
                {
                    line = candidate;
                    continue;
                }
                lines.Add(line);
                line = word;
            }
            lines.Add(line ?? string.Empty);
        }
        return lines;
    }

    /// <summary>True when <paramref name="text"/> holds a letter of the Arabic block (U+0621 to U+064A) not yet shaped: a logical right-to-left text the per-line API lays out (a text already shaped holds presentation forms instead, and is printed as it is).</summary>
    public static bool IsLogicalArabic(string text)
    {
        foreach (char c in text ?? string.Empty)
            if (c >= 'ء' && c <= 'ي')
                return true;
        return false;
    }
}

/// <summary>
/// Text on a ruled page (the rulebook's RULES sheet, BUG-10c: its text sat
/// through the art's ruled lines): a wrapped text's lines step down whole
/// rules, and a row that grows grows by whole rules, so every line keeps to
/// a rule. Pure.
/// </summary>
public static class RuledLines
{
    /// <summary>
    /// TextMeshPro's extra line spacing (hundredths of an em) that makes a
    /// text of <paramref name="em"/> (metres) whose own line is
    /// <paramref name="lineHeightPerEm"/> ems tall step down
    /// <paramref name="pitch"/> (the rules' spacing) per line, or a whole
    /// number of pitches when its own line is taller than one; 0 without a
    /// pitch.
    /// </summary>
    public static float Spacing(float pitch, float em, float lineHeightPerEm)
    {
        if (pitch <= 0f || em <= 0f || lineHeightPerEm <= 0f)
            return 0f;
        float natural = em * lineHeightPerEm;
        float step = pitch * Math.Max(1f, (float)Math.Ceiling(natural / pitch - 0.01f));
        return (step - natural) / em * 100f;
    }

    /// <summary><paramref name="extra"/> (how much taller a row grew, metres) rounded up to whole <paramref name="pitch"/>es (a hair under a pitch counts as none); as it is without a pitch.</summary>
    public static float Snap(float extra, float pitch)
    {
        if (pitch <= 0f || extra <= 0f)
            return Math.Max(0f, extra);
        return pitch * (float)Math.Ceiling(extra / pitch - 0.05f);
    }
}
