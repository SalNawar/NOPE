using System.Collections.Generic;

/// <summary>
/// Which categories can carry a liar's tell, on the papers or in an answer:
/// only those the player can disprove; a place-fact tell's origin proof can
/// only name the true home. Every category is one of: a name (never a tell),
/// a directive-only date (read against the calendar, never compared), a
/// record category (the traveller's own record proves it; the record lies'
/// forged fields, RecordLies) or a place fact (a reference book proves it).
/// Pure, so the decision table is tested headless.
/// </summary>
public static class Forgery
{
    /// <summary>
    /// True for a directive-only category: a departure date or a Valid Until,
    /// read against the desk calendar by the Directives (traveller types P1,
    /// F7), or a waiver's signature, read against the paper-set directive;
    /// never compared with a truth and never a tell.
    /// </summary>
    public static bool IsDirectiveOnly(ClueCategory category) =>
        category == ClueCategory.DepartureDate || category == ClueCategory.Expiry || category == ClueCategory.Signature;

    /// <summary>
    /// True for a visual check (the document design spec, D4, D8): a paper's
    /// seal, held against the Seal Register, or its photo, held against the
    /// traveller at the desk; never a place fact, a record category, a
    /// spoken answer or a place lie's tell (IsProvableCategory is false).
    /// </summary>
    public static bool IsVisual(ClueCategory category) => category == ClueCategory.Seal || category == ClueCategory.Photo;

    /// <summary>
    /// True for a record category: a value the traveller's own record (the
    /// Citizen Account or the Displacement Registry entry) holds and proves,
    /// whatever the books: the birth date, the agency number, the destination,
    /// the incident, the account's status, transponder, class, debt,
    /// waiver and proof of means (a credit line, savings or a policy number),
    /// and the registered contract's employer, term and wage.
    /// </summary>
    public static bool IsRecordCategory(ClueCategory category)
    {
        switch (category)
        {
            case ClueCategory.BirthDate:
            case ClueCategory.CitizenId:
            case ClueCategory.Destination:
            case ClueCategory.Incident:
            case ClueCategory.AccountStatus:
            case ClueCategory.TransponderId:
            case ClueCategory.TransponderClass:
            case ClueCategory.Debt:
            case ClueCategory.WaiverNo:
            case ClueCategory.Credit:
            case ClueCategory.Funds:
            case ClueCategory.PolicyNo:
            case ClueCategory.Employer:
            case ClueCategory.Term:
            case ClueCategory.Wage:
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Whether any tell in this category could ever be proven: the one rule
    /// interview questions and tells share. Never a name; never a
    /// directive-only date; never a visual check (IsVisual: the Seal
    /// Register is a book, but no place's fact); always a record category (the traveller's own
    /// record proves it, redesign phase 7); a place fact exactly when
    /// <paramref name="bookCategories"/> is non-null and holds the category.
    /// </summary>
    public static bool IsProvableCategory(ClueCategory category, ICollection<ClueCategory> bookCategories)
    {
        if (category == ClueCategory.Name || IsDirectiveOnly(category) || IsVisual(category))
            return false;
        if (IsRecordCategory(category))
            return true;
        return bookCategories != null && bookCategories.Contains(category);
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
        if (string.IsNullOrEmpty(claimValue) || string.IsNullOrEmpty(homeValue) || Values.Match(claimValue, homeValue))
            return false;

        return !facts.TryFindOtherPlaceWith(category, home.NationId, home.EraId, homeValue, out _);
    }
}
