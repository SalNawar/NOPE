using System;
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
/// What the record lies' makers draw from beside the account (explicit
/// inputs, audit R3-025): the cover birth date and the present's years (the
/// year maker), the agency's transponder models and today's numbers (the
/// transponder and number makers), the status a debtor's tourist papers pose
/// as (L4), the era's employers and today's other open places (the
/// contract makers, L5), the waiver prefix (a fake waiver's number, L3) and
/// the proofs of means (a forged policy's prefix, L10).
/// </summary>
public sealed class RecordLieContext
{
    /// <summary>The registered birth date (the visa's honest value): the year maker keeps its day and month.</summary>
    public string CoverBirthDate;

    /// <summary>The present's earliest birth year (BirthDates.PickOtherYear).</summary>
    public int BirthYearMin;

    /// <summary>The present's latest birth year.</summary>
    public int BirthYearMax;

    /// <summary>The agency's transponder models (a borrowed manifest's transponder is another model of the class needed).</summary>
    public IReadOnlyList<TransponderModel> Transponders;

    /// <summary>The agency numbers handed out today, which every fresh number joins.</summary>
    public ISet<string> TakenToday;

    /// <summary>The status the traveller's papers pose as: the kind they were drawn from (AccountMaker.StatusOf), for a debtor posing as a tourist (L4); null otherwise.</summary>
    public CitizenStatus? PosedStatus;

    /// <summary>The employers of the worksite's era, by printed name (the account's among them): a forged contract names another (L5).</summary>
    public IReadOnlyList<string> Employers;

    /// <summary>Today's other open places' labels (never the claim, never a closed one, which would add a directive fault): a forged contract swaps the worksite for one (L5).</summary>
    public IReadOnlyList<string> OpenPlaces;

    /// <summary>The waiver numbers' prefix (agency.accounts.waiverPrefix, "SW"): a fake waiver's number is a fresh one (L3).</summary>
    public string WaiverPrefix;

    /// <summary>The agency's proofs of means (agency.proofs): a forged policy is a fresh number with its proof's prefix (L10).</summary>
    public IReadOnlyList<ProofOfMeans> Proofs;
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
/// variant's fixed order, each maker drawing what it needs; a category
/// forged on two papers is made once and printed on both (a forger's papers
/// agree with each other), and a variant's optional field is forged only
/// when its form is carried. Pure, so every maker and the draw order are
/// tested headless.
/// </summary>
public static class RecordLies
{
    /// <summary>The Leisure Departure Visa's form number: its class, Citizen ID and birth date are what the tourists' lies forge.</summary>
    public const string Visa = Directives.Visa;

    /// <summary>The Departure Manifest's form number: its Citizen ID, transponder and class are what a borrowed manifest carries.</summary>
    public const string Manifest = Directives.Manifest;

    /// <summary>The Debt Relief Labour Contract's form number: its employer, worksite, term and wage are what a forged contract rewrites.</summary>
    public const string Contract = Directives.Contract;

    /// <summary>The Stranding Waiver's form number: its number and transponder are what a fake waiver rewrites, and its debt what a debtor posing as poor shrinks.</summary>
    public const string Waiver = Directives.Waiver;

    /// <summary>The Holiday Credit Agreement's form number (a proof of means: its credit line; its account class).</summary>
    public const string CreditAgreement = "TC-415";

    /// <summary>The Proof of Funds' form number (a proof of means: the savings held; its account class).</summary>
    public const string ProofOfFunds = "TC-416";

    /// <summary>The Travel Insurance Certificate's form number (a proof of means: the policy number).</summary>
    public const string Insurance = "TC-417";

    /// <summary>One forged field: the form it is on and its category.</summary>
    private readonly struct Forged
    {
        /// <summary>The form's number.</summary>
        public readonly string Form;

        /// <summary>The forged category.</summary>
        public readonly ClueCategory Category;

        /// <summary>True when the variant shows without it: forged only when its form is carried and prints it.</summary>
        public readonly bool Optional;

        /// <summary>Creates a named field.</summary>
        public Forged(string form, ClueCategory category, bool optional = false)
        {
            Form = form;
            Category = category;
            Optional = optional;
        }
    }

