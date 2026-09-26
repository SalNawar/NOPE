using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

/// <summary>
/// A 2150 citizen's account (traveller types R1-R3, §4.1, §4.3): drawn on
/// the traveller's account stream in the fixed order, each status in its
/// ranges, every number unique within the day; and the account as record
/// rows in the art's three groups, found by Citizen ID or name.
/// </summary>
public class AccountMakerTests
{
    private static readonly DateTime Today = new DateTime(2150, 3, 14);

    private static AccountRanges Ranges() => new AccountRanges
    {
        validDaysMin = 3,
        validDaysMax = 365,
        tripsWithinDays = 1095,
        statuses = new List<StatusRanges>
        {
            new StatusRanges { status = CitizenStatus.Premium, debtMin = 0, debtMax = 0, tripsMin = 0, tripsMax = 3 },
            new StatusRanges { status = CitizenStatus.Standard, debtMin = 0, debtMax = 18000, tripsMin = 0, tripsMax = 3 },
            new StatusRanges { status = CitizenStatus.Eligible, debtMin = 40000, debtMax = 320000, tripsMin = 0, tripsMax = 2 }
        }
    };

    private static List<TransponderModel> Transponders() => new List<TransponderModel>
    {
        new TransponderModel { id = "hopper2", transponderClass = TransponderClass.Premium, model = "Hopper Mk II", prefix = "HP", weight = 1f },
        new TransponderModel { id = "tick", transponderClass = TransponderClass.Economy, model = "Tick-Tock Basic", prefix = "TT", weight = 1f },
        new TransponderModel { id = "aurelian", transponderClass = TransponderClass.Premium, model = "Aurelian 9", prefix = "AU", weight = 1f }
    };

    private static AccountRequest Request(CitizenStatus status, int expiringForms = 1) => new AccountRequest
    {
        Status = status,
        Lineages = new[] { "New Kingdom Egypt (Ancient)", "Mamluk Cairo (Medieval)" },
        TripPlaces = new[] { "Periclean Athens (Ancient)", "Republican Rome (Ancient)" },
        ExpiringForms = expiringForms
    };

    [TestCase(TravellerKind.RichTourist, CitizenStatus.Premium)]
    [TestCase(TravellerKind.PoorTourist, CitizenStatus.Standard)]
    [TestCase(TravellerKind.Labourer, CitizenStatus.Eligible)]
    public void StatusOf_EachCitizenKind(TravellerKind kind, CitizenStatus expected)
    {
        Assert.IsTrue(AccountMaker.StatusOf(kind, out CitizenStatus status));
        Assert.AreEqual(expected, status);
    }

    [Test]
    public void StatusOf_TheDisplaced_HaveNoAccount()
    {
        Assert.IsFalse(AccountMaker.StatusOf(TravellerKind.Displaced, out _), "a registry entry, not an account (§4.2)");
    }

    [Test]
    public void ClassOf_OnlyPremiumTravelsOnAPremiumTransponder()
    {
        Assert.AreEqual(TransponderClass.Premium, AccountMaker.ClassOf(CitizenStatus.Premium));
        Assert.AreEqual(TransponderClass.Economy, AccountMaker.ClassOf(CitizenStatus.Standard));
        Assert.AreEqual(TransponderClass.Economy, AccountMaker.ClassOf(CitizenStatus.Eligible));
    }

    [Test]
    public void CitizenId_IsThreeFourTwoDigits_FromThreeDraws()
    {
        var rng = new ScriptedRandom(ScriptStep.Range(418), ScriptStep.Range(937), ScriptStep.Range(52));
        Assert.AreEqual("418-0937-52", AccountMaker.CitizenId(rng));
        Assert.IsTrue(rng.Done);
        Assert.AreEqual("000-0000-00", AccountMaker.CitizenId(new ScriptedRandom(ScriptStep.Range(0), ScriptStep.Range(0), ScriptStep.Range(0))));
        Assert.AreEqual("999-9999-99", AccountMaker.CitizenId(new ScriptedRandom(ScriptStep.Range(999), ScriptStep.Range(9999), ScriptStep.Range(99))));
    }

