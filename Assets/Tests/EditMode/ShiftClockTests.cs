using System;
using NUnit.Framework;

public class ShiftClockTests
{
    /// <summary>09:00-17:00 over 480 real seconds = one game minute per real second.</summary>
    private static ShiftClock NineToFive() => new ShiftClock(9 * 60, 17 * 60, 480f);

    [Test]
    public void NewClock_StartsAtOpening_AndIsNotRunning()
    {
        var clock = NineToFive();
        Assert.AreEqual(540f, clock.CurrentMinute);
        Assert.AreEqual(0f, clock.Progress01);
        Assert.IsFalse(clock.IsRunning);
        Assert.IsFalse(clock.IsClosed);
    }

    [Test]
    public void Tick_BeforeStart_DoesNothing()
    {
        var clock = NineToFive();
        clock.Tick(60f);
        Assert.AreEqual(540f, clock.CurrentMinute);
    }

    [Test]
    public void Tick_AfterStart_AdvancesAtShiftRate()
    {
        var clock = NineToFive();
        clock.Start();
        clock.Tick(30f);
        Assert.AreEqual(570f, clock.CurrentMinute, 1e-3f);
        Assert.AreEqual(30f / 480f, clock.Progress01, 1e-4f);
    }

    [Test]
    public void ReachingClosing_ClampsAndFiresClosedOnce()
    {
        var clock = NineToFive();
        int closed = 0;
        clock.Closed += () => closed++;
        clock.Start();
        clock.Tick(1000f);
        clock.Tick(10f);
        Assert.AreEqual(1020f, clock.CurrentMinute);
        Assert.AreEqual(1f, clock.Progress01);
        Assert.IsTrue(clock.IsClosed);
        Assert.IsFalse(clock.IsRunning);
        Assert.AreEqual(1, closed);
    }

    [Test]
    public void Pause_HoldsTime_ResumeContinues()
    {
        var clock = NineToFive();
        clock.Start();
        clock.Pause();
        clock.Tick(30f);
        Assert.AreEqual(540f, clock.CurrentMinute);
        clock.Resume();
        clock.Tick(30f);
        Assert.AreEqual(570f, clock.CurrentMinute, 1e-3f);
    }

    [Test]
    public void NestedPauses_NeedMatchingResumes()
    {
        var clock = NineToFive();
        clock.Start();
        clock.Pause();
        clock.Pause();
        clock.Resume();
        clock.Tick(30f);
        Assert.IsTrue(clock.IsPaused);
        Assert.AreEqual(540f, clock.CurrentMinute);
        clock.Resume();
        clock.Tick(30f);
        Assert.AreEqual(570f, clock.CurrentMinute, 1e-3f);
    }

    [Test]
    public void Resume_WithoutPause_IsHarmless()
    {
        var clock = NineToFive();
        clock.Resume();
        clock.Start();
        clock.Tick(10f);
        Assert.IsFalse(clock.IsPaused);
        Assert.AreEqual(550f, clock.CurrentMinute, 1e-3f);
    }

    [Test]
    public void Stop_HaltsWithoutClosing()
    {
        var clock = NineToFive();
        int closed = 0;
        clock.Closed += () => closed++;
        clock.Start();
        clock.Tick(10f);
        clock.Stop();
        clock.Tick(1000f);
        Assert.AreEqual(550f, clock.CurrentMinute, 1e-3f);
        Assert.IsFalse(clock.IsClosed);
        Assert.AreEqual(0, closed);
    }

    [Test]
    public void Start_AfterClosing_StaysClosed()
    {
        var clock = NineToFive();
        clock.Start();
        clock.Tick(1000f);
        clock.Start();
        Assert.IsTrue(clock.IsClosed);
        Assert.IsFalse(clock.IsRunning);
    }

    [TestCase(0f)]
    [TestCase(-5f)]
    public void NonPositiveTick_IsIgnored(float seconds)
    {
        var clock = NineToFive();
        clock.Start();
        clock.Tick(seconds);
        Assert.AreEqual(540f, clock.CurrentMinute);
    }

    [TestCase(540f, "09:00")]
    [TestCase(545.9f, "09:05")]
    [TestCase(1020f, "17:00")]
    [TestCase(0f, "00:00")]
    [TestCase(1439.5f, "23:59")]
    public void Format_IsZeroPaddedTwentyFourHour(float minute, string expected)
    {
        Assert.AreEqual(expected, ShiftClock.Format(minute));
    }

    [TestCase(180f, 90f, 0f)]      // 03:00
    [TestCase(570f, 285f, 180f)]   // 09:30
    [TestCase(945f, 112.5f, 270f)] // 15:45
    [TestCase(720f, 0f, 0f)]       // 12:00
    public void HandAngles_AreClockwiseDegreesFromTwelve(float minute, float hourDeg, float minuteDeg)
    {
        (float hour, float min) = ShiftClock.HandAngles(minute);
        Assert.AreEqual(hourDeg, hour, 1e-3f);
        Assert.AreEqual(minuteDeg, min, 1e-3f);
    }

    [Test]
    public void Constructor_RejectsInvalidShifts()
    {
        Assert.Throws<ArgumentException>(() => new ShiftClock(600, 600, 60f));
        Assert.Throws<ArgumentException>(() => new ShiftClock(600, 540, 60f));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ShiftClock(540, 1020, 0f));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ShiftClock(-1, 1020, 60f));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ShiftClock(540, 1441, 60f));
    }
}
