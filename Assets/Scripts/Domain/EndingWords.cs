using System.Collections.Generic;
using System.Text.RegularExpressions;

/// <summary>
/// The words a screen of the run's end never shows (the endings, the evening
/// edition, the world's outcomes): a judgement of the run (a rank, a score, a
/// grade, retirement, a win or a loss; Saleh 2026-09-29: "we dont make
/// judgements ... you dont retire at the end of the game ... the outcome
/// depends on your choices") and the em dash (his standing rule: "never use
/// em dashes"). Pure, so the ending assets' test (EndingWordsTests) and the QA
/// rig's read of the screen (FlowQaOps' qaTexts) share one list.
/// </summary>
public static class EndingWords
{
    /// <summary>The long dash Saleh banned (U+2014).</summary>
    public const char EmDash = '\u2014';

    /// <summary>
    /// Every form of the long dash in a source file: the character, its escape in C# and in the YAML of
    /// Unity (backslash u2014), the HTML entities, and the en dash (U+2013) between spaces, used as a
    /// long dash. A lone en dash (a table cell) or a range such as "Nara-Osaka" stays.
    /// </summary>
    private static readonly Regex DashForm = new Regex(
        @"—|\\u2014|&mdash;|&#0*8212;|&#x0*2014;|(?<=\s)(?:–|\\u2013)(?=\s)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>The judgement words and phrases, whole words only, any case.</summary>
    private static readonly Regex Judgement = new Regex(
        @"\b(retire[ds]?|retirement|retiring|rank|ranks|ranked|ranking|score[ds]?|scoring|grade[ds]?|rating|judgement|judgment|good ending|bad ending|best ending|worst ending|you win|you won|you lose|you lost|victory|defeat|defeated|congratulations|well done|game over)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>
    /// The 1-based numbers of the lines of <paramref name="source"/> that hold a long dash in any form
    /// (see DashForm). Empty for a clean file, null or "". A YAML string that wraps over two lines
    /// counts at the line where the dash is. Each line shows once.
    /// </summary>
    public static List<int> DashLines(string source)
    {
        var lines = new List<int>();
        if (string.IsNullOrEmpty(source))
            return lines;

        foreach (Match m in DashForm.Matches(source))
        {
            int line = 1;
            for (int i = 0; i < m.Index; i++)
                if (source[i] == '\n')
                    line++;
            if (!lines.Contains(line))
                lines.Add(line);
        }
        return lines;
    }

    /// <summary>
    /// What <paramref name="text"/> holds that an ending screen never shows:
    /// "N em dash(es)" first when it has any, then one entry per judgement word
    /// met ("judgement word 'Retirement'"). Empty for plain text, null or "".
    /// </summary>
    public static List<string> Problems(string text)
    {
        var problems = new List<string>();
        if (string.IsNullOrEmpty(text))
            return problems;

        int dashes = 0;
        foreach (char c in text)
            if (c == EmDash)
                dashes++;
        if (dashes > 0)
            problems.Add($"{dashes} em dash{(dashes > 1 ? "es" : string.Empty)}");

        foreach (Match m in Judgement.Matches(text))
            problems.Add($"judgement word '{m.Value}'");
        return problems;
    }
}
