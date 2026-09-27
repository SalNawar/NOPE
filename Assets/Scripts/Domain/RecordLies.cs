using System.Collections.Generic;
using System.Linq;

/// <summary>One of a traveller's papers as the record lies see it: its form number ("TC-101") and its fields, in form order.</summary>
public readonly struct RecordForm
{
    /// <summary>The agency form number ("TC-101").</summary>
    public readonly string FormNumber;

    /// <summary>The paper's fields, in form order (the values the honest traveller would print).</summary>
    public readonly IReadOnlyList<DocumentField> Fields;

    /// <summary>Creates a form.</summary>
    public RecordForm(string formNumber, IReadOnlyList<DocumentField> fields)
    {
        FormNumber = formNumber;
        Fields = fields;
    }
}

/// <summary>
/// The record lies (traveller types L2, §6.2-6.3): a 2150 citizen who is who
/// they say, from where they say, but whose papers forge fields their own
/// Citizen Account disproves. Each lie names its forged fields (form,
/// category) per variant, and each record category has a false-value maker,
/// so every forged field differs from the account and is provable
/// (Forgery.IsRecordCategory). Draws on the traveller's lie stream after
/// Lies.Roll, in this order: the variant (one Range draw when two or more
/// variants can show; none when one can), then each forged value in the
/// variant's fixed order, each maker drawing what it needs. Pure, so every
/// maker and the draw order are tested headless.
/// </summary>
public static class RecordLies
{
    /// <summary>The Leisure Departure Visa's form number: its class, Citizen ID and birth date are what the tourists' lies forge.</summary>
    public const string Visa = "TC-101";

    /// <summary>The Departure Manifest's form number: its Citizen ID, transponder and class are what a borrowed manifest carries.</summary>
    public const string Manifest = "TC-230";

    /// <summary>One forged field: the form it is on and its category.</summary>
    private readonly struct Forged
    {
        /// <summary>The form's number.</summary>
        public readonly string Form;

        /// <summary>The forged category.</summary>
        public readonly ClueCategory Category;

        /// <summary>Creates a named field.</summary>
        public Forged(string form, ClueCategory category)
        {
            Form = form;
            Category = category;
        }
    }

    /// <summary>Poor posing as rich, forged (L1): the visa's class and the manifest's class read Premium.</summary>
    private static readonly Forged[] RichForged =
    {
        new Forged(Visa, ClueCategory.AccountStatus),
        new Forged(Manifest, ClueCategory.TransponderClass)
    };

    /// <summary>Poor posing as rich, borrowed (L1): the visa's class reads Premium and the manifest is a rich citizen's (their ID, transponder and class).</summary>
    private static readonly Forged[] RichBorrowed =
    {
        new Forged(Visa, ClueCategory.AccountStatus),
        new Forged(Manifest, ClueCategory.CitizenId),
        new Forged(Manifest, ClueCategory.TransponderId),
        new Forged(Manifest, ClueCategory.TransponderClass)
    };

    /// <summary>A doctored identity (L2), the ID: the visa's Citizen ID is another number (so it also contradicts the manifest's).</summary>
    private static readonly Forged[] DoctoredId = { new Forged(Visa, ClueCategory.CitizenId) };

    /// <summary>A doctored identity (L2), the year: the visa's birth date has another year.</summary>
    private static readonly Forged[] DoctoredYear = { new Forged(Visa, ClueCategory.BirthDate) };

    /// <summary>Each record lie's variants, in draw order (the variant draw indexes the ones that can show).</summary>
    private static IReadOnlyList<Forged[]> VariantsOf(LieKind kind)
    {
        switch (kind)
        {
            case LieKind.PoorPosingAsRich:
                return new[] { RichForged, RichBorrowed };
            case LieKind.DoctoredIdentity:
                return new[] { DoctoredId, DoctoredYear };
            default:
                return new Forged[0][];
        }
    }

