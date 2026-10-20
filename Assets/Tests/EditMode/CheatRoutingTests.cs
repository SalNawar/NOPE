using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// RUN-SAFE's cheat routing (BUG-27a, BUG-28a, BUG-56b): a skip, a jump and a
/// restart run the same steps play runs (the shift's close, Home's night on
/// the default care, Sleep, the morning), a jump back replays from a new
/// world, a restart restores the morning; Sleep checks the endings after the
/// night's resolve as well as before it; every rewind names what it loses.
/// The ledger half (money, citations, the pet, orders and premades equal a
/// played day field by field) needs the content library: CheatLedgerTests,
/// in the Editor tests, run in Unity.
/// </summary>
public class CheatRoutingTests
{
    private static readonly RunStep[] Day = { RunStep.CloseShift, RunStep.HomeNight, RunStep.Sleep };

    private static List<RunStep> Concat(params IEnumerable<RunStep>[] parts)
    {
        var all = new List<RunStep>();
        foreach (IEnumerable<RunStep> p in parts)
            all.AddRange(p);
        return all;
    }

    [Test]
    public void SkipDuringTheShift_IsTheShiftsClose_ThenHomesNight_ThenSleep_ThenTheMorning()
    {
        CollectionAssert.AreEqual(Concat(Day, new[] { RunStep.OpenMorning }), RunRoutes.Skip(shiftClosed: false));
    }

    [Test]
    public void SkipAfterTheShiftClosed_NeverClosesItTwice()
    {
        CollectionAssert.AreEqual(new[] { RunStep.HomeNight, RunStep.Sleep, RunStep.OpenMorning }, RunRoutes.Skip(shiftClosed: true));
    }

    [Test]
    public void JumpForward_PlaysEveryNightBetween_OnTheDefaultCare()
    {
        List<RunStep> route = RunRoutes.Jump(today: 2, target: 5, lastDay: 15, shiftClosed: false);
        CollectionAssert.AreEqual(Concat(Day, Day, Day, new[] { RunStep.OpenMorning }), route, "days 2, 3 and 4 close and sleep; day 5 opens");
    }

    [Test]
    public void JumpForward_FromHome_SkipsTheCloseOfAShiftAlreadyClosed()
    {
        List<RunStep> route = RunRoutes.Jump(today: 6, target: 7, lastDay: 15, shiftClosed: true);
        CollectionAssert.AreEqual(new[] { RunStep.HomeNight, RunStep.Sleep, RunStep.OpenMorning }, route);
    }

    [Test]
    public void JumpBack_StartsANewWorld_ThenPlaysEachNightUpToTheDay()
    {
        List<RunStep> route = RunRoutes.Jump(today: 6, target: 3, lastDay: 15, shiftClosed: false);
        CollectionAssert.AreEqual(Concat(new[] { RunStep.NewWorld }, Day, Day, new[] { RunStep.OpenMorning }), route, "days 1 and 2 are played through, day 3 opens");
        CollectionAssert.AreEqual(new[] { RunStep.NewWorld, RunStep.OpenMorning }, RunRoutes.Jump(6, 1, 15, false), "back to day 1: the new world's own morning");
    }

    [Test]
    public void JumpToToday_IsARestart()
    {
        CollectionAssert.AreEqual(RunRoutes.Restart(), RunRoutes.Jump(today: 4, target: 4, lastDay: 15, shiftClosed: true));
    }

    [Test]
    public void Restart_RestoresTheMorningSnapshot_ThenOpensIt()
    {
        CollectionAssert.AreEqual(new[] { RunStep.RestoreMorning, RunStep.OpenMorning }, RunRoutes.Restart());
    }

