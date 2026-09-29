using System;
using System.Collections.Generic;

/// <summary>
/// Proves authored history keeps every book value unique (piece-2 R13):
/// pairwise, so it holds under every subset and order of rules; and that
/// each rule can do something and fires when meant (days 7-15 V8: a story
/// rule). Generate World checks the source with it and Validate Content
/// Library the assets.
/// </summary>
public static class HistoryChecks
{
    /// <summary>
    /// The Return rules nobody reads (days 7-15 Q9, V8's warning): a rule of
    /// <paramref name="rules"/> in StorySection.Return is never printed, so its
    /// consequence reaches the player only through an appearance whose
    /// condition reads its fired flag (FlagKeys.HistoryRuleFired, among
    /// <paramref name="conditionKeys"/>: the forced entries' conditions); one
    /// warning per Return rule none reads. Empty when every one is read.
    /// </summary>
    public static List<string> ReturnProblems(IEnumerable<(string rule, StorySection section)> rules, IEnumerable<string> conditionKeys)
    {
        var read = new HashSet<string>(conditionKeys ?? Array.Empty<string>());
        var problems = new List<string>();
        foreach ((string rule, StorySection section) in rules ?? Array.Empty<(string, StorySection)>())
            if (section == StorySection.Return && !read.Contains(FlagKeys.HistoryRuleFired(rule)))
                problems.Add($"History rule '{rule}' is never printed (section Return), but no forced entry's condition reads '{FlagKeys.HistoryRuleFired(rule)}', so its consequence never reaches the player.");
        return problems;
    }

    /// <summary>The largest stability change a rule may carry, either way (a percent of where stability stands).</summary>
    public const float MaxStabilityChange = 100f;

    /// <summary>
    /// One message per problem of one history rule (days 7-15 V8), named by
    /// <paramref name="rule"/>: a rule with no edit
    /// (<paramref name="edits"/> 0, a story rule) needs a news line, or it
    /// would do nothing; every rule needs a condition, or it would fire on
    /// the first night; a condition's flag of the premade grammar
    /// (FlagKeys.TryParsePremade: met, accepted, denied) names a premade of
    /// <paramref name="premadeIds"/> (a typo never passes); the stability it
    /// moves is within <see cref="MaxStabilityChange"/> either way. Empty when sound.
    /// </summary>
    public static List<string> RuleProblems(string rule, int edits, bool hasNews, int conditions, IEnumerable<string> flagKeys, float stability, ICollection<string> premadeIds)
    {
        var problems = new List<string>();
        if (edits <= 0 && !hasNews)
            problems.Add($"History rule '{rule}' has no edit and no news line, so it would do nothing: a story rule prints its line.");
        if (conditions <= 0)
            problems.Add($"History rule '{rule}' needs at least one condition (it would fire on the first night).");
        foreach (string key in flagKeys ?? Array.Empty<string>())
            if (FlagKeys.TryParsePremade(key, out string premade, out _) && (premadeIds == null || !premadeIds.Contains(premade)))
                problems.Add($"History rule '{rule}' reads the flag '{key}', whose premade '{premade}' does not exist, so it never passes.");
        if (float.IsNaN(stability) || Math.Abs(stability) > MaxStabilityChange)
            problems.Add($"History rule '{rule}' moves stability by {stability}; a change is a percent from -{MaxStabilityChange} to {MaxStabilityChange}.");
        return problems;
    }

    /// <summary>
    /// One message per problem, naming the edit's source, place and category:
    /// a blank value or one longer than FactTable.MaxValueLength; a category
    /// history may not edit; a place <paramref name="baseWorld"/> (every
    /// place's authored facts, no history) does not hold; a value equal to the
    /// place's own base value (it would change nothing), to another place's
    /// base value in that category, or to another edit's value for a different
    /// place in that category (two edits of one place may share a value).
    /// </summary>
    public static List<string> Problems(IReadOnlyList<FactEdit> edits, FactTable baseWorld)
    {
        var problems = new List<string>();
        if (edits == null)
            return problems;

        for (int i = 0; i < edits.Count; i++)
        {
            FactEdit e = edits[i];
            if (e == null)
                continue;

            string what = $"History edit '{e.source}' ({e.nationId}_{e.eraId} {e.category} = '{e.value}')";
            if (string.IsNullOrWhiteSpace(e.value))
            {
                problems.Add($"{what} has a blank value.");
                continue;
            }

            if (e.value.Length > FactTable.MaxValueLength)
                problems.Add($"{what} is {e.value.Length} characters long; a book row holds at most {FactTable.MaxValueLength}.");

            if (!History.IsEditable(e.category))
            {
                problems.Add($"{what} changes {e.category}, which history may not edit (only {string.Join(", ", History.EditableCategories)}).");
                continue;
            }

            if (baseWorld == null || baseWorld.OriginLabel(e.nationId, e.eraId) == null)
            {
                problems.Add($"{what} names a place the world does not have.");
                continue;
            }

            if (Values.Match(e.value, baseWorld.Get(e.nationId, e.eraId, e.category)))
                problems.Add($"{what} equals the place's own value, so it would change nothing.");
            else if (baseWorld.TryFindOtherPlaceWith(e.category, e.nationId, e.eraId, e.value, out FactRow other))
                problems.Add($"{what} equals {other.OriginLabel}'s {e.category}, so neither could leak a {e.category} tell while both are in the world.");

            for (int j = 0; j < edits.Count; j++)
            {
                FactEdit o = edits[j];
                if (j == i || o == null || o.category != e.category || (o.nationId == e.nationId && o.eraId == e.eraId))
                    continue;
                if (Values.Match(e.value, o.value))
                {
                    problems.Add($"{what} gives the value of history edit '{o.source}' to another place ({o.nationId}_{o.eraId}).");
                    break;
                }
            }
        }

        return problems;
    }
}
