using System;
using NUnit.Framework;

/// <summary>
/// The daters' date wheels (the desk machine spec §1): day (31 notches),
/// month (12) and year (a decade band, 10). Each morning they still show
/// yesterday; the first pick-up rolls each wheel forward to today, one
/// ratchet click per notch (a wheel only turns forward).
/// </summary>
public class DaterWheelsTests
{
    private static DateTime D(int day, int month, int year) => new DateTime(year, month, day);

    [Test]
    public void ADate_SetsEachWheelsNotch()
    {
        WheelSetting w = DaterWheels.Notches(D(14, 3, 2150));
        Assert.AreEqual(13, w.Day);
        Assert.AreEqual(2, w.Month);
        Assert.AreEqual(0, w.Year);
        Assert.AreEqual(9, DaterWheels.Notches(D(31, 12, 2159)).Year);
    }

    [Test]
    public void TheMorningDate_IsYesterday()
    {
        Assert.AreEqual(D(13, 3, 2150), DaterWheels.Shown(D(14, 3, 2150)));
    }

    [Test]
    public void OneDayOn_RollsTheDayWheelOneClick()
    {
        WheelSetting s = DaterWheels.Steps(D(14, 3, 2150), D(15, 3, 2150));
        Assert.AreEqual((1, 0, 0), (s.Day, s.Month, s.Year));
        Assert.AreEqual(1, s.Clicks);
    }

    [Test]
    public void TheMonthTurns_TheDayWheelWrapsForward()
    {
        WheelSetting s = DaterWheels.Steps(D(31, 3, 2150), D(1, 4, 2150));
        Assert.AreEqual((1, 1, 0), (s.Day, s.Month, s.Year));
    }

    [Test]
    public void AShortMonth_RollsPastTheUnusedDays()
    {
        WheelSetting s = DaterWheels.Steps(D(28, 2, 2151), D(1, 3, 2151));
        Assert.AreEqual(4, s.Day, "28 -> 29, 30, 31, 1");
        Assert.AreEqual(1, s.Month);
        Assert.AreEqual(5, s.Clicks);
    }

    [Test]
    public void TheDecadeTurns_TheYearBandWraps()
    {
        WheelSetting s = DaterWheels.Steps(D(31, 12, 2159), D(1, 1, 2160));
        Assert.AreEqual((1, 1, 1), (s.Day, s.Month, s.Year));
    }

    [Test]
    public void TheSameDate_NeedsNoRoll()
    {
        Assert.AreEqual(0, DaterWheels.Steps(D(14, 3, 2150), D(14, 3, 2150)).Clicks);
    }

    [TestCase(5, 5, 31, 0)]
    [TestCase(5, 6, 31, 1)]
    [TestCase(30, 0, 31, 1)]
    [TestCase(11, 0, 12, 1)]
    [TestCase(3, 2, 10, 9)]
    public void AWheel_OnlyTurnsForward(int from, int to, int notches, int expected)
    {
        Assert.AreEqual(expected, DaterWheels.Forward(from, to, notches));
    }

    [Test]
    public void TheDayWheel_SetsTheMonthsLength()
    {
        Assert.AreEqual(DaterWheels.DayNotches, 31);
        Assert.AreEqual(DaterWheels.MonthNotches, 12);
        Assert.AreEqual(DaterWheels.YearNotches, 10);
    }
}
