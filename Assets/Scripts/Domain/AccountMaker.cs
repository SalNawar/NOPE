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

    /// <summary>Each status's ranges (one entry per status).</summary>
    public List<StatusRanges> statuses = new();

    /// <summary>The ranges of <paramref name="status"/>, or null when none are authored.</summary>
    public StatusRanges For(CitizenStatus status) => statuses?.FirstOrDefault(s => s != null && s.status == status);

    /// <summary>
    /// What Generate World and the validator refuse: Valid Until and trip
    /// windows AgencyNumbers cannot draw from; a status with no ranges, or
    /// with a debt or trip range out of order or a debt above
    /// <see cref="MaxDebt"/>; and, over <paramref name="transponders"/>, a
    /// model id used twice, a blank model or prefix, a printed name wider than
    /// a book row (FactTable.MaxValueLength, a form's box), a weight of 0 or
    /// less, and a class some status travels on with no model. Empty when sound.
    /// </summary>
    public List<string> Problems(IReadOnlyList<TransponderModel> transponders)
    {
        var problems = new List<string>();
        if (validDaysMin < 0 || validDaysMin > validDaysMax)
            problems.Add($"agency.accounts.validDaysMin {validDaysMin} and validDaysMax {validDaysMax}: an honest paper is valid from 0 <= validDaysMin <= validDaysMax days after today.");
        if (tripsWithinDays < 1)
            problems.Add($"agency.accounts.tripsWithinDays is {tripsWithinDays}: a past trip left at least 1 day before today.");

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

    /// <summary>How many of the traveller's forms print a Valid Until (one honest date is drawn for each, in form order).</summary>
    public int ExpiringForms;
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

    /// <summary>
    /// A citizen's account, in the fixed draw order (§4.3): the Citizen ID
    /// (<see cref="CitizenId"/>, redrawn while taken today); the debt
    /// (<see cref="Amount"/>, one draw); the transponder model (one weighted
    /// draw among the models of the status's class; none without a model) and
    /// its serial (<see cref="Serial"/>, redrawn while taken); the lineage (one
    /// draw; none without lineages); the number of past trips (one draw), then
    /// each trip's day (AgencyNumbers.DaysAgo) and place (one draw each); then
    /// one honest Valid Until per expiring form (AgencyNumbers.DaysAhead). The
    /// ID and the serial join <paramref name="takenToday"/>. Later phases add
    /// their draws in their places (a poor tourist's proof of means after the
    /// debt; the waiver after the transponder; a labourer's contract after it).
    /// </summary>
    public static CitizenAccount Make(AccountRequest request, AccountRanges ranges, IReadOnlyList<TransponderModel> transponders,
                                      DateTime today, ISet<string> takenToday, IRandomSource rng)
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

        account.CitizenId = AgencyNumbers.TakeUnique(takenToday, () => CitizenId(rng));
        account.Debt = Amount(status.debtMin, status.debtMax, rng);

        List<TransponderModel> models = (transponders ?? Array.Empty<TransponderModel>())
            .Where(t => t != null && t.transponderClass == grade)
            .ToList();
        TransponderModel model = WeightedRandom.Pick(models, t => t.weight, rng);
        if (model != null)
            account.Transponder = TransponderName(model.model, AgencyNumbers.TakeUnique(takenToday, () => Serial(model.prefix, rng)));

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

        var validUntil = new string[Math.Max(0, request.ExpiringForms)];
        for (int i = 0; i < validUntil.Length; i++)
            validUntil[i] = AgencyCalendar.Write(AgencyNumbers.DaysAhead(today, ranges.validDaysMin, ranges.validDaysMax, rng));
        account.ValidUntil = validUntil;

        return account;
    }

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
    public static int Amount(int min, int max, IRandomSource rng)
    {
        if (max < min)
            (min, max) = (max, min);
        int steps = (max - min) / 10;
        return min + 10 * rng.Range(0, steps + 1);
    }
}

/// <summary>
/// A Citizen Account as record rows (traveller types R1, §4.1): the art's
/// three groups (Records, Forms on file, Travel) and the note, found by the
/// Citizen ID or the name. A row whose category is compared is evidence (a
/// compare pick); the rest (standing, lineage, the forms not on file, the
/// departure date, past trips, the note) is shown only. Labels and fixed
/// words come through <c>text</c> (UI string keys), values from the account.
/// The clerk's own account is a record too, with no evidence row.
/// </summary>
public static class AccountRecords
{
    /// <summary>
    /// The record of a citizen named <paramref name="name"/>, born
    /// <paramref name="born"/>, booked to <paramref name="destination"/> today.
    /// </summary>
    public static CitizenRecord Record(string name, string born, string destination, CitizenAccount account, Func<string, string> text)
    {
        account = account ?? new CitizenAccount();
        string none = text("records.none");

        var records = new List<RecordRow>
        {
            new RecordRow(text("records.row.name"), name, ClueCategory.Name),
            new RecordRow(text("records.row.citizenId"), account.CitizenId, ClueCategory.CitizenId),
            new RecordRow(text("records.row.born"), born, ClueCategory.BirthDate),
            new RecordRow(text("records.row.status"), account.Status.ToString(), ClueCategory.AccountStatus),
            new RecordRow(text("records.row.standing"), text("records.standing.good")),
            new RecordRow(text("records.row.debt"), AccountMaker.Credits(account.Debt), ClueCategory.Debt),
            new RecordRow(text("records.row.lineage"), account.Lineage ?? none)
        };

        var forms = new List<RecordRow>
        {
            new RecordRow(text("records.row.transponder"), account.Transponder ?? none, ClueCategory.TransponderId),
            new RecordRow(text("records.row.transponderClass"), account.TransponderClass.ToString(), ClueCategory.TransponderClass),
            new RecordRow(text("records.row.waiver"), none),
            new RecordRow(text("records.row.proof"), none),
            new RecordRow(text("records.row.contract"), none)
        };

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
            new RecordGroup(string.Empty, new[] { new RecordRow(text("records.row.note"), text("records.note.none")) })
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
