using NUnit.Framework;

/// <summary>
/// One document's written reveal (piece 10 X18), shared by its desk paper and
/// its scanned copy: NaN before the first sighting (piece 9's "not yet
/// revealed"), then seconds since it; the first start wins, so a second
/// sighting never replays; a finish (a row click) shows English at once from
/// then on, and does nothing before a start.
/// </summary>
public class RevealClockTests
{
    [Test]
    public void NaNBeforeStart()
    {
        var clock = new RevealClock();
        Assert.IsFalse(clock.Started);
        Assert.IsTrue(float.IsNaN(clock.Elapsed(10f)));
    }

    [Test]
    public void FirstStartWins()
    {
        var clock = new RevealClock();
        clock.Start(5f);
        clock.Start(9f);
        Assert.IsTrue(clock.Started);
        Assert.AreEqual(5f, clock.Elapsed(10f), 1e-5f, "a later sighting never replays");
    }

    [Test]
    public void ElapsedCounts()
    {
        var clock = new RevealClock();
        clock.Start(2f);
        Assert.AreEqual(0f, clock.Elapsed(2f), 1e-5f);
        Assert.AreEqual(1.25f, clock.Elapsed(3.25f), 1e-5f);
    }

    [Test]
    public void FinishIsInfinity()
    {
        var clock = new RevealClock();
        clock.Start(2f);
        clock.Finish();
        Assert.IsTrue(float.IsPositiveInfinity(clock.Elapsed(2.1f)));
        Assert.IsTrue(clock.Started);
        clock.Start(50f);
        Assert.IsTrue(float.IsPositiveInfinity(clock.Elapsed(60f)), "a finished reveal stays finished");
    }

    [Test]
    public void FinishBeforeStart_Nothing()
    {
        var clock = new RevealClock();
        clock.Finish();
        Assert.IsFalse(clock.Started);
        Assert.IsTrue(float.IsNaN(clock.Elapsed(1f)));
        clock.Start(4f);
        Assert.AreEqual(1f, clock.Elapsed(5f), 1e-5f, "a later start still counts");
    }
}
