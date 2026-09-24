using System.Collections.Generic;

/// <summary>One of today's places as the lie rules see it: its ids and birth years.</summary>
public readonly struct HomeCandidate
{
    /// <summary>Nation id of the place (matches NationSO.id).</summary>
    public readonly string NationId;

    /// <summary>Era id of the place (matches EraSO.id).</summary>
    public readonly string EraId;

    /// <summary>Earliest birth year of a traveller from here (negative = BCE; 0..0 = none authored).</summary>
    public readonly int BirthYearMin;

    /// <summary>Latest birth year of a traveller from here (negative = BCE).</summary>
    public readonly int BirthYearMax;

    /// <summary>Creates a candidate.</summary>
    public HomeCandidate(string nationId, string eraId, int birthYearMin, int birthYearMax)
    {
        NationId = nationId;
        EraId = eraId;
        BirthYearMin = birthYearMin;
        BirthYearMax = birthYearMax;
    }
}

/// <summary>What one traveller's lie roll decided.</summary>
public enum LieOutcome
{
    /// <summary>The traveller really comes from the place they claim.</summary>
    Honest,

    /// <summary>The traveller comes from another of today's places; their papers leak tells.</summary>
    Liar,

    /// <summary>The roll said liar, but no other place today could give a tell; the traveller stays honest.</summary>
    NoPossibleLie
}

/// <summary>The outcome of one traveller's lie roll: the true home and the tells their papers leak.</summary>
public sealed class LiePlan
{
    /// <summary>Shared empty tell list.</summary>
    private static readonly ClueCategory[] NoTells = new ClueCategory[0];

    /// <summary>The value each tell category prints (the home's fact, or the tell birth date).</summary>
    private readonly Dictionary<ClueCategory, string> _values;

    /// <summary>Creates a plan (Lies.Plan only).</summary>
    internal LiePlan(LieOutcome outcome, int homeIndex, IReadOnlyList<ClueCategory> tells, Dictionary<ClueCategory, string> values)
    {
        Outcome = outcome;
        HomeIndex = homeIndex;
        Tells = tells;
        _values = values;
    }

    /// <summary>A plan with no home and no tells.</summary>
    internal static LiePlan Without(LieOutcome outcome) =>
        new LiePlan(outcome, -1, NoTells, new Dictionary<ClueCategory, string>());

    /// <summary>What the roll decided.</summary>
    public LieOutcome Outcome { get; }

    /// <summary>Index of the true home in the (unfiltered) list Lies.Plan was given; -1 unless <see cref="LieOutcome.Liar"/>.</summary>
    public int HomeIndex { get; }

    /// <summary>The tell categories, in pick order; empty unless <see cref="LieOutcome.Liar"/>.</summary>
    public IReadOnlyList<ClueCategory> Tells { get; }

    /// <summary>
    /// Rewrites every field whose category is a tell with the tell's value and
    /// flags it as an anachronism. Changes nothing for an Honest or
    /// NoPossibleLie plan.
    /// </summary>
    public void ApplyTo(IEnumerable<DocumentField> fields)
    {
        if (fields == null)
            return;

        foreach (DocumentField field in fields)
        {
            if (field != null && _values.TryGetValue(field.category, out string value))
            {
                field.value = value;
                field.isAnachronism = true;
            }
        }
    }
}

/// <summary>
/// Who lies about their home, where they really come from, and which tells
/// their papers leak. Pure and seeded, so every rule and the draw order are
/// tested headless. Draws on the traveller's lie stream (Seeds.ForLies), in
/// this order: the roll; for a liar, the home; one pick per tell category;
/// then the birth year when BirthDate is a tell.
/// </summary>
public static class Lies
{
    /// <summary>
    /// Whether a traveller may lie at all: not a legendary, a claim today's
    /// rules allow, and papers to leak tells on. Exempt travellers make no draw.
    /// </summary>
    public static bool MayLie(bool isLegendary, bool claimAllowed, IReadOnlyList<DocumentField> papers) =>
        !isLegendary && claimAllowed && papers != null && papers.Count > 0;

    /// <summary>
    /// Rolls one traveller's lie. With probability <paramref name="liarChance"/>
    /// the traveller lies: the true home is picked uniformly among today's other
    /// places that could give at least one tell, and
    /// max(1, min(<paramref name="tellCount"/>, that home's eligible categories))
    /// tell categories are picked uniformly without replacement. A category is
    /// eligible when the papers print it and Forgery.IsProvableTell holds; the
    /// candidates keep the papers' first-appearance order. A liar for whom no
    /// place qualifies is NoPossibleLie. A null <paramref name="rng"/> is Honest
    /// with no draw.
    /// </summary>
    public static LiePlan Plan(float liarChance, int tellCount,
                               string claimNationId, string claimEraId, string coverBirthDate,
                               IReadOnlyList<HomeCandidate> todays, IReadOnlyList<DocumentField> papers,
                               FactTable facts, ICollection<ClueCategory> bookCategories, IRandomSource rng)
    {
        if (rng == null || !(rng.Value() < liarChance))
            return LiePlan.Without(LieOutcome.Honest);

        List<ClueCategory> printed = PrintedCategories(papers);
        var candidates = new List<int>();
        var eligibleByCandidate = new List<List<ClueCategory>>();

        if (todays != null)
        {
            for (int i = 0; i < todays.Count; i++)
            {
                HomeCandidate place = todays[i];
                if (place.NationId == claimNationId && place.EraId == claimEraId)
                    continue;

                var eligible = new List<ClueCategory>();
                foreach (ClueCategory category in printed)
                    if (Forgery.IsProvableTell(category, claimNationId, claimEraId, coverBirthDate, place, facts, bookCategories))
                        eligible.Add(category);

                if (eligible.Count > 0)
                {
                    candidates.Add(i);
                    eligibleByCandidate.Add(eligible);
                }
            }
        }

        if (candidates.Count == 0)
            return LiePlan.Without(LieOutcome.NoPossibleLie);

        int pick = rng.Range(0, candidates.Count);
        int homeIndex = candidates[pick];
        HomeCandidate home = todays[homeIndex];
        List<ClueCategory> pool = eligibleByCandidate[pick];

        int count = tellCount < 1 ? 1 : tellCount > pool.Count ? pool.Count : tellCount;
        var tells = new List<ClueCategory>(count);
        for (int k = 0; k < count; k++)
        {
            int index = rng.Range(0, pool.Count);
            tells.Add(pool[index]);
            pool.RemoveAt(index);
        }

        var values = new Dictionary<ClueCategory, string>();
        foreach (ClueCategory category in tells)
            if (category != ClueCategory.BirthDate)
                values[category] = facts.Get(home.NationId, home.EraId, category);

        if (tells.Contains(ClueCategory.BirthDate))
            values[ClueCategory.BirthDate] = BirthDates.PickOtherYear(coverBirthDate, home.BirthYearMin, home.BirthYearMax, rng);

        return new LiePlan(LieOutcome.Liar, homeIndex, tells.AsReadOnly(), values);
    }

    /// <summary>The distinct categories the papers print, in first-appearance order.</summary>
    private static List<ClueCategory> PrintedCategories(IReadOnlyList<DocumentField> papers)
    {
        var printed = new List<ClueCategory>();
        if (papers == null)
            return printed;

        foreach (DocumentField field in papers)
            if (field != null && !printed.Contains(field.category))
                printed.Add(field.category);

        return printed;
    }
}
