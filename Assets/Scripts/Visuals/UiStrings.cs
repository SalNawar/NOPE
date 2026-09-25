using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

/// <summary>How a culture label shows its English word (the reading language) beside it.</summary>
public enum GlossStyle
{
    /// <summary>No gloss.</summary>
    None,

    /// <summary>On a second, smaller line (Accept, Deny, START SHIFT...).</summary>
    Below,

    /// <summary>In brackets after it (MATCH, MISMATCH).</summary>
    Inline
}

/// <summary>Whether a culture table may translate a key (piece 6 R2: only the flavour tier in v1).</summary>
public enum StringTier
{
    /// <summary>English in every culture in v1.</summary>
    Full,

    /// <summary>One of the curated labels a culture translates.</summary>
    Flavour
}

/// <summary>One UI string of a table. Gloss and tier are read from the reading (English) table only.</summary>
[Serializable]
public sealed class UiStringEntry
{
    /// <summary>The key ("accept", "tray.day").</summary>
    public string key;

    /// <summary>The text or template ("Day {0}", "{0:+0;-0;0}").</summary>
    public string text;

    /// <summary>How a culture label shows the English word (reading table only).</summary>
    public GlossStyle gloss;

    /// <summary>Whether cultures may translate it (reading table only).</summary>
    public StringTier tier;
}

/// <summary>
/// The UI string lookup (piece 6 U7): the culture's label, else the reading
/// language's, else the key itself (recorded as missing). Templates take
/// {n} and {n:format} arguments; numbers are formatted in the invariant
/// culture (R17) and strings are inserted verbatim, so canonical values stay
/// exactly as generated. A right-to-left culture's text is shaped
/// (ArabicShaper) after its arguments are filled; a culture label whose
/// reading entry has a gloss shows the English word with it.
/// </summary>
public sealed class UiStrings
{
    private readonly Dictionary<string, UiStringEntry> _reading = new Dictionary<string, UiStringEntry>(StringComparer.Ordinal);
    private readonly Dictionary<string, UiStringEntry> _culture = new Dictionary<string, UiStringEntry>(StringComparer.Ordinal);
    private readonly bool _rightToLeft;
    private readonly int _glossPercent;
    private readonly List<string> _missing = new List<string>();
    private readonly HashSet<string> _missingSet = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>A lookup over the reading table and, when <paramref name="culture"/> is not null, a culture table (first entry per key wins).</summary>
    public UiStrings(IReadOnlyList<UiStringEntry> reading, IReadOnlyList<UiStringEntry> culture, bool cultureRightToLeft, int glossPercent)
    {
        Fill(_reading, reading);
        Fill(_culture, culture);
        _rightToLeft = cultureRightToLeft;
        _glossPercent = glossPercent;
    }

    /// <summary>The keys looked up and found in no table, distinct, in first-miss order.</summary>
    public IReadOnlyCollection<string> MissingKeys => _missing;

    /// <summary>The string for a key (a template's placeholders unfilled).</summary>
    public string Get(string key) => Format(key);

    /// <summary>
    /// The string for a key with its placeholders filled: an IFormattable
    /// argument through its placeholder's format and the invariant culture, a
    /// string verbatim (its format ignored), null as "". Then shaping (a
    /// right-to-left culture label) and the gloss.
    /// </summary>
    public string Format(string key, params object[] args)
    {
        key = key ?? string.Empty;
        _reading.TryGetValue(key, out UiStringEntry reading);
        if (_culture.TryGetValue(key, out UiStringEntry culture) && culture.text != null)
        {
            string native = Fill(culture.text, args);
            if (_rightToLeft)
                native = ArabicShaper.ToVisual(native);
            if (reading == null || reading.text == null)
                return native;
            string english = Fill(reading.text, args);
            switch (reading.gloss)
            {
                case GlossStyle.Below:
                    return native + "\n<size=" + _glossPercent.ToString(CultureInfo.InvariantCulture) + "%><noparse>" + english + "</noparse></size>";
                case GlossStyle.Inline:
                    return native + " (" + english + ")";
                default:
                    return native;
            }
        }

        if (reading != null && reading.text != null)
            return Fill(reading.text, args);

        if (_missingSet.Add(key))
            _missing.Add(key);
        return key;
    }

    /// <summary>The distinct placeholder tokens of a template in first-appearance order ("0", "1:+0.#;-0.#").</summary>
    public static IReadOnlyList<string> Placeholders(string template)
    {
        var tokens = new List<string>();
        Scan(template, token =>
        {
            if (!tokens.Contains(token))
                tokens.Add(token);
        }, out _);
        return tokens;
    }

