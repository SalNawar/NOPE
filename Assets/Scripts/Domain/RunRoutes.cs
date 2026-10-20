using System;
using System.Collections.Generic;

/// <summary>One step of a day-changing cheat's route (RunRoutes), each run by RunManager with the same code play runs.</summary>
public enum RunStep
{
    /// <summary>Today's shift closes as play closes it (DayCycle.EndShift on the shift's ledger), with the ending check play runs after it.</summary>
    CloseShift,

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
/// today's shift (unless play closed it already), plays Home's night on the
/// default care, sleeps and opens the next morning; a jump forward does the
/// same for every day up to the target; a jump back starts a new world with
/// the run's seed and pet and plays each day up to the target the same way;
/// a jump to today is a restart, which restores today's morning snapshot.
/// RunManager runs the steps in order and stops where one ends the run.
/// Pure, so the routes are tested headless.
/// </summary>
public static class RunRoutes
{
    /// <summary>One day played through with nothing decided: its shift closes, Home's night, Sleep.</summary>
    private static readonly RunStep[] PlayedDay = { RunStep.CloseShift, RunStep.HomeNight, RunStep.Sleep };

    /// <summary>The cheat menu's "Skip day": today's close (unless <paramref name="shiftClosed"/>: play closed it, the run is at Home), Home's night, Sleep, the next morning.</summary>
    public static List<RunStep> Skip(bool shiftClosed)
    {
        var route = new List<RunStep>();
        AddDay(route, shiftClosed);
        route.Add(RunStep.OpenMorning);
        return route;
    }

    /// <summary>
    /// The cheat menu's "Jump to day N" from <paramref name="today"/> to
    /// <paramref name="target"/> (Clamp to the run's <paramref name="lastDay"/>):
    /// today itself is a restart; forward, today's close (unless
    /// <paramref name="shiftClosed"/>), its night and every day after it up to
    /// the target played through; back, a new world played through from day
    /// 1 up to the target. Each ends at the target's morning.
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
            shiftClosed = false;
        }
        for (; day < target; day++)
        {
            AddDay(route, shiftClosed);
            shiftClosed = false;
        }
        route.Add(RunStep.OpenMorning);
        return route;
    }

    /// <summary>The cheat menu's "Restart shift": today's morning snapshot back, then its morning opened.</summary>
    public static List<RunStep> Restart() => new List<RunStep> { RunStep.RestoreMorning, RunStep.OpenMorning };

    /// <summary>A jump's target within the run's days (1 to <paramref name="lastDay"/>); with no last day known (0 or less) the target stands, at least 1.</summary>
    public static int Clamp(int target, int lastDay) => Math.Max(1, lastDay > 0 ? Math.Min(target, lastDay) : target);

    /// <summary>A day played through: its shift's close (not when already closed), Home's night, Sleep.</summary>
    private static void AddDay(List<RunStep> route, bool shiftClosed)
    {
        for (int i = shiftClosed ? 1 : 0; i < PlayedDay.Length; i++)
            route.Add(PlayedDay[i]);
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

    /// <summary>A jump from <paramref name="today"/> back to <paramref name="target"/>: the days from the target to today as played are lost; the run replays from day 1 with its seed and pet.</summary>
    public static string JumpBack(int today, int target) =>
        target >= today
            ? $"Jump back to day {target}? Lost: today as played."
            : $"Jump back to day {target}? Lost: days {target}-{today} as played. The run replays from day 1 with its seed and pet, each night on the default care.";
}
