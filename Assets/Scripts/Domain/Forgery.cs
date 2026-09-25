using System.Collections.Generic;

/// <summary>
/// Which categories can carry a liar's tell, on the papers or in an answer:
/// only those the player can disprove; a place-fact tell's origin proof can
/// only name the true home. Pure, so the decision table is tested headless.
/// </summary>
public static class Forgery
{
    /// <summary>
    /// Whether any tell in this category could ever be proven: the one rule
    /// interview questions and tells share. Never a name; always a birth date
    /// (the Citizen Record proves it); a place fact exactly when
    /// <paramref name="bookCategories"/> is non-null and holds the category.
    /// </summary>
    public static bool IsProvableCategory(ClueCategory category, ICollection<ClueCategory> bookCategories)
    {
        switch (category)
        {
            case ClueCategory.Name:
                return false;
            case ClueCategory.BirthDate:
                return true;
            default:
                return bookCategories != null && bookCategories.Contains(category);
        }
    }

    /// <summary>
    /// Whether a liar claiming (<paramref name="claimNationId"/>, <paramref name="claimEraId"/>)
    /// whose true home is <paramref name="home"/> can leak a provable tell in
    /// <paramref name="category"/>: never outside <see cref="IsProvableCategory"/>;
    /// a birth date when the cover date is readable and the home's birth years
    /// hold another year (BirthDates.HasOtherYear); a place fact when today's
    /// table holds both the claim's and the home's value, they differ under the
    /// scanner comparison, and no other place today shares the home's value (so
    /// the origin proof can only name the home).
    /// </summary>
    public static bool IsProvableTell(ClueCategory category, string claimNationId, string claimEraId,
                                      string coverBirthDate, HomeCandidate home, FactTable facts,
                                      ICollection<ClueCategory> bookCategories)
    {
        if (!IsProvableCategory(category, bookCategories))
            return false;

        if (category == ClueCategory.BirthDate)
            return BirthDates.HasOtherYear(coverBirthDate, home.BirthYearMin, home.BirthYearMax);

        if (facts == null)
            return false;

        string claimValue = facts.Get(claimNationId, claimEraId, category);
        string homeValue = facts.Get(home.NationId, home.EraId, category);
        if (string.IsNullOrEmpty(claimValue) || string.IsNullOrEmpty(homeValue) || DiscrepancyLog.ValuesMatch(claimValue, homeValue))
            return false;

        return !facts.TryFindOtherPlaceWith(category, home.NationId, home.EraId, homeValue, out _);
    }
}
