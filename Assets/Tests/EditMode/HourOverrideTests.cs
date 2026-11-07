using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// The one hour override (GATE-22, D13: "the Time cheat drives the clock, the
/// hall and the city"): the clock shows the forced hour while one is forced,
/// else its own minute; and the cheat menu's day buttons name each day's
/// opening hour (DAY-01).
/// </summary>
public class HourOverrideTests
{
    [Test]
    public void NoForcedHour_TheClockShowsItsOwnMinute()
    {
        Assert.AreEqual(791f, HourOverride.ShownMinute(null, 791f), "13:11 stays 13:11");
    }

    [Test]
    public void ForcedHour_TheClockShowsIt_WhateverTheClockSays()
    {
        Assert.AreEqual(1320f, HourOverride.ShownMinute(22f, 791f), "Time: Night shows 22:00, not 13:11 (GATE-22)");
        Assert.AreEqual("22:00", ShiftClock.Format(HourOverride.ShownMinute(22f, 791f)));
        Assert.AreEqual("09:00", ShiftClock.Format(HourOverride.ShownMinute(9f, 1000f)), "Time: Morning");
        Assert.AreEqual("18:30", ShiftClock.Format(HourOverride.ShownMinute(18.5f, 600f)), "Time: Dusk (HallDayCycle.DuskHour)");
    }

    [Test]
    public void ForcedHour_IsClampedToTheDay()
    {
        Assert.AreEqual(HourOverride.MaxMinute, HourOverride.ShownMinute(30f, 600f), "24:00 at the latest");
        Assert.AreEqual(0f, HourOverride.ShownMinute(-2f, 600f), "00:00 at the earliest");
    }

    [Test]
    public void DayHoursHint_NameEachDaysOpeningHour_InRuns()
    {
        var hours = new List<ShiftHours>();
        for (int day = 1; day <= 21; day++)
            hours.Add(day <= 7 ? new ShiftHours(540, 1020) : day <= 14 ? new ShiftHours(780, 1260) : new ShiftHours(960, 1440));
        Assert.AreEqual("Opens 09:00 days 1-7, 13:00 days 8-14, 16:00 days 15-21", DayHoursHint.Summary(hours));
        Assert.AreEqual("Day 12 opens at 13:00 (closes 21:00)", DayHoursHint.Hint(12, hours[11]));
        Assert.AreEqual("Day 21 opens at 16:00 (closes 24:00)", DayHoursHint.Hint(21, hours[20]), "the night shift's midnight is 24:00");
    }

    [Test]
    public void DayHoursHint_ARunOfOneDay_AndNoDays()
    {
        var hours = new[] { new ShiftHours(540, 1020), new ShiftHours(780, 1260), new ShiftHours(540, 1020) };
        Assert.AreEqual("Opens 09:00 day 1, 13:00 day 2, 09:00 day 3", DayHoursHint.Summary(hours));
        Assert.AreEqual(string.Empty, DayHoursHint.Summary(new ShiftHours[0]));
        Assert.AreEqual(string.Empty, DayHoursHint.Summary(null));
    }
}