    [Test]
    public void Serial_IsThePrefixAndFiveDigits_FromOneDraw()
    {
        var rng = new ScriptedRandom(ScriptStep.Range(40718));
        Assert.AreEqual("HP-40718", AccountMaker.Serial("HP", rng));
        Assert.IsTrue(rng.Done);
        Assert.AreEqual("TT-00007", AccountMaker.Serial("TT", new ScriptedRandom(ScriptStep.Range(7))));
        Assert.AreEqual("Hopper Mk II · HP-40718", AccountMaker.TransponderName("Hopper Mk II", "HP-40718"));
    }

    [Test]
    public void Credits_AreWholeCreditsWithThousandsSeparators()
    {
        Assert.AreEqual("0 cr", AccountMaker.Credits(0));
        Assert.AreEqual("9,400 cr", AccountMaker.Credits(9400));
        Assert.AreEqual("125,430 cr", AccountMaker.Credits(125430));
    }

    [Test]
    public void Amount_IsInStepsOfTen_WithinTheRange_OneDraw()
    {
        Assert.AreEqual(40000, AccountMaker.Amount(40000, 320000, new ScriptedRandom(ScriptStep.Range(0))));
        Assert.AreEqual(40010, AccountMaker.Amount(40000, 320000, new ScriptedRandom(ScriptStep.Range(1))));
        Assert.AreEqual(320000, AccountMaker.Amount(40000, 320000, new ScriptedRandom(ScriptStep.Range(99999999))), "clamped to the top");
        var zero = new ScriptedRandom(ScriptStep.Range(5));
        Assert.AreEqual(0, AccountMaker.Amount(0, 0, zero), "a fixed amount still draws once, so the order never depends on the range");
        Assert.IsTrue(zero.Done);
    }

    [Test]
    public void Make_DrawsInTheFixedOrder_IdDebtTransponderLineageTripsValidUntil()
    {
        var rng = new ScriptedRandom(
            ScriptStep.Range(418), ScriptStep.Range(937), ScriptStep.Range(52), // Citizen ID
            ScriptStep.Range(0),                                                // debt (Premium: 0 cr)
            ScriptStep.Value(0.9f),                                             // the Premium model: the second of two
            ScriptStep.Range(40718),                                            // its serial
            ScriptStep.Range(1),                                                // lineage: the second
            ScriptStep.Range(2),                                                // two past trips
            ScriptStep.Range(9), ScriptStep.Range(0),                           // 10 days ago, Periclean Athens
            ScriptStep.Range(99), ScriptStep.Range(1),                          // 100 days ago, Republican Rome
            ScriptStep.Range(10));                                              // the visa: valid 13 days ahead
        var taken = new HashSet<string>();

        CitizenAccount account = AccountMaker.Make(Request(CitizenStatus.Premium), Ranges(), Transponders(), Today, taken, rng);

        Assert.IsTrue(rng.Done, "every draw in order");
        Assert.AreEqual("418-0937-52", account.CitizenId);
        Assert.AreEqual(CitizenStatus.Premium, account.Status);
        Assert.AreEqual(0, account.Debt);
        Assert.AreEqual(TransponderClass.Premium, account.TransponderClass);
        Assert.AreEqual("Aurelian 9 · AU-40718", account.Transponder, "only models of the account's class are drawn from");
        Assert.AreEqual("Mamluk Cairo (Medieval)", account.Lineage);
        Assert.AreEqual(2, account.Trips.Count);
        Assert.AreEqual("4 Mar 2150", account.Trips[0].Date, "newest first");
        Assert.AreEqual("Periclean Athens (Ancient)", account.Trips[0].Place);
        Assert.AreEqual("4 Dec 2149", account.Trips[1].Date);
        Assert.AreEqual("Republican Rome (Ancient)", account.Trips[1].Place);
        CollectionAssert.AreEqual(new[] { "27 Mar 2150" }, account.ValidUntil.ToArray());
        Assert.AreEqual("14 Mar 2150", account.Departure, "booked for today");
        CollectionAssert.AreEquivalent(new[] { "418-0937-52", "AU-40718" }, taken.ToArray(), "the numbers are taken for the day");
    }