    /// <summary>Poor posing as rich, forged (L1): the visa's class and the manifest's class read Premium; the manifest only when it is carried (lesson D7: on day 1 the visa travels alone, and its class is the lie).</summary>
    private static readonly Forged[] RichForged =
    {
        new Forged(Visa, ClueCategory.AccountStatus),
        new Forged(Manifest, ClueCategory.TransponderClass, optional: true)
    };

    /// <summary>The visa's class and the manifest's class, both required: a debtor's rich papers (DebtorAsRich).</summary>
    private static readonly Forged[] RichClasses =
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

    /// <summary>A debtor posing as a tourist (L4), with rich papers: the visa's class and the manifest's class read the posed status's (Premium); the same fields as the forged rich set.</summary>
    private static readonly Forged[] DebtorAsRich = RichClasses;

    /// <summary>
    /// A debtor posing as a tourist (L4), with poor papers: the visa's class
    /// reads the posed status (Standard) and the waiver's debt a sliver of the
    /// real one; the carried proof of means, which the account does not hold
    /// (AccountRequest.ForgedProof), prints the posed class too where it has
    /// one (the credit agreement and the proof of funds).
    /// </summary>
    private static readonly Forged[] DebtorAsPoor =
    {
        new Forged(Visa, ClueCategory.AccountStatus),
        new Forged(Waiver, ClueCategory.Debt),
        new Forged(CreditAgreement, ClueCategory.AccountStatus, optional: true),
        new Forged(ProofOfFunds, ClueCategory.AccountStatus, optional: true)
    };

    /// <summary>A forged contract (L5), the wage: the contract's day wage is 1.5 to 3 times the registered one.</summary>
    private static readonly Forged[] ContractWage = { new Forged(Contract, ClueCategory.Wage) };

    /// <summary>A forged contract (L5), the term: the contract's term is a quarter to six tenths of the registered one.</summary>
    private static readonly Forged[] ContractTerm = { new Forged(Contract, ClueCategory.Term) };

    /// <summary>A forged contract (L5), the employer: another employer of the era.</summary>
    private static readonly Forged[] ContractEmployer = { new Forged(Contract, ClueCategory.Employer) };

    /// <summary>A forged contract (L5), the worksite: another place open today.</summary>
    private static readonly Forged[] ContractWorksite = { new Forged(Contract, ClueCategory.Destination) };

    /// <summary>A fake waiver (L3), the number: a waiver number the account never registered.</summary>
    private static readonly Forged[] WaiverNumber = { new Forged(Waiver, ClueCategory.WaiverNo) };

    /// <summary>A fake waiver (L3), the transponder: made out for another unit of the account's class (so it also contradicts the manifest).</summary>
    private static readonly Forged[] WaiverTransponder = { new Forged(Waiver, ClueCategory.TransponderId) };

    /// <summary>A forged proof of means (L10), a credit line: the agreement's line is 3 to 10 times the one on file.</summary>
    private static readonly Forged[] ProofCredit = { new Forged(CreditAgreement, ClueCategory.Credit) };

    /// <summary>A forged proof of means (L10), savings: the funds held are 3 to 10 times those on file.</summary>
    private static readonly Forged[] ProofFunds = { new Forged(ProofOfFunds, ClueCategory.Funds) };

    /// <summary>A forged proof of means (L10), a policy: a policy number the account does not hold.</summary>
    private static readonly Forged[] ProofPolicy = { new Forged(Insurance, ClueCategory.PolicyNo) };

