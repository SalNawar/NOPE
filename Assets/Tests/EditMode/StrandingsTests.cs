using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// Cheap transponders fail (the traveller-types spec's S1-S3; redesign phase
/// 13b): one draw per accepted Economy traveller in queue order on the day's
/// own stream, the real class deciding; the morning paper's stranding lines.
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

    [Test]
    public void Lines_EachRecordsLine_InOrder_TheForgottenSilent()
    {
        var strandings = new List<StrandingRecord>
        {
            new StrandingRecord { travellerName = "Lysimache", placeLabel = "Periclean Athens (Ancient)", day = 2, fate = StrandingFate.Police, line = "TIME POLICE: an unregistered traveller was removed from Periclean Athens (Ancient)." },
            null,
            new StrandingRecord { travellerName = "Hori", placeLabel = "New Kingdom Egypt (Ancient)", day = 2, fate = StrandingFate.Forgotten },
            new StrandingRecord { travellerName = "Ines", placeLabel = "Mamluk Egypt (Medieval)", day = 2, fate = StrandingFate.Carry, line = "Stranded: Ines, lost in Mamluk Egypt (Medieval)." }
        };

        CollectionAssert.AreEqual(new[]
        {
            "TIME POLICE: an unregistered traveller was removed from Periclean Athens (Ancient).",
            "Stranded: Ines, lost in Mamluk Egypt (Medieval)."
        }, Strandings.Lines(strandings));
        CollectionAssert.IsEmpty(Strandings.Lines(null));
    }
}