    [Test]
    public void Make_OneValidUntilPerExpiringForm_InFormOrder()
    {
        var rng = new ScriptedRandom(
            ScriptStep.Range(1), ScriptStep.Range(2), ScriptStep.Range(3),
            ScriptStep.Range(0), ScriptStep.Value(0f), ScriptStep.Range(1), ScriptStep.Range(0),
            ScriptStep.Range(0),                          // no trips
            ScriptStep.Range(0), ScriptStep.Range(362));  // two forms: 3 and 365 days ahead
        CitizenAccount account = AccountMaker.Make(Request(CitizenStatus.Premium, 2), Ranges(), Transponders(), Today, new HashSet<string>(), rng);
        Assert.IsTrue(rng.Done);
        CollectionAssert.AreEqual(new[] { "17 Mar 2150", "14 Mar 2151" }, account.ValidUntil.ToArray());
        Assert.AreEqual(0, account.Trips.Count);
    }

    [Test]
    public void Make_RedrawsANumberAlreadyTakenToday()
    {
        var taken = new HashSet<string> { "418-0937-52", "HP-40718" };
        var rng = new ScriptedRandom(
            ScriptStep.Range(418), ScriptStep.Range(937), ScriptStep.Range(52),  // taken: drawn again
            ScriptStep.Range(418), ScriptStep.Range(937), ScriptStep.Range(53),
            ScriptStep.Range(0), ScriptStep.Value(0f),
            ScriptStep.Range(40718), ScriptStep.Range(40719),                     // taken serial: drawn again
            ScriptStep.Range(0), ScriptStep.Range(0), ScriptStep.Range(0));
        CitizenAccount account = AccountMaker.Make(Request(CitizenStatus.Premium), Ranges(), Transponders(), Today, taken, rng);
        Assert.IsTrue(rng.Done);
        Assert.AreEqual("418-0937-53", account.CitizenId);
        Assert.AreEqual("Hopper Mk II · HP-40719", account.Transponder);
    }

    [TestCase(CitizenStatus.Premium, 0, 0)]
    [TestCase(CitizenStatus.Standard, 0, 18000)]
    [TestCase(CitizenStatus.Eligible, 40000, 320000)]
    public void Make_DebtStaysInItsStatusRange_AndTheClassFollowsTheStatus(CitizenStatus status, int min, int max)
    {
        for (int seed = 0; seed < 200; seed++)
        {
            CitizenAccount a = AccountMaker.Make(Request(status), Ranges(), Transponders(), Today, new HashSet<string>(), new SeededRandom(seed));
            Assert.That(a.Debt, Is.InRange(min, max), $"seed {seed}");
            Assert.AreEqual(0, a.Debt % 10, "whole tens of credits");
            Assert.AreEqual(AccountMaker.ClassOf(status), a.TransponderClass);
            Assert.IsTrue(Transponders().Any(t => t.transponderClass == a.TransponderClass && a.Transponder.StartsWith(t.model + " · " + t.prefix + "-")), a.Transponder);
            Assert.That(a.Trips.Count, Is.InRange(0, Ranges().For(status).tripsMax));
            Assert.IsTrue(Regex.IsMatch(a.CitizenId, @"^\d{3}-\d{4}-\d{2}$"), a.CitizenId);
        }
    }

    [Test]
    public void Make_NumbersAreUniqueWithinTheDay()
    {
        var taken = new HashSet<string>();
        var ids = new HashSet<string>();
        var serials = new HashSet<string>();
        for (int i = 0; i < 300; i++)
        {
            CitizenAccount a = AccountMaker.Make(Request(CitizenStatus.Premium), Ranges(), Transponders(), Today, taken, new SeededRandom(i % 3));
            Assert.IsTrue(ids.Add(a.CitizenId), $"traveller {i}: {a.CitizenId} twice");
            Assert.IsTrue(serials.Add(a.Transponder.Split('·')[1].Trim()), $"traveller {i}: serial twice");
        }
    }

