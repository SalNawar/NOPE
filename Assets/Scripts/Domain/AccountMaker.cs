using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>
/// A 2150 citizen's account status (traveller types §4.1): what their visa
/// class must read. Serialized in the content library's account ranges:
/// append only (SerializedEnumsTests pins every value).
/// </summary>
public enum CitizenStatus
{
    /// <summary>A citizen with money: a rich tourist, on a Premium transponder.</summary>
    Premium,

    /// <summary>A citizen travelling on credit, savings or insurance: a poor tourist.</summary>
    Standard,

    /// <summary>A citizen in debt, eligible for Debt Relief: a labourer.</summary>
    Eligible
}

/// <summary>
/// A transponder's class (traveller types F3): Premium units are reliable,
/// Economy units can fail (phase 13's strandings). Serialized in the content
/// library's transponder models: append only (SerializedEnumsTests).
/// </summary>
public enum TransponderClass
{
    /// <summary>A reliable unit (rich tourists).</summary>
    Premium,

    /// <summary>A cheap unit that can fail (poor tourists and labourers).</summary>
    Economy
}

/// <summary>
/// A Citizen Account's standing (traveller types §4.1): Good, or Frozen for
/// a debtor in default, who may not depart (TravelRuleType.DebtStanding).
/// Runtime only (not serialized).
/// </summary>
public enum AccountStanding
{
    /// <summary>In good standing.</summary>
    Good,

    /// <summary>Frozen: in default since a date ("Frozen: default, 2 Mar 2150"); a directive fault.</summary>
    Frozen
}

/// <summary>One employer of the Debt Relief programme (world_source.json agency.employers; traveller types §4.3): a labourer's contract names one of their worksite's era.</summary>
[Serializable]
public sealed class Employer
{
    /// <summary>The employer's id ("tyburn"), unique.</summary>
    public string id;

    /// <summary>The era it hires for (an era id, "industrial").</summary>
    public string era;

    /// <summary>Its printed name ("Tyburn Mills Consortium").</summary>
    public string name;
}

/// <summary>The ranges a labourer's registered contract is drawn from (world_source.json agency.accounts.contract; traveller types §4.1: a term of 90-720 days, a day wage of 180-520 cr).</summary>
[Serializable]
public sealed class ContractRanges
{
    /// <summary>The longest term a contract may hold, so the widest term fits its box on a form (FieldLengths.Longest; "99999 days").</summary>
    public const int MaxTermDays = 99_999;

    /// <summary>The shortest term, in days (a whole number of 30-day months).</summary>
    public int termMin;

    /// <summary>The longest term, in days.</summary>
    public int termMax;

    /// <summary>The lowest day wage, in credits.</summary>
    public int wageMin;

    /// <summary>The highest day wage, in credits.</summary>
    public int wageMax;
}

/// <summary>One transponder model a citizen can travel on (world_source.json agency.transponders; a weighted list per class).</summary>
[Serializable]
public sealed class TransponderModel
{
    /// <summary>The model's id ("hopper2"), unique.</summary>
    public string id;

    /// <summary>The model's class.</summary>
    public TransponderClass transponderClass;

    /// <summary>The model's printed name ("Hopper Mk II").</summary>
    public string model;

    /// <summary>The serial's prefix ("HP" gives "HP-40718").</summary>
    public string prefix;

    /// <summary>Relative weight among the models of its class (positive).</summary>
    public float weight = 1f;
}

/// <summary>
/// One proof of means a poor tourist may hold (world_source.json
/// agency.proofs; traveller types F2, §4.1, §4.3): the form that prints it,
/// the record category it is compared under (Credit, Funds or PolicyNo), its
/// weight in the draw, and its value's range (an amount of credits) or its
/// number's prefix ("TI" gives "TI-551902").
/// </summary>
[Serializable]
public sealed class ProofOfMeans
{
    /// <summary>The form that prints the proof ("TC-415"), unique among the proofs; the account's ProofForm names it.</summary>
    public string form;

    /// <summary>The record category the proof is compared under: Credit or Funds (an amount) or PolicyNo (a number).</summary>
    public ClueCategory category;

    /// <summary>Relative weight among the proofs (positive).</summary>
    public float weight = 1f;

    /// <summary>The least amount, in credits (an amount proof).</summary>
    public int amountMin;

    /// <summary>The most amount, in credits (an amount proof).</summary>
    public int amountMax;

    /// <summary>The number's prefix (a number proof); blank for an amount.</summary>
    public string prefix = string.Empty;
}

/// <summary>The ranges one account status is drawn from (world_source.json agency.accounts.statuses; traveller types §4.1).</summary>
[Serializable]
public sealed class StatusRanges
{
    /// <summary>The status these ranges are for.</summary>
    public CitizenStatus status;

    /// <summary>The least debt, in credits (0 for Premium and Standard).</summary>
    public int debtMin;