    [Test]
    public void EveryRoute_EndsAtTheMorningOpen_AndNoNightRunsWithoutTheShiftClosingFirst()
    {
        for (int today = 1; today <= 15; today++)
            for (int target = 1; target <= 15; target++)
                foreach (bool closed in new[] { false, true })
                {
                    List<RunStep> route = RunRoutes.Jump(today, target, 15, closed);
                    Assert.AreEqual(RunStep.OpenMorning, route[route.Count - 1], $"{today}->{target}");
                    bool open = !closed || route.Contains(RunStep.NewWorld) || route.Contains(RunStep.RestoreMorning);
                    foreach (RunStep step in route)
                    {
                        if (step == RunStep.CloseShift)
                            open = false;
                        if (step == RunStep.HomeNight)
                            Assert.IsFalse(open, $"{today}->{target}: Home's night before the shift closed");
                        if (step == RunStep.Sleep)
                            open = true;
                    }
                }
    }

    [Test]
    public void TheTarget_IsClampedToTheRunsDays()
    {
        Assert.AreEqual(15, RunRoutes.Clamp(40, 15));
        Assert.AreEqual(1, RunRoutes.Clamp(-3, 15));
        Assert.AreEqual(9, RunRoutes.Clamp(9, 0), "no last day known: the target stands");
        CollectionAssert.AreEqual(RunRoutes.Jump(14, 15, 15, false), RunRoutes.Jump(14, 99, 15, false));
    }

    [Test]
    public void Sleep_ChecksTheEndingsAfterTheNightsResolve_SoANightEventFiresTheClerkThatNight()
    {
        var steps = new List<string>();
        string ending = NightOrder.Run<string>(
            () => { steps.Add("check before"); return null; },
            e => steps.Add("end run " + e),
            () => steps.Add("night"),
            () => { steps.Add("check after"); return "fired"; },
            () => steps.Add("turn day"));
        Assert.AreEqual("fired", ending);
        CollectionAssert.AreEqual(new[] { "check before", "night", "check after" }, steps, "the night's failure ends the run before the day turns");
    }

    [Test]
    public void Sleep_WithNoEnding_TurnsTheDayAfterTheNight()
    {
        var steps = new List<string>();
        string ending = NightOrder.Run<string>(() => null, e => steps.Add("end run"), () => steps.Add("night"), () => null, () => steps.Add("turn day"));
        Assert.IsNull(ending);
        CollectionAssert.AreEqual(new[] { "night", "turn day" }, steps);
    }

    [Test]
    public void Sleep_OnAnEndingBeforeTheNight_EndsTheRun_AndTheDayNeverTurns()
    {
        var steps = new List<string>();
        string ending = NightOrder.Run<string>(() => "last day", e => steps.Add("end run " + e), () => steps.Add("night"), () => "never asked", () => steps.Add("turn day"));
        Assert.AreEqual("last day", ending);
        CollectionAssert.AreEqual(new[] { "end run last day" }, steps, "the run's last night is EndRun's (DayCycle.EndRun resolves it)");
    }

    [Test]
    public void ARestartsWarning_NamesTodaysDecisions_Money_AndCitations()
    {
        string line = RewindWarnings.Restart(day: 6, decisions: 3, moneyChange: 45, citations: 1);
        StringAssert.Contains("day 6", line);
        StringAssert.Contains("3 decision", line);
        StringAssert.Contains("+45", line);
        StringAssert.Contains("1 citation", line);
        StringAssert.Contains("-20", RewindWarnings.Restart(2, 0, -20, 0));
    }

    [Test]
    public void AJumpBacksWarning_NamesTheDaysLost()
    {
        string line = RewindWarnings.JumpBack(today: 6, target: 3);
        StringAssert.Contains("day 3", line);
        StringAssert.Contains("days 3-6", line);
        StringAssert.Contains("day 3", RewindWarnings.JumpBack(3, 3), "the same day: today alone");
    }

    [Test]
    public void OnlyARewind_NeedsAWarning()
    {
        Assert.AreEqual(Rewind.JumpBack, RewindWarnings.Of(today: 6, target: 3));
        Assert.AreEqual(Rewind.Restart, RewindWarnings.Of(6, 6));
        Assert.AreEqual(Rewind.None, RewindWarnings.Of(6, 9));
    }
}