    /// <summary>
    /// Plans a rolled record lie of <paramref name="kind"/> against
    /// <paramref name="account"/> (the truth) on the traveller's
    /// <paramref name="forms"/> (their papers in case order, honest as
    /// printed): the variant among those that can show (each of its named
    /// fields is printed and its maker can make a false value), then each
    /// forged value in the variant's fixed order. The birth year comes from
    /// the present's years (<paramref name="birthYearMin"/>,
    /// <paramref name="birthYearMax"/>) around <paramref name="coverBirthDate"/>
    /// (BirthDates.PickOtherYear); fresh numbers never belong to anyone today
    /// (<paramref name="takenToday"/>, which they join); a transponder is
    /// another model of the class needed among <paramref name="transponders"/>.
    /// NoPossibleLie, with no draw, when no variant can show; Honest, with no
    /// draw, for a null <paramref name="rng"/>, a null account or a place lie.
    /// </summary>
    public static LiePlan Plan(LieKind kind, IReadOnlyList<RecordForm> forms, CitizenAccount account,
                               string coverBirthDate, int birthYearMin, int birthYearMax,
                               IReadOnlyList<TransponderModel> transponders, ISet<string> takenToday, IRandomSource rng)
    {
        if (rng == null || account == null || !LieKinds.IsRecordLie(kind))
            return LiePlan.Without(LieOutcome.Honest, kind);

        forms = forms ?? new RecordForm[0];
        var showable = new List<Forged[]>();
        foreach (Forged[] variant in VariantsOf(kind))
            if (variant.All(f => Prints(forms, f) && CanForge(f.Category, account, coverBirthDate, birthYearMin, birthYearMax, transponders)))
                showable.Add(variant);

        if (showable.Count == 0)
            return LiePlan.Without(LieOutcome.NoPossibleLie, kind);

        Forged[] chosen = showable.Count == 1 ? showable[0] : showable[rng.Range(0, showable.Count)];
        var tells = new List<RecordTell>(chosen.Length);
        foreach (Forged field in chosen)
        {
            string value = FalseValue(field.Category, account, coverBirthDate, birthYearMin, birthYearMax, transponders, takenToday, rng);
            for (int d = 0; d < forms.Count; d++)
                if (forms[d].FormNumber == field.Form)
                    tells.Add(new RecordTell(d, field.Category, value));
        }

        return new LiePlan(LieOutcome.Forger, kind, tells.AsReadOnly());
    }

    /// <summary>True when one of the forms is <paramref name="field"/>'s form and prints its category.</summary>
    private static bool Prints(IReadOnlyList<RecordForm> forms, Forged field) =>
        forms.Any(f => f.FormNumber == field.Form && f.Fields != null && f.Fields.Any(x => x != null && x.category == field.Category));

    /// <summary>True when the category's maker can make a false value for this account (draws nothing).</summary>
    private static bool CanForge(ClueCategory category, CitizenAccount account, string coverBirthDate, int birthYearMin, int birthYearMax,
                                 IReadOnlyList<TransponderModel> transponders)
    {
        switch (category)
        {
            case ClueCategory.AccountStatus:
                return HigherStatuses(account.Status).Count > 0;
            case ClueCategory.TransponderClass:
                return FalseTransponderClass(account.TransponderClass) != null;
            case ClueCategory.TransponderId:
                return ModelsOf(FalseTransponderClass(account.TransponderClass) ?? account.TransponderClass, transponders).Count > 0;
            case ClueCategory.CitizenId:
                return true;
            case ClueCategory.BirthDate:
                return BirthDates.HasOtherYear(coverBirthDate, birthYearMin, birthYearMax);
            default:
                return false;
        }
    }

    /// <summary>The category's false value (the makers below); the transponder is one of the class the forged papers need (the false class when the class is forged).</summary>
    private static string FalseValue(ClueCategory category, CitizenAccount account, string coverBirthDate, int birthYearMin, int birthYearMax,
                                     IReadOnlyList<TransponderModel> transponders, ISet<string> takenToday, IRandomSource rng)
    {
        switch (category)
        {
            case ClueCategory.AccountStatus:
                return FalseStatus(account.Status, rng).ToString();
            case ClueCategory.TransponderClass:
                return FalseTransponderClass(account.TransponderClass).ToString();
            case ClueCategory.TransponderId:
                return FalseTransponder(FalseTransponderClass(account.TransponderClass) ?? account.TransponderClass, account.Transponder, transponders, takenToday, rng);
            case ClueCategory.CitizenId:
                return FreshCitizenId(takenToday, rng);
            default:
                return BirthDates.PickOtherYear(coverBirthDate, birthYearMin, birthYearMax, rng);
        }
    }

