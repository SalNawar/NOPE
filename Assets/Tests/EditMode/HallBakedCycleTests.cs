using NUnit.Framework;

/// <summary>
/// The hall painting's four states read off the one day cycle (HallBakedCycle,
/// BUG-25b): the night's share is the cycle's evening (what the lamps, the
/// city's windows and the calendar switch on), the daylight is split into
/// morning, noon and evening by the sun's place, and the whole is continuous,
/// normalized and cyclic.
/// </summary>
public sealed class HallBakedCycleTests
{
    /// <summary>HallLightingSO's defaults: sunrise 07:00, sunset 16:30, 1.5 h twilights.</summary>
    private static readonly HallDayCycle.Settings Defaults = new HallDayCycle.Settings(7f, 16.5f, 1.5f, 0.6f, 0.1f, 4f);

    [Test]
    public void TheNightsShare_IsTheCyclesEvening_AtEveryMinute()
    {
        for (float h = 0f; h < 24f; h += 1f / 60f)
            Assert.AreEqual(HallDayCycle.Evening(h, Defaults), HallBakedCycle.Weights(h, Defaults).w, 1e-5f, $"{h:0.00}");
    }

    [TestCase(12f, 0f, 1f, 0f, 0f)]
    [TestCase(22f, 0f, 0f, 0f, 1f)]
    [TestCase(3f, 0f, 0f, 0f, 1f)]
    [TestCase(17.5f, 0f, 0f, 0f, 1f)]
    public void KeyHours(float hour, float morning, float noon, float evening, float night)
    {
        HallBakedCycle.Weights4 w = HallBakedCycle.Weights(hour, Defaults);
        Assert.AreEqual(morning, w.x, 1e-4f, "morning");
        Assert.AreEqual(noon, w.y, 1e-4f, "noon");
        Assert.AreEqual(evening, w.z, 1e-4f, "evening");
        Assert.AreEqual(night, w.w, 1e-4f, "night");
    }

    [Test]
    public void TheDay_GoesMorningThenNoonThenEvening()
    {
        Assert.That(HallBakedCycle.Weights(8f, Defaults).x, Is.GreaterThan(0.9f), "08:00 is morning");
        Assert.That(HallBakedCycle.Weights(11f, Defaults).y, Is.GreaterThan(0.9f), "11:00 is noon");
        Assert.That(HallBakedCycle.Weights(16f, Defaults).z, Is.GreaterThan(HallBakedCycle.Weights(16f, Defaults).y), "16:00 is more evening than noon");
        HallBakedCycle.Weights4 sunset = HallBakedCycle.Weights(16.5f, Defaults);
        Assert.AreEqual(0.5f, sunset.z, 1e-4f, "the sunset is half evening");
        Assert.AreEqual(0.5f, sunset.w, 1e-4f, "and half night");
    }

    [Test]
    public void WholeCycleIsContinuousNormalizedAndCyclic()
    {
        for (float h = -24f; h < 48f; h += 0.025f)
        {
            HallBakedCycle.Weights4 w = HallBakedCycle.Weights(h, Defaults);
            Assert.AreEqual(1f, w.x + w.y + w.z + w.w, 1e-5f);
            Assert.That(w.x, Is.InRange(0f, 1f)); Assert.That(w.y, Is.InRange(0f, 1f));
            Assert.That(w.z, Is.InRange(0f, 1f)); Assert.That(w.w, Is.InRange(0f, 1f));
            HallBakedCycle.Weights4 next = HallBakedCycle.Weights(h + 0.001f, Defaults);
            Assert.Less(System.Math.Abs(w.x - next.x) + System.Math.Abs(w.y - next.y) + System.Math.Abs(w.z - next.z) + System.Math.Abs(w.w - next.w), 0.005);
            HallBakedCycle.Weights4 repeat = HallBakedCycle.Weights(h + 24f, Defaults);
            Assert.AreEqual(w.x, repeat.x, 1e-4f); Assert.AreEqual(w.w, repeat.w, 1e-4f);
        }
    }

    [Test]
    public void MovingTheSunset_MovesThePaintingsNightWithIt()
    {
        var late = new HallDayCycle.Settings(7f, 19f, 1.5f, 0.6f, 0.1f, 4f);
        Assert.AreEqual(1f, HallBakedCycle.Weights(18f, Defaults).w, 1e-4f, "18:00 is night with a 16:30 sunset");
        Assert.That(HallBakedCycle.Weights(18f, late).w, Is.LessThan(0.01f), "and day with a 19:00 sunset");
    }

    [Test]
    public void TheSharedCycle_IsWhatTheOneArgumentFormReads()
    {
        HallDayCycle.Settings before = HallDayCycle.Active;
        try
        {
            HallDayCycle.Publish(new HallDayCycle.Settings(7f, 19f, 1.5f, 0.6f, 0.1f, 4f));
            Assert.That(HallBakedCycle.Weights(18f).w, Is.LessThan(0.01f));
            HallDayCycle.Publish(Defaults);
            Assert.AreEqual(1f, HallBakedCycle.Weights(18f).w, 1e-4f);
        }
        finally
        {
            HallDayCycle.Publish(before);
        }
    }

    [Test] public void BadClockDefaultsToNoon() => Assert.AreEqual(1f, HallBakedCycle.Weights(float.NaN, Defaults).y, 1e-4f);

    [Test]
    public void InfiniteClockDefaultsToNoon()
    {
        Assert.AreEqual(1f, HallBakedCycle.Weights(float.PositiveInfinity, Defaults).y, 1e-4f);
        Assert.AreEqual(1f, HallBakedCycle.Weights(float.NegativeInfinity, Defaults).y, 1e-4f);
    }
}
