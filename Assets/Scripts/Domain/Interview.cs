using System.Collections.Generic;

/// <summary>One spoken answer, computed at case generation from the same values as the papers.</summary>
public sealed class InterviewAnswer
{
    /// <summary>The fact the answer gives.</summary>
    public ClueCategory category;

    /// <summary>The canonical value: a FactTable value, a placeholder, or a date.</summary>
    public string value;

    /// <summary>True only for an Answer-channel tell (the true home's value while the papers show the cover).</summary>
    public bool isTell;
}

/// <summary>
/// The interview's wording and answer rules, pure so they are tested
/// headless: what a traveller answers, the desk's opener, the claim sentence,
/// the small-talk pick, and the token fills and checks (with the worst-case
/// length the generator checks lines against).
/// </summary>
public static class Interview
{
    /// <summary>The canonical fact value in an answer template.</summary>
    public const string ValueToken = "value";

    /// <summary>The traveller's honorific in the opener.</summary>
    public const string HonorificToken = "honorific";

    /// <summary>A legendary's name in the legendary opener.</summary>
    public const string NameToken = "name";

    /// <summary>A document's name in a request.</summary>
    public const string DocumentToken = "document";

    /// <summary>The claimed place's label in the claim.</summary>
    public const string PlaceToken = "place";

    /// <summary>
    /// What a traveller answers about <paramref name="category"/>: the tell's
    /// value when <paramref name="lie"/> leaks this category on the Answer
    /// channel (a tell); otherwise the cover value, the same one the papers
    /// print (honest travellers, NoPossibleLie, other categories, Papers tells).
    /// </summary>
    public static InterviewAnswer Answer(ClueCategory category, string coverValue, LiePlan lie)
    {
        if (lie != null && lie.ChannelOf(category) == TellChannel.Answer)
            return new InterviewAnswer { category = category, value = lie.TellValue(category), isTell = true };

        return new InterviewAnswer { category = category, value = coverValue, isTell = false };
    }

    /// <summary>
    /// The desk's opener: a premade's own opener (a non-blank
    /// <paramref name="authoredIntro"/>) as it is; else the legendary template
    /// with the name for a premade (a non-blank <paramref name="legendaryName"/>);
    /// otherwise the opener with the honorific of <paramref name="gender"/>.
    /// "" for null lines (and no authored intro).
    /// </summary>
    public static string Opener(InterviewLines lines, TravellerGender gender, string legendaryName, string authoredIntro)
    {
        if (!string.IsNullOrWhiteSpace(authoredIntro))
            return authoredIntro;

        if (lines == null)
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(legendaryName))
            return Fill(lines.openerLegendary != null ? lines.openerLegendary.text : null, NameToken, legendaryName);

        return Fill(lines.opener != null ? lines.opener.text : null, HonorificToken, Honorific(gender, lines));
    }

    /// <summary>A traveller's claim sentence for a place label, in their kind's words; the bare label when the kind has no line (the banner never goes empty).</summary>
    public static string Claim(InterviewLines lines, TravellerKind kind, string placeLabel) => Fill(ClaimTemplate(lines, kind), PlaceToken, placeLabel);

    /// <summary>The claim's template for a kind: its authored line, or "{place}" alone when it has none or it is blank (the claim's key-word spans are taken over it).</summary>
    public static string ClaimTemplate(InterviewLines lines, TravellerKind kind)
    {
        LineText line = ClaimLine(lines, kind);
        string template = line != null ? line.text : null;
        return string.IsNullOrWhiteSpace(template) ? Placeholder(PlaceToken) : template;
    }

    /// <summary>The first claim line of a kind (null entries skipped), or null when it has none.</summary>
    public static LineText ClaimLine(InterviewLines lines, TravellerKind kind)
    {
        if (lines == null || lines.claims == null)
            return null;

        foreach (KindLine k in lines.claims)
            if (k != null && k.kind == kind)
                return k.line;

        return null;
    }

    /// <summary>
    /// Every problem of the per-kind claim lines, the one rule Generate World
    /// and the content validator share: an empty entry, a kind listed twice, a
    /// blank line, a line without {place}, and a kind in <paramref name="kindsInPlay"/>
    /// (the kinds some blueprint makes) with no line. Empty when sound.
    /// </summary>
    public static List<string> ClaimProblems(IReadOnlyList<KindLine> claims, IEnumerable<TravellerKind> kindsInPlay)
    {
        var problems = new List<string>();
        var listed = new HashSet<TravellerKind>();
        foreach (KindLine k in claims ?? new KindLine[0])
        {
            if (k == null)
            {
                problems.Add("interview.claims: an entry is empty.");
                continue;
            }

            if (!listed.Add(k.kind))
                problems.Add($"interview.claims: {k.kind} is listed twice.");
            string text = k.line != null ? k.line.text : null;
            if (string.IsNullOrWhiteSpace(text))
                problems.Add($"interview.claims: the line of {k.kind} is blank.");
            else if (!HoldsToken(text, PlaceToken))
                problems.Add($"interview.claims: the line of {k.kind} must hold {Placeholder(PlaceToken)}.");
        }

        foreach (TravellerKind kind in kindsInPlay ?? new TravellerKind[0])
            if (!listed.Contains(kind))
                problems.Add($"interview.claims: {kind} has no claim line, but a blueprint makes that kind.");

        return problems;
    }

    /// <summary>
    /// A traveller's small talk: from their claimed place's lines, or its
    /// era's when the place has none. One Range draw when there is a line;
    /// null, with no draw, when there is none or <paramref name="rng"/> is null.
    /// </summary>
    public static LineText PickSmallTalk(IReadOnlyList<LineText> placeLines, IReadOnlyList<LineText> eraLines, IRandomSource rng)
    {
        IReadOnlyList<LineText> pool = placeLines != null && placeLines.Count > 0 ? placeLines : eraLines;
        if (rng == null || pool == null || pool.Count == 0)
            return null;

        return pool[rng.Range(0, pool.Count)];
    }

    /// <summary>The template's length with every {token} filled by a value <paramref name="longestValue"/> characters long (0 for null).</summary>
    public static int WorstCaseLength(string template, string token, int longestValue)
    {
        if (template == null)
            return 0;

        string placeholder = Placeholder(token);
        int occurrences = 0;
        for (int i = template.IndexOf(placeholder, System.StringComparison.Ordinal); i >= 0;
             i = template.IndexOf(placeholder, i + placeholder.Length, System.StringComparison.Ordinal))
            occurrences++;

        return template.Length + occurrences * (longestValue - placeholder.Length);
    }

    /// <summary>A token as templates write it ("{value}").</summary>
    public static string Placeholder(string token) => "{" + token + "}";

    /// <summary>True when the template holds the {token} placeholder (false for null); Generate World and the content validator both check templates with it.</summary>
    public static bool HoldsToken(string template, string token) =>
        template != null && template.IndexOf(Placeholder(token), System.StringComparison.Ordinal) >= 0;

    /// <summary>Replaces every {token} in the template; null template gives "", a null value inserts "", other tokens stay.</summary>
    public static string Fill(string template, string token, string value)
    {
        if (template == null)
            return string.Empty;

        return template.Replace(Placeholder(token), value ?? string.Empty);
    }

    /// <summary>The honorific for a gender.</summary>
    private static string Honorific(TravellerGender gender, InterviewLines lines)
    {
        switch (gender)
        {
            case TravellerGender.Male: return lines.honorificMale;
            case TravellerGender.Female: return lines.honorificFemale;
            default: return lines.honorificUnknown;
        }
    }
}