    /// <summary>The most debt, in credits (18,000 for Standard; 320,000 for Eligible).</summary>
    public int debtMax;

    /// <summary>The fewest past trips on the account's Travel history.</summary>
    public int tripsMin;

    /// <summary>The most past trips on the account's Travel history.</summary>
    public int tripsMax;
}

/// <summary>
/// The ranges every Citizen Account is drawn from (world_source.json
/// agency.accounts; traveller types §4.3), written by Generate World: the
/// Valid Until of an honest paper, how far back past trips go, and each
/// status's debt and trip counts.
/// </summary>
[Serializable]
public sealed class AccountRanges
{
    /// <summary>The most debt an account may hold, so the widest debt fits its box on a form (FieldLengths.Longest; "9,999,999 cr").</summary>
    public const int MaxDebt = 9_999_999;

    /// <summary>The fewest days after today an honest paper is valid (3).</summary>
    public int validDaysMin;

    /// <summary>The most days after today an honest paper is valid (365).</summary>
    public int validDaysMax;

    /// <summary>A past trip left 1 to this many days before today.</summary>
    public int tripsWithinDays;

    /// <summary>The Stranding Waiver number's prefix ("SW" gives "SW-204817"; F3): every account on an Economy transponder registers one.</summary>
    public string waiverPrefix = string.Empty;
    /// <summary>A Frozen account went into default 1 to this many days before today (the debt-standing maker, CaseFactory.PlanViolation).</summary>
    public int frozenWithinDays;

    /// <summary>Each status's ranges (one entry per status).</summary>
    public List<StatusRanges> statuses = new();

    /// <summary>The ranges a labourer's registered contract is drawn from (agency.accounts.contract).</summary>
    public ContractRanges contract = new ContractRanges();

    /// <summary>The ranges of <paramref name="status"/>, or null when none are authored.</summary>
    public StatusRanges For(CitizenStatus status) => statuses?.FirstOrDefault(s => s != null && s.status == status);

