using System.Collections.Generic;

/// <summary>
/// Which fields can carry a liar's tell: only fields the player can disprove;
/// a place-fact tell's origin proof can only name the true home. Pure, so the
/// decision table is tested headless.
/// </summary>
public static class Forgery
{
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
