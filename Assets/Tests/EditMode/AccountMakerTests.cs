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
        waiverPrefix = "SW",
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

    /// <summary>The proofs of means as world_source.json authors them: a credit line, savings (amounts) and a policy (a number).</summary>
    private static List<ProofOfMeans> Proofs() => new List<ProofOfMeans>
    {
        new ProofOfMeans { form = "TC-415", category = ClueCategory.Credit, weight = 1f, amountMin = 4000, amountMax = 12000 },
        new ProofOfMeans { form = "TC-416", category = ClueCategory.Funds, weight = 1f, amountMin = 3000, amountMax = 15000 },
        new ProofOfMeans { form = "TC-417", category = ClueCategory.PolicyNo, weight = 1f, prefix = "TI" }
    };

    /// <summary>A request whose blueprint carries <paramref name="expiringForms"/> forms outside any group, each printing a Valid Until.</summary>
    private static AccountRequest Request(CitizenStatus status, int expiringForms = 1) => new AccountRequest
    {
        Status = status,
        Lineages = new[] { "New Kingdom Egypt (Ancient)", "Mamluk Cairo (Medieval)" },
        TripPlaces = new[] { "Periclean Athens (Ancient)", "Republican Rome (Ancient)" },
        Forms = Enumerable.Range(0, expiringForms).Select(i => new FormEntry("TC-10" + i, string.Empty, true)).ToList()
    };

    /// <summary>The poor tourist's blueprint as the account maker sees it: the visa (expires), the manifest and the waiver (no Valid Until), and the three proofs (each expires) in the proof group.</summary>
    private static AccountRequest PoorRequest()
    {
        AccountRequest r = Request(CitizenStatus.Standard);
        r.Forms = new[]
        {
            new FormEntry("TC-101", string.Empty, true), new FormEntry("TC-230", string.Empty, false), new FormEntry("TC-310", string.Empty, false),
            new FormEntry("TC-415", AccountMaker.ProofGroup, true), new FormEntry("TC-416", AccountMaker.ProofGroup, true), new FormEntry("TC-417", AccountMaker.ProofGroup, true)
        };
        return r;
    }

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

        CitizenAccount account = AccountMaker.Make(Request(CitizenStatus.Premium), Ranges(), Transponders(), Proofs(), Today, taken, rng);

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
        CitizenAccount account = AccountMaker.Make(Request(CitizenStatus.Premium, 2), Ranges(), Transponders(), Proofs(), Today, new HashSet<string>(), rng);
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
        CitizenAccount account = AccountMaker.Make(Request(CitizenStatus.Premium), Ranges(), Transponders(), Proofs(), Today, taken, rng);
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
            CitizenAccount a = AccountMaker.Make(Request(status), Ranges(), Transponders(), Proofs(), Today, new HashSet<string>(), new SeededRandom(seed));
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
            CitizenAccount a = AccountMaker.Make(Request(CitizenStatus.Premium), Ranges(), Transponders(), Proofs(), Today, taken, new SeededRandom(i % 3));
            Assert.IsTrue(ids.Add(a.CitizenId), $"traveller {i}: {a.CitizenId} twice");
            Assert.IsTrue(serials.Add(a.Transponder.Split('·')[1].Trim()), $"traveller {i}: serial twice");
        }
    }

    [Test]
    public void Make_IsDeterministic_AndPinned()
    {
        int seed = Seeds.ForAccount(Seeds.ForCase(Seeds.Day(12345, 1), 1));
        CitizenAccount a = AccountMaker.Make(Request(CitizenStatus.Premium), Ranges(), Transponders(), Proofs(), Today, new HashSet<string>(), new SeededRandom(seed));
        CitizenAccount b = AccountMaker.Make(Request(CitizenStatus.Premium), Ranges(), Transponders(), Proofs(), Today, new HashSet<string>(), new SeededRandom(seed));
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
            ScriptStep.Value(0f), ScriptStep.Range(0), // the proof: the credit line, 4,000 cr
            ScriptStep.Range(204817),             // the waiver number
            ScriptStep.Range(0),                  // lineage
            ScriptStep.Range(0),                  // no trips
            ScriptStep.Range(0));                 // Valid Until
        CitizenAccount a = AccountMaker.Make(Request(CitizenStatus.Standard), Ranges(), premiumOnly, Proofs(), Today, new HashSet<string>(), rng);
        Assert.IsTrue(rng.Done);
        Assert.IsNull(a.Transponder);
    }

    // ---- Phase 8: the waiver and the proof of means (§4.1, §4.3) ----

    [TestCase(CitizenStatus.Premium, false, false)]
    [TestCase(CitizenStatus.Standard, true, true)]
    [TestCase(CitizenStatus.Eligible, false, true)]
    public void HoldsProof_OnlyAStandardAccount_HoldsWaiver_EveryEconomyAccount(CitizenStatus status, bool proof, bool waiver)
    {
        Assert.AreEqual(proof, AccountMaker.HoldsProof(status));
        Assert.AreEqual(waiver, AccountMaker.HoldsWaiver(status));
    }

    [Test]
    public void IsProofCategory_AndIsAmount()
    {
        foreach (ClueCategory c in (ClueCategory[])Enum.GetValues(typeof(ClueCategory)))
        {
            Assert.AreEqual(c == ClueCategory.Credit || c == ClueCategory.Funds || c == ClueCategory.PolicyNo, AccountMaker.IsProofCategory(c), c.ToString());
            Assert.AreEqual(c == ClueCategory.Credit || c == ClueCategory.Funds, AccountMaker.IsAmount(c), c.ToString());
        }
    }

    [Test]
    public void Numbered_IsThePrefixAndSixDigits_FromOneDraw()
    {
        var rng = new ScriptedRandom(ScriptStep.Range(204817));
        Assert.AreEqual("SW-204817", AccountMaker.Numbered("SW", rng));
        Assert.IsTrue(rng.Done);
        Assert.AreEqual("TI-000007", AccountMaker.Numbered("TI", new ScriptedRandom(ScriptStep.Range(7))));
    }

    [Test]
    public void Make_AStandardAccount_DrawsTheProofAfterTheDebt_AndTheWaiverAfterTheTransponder()
    {
        var rng = new ScriptedRandom(
            ScriptStep.Range(418), ScriptStep.Range(937), ScriptStep.Range(52), // Citizen ID
            ScriptStep.Range(940),                                              // debt: 9,400 cr
            ScriptStep.Value(0.5f),                                             // the proof: the second of three (savings)
            ScriptStep.Range(320),                                              // 3,000 + 3,200 = 6,200 cr
            ScriptStep.Value(0f),                                               // the Economy model
            ScriptStep.Range(11952),                                            // its serial
            ScriptStep.Range(204817),                                           // the waiver number
            ScriptStep.Range(0),                                                // lineage
            ScriptStep.Range(0),                                                // no trips
            ScriptStep.Range(10), ScriptStep.Range(20));                        // the visa's and the proof's Valid Until, in form order
        var taken = new HashSet<string>();

        CitizenAccount a = AccountMaker.Make(PoorRequest(), Ranges(), Transponders(), Proofs(), Today, taken, rng);

        Assert.IsTrue(rng.Done, "every draw in order");
        Assert.AreEqual(9400, a.Debt);
        Assert.AreEqual("TC-416", a.ProofForm);
        Assert.AreEqual(ClueCategory.Funds, a.ProofCategory);
        Assert.AreEqual("6,200 cr", a.ProofValue);
        Assert.AreEqual("Tick-Tock Basic · TT-11952", a.Transponder);
        Assert.AreEqual("SW-204817", a.WaiverNo);
        CollectionAssert.AreEqual(new[] { "27 Mar 2150", "6 Apr 2150" }, a.ValidUntil.ToArray(), "one date per carried form that expires: the visa and the held proof, never the two proofs left with the agency");
        CollectionAssert.AreEquivalent(new[] { "418-0937-52", "TT-11952", "SW-204817" }, taken.ToArray(), "the numbers are taken for the day");
    }

    [Test]
    public void Make_APolicy_IsANumberNobodyHoldsToday()
    {
        var taken = new HashSet<string> { "TI-551902" };
        var rng = new ScriptedRandom(
            ScriptStep.Range(1), ScriptStep.Range(2), ScriptStep.Range(3), ScriptStep.Range(0),
            ScriptStep.Value(0.9f),                                             // the policy
            ScriptStep.Range(551902), ScriptStep.Range(551903),                 // taken: drawn again
            ScriptStep.Value(0f), ScriptStep.Range(1), ScriptStep.Range(2), ScriptStep.Range(0), ScriptStep.Range(0), ScriptStep.Range(0), ScriptStep.Range(0));
        CitizenAccount a = AccountMaker.Make(PoorRequest(), Ranges(), Transponders(), Proofs(), Today, taken, rng);
        Assert.IsTrue(rng.Done);
        Assert.AreEqual("TC-417", a.ProofForm);
        Assert.AreEqual(ClueCategory.PolicyNo, a.ProofCategory);
        Assert.AreEqual("TI-551903", a.ProofValue);
        Assert.IsTrue(taken.Contains("TI-551903"));
    }

    [Test]
    public void Make_APremiumAccount_HoldsNoWaiverAndNoProof_AndDrawsNothingForThem()
    {
        CitizenAccount a = AccountMaker.Make(Request(CitizenStatus.Premium), Ranges(), Transponders(), Proofs(), Today, new HashSet<string>(), new SeededRandom(7));
        Assert.IsNull(a.WaiverNo);
        Assert.IsNull(a.ProofForm);
        Assert.IsNull(a.ProofValue);
        CitizenAccount b = AccountMaker.Make(Request(CitizenStatus.Premium), Ranges(), Transponders(), null, Today, new HashSet<string>(), new SeededRandom(7));
        Assert.AreEqual(a.Transponder, b.Transponder, "no proof draw shifts the transponder");
    }

    [Test]
    public void Make_AnEligibleAccount_RegistersAWaiver_ButHoldsNoProof()
    {
        CitizenAccount a = AccountMaker.Make(Request(CitizenStatus.Eligible), Ranges(), Transponders(), Proofs(), Today, new HashSet<string>(), new SeededRandom(7));
        StringAssert.IsMatch(@"^SW-\d{6}$", a.WaiverNo);
        Assert.IsNull(a.ProofForm);
    }

    [Test]
    public void Make_EveryStandardAccount_HoldsOneAuthoredProof_InItsRange()
    {
        for (int seed = 0; seed < 200; seed++)
        {
            CitizenAccount a = AccountMaker.Make(PoorRequest(), Ranges(), Transponders(), Proofs(), Today, new HashSet<string>(), new SeededRandom(seed));
            ProofOfMeans proof = Proofs().Single(p => p.form == a.ProofForm);
            Assert.AreEqual(proof.category, a.ProofCategory, $"seed {seed}");
            if (AccountMaker.IsAmount(proof.category))
                Assert.That(int.Parse(a.ProofValue.Replace(",", "").Replace(" cr", "")), Is.InRange(proof.amountMin, proof.amountMax), $"seed {seed}: {a.ProofValue}");
            else
                StringAssert.IsMatch(@"^TI-\d{6}$", a.ProofValue, $"seed {seed}");
            StringAssert.IsMatch(@"^SW-\d{6}$", a.WaiverNo, $"seed {seed}");
            Assert.AreEqual(2, a.ValidUntil.Count, $"seed {seed}: the visa and the held proof expire");
        }
    }

    [Test]
    public void Carries_EveryUngroupedForm_AndOfTheProofGroupTheAccountsOne()
    {
        var standard = new CitizenAccount { Status = CitizenStatus.Standard, ProofForm = "TC-416", ProofCategory = ClueCategory.Funds, ProofValue = "6,200 cr" };
        Assert.IsTrue(AccountMaker.Carries(string.Empty, "TC-101", standard));
        Assert.IsTrue(AccountMaker.Carries(null, "TC-310", standard));
        Assert.IsTrue(AccountMaker.Carries(AccountMaker.ProofGroup, "TC-416", standard));
        Assert.IsFalse(AccountMaker.Carries(AccountMaker.ProofGroup, "TC-415", standard));
        Assert.IsFalse(AccountMaker.Carries(AccountMaker.ProofGroup, "TC-417", standard));
        Assert.IsFalse(AccountMaker.Carries("other", "TC-416", standard), "a group that is not the proof group is never carried");
        var premium = new CitizenAccount { Status = CitizenStatus.Premium };
        Assert.IsFalse(AccountMaker.Carries(AccountMaker.ProofGroup, "TC-416", premium), "no proof on file: none carried");
        Assert.IsTrue(AccountMaker.Carries(string.Empty, "TC-101", premium));
        Assert.IsTrue(AccountMaker.Carries(string.Empty, "TC-610", null), "the displaced hold no account and no grouped form");
        Assert.IsFalse(AccountMaker.Carries(AccountMaker.ProofGroup, "TC-416", null));
    }

    [Test]
    public void Problems_TheProofsOfMeans()
    {
        Assert.IsTrue(Ranges().Problems(Transponders(), new List<ProofOfMeans>()).Any(p => p.Contains("agency.proofs is empty")));
        Assert.IsTrue(Ranges().Problems(Transponders(), null).Any(p => p.Contains("agency.proofs is empty")));

        List<ProofOfMeans> proofs = Proofs();
        proofs[0].form = "TC-416";                       // twice
        proofs[0].amountMin = 5000; proofs[0].amountMax = 100; // out of order
        proofs[1].category = ClueCategory.Debt;          // not a proof
        proofs[1].weight = 0f;
        proofs[2].prefix = " ";                          // a number without its prefix
        proofs.Add(new ProofOfMeans { form = "TC-418", category = ClueCategory.Credit, weight = 1f, amountMin = 0, amountMax = AccountRanges.MaxDebt + 1 });
        List<string> problems = Ranges().Problems(Transponders(), proofs);
        Assert.IsTrue(problems.Any(p => p.Contains("'TC-416'") && p.Contains("twice")), string.Join("\n", problems));
        Assert.IsTrue(problems.Any(p => p.Contains("amount range")));
        Assert.IsTrue(problems.Any(p => p.Contains("Debt is not a proof")));
        Assert.IsTrue(problems.Any(p => p.Contains("weight")));
        Assert.IsTrue(problems.Any(p => p.Contains("prefix")));
        Assert.IsTrue(problems.Any(p => p.Contains("'TC-418'") && p.Contains("widest")));

        AccountRanges r = Ranges();
        r.waiverPrefix = "";
        Assert.IsTrue(r.Problems(Transponders(), Proofs()).Any(p => p.Contains("waiverPrefix")));
    }

    [Test]
    public void Problems_NoneForSoundContent()
    {
        CollectionAssert.IsEmpty(Ranges().Problems(Transponders(), Proofs()));
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

        List<string> problems = r.Problems(models, Proofs());

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
        List<string> problems = r.Problems(models, Proofs());
        Assert.IsTrue(problems.Any(p => p.Contains("Eligible") && p.Contains("9,999,999")), string.Join(" | ", problems));
        Assert.IsTrue(problems.Any(p => p.Contains("'long'") && p.Contains("28")), string.Join(" | ", problems));
    }

    [Test]
    public void Problems_AClassWithNoModel()
    {
        var premiumOnly = Transponders().Where(t => t.transponderClass == TransponderClass.Premium).ToList();
        Assert.IsTrue(Ranges().Problems(premiumOnly, Proofs()).Any(p => p.Contains("Economy")), "Standard and Eligible accounts need an Economy model");
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

    /// <summary>Phase 8: a Standard account's waiver and proof of means are evidence rows under their categories (the proof under its own: Credit, Funds or PolicyNo).</summary>
    [Test]
    public void Record_AStandardAccount_ListsItsWaiverAndProof_AsEvidence()
    {
        CitizenAccount a = Account();
        a.Status = CitizenStatus.Standard;
        a.TransponderClass = TransponderClass.Economy;
        a.WaiverNo = "SW-204817";
        a.ProofForm = "TC-417";
        a.ProofCategory = ClueCategory.PolicyNo;
        a.ProofValue = "TI-551902";
        var rows = Record(a).Groups.SelectMany(g => g.Rows).ToList();
        RecordRow waiver = rows.Single(r => r.Label == "<records.row.waiver>");
        RecordRow proof = rows.Single(r => r.Label == "<records.row.proof>");
        Assert.IsTrue(waiver.IsEvidence && waiver.Category == ClueCategory.WaiverNo && waiver.Value == "SW-204817");
        Assert.IsTrue(proof.IsEvidence && proof.Category == ClueCategory.PolicyNo && proof.Value == "TI-551902");
        Assert.AreEqual(10, rows.Count(r => r.IsEvidence));
        Assert.AreEqual(1, rows.Count(r => r.Value == "<records.none>"), "only the contract is none on file");
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
        Assert.AreSame(clerk, registry.Find("773-2840-19"));
        Assert.AreSame(clerk, registry.Find("theo marlow"));
    }

    [Test]
    public void Clerk_WithNoAuthoredAccount_IsNoRecord()
    {
        Assert.IsNull(AccountRecords.Clerk(new ClerkContent(), new AccountRow[0]), "a blank name is no record (CitizenRegistry.Add ignores it anyway)");
        Assert.IsNull(AccountRecords.Clerk(null, null));
    }

    [Test]
    public void Record_IsFoundByNumberAndByName_InTheRegistry()
    {
        var registry = new CitizenRegistry();
        registry.Add(Record(Account()));
        Assert.AreEqual("Omar", registry.Find("418-0937-52")?.FullName);
        Assert.AreEqual("Omar", registry.Find(" omar ")?.FullName);
        Assert.IsNull(registry.Find("418-0937-5"), "a number is found whole only");
    }
}
