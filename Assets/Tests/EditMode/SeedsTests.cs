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
    };

    [TestCase("lie")]
    [TestCase("dialog")]
    [TestCase("look")]
    [TestCase("legendary")]
    [TestCase("account")]
    [TestCase("forms")]
    [TestCase("faults")]
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

    [Test]
    public void Salts_AreDistinct_TheRetiredClueSaltIncluded()
    {
        var salts = new[] { Seeds.CaseSalt, Seeds.ViolatorSalt, Seeds.ClueSalt, Seeds.LieSalt, Seeds.DialogSalt, Seeds.LookSalt, Seeds.LegendarySalt, Seeds.SlotSalt, Seeds.AccountSalt, Seeds.FormsSalt, Seeds.FaultSalt };
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
    /// stream of the run and the day, apart from every other stream and from
    /// the day's raw stream the family conditions draw from.
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

    /// <summary>
    /// The day's raw seed and every stream of the day but <paramref name="except"/>:
    /// the violator and slot streams, and each of 20 travellers' case stream and
    /// every stream of <see cref="TravellerStreams"/>.
    /// </summary>
    private static List<int> EveryOtherStream(int daySeed, string except)
    {
        var streams = new List<int> { daySeed };
        if (except != "violator")
            streams.Add(Seeds.ForViolators(daySeed));
        if (except != "slot")
            streams.Add(Seeds.ForSlot(daySeed));
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
