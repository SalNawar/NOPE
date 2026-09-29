using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public class SeedsTests
{
    [TestCase(12345, 3, 4887720)]
    [TestCase(-7, 1, -5174)]
    [TestCase(int.MaxValue, 30, 2147245681)]
    public void Day_MatchesTheOriginalRunManagerFormula(int runSeed, int day, int expected)
    {
        Assert.AreEqual(expected, Seeds.Day(runSeed, day));
    }

    [Test]
    public void Day_DiffersFromDayToDay()
    {
        var seeds = Enumerable.Range(1, 30).Select(d => Seeds.Day(12345, d)).ToList();
        Assert.AreEqual(seeds.Count, seeds.Distinct().Count());
    }

    [Test]
    public void ForCase_DiffersFromCaseToCase_AndFromTheDaySeed()
    {
        int daySeed = Seeds.Day(12345, 1);
        var seeds = new HashSet<int>(Enumerable.Range(1, 20).Select(c => Seeds.ForCase(daySeed, c)));
        Assert.AreEqual(20, seeds.Count);
        Assert.IsFalse(seeds.Contains(daySeed));
    }

    [Test]
    public void CaseStream_NeverReplaysTheRawDaySeedStream()
    {
        int daySeed = Seeds.Day(777, 2);
        var raw = new SeededRandom(daySeed);
        var firstCase = new SeededRandom(Seeds.ForCase(daySeed, 1));
        var a = Enumerable.Range(0, 10).Select(_ => raw.Range(0, 1000000)).ToArray();
        var b = Enumerable.Range(0, 10).Select(_ => firstCase.Range(0, 1000000)).ToArray();
        CollectionAssert.AreNotEqual(a, b);
    }

    /// <summary>
    /// Every per-traveller stream, by name (audit R3-035: one table, so a new
    /// salt is one row here and one TestCase below, not another copy of the
    /// distinctness loop).
    /// </summary>
    private static readonly Dictionary<string, Func<int, int>> TravellerStreams = new Dictionary<string, Func<int, int>>
    {
        { "lie", Seeds.ForLies },
        { "dialog", Seeds.ForDialog },
        { "look", Seeds.ForLooks },
        { "legendary", Seeds.ForLegendary },
        { "account", Seeds.ForAccount },
        { "forms", Seeds.ForForms },
        { "faults", Seeds.ForFaults },
        { "personality", Seeds.ForPersonality },
    };

    [TestCase("lie")]
    [TestCase("dialog")]
    [TestCase("look")]
    [TestCase("legendary")]
    [TestCase("account")]
    [TestCase("forms")]
    [TestCase("faults")]
    [TestCase("personality")]
    public void TravellerStream_IsDeterministic_OnePerTraveller_AndApartFromEveryOtherStream(string name)
    {
        Assert.AreEqual(TravellerStreams.Count, typeof(SeedsTests).GetMethod(nameof(TravellerStream_IsDeterministic_OnePerTraveller_AndApartFromEveryOtherStream))
                                                            .GetCustomAttributes(typeof(TestCaseAttribute), false).Length, "one TestCase per table row");
        Func<int, int> stream = TravellerStreams[name];
        int daySeed = Seeds.Day(12345, 2);
        List<int> others = EveryOtherStream(daySeed, name);
        var seeds = new HashSet<int>();
        for (int c = 1; c <= 20; c++)
        {
            int caseSeed = Seeds.ForCase(daySeed, c);
            int seed = stream(caseSeed);
            Assert.AreEqual(seed, stream(caseSeed), $"case {c}: the {name} seed is not deterministic");
            CollectionAssert.DoesNotContain(others, seed, $"case {c}: the {name} seed is another stream's");
            seeds.Add(seed);
        }
        Assert.AreEqual(20, seeds.Count, $"{name} seeds repeat across travellers");
    }

    [Test]
    public void ViolatorStream_IsDeterministic_AndApartFromEveryTravellersStreams()
    {
        int daySeed = Seeds.Day(12345, 2);
        Assert.AreEqual(Seeds.ForViolators(daySeed), Seeds.ForViolators(daySeed));
        CollectionAssert.DoesNotContain(EveryOtherStream(daySeed, "violator"), Seeds.ForViolators(daySeed));
    }

    /// <summary>Audit R3-010 (phase 9): the day's event placements draw from their own salted stream, apart from the day's raw seed and every other stream.</summary>
    [Test]
    public void EventStream_IsDeterministic_AndApartFromEveryOtherStream()
    {
        int daySeed = Seeds.Day(12345, 2);
        Assert.AreEqual(Seeds.ForEvents(daySeed), Seeds.ForEvents(daySeed));
        CollectionAssert.DoesNotContain(EveryOtherStream(daySeed, "events"), Seeds.ForEvents(daySeed));
        CollectionAssert.AreNotEqual(TenDraws(daySeed), TenDraws(Seeds.ForEvents(daySeed)), "the raw day stream");
        CollectionAssert.AllItemsAreUnique(Enumerable.Range(1, 30).Select(day => Seeds.ForEvents(Seeds.Day(12345, day))).ToList(), "each day its own placements");
    }

    [Test]
    public void Salts_AreDistinct_TheRetiredClueSaltIncluded()
    {
        var salts = new[] { Seeds.CaseSalt, Seeds.ViolatorSalt, Seeds.ClueSalt, Seeds.LieSalt, Seeds.DialogSalt, Seeds.LookSalt, Seeds.LegendarySalt, Seeds.SlotSalt, Seeds.AccountSalt, Seeds.FormsSalt, Seeds.DebtNewsSalt, Seeds.FaultSalt, Seeds.EventSalt, Seeds.StrandingSalt, Seeds.FamilySalt, Seeds.PersonalitySalt, Seeds.PremadeLookSalt, Seeds.RecoverySalt, Seeds.BreakInSalt };
        CollectionAssert.AllItemsAreUnique(salts);
    }

    /// <summary>
    /// Audit R1-002 (in part): the seeds themselves are pinned, so a changed
    /// salt or Mix formula, which would reshuffle every run's travellers,
    /// fails here. Values of run 12345, day 3, slot 1.
    /// </summary>
    [Test]
    public void Streams_KeepTheirSeeds()
    {
        int daySeed = Seeds.Day(12345, 3);
        int caseSeed = Seeds.ForCase(daySeed, 1);
        Assert.AreEqual(1611744290, Seeds.Mix(12345, Seeds.CaseSalt));
        Assert.AreEqual(-263357159, caseSeed);
        Assert.AreEqual(1520228237, Seeds.ForViolators(daySeed));
        Assert.AreEqual(954518297, Seeds.ForSlot(daySeed));
        Assert.AreEqual(-816652609, Seeds.ForLies(caseSeed));
        Assert.AreEqual(1857474870, Seeds.ForDialog(caseSeed));
        Assert.AreEqual(542320186, Seeds.ForLooks(caseSeed));
        Assert.AreEqual(1809927997, Seeds.ForLegendary(caseSeed));
        Assert.AreEqual(-917021711, Seeds.ForAccount(caseSeed));
        Assert.AreEqual(0x41434354, Seeds.AccountSalt, "\"ACCT\"");
        Assert.AreEqual(-390461085, Seeds.ForForms(caseSeed));
        Assert.AreEqual(0x464F524D, Seeds.FormsSalt, "\"FORM\"");
        Assert.AreEqual(-1684775777, Seeds.ForFaults(caseSeed));
        Assert.AreEqual(0x46414C54, Seeds.FaultSalt, "\"FALT\"");
        Assert.AreEqual(-230985786, Seeds.ForDebtNews(12345));
        Assert.AreEqual(0x44454254, Seeds.DebtNewsSalt, "\"DEBT\"");
        Assert.AreEqual(-1597173572, Seeds.ForEvents(daySeed));
        Assert.AreEqual(0x45564E54, Seeds.EventSalt, "\"EVNT\"");
        Assert.AreEqual(Seeds.Mix(daySeed, 0x53545244), Seeds.ForStrandings(daySeed));
        Assert.AreEqual(0x53545244, Seeds.StrandingSalt, "\"STRD\"");
        Assert.AreEqual(-288322321, Seeds.ForFamily(daySeed));
        Assert.AreEqual(0x464D4C59, Seeds.FamilySalt, "\"FMLY\"");
        Assert.AreEqual(1986888608, Seeds.ForRecovery(daySeed));
        Assert.AreEqual(0x52435652, Seeds.RecoverySalt, "\"RCVR\"");
        Assert.AreEqual(-1690349512, Seeds.ForBreakIns(daySeed));
        Assert.AreEqual(0x42524B4E, Seeds.BreakInSalt, "\"BRKN\"");
    }

    /// <summary>
    /// A premade's generated stand-in look (days 7-15 B4) is drawn on a stream
    /// seeded by the premade's id alone: the same face at every appearance and
    /// in every run, apart from every premade's else and every traveller's streams.
    /// </summary>
    [Test]
    public void ForPremadeLook_IsStableAndDistinct()
    {
        Assert.AreEqual(0x504C4F4B, Seeds.PremadeLookSalt, "\"PLOK\"");
        Assert.AreEqual(Seeds.ForPremadeLook("pell"), Seeds.ForPremadeLook("pell"), "one id, one seed");
        string[] ids = { "pell", "ines", "rook", "ada", "hollis", "auditor", "senenmut", "socrates", "turing", "meitner", "toyoda", "a", "b", "ab", "ba", "" };
        CollectionAssert.AllItemsAreUnique(ids.Select(Seeds.ForPremadeLook).ToList(), "a different id, a different face");
        int caseSeed = Seeds.ForCase(Seeds.Day(12345, 7), 5);
        CollectionAssert.AreNotEqual(TenDraws(Seeds.ForLooks(caseSeed)), TenDraws(Seeds.ForPremadeLook("pell")), "apart from the slot's own look stream");
        Assert.AreEqual(Seeds.ForPremadeLook(null), Seeds.ForPremadeLook(string.Empty), "a missing id reads blank");
    }

    /// <summary>The personality stream's salt is "PRSN" (the personalities spec's PS2).</summary>
    [Test]
    public void PersonalitySalt_IsPinned()
    {
        Assert.AreEqual(0x5052534E, Seeds.PersonalitySalt, "\"PRSN\"");
    }

    /// <summary>The personality draw's seed of sample case seeds (run 12345, day 3, slot 1 among them): pinned, so a changed salt fails here.</summary>
    [Test]
    public void ForPersonality_IsPinnedForSampleSeeds()
    {
        Assert.AreEqual(-45739289, Seeds.ForPersonality(Seeds.ForCase(Seeds.Day(12345, 3), 1)));
        Assert.AreEqual(-666199600, Seeds.ForPersonality(1));
        Assert.AreEqual(443472842, Seeds.ForPersonality(12345));
    }

    /// <summary>A slot key's value (the personalities spec's V4: a line's pick is a value of the dialog seed and the key): Seeds.Mix folded over the key's characters, the same in every runtime.</summary>
    [Test]
    public void OfKey_IsPinned()
    {
        Assert.AreEqual(24390979, Seeds.OfKey("claim"));
        Assert.AreEqual(1046563660, Seeds.OfKey("smalltalk"));
        Assert.AreEqual(-1783325148, Seeds.OfKey("answer:q_currency"));
    }

    [Test]
    public void OfKey_DiffersPerKey()
    {
        string[] keys = { "claim", "smalltalk", "smalltalk:source", "answer:q_currency", "answer:q_device", "missing:TC-310:Honest", "missing:TC-310:Missing", "spoken:step_closer", "handover:TC-230" };
        CollectionAssert.AllItemsAreUnique(keys.Select(Seeds.OfKey).ToList());
        Assert.AreEqual(Seeds.OfKey("claim"), Seeds.OfKey("claim"));
    }

    [Test]
    public void OfKey_OfEmptyIsItsStart()
    {
        Assert.AreEqual(Seeds.KeyStart, Seeds.OfKey(string.Empty));
        Assert.AreEqual(Seeds.KeyStart, Seeds.OfKey(null), "no key: the start");
        Assert.AreEqual(0x4B455953, Seeds.KeyStart, "\"KEYS\"");
    }

    /// <summary>The stranding draws (redesign phase 13b) are the day's own stream, apart from the day's other streams and every traveller's.</summary>
    [Test]
    public void StrandingStream_IsTheDaysOwn_ApartFromEveryOtherStream()
    {
        int daySeed = Seeds.Day(12345, 2);
        Assert.AreEqual(Seeds.ForStrandings(daySeed), Seeds.ForStrandings(daySeed));
        Assert.AreNotEqual(Seeds.ForStrandings(daySeed), Seeds.ForStrandings(Seeds.Day(12345, 3)), "another day, another stream");
        CollectionAssert.DoesNotContain(EveryOtherStream(daySeed, "stranding"), Seeds.ForStrandings(daySeed));
        CollectionAssert.DoesNotContain(new[] { Seeds.ForDebtNews(12345) }, Seeds.ForStrandings(daySeed));
    }

    /// <summary>The debt line's order (redesign phase 13) is the run's own stream, apart from the day's and every traveller's.</summary>
    [Test]
    public void DebtNewsStream_IsTheRunsOwn_ApartFromTheDaysStreams()
    {
        Assert.AreEqual(Seeds.ForDebtNews(12345), Seeds.ForDebtNews(12345));
        Assert.AreNotEqual(Seeds.ForDebtNews(12345), Seeds.ForDebtNews(12346));
        for (int day = 1; day <= 15; day++)
        {
            int daySeed = Seeds.Day(12345, day);
            CollectionAssert.DoesNotContain(new[] { daySeed, Seeds.ForViolators(daySeed), Seeds.ForSlot(daySeed), Seeds.ForEvents(daySeed), Seeds.ForCase(daySeed, 1) }, Seeds.ForDebtNews(12345));
        }
    }

    [Test]
    public void Mix_IsDeterministic_AndSaltSensitive()
    {
        Assert.AreEqual(Seeds.Mix(5, 9), Seeds.Mix(5, 9));
        Assert.AreNotEqual(Seeds.Mix(5, 9), Seeds.Mix(5, 10));
        Assert.AreNotEqual(Seeds.Mix(5, 9), Seeds.Mix(6, 9));
    }

    /// <summary>
    /// Audit R2-004: the night's slot spins drew from the unseeded
    /// UnityEngine.Random, so a run did not replay and Continue (Home reloads
    /// from the save made before it) rerolled a spin. They draw from their own
    /// stream of the run and the day, apart from every other stream (the
    /// family's included) and from the day's raw stream.
    /// </summary>
    [Test]
    public void SlotStream_IsDeterministic_AndApartFromEveryOtherStream_NightByNight()
    {
        int daySeed = Seeds.Day(12345, 2);
        int slot = Seeds.ForSlot(daySeed);

        Assert.AreEqual(slot, Seeds.ForSlot(daySeed), "the same run and day give the same spins");
        CollectionAssert.DoesNotContain(EveryOtherStream(daySeed, "slot"), slot);
        CollectionAssert.AreNotEqual(TenDraws(daySeed), TenDraws(slot), "the raw day stream");
        CollectionAssert.AllItemsAreUnique(Enumerable.Range(1, 30).Select(night => Seeds.ForSlot(Seeds.Day(12345, night))).ToList(), "each night its own spins");
        Assert.AreNotEqual(slot, Seeds.ForSlot(Seeds.Day(999, 2)), "each run its own spins");
    }

    /// <summary>The house upgrades' night draws (the Home upgrades spec HU6, HU7): the break-in roll and each member's recovery roll are the night's own salted streams, apart from every other stream, the family's drift included.</summary>
    [TestCase("recovery")]
    [TestCase("breakins")]
    public void HouseStreams_AreDeterministic_AndApartFromEveryOtherStream_NightByNight(string name)
    {
        Func<int, int> stream = name == "recovery" ? (Func<int, int>)Seeds.ForRecovery : Seeds.ForBreakIns;
        int daySeed = Seeds.Day(12345, 2);
        int seed = stream(daySeed);
        Assert.AreEqual(seed, stream(daySeed), "the same run and night give the same draws");
        CollectionAssert.DoesNotContain(EveryOtherStream(daySeed, name), seed);
        CollectionAssert.AreNotEqual(TenDraws(daySeed), TenDraws(seed), "the raw day stream");
        CollectionAssert.AllItemsAreUnique(Enumerable.Range(1, 30).Select(night => stream(Seeds.Day(12345, night))).ToList(), "each night its own draws");
        Assert.AreNotEqual(seed, stream(Seeds.Day(999, 2)), "each run its own draws");
    }

    /// <summary>
    /// Audit R2-008: the night's family drift drew from its own hash of the day
    /// seed over System.Random. It is the day's own salted stream now (one
    /// seed per member, mixed from it), apart from every other stream, the
    /// slot's included, and from the day's raw stream.
    /// </summary>
    [Test]
    public void FamilyStream_IsDeterministic_AndApartFromEveryOtherStream_NightByNight()
    {
        int daySeed = Seeds.Day(12345, 2);
        int family = Seeds.ForFamily(daySeed);

        Assert.AreEqual(family, Seeds.ForFamily(daySeed), "the same run and night give the same drift");
        CollectionAssert.DoesNotContain(EveryOtherStream(daySeed, "family"), family);
        CollectionAssert.AreNotEqual(TenDraws(daySeed), TenDraws(family), "the raw day stream");
        CollectionAssert.AllItemsAreUnique(Enumerable.Range(1, 30).Select(night => Seeds.ForFamily(Seeds.Day(12345, night))).ToList(), "each night its own drift");
        Assert.AreNotEqual(family, Seeds.ForFamily(Seeds.Day(999, 2)), "each run its own drift");
    }

    /// <summary>
    /// The day's raw seed and every stream of the day but <paramref name="except"/>:
    /// the violator, slot, family and event streams, and each of 20 travellers'
    /// case stream and every stream of <see cref="TravellerStreams"/>.
    /// </summary>
    private static List<int> EveryOtherStream(int daySeed, string except)
    {
        var streams = new List<int> { daySeed };
        if (except != "violator")
            streams.Add(Seeds.ForViolators(daySeed));
        if (except != "slot")
            streams.Add(Seeds.ForSlot(daySeed));
        if (except != "family")
            streams.Add(Seeds.ForFamily(daySeed));
        if (except != "events")
            streams.Add(Seeds.ForEvents(daySeed));
        if (except != "recovery")
            streams.Add(Seeds.ForRecovery(daySeed));
        if (except != "breakins")
            streams.Add(Seeds.ForBreakIns(daySeed));
        foreach (int caseSeed in Enumerable.Range(1, 20).Select(slotIndex => Seeds.ForCase(daySeed, slotIndex)))
        {
            streams.Add(caseSeed);
            foreach (KeyValuePair<string, Func<int, int>> stream in TravellerStreams)
                if (stream.Key != except)
                    streams.Add(stream.Value(caseSeed));
        }
        return streams;
    }

    /// <summary>The first ten values a seed's stream draws.</summary>
    private static float[] TenDraws(int seed)
    {
        var rng = new SeededRandom(seed);
        var values = new float[10];
        for (int i = 0; i < values.Length; i++)
            values[i] = rng.Value();
        return values;
    }
}
