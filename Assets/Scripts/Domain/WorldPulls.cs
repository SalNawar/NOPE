using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// One answer to a world factor (world_source.json world.outcomes; the
/// endings spec §3): "Monarchy" for "Who runs 2150?". Every outcome is equal:
/// none carries a sign, a score or a verdict (WorldChecks.RankingWords).
/// </summary>
[Serializable]
public sealed class WorldOutcome
{
    /// <summary>The factor it answers ("government", "future", "money").</summary>
    public string factor;

    /// <summary>Stable id within its factor ("monarchy"); pulls, leanings and the saved leads name it.</summary>
    public string id;

    /// <summary>The answer as the player reads it ("Monarchy", "The Space Age").</summary>
    public string name;

    /// <summary>The morning paper's line the morning after it takes the lead (history news, under the night's cap).</summary>
    public string headline;

    /// <summary>One or two sentences on 2150 under this answer, with a joke and an upside, never a verdict (the end of the demo and Chronopedia's present page).</summary>
    public string report;
}

/// <summary>
/// A role's pull (world_source.json world.roles; the endings spec §4.1): an
/// accepted traveller of the archetype pulls the factor toward the
/// destination's leaning on it, by <see cref="pull"/> times their kind's scale.
/// </summary>
[Serializable]
public sealed class WorldRole
{
    /// <summary>The archetype's id ("scientist").</summary>
    public string archetype;

    /// <summary>The factor its travellers pull ("future").</summary>
    public string factor;

    /// <summary>How much one accepted traveller pulls, before the kind's scale (greater than 0).</summary>
    public float pull;
}

/// <summary>A factor and one of its outcomes: a place's leaning (world_source.json places[].leanings) or a factor's "as you found it" answer.</summary>
[Serializable]
public sealed class OutcomeRef
{
    /// <summary>The factor's id.</summary>
    public string factor;

    /// <summary>The outcome's id within the factor.</summary>
    public string outcome;
}

/// <summary>
/// An amount of pull on one outcome: authored (a famous traveller's
/// premades[].pulls, a story rule's history.rules[].pulls, a dialog effect's
/// PullOutcome op) or gathered by the run (WorldState.pulls, one entry per
/// outcome). Never negative: nothing pushes against an outcome.
/// </summary>
[Serializable]
public sealed class OutcomePull
{
    /// <summary>The factor's id.</summary>
    public string factor;

    /// <summary>The outcome's id within the factor.</summary>
    public string outcome;

    /// <summary>The pull, greater than 0.</summary>
    public float amount;
}

/// <summary>
/// A factor's answer as the run holds it (WorldState.leads, latched each
/// night; the endings spec §4.3): one outcome, or a split between two.
/// </summary>
[Serializable]
public sealed class FactorLead
{
    /// <summary>The factor's id.</summary>
    public string factor;

    /// <summary>The leading outcome (the first half of a split).</summary>
    public string outcome;

    /// <summary>The split's other half; blank when <see cref="outcome"/> leads alone.</summary>
    public string split;

    /// <summary>The day from which this answer holds (0: as the run found it).</summary>
    public int sinceDay;

    /// <summary>True when the factor is split between <see cref="outcome"/> and <see cref="split"/>.</summary>
    public bool IsSplit => !string.IsNullOrEmpty(split);

    /// <summary>True when <paramref name="other"/> gives the same answer (a split's halves in either order), whatever its day.</summary>
    public bool SameAnswer(FactorLead other) =>
        other != null && (IsSplit
            ? other.IsSplit && ((outcome == other.outcome && split == other.split) || (outcome == other.split && split == other.outcome))
            : !other.IsSplit && outcome == other.outcome);
}

/// <summary>A factor answered by pulls: its id, its "as you found it" outcome and its outcomes in content order.</summary>
public readonly struct PullFactor
{
    /// <summary>A factor <paramref name="id"/> whose outcomes are <paramref name="outcomes"/> (content order) and whose "as you found it" answer is <paramref name="statusQuo"/>.</summary>
    public PullFactor(string id, string statusQuo, IReadOnlyList<string> outcomes)
    {
        Id = id;
        StatusQuo = statusQuo;
        Outcomes = outcomes ?? Array.Empty<string>();
    }

    /// <summary>The factor's id.</summary>
    public string Id { get; }

    /// <summary>The "as you found it" outcome: starts ahead by the status-quo weight, and a denial pulls it.</summary>
    public string StatusQuo { get; }

    /// <summary>Its outcomes' ids in content order (ties go to the earlier one).</summary>
    public IReadOnlyList<string> Outcomes { get; }
}