    /// <summary>
    /// What Generate World and the validator refuse: Valid Until and trip
    /// windows AgencyNumbers cannot draw from; a status with no ranges, or
    /// with a debt or trip range out of order or a debt above
    /// <see cref="MaxDebt"/>; and, over <paramref name="transponders"/>, a
    /// model id used twice, a blank model or prefix, a printed name wider than
    /// a book row (FactTable.MaxValueLength, a form's box), a weight of 0 or
    /// less, and a class some status travels on with no model; a blank waiver
    /// prefix or one whose number is wider than a book row; and, over
    /// <paramref name="proofs"/> (phase 8), no proof at all (a Standard account
    /// holds one), a form listed twice or blank, a category that is not a
    /// proof's (AccountMaker.IsProofCategory), a weight of 0 or less, an
    /// amount range out of order or above <see cref="MaxDebt"/>, and a number
    /// proof with a blank prefix or one wider than a book row; and (phase 9)
    /// a freeze window below 1 day, a contract range out of order, a term
    /// below one month (AccountMaker.MonthDays) or above the widest a form
    /// prints, or a day wage below 1 cr or above the widest. Empty when sound.
    /// </summary>
    public List<string> Problems(IReadOnlyList<TransponderModel> transponders, IReadOnlyList<ProofOfMeans> proofs)
    {
        var problems = new List<string>();
        if (validDaysMin < 0 || validDaysMin > validDaysMax)
            problems.Add($"agency.accounts.validDaysMin {validDaysMin} and validDaysMax {validDaysMax}: an honest paper is valid from 0 <= validDaysMin <= validDaysMax days after today.");
        if (tripsWithinDays < 1)
            problems.Add($"agency.accounts.tripsWithinDays is {tripsWithinDays}: a past trip left at least 1 day before today.");
        if (string.IsNullOrWhiteSpace(waiverPrefix))
            problems.Add("agency.accounts.waiverPrefix is blank: the Stranding Waiver number's prefix (\"SW\").");
        else if (AccountMaker.Numbered(waiverPrefix, new TopDraws()).Length > FactTable.MaxValueLength)
            problems.Add($"agency.accounts.waiverPrefix '{waiverPrefix}': a waiver number is wider than the {FactTable.MaxValueLength} characters a form's box holds.");
        if (frozenWithinDays < 1)
            problems.Add($"agency.accounts.frozenWithinDays is {frozenWithinDays}: a frozen account went into default at least 1 day before today.");
        if (contract == null)
            problems.Add("agency.accounts.contract is missing: the term and day wage ranges a labourer's contract is drawn from.");
        else
        {
            if (contract.termMin < AccountMaker.MonthDays || contract.termMin > contract.termMax)
                problems.Add($"agency.accounts.contract: the term range {contract.termMin}-{contract.termMax} must run from at least {AccountMaker.MonthDays} days upwards.");
            if (contract.termMax > ContractRanges.MaxTermDays)
                problems.Add($"agency.accounts.contract: the term {contract.termMax} is above {AccountMaker.Term(ContractRanges.MaxTermDays)}, the widest a form prints.");
            if (contract.wageMin < 1 || contract.wageMin > contract.wageMax)
                problems.Add($"agency.accounts.contract: the day wage range {contract.wageMin}-{contract.wageMax} must run from at least 1 cr upwards.");
            if (contract.wageMax > MaxDebt)
                problems.Add($"agency.accounts.contract: the day wage {contract.wageMax} is above {AccountMaker.Credits(MaxDebt)}, the widest a form prints.");
        }

        foreach (CitizenStatus status in (CitizenStatus[])Enum.GetValues(typeof(CitizenStatus)))
        {
            StatusRanges r = For(status);
            if (r == null)
            {
                problems.Add($"agency.accounts.statuses has no ranges for {status} accounts.");
                continue;
            }
            if (r.debtMin < 0 || r.debtMin > r.debtMax)
                problems.Add($"agency.accounts.statuses {status}: the debt range {r.debtMin}-{r.debtMax} must run from 0 or more upwards.");
            if (r.debtMax > MaxDebt)
                problems.Add($"agency.accounts.statuses {status}: the debt {r.debtMax} is above {AccountMaker.Credits(MaxDebt)}, the widest a form prints.");
            if (r.tripsMin < 0 || r.tripsMin > r.tripsMax)
                problems.Add($"agency.accounts.statuses {status}: the trips range {r.tripsMin}-{r.tripsMax} must run from 0 or more upwards.");
        }

        var ids = new HashSet<string>();
        foreach (TransponderModel t in transponders ?? Array.Empty<TransponderModel>())
        {
            if (t == null)
                continue;
            if (!ids.Add(t.id ?? string.Empty))
                problems.Add($"agency.transponders: the model id '{t.id}' is listed twice.");
            if (string.IsNullOrWhiteSpace(t.model))
                problems.Add($"agency.transponders '{t.id}': the model name is blank.");
            if (string.IsNullOrWhiteSpace(t.prefix))
                problems.Add($"agency.transponders '{t.id}': the serial prefix is blank.");
            string widest = AccountMaker.TransponderName(t.model, AccountMaker.Serial(t.prefix, new TopDraws()));
            if (widest.Length > FactTable.MaxValueLength)
                problems.Add($"agency.transponders '{t.id}': '{widest}' is {widest.Length} characters; a form's box and a book row hold {FactTable.MaxValueLength}.");
            if (t.weight <= 0f)
                problems.Add($"agency.transponders '{t.id}': the weight {t.weight} must be positive.");
        }

        foreach (TransponderClass needed in ((CitizenStatus[])Enum.GetValues(typeof(CitizenStatus))).Select(AccountMaker.ClassOf).Distinct())
            if (transponders == null || !transponders.Any(t => t != null && t.transponderClass == needed && t.weight > 0f))
                problems.Add($"agency.transponders has no {needed} model, but some accounts travel on one.");

        var forms = new HashSet<string>();
        List<ProofOfMeans> listed = (proofs ?? Array.Empty<ProofOfMeans>()).Where(p => p != null).ToList();
        if (listed.Count == 0)
            problems.Add("agency.proofs is empty: a Standard account holds one proof of means (a credit line, savings or a policy).");
        foreach (ProofOfMeans p in listed)
        {
            if (string.IsNullOrWhiteSpace(p.form))
                problems.Add($"agency.proofs: a {p.category} proof names no form.");
            else if (!forms.Add(p.form))
                problems.Add($"agency.proofs: the form '{p.form}' is listed twice.");
            if (!AccountMaker.IsProofCategory(p.category))
                problems.Add($"agency.proofs '{p.form}': {p.category} is not a proof of means (Credit, Funds or PolicyNo).");
            if (p.weight <= 0f)
                problems.Add($"agency.proofs '{p.form}': the weight {p.weight} must be positive.");
            if (AccountMaker.IsAmount(p.category))
            {
                if (p.amountMin < 0 || p.amountMin > p.amountMax)
                    problems.Add($"agency.proofs '{p.form}': the amount range {p.amountMin}-{p.amountMax} must run from 0 or more upwards.");
                if (p.amountMax > MaxDebt)
                    problems.Add($"agency.proofs '{p.form}': the amount {p.amountMax} is above {AccountMaker.Credits(MaxDebt)}, the widest a form prints.");
            }
            else if (string.IsNullOrWhiteSpace(p.prefix))
            {
                problems.Add($"agency.proofs '{p.form}': a {p.category} proof needs its number's prefix (\"TI\").");
            }
            else if (AccountMaker.Numbered(p.prefix, new TopDraws()).Length > FactTable.MaxValueLength)
            {
                problems.Add($"agency.proofs '{p.form}': a number with the prefix '{p.prefix}' is wider than the {FactTable.MaxValueLength} characters a form's box holds.");
            }
        }

        return problems;
    }
}

