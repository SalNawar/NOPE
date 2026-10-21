using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// Arabic for TextMeshPro, which neither shapes nor reorders right-to-left
/// text (piece 6 U9, R6; BUG-37a): contextual shaping into Presentation
/// Forms-B (with the four lam-alef ligatures) and a simplified bidi reorder
/// into visual order, rendered left to right. Numbers and Latin inside keep
/// their order, and a bracket pair around Latin that follows Latin reads
/// with it (a simplified paired-bracket rule, audit R2-022: a place label such
/// as "Abbasid Baghdad (Medieval)" stays whole in an Arabic line).
/// TMP's rich-text tags are tokens without a direction: they never reorder
/// or mirror, and each styled run is written with balanced tags in visual
/// order (CODE-43). A text is shaped exactly once: a text already in visual
/// order comes back unchanged (<see cref="IsVisual"/>), and
/// <see cref="ToLogical"/> turns a shaped argument back into logical text
/// before an outer template shapes it (CODE-44, UiStrings.Format). A text that
/// wraps is broken into lines on its logical text first, then each line goes
/// into visual order (<see cref="Wrap"/>, <see cref="ToVisualLines"/>:
/// CODE-42), keeping its plain spaces; the one-string <see cref="ToVisual"/>
/// keeps a right-to-left run's spaces no-break, so a box that wraps a visual
/// string never breaks a line inside an Arabic phrase. The reference model is
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

    /// <summary>Brackets mirrored inside a right-to-left run (a rich-text tag is a token, never a bracket: CODE-43).</summary>
    private static readonly Dictionary<char, char> Mirror = new Dictionary<char, char>
    {
        { '(', ')' }, { ')', '(' }, { '[', ']' }, { ']', '[' }, { '{', '}' }, { '}', '{' }, { '<', '>' }, { '>', '<' }, { '«', '»' }, { '»', '«' },
    };

    /// <summary>Each presentation form back to the logical letters it draws (a lam-alef ligature: lam, then its alef).</summary>
    private static readonly Dictionary<char, string> Unshaped = BuildUnshaped();

    /// <summary>The rich-text tags TextMeshPro reads (a "&lt;" that opens none of them is text).</summary>
    private static readonly HashSet<string> TagNames = new HashSet<string>(StringComparer.Ordinal)
    {
        "b", "i", "u", "s", "size", "color", "alpha", "mark", "noparse", "sup", "sub", "font", "material", "cspace", "mspace", "voffset",
        "lowercase", "uppercase", "smallcaps", "allcaps", "nobr", "link", "sprite", "style", "margin", "margin-left", "margin-right",
        "indent", "line-indent", "line-height", "align", "rotate", "width", "pos", "space", "br", "page", "gradient", "strikethrough", "underline",
    };

    /// <summary>Tags that stand alone (no closing tag): drawn where they lie, as an object between the letters.</summary>
    private static readonly HashSet<string> VoidTags = new HashSet<string>(StringComparer.Ordinal) { "br", "sprite", "space", "pos", "page" };

    /// <summary>The Lam letter.</summary>
    private const char Lam = 'ل';

    /// <summary>A standalone tag's place among the letters while they reorder (neutral, never joins).</summary>
    private const char ObjectChar = '￼';

    /// <summary>The no-break space ToVisual puts in a right-to-left run.</summary>
    private const char NoBreakSpace = ' ';

    /// <summary>Opening paired brackets; each closes with the character at the same place in <see cref="Closers"/>.</summary>
    private const string Openers = "([{";

    /// <summary>Closing paired brackets.</summary>
    private const string Closers = ")]}";

    /// <summary>
    /// Logical Arabic text in visual order for left-to-right rendering: shaped
    /// letters, right-to-left runs reversed (brackets mirrored, spaces made
    /// no-break), runs in reverse order; digits and Latin keep their order;
    /// tags stay balanced around the letters they style. Text with no Arabic
    /// letter, or already in visual order (<see cref="IsVisual"/>: shaped
    /// once), comes back unchanged; null gives "". Each line goes into visual
    /// order by itself, the lines keeping their order.
    /// </summary>
    public static string ToVisual(string logical) => ToVisual(logical, null);

    /// <summary>
    /// ToVisual, also saying where each visual character came from (audit
    /// R2-001: a typed line shows its characters in reading order):
    /// <paramref name="sources"/> (cleared first; null skips it) receives, per
    /// character of the result, the index in <paramref name="logical"/> of the
    /// character it shows (a lam-alef ligature: its lam's; a character of a
    /// tag written again around a styled run: -1).
    /// </summary>
    public static string ToVisual(string logical, List<int> sources)
    {
        sources?.Clear();
        if (string.IsNullOrEmpty(logical))
            return string.Empty;
        if (IsVisual(logical))
        {
            for (int i = 0; sources != null && i < logical.Length; i++)
                sources.Add(i);
            return logical;
        }
        if (logical.IndexOf('\n') < 0)
            return Line(logical, sources, true, true);

        // Each line into visual order by itself, the lines kept in their order (TMP draws a break where it lies).
        var sb = new StringBuilder(logical.Length);
        List<int> part = sources != null ? new List<int>() : null;
        for (int start = 0; ;)
        {
            int end = logical.IndexOf('\n', start);
            sb.Append(Line(logical.Substring(start, (end < 0 ? logical.Length : end) - start), part, true, true));
            if (sources != null)
                foreach (int i in part)
                    sources.Add(i < 0 ? -1 : start + i);
            if (end < 0)
                break;
            sb.Append('\n');
            sources?.Add(end);
            start = end + 1;
        }
        return sb.ToString();
    }

    /// <summary>
    /// The per-line right-to-left API (P0-A seam, owner TEXT-LANG; DOC-LOOK's
    /// DeskRulebook and TmpFormText and CONTENT call it, BUG-37b/37c): lines
    /// of logical text, already broken by the text box (each without a line
    /// break), into visual order one by one. A tag left open at a line's end
    /// is closed there and opened again on the next line, so every line is
    /// balanced; spaces stay plain spaces (the lines are laid out already); a
    /// line already in visual order comes back as it is.
    /// </summary>
    public static List<string> ToVisualLines(IReadOnlyList<string> lines)
    {
        var visual = new List<string>(lines != null ? lines.Count : 0);
        if (lines == null)
            return visual;
        foreach (string line in CarryTags(lines))
            visual.Add(string.IsNullOrEmpty(line) ? string.Empty : IsVisual(line) ? line : Line(line, null, false, true));
        return visual;
    }

    /// <summary>
    /// A logical text broken into lines no wider than <paramref name="width"/>
    /// on its logical text, then each line in visual order (CODE-42: the first
    /// line holds the first words read, and no word breaks unless it alone is
    /// wider than the line, when it breaks between letters, its first letters
    /// first). <paramref name="measure"/> gives a visual line's width (a text
    /// box's preferred width); with no measure or no width the paragraphs are
    /// not broken. Each paragraph (split at a line break) starts a new line.
    /// </summary>
    public static List<string> Wrap(string logical, float width, Func<string, float> measure)
    {
        var lines = new List<string>();
        foreach (string paragraph in (logical ?? string.Empty).Split('\n'))
        {
            string line = null;
            foreach (string word in paragraph.Split(' '))
            {
                string candidate = line == null ? word : line + " " + word;
                if (line == null || Fits(candidate, width, measure))
                {
                    line = candidate;
                }
                else
                {
                    lines.Add(line);
                    line = word;
                }
                while (!Fits(line, width, measure))
                {
                    int cut = LongestFit(line, width, measure);
                    if (cut <= 0 || cut >= line.Length)
                        break;
                    lines.Add(line.Substring(0, cut));
                    line = line.Substring(cut);
                }
            }
            lines.Add(line ?? string.Empty);
        }
        return ToVisualLines(lines);
    }

    /// <summary>
    /// True when the text is already in visual order: it holds a presentation
    /// form (a shaped letter) and no letter left to shape (the shape-once
    /// guard: ToVisual and ToVisualLines return such a text unchanged).
    /// </summary>
    public static bool IsVisual(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;
        bool shaped = false;
        foreach (char c in text)
        {
            if (Forms.ContainsKey(c))
                return false;
            shaped |= IsPresentationForm(c);
        }
        return shaped;
    }

    /// <summary>
    /// A visual text (ToVisual's) back in logical order with its letters
    /// unshaped: the inverse of ToVisual, line by line (a ligature gives its
    /// lam and alef, a no-break space a space). An argument a right-to-left
    /// template is filled with goes through it first, so the whole label is
    /// shaped once, at the outermost format (CODE-44). A text with no shaped
    /// letter comes back unchanged.
    /// </summary>
    public static string ToLogical(string visual)
    {
        if (string.IsNullOrEmpty(visual))
            return string.Empty;
        if (!visual.Any(IsPresentationForm))
            return visual;
        return string.Join("\n", visual.Split('\n').Select(line => Line(line, null, false, false)));
    }

    /// <summary>True when the text reads right to left: its first letter (tags and digits skipped: a digit is weak) is Arabic, so the box that shows it is right-aligned (an RTL body, BUG-37a).</summary>
    public static bool IsRightToLeft(string text)
    {
        foreach (char c in StripTags(text ?? string.Empty))
        {
            char d = Class(c);
            if (d != 'N' && !char.IsDigit(c))
                return d == 'R';
        }
        return false;
    }

    /// <summary>False when the text holds a character of the Arabic block (U+0600–U+06FF) the tables do not cover; <paramref name="unsupported"/> lists them.</summary>
    public static bool CanShape(string text, out string unsupported)
    {
        string bad = new string((text ?? string.Empty).Where(c => c >= '؀' && c <= 'ۿ' && !Forms.ContainsKey(c)).Distinct().ToArray());
        unsupported = string.Join(", ", bad.Select(c => $"U+{(int)c:X4}"));
        return bad.Length == 0;
    }

    /// <summary>
    /// One line (no line break) into visual order (<paramref name="shape"/>:
    /// shaped, from logical text) or back into logical order (not shaped: from
    /// visual text, its letters unshaped); <paramref name="noBreak"/> makes a
    /// right-to-left run's spaces no-break. <paramref name="sources"/>
    /// (cleared first; null skips it) as ToVisual's.
    /// </summary>
    private static string Line(string text, List<int> sources, bool noBreak, bool shape)
    {
        sources?.Clear();
        if (string.IsNullOrEmpty(text))
            return string.Empty;
        if (shape ? !HasArabicLetter(text) : !text.Any(IsPresentationForm))
        {
            for (int i = 0; sources != null && i < text.Length; i++)
                sources.Add(i);
            return text;
        }

        List<Item> items = Tokenize(text, shape);
        var from = new List<int>(items.Count);
        List<char> chars = shape ? Shape(items, from) : Plain(items, from);
        return Emit(items, chars, from, Directions(chars), sources, noBreak);
    }

    /// <summary>One character of a line: a letter (or a standalone tag, <see cref="ObjectChar"/>), where it came from, and the tags open around it.</summary>
    private readonly struct Item
    {
        /// <summary>An item.</summary>
        public Item(char c, int source, string[] tags, string objectText)
        {
            Char = c;
            Source = source;
            Tags = tags;
            ObjectText = objectText;
        }

        /// <summary>The character (ObjectChar for a standalone tag).</summary>
        public readonly char Char;

        /// <summary>Its index in the line.</summary>
        public readonly int Source;

        /// <summary>The opening tags around it, outermost first (one array shared by the items between two tag changes).</summary>
        public readonly string[] Tags;

        /// <summary>A standalone tag's text, else null.</summary>
        public readonly string ObjectText;
    }

    /// <summary>
    /// A line as characters with their tag stacks: each known tag is a token
    /// (a standalone one, and a closing tag with no opening one, an object
    /// among the characters), the text inside a noparse tag is text, and a
    /// visual line's presentation forms are given back as their logical
    /// letters (<paramref name="shape"/> false; a ligature's two letters in
    /// visual order, alef first, so the reorder reads them lam first).
    /// </summary>
    private static List<Item> Tokenize(string text, bool shape)
    {
        var items = new List<Item>(text.Length);
        var open = new List<string>();
        string[] stack = Array.Empty<string>();
        bool noParse = false;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '<' && TryTag(text, i, out int end, out string name, out bool closing) && (!noParse || (closing && name == "noparse")))
            {
                string tag = text.Substring(i, end - i + 1);
                if (closing)
                {
                    int at = LastIndexOfTag(open, name);
                    if (at < 0)
                    {
                        items.Add(new Item(ObjectChar, i, stack, tag));
                    }
                    else
                    {
                        open.RemoveAt(at);
                        stack = open.ToArray();
                        noParse &= name != "noparse";
                    }
                }
                else if (VoidTags.Contains(name))
                {
                    items.Add(new Item(ObjectChar, i, stack, tag));
                }
                else
                {
                    open.Add(tag);
                    stack = open.ToArray();
                    noParse |= name == "noparse";
                }
                i = end;
                continue;
            }

            if (!shape && Unshaped.TryGetValue(c, out string letters))
            {
                for (int k = letters.Length - 1; k >= 0; k--)
                    items.Add(new Item(letters[k], i, stack, null));
                continue;
            }
            items.Add(new Item(!shape && c == NoBreakSpace ? ' ' : c, i, stack, null));
        }
        return items;
    }

    /// <summary>
    /// The items' characters in visual order, written left to right: each
    /// character's tags opened and closed as the styled runs change (balanced,
    /// outermost first), right-to-left characters mirrored, and a
    /// right-to-left run's spaces no-break when <paramref name="noBreak"/>.
    /// </summary>
    private static string Emit(List<Item> items, List<char> chars, List<int> from, char[] resolved, List<int> sources, bool noBreak)
    {
        var sb = new StringBuilder(chars.Count + 16);
        string[] current = Array.Empty<string>();
        foreach (int i in Order(resolved))
        {
            Item item = items[from[i]];
            Restyle(sb, current, item.Tags, sources);
            current = item.Tags;
            if (item.ObjectText != null)
            {
                sb.Append(item.ObjectText);
                for (int k = 0; sources != null && k < item.ObjectText.Length; k++)
                    sources.Add(item.Source);
                continue;
            }

            char c = chars[i];
            bool rtl = resolved[i] == 'R';
            sb.Append(!rtl ? c : c == ' ' && noBreak ? NoBreakSpace : Mirror.TryGetValue(c, out char m) ? m : c);
            sources?.Add(item.Source);
        }
        Restyle(sb, current, Array.Empty<string>(), sources);
        return sb.ToString();
    }

    /// <summary>
    /// The visual order of the characters: runs of one direction walked from
    /// the last to the first, a right-to-left run backwards. The walk is its
    /// own inverse, so it also takes visual text back to logical order.
    /// </summary>
    private static List<int> Order(char[] resolved)
    {
        var order = new List<int>(resolved.Length);
        for (int end = resolved.Length - 1; end >= 0;)
        {
            int start = end;
            while (start > 0 && resolved[start - 1] == resolved[end])
                start--;
            bool rtl = resolved[end] == 'R';
            for (int k = 0; k <= end - start; k++)
                order.Add(rtl ? end - k : start + k);
            end = start - 1;
        }
        return order;
    }

    /// <summary>Closes the tags of <paramref name="from"/> that <paramref name="to"/> does not share (innermost first) and opens the rest of <paramref name="to"/>.</summary>
    private static void Restyle(StringBuilder sb, string[] from, string[] to, List<int> sources)
    {
        if (ReferenceEquals(from, to))
            return;
        int shared = 0;
        while (shared < from.Length && shared < to.Length && ReferenceEquals(from[shared], to[shared]))
            shared++;
        for (int k = from.Length - 1; k >= shared; k--)
            Append(sb, "</" + NameOf(from[k]) + ">", sources);
        for (int k = shared; k < to.Length; k++)
            Append(sb, to[k], sources);
    }

    /// <summary>Appends a tag's text; its characters come from no letter (-1).</summary>
    private static void Append(StringBuilder sb, string tag, List<int> sources)
    {
        sb.Append(tag);
        for (int k = 0; sources != null && k < tag.Length; k++)
            sources.Add(-1);
    }

    /// <summary>
    /// Lines with every tag balanced: a tag still open at a line's end is
    /// closed there and opened again at the next line's start (a box that
    /// breaks a styled phrase keeps its style on both lines).
    /// </summary>
    private static IEnumerable<string> CarryTags(IReadOnlyList<string> lines)
    {
        var open = new List<string>();
        foreach (string raw in lines)
        {
            string line = raw ?? string.Empty;
            string prefix = string.Concat(open);
            bool noParse = open.Any(t => NameOf(t) == "noparse");
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] != '<' || !TryTag(line, i, out int end, out string name, out bool closing) || (noParse && !(closing && name == "noparse")))
                    continue;
                if (closing)
                {
                    int at = LastIndexOfTag(open, name);
                    if (at >= 0)
                        open.RemoveAt(at);
                    noParse &= name != "noparse";
                }
                else if (!VoidTags.Contains(name))
                {
                    open.Add(line.Substring(i, end - i + 1));
                    noParse |= name == "noparse";
                }
                i = end;
            }
            var suffix = new StringBuilder();
            for (int k = open.Count - 1; k >= 0; k--)
                suffix.Append("</").Append(NameOf(open[k])).Append('>');
            yield return prefix + line + suffix;
        }
    }

    /// <summary>
    /// True when a known rich-text tag starts at <paramref name="start"/>: "&lt;",
    /// an optional "/", a tag name (or "#" for a colour), then anything up to a
    /// "&gt;" with no "&lt;" or line break before it. <paramref name="end"/> is
    /// the "&gt;"; <paramref name="name"/> the name in lower case ("color" for "#").
    /// </summary>
    private static bool TryTag(string text, int start, out int end, out string name, out bool closing)
    {
        end = -1;
        name = null;
        closing = start + 1 < text.Length && text[start + 1] == '/';
        int n = start + (closing ? 2 : 1);
        if (n >= text.Length)
            return false;
        if (text[n] == '#')
        {
            name = "color";
        }
        else
        {
            int k = n;
            while (k < text.Length && (char.IsLetter(text[k]) || text[k] == '-'))
                k++;
            if (k == n)
                return false;
            name = text.Substring(n, k - n).ToLowerInvariant();
            if (!TagNames.Contains(name) || (k < text.Length && text[k] != '>' && text[k] != '=' && text[k] != ' '))
                return false;
        }
        for (int k = n; k < text.Length; k++)
        {
            if (text[k] == '<' || text[k] == '\n')
                return false;
            if (text[k] == '>')
            {
                end = k;
                return true;
            }
        }
        return false;
    }

    /// <summary>The index of the last open tag named <paramref name="name"/>, or -1.</summary>
    private static int LastIndexOfTag(List<string> open, string name)
    {
        for (int k = open.Count - 1; k >= 0; k--)
            if (NameOf(open[k]) == name)
                return k;
        return -1;
    }

    /// <summary>An opening tag's name ("size" for "&lt;size=60%&gt;", "color" for "&lt;#fff&gt;").</summary>
    private static string NameOf(string tag) => TryTag(tag, 0, out _, out string name, out _) ? name : string.Empty;

    /// <summary>The text without its rich-text tags.</summary>
    private static string StripTags(string text)
    {
        if (text.IndexOf('<') < 0)
            return text;
        var sb = new StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '<' && TryTag(text, i, out int end, out _, out _))
            {
                i = end;
                continue;
            }
            sb.Append(text[i]);
        }
        return sb.ToString();
    }

    /// <summary>True when one line holds <paramref name="logical"/> (always, with no measure or no width).</summary>
    private static bool Fits(string logical, float width, Func<string, float> measure) =>
        measure == null || width <= 0f || measure(ToVisualLines(new[] { logical })[0]) <= width;

    /// <summary>The length of the longest start of <paramref name="logical"/> that fits (at least its first character; a tag is never cut).</summary>
    private static int LongestFit(string logical, float width, Func<string, float> measure)
    {
        int best = 0;
        for (int i = 0; i < logical.Length; i++)
        {
            if (logical[i] == '<' && TryTag(logical, i, out int end, out _, out _))
            {
                i = end;
                continue;
            }
            int cut = i + 1;
            if (best > 0 && !Fits(logical.Substring(0, cut), width, measure))
                break;
            best = cut;
        }
        return best;
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

    /// <summary>Contextual shaping of the items' characters in logical order (lam-alef ligatures included; a tag between two letters never breaks their join, a standalone tag does); <paramref name="from"/> receives each shaped character's item (a ligature's: its lam's).</summary>
    private static List<char> Shape(List<Item> items, List<int> from)
    {
        var output = new List<char>(items.Count);
        for (int i = 0; i < items.Count; i++)
        {
            char c = items[i].Char;
            from.Add(i);
            if (!Forms.TryGetValue(c, out var f))
            {
                output.Add(c);
                continue;
            }

            // A letter joins the one before only when both join (a hamza never does: it has no final form).
            bool prevJoins = i > 0 && DualJoining(items[i - 1].Char) && Joins(c);
            if (c == Lam && i + 1 < items.Count && LamAlef.TryGetValue(items[i + 1].Char, out var ligature))
            {
                output.Add(prevJoins ? ligature.fin : ligature.iso);
                i++;
                continue;
            }

            bool nextJoins = DualJoining(c) && i + 1 < items.Count && Joins(items[i + 1].Char);
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

    /// <summary>The items' characters as they are (a visual line going back to logical order); <paramref name="from"/> as Shape's.</summary>
    private static List<char> Plain(List<Item> items, List<int> from)
    {
        var output = new List<char>(items.Count);
        for (int i = 0; i < items.Count; i++)
        {
            from.Add(i);
            output.Add(items[i].Char);
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

    /// <summary>True for an Arabic presentation form (a shaped letter: U+FB50–U+FDFF, U+FE70–U+FEFF).</summary>
    private static bool IsPresentationForm(char c) => (c >= 'ﭐ' && c <= '﷿') || (c >= 'ﹰ' && c <= '﻿');

    /// <summary>Every presentation form of the tables back to its logical letters.</summary>
    private static Dictionary<char, string> BuildUnshaped()
    {
        var map = new Dictionary<char, string>();
        foreach (KeyValuePair<char, (char iso, char fin, char ini, char med)> f in Forms)
            foreach (char form in new[] { f.Value.iso, f.Value.fin, f.Value.ini, f.Value.med })
                if (form != '\0')
                    map[form] = f.Key.ToString();
        foreach (KeyValuePair<char, (char iso, char fin)> l in LamAlef)
        {
            map[l.Value.iso] = Lam.ToString() + l.Key;
            map[l.Value.fin] = Lam.ToString() + l.Key;
        }
        return map;
    }

    /// <summary>A letter that connects to the next one.</summary>
    private static bool DualJoining(char c) => Forms.TryGetValue(c, out var f) && f.ini != '\0';

    /// <summary>A letter the previous one can connect to (every letter but hamza).</summary>
    private static bool Joins(char c) => Forms.ContainsKey(c) && c != 'ء';

    /// <summary>Direction class: R for Arabic, L for ASCII letters and digits, N for the rest.</summary>
    private static char Class(char c)
    {
        if ((c >= '؀' && c <= 'ۿ') || IsPresentationForm(c))
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