/// <summary>
/// How the run's choices become the world's answers, without ranking (the
/// endings spec §4, Saleh 2026-09-29: "we dont make judgements"). Every
/// choice adds a non-negative pull to one outcome of one factor: an accepted
/// traveller through their role and the destination's leaning, times their
/// kind's scale; a famous traveller through authored pulls; a denial a small
/// pull toward each "as you found it" outcome; story rules and dialog
/// choices through authored pulls. A factor's answer is the outcome with the
/// most pull when it is ahead by more than the margin, else a split of the top
/// two, with hysteresis. Pure, so it is tested headless.
/// </summary>
public static class WorldPulls
{
    /// <summary>
    /// Adds <paramref name="amount"/> to the outcome's entry in
    /// <paramref name="pulls"/> (one entry per outcome, created on its first
    /// pull). Nothing for a blank factor or outcome, or an amount that is not
    /// greater than 0 (a pull is never negative). True when added.
    /// </summary>
    public static bool Add(List<OutcomePull> pulls, string factor, string outcome, float amount)
    {
        if (pulls == null || string.IsNullOrWhiteSpace(factor) || string.IsNullOrWhiteSpace(outcome) || !(amount > 0f) || float.IsInfinity(amount))
            return false;

        foreach (OutcomePull p in pulls)
        {
            if (p != null && p.factor == factor && p.outcome == outcome)
            {
                p.amount += amount;
                return true;
            }
        }

        pulls.Add(new OutcomePull { factor = factor, outcome = outcome, amount = amount });
        return true;
    }

    /// <summary>Adds every pull of <paramref name="add"/> (see <see cref="Add(List{OutcomePull}, string, string, float)"/>). Returns how many landed.</summary>
    public static int AddAll(List<OutcomePull> pulls, IEnumerable<OutcomePull> add)
    {
        int added = 0;
        foreach (OutcomePull p in add ?? Enumerable.Empty<OutcomePull>())
            if (p != null && Add(pulls, p.factor, p.outcome, p.amount))
                added++;
        return added;
    }

    /// <summary>The pull gathered on one outcome (0 when none).</summary>
    public static float Weight(IEnumerable<OutcomePull> pulls, string factor, string outcome)
    {
        float sum = 0f;
        foreach (OutcomePull p in pulls ?? Enumerable.Empty<OutcomePull>())
            if (p != null && p.factor == factor && p.outcome == outcome && p.amount > 0f)
                sum += p.amount;
        return sum;
    }

    /// <summary>
    /// A kind's scale on a role's pull (GameConfigSO, the endings spec §8.2):
    /// a tourist's holiday leaves a lighter mark than a labourer's or a
    /// displaced person's life. Never below 0.
    /// </summary>
    public static float KindScale(TravellerKind kind, float rich, float poor, float labourer, float displaced)
    {
        float scale;
        switch (kind)
        {
            case TravellerKind.RichTourist: scale = rich; break;
            case TravellerKind.PoorTourist: scale = poor; break;
            case TravellerKind.Labourer: scale = labourer; break;
            default: scale = displaced; break;
        }
        return Math.Max(0f, scale);
    }

    /// <summary>
    /// What one accepted traveller pulls (right or wrong: the stamp's price
    /// is the fine, the world only reads where they went). A famous
    /// traveller's <paramref name="authored"/> pulls, when there are any,
    /// stand instead of the role's and are not scaled. Otherwise every role
    /// of <paramref name="roles"/> for <paramref name="archetypeId"/> pulls
    /// its factor toward the destination's leaning on it
    /// (<paramref name="leanings"/>) by its pull times
    /// <paramref name="kindScale"/>; a role with no factor the destination
    /// leans on adds nothing.
    /// </summary>
    public static List<OutcomePull> ForAccept(IEnumerable<OutcomePull> authored, IEnumerable<WorldRole> roles, string archetypeId,
                                              IEnumerable<OutcomeRef> leanings, float kindScale)
    {
        var result = new List<OutcomePull>();
        List<OutcomePull> famous = (authored ?? Enumerable.Empty<OutcomePull>()).Where(p => p != null && p.amount > 0f).ToList();
        if (famous.Count > 0)
        {
            AddAll(result, famous);
            return result;
        }

        if (string.IsNullOrWhiteSpace(archetypeId) || !(kindScale > 0f))
            return result;

        List<OutcomeRef> leans = (leanings ?? Enumerable.Empty<OutcomeRef>()).Where(l => l != null).ToList();
        foreach (WorldRole role in roles ?? Enumerable.Empty<WorldRole>())
        {
            if (role == null || role.archetype != archetypeId || string.IsNullOrWhiteSpace(role.factor))
                continue;
            OutcomeRef lean = leans.FirstOrDefault(l => l.factor == role.factor);
            if (lean != null)
                Add(result, role.factor, lean.outcome, role.pull * kindScale);
        }
        return result;
    }