/// <summary>A source that draws the top of every range: a maker's widest value.</summary>
internal sealed class TopDraws : IRandomSource
{
    /// <inheritdoc />
    public int Range(int minInclusive, int maxExclusive) => maxExclusive - 1;

    /// <inheritdoc />
    public float Value() => 0.999f;
}

/// <summary>One past trip on an account's Travel history ("12 Aug 2149, Periclean Athens (Ancient), returned").</summary>
public sealed class PastTrip
{
    /// <summary>A trip on <paramref name="date"/> to <paramref name="place"/>.</summary>
    public PastTrip(string date, string place)
    {
        Date = date;
        Place = place;
    }

    /// <summary>The day it left, as the agency calendar writes dates.</summary>
    public string Date { get; }

    /// <summary>The place visited, by its label.</summary>
    public string Place { get; }
}

/// <summary>What the account maker needs to know about one citizen (explicit inputs, audit R3-025).</summary>
public sealed class AccountRequest
{
    /// <summary>The account's status (AccountMaker.StatusOf the traveller's kind).</summary>
    public CitizenStatus Status;

    /// <summary>The lineages to draw from: the past places of the family's country (the country whose Future list gave the name).</summary>
    public IReadOnlyList<string> Lineages;

    /// <summary>The places a past trip may have visited: every past place, by label.</summary>
    public IReadOnlyList<string> TripPlaces;

    /// <summary>The forms of the traveller's blueprint, in paper order: the account decides which of them the traveller carries (AccountMaker.Carries), and one honest Valid Until is drawn for each carried form that expires, in form order.</summary>
    public IReadOnlyList<FormEntry> Forms;

    /// <summary>True for a labourer: a registered Debt Relief Labour Contract is drawn (an employer, a term and a day wage).</summary>
    public bool Contract;

    /// <summary>The employers the contract may name: those of the worksite's era, by printed name (agency.employers).</summary>
    public IReadOnlyList<string> Employers;

    /// <summary>
    /// True for a debtor posing as a poor tourist (L4's poor variant, phase
    /// 9): a proof of means is drawn where a Standard account's would be, and
    /// the traveller carries it, but the account does not hold it
    /// (CitizenAccount.ProofForged: the record shows none on file).
    /// </summary>
    public bool ForgedProof;

    /// <summary>A story character's authored Citizen ID (days 7-15 B3; blank: drawn): reserved in the day's numbers before slot 1 and taken as it is, never redrawn, so it is the same at every appearance.</summary>
    public string CitizenId;

    /// <summary>A story character's authored debt in cr (0: drawn); the draw is still made, so the account's later draws keep their order.</summary>
    public int Debt;

    /// <summary>A labourer story character's authored employer, by printed name (blank: drawn); the employer draw is still made.</summary>
    public string Employer;
}

/// <summary>One form of a blueprint as the account maker sees it (phase 8): its number, its request group and whether it prints a Valid Until.</summary>
public readonly struct FormEntry
{
    /// <summary>The agency form number ("TC-415").</summary>
    public readonly string FormNumber;

    /// <summary>The request group it belongs to (DocumentTemplateSO.askGroup; AccountMaker.ProofGroup for a proof of means), or blank.</summary>
    public readonly string AskGroup;

    /// <summary>True when the form prints a Valid Until (an Expiry field).</summary>
    public readonly bool Expires;

    /// <summary>Creates an entry.</summary>
    public FormEntry(string formNumber, string askGroup, bool expires)
    {
        FormNumber = formNumber;
        AskGroup = askGroup;
        Expires = expires;
    }
}

/// <summary>
/// A 2150 citizen's Citizen Account (traveller types R1, §4.1): the truth
/// about them, which their papers must match. Drawn by AccountMaker.Make.
/// </summary>
public sealed class CitizenAccount
{
    /// <summary>The Citizen ID ("418-0937-52"), unique within the day: the category CitizenId.</summary>
    public string CitizenId;

    /// <summary>The account's status (the category AccountStatus prints its name).</summary>
    public CitizenStatus Status;

    /// <summary>What the citizen owes, in credits (the category Debt prints AccountMaker.Credits).</summary>
    public int Debt;

    /// <summary>The transponder on file, "{model} · {serial}" (the category TransponderId); null when no model of the class is authored.</summary>
    public string Transponder;

    /// <summary>The transponder's class (the category TransponderClass prints its name).</summary>
    public TransponderClass TransponderClass;

    /// <summary>The Stranding Waiver registered on the account ("SW-204817", the category WaiverNo): every account on an Economy transponder holds one; null on a Premium one (none on file).</summary>
    public string WaiverNo;

    /// <summary>The form of the proof of means on file ("TC-416"): the one form of the proof group the traveller carries; null when the account holds none (every status but Standard).</summary>
    public string ProofForm;

