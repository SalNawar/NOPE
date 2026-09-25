using System.Collections.Generic;

/// <summary>One of today's places as the lie rules see it: its ids, birth years and whether its dress can leak.</summary>
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

    /// <summary>This place's signature garment can leak onto the traveller's claimed look (Looks.CanLeak).</summary>
    public readonly bool AppearanceLeakable;

    /// <summary>Creates a candidate.</summary>
    public HomeCandidate(string nationId, string eraId, int birthYearMin, int birthYearMax, bool appearanceLeakable = false)
    {
        NationId = nationId;
        EraId = eraId;
        BirthYearMin = birthYearMin;
        BirthYearMax = birthYearMax;
        AppearanceLeakable = appearanceLeakable;
    }
}

/// <summary>What one traveller's lie roll decided.</summary>
public enum LieOutcome
{
    /// <summary>The traveller really comes from the place they claim.</summary>
    Honest,

    /// <summary>The traveller comes from another of today's places; their papers, answers or dress leak tells.</summary>
    Liar,

    /// <summary>The roll said liar, but no other place today could give a tell; the traveller stays honest.</summary>
    NoPossibleLie
}

/// <summary>Where a liar's tell shows; serialized in DayPlanSO, append only.</summary>
public enum TellChannel
{
    /// <summary>Every paper field of the tell's category shows the true home's value.</summary>
    Papers,

    /// <summary>The spoken answer to the category's question gives the true home's value; the papers show the cover.</summary>
    Answer,

    /// <summary>The liar's dress: a dress tell is the true home's signature garment, worn over the claimed look.</summary>
    Appearance
}

/// <summary>The outcome of one traveller's lie roll: the true home and the tells the liar leaks, on the papers, in speech or in dress.</summary>
public sealed class LiePlan
{
    /// <summary>Shared empty tell list.</summary>
    private static readonly ClueCategory[] NoTells = new ClueCategory[0];

    /// <summary>The value of each tell category, printed or spoken (the home's fact, or the tell birth date).</summary>
    private readonly Dictionary<ClueCategory, string> _values;

    /// <summary>The channel each tell category leaks on.</summary>
    private readonly Dictionary<ClueCategory, TellChannel> _channels;

    /// <summary>Creates a plan (Lies.Plan only).</summary>
    internal LiePlan(LieOutcome outcome, int homeIndex, IReadOnlyList<ClueCategory> tells,
                     Dictionary<ClueCategory, string> values, Dictionary<ClueCategory, TellChannel> channels)
    {
        Outcome = outcome;
        HomeIndex = homeIndex;
        Tells = tells;
        _values = values;
        _channels = channels;
    }

    /// <summary>A plan with no home and no tells.</summary>
    internal static LiePlan Without(LieOutcome outcome) =>
        new LiePlan(outcome, -1, NoTells, new Dictionary<ClueCategory, string>(), new Dictionary<ClueCategory, TellChannel>());

    /// <summary>What the roll decided.</summary>
    public LieOutcome Outcome { get; }

    /// <summary>Index of the true home in the (unfiltered) list Lies.Plan was given; -1 unless <see cref="LieOutcome.Liar"/>.</summary>
    public int HomeIndex { get; }

    /// <summary>The tell categories, in pick order; empty unless <see cref="LieOutcome.Liar"/>.</summary>
    public IReadOnlyList<ClueCategory> Tells { get; }

    /// <summary>The channel a tell category leaks on, or null when the category is not a tell.</summary>
    public TellChannel? ChannelOf(ClueCategory category) =>
        _channels.TryGetValue(category, out TellChannel channel) ? channel : (TellChannel?)null;

    /// <summary>A tell's value (the true home's fact, or the tell birth date), or null when the category is not a tell.</summary>
    public string TellValue(ClueCategory category) =>
        _values.TryGetValue(category, out string value) ? value : null;

    /// <summary>
    /// Rewrites every field whose category is a Papers tell with the tell's
    /// value and flags it as an anachronism. An Answer or Appearance tell
    /// leaves the papers on the cover. Changes nothing for an Honest or
    /// NoPossibleLie plan.
    /// </summary>
    public void ApplyTo(IEnumerable<DocumentField> fields)
    {
        if (fields == null)
            return;

        foreach (DocumentField field in fields)
        {
            if (field != null && ChannelOf(field.category) == TellChannel.Papers)
            {
                field.value = _values[field.category];
                field.isAnachronism = true;
            }
        }
    }
}

/// <summary>
/// Who lies about their home, where they really come from, and which tells
/// they leak on their papers, in their answers or in their dress. Pure and seeded, so every
/// rule and the draw order are tested headless. Draws on the traveller's lie
/// stream (Seeds.ForLies), in this order: the roll; for a liar, the home; one
/// pick per tell (a category/channel option); then the birth year when
/// BirthDate is a tell on either channel.
/// </summary>
public static class Lies
{
    /// <summary>One way a home can leak a category: on the papers, in an answer or in dress.</summary>
    private readonly struct TellOption
    {
        /// <summary>The leaked category.</summary>
        public readonly ClueCategory Category;

        /// <summary>Where it leaks.</summary>
        public readonly TellChannel Channel;

        /// <summary>Creates an option.</summary>
        public TellOption(ClueCategory category, TellChannel channel)
        {
            Category = category;
            Channel = channel;
        }
    }

    /// <summary>
    /// Whether a traveller may lie at all: not an honest premade (premades are
    /// honest unless authored as liars), a claim today's rules allow, and
    /// papers to leak tells on. Exempt travellers make no draw.
    /// </summary>
    public static bool MayLie(bool honestPremade, bool claimAllowed, IReadOnlyList<DocumentField> papers) =>
        !honestPremade && claimAllowed && papers != null && papers.Count > 0;