    /// <summary>The statuses above <paramref name="status"/>, in rank order: Standard to Premium; Eligible to Standard or Premium; none above Premium.</summary>
    private static List<CitizenStatus> HigherStatuses(CitizenStatus status)
    {
        var higher = new List<CitizenStatus>();
        if (status == CitizenStatus.Eligible)
            higher.Add(CitizenStatus.Standard);
        if (status != CitizenStatus.Premium)
            higher.Add(CitizenStatus.Premium);
        return higher;
    }

    /// <summary>
    /// A false account status: one class up (Standard to Premium; Eligible to
    /// Standard or Premium), one Range draw over the higher statuses even
    /// when there is one, so the draw count never depends on the status.
    /// Null, with no draw, for Premium (nothing is above it).
    /// </summary>
    public static CitizenStatus? FalseStatus(CitizenStatus status, IRandomSource rng)
    {
        List<CitizenStatus> higher = HigherStatuses(status);
        if (higher.Count == 0)
            return null;
        return higher[rng.Range(0, higher.Count)];
    }

    /// <summary>A false transponder class: Economy to Premium; null for Premium (nothing is above it). No draw.</summary>
    public static TransponderClass? FalseTransponderClass(TransponderClass transponderClass) =>
        transponderClass == TransponderClass.Economy ? TransponderClass.Premium : (TransponderClass?)null;

    /// <summary>The models of <paramref name="transponderClass"/> with a positive weight.</summary>
    private static List<TransponderModel> ModelsOf(TransponderClass transponderClass, IReadOnlyList<TransponderModel> transponders) =>
        (transponders ?? new TransponderModel[0]).Where(t => t != null && t.transponderClass == transponderClass && t.weight > 0f).ToList();

    /// <summary>
    /// A false transponder of <paramref name="needed"/>'s class: another model
    /// than <paramref name="ownTransponder"/>'s (every model of the class when
    /// it is the only one), one weighted draw (WeightedRandom.Pick), with a
    /// fresh serial nobody holds today (AgencyNumbers.TakeUnique, which joins
    /// <paramref name="takenToday"/>), printed as accounts print it
    /// (AccountMaker.TransponderName). Null, with no draw, without a model of
    /// the class.
    /// </summary>
    public static string FalseTransponder(TransponderClass needed, string ownTransponder, IReadOnlyList<TransponderModel> transponders,
                                          ISet<string> takenToday, IRandomSource rng)
    {
        List<TransponderModel> models = ModelsOf(needed, transponders);
        List<TransponderModel> others = models.Where(m => !IsModelOf(ownTransponder, m)).ToList();
        if (others.Count > 0)
            models = others;
        if (models.Count == 0)
            return null;

        TransponderModel model = WeightedRandom.Pick(models, t => t.weight, rng);
        return AccountMaker.TransponderName(model.model, AgencyNumbers.TakeUnique(takenToday, () => AccountMaker.Serial(model.prefix, rng)));
    }

    /// <summary>True when a printed transponder ("Hopper Mk II · HP-40718") is of <paramref name="model"/>.</summary>
    private static bool IsModelOf(string transponder, TransponderModel model) =>
        !string.IsNullOrEmpty(transponder) && !string.IsNullOrEmpty(model.model) &&
        transponder.StartsWith(model.model + " ", System.StringComparison.Ordinal);

    /// <summary>A Citizen ID nobody holds today (AgencyNumbers.TakeUnique over AccountMaker.CitizenId: three draws per attempt), which joins <paramref name="takenToday"/>.</summary>
    public static string FreshCitizenId(ISet<string> takenToday, IRandomSource rng) =>
        AgencyNumbers.TakeUnique(takenToday, () => AccountMaker.CitizenId(rng));
}