    /// <summary>The proof's record category (Credit, Funds or PolicyNo); meaningless without a <see cref="ProofForm"/>.</summary>
    public ClueCategory ProofCategory;

    /// <summary>The proof's value as papers and the record print it ("9,400 cr", "TI-551902"); null without a proof.</summary>
    public string ProofValue;

    /// <summary>The proof's amount in credits (a credit line or savings), which a forged proof inflates (L10); 0 for a policy or without a proof.</summary>
    public int ProofAmount;

    /// <summary>True when the proof the traveller carries is not on file (L4's poor variant, AccountRequest.ForgedProof): their paper prints it, the record shows none.</summary>
    public bool ProofForged;

    /// <summary>The account's standing: Good, or Frozen for a debtor in default (the debt-standing maker sets it; TravelRuleType.DebtStanding reads it).</summary>
    public AccountStanding Standing = AccountStanding.Good;

    /// <summary>The date the account was frozen ("2 Mar 2150"; the Standing row prints it); null while Good.</summary>
    public string FrozenSince;

    /// <summary>The registered contract's employer (the category Employer); null without a contract.</summary>
    public string Employer;

    /// <summary>The registered contract's term in days (the category Term prints AccountMaker.Term); 0 without a contract.</summary>
    public int TermDays;

    /// <summary>The registered contract's day wage in credits (the category Wage prints AccountMaker.Credits); 0 without a contract.</summary>
    public int Wage;

    /// <summary>True when a Debt Relief Labour Contract is registered on the account.</summary>
    public bool HasContract => Employer != null;

    /// <summary>The family's lineage: a past place (flavour, traveller types R4); null when none is authored.</summary>
    public string Lineage;

    /// <summary>Past trips, newest first.</summary>
    public IReadOnlyList<PastTrip> Trips = Array.Empty<PastTrip>();

    /// <summary>The honest Valid Until of each of the traveller's expiring forms, in form order.</summary>
    public IReadOnlyList<string> ValidUntil = Array.Empty<string>();

    /// <summary>The booked departure's date: today.</summary>
    public string Departure;
}

/// <summary>
/// The Citizen Account maker (traveller types R2, §4.3): every value is drawn
/// on the traveller's account stream (Seeds.ForAccount) in a fixed order, so
/// tuning a range never changes who travels, and every number is unique
/// within the day (AgencyNumbers.TakeUnique), so a number never belongs to
/// two travellers. Pure, so every step is tested headless.
/// </summary>
public static class AccountMaker
{
    /// <summary>The account status of a 2150 citizen's kind (rich: Premium, poor: Standard, labourer: Eligible); false for the displaced, who have a registry entry instead.</summary>
    public static bool StatusOf(TravellerKind kind, out CitizenStatus status)
    {
        switch (kind)
        {
            case TravellerKind.RichTourist:
                status = CitizenStatus.Premium;
                return true;
            case TravellerKind.PoorTourist:
                status = CitizenStatus.Standard;
                return true;
            case TravellerKind.Labourer:
                status = CitizenStatus.Eligible;
                return true;
            default:
                status = default;
                return false;
        }
    }

    /// <summary>The class of transponder an account of <paramref name="status"/> travels on: Premium for Premium, Economy otherwise (F3).</summary>
    public static TransponderClass ClassOf(CitizenStatus status) =>
        status == CitizenStatus.Premium ? TransponderClass.Premium : TransponderClass.Economy;

    /// <summary>The request group of the proofs of means (DocumentTemplateSO.askGroup, traveller types I2): the three proof forms share it, and a traveller carries the one their account holds.</summary>
    public const string ProofGroup = "proof";

    /// <summary>True for a proof of means' record category: a credit line, savings or a policy number (§4.1's one Forms-on-file row).</summary>
    public static bool IsProofCategory(ClueCategory category) =>
        category == ClueCategory.Credit || category == ClueCategory.Funds || category == ClueCategory.PolicyNo;

    /// <summary>True when a proof of <paramref name="category"/> is an amount of credits (a credit line or savings); a policy is a number.</summary>
    public static bool IsAmount(ClueCategory category) => category == ClueCategory.Credit || category == ClueCategory.Funds;

    /// <summary>True when an account of <paramref name="status"/> holds a proof of means on file: a Standard account (a poor tourist, or poor posing as rich); a Premium citizen pays their own way and an Eligible one departs on Debt Relief.</summary>
    public static bool HoldsProof(CitizenStatus status) => status == CitizenStatus.Standard;

    /// <summary>True when an account of <paramref name="status"/> registers a Stranding Waiver: every account on an Economy transponder (F3).</summary>
    public static bool HoldsWaiver(CitizenStatus status) => ClassOf(status) == TransponderClass.Economy;

