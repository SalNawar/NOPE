using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// How a world factor is answered (world_source.json world.factors[].answer).
/// Serialized in WorldFactor.answer: append only (SerializedEnumsTests pins
/// every value).
/// </summary>
public enum FactorAnswer
{
    /// <summary>As the run found it: the factor's <see cref="WorldFactor.foundAs"/> text (no state moves it).</summary>
    AsFound,

    /// <summary>The timeline's leader (HistoryState.leaderId): the leading nation's name, else <see cref="WorldFactor.foundAs"/> while none leads.</summary>
    Leader,

    /// <summary>The run's pulls (the endings spec E1-E2, WorldPulls): one of the factor's outcomes, or a split of two, starting at its <see cref="WorldFactor.statusQuo"/> outcome.</summary>
    Pulls
}

/// <summary>One question the end of the demo answers about 2150 ("Who runs 2150?"), a row of world_source.json world.factors.</summary>
[Serializable]
public sealed class WorldFactor
{
    /// <summary>Stable id ("government", "future", "money", "culture"); the outcomes, leanings and pulls name their factor by it.</summary>
    public string id;

    /// <summary>The question as the player reads it ("Who runs 2150?").</summary>
    public string question;

    /// <summary>How it is answered.</summary>
    public FactorAnswer answer;

    /// <summary>The answer as the run found 2150 ("None: the neutral Temporal Customs Zone"); also the Leader answer while no nation leads. A Pulls factor's is its <see cref="statusQuo"/> outcome's name (blank here).</summary>
    public string foundAs;

    /// <summary>A Pulls factor's "as you found it" outcome id ("directorate"): it starts ahead by GameConfigSO.worldStatusQuoWeight and a denial pulls it.</summary>
    public string statusQuo;

    /// <summary>A Pulls factor's answer when split between two outcomes, with {a} and {b} (their names): a split is its own answer, never a failure.</summary>
    public string splitLine;

    /// <summary>The morning paper's line the morning after the factor splits, with {a} and {b}.</summary>
    public string splitHeadline;
}

/// <summary>The world block of the content (world_source.json "world", written by Generate World into ContentLibrarySO.World).</summary>
[Serializable]
public sealed class WorldContent
{
    /// <summary>The factors in the order the end of the demo lists them.</summary>
    public List<WorldFactor> factors = new();

    /// <summary>Every Pulls factor's answers (world.outcomes), in content order per factor; any number of them (Saleh: "many combinations, the game is config based").</summary>
    public List<WorldOutcome> outcomes = new();

    /// <summary>Which factor each role pulls and by how much (world.roles, the endings spec §4.1); a role not listed pulls nothing.</summary>
    public List<WorldRole> roles = new();

    /// <summary>The Pulls factors as the rules read them (WorldPulls), in content order.</summary>
    public List<PullFactor> PullFactors() =>
        (factors ?? new List<WorldFactor>())
            .Where(f => f != null && f.answer == FactorAnswer.Pulls && !string.IsNullOrWhiteSpace(f.id))
            .Select(f => new PullFactor(f.id, f.statusQuo, OutcomesOf(f.id).Select(o => o.id).ToList()))
            .ToList();

    /// <summary>A factor's outcomes in content order.</summary>
    public IEnumerable<WorldOutcome> OutcomesOf(string factor) =>
        (outcomes ?? new List<WorldOutcome>()).Where(o => o != null && o.factor == factor);

    /// <summary>The outcome <paramref name="id"/> of <paramref name="factor"/>, or null.</summary>
    public WorldOutcome Outcome(string factor, string id) => OutcomesOf(factor).FirstOrDefault(o => o.id == id);

    /// <summary>The factor with this id, or null.</summary>
    public WorldFactor Factor(string id) => (factors ?? new List<WorldFactor>()).FirstOrDefault(f => f != null && f.id == id);

    /// <summary>
    /// A Pulls factor's answer in words (WorldPulls.Words): the outcome's
    /// name, or the factor's split line with both names; null when
    /// <paramref name="lead"/> answers nothing.
    /// </summary>
    public string Words(FactorLead lead)
    {
        if (lead == null)
            return null;
        WorldFactor f = Factor(lead.factor);
        return WorldPulls.Words(lead, id => Outcome(lead.factor, id)?.name, f?.splitLine);
    }

