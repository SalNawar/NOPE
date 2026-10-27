using System;
using System.Collections.Generic;

/// <summary>One step of a day-changing cheat's route (RunRoutes), each run by RunManager with the same code play runs.</summary>
public enum RunStep
{
    /// <summary>Today's shift closes as played so far (DayCycle.EndShift on the shift's ledger), with the ending check play runs after it.</summary>
    CloseShift,

    /// <summary>A day the cheat passes over is played: its whole queue decided correctly, as the cheat menu's Auto-decide does, then its close (DayCycle.PlayShiftCorrectly).</summary>
    PlayShift,

    /// <summary>Home's night on the default care (DayCycle.HomeNight: the break-in and the fixed bill, the default care's bills, the pet's night).</summary>
    HomeNight,

    /// <summary>Sleep (DayCycle.Sleep, NightOrder: the endings before and after the night's resolve, then the next morning).</summary>
    Sleep,

    /// <summary>A new world with the run's seed and its pet (a jump back).</summary>
    NewWorld,

    /// <summary>Today's morning snapshot comes back (a restart).</summary>
    RestoreMorning,

    /// <summary>The landing: the day-scoped switches cleared, the morning saved, the office opened at the briefing.</summary>
    OpenMorning
}

/// <summary>
/// The routes of the cheat menu's day changes (RUN-SAFE, BUG-28a: "cheats
/// route through the same close and night code as play"). A skip closes
/// today's shift as played (unless play closed it already), plays Home's
/// night on the default care, sleeps and opens the next morning; a jump
/// forward does the same for today, then plays every day before the target
/// (its whole queue decided correctly, as the cheat menu's Auto-decide plays
/// it, so the run earns its pay and the economy stays a played run's), each
/// with its night; a jump back starts a new world with the run's seed and pet
/// and plays each day up to the target the same way; a jump to today is a
/// restart, which restores today's morning snapshot. RunManager runs the
/// steps in order and stops where one ends the run. Pure, so the routes are
/// tested headless.
/// </summary>
public static class RunRoutes
{
    /// <summary>The cheat menu's "Skip day": today's close as played (unless <paramref name="shiftClosed"/>: play closed it, the run is at Home), Home's night, Sleep, the next morning.</summary>
    public static List<RunStep> Skip(bool shiftClosed)
    {
        var route = new List<RunStep>();
        AddDay(route, shiftClosed ? (RunStep?)null : RunStep.CloseShift);
        route.Add(RunStep.OpenMorning);
        return route;
    }

    /// <summary>
    /// The cheat menu's "Jump to day N" from <paramref name="today"/> to
    /// <paramref name="target"/> (Clamp to the run's <paramref name="lastDay"/>):
    /// today itself is a restart; forward, today's close as played (unless
    /// <paramref name="shiftClosed"/>) and its night, then every day before the
    /// target played (PlayShift) with its night; back, a new world with every
    /// day from day 1 to the target's eve played the same way. Each ends at
    /// the target's morning.
    /// </summary>
    public static List<RunStep> Jump(int today, int target, int lastDay, bool shiftClosed)
    {
        target = Clamp(target, lastDay);
        if (target == today)
            return Restart();

        var route = new List<RunStep>();
        int day = today;
        if (target < today)
        {
            route.Add(RunStep.NewWorld);
            day = 1;
        }
        else
        {
            AddDay(route, shiftClosed ? (RunStep?)null : RunStep.CloseShift);
            day++;
        }
        for (; day < target; day++)
            AddDay(route, RunStep.PlayShift);
        route.Add(RunStep.OpenMorning);
        return route;
    }

    /// <summary>The cheat menu's "Restart shift": today's morning snapshot back, then its morning opened.</summary>
    public static List<RunStep> Restart() => new List<RunStep> { RunStep.RestoreMorning, RunStep.OpenMorning };

    /// <summary>A jump's target within the run's days (1 to <paramref name="lastDay"/>); with no last day known (0 or less) the target stands, at least 1.</summary>
    public static int Clamp(int target, int lastDay) => Math.Max(1, lastDay > 0 ? Math.Min(target, lastDay) : target);

    /// <summary>A day through: its <paramref name="shift"/> step (none when play closed it already), Home's night, Sleep.</summary>
    private static void AddDay(List<RunStep> route, RunStep? shift)
    {
        if (shift.HasValue)
            route.Add(shift.Value);
        route.Add(RunStep.HomeNight);
        route.Add(RunStep.Sleep);
    }
}