    [Test]
    public void Make_IsDeterministic_AndPinned()
    {
        int seed = Seeds.ForAccount(Seeds.ForCase(Seeds.Day(12345, 1), 1));
        CitizenAccount a = AccountMaker.Make(Request(CitizenStatus.Premium), Ranges(), Transponders(), Today, new HashSet<string>(), new SeededRandom(seed));
        CitizenAccount b = AccountMaker.Make(Request(CitizenStatus.Premium), Ranges(), Transponders(), Today, new HashSet<string>(), new SeededRandom(seed));
        Assert.AreEqual(a.CitizenId, b.CitizenId);
        Assert.AreEqual(a.Transponder, b.Transponder);
        CollectionAssert.AreEqual(a.ValidUntil.ToArray(), b.ValidUntil.ToArray());

        // Audit R1-002: the numbers themselves, so a changed draw order or range reduction fails here.
        Assert.AreEqual(PinnedId, a.CitizenId);
        Assert.AreEqual(PinnedTransponder, a.Transponder);
        Assert.AreEqual(PinnedLineage, a.Lineage);
        Assert.AreEqual(PinnedTrips, a.Trips.Count);
        Assert.AreEqual(PinnedValidUntil, a.ValidUntil[0]);
    }

    private const string PinnedId = "238-5108-55";
    private const string PinnedTransponder = "Hopper Mk II · HP-18583";
    private const string PinnedLineage = "New Kingdom Egypt (Ancient)";
    private const int PinnedTrips = 2;
    private const string PinnedValidUntil = "18 Oct 2150";

    [Test]
    public void Make_WithNoModelOfTheClass_LeavesTheTransponderBlank_AndDrawsNothingForIt()
    {
        var premiumOnly = Transponders().Where(t => t.transponderClass == TransponderClass.Premium).ToList();
        var rng = new ScriptedRandom(
            ScriptStep.Range(1), ScriptStep.Range(2), ScriptStep.Range(3),
            ScriptStep.Range(0),                  // debt
            ScriptStep.Range(0),                  // lineage
            ScriptStep.Range(0),                  // no trips
            ScriptStep.Range(0));                 // Valid Until
        CitizenAccount a = AccountMaker.Make(Request(CitizenStatus.Standard), Ranges(), premiumOnly, Today, new HashSet<string>(), rng);
        Assert.IsTrue(rng.Done);
        Assert.IsNull(a.Transponder);
    }

    [Test]
    public void Problems_NoneForSoundContent()
    {
        CollectionAssert.IsEmpty(Ranges().Problems(Transponders()));
    }

    [Test]
    public void Problems_NameEachBrokenKnob()
    {
        AccountRanges r = Ranges();
        r.validDaysMin = 10;
        r.validDaysMax = 5;
        r.tripsWithinDays = 0;
        r.statuses[1].debtMin = 500;
        r.statuses[1].debtMax = 100;
        r.statuses[2].tripsMin = -1;
        r.statuses.RemoveAt(0);
        var models = Transponders();
        models.Add(new TransponderModel { id = "tick", transponderClass = TransponderClass.Economy, model = " ", prefix = "", weight = 0f });

        List<string> problems = r.Problems(models);

        Assert.IsTrue(problems.Any(p => p.Contains("validDaysMin")), string.Join("\n", problems));
        Assert.IsTrue(problems.Any(p => p.Contains("tripsWithinDays")));
        Assert.IsTrue(problems.Any(p => p.Contains("Standard") && p.Contains("debt")));
        Assert.IsTrue(problems.Any(p => p.Contains("Eligible") && p.Contains("trips")));
        Assert.IsTrue(problems.Any(p => p.Contains("Premium") && p.Contains("no ranges")));
        Assert.IsTrue(problems.Any(p => p.Contains("'tick'") && p.Contains("twice")));
        Assert.IsTrue(problems.Any(p => p.Contains("model")));
        Assert.IsTrue(problems.Any(p => p.Contains("prefix")));
        Assert.IsTrue(problems.Any(p => p.Contains("weight")));
    }

