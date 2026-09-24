using System.Collections.Generic;

/// <summary>
/// Which document fields may be forged: only fields the player can disprove.
/// Pure, so the decision table is tested headless.
/// </summary>
public static class Forgery
{
    /// <summary>
    /// Whether forging a field of <paramref name="category"/> on a claim of
    /// (<paramref name="nationId"/>, <paramref name="eraId"/>) could be proven:
    /// names never (forged names wait for the missing-record mechanic); birth
    /// dates when the true date is readable (Citizen Records hold it); place
    /// facts when a reference book covers the category and today's table holds
    /// the claim's truth plus a different value to forge with.
    /// </summary>
    public static bool IsProvable(ClueCategory category, string nationId, string eraId, FactTable facts,
                                  ICollection<ClueCategory> bookCategories, string trueBirthDate)
    {
        switch (category)
        {
            case ClueCategory.Name:
                return false;
            case ClueCategory.BirthDate:
                return BirthDates.TryParse(trueBirthDate, out _, out _, out _);
        }

        if (facts == null || bookCategories == null || !bookCategories.Contains(category))
            return false;

        string truth = facts.Get(nationId, eraId, category);
        return !string.IsNullOrEmpty(truth) && facts.HasOtherValue(category, truth);
    }
}