    /// <summary>
    /// True when a traveller with <paramref name="account"/> carries a form of
    /// their blueprint: every form outside a request group, and of the proof
    /// group (<see cref="ProofGroup"/>) the one form the account holds
    /// (CitizenAccount.ProofForm); a form of another group, or a proof when
    /// the account holds none, is not carried.
    /// </summary>
    public static bool Carries(string askGroup, string formNumber, CitizenAccount account) =>
        string.IsNullOrEmpty(askGroup) ||
        (askGroup == ProofGroup && account != null && account.ProofForm != null && account.ProofForm == formNumber);

    /// <summary>The days of a contract month: terms are drawn and printed in whole months (30 days).</summary>
    public const int MonthDays = 30;

    /// <summary>
    /// A citizen's account, in the fixed draw order (§4.3): the Citizen ID
    /// (<see cref="CitizenId"/>, redrawn while taken today); the debt
    /// (<see cref="Amount"/>, one draw); for an account that holds a proof of
    /// means (<see cref="HoldsProof"/>), or a debtor posing as a poor tourist
    /// who carries one not on file (AccountRequest.ForgedProof), the proof (one weighted draw over
    /// <paramref name="proofs"/>; none without proofs) and its value (an
    /// amount: <see cref="Amount"/>, one draw; a number: <see cref="Numbered"/>,
    /// redrawn while taken); the transponder model (one weighted
    /// draw among the models of the status's class; none without a model) and
    /// its serial (<see cref="Serial"/>, redrawn while taken); for an account
    /// that registers a waiver (<see cref="HoldsWaiver"/>) its number
    /// (<see cref="Numbered"/> with the waiver prefix, redrawn while taken); the lineage (one
    /// draw; none without lineages); the number of past trips (one draw), then
    /// each trip's day (AgencyNumbers.DaysAgo) and place (one draw each); then
    /// one honest Valid Until per carried form that expires
    /// (<see cref="Carries"/> over the request's forms, in form order;
    /// AgencyNumbers.DaysAhead). The ID, the serial and every number join
    /// <paramref name="takenToday"/>. For a labourer (AccountRequest.Contract)
    /// the registered contract is drawn right after the waiver: its employer
    /// (one draw over the era's employers; none without one), its term
    /// (<see cref="TermDays"/>, one draw) and its day wage (<see cref="Amount"/>,
    /// one draw).
    /// </summary>
    public static CitizenAccount Make(AccountRequest request, AccountRanges ranges, IReadOnlyList<TransponderModel> transponders,
                                      IReadOnlyList<ProofOfMeans> proofs, DateTime today, ISet<string> takenToday, IRandomSource rng)
    {
        ranges = ranges ?? new AccountRanges();
        StatusRanges status = ranges.For(request.Status) ?? new StatusRanges { status = request.Status };
        TransponderClass grade = ClassOf(request.Status);

        var account = new CitizenAccount
        {
            Status = request.Status,
            TransponderClass = grade,
            Departure = AgencyCalendar.Write(today)
        };

        account.CitizenId = !string.IsNullOrWhiteSpace(request.CitizenId) ? request.CitizenId.Trim() : AgencyNumbers.TakeUnique(takenToday, () => CitizenId(rng));
        int debt = Amount(status.debtMin, status.debtMax, rng);
        account.Debt = request.Debt > 0 ? request.Debt : debt;

        if (HoldsProof(request.Status) || request.ForgedProof)
        {
            List<ProofOfMeans> held = (proofs ?? Array.Empty<ProofOfMeans>()).Where(p => p != null && p.weight > 0f).ToList();
            ProofOfMeans proof = WeightedRandom.Pick(held, p => p.weight, rng);
            if (proof != null)
            {
                account.ProofForm = proof.form;
                account.ProofCategory = proof.category;
                account.ProofForged = !HoldsProof(request.Status);
                if (IsAmount(proof.category))
                {
                    account.ProofAmount = Amount(proof.amountMin, proof.amountMax, rng);
                    account.ProofValue = Credits(account.ProofAmount);
                }
                else
                {
                    account.ProofValue = AgencyNumbers.TakeUnique(takenToday, () => Numbered(proof.prefix, rng));
                }
            }
        }

        List<TransponderModel> models = (transponders ?? Array.Empty<TransponderModel>())
            .Where(t => t != null && t.transponderClass == grade)
            .ToList();
        TransponderModel model = WeightedRandom.Pick(models, t => t.weight, rng);
        if (model != null)
            account.Transponder = TransponderName(model.model, AgencyNumbers.TakeUnique(takenToday, () => Serial(model.prefix, rng)));

        if (HoldsWaiver(request.Status))
            account.WaiverNo = AgencyNumbers.TakeUnique(takenToday, () => Numbered(ranges.waiverPrefix, rng));
        IReadOnlyList<string> employers = request.Employers ?? Array.Empty<string>();
        if (request.Contract && employers.Count > 0)
        {
            ContractRanges contract = ranges.contract ?? new ContractRanges();
            string employer = employers[rng.Range(0, employers.Count)];
            account.Employer = !string.IsNullOrWhiteSpace(request.Employer) ? request.Employer : employer;
            account.TermDays = TermDays(contract.termMin, contract.termMax, rng);
            account.Wage = Amount(contract.wageMin, contract.wageMax, rng);
        }

        IReadOnlyList<string> lineages = request.Lineages ?? Array.Empty<string>();
        if (lineages.Count > 0)
            account.Lineage = lineages[rng.Range(0, lineages.Count)];

        int trips = rng.Range(status.tripsMin, status.tripsMax + 1);
        IReadOnlyList<string> places = request.TripPlaces ?? Array.Empty<string>();
        var drawn = new List<(DateTime day, string place)>();
        for (int i = 0; i < trips && places.Count > 0; i++)
        {
            DateTime day = AgencyNumbers.DaysAgo(today, ranges.tripsWithinDays, rng);
            drawn.Add((day, places[rng.Range(0, places.Count)]));
        }
        account.Trips = drawn
            .Select((t, i) => (t.day, t.place, i))
            .OrderByDescending(t => t.day)
            .ThenBy(t => t.i)
            .Select(t => new PastTrip(AgencyCalendar.Write(t.day), t.place))
            .ToArray();

        var validUntil = new List<string>();
        foreach (FormEntry form in request.Forms ?? Array.Empty<FormEntry>())
            if (form.Expires && Carries(form.AskGroup, form.FormNumber, account))
                validUntil.Add(AgencyCalendar.Write(AgencyNumbers.DaysAhead(today, ranges.validDaysMin, ranges.validDaysMax, rng)));
        account.ValidUntil = validUntil;

        return account;
    }