    /// <summary>What one denial pulls: <paramref name="denialPull"/> toward each factor's "as you found it" outcome (the past stays untouched). Empty when the pull is not greater than 0.</summary>
    public static List<OutcomePull> ForDenial(IEnumerable<PullFactor> factors, float denialPull)
    {
        var result = new List<OutcomePull>();
        foreach (PullFactor f in factors ?? Enumerable.Empty<PullFactor>())
            Add(result, f.Id, f.StatusQuo, denialPull);
        return result;
    }

    /// <summary>
    /// A factor's answer now (the endings spec §4.3). Each outcome's weight is
    /// its pull, and the "as you found it" outcome starts
    /// <paramref name="statusQuoWeight"/> ahead (the world's inertia). A held
    /// single answer (<paramref name="held"/>; none held reads as "as you
    /// found it") stays until another outcome passes it by more than
    /// <paramref name="margin"/>. Otherwise the heaviest outcome leads when it
    /// is ahead of the next by more than the margin, else the factor is split
    /// between the two; ties go to content order. Pulls on outcomes the
    /// factor does not list are ignored. The returned lead keeps
    /// <paramref name="held"/>'s day when the answer is the same, else 0
    /// (the caller dates it).
    /// </summary>
    public static FactorLead Lead(PullFactor factor, IEnumerable<OutcomePull> pulls, float statusQuoWeight, float margin, FactorLead held)
    {
        List<OutcomePull> list = (pulls ?? Enumerable.Empty<OutcomePull>()).ToList();
        float gap = Math.Max(0f, margin);
        List<(string id, float weight, int order)> ranked = factor.Outcomes
            .Where(o => !string.IsNullOrWhiteSpace(o))
            .Distinct()
            .Select((o, i) => (o, Weight(list, factor.Id, o) + (o == factor.StatusQuo ? Math.Max(0f, statusQuoWeight) : 0f), i))
            .OrderByDescending(r => r.Item2).ThenBy(r => r.Item3)
            .ToList();

        if (ranked.Count == 0)
            return new FactorLead { factor = factor.Id, outcome = string.Empty, split = string.Empty };

        FactorLead holding = held != null && !string.IsNullOrEmpty(held.outcome) ? held : new FactorLead { factor = factor.Id, outcome = factor.StatusQuo, split = string.Empty };
        FactorLead answer = null;
        if (!holding.IsSplit)
        {
            int at = ranked.FindIndex(r => r.id == holding.outcome);
            if (at >= 0)
            {
                bool passed = false;
                foreach ((string id, float weight, int order) r in ranked)
                {
                    if (r.id == holding.outcome)
                        continue;
                    passed = r.weight > ranked[at].weight + gap;
                    break;
                }
                if (!passed)
                    answer = new FactorLead { factor = factor.Id, outcome = holding.outcome, split = string.Empty };
            }
        }

        if (answer == null)
        {
            bool alone = ranked.Count == 1 || ranked[0].weight > ranked[1].weight + gap;
            answer = new FactorLead { factor = factor.Id, outcome = ranked[0].id, split = alone ? string.Empty : ranked[1].id };
        }

        answer.sinceDay = held != null && answer.SameAnswer(held) ? held.sinceDay : 0;
        return answer;
    }

    /// <summary>
    /// The night's latch (the endings spec §4.3, §5.1): each factor's answer
    /// from <paramref name="pulls"/> against its held lead in
    /// <paramref name="leads"/> (updated in place, one entry per factor). A
    /// changed answer is dated <paramref name="day"/> and returned, in factor
    /// order, for the morning paper; a factor answered as the run found it
    /// for the first time is saved silently (nothing changed).
    /// </summary>
    public static List<FactorLead> Latch(IEnumerable<PullFactor> factors, IEnumerable<OutcomePull> pulls, List<FactorLead> leads,
                                         float statusQuoWeight, float margin, int day)
    {
        var changed = new List<FactorLead>();
        if (leads == null)
            return changed;

        List<OutcomePull> list = (pulls ?? Enumerable.Empty<OutcomePull>()).ToList();
        foreach (PullFactor f in factors ?? Enumerable.Empty<PullFactor>())
        {
            if (string.IsNullOrWhiteSpace(f.Id))
                continue;
            int at = leads.FindIndex(l => l != null && l.factor == f.Id);
            FactorLead held = at >= 0 ? leads[at] : null;
            FactorLead now = Lead(f, list, statusQuoWeight, margin, held);
            if (string.IsNullOrEmpty(now.outcome))
                continue;

            bool asFound = !now.IsSplit && now.outcome == f.StatusQuo;
            bool same = held != null ? now.SameAnswer(held) : asFound;
            if (!same)
            {
                now.sinceDay = day;
                changed.Add(now);
            }

            if (at >= 0)
                leads[at] = now;
            else
                leads.Add(now);
        }
        return changed;
    }