    /// <summary>
    /// The morning paper's line for a changed answer (the endings spec
    /// §5.1): the outcome's headline, or the factor's split headline with
    /// both names; null when there is none.
    /// </summary>
    public string Headline(FactorLead lead)
    {
        if (lead == null || string.IsNullOrEmpty(lead.outcome))
            return null;
        if (!lead.IsSplit)
            return Outcome(lead.factor, lead.outcome)?.headline;
        WorldFactor f = Factor(lead.factor);
        return string.IsNullOrWhiteSpace(f?.splitHeadline) ? null
            : WorldPulls.Fill(f.splitHeadline, Outcome(lead.factor, lead.outcome)?.name ?? lead.outcome, Outcome(lead.factor, lead.split)?.name ?? lead.split);
    }

    /// <summary>
    /// Each Pulls factor's answer now (WorldPulls.Lead over
    /// <paramref name="pulls"/>, with each factor's held lead of
    /// <paramref name="leads"/> for its hysteresis): what the end of the demo
    /// and Chronopedia read, the run's latest choices included, without
    /// changing the saved leads.
    /// </summary>
    public List<FactorLead> AnswersNow(IEnumerable<OutcomePull> pulls, IEnumerable<FactorLead> leads, float statusQuoWeight, float margin)
    {
        List<OutcomePull> list = (pulls ?? Enumerable.Empty<OutcomePull>()).ToList();
        List<FactorLead> held = (leads ?? Enumerable.Empty<FactorLead>()).Where(l => l != null).ToList();
        return PullFactors().Select(f => WorldPulls.Lead(f, list, statusQuoWeight, margin, held.FirstOrDefault(l => l.factor == f.Id))).ToList();
    }

    /// <summary>
    /// What Generate World and the validator refuse: no factor; a blank id
    /// or question; an id used twice; an AsFound or Leader factor with no
    /// foundAs; a Pulls factor whose "as you found it" outcome is not one of
    /// its outcomes, or with fewer than two outcomes, or no split line or
    /// split headline; an outcome of no Pulls factor, listed twice, or with
    /// a blank name, headline or report; a role with a blank archetype, a
    /// factor that is no Pulls factor or a pull not above 0; and any world
    /// text that ranks or judges (<see cref="WorldFactors.JudgingWords"/>).
    /// Empty when sound.
    /// </summary>
    public List<string> Problems()
    {
        var problems = new List<string>();
        if (factors == null || factors.Count == 0)
        {
            problems.Add("world.factors is empty: the end of the demo lists the world's outcomes from it.");
            return problems;
        }
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < factors.Count; i++)
        {
            WorldFactor f = factors[i];
            if (f == null || string.IsNullOrWhiteSpace(f.id))
            {
                problems.Add($"world.factors[{i}] has no id.");
                continue;
            }
            if (!seen.Add(f.id))
                problems.Add($"world.factors '{f.id}' is listed twice.");
            if (string.IsNullOrWhiteSpace(f.question))
                problems.Add($"world.factors '{f.id}' has no question.");
            if (f.answer != FactorAnswer.Pulls && string.IsNullOrWhiteSpace(f.foundAs))
                problems.Add($"world.factors '{f.id}' has no foundAs: the answer as the run found 2150.");
            if (f.answer == FactorAnswer.Pulls)
            {
                if (OutcomesOf(f.id).Count() < 2)
                    problems.Add($"world.factors '{f.id}' is answered by pulls but has {OutcomesOf(f.id).Count()} outcome(s) in world.outcomes; it needs at least two.");
                if (Outcome(f.id, f.statusQuo) == null)
                    problems.Add($"world.factors '{f.id}': statusQuo '{f.statusQuo}' is not one of its world.outcomes (the answer as the run found 2150).");
                if (string.IsNullOrWhiteSpace(f.splitLine) || !f.splitLine.Contains(WorldPulls.SplitA) || !f.splitLine.Contains(WorldPulls.SplitB))
                    problems.Add($"world.factors '{f.id}' needs a splitLine holding {WorldPulls.SplitA} and {WorldPulls.SplitB} (a split is its own answer).");
                if (string.IsNullOrWhiteSpace(f.splitHeadline) || !f.splitHeadline.Contains(WorldPulls.SplitA) || !f.splitHeadline.Contains(WorldPulls.SplitB))
                    problems.Add($"world.factors '{f.id}' needs a splitHeadline holding {WorldPulls.SplitA} and {WorldPulls.SplitB} (the morning paper's line when it splits).");
            }
            problems.AddRange(WorldFactors.JudgingProblems($"world.factors '{f.id}'", f.question, f.foundAs, f.splitLine, f.splitHeadline));
        }

