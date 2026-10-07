using NUnit.Framework;

/// <summary>
/// The hours grow (night shifts, 2026-10-07): each day's desk hours
/// (ShiftHours, world_source.json days[].shiftStart / shiftEnd, else
/// GameConfigSO's), the clock face they give the shift, the hour each visual
/// system reads from it, the briefing's announcement, the art's evening curve
/// on the standard day and Home's lateness.
/// </summary>
public class ShiftHoursTests
{
    private const float Tolerance = 1e-4f;

    /// <summary>GameConfigSO's standard day: 09:00 to 17:00.</summary>
    private static readonly ShiftHours Standard = new ShiftHours(9 * 60, 17 * 60);

    /// <summary>The HallLightingSO defaults (sunrise 07:00, sunset 16:30, 1.5 h twilights; HallDayCycleTests).</summary>
    private static readonly HallDayCycle.Settings Lights = new HallDayCycle.Settings(7f, 16.5f, 1.5f, 0.6f, 0.1f, 4f);

    [TestCase("09:00", 540)]
    [TestCase("13:00", 780)]
    [TestCase("16:30", 990)]
    [TestCase("00:00", 0)]
    [TestCase("24:00", 1440)]
    [TestCase(" 21:00 ", 1260)]
    public void TryParse_ReadsTwentyFourHourTimes_UpToMidnightAsTwentyFour(string text, int minute)
    {
        Assert.IsTrue(ShiftHours.TryParse(text, out int parsed), text);
        Assert.AreEqual(minute, parsed, text);
    }

    [TestCase("")]
    [TestCase(null)]
    [TestCase("9:00")]
    [TestCase("24:01")]
    [TestCase("25:00")]
    [TestCase("12:60")]
    [TestCase("12-00")]
    [TestCase("noon")]
    [TestCase("-1:00")]
    public void TryParse_RefusesAnythingElse(string text)
    {
        Assert.IsFalse(ShiftHours.TryParse(text, out _), text ?? "null");
    }

    [Test]
    public void TheClockFace_PrintsMidnightAsZeroHundred()
    {
        var night = new ShiftHours(16 * 60, 24 * 60);
        Assert.AreEqual("16:00", night.Open);
        Assert.AreEqual("00:00", night.Close, "the night shift closes at 24:00, shown 00:00");
        Assert.AreEqual(480, night.LengthMinutes);
        Assert.AreEqual("09:00", Standard.Open);
        Assert.AreEqual("17:00", Standard.Close);
    }

    [Test]
    public void ADayWithoutHours_KeepsTheStandardDay_ADayWithHours_KeepsItsOwn()
    {
        Assert.AreEqual(Standard, ShiftHours.For(-1, -1, Standard));
        Assert.AreEqual(new ShiftHours(780, 1260), ShiftHours.For(780, 1260, Standard));
        Assert.AreEqual(new ShiftHours(960, 1440), ShiftHours.For(960, 1440, Standard));
    }

    [Test]
    public void TheClock_RunsTheDaysOwnSpan_InTheSameRealTime()
    {
        // Day 12: 16:00 to midnight over the standard 480 real seconds, a game minute a second as on day 1.
        var hours = new ShiftHours(960, 1440);
        var clock = new ShiftClock(hours.StartMinute, hours.EndMinute, 480f);
        bool closed = false;
        clock.Closed += () => closed = true;
        clock.Start();
        Assert.AreEqual("16:00", ShiftClock.Format(clock.CurrentMinute));
        clock.Tick(240f);
        Assert.AreEqual("20:00", ShiftClock.Format(clock.CurrentMinute), "half the shift");
        Assert.AreEqual(0.5f, clock.Progress01, Tolerance);
        clock.Tick(210f);
        Assert.AreEqual("23:30", ShiftClock.Format(clock.CurrentMinute));
        Assert.IsFalse(closed);
        clock.Tick(30f);
        Assert.IsTrue(closed, "the desk closes at midnight");
        Assert.AreEqual(1440f, clock.CurrentMinute);
        Assert.AreEqual("00:00", ShiftClock.Format(clock.CurrentMinute));
    }

