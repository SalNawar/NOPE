using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// Cheap transponders fail (the traveller-types spec's S1-S3; redesign phase
/// 13b): one draw per accepted Economy traveller in queue order on the day's
/// own stream, the real class deciding; the waiver's standing and the fine's
/// cases; the morning paper's stranding lines.
/// </summary>
public class StrandingsTests
{
    private static readonly TransponderClass?[] Queue =
    {
        TransponderClass.Premium, TransponderClass.Economy, null, TransponderClass.Economy, TransponderClass.Economy
    };

    /// <summary>A source whose draws are scripted, so which traveller strands is read off the script.</summary>
    private sealed class Scripted : IRandomSource
    {
        private readonly Queue<float> _values;

        public Scripted(params float[] values) => _values = new Queue<float>(values);

        public int Draws { get; private set; }

        public int Range(int minInclusive, int maxExclusive) => minInclusive;

        public float Value()
        {
            Draws++;
            return _values.Count > 0 ? _values.Dequeue() : 0.99f;
        }
    }

    [Test]
    public void Roll_OneDrawPerEconomyTraveller_InQueueOrder_TheRealClassDeciding()
    {
        var rng = new Scripted(0.5f, 0.02f, 0.9f);

        List<int> stranded = Strandings.Roll(Queue, 0.08f, rng);

        Assert.AreEqual(3, rng.Draws, "the Premium unit and the displaced (no transponder) draw nothing");
        CollectionAssert.AreEqual(new[] { 3 }, stranded, "the second Economy traveller's draw fell under the chance");
    }

    [Test]
    public void Roll_EveryEconomyTravellerDraws_WhateverTheChance()
    {
        var none = new Scripted(0.5f, 0.02f, 0.9f);
        CollectionAssert.IsEmpty(Strandings.Roll(Queue, 0f, none));
        Assert.AreEqual(3, none.Draws, "a chance of 0 still draws, so tuning never shifts a later draw");

        var all = new Scripted(0.5f, 0.02f, 0.9f);
        CollectionAssert.AreEqual(new[] { 1, 3, 4 }, Strandings.Roll(Queue, 1f, all));
    }

    [Test]
    public void Roll_IsDeterministicOnTheDaysStream_AndDrawsNothingWithoutTravellers()
    {
        int seed = Seeds.ForStrandings(Seeds.Day(12345, 2));
        var many = Enumerable.Repeat<TransponderClass?>(TransponderClass.Economy, 200).ToList();

        List<int> a = Strandings.Roll(many, 0.08f, new SeededRandom(seed));
        List<int> b = Strandings.Roll(many, 0.08f, new SeededRandom(seed));

        CollectionAssert.AreEqual(a, b);
        Assert.That(a.Count, Is.InRange(5, 35), $"about 8 % of 200 strand ({a.Count})");
        CollectionAssert.IsEmpty(Strandings.Roll(new TransponderClass?[0], 1f, new SeededRandom(seed)));
        CollectionAssert.IsEmpty(Strandings.Roll(null, 1f, new SeededRandom(seed)));
        CollectionAssert.IsEmpty(Strandings.Roll(many, 1f, null));
    }

    [TestCase(false, "T. Marlow", "SW-204817", "SW-204817", WaiverStanding.None, Description = "no waiver handed over")]
    [TestCase(true, "UNSIGNED", "SW-204817", "SW-204817", WaiverStanding.Unsigned)]
    [TestCase(true, "unsigned", "SW-204817", "SW-204817", WaiverStanding.Unsigned, Description = "whatever its case")]
    [TestCase(true, " ", "SW-204817", "SW-204817", WaiverStanding.Unsigned, Description = "a blank signature row")]
    [TestCase(true, "T. Marlow", "SW-999999", "SW-204817", WaiverStanding.Unregistered, Description = "a forged waiver is no waiver")]
    [TestCase(true, "T. Marlow", "SW-204817", "", WaiverStanding.Unregistered, Description = "the account registers no waiver")]
    [TestCase(true, "T. Marlow", "SW-204817", null, WaiverStanding.Unregistered)]
    [TestCase(true, "T. Marlow", " sw-204817 ", "SW-204817", WaiverStanding.Signed, Description = "Values.Match: case and spacing")]
    [TestCase(true, "T. Marlow", "SW-204817", "SW-204817", WaiverStanding.Signed)]
    public void Standing_ByWhatTheDeskSawOfTheWaiver(bool handedOver, string signature, string waiverNo, string registeredNo, WaiverStanding expected)
    {
        Assert.AreEqual(expected, Strandings.Standing(handedOver, signature, waiverNo, registeredNo));
    }

    [TestCase(WaiverStanding.None, true)]
    [TestCase(WaiverStanding.Unsigned, true)]
    [TestCase(WaiverStanding.Unregistered, true)]
    [TestCase(WaiverStanding.Signed, false)]
    public void Fined_UnlessAValidSignedWaiverWasPresented(WaiverStanding waiver, bool fined)
    {
        Assert.AreEqual(fined, Strandings.Fined(waiver));
        Assert.AreEqual(fined ? 150 : 0, Strandings.Fine(waiver, 150));
        Assert.AreEqual(0, Strandings.Fine(waiver, -20), "a fine is never below 0");
    }

    [Test]
    public void WaiverStanding_KeepsItsValues()
    {
        Assert.AreEqual(0, (int)WaiverStanding.None);
        Assert.AreEqual(1, (int)WaiverStanding.Unsigned);
        Assert.AreEqual(2, (int)WaiverStanding.Unregistered);
        Assert.AreEqual(3, (int)WaiverStanding.Signed);
    }

    [Test]
    public void Lines_OnePerStranding_InOrder()
    {
        var strandings = new List<StrandingRecord>
        {
            new StrandingRecord { travellerName = "Lysimache", placeLabel = "Periclean Athens (Ancient)", day = 2 },
            null,
            new StrandingRecord { travellerName = "Hori", placeLabel = "New Kingdom Egypt (Ancient)", day = 2 }
        };
        const string template = "Stranded: {name}, lost in {place} when an Economy transponder failed.";

        CollectionAssert.AreEqual(new[]
        {
            "Stranded: Lysimache, lost in Periclean Athens (Ancient) when an Economy transponder failed.",
            "Stranded: Hori, lost in New Kingdom Egypt (Ancient) when an Economy transponder failed."
        }, Strandings.Lines(template, strandings));
        CollectionAssert.IsEmpty(Strandings.Lines(" ", strandings), "a blank template");
        CollectionAssert.IsEmpty(Strandings.Lines(template, null));
    }
}