    [Test]
    public void Problems_ADebtOrATransponderNameTooWideToPrint()
    {
        AccountRanges r = Ranges();
        r.statuses[2].debtMax = AccountRanges.MaxDebt + 10;
        var models = Transponders();
        models.Add(new TransponderModel { id = "long", transponderClass = TransponderClass.Economy, model = "Extraordinarily Long Unit", prefix = "XL", weight = 1f });
        List<string> problems = r.Problems(models);
        Assert.IsTrue(problems.Any(p => p.Contains("Eligible") && p.Contains("9,999,999")), string.Join(" | ", problems));
        Assert.IsTrue(problems.Any(p => p.Contains("'long'") && p.Contains("28")), string.Join(" | ", problems));
    }

    [Test]
    public void Problems_AClassWithNoModel()
    {
        var premiumOnly = Transponders().Where(t => t.transponderClass == TransponderClass.Premium).ToList();
        Assert.IsTrue(Ranges().Problems(premiumOnly).Any(p => p.Contains("Economy")), "Standard and Eligible accounts need an Economy model");
    }

    // ---- The account as record rows (§4.1) ----

    private static CitizenAccount Account() => new CitizenAccount
    {
        CitizenId = "418-0937-52",
        Status = CitizenStatus.Premium,
        Debt = 0,
        Transponder = "Hopper Mk II · HP-40718",
        TransponderClass = TransponderClass.Premium,
        Lineage = "Mamluk Cairo (Medieval)",
        Trips = new[] { new PastTrip("4 Mar 2150", "Periclean Athens (Ancient)") },
        ValidUntil = new[] { "27 Mar 2150" },
        Departure = "14 Mar 2150"
    };

    private static CitizenRecord Record(CitizenAccount account) =>
        AccountRecords.Record("Omar", "3 May 2101", "Periclean Athens (Ancient)", account, key => "<" + key + ">");

    [Test]
    public void Record_IsFoundByCitizenId_AndHoldsTheArtsThreeGroups_ThenTheNote()
    {
        CitizenRecord rec = Record(Account());
        Assert.AreEqual("Omar", rec.FullName);
        Assert.AreEqual("418-0937-52", rec.Number);
        CollectionAssert.AreEqual(new[] { "<records.group.account>", "<records.group.forms>", "<records.group.travel>", string.Empty },
                                  rec.Groups.Select(g => g.Title).ToArray());
    }

    [Test]
    public void Record_EvidenceRowsCarryTheirCategory_TheRestAreShownOnly()
    {
        CitizenRecord rec = Record(Account());
        var rows = rec.Groups.SelectMany(g => g.Rows).ToList();
        string[] Evidence(ClueCategory c) => rows.Where(r => r.IsEvidence && r.Category == c).Select(r => r.Value).ToArray();

        CollectionAssert.AreEqual(new[] { "Omar" }, Evidence(ClueCategory.Name));
        CollectionAssert.AreEqual(new[] { "418-0937-52" }, Evidence(ClueCategory.CitizenId));
        CollectionAssert.AreEqual(new[] { "3 May 2101" }, Evidence(ClueCategory.BirthDate));
        CollectionAssert.AreEqual(new[] { "Premium" }, Evidence(ClueCategory.AccountStatus));
        CollectionAssert.AreEqual(new[] { "0 cr" }, Evidence(ClueCategory.Debt));
        CollectionAssert.AreEqual(new[] { "Hopper Mk II · HP-40718" }, Evidence(ClueCategory.TransponderId));
        CollectionAssert.AreEqual(new[] { "Premium" }, Evidence(ClueCategory.TransponderClass));
        CollectionAssert.AreEqual(new[] { "Periclean Athens (Ancient)" }, Evidence(ClueCategory.Destination), "the booked departure");
        Assert.AreEqual(8, rows.Count(r => r.IsEvidence), "one row per compared category, nothing else");

        Assert.IsTrue(rows.Any(r => !r.IsEvidence && r.Label == "<records.row.standing>" && r.Value == "<records.standing.good>"));
        Assert.IsTrue(rows.Any(r => !r.IsEvidence && r.Label == "<records.row.lineage>" && r.Value == "Mamluk Cairo (Medieval)"));
        Assert.IsTrue(rows.Any(r => !r.IsEvidence && r.Label == "<records.row.departureDate>" && r.Value == "14 Mar 2150"), "the date beside the booking is never compared");
        Assert.IsTrue(rows.Any(r => !r.IsEvidence && r.Label == "<records.row.trip>" && r.Value == "4 Mar 2150, Periclean Athens (Ancient), <records.trip.returned>"));
        Assert.AreEqual(3, rows.Count(r => r.Value == "<records.none>"), "waiver, proof of means and contract: none on file for a Premium account");
        Assert.AreEqual("<records.note.none>", rec.Groups[3].Rows.Single().Value);
    }