    [Test]
    public void EveryVisualSystem_ReadsTheRealHour_SoALateShiftGetsDark()
    {
        // The hall's lights and the city read HallDayCycle.Hour of the clock's minute; the baked four states and the city's paintings HallBakedCycle.Weights of that hour.
        Assert.AreEqual(1f, HallBakedCycle.Weights(HallDayCycle.Hour(1440f)).w, Tolerance, "midnight is full night");
        Assert.AreEqual(1f, HallBakedCycle.Weights(HallDayCycle.Hour(23.5f * 60f)).w, Tolerance, "23:30 is full night");
        Assert.AreEqual(1f, HallBakedCycle.Weights(HallDayCycle.Hour(21f * 60f)).w, Tolerance, "day 8 closes at night");
        Assert.AreEqual(1f, HallBakedCycle.Weights(HallDayCycle.Hour(13f * 60f)).y + HallBakedCycle.Weights(HallDayCycle.Hour(13f * 60f)).z, Tolerance, "day 8 opens between noon and evening");
        Assert.AreEqual(1f, HallBakedCycle.Weights(HallDayCycle.Hour(16.5f * 60f)).z, Tolerance, "day 12 opens at 16:00, in the evening by 16:30");
        Assert.AreEqual(1f, HallDayCycle.Evening(HallDayCycle.Hour(1440f), Lights), Tolerance);
        Assert.AreEqual(1f, HallDayCycle.Evening(HallDayCycle.Hour(21f * 60f), Lights), Tolerance);
        Assert.AreEqual(0f, HallDayCycle.Evening(HallDayCycle.Hour(13f * 60f), Lights), Tolerance);
    }

    [Test]
    public void TheArtsEveningCurve_ReadsTheHourOnTheStandardDay()
    {
        Assert.AreEqual(0f, ShiftHours.StandardProgress(540f, Standard), Tolerance);
        Assert.AreEqual(0.5f, ShiftHours.StandardProgress(780f, Standard), Tolerance, "13:00 is halfway through the standard day");
        Assert.AreEqual(1f, ShiftHours.StandardProgress(1020f, Standard), Tolerance);
        Assert.AreEqual(1f, ShiftHours.StandardProgress(1440f, Standard), Tolerance, "after the standard closing it stays evening");
        Assert.AreEqual(0f, ShiftHours.StandardProgress(300f, Standard), Tolerance);
        Assert.AreEqual(0f, ShiftHours.StandardProgress(float.NaN, Standard), Tolerance);

        // The crowds' curve (CrowdPaletteBlend, darkening from 0.5 to 0.9): a night shift opens nearly in full evening.
        Assert.AreEqual(1f, CrowdPaletteBlend.Evening(ShiftHours.StandardProgress(16f * 60f, Standard), 0.5f, 0.9f), 0.02f);
        Assert.AreEqual(0f, CrowdPaletteBlend.Evening(ShiftHours.StandardProgress(13f * 60f, Standard), 0.5f, 0.9f), Tolerance);
    }

    [Test]
    public void TheBriefingAnnounces_OnlyADayWhoseHoursChanged()
    {
        Assert.IsFalse(ShiftHours.Announces(null, Standard), "day 1 has no yesterday");
        Assert.IsFalse(ShiftHours.Announces(Standard, Standard));
        Assert.IsTrue(ShiftHours.Announces(Standard, new ShiftHours(780, 1260)), "day 8");
        Assert.IsFalse(ShiftHours.Announces(new ShiftHours(780, 1260), new ShiftHours(780, 1260)));
        Assert.IsTrue(ShiftHours.Announces(new ShiftHours(780, 1260), new ShiftHours(960, 1440)), "day 12");
    }

    [Test]
    public void HomesLateness_GrowsFromTheStandardClosingToMidnight()
    {
        Assert.AreEqual(0f, ShiftHours.Lateness(1020, Standard), Tolerance);
        Assert.AreEqual(0f, ShiftHours.Lateness(900, Standard), Tolerance);
        Assert.AreEqual(4f / 7f, ShiftHours.Lateness(1260, Standard), Tolerance, "21:00");
        Assert.AreEqual(1f, ShiftHours.Lateness(1440, Standard), Tolerance, "midnight is deep night");
        Assert.AreEqual(1f, ShiftHours.Lateness(1440, new ShiftHours(0, 1440)), Tolerance, "a standard day closing at midnight");
    }

    [Test]
    public void Problems_AcceptsAValidDay_OrNoHoursAtAll()
    {
        CollectionAssert.IsEmpty(ShiftHours.Problems("Day 8", 780, 1260, 4, 12));
        CollectionAssert.IsEmpty(ShiftHours.Problems("Day 12", 960, 1440, 4, 12));
        CollectionAssert.IsEmpty(ShiftHours.Problems("Day 1", -1, -1, 4, 12));
    }

    [TestCase(780, -1, "both")]
    [TestCase(-1, 1260, "both")]
    [TestCase(1260, 780, "before")]
    [TestCase(780, 780, "before")]
    [TestCase(960, 1500, "24:00")]
    [TestCase(1440, 1440, "before")]
    [TestCase(780, 840, "at least 4")]
    [TestCase(0, 1440, "at most 12")]
    public void Problems_NamesTheDay_AndWhatIsWrong(int start, int end, string words)
    {
        var problems = ShiftHours.Problems("Day 9", start, end, 4, 12);
        Assert.IsNotEmpty(problems, $"{start}-{end}");
        StringAssert.Contains("Day 9", problems[0]);
        StringAssert.Contains(words, string.Join(" ", problems));
    }
}