    /// <summary>
    /// The one-time seed of an older save continued mid-run (the endings spec
    /// §9): true (and <paramref name="pulls"/> filled) only when it has no
    /// pull yet and <paramref name="day"/> is past 1. Each saved per-place
    /// attribute delta (<paramref name="placeDeltas"/>: place id, attribute
    /// id, delta) whose attribute seeds a factor (<paramref name="seeds"/>,
    /// from <see cref="SeedFactors"/>) pulls that factor toward the place's
    /// leaning (<paramref name="leaningsOf"/>) by the delta's magnitude (the
    /// sign is never read: a pull is never negative).
    /// </summary>
    public static bool FromScores(List<OutcomePull> pulls, int day, IEnumerable<(string place, string attribute, float delta)> placeDeltas,
                                  IReadOnlyDictionary<string, string> seeds, Func<string, IEnumerable<OutcomeRef>> leaningsOf)
    {
        if (pulls == null || pulls.Count > 0 || day <= 1 || seeds == null || leaningsOf == null)
            return false;

        foreach ((string place, string attribute, float delta) in placeDeltas ?? Enumerable.Empty<(string, string, float)>())
        {
            if (attribute == null || !seeds.TryGetValue(attribute, out string factor))
                continue;
            OutcomeRef lean = (leaningsOf(place) ?? Enumerable.Empty<OutcomeRef>()).FirstOrDefault(l => l != null && l.factor == factor);
            if (lean != null)
                Add(pulls, factor, lean.outcome, Math.Abs(delta));
        }
        return true;
    }

    /// <summary>
    /// Which attribute seeds which factor for <see cref="FromScores"/>: an
    /// attribute that the archetypes of <paramref name="moves"/> (archetype
    /// id, an attribute its default impacts move) move, when every archetype
    /// moving it has a role of <paramref name="roles"/> pulling one and the
    /// same factor (Science: the scientists, the future). An attribute also
    /// moved by an archetype pulling nothing or another factor (Art: the
    /// artists and the merchants) seeds nothing: its deltas cannot be told apart.
    /// </summary>
    public static Dictionary<string, string> SeedFactors(IEnumerable<(string archetype, string attribute)> moves, IEnumerable<WorldRole> roles)
    {
        var factorOf = new Dictionary<string, string>();
        foreach (WorldRole r in roles ?? Enumerable.Empty<WorldRole>())
            if (r != null && !string.IsNullOrWhiteSpace(r.archetype) && !string.IsNullOrWhiteSpace(r.factor) && !factorOf.ContainsKey(r.archetype))
                factorOf[r.archetype] = r.factor;

        var seeds = new Dictionary<string, string>();
        var ambiguous = new HashSet<string>();
        foreach ((string archetype, string attribute) in moves ?? Enumerable.Empty<(string, string)>())
        {
            if (string.IsNullOrWhiteSpace(attribute) || ambiguous.Contains(attribute))
                continue;
            string factor = archetype != null && factorOf.TryGetValue(archetype, out string f) ? f : null;
            if (factor == null || (seeds.TryGetValue(attribute, out string had) && had != factor))
            {
                seeds.Remove(attribute);
                ambiguous.Add(attribute);
                continue;
            }
            seeds[attribute] = factor;
        }
        return seeds;
    }

    /// <summary>
    /// A factor's answer in words: the outcome's name
    /// (<paramref name="nameOf"/>, the id when it has none), or a split
    /// through <paramref name="splitLine"/> with {a} and {b} (the two
    /// names); null for no answer.
    /// </summary>
    public static string Words(FactorLead lead, Func<string, string> nameOf, string splitLine)
    {
        if (lead == null || string.IsNullOrEmpty(lead.outcome))
            return null;
        string Name(string id) => (nameOf != null ? nameOf(id) : null) ?? id;
        return lead.IsSplit ? Fill(splitLine, Name(lead.outcome), Name(lead.split)) : Name(lead.outcome);
    }

    /// <summary>The split token of the first half.</summary>
    public const string SplitA = "{a}";

    /// <summary>The split token of the second half.</summary>
    public const string SplitB = "{b}";

    /// <summary><paramref name="template"/> with {a} and {b} filled; "a / b" when the template is blank.</summary>
    public static string Fill(string template, string a, string b) =>
        string.IsNullOrWhiteSpace(template) ? $"{a} / {b}" : template.Replace(SplitA, a).Replace(SplitB, b);
}