        var pullFactors = new HashSet<string>(factors.Where(f => f != null && f.answer == FactorAnswer.Pulls && !string.IsNullOrWhiteSpace(f.id)).Select(f => f.id));
        var outcomeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (WorldOutcome o in outcomes ?? new List<WorldOutcome>())
        {
            if (o == null)
                continue;
            string owner = $"world.outcomes '{o.factor}/{o.id}'";
            if (!pullFactors.Contains(o.factor ?? string.Empty))
                problems.Add($"{owner} names no factor answered by pulls.");
            if (string.IsNullOrWhiteSpace(o.id) || o.id.Contains("/"))
                problems.Add($"{owner} needs an id without '/'.");
            else if (!outcomeIds.Add(o.factor + "/" + o.id))
                problems.Add($"{owner} is listed twice.");
            if (string.IsNullOrWhiteSpace(o.name))
                problems.Add($"{owner} has no name.");
            if (string.IsNullOrWhiteSpace(o.headline))
                problems.Add($"{owner} has no headline (the morning paper's line when it takes the lead).");
            if (string.IsNullOrWhiteSpace(o.report))
                problems.Add($"{owner} has no report (2150 under this answer, in a sentence or two).");
            problems.AddRange(WorldFactors.JudgingProblems(owner, o.name, o.headline, o.report));
        }

        foreach (WorldRole r in roles ?? new List<WorldRole>())
        {
            if (r == null)
                continue;
            if (string.IsNullOrWhiteSpace(r.archetype))
                problems.Add("world.roles has a row with no archetype.");
            if (!pullFactors.Contains(r.factor ?? string.Empty))
                problems.Add($"world.roles '{r.archetype}' pulls '{r.factor}', which is no factor answered by pulls.");
            if (!(r.pull > 0f))
                problems.Add($"world.roles '{r.archetype}' has pull {r.pull}; a pull is greater than 0 (nothing pushes against an outcome).");
        }
        return problems;
    }

    /// <summary>
    /// The problems of one authored pull or leaning (a place's leaning, a
    /// premade's or a history rule's pull, a dialog effect's PullOutcome op),
    /// named by <paramref name="owner"/>: the factor is no Pulls factor, the
    /// outcome is not one of its outcomes, or (when <paramref name="amount"/>
    /// is given) the amount is not greater than 0. Empty when sound.
    /// </summary>
    public List<string> RefProblems(string owner, string factor, string outcome, float? amount)
    {
        var problems = new List<string>();
        WorldFactor f = Factor(factor);
        if (f == null || f.answer != FactorAnswer.Pulls)
            problems.Add($"{owner} names '{factor}', which is no world factor answered by pulls.");
        else if (Outcome(factor, outcome) == null)
            problems.Add($"{owner} names '{factor}/{outcome}', which is not one of world.outcomes.");
        if (amount.HasValue && !(amount.Value > 0f))
            problems.Add($"{owner} pulls '{factor}/{outcome}' by {amount.Value}; a pull is greater than 0 (nothing pushes against an outcome).");
        return problems;
    }
}

/// <summary>One line of the world's outcomes: a factor's question and its answer now.</summary>
public readonly struct OutcomeLine
{
    /// <summary>A line of <paramref name="factorId"/>'s question and answer, and what the answer means for 2150 (<paramref name="report"/>; null for none).</summary>
    public OutcomeLine(string factorId, string question, string answer, string report = null)
    {
        FactorId = factorId;
        Question = question;
        Answer = answer;
        Report = report;
    }

    /// <summary>The factor's id.</summary>
    public string FactorId { get; }

    /// <summary>The question ("Who runs 2150?").</summary>
    public string Question { get; }

    /// <summary>The answer ("The Directorate", "Japan").</summary>
    public string Answer { get; }

    /// <summary>What the answer means for 2150 (a Pulls answer's outcome report; a split's two reports); null for none.</summary>
    public string Report { get; }
}

/// <summary>
/// The world's outcomes at the end of the demo (Saleh, 2026-09-29: "for now
/// no ending, just end demo with those 4 outcomes"; the endings spec's
/// phases E0-E2): each factor's question and its answer, listed plainly,
/// never ranked, scored or judged. Shown on the run's last day under END OF
/// DEMO, and after a failure as "the world you leave behind". The culture is
/// the timeline's leader; who runs 2150, what it runs on and how it pays its
/// way are answered by the run's pulls (WorldPulls). Pure, so it is tested
/// headless.
/// </summary>
public static class WorldFactors
{
    /// <summary>The UI string key of the page a failure ending opens ("The world you leave behind"): its button and its heading (world_source.json ui.strings; Generate World and the validator require it).</summary>
    public const string LeftBehindKey = "ending.world.leftBehind";

