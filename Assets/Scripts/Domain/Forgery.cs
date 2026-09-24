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

    /// <summary>
    /// Whether a liar claiming (<paramref name="claimNationId"/>, <paramref name="claimEraId"/>)
    /// whose true home is <paramref name="home"/> can leak a provable tell in
    /// <paramref name="category"/>: names never; a birth date when the cover date
    /// is readable and the home's birth years hold another year
    /// (BirthDates.HasOtherYear); a place fact when a reference book covers the
    /// category, today's table holds both the claim's and the home's value, they
    /// differ under the scanner comparison, and no other place today shares the
    /// home's value (so the origin proof can only name the home).
    /// </summary>
    public static bool IsProvableTell(ClueCategory category, string claimNationId, string claimEraId,
                                      string coverBirthDate, HomeCandidate home, FactTable facts,
                                      ICollection<ClueCategory> bookCategories)
    {
        switch (category)
        {
            case ClueCategory.Name:
                return false;
            case ClueCategory.BirthDate:
                return BirthDates.HasOtherYear(coverBirthDate, home.BirthYearMin, home.BirthYearMax);
        }

        if (facts == null || bookCategories == null || !bookCategories.Contains(category))
            return false;

        string claimValue = facts.Get(claimNationId, claimEraId, category);
        string homeValue = facts.Get(home.NationId, home.EraId, category);
        if (string.IsNullOrEmpty(claimValue) || string.IsNullOrEmpty(homeValue) || DiscrepancyLog.ValuesMatch(claimValue, homeValue))
            return false;

        foreach (FactRow row in facts.Rows(category))
        {
            bool homesOwnRow = row.NationId == home.NationId && row.EraId == home.EraId;
            if (!homesOwnRow && DiscrepancyLog.ValuesMatch(row.Value, homeValue))
                return false;
        }

        return true;
    }
}