/// <summary>
/// Sleep's order (RUN-SAFE, BUG-56b, CODE-99): the day-boundary ending check
/// first (failures, and the run's last day, which still gets its own night
/// resolve: EndRun); else the night's resolve, then the failures checked again
/// on the world the night left, so a night story event that pushes the run
/// past a line ends it that night, never at the next morning's first correct
/// stamp; else the day turns. Pure over its steps, so the order is tested
/// headless; DayCycle.Sleep runs it with the game's own steps.
/// </summary>
public static class NightOrder
{
    /// <summary>Runs the night: <paramref name="boundaryEnding"/> (an ending there goes to <paramref name="endRun"/>), else <paramref name="resolveNight"/>, then <paramref name="failureAfterNight"/>, else <paramref name="turnDay"/>. Returns the ending, or null when the day turned.</summary>
    public static T Run<T>(Func<T> boundaryEnding, Action<T> endRun, Action resolveNight, Func<T> failureAfterNight, Action turnDay) where T : class
    {
        T ending = boundaryEnding();
        if (ending != null)
        {
            endRun(ending);
            return ending;
        }

        resolveNight();
        ending = failureAfterNight();
        if (ending != null)
            return ending;

        turnDay();
        return null;
    }
}

/// <summary>What a day-changing cheat rewinds (RewindWarnings.Of).</summary>
public enum Rewind
{
    /// <summary>Nothing played is lost (a jump forward).</summary>
    None,

    /// <summary>Today goes back to its morning.</summary>
    Restart,

    /// <summary>The run goes back to an earlier day.</summary>
    JumpBack
}

/// <summary>
/// The cheat menu's warnings before a rewind (RUN-SAFE, BUG-28: "Rewinding
/// cheats warn first"): a restart, a jump to an earlier day and the
/// appearance cheat each name what will be lost, and the overlay acts only
/// on a confirm. The cheat menu is a developer tool, so its lines are plain
/// English like the rest of the overlay. Pure, so the lines are tested
/// headless.
/// </summary>
public static class RewindWarnings
{
    /// <summary>What a day change from <paramref name="today"/> to <paramref name="target"/> rewinds: an earlier day, today (a restart), or nothing.</summary>
    public static Rewind Of(int today, int target) => target < today ? Rewind.JumpBack : target == today ? Rewind.Restart : Rewind.None;

    /// <summary>A restart of <paramref name="day"/>: today's decisions, the money since the morning and today's citations are lost, and any cheat change made since the morning.</summary>
    public static string Restart(int day, int decisions, int moneyChange, int citations) =>
        $"Restart day {day} from its morning? Lost: {decisions} decision(s) today, {moneyChange:+0;-0;0} credits since the morning, " +
        $"{citations} citation(s), and every cheat change made since the morning.";

    /// <summary>A jump from <paramref name="today"/> back to an earlier <paramref name="target"/> (a jump to today is a restart: Restart's warning): the days from the target to today as played are lost; the run replays from day 1 with its seed and pet.</summary>
    public static string JumpBack(int today, int target) =>
        $"Jump back to day {target}? Lost: days {target}-{today} as played. The run replays from day 1 with its seed and pet, each day's travellers decided correctly and each night on the default care.";
}

/// <summary>What the cheat menu's "End shift" does now (EndShiftRule.Decide).</summary>
public enum EndShiftAction
{
    /// <summary>No shift to end: it has not started, or it is over.</summary>
    NoShift,

    /// <summary>Closing time now: the clock stops, nobody else is called, the report follows.</summary>
    CloseNow,

    /// <summary>A citation slip is open: closing time once it is acknowledged, so the report is never drawn over the slip.</summary>
    CloseAfterSlip,

    /// <summary>The run has ended: its ending now, never Home.</summary>
    EndingNow,

    /// <summary>The run has ended and a citation slip is open: the slip's Acknowledge leads to the ending (its own way there).</summary>
    EndingAfterSlip
}

/// <summary>
/// The cheat menu's "End shift" (RUN-SAFE, BUG-28b, the cheat side; FLOW's
/// CloseNow honours the ending in play): a run that has ended (an ending set:
/// fired, bankrupt) goes to its ending, never Home, after an open citation
/// slip's Acknowledge (its own way there), else at once; otherwise, with a
/// slip open, closing time waits for its Acknowledge, so the report is never
/// drawn over it; with no shift running there is nothing to end. A slip is
/// open only where one is shown (SlipOpens). Pure, so GameManager.Cheats'
/// decision is tested headless.
/// </summary>
public static class EndShiftRule
{
    /// <summary>What End Shift does: <paramref name="endingSet"/> (the run has ended), <paramref name="slipOpen"/> (a citation slip waits for Acknowledge), <paramref name="shiftRunning"/> (after the briefing, before the shift's end) and <paramref name="deskClosed"/> (closing time already called).</summary>
    public static EndShiftAction Decide(bool endingSet, bool slipOpen, bool shiftRunning, bool deskClosed)
    {
        if (endingSet)
            return slipOpen ? EndShiftAction.EndingAfterSlip : EndShiftAction.EndingNow;
        if (!shiftRunning || deskClosed)
            return EndShiftAction.NoShift;
        return slipOpen ? EndShiftAction.CloseAfterSlip : EndShiftAction.CloseNow;
    }

    /// <summary>True when a verdict opens a slip End Shift must wait for: it issued a citation and the office shows slips (<paramref name="slipShown"/>: with no office UI the verdict's continuation runs at once and no Acknowledge ever comes).</summary>
    public static bool SlipOpens(bool citationIssued, bool slipShown) => citationIssued && slipShown;
}