    /// <summary>An agency number with a prefix, "{prefix}-nnnnnn": one draw, 000000-999999 (a Stranding Waiver "SW-204817", a policy "TI-551902").</summary>
    public static string Numbered(string prefix, IRandomSource rng) => $"{prefix}-{rng.Range(0, 1000000):D6}";

    /// <summary>A Citizen ID, "nnn-nnnn-nn": three draws, 000-999, 0000-9999 then 00-99.</summary>
    public static string CitizenId(IRandomSource rng) =>
        $"{rng.Range(0, 1000):D3}-{rng.Range(0, 10000):D4}-{rng.Range(0, 100):D2}";

    /// <summary>A transponder serial, "{prefix}-nnnnn": one draw, 00000-99999.</summary>
    public static string Serial(string prefix, IRandomSource rng) => $"{prefix}-{rng.Range(0, 100000):D5}";

    /// <summary>The transponder as papers and accounts print it: "Hopper Mk II · HP-40718".</summary>
    public static string TransponderName(string model, string serial) => $"{model} · {serial}";

    /// <summary>An amount of credits as papers and accounts print it (F6): "125,430 cr".</summary>
    public static string Credits(int amount) => amount.ToString("N0", CultureInfo.InvariantCulture) + " cr";

    /// <summary>An amount from <paramref name="min"/> to <paramref name="max"/> in whole tens of credits: one draw, even for a fixed amount (so the draw order never depends on the range).</summary>
    public static int Amount(int min, int max, IRandomSource rng) => Steps(min, max, 10, rng);

    /// <summary>A contract term from <paramref name="min"/> to <paramref name="max"/> days in whole months (<see cref="MonthDays"/>): one draw, even for a fixed term.</summary>
    public static int TermDays(int min, int max, IRandomSource rng) => Steps(min, max, MonthDays, rng);

    /// <summary>A value from <paramref name="min"/> up in steps of <paramref name="step"/>, never above <paramref name="max"/>: one Range draw over the steps that fit.</summary>
    private static int Steps(int min, int max, int step, IRandomSource rng)
    {
        if (max < min)
            (min, max) = (max, min);
        int steps = (max - min) / step;
        return min + step * rng.Range(0, steps + 1);
    }

    /// <summary>A contract term as papers and accounts print it (§3.7): "180 days".</summary>
    public static string Term(int days) => days.ToString(CultureInfo.InvariantCulture) + " days";
}