    /// <summary>
    /// Every problem of a reading table and, when given, a culture table
    /// against it: duplicate or blank keys, an unmatched brace or a malformed
    /// placeholder, a culture key the reading table lacks, a culture key whose
    /// reading entry is not Flavour (R2), culture placeholders (format
    /// specifiers included) that differ from the reading entry's, and
    /// right-to-left text the shaper cannot shape. Generate World and the
    /// content validator both call it.
    /// </summary>
    public static List<string> TableProblems(IReadOnlyList<UiStringEntry> reading, IReadOnlyList<UiStringEntry> culture, bool rightToLeft)
    {
        var problems = new List<string>();
        Dictionary<string, UiStringEntry> readingByKey = CheckTable("reading table", reading, problems);
        if (culture == null)
            return problems;

        CheckTable("culture table", culture, problems);
        foreach (UiStringEntry e in culture.Where(e => e != null && !string.IsNullOrWhiteSpace(e.key)))
        {
            if (!readingByKey.TryGetValue(e.key, out UiStringEntry r))
            {
                problems.Add($"culture table: key '{e.key}' is not in the reading table.");
                continue;
            }
            if (r.tier != StringTier.Flavour)
                problems.Add($"culture table: key '{e.key}' is not a flavour key; only flavour keys are translated in v1.");
            if (!Placeholders(e.text).OrderBy(t => t, StringComparer.Ordinal).SequenceEqual(Placeholders(r.text).OrderBy(t => t, StringComparer.Ordinal)))
                problems.Add($"culture table: key '{e.key}' has placeholders [{string.Join(", ", Placeholders(e.text))}] but the reading entry has [{string.Join(", ", Placeholders(r.text))}].");
            if (rightToLeft && !ArabicShaper.CanShape(e.text, out string unsupported))
                problems.Add($"culture table: key '{e.key}' holds characters the Arabic shaper cannot shape ({unsupported}).");
        }
        return problems;
    }

    /// <summary>Duplicate and blank keys and brace problems of one table; returns its entries by key.</summary>
    private static Dictionary<string, UiStringEntry> CheckTable(string name, IReadOnlyList<UiStringEntry> table, List<string> problems)
    {
        var byKey = new Dictionary<string, UiStringEntry>(StringComparer.Ordinal);
        foreach (UiStringEntry e in table ?? Array.Empty<UiStringEntry>())
        {
            if (e == null || string.IsNullOrWhiteSpace(e.key))
            {
                problems.Add($"{name}: an entry has a blank key.");
                continue;
            }
            if (!byKey.ContainsKey(e.key))
                byKey[e.key] = e;
            else
                problems.Add($"{name}: key '{e.key}' appears more than once.");

            Scan(e.text, _ => { }, out string error);
            if (error != null)
                problems.Add($"{name}: key '{e.key}': {error}.");
        }
        return byKey;
    }

    /// <summary>Fills a template's placeholders (an argument index out of range leaves its token as written).</summary>
    private static string Fill(string template, object[] args)
    {
        if (string.IsNullOrEmpty(template))
            return template ?? string.Empty;

        var sb = new StringBuilder(template.Length + 16);
        int i = 0;
        while (i < template.Length)
        {
            char c = template[i];
            if (c == '{' && i + 1 < template.Length && template[i + 1] == '{')
            {
                sb.Append('{');
                i += 2;
                continue;
            }
            if (c == '}' && i + 1 < template.Length && template[i + 1] == '}')
            {
                sb.Append('}');
                i += 2;
                continue;
            }
            if (c == '{')
            {
                int close = template.IndexOf('}', i + 1);
                if (close > i && TryToken(template.Substring(i + 1, close - i - 1), out int index, out string format) && args != null && index < args.Length)
                {
                    sb.Append(Arg(args[index], format));
                    i = close + 1;
                    continue;
                }
            }
            sb.Append(c);
            i++;
        }
        return sb.ToString();
    }

    /// <summary>One argument: a string verbatim, a number through its format in the invariant culture, null as "".</summary>
    private static string Arg(object arg, string format)
    {
        switch (arg)
        {
            case null: return string.Empty;
            case string s: return s;
            case IFormattable f: return f.ToString(format, CultureInfo.InvariantCulture);
            default: return arg.ToString();
        }
    }

    /// <summary>Parses "n" or "n:format".</summary>
    private static bool TryToken(string token, out int index, out string format)
    {
        index = -1;
        int colon = token.IndexOf(':');
        string number = colon < 0 ? token : token.Substring(0, colon);
        format = colon < 0 ? null : token.Substring(colon + 1);
        return number.Length > 0 && number.All(ch => ch >= '0' && ch <= '9') &&
               int.TryParse(number, NumberStyles.None, CultureInfo.InvariantCulture, out index);
    }

    /// <summary>Walks a template's placeholders; <paramref name="error"/> names the first unmatched brace or malformed placeholder.</summary>
    private static void Scan(string template, Action<string> onToken, out string error)
    {
        error = null;
        if (string.IsNullOrEmpty(template))
            return;

        int i = 0;
        while (i < template.Length)
        {
            char c = template[i];
            if ((c == '{' || c == '}') && i + 1 < template.Length && template[i + 1] == c)
            {
                i += 2;
                continue;
            }
            if (c == '}')
            {
                error = error ?? $"an unmatched '}}' at {i}";
                i++;
                continue;
            }
            if (c == '{')
            {
                int close = template.IndexOf('}', i + 1);
                int nextOpen = template.IndexOf('{', i + 1);
                if (close < 0 || (nextOpen >= 0 && nextOpen < close))
                {
                    error = error ?? $"an unmatched '{{' at {i}";
                    i++;
                    continue;
                }
                string token = template.Substring(i + 1, close - i - 1);
                if (TryToken(token, out _, out _))
                    onToken(token);
                else
                    error = error ?? $"a malformed placeholder '{{{token}}}'";
                i = close + 1;
                continue;
            }
            i++;
        }
    }

    /// <summary>Indexes a table by key (the first entry per key wins).</summary>
    private static void Fill(Dictionary<string, UiStringEntry> target, IReadOnlyList<UiStringEntry> entries)
    {
        if (entries == null)
            return;
        foreach (UiStringEntry e in entries)
            if (e != null && !string.IsNullOrEmpty(e.key) && !target.ContainsKey(e.key))
                target[e.key] = e;
    }
}