    /// <summary>
    /// Words that rank or judge a world (Saleh, 2026-09-29: "we dont make
    /// judgements"; the endings spec §10): no world text uses one, matched as
    /// a whole word (its forms listed), whatever its case, so "goods" and
    /// "window" stay free.
    /// </summary>
    public static readonly string[] JudgingWords =
    {
        "better", "worse", "best", "worst", "good", "bad", "triumph", "triumphs", "collapse", "collapsed", "golden", "success", "successful",
        "fail", "fails", "failed", "failure", "win", "wins", "winner", "winners", "winning", "lose", "loses", "loser", "losers", "losing",
        "score", "scores", "rank", "ranked", "ranking", "victory", "defeat", "defeated", "retire", "retired", "retirement",
        "ideal", "utopia", "dystopia", "good ending", "bad ending"
    };

    /// <summary>One problem per text of <paramref name="texts"/> that ranks or judges (<see cref="JudgingWords"/>) or prints a percentage, named by <paramref name="owner"/>.</summary>
    public static List<string> JudgingProblems(string owner, params string[] texts)
    {
        var problems = new List<string>();
        foreach (string text in texts ?? Array.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(text))
                continue;
            string word = JudgingWords.FirstOrDefault(w => Regex.IsMatch(text, $@"\b{Regex.Escape(w)}\b", RegexOptions.IgnoreCase));
            if (word != null)
                problems.Add($"{owner} judges the world ('{word}'): \"{text}\". Outcomes are equal answers, never ranked.");
            if (Regex.IsMatch(text, @"\d\s*%"))
                problems.Add($"{owner} prints a percentage: \"{text}\". The world is told in words, never numbers.");
        }
        return problems;
    }

    /// <summary>
    /// The outcome lines, in content order: an AsFound factor answers its
    /// foundAs; a Leader factor the leading nation's name
    /// (<paramref name="leaderName"/>), or its foundAs when none leads (null
    /// or blank); a Pulls factor its answer in <paramref name="pullAnswers"/>
    /// in words (WorldContent.Words), or its "as you found it" outcome's name
    /// when it has none, with its report. Null factors and factors without an
    /// id are skipped.
    /// </summary>
    public static List<OutcomeLine> Lines(WorldContent world, string leaderName, IEnumerable<FactorLead> pullAnswers)
    {
        List<FactorLead> answers = (pullAnswers ?? Enumerable.Empty<FactorLead>()).Where(a => a != null).ToList();
        return (world?.factors ?? new List<WorldFactor>())
            .Where(f => f != null && !string.IsNullOrWhiteSpace(f.id))
            .Select(f => new OutcomeLine(f.id, f.question, Answer(world, f, leaderName, answers), Report(world, f, answers)))
            .ToList();
    }

    /// <summary>A Pulls factor's answer's report (both halves' for a split; the "as you found it" outcome's without an answer); null for any other factor.</summary>
    private static string Report(WorldContent world, WorldFactor f, List<FactorLead> answers)
    {
        if (f.answer != FactorAnswer.Pulls)
            return null;
        FactorLead lead = answers.FirstOrDefault(a => a.factor == f.id);
        if (lead == null || string.IsNullOrEmpty(lead.outcome))
            return world.Outcome(f.id, f.statusQuo)?.report;
        string first = world.Outcome(f.id, lead.outcome)?.report;
        return lead.IsSplit ? string.Join(" ", new[] { first, world.Outcome(f.id, lead.split)?.report }.Where(s => !string.IsNullOrWhiteSpace(s))) : first;
    }

    /// <summary>A factor's answer now (see <see cref="Lines"/>).</summary>
    private static string Answer(WorldContent world, WorldFactor f, string leaderName, List<FactorLead> answers)
    {
        switch (f.answer)
        {
            case FactorAnswer.Leader:
                return !string.IsNullOrWhiteSpace(leaderName) ? leaderName : f.foundAs;
            case FactorAnswer.Pulls:
                return world.Words(answers.FirstOrDefault(a => a.factor == f.id)) ?? world.Outcome(f.id, f.statusQuo)?.name ?? f.foundAs;
            default:
                return f.foundAs;
        }
    }
}