/// <summary>
/// A Citizen Account as record rows (traveller types R1, §4.1): the art's
/// three groups (Records, Forms on file, Travel) and the note, found by the
/// Citizen ID or the name. A row whose category is compared is evidence (a
/// compare pick): the waiver and the proof of means (under the proof's own
/// category) when the account holds them (a forged proof, carried but not
/// on file, shows none), and a labourer's registered
/// contract (three rows: Employer, Term and Wage); the rest (standing, Good
/// or Frozen with its date, lineage, the
/// forms not on file, the departure date, past trips, the note) is shown only. Labels and fixed
/// words come through <c>text</c> (UI string keys), values from the account.
/// The clerk's own account is a record too, with no evidence row.
/// </summary>
public static class AccountRecords
{
    /// <summary>
    /// The record of a citizen named <paramref name="name"/>, born
    /// <paramref name="born"/>, booked to <paramref name="destination"/> today;
    /// its Note row reads <paramref name="note"/> (a story character's, days
    /// 7-15 B3), none when blank.
    /// </summary>
    public static CitizenRecord Record(string name, string born, string destination, CitizenAccount account, Func<string, string> text, string note = null)
    {
        account = account ?? new CitizenAccount();
        string none = text("records.none");

        var records = new List<RecordRow>
        {
            new RecordRow(text("records.row.name"), name, ClueCategory.Name),
            new RecordRow(text("records.row.citizenId"), account.CitizenId, ClueCategory.CitizenId),
            new RecordRow(text("records.row.born"), born, ClueCategory.BirthDate),
            new RecordRow(text("records.row.status"), account.Status.ToString(), ClueCategory.AccountStatus),
            new RecordRow(text("records.row.standing"), account.Standing == AccountStanding.Frozen
                ? string.Format(CultureInfo.InvariantCulture, text("records.standing.frozen"), account.FrozenSince)
                : text("records.standing.good")),
            new RecordRow(text("records.row.debt"), AccountMaker.Credits(account.Debt), ClueCategory.Debt),
            new RecordRow(text("records.row.lineage"), account.Lineage ?? none)
        };

        var forms = new List<RecordRow>
        {
            new RecordRow(text("records.row.transponder"), account.Transponder ?? none, ClueCategory.TransponderId),
            new RecordRow(text("records.row.transponderClass"), account.TransponderClass.ToString(), ClueCategory.TransponderClass),
            account.WaiverNo != null ? new RecordRow(text("records.row.waiver"), account.WaiverNo, ClueCategory.WaiverNo) : new RecordRow(text("records.row.waiver"), none),
            account.ProofForm != null && !account.ProofForged ? new RecordRow(text("records.row.proof"), account.ProofValue, account.ProofCategory) : new RecordRow(text("records.row.proof"), none)
        };
        if (account.HasContract)
        {
            forms.Add(new RecordRow(text("contract.row.employer"), account.Employer, ClueCategory.Employer));
            forms.Add(new RecordRow(text("contract.row.term"), AccountMaker.Term(account.TermDays), ClueCategory.Term));
            forms.Add(new RecordRow(text("contract.row.wage"), AccountMaker.Credits(account.Wage), ClueCategory.Wage));
        }
        else
            forms.Add(new RecordRow(text("records.row.contract"), none));

        var travel = new List<RecordRow>
        {
            new RecordRow(text("records.row.departure"), destination, ClueCategory.Destination),
            new RecordRow(text("records.row.departureDate"), account.Departure)
        };
        IReadOnlyList<PastTrip> trips = account.Trips ?? Array.Empty<PastTrip>();
        if (trips.Count == 0)
            travel.Add(new RecordRow(text("records.row.trips"), none));
        foreach (PastTrip trip in trips)
            travel.Add(new RecordRow(text("records.row.trip"), $"{trip.Date}, {trip.Place}, {text("records.trip.returned")}"));

        return new CitizenRecord(name, account.CitizenId, new[]
        {
            new RecordGroup(text("records.group.account"), records),
            new RecordGroup(text("records.group.forms"), forms),
            new RecordGroup(text("records.group.travel"), travel),
            new RecordGroup(string.Empty, new[] { new RecordRow(text("records.row.note"), string.IsNullOrWhiteSpace(note) ? text("records.note.none") : note) })
        });
    }

    /// <summary>
    /// The clerk's own account as a record (traveller types R1, D1, §4.4):
    /// the rows the Citizen Account app shows (Account.ExtractRows over the
    /// clerk's IClerkAccountSource, one source), grouped as there, found by
    /// the clerk's Citizen ID (773-2840-19) or name. No row carries a
    /// category: the clerk is nobody's case, so nothing on it is a compare
    /// pick. Null when no name is authored.
    /// </summary>
    public static CitizenRecord Clerk(ClerkContent profile, IEnumerable<AccountRow> rows)
    {
        if (profile == null || string.IsNullOrWhiteSpace(profile.name))
            return null;

        var groups = new List<RecordGroup>();
        string title = null;
        var current = new List<RecordRow>();
        foreach (AccountRow row in rows ?? Enumerable.Empty<AccountRow>())
        {
            string group = row.Group ?? string.Empty;
            if (title != null && group != title)
            {
                groups.Add(new RecordGroup(title, current));
                current = new List<RecordRow>();
            }
            title = group;
            current.Add(new RecordRow(row.Label, row.Value));
        }
        if (title != null)
            groups.Add(new RecordGroup(title, current));

        return new CitizenRecord(profile.name, profile.citizenId, groups);
    }
}