    /// <summary>Each record lie's variants, in draw order (the variant draw indexes the ones that can show).</summary>
    private static IReadOnlyList<Forged[]> VariantsOf(LieKind kind)
    {
        switch (kind)
        {
            case LieKind.PoorPosingAsRich:
                return new[] { RichForged, RichBorrowed };
            case LieKind.DoctoredIdentity:
                return new[] { DoctoredId, DoctoredYear };
            case LieKind.DebtorPosingAsTourist:
                return new[] { DebtorAsRich, DebtorAsPoor };
            case LieKind.ForgedContract:
                return new[] { ContractWage, ContractTerm, ContractEmployer, ContractWorksite };
            case LieKind.FakeWaiver:
                return new[] { WaiverNumber, WaiverTransponder };
            case LieKind.ForgedProof:
                return new[] { ProofCredit, ProofFunds, ProofPolicy };
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
    /// forged value in the variant's fixed order, drawing what the makers
    /// need from <paramref name="context"/>. NoPossibleLie, with no draw,
    /// when no variant can show; Honest, with no draw, for a null
    /// <paramref name="rng"/>, a null account or context, or a place lie.
    /// </summary>
    public static LiePlan Plan(LieKind kind, IReadOnlyList<RecordForm> forms, CitizenAccount account, RecordLieContext context, IRandomSource rng)
    {
        if (rng == null || account == null || context == null || !LieKinds.IsRecordLie(kind))
            return LiePlan.Without(LieOutcome.Honest, kind);

        forms = forms ?? new RecordForm[0];
        var showable = new List<Forged[]>();
        foreach (Forged[] variant in VariantsOf(kind))
            if (variant.All(f => f.Optional || Forgeable(kind, f, forms, account, context)))
                showable.Add(variant);

        if (showable.Count == 0)
            return LiePlan.Without(LieOutcome.NoPossibleLie, kind);

        Forged[] chosen = showable.Count == 1 ? showable[0] : showable[rng.Range(0, showable.Count)];
        var tells = new List<RecordTell>(chosen.Length);
        var made = new Dictionary<ClueCategory, string>();
        foreach (Forged field in chosen)
        {
            if (field.Optional && !Forgeable(kind, field, forms, account, context))
                continue;
            if (!made.TryGetValue(field.Category, out string value))
                made[field.Category] = value = FalseValue(kind, field.Category, account, context, rng);
            for (int d = 0; d < forms.Count; d++)
                if (forms[d].FormNumber == field.Form)
                    tells.Add(new RecordTell(d, field.Category, value));
        }

        return new LiePlan(LieOutcome.Forger, kind, tells.AsReadOnly());
    }

    /// <summary>True when <paramref name="field"/> is printed on the forms and its maker can make a false value for the account (draws nothing).</summary>
    private static bool Forgeable(LieKind kind, Forged field, IReadOnlyList<RecordForm> forms, CitizenAccount account, RecordLieContext context) =>
        Prints(forms, field) && CanForge(kind, field.Category, account, context);

    /// <summary>True when one of the forms is <paramref name="field"/>'s form and prints its category.</summary>
    private static bool Prints(IReadOnlyList<RecordForm> forms, Forged field) =>
        forms.Any(f => f.FormNumber == field.Form && f.Fields != null && f.Fields.Any(x => x != null && x.category == field.Category));

    /// <summary>The class of transponder the posed status travels on; null without a posed status.</summary>
    private static TransponderClass? PosedClass(RecordLieContext context) =>
        context.PosedStatus.HasValue ? AccountMaker.ClassOf(context.PosedStatus.Value) : (TransponderClass?)null;

    /// <summary>The class of the transponder a forged manifest or waiver names: a fake waiver's is the account's own (another unit of it); poor posing as rich names a Premium one (the false class); otherwise the account's.</summary>
    private static TransponderClass ForgedUnitClass(LieKind kind, CitizenAccount account) =>
        kind == LieKind.FakeWaiver ? account.TransponderClass : FalseTransponderClass(account.TransponderClass) ?? account.TransponderClass;

    /// <summary>True when the account holds the proof of <paramref name="category"/> on file (a forged proof, L4's, is not on file).</summary>
    private static bool HoldsProofOf(CitizenAccount account, ClueCategory category) =>
        account.ProofForm != null && !account.ProofForged && account.ProofCategory == category;

    /// <summary>True when the category's maker can make a false value for this account (draws nothing); a debtor's classes are the posed status's, which must differ from the account's.</summary>
    private static bool CanForge(LieKind kind, ClueCategory category, CitizenAccount account, RecordLieContext context)
    {
        bool posed = kind == LieKind.DebtorPosingAsTourist;
        switch (category)
        {
            case ClueCategory.AccountStatus:
                return posed ? context.PosedStatus.HasValue && context.PosedStatus.Value != account.Status : HigherStatuses(account.Status).Count > 0;
            case ClueCategory.TransponderClass:
                return posed ? PosedClass(context).HasValue && PosedClass(context).Value != account.TransponderClass : FalseTransponderClass(account.TransponderClass) != null;
            case ClueCategory.TransponderId:
                return ModelsOf(ForgedUnitClass(kind, account), context.Transponders).Count > 0;
            case ClueCategory.WaiverNo:
                return account.WaiverNo != null && !string.IsNullOrWhiteSpace(context.WaiverPrefix);
            case ClueCategory.Debt:
                return account.Debt > 0;
            case ClueCategory.Credit:
            case ClueCategory.Funds:
                return HoldsProofOf(account, category) && account.ProofAmount > 0;
            case ClueCategory.PolicyNo:
                return HoldsProofOf(account, category) && !string.IsNullOrWhiteSpace(PolicyPrefix(context));
            case ClueCategory.CitizenId:
                return true;
            case ClueCategory.BirthDate:
                return BirthDates.HasOtherYear(context.CoverBirthDate, context.BirthYearMin, context.BirthYearMax);
            case ClueCategory.Wage:
                return account.Wage > 0;
            case ClueCategory.Term:
                return account.TermDays > 0;
            case ClueCategory.Employer:
                return account.HasContract && Others(context.Employers, account.Employer).Count > 0;
            case ClueCategory.Destination:
                return account.HasContract && (context.OpenPlaces?.Count ?? 0) > 0;
            default:
                return false;
        }
    }

    /// <summary>The category's false value (the makers below); the transponder is one of the class the forged papers need (the false class when the class is forged); a debtor's classes are the posed status's (no draw).</summary>
    private static string FalseValue(LieKind kind, ClueCategory category, CitizenAccount account, RecordLieContext context, IRandomSource rng)
    {
        bool posed = kind == LieKind.DebtorPosingAsTourist;
        switch (category)
        {
            case ClueCategory.AccountStatus:
                return posed ? context.PosedStatus.Value.ToString() : FalseStatus(account.Status, rng).ToString();
            case ClueCategory.TransponderClass:
                return posed ? PosedClass(context).Value.ToString() : FalseTransponderClass(account.TransponderClass).ToString();
            case ClueCategory.TransponderId:
                return FalseTransponder(ForgedUnitClass(kind, account), account.Transponder, context.Transponders, context.TakenToday, rng);
            case ClueCategory.WaiverNo:
                return AgencyNumbers.TakeUnique(context.TakenToday, () => AccountMaker.Numbered(context.WaiverPrefix, rng));
            case ClueCategory.Debt:
                return AccountMaker.Credits(DebtSliver(account.Debt, rng));
            case ClueCategory.Credit:
            case ClueCategory.Funds:
                return AccountMaker.Credits(FalseMeans(account.ProofAmount, rng));
            case ClueCategory.PolicyNo:
                return AgencyNumbers.TakeUnique(context.TakenToday, () => AccountMaker.Numbered(PolicyPrefix(context), rng));
            case ClueCategory.CitizenId:
                return FreshCitizenId(context.TakenToday, rng);
            case ClueCategory.Wage:
                return AccountMaker.Credits(FalseWage(account.Wage, rng));
            case ClueCategory.Term:
                return AccountMaker.Term(FalseTerm(account.TermDays, rng));
            case ClueCategory.Employer:
                return OtherOf(context.Employers, account.Employer, rng);
            case ClueCategory.Destination:
                return context.OpenPlaces[rng.Range(0, context.OpenPlaces.Count)];
            default:
                return BirthDates.PickOtherYear(context.CoverBirthDate, context.BirthYearMin, context.BirthYearMax, rng);
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
    /// the class. Also the paper-set maker's Economy unit for a Premium
    /// citizen (Directives, PaperSetBreak.EconomyManifest), whose own model is
    /// Premium, so every Economy model qualifies.
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

    /// <summary>The id of the model a printed transponder ("Driftbox 3 · DB-00412") is of, among <paramref name="models"/>; null when none is (a placeholder, no manifest).</summary>
    public static string ModelIdOf(string transponder, IReadOnlyList<TransponderModel> models) =>
        (models ?? Array.Empty<TransponderModel>()).FirstOrDefault(m => m != null && IsModelOf(transponder, m))?.id;

    /// <summary>True when a printed transponder ("Hopper Mk II · HP-40718") is of <paramref name="model"/>.</summary>
    private static bool IsModelOf(string transponder, TransponderModel model) =>
        !string.IsNullOrEmpty(transponder) && !string.IsNullOrEmpty(model.model) &&
        transponder.StartsWith(model.model + " ", StringComparison.Ordinal);

    /// <summary>A Citizen ID nobody holds today (AgencyNumbers.TakeUnique over AccountMaker.CitizenId: three draws per attempt), which joins <paramref name="takenToday"/>.</summary>
    public static string FreshCitizenId(ISet<string> takenToday, IRandomSource rng) =>
        AgencyNumbers.TakeUnique(takenToday, () => AccountMaker.CitizenId(rng));

    /// <summary>
    /// A forged day wage (§6.3): the registered <paramref name="wage"/> times
    /// 1.5 to 3 (one Value draw), rounded to 10 cr; one step up when the
    /// rounding lands on the registered wage, so it always differs.
    /// </summary>
    public static int FalseWage(int wage, IRandomSource rng)
    {
        int forged = RoundTo(wage * (1.5 + 1.5 * rng.Value()), 10);
        return forged == wage ? forged + 10 : forged;
    }

    /// <summary>
    /// A forged term (§6.3): the registered <paramref name="days"/> times
    /// 0.25 to 0.6 (one Value draw), rounded to whole months
    /// (AccountMaker.MonthDays) and at least one; one month down when the
    /// rounding lands on the registered term (up, when that would be none).
    /// </summary>
    public static int FalseTerm(int days, IRandomSource rng)
    {
        int forged = Math.Max(AccountMaker.MonthDays, RoundTo(days * (0.25 + 0.35 * rng.Value()), AccountMaker.MonthDays));
        if (forged != days)
            return forged;
        return forged > AccountMaker.MonthDays ? forged - AccountMaker.MonthDays : forged + AccountMaker.MonthDays;
    }

    /// <summary>
    /// A forged proof's amount (§6.1, L10): the credit line or the savings on
    /// file times 3 to 10 (one Value draw), rounded to 100 cr; always above
    /// the amount on file.
    /// </summary>
    public static int FalseMeans(int amount, IRandomSource rng)
    {
        int forged = RoundTo(amount * (3.0 + 7.0 * rng.Value()), 100);
        return forged > amount ? forged : amount + 100;
    }

    /// <summary>
    /// The debt a debtor posing as poor writes on their waiver (§6.1, L4): a
    /// sliver of the real <paramref name="debt"/>, a twentieth to a fifth of
    /// it (one Value draw), rounded to 100 cr and at least 100 cr; 100 cr less
    /// when the rounding lands on the real debt.
    /// </summary>
    public static int DebtSliver(int debt, IRandomSource rng)
    {
        int sliver = Math.Max(100, RoundTo(debt * (0.05 + 0.15 * rng.Value()), 100));
        return sliver != debt ? sliver : Math.Max(0, sliver - 100);
    }

    /// <summary>The prefix of the policy proof (the proof of means whose category is PolicyNo), or null.</summary>
    private static string PolicyPrefix(RecordLieContext context) =>
        (context.Proofs ?? new ProofOfMeans[0]).FirstOrDefault(p => p != null && p.category == ClueCategory.PolicyNo)?.prefix;

    /// <summary><paramref name="value"/> rounded to the nearest multiple of <paramref name="step"/> (halves away from zero).</summary>
    private static int RoundTo(double value, int step) => (int)Math.Round(value / step, MidpointRounding.AwayFromZero) * step;

    /// <summary>The entries of <paramref name="values"/> other than <paramref name="own"/> (Values.Match), in order.</summary>
    private static List<string> Others(IReadOnlyList<string> values, string own) =>
        (values ?? new string[0]).Where(v => !string.IsNullOrWhiteSpace(v) && !Values.Match(v, own)).ToList();

    /// <summary>Another of <paramref name="values"/> than <paramref name="own"/>: one Range draw over the others; null, with no draw, without one.</summary>
    public static string OtherOf(IReadOnlyList<string> values, string own, IRandomSource rng)
    {
        List<string> others = Others(values, own);
        return others.Count == 0 ? null : others[rng.Range(0, others.Count)];
    }
}
