using System.Collections.Generic;

/// <summary>
/// Proves authored history keeps every book value unique (piece-2 R13):
/// pairwise, so it holds under every subset and order of rules. Generate
/// World checks the source with it and Validate Content Library the assets.
/// </summary>
public static class HistoryChecks
{
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

            if (DiscrepancyLog.ValuesMatch(e.value, baseWorld.Get(e.nationId, e.eraId, e.category)))
                problems.Add($"{what} equals the place's own value, so it would change nothing.");
            else if (baseWorld.TryFindOtherPlaceWith(e.category, e.nationId, e.eraId, e.value, out FactRow other))
                problems.Add($"{what} equals {other.OriginLabel}'s {e.category}, so neither could leak a {e.category} tell while both are in the world.");

            for (int j = 0; j < edits.Count; j++)
            {
                FactEdit o = edits[j];
                if (j == i || o == null || o.category != e.category || (o.nationId == e.nationId && o.eraId == e.eraId))
                    continue;
                if (DiscrepancyLog.ValuesMatch(e.value, o.value))
                {
                    problems.Add($"{what} gives the value of history edit '{o.source}' to another place ({o.nationId}_{o.eraId}).");
                    break;
                }
            }
        }

        return problems;
    }
}