    /// <summary>
    /// Rolls one traveller's lie. With probability <paramref name="liarChance"/>
    /// the traveller lies. A (category, channel) option of another of today's
    /// places is open when <paramref name="channels"/> allows the channel and
    /// Forgery.IsProvableTell holds: Papers for a category the papers print
    /// (first-appearance order), then Answer for one of
    /// <paramref name="answerTellCategories"/> (today's question categories that
    /// may carry an Answer tell, in question order; InterviewDay.AnswerTellCategories),
    /// then Appearance for Culture (Looks.EvidenceCategory) when the place's
    /// HomeCandidate.AppearanceLeakable holds.
    /// The true home is picked uniformly among the places with at least one
    /// open option, and max(1, min(<paramref name="tellCount"/>, the distinct
    /// categories among the home's options)) tells are picked one uniform
    /// draw each over the options still open; a pick closes the other option
    /// of its category, so a category leaks on one channel only. A liar for
    /// whom no place qualifies is NoPossibleLie. A null <paramref name="rng"/>
    /// is Honest with no draw; a null list counts as empty.
    /// </summary>
    public static LiePlan Plan(float liarChance, int tellCount,
                               string claimNationId, string claimEraId, string coverBirthDate,
                               IReadOnlyList<HomeCandidate> todays, IReadOnlyList<DocumentField> papers,
                               IReadOnlyList<ClueCategory> answerTellCategories, IReadOnlyList<TellChannel> channels,
                               FactTable facts, ICollection<ClueCategory> bookCategories, IRandomSource rng)
    {
        if (rng == null || !(rng.Value() < liarChance))
            return LiePlan.Without(LieOutcome.Honest);

        bool papersOpen = Allows(channels, TellChannel.Papers);
        bool answersOpen = Allows(channels, TellChannel.Answer);
        bool dressOpen = Allows(channels, TellChannel.Appearance);
        List<ClueCategory> printed = papersOpen ? PrintedCategories(papers) : new List<ClueCategory>();
        List<ClueCategory> asked = answersOpen ? Distinct(answerTellCategories) : new List<ClueCategory>();

        var candidates = new List<int>();
        var optionsByCandidate = new List<List<TellOption>>();

        if (todays != null)
        {
            for (int i = 0; i < todays.Count; i++)
            {
                HomeCandidate place = todays[i];
                if (place.NationId == claimNationId && place.EraId == claimEraId)
                    continue;

                var options = new List<TellOption>();
                foreach (ClueCategory category in printed)
                    if (Forgery.IsProvableTell(category, claimNationId, claimEraId, coverBirthDate, place, facts, bookCategories))
                        options.Add(new TellOption(category, TellChannel.Papers));
                foreach (ClueCategory category in asked)
                    if (Forgery.IsProvableTell(category, claimNationId, claimEraId, coverBirthDate, place, facts, bookCategories))
                        options.Add(new TellOption(category, TellChannel.Answer));
                if (dressOpen && place.AppearanceLeakable &&
                    Forgery.IsProvableTell(Looks.EvidenceCategory, claimNationId, claimEraId, coverBirthDate, place, facts, bookCategories))
                    options.Add(new TellOption(Looks.EvidenceCategory, TellChannel.Appearance));

                if (options.Count > 0)
                {
                    candidates.Add(i);
                    optionsByCandidate.Add(options);
                }
            }
        }

        if (candidates.Count == 0)
            return LiePlan.Without(LieOutcome.NoPossibleLie);

        int pick = rng.Range(0, candidates.Count);
        int homeIndex = candidates[pick];
        HomeCandidate home = todays[homeIndex];
        List<TellOption> pool = optionsByCandidate[pick];

        int categories = CategoryCount(pool);
        int count = tellCount < 1 ? 1 : tellCount > categories ? categories : tellCount;
        var tells = new List<ClueCategory>(count);
        var channelOf = new Dictionary<ClueCategory, TellChannel>();
        for (int k = 0; k < count; k++)
        {
            TellOption option = pool[rng.Range(0, pool.Count)];
            tells.Add(option.Category);
            channelOf[option.Category] = option.Channel;
            pool.RemoveAll(o => o.Category == option.Category);
        }

        var values = new Dictionary<ClueCategory, string>();
        foreach (ClueCategory category in tells)
            if (category != ClueCategory.BirthDate)
                values[category] = facts.Get(home.NationId, home.EraId, category);

        if (tells.Contains(ClueCategory.BirthDate))
            values[ClueCategory.BirthDate] = BirthDates.PickOtherYear(coverBirthDate, home.BirthYearMin, home.BirthYearMax, rng);

        return new LiePlan(LieOutcome.Liar, homeIndex, tells.AsReadOnly(), values, channelOf);
    }

    /// <summary>True when the channel list holds the channel (a null list holds none).</summary>
    private static bool Allows(IReadOnlyList<TellChannel> channels, TellChannel channel)
    {
        if (channels == null)
            return false;

        foreach (TellChannel c in channels)
            if (c == channel)
                return true;

        return false;
    }

    /// <summary>The distinct categories of a list, in order (empty for null).</summary>
    private static List<ClueCategory> Distinct(IReadOnlyList<ClueCategory> categories)
    {
        var distinct = new List<ClueCategory>();
        if (categories == null)
            return distinct;

        foreach (ClueCategory category in categories)
            if (!distinct.Contains(category))
                distinct.Add(category);

        return distinct;
    }

    /// <summary>How many distinct categories the options cover.</summary>
    private static int CategoryCount(List<TellOption> options)
    {
        var seen = new List<ClueCategory>();
        foreach (TellOption option in options)
            if (!seen.Contains(option.Category))
                seen.Add(option.Category);
        return seen.Count;
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
