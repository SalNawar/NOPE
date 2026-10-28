using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using NUnit.Framework;

/// <summary>
/// A cheat's day jump and the desk hours, over every day of the run (Saleh
/// 2026-10-28: "switching days with cheats start the day at the wrong time"),
/// read from the content source as Generate World reads it. Every day runs
/// its own plan (DayPlans.Pick), so a jump lands on the day's own hours, as a
/// played day does: 09:00 in week one, 13:00 in week two, 16:00 in week three.
/// The clerk who jumps never saw the days between, so the briefing names the
/// new hours whenever they differ from the hours of the day the jump left
/// (ShiftHours.ComparedDay, RunManager.LeftDeskDay); played in order, it
/// names them on days 8 and 15 only, as before.
/// </summary>
public class JumpHoursTests
{
    private const string SourcePath = "Assets/Data/World/world_source.json";

    /// <summary>The run's last day (the three-week progression).</summary>
    private const int LastDay = 21;

    /// <summary>GameConfigSO's standard day (shiftStartHour 9 to shiftEndHour 17), which a day without authored hours keeps.</summary>
    private static readonly ShiftHours Standard = new ShiftHours(540, 1020);

    private static string Read(string relative, [CallerFilePath] string here = "")
    {
        string path = File.Exists(relative) ? relative : Path.Combine(Path.GetDirectoryName(here), "..", "..", "..", relative);
        return File.ReadAllText(path).Replace("\r\n", "\n");
    }

    /// <summary>Each authored day's number and hours (its shiftStart and shiftEnd, else the standard day), as Generate World writes the day plans.</summary>
    private static (int[] numbers, ShiftHours[] hours) Plans()
    {
        var numbers = new List<int>();
        var hours = new List<ShiftHours>();
        foreach (ContentNode d in ContentJson.Parse(Read(SourcePath)).Get("days").Items)
        {
            numbers.Add(int.Parse(d.Get("day").Text));
            int start = -1, end = -1;
            string open = d.Get("shiftStart")?.Text, close = d.Get("shiftEnd")?.Text;
            if (!string.IsNullOrWhiteSpace(open))
                Assert.IsTrue(ShiftHours.TryParse(open, out start), $"day {numbers[numbers.Count - 1]}'s shiftStart '{open}'");
            if (!string.IsNullOrWhiteSpace(close))
                Assert.IsTrue(ShiftHours.TryParse(close, out end), $"day {numbers[numbers.Count - 1]}'s shiftEnd '{close}'");
            hours.Add(ShiftHours.For(start, end, Standard));
        }
        return (numbers.ToArray(), hours.ToArray());
    }

    /// <summary>The hours day <paramref name="day"/> opens with: its plan's (DayPlans.Pick), as GameManager builds the clock, whether the day was played into or jumped to.</summary>
    private static ShiftHours HoursOf((int[] numbers, ShiftHours[] hours) plans, int day)
    {
        int i = DayPlans.Pick(plans.numbers, day);
        Assert.GreaterOrEqual(i, 0, $"day {day} has a plan");
        return plans.hours[i];
    }

    /// <summary>True when day <paramref name="today"/>'s briefing names its hours after the run left the desk of day <paramref name="leftDesk"/> (0: none known), as GameManager.TodaysBulletin decides it.</summary>
    private static bool Names((int[] numbers, ShiftHours[] hours) plans, int today, int leftDesk)
    {
        int compared = ShiftHours.ComparedDay(today, leftDesk);
        ShiftHours? earlier = compared >= 1 ? HoursOf(plans, compared) : (ShiftHours?)null;
        return ShiftHours.Announces(earlier, HoursOf(plans, today));
    }

    [Test]
    public void EveryDay_RunsItsOwnPlan_AndOpensAtItsWeeksHour()
    {
        var plans = Plans();
        for (int day = 1; day <= LastDay; day++)
        {
            Assert.AreEqual(day, plans.numbers[DayPlans.Pick(plans.numbers, day)], $"day {day} runs its own plan, so a jump to it opens at its own hours");
            int opens = day <= 7 ? 540 : day <= 14 ? 780 : 960;
            Assert.AreEqual(opens, HoursOf(plans, day).StartMinute, $"day {day} opens at {ShiftClock.Format(opens)}");
        }
    }

    [Test]
    public void AJumpBetweenAnyTwoDays_NamesTheHours_ExactlyWhenTheyDifferFromTheDayItLeft()
    {
        var plans = Plans();
        for (int to = 1; to <= LastDay; to++)
            for (int from = 1; from <= LastDay; from++)
            {
                ShiftHours today = HoursOf(plans, to);
                // A jump to today is a restart: the briefing is the played day's own (yesterday's hours, none on day 1).
                bool named = from == to ? to > 1 && !HoursOf(plans, to - 1).Equals(today) : !HoursOf(plans, from).Equals(today);
                Assert.AreEqual(named, Names(plans, to, from), $"a jump from day {from} ({HoursOf(plans, from)}) to day {to} ({today})");
            }
    }

    [Test]
    public void TheJumpsSalehMade_NameTheNewHours()
    {
        var plans = Plans();
        // His playtest's Player.log: day 1 to day 11, then day 11 to day 20.
        Assert.IsTrue(Names(plans, 11, 1), "day 11 opens at 13:00 after day 1's 09:00");
        Assert.IsTrue(Names(plans, 20, 11), "day 20 opens at 16:00 after day 11's 13:00");
        Assert.IsFalse(Names(plans, 20, 16), "day 16 already opened at 16:00");
        Assert.IsTrue(Names(plans, 3, 20), "a jump back to week one opens at 09:00 again");
    }

    [Test]
    public void PlayedInOrder_TheBriefingNamesTheHours_OnDays8And15Only()
    {
        var plans = Plans();
        var named = new List<int>();
        for (int day = 1; day <= LastDay; day++)
            if (Names(plans, day, day - 1))
                named.Add(day);
        CollectionAssert.AreEqual(new[] { 8, 15 }, named);
    }

    [Test]
    public void TheComparedDay_IsTheDeskLeft_ElseYesterday()
    {
        Assert.AreEqual(0, ShiftHours.ComparedDay(1, 0), "a new run's day 1: no earlier desk, no hours to compare");
        Assert.AreEqual(10, ShiftHours.ComparedDay(11, 0), "a continued run: yesterday");
        Assert.AreEqual(10, ShiftHours.ComparedDay(11, 10), "a night's sleep: yesterday");
        Assert.AreEqual(1, ShiftHours.ComparedDay(11, 1), "a jump from day 1");
        Assert.AreEqual(20, ShiftHours.ComparedDay(3, 20), "a jump back from day 20");
        Assert.AreEqual(7, ShiftHours.ComparedDay(8, 8), "a restart of today: yesterday");
    }
}