    [Test]
    public void Record_WithNoPastTrips_SaysNoneOnFile()
    {
        CitizenAccount a = Account();
        a.Trips = Array.Empty<PastTrip>();
        RecordRow trips = Record(a).Groups[2].Rows.Last();
        Assert.AreEqual("<records.row.trips>", trips.Label);
        Assert.AreEqual("<records.none>", trips.Value);
        Assert.IsFalse(trips.IsEvidence);
    }

    [Test]
    public void Clerk_IsFoundByCitizenIdAndName_AndNothingOnItIsEvidence()
    {
        var rows = new[]
        {
            new AccountRow("RECORDS", "Name", "Theo Marlow"),
            new AccountRow("RECORDS", "Citizen ID", "773-2840-19"),
            new AccountRow("RECORDS", "Debt", "–"),
            new AccountRow("FORMS ON FILE", "Employment", "Temporal Customs · Desk 3"),
            new AccountRow("TRAVEL", "Booked departure", "None"),
            new AccountRow(string.Empty, "Note", "No remarks on file.")
        };
        CitizenRecord clerk = AccountRecords.Clerk(new ClerkContent { name = "Theo Marlow", citizenId = "773-2840-19" }, rows);

        Assert.AreEqual("Theo Marlow", clerk.FullName);
        Assert.AreEqual("773-2840-19", clerk.Number);
        CollectionAssert.AreEqual(new[] { "RECORDS", "FORMS ON FILE", "TRAVEL", string.Empty }, clerk.Groups.Select(g => g.Title).ToArray(), "the Citizen Account app's groups, in order");
        CollectionAssert.AreEqual(rows.Select(r => r.Label + "=" + r.Value).ToArray(), clerk.Groups.SelectMany(g => g.Rows).Select(r => r.Label + "=" + r.Value).ToArray(), "the app's rows, one source");
        Assert.IsTrue(clerk.Groups.SelectMany(g => g.Rows).All(r => !r.IsEvidence), "the clerk is nobody's case: no row is a compare pick (§4.4)");

        var registry = new CitizenRegistry();
        registry.Add(Record(Account()));
        registry.Add(clerk);
        Assert.AreSame(clerk, Find(registry, "773-2840-19"));
        Assert.AreSame(clerk, Find(registry, "theo marlow"));
    }

    [Test]
    public void Clerk_WithNoAuthoredAccount_IsNoRecord()
    {
        Assert.IsNull(AccountRecords.Clerk(new ClerkContent(), new AccountRow[0]), "a blank name is no record (CitizenRegistry.Add ignores it anyway)");
        Assert.IsNull(AccountRecords.Clerk(null, null));
    }

    /// <summary>The Records lookup as the Records tab runs it (redesign phase 19: the search index scoped to Records, its best hit).</summary>
    private static CitizenRecord Find(CitizenRegistry registry, string query)
    {
        var index = new CaseIndex();
        index.SetDay(IndexEntries.Records(registry, "{0} · {1}"));
        IndexEntry hit = index.Search(SearchQuery.Parse(query), new[] { AppTab.Records }, 1, AppTab.Records).FirstOrDefault()?.Hits[0].Entry;
        return hit != null ? registry.Records[hit.Item] : null;
    }

    [Test]
    public void Record_IsFoundByNumberAndByName_InTheRegistry()
    {
        var registry = new CitizenRegistry();
        registry.Add(Record(Account()));
        Assert.AreEqual("Omar", Find(registry, "418-0937-52")?.FullName);
        Assert.AreEqual("Omar", Find(registry, " omar ")?.FullName);
        Assert.AreEqual("Omar", Find(registry, "418-0937-5")?.FullName, "a number's parts are found by their starts: one matcher with search (the PC spec's SE3, SE6)");
    }
}
