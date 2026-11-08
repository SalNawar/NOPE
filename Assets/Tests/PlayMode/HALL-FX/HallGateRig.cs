#if UNITY_EDITOR || TIMEDESK_QA
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEngine;

/// <summary>
/// The hall gates' ops for QA-RIG scenarios (the night of 2026-11-07, DECISIONS D6; tools/qa/scenarios/hall_portals_gate.json,
/// read by tools/hall/gate_check.py). <c>hallGateDial</c> (n = the module's number, 1 for 01; text "optional": no answerable
/// call is a note, not a failure) answers the first waiting call no module holds, whose jacks the module can take and
/// whose era's bars the rack's free cells (and the module's own) reach, through the board's own rules, as a hand on the
/// board ends up: its PLACE and PERIOD cords into the call's route, parked power cords (the dialer: cells) into it until
/// it is powered, MASTER pulled (Switchboard.PlugPlace, PlugPeriod, PlugPower, PullMaster),
/// then the board's cords redrawn where the rules have them; the board's drags are swb01-play's to test.
/// <c>hallGateClear</c> (n = the module's number) pulls the module's PLACE cord (Switchboard.UnplugPlace): its route
/// unlocks and its ring closes. <c>hallGateWait</c> (n = the ring, name = a PortalGateState, f = a timeout in real
/// seconds, 120 by default) waits until the ring's gate is in that state; it fails at once when it waits for LockedIn or
/// Sending and the ring's module holds no route. <c>hallGateTrack</c> (n = the ring) adds the
/// ring's gate to every captured frame's row (gate, lit, chev, pool, iris, burst, env: PortalEffect.Gate) and writes the
/// ring's screen quad and the hall drawing's projection into qa_report.txt ("== gate ring N at WxH"), so gate_check.py
/// finds the ring on the frames. The ring's quad is not tracked as motion: the flicker check looks at it.
/// </summary>
public static class HallGateRig
{
    /// <summary>Registers the ops before the first scene of a play run and, in the editor, at every domain load (QaEditorGateTests reads the scenarios' ops).</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
#endif
    private static void Register()
    {
        QaRunner.RegisterOp("hallGateDial", Dial);
        QaRunner.RegisterOp("hallGateClear", Clear);
        QaRunner.RegisterOp("hallGateWait", Wait);
        QaRunner.RegisterOp("hallGateTrack", Track);
    }

    /// <summary>The ring numbered <paramref name="portal"/>'s effect (HallPortalsSO's order), or null.</summary>
    private static PortalEffect Ring(int portal)
    {
        foreach (PortalEffect e in UnityEngine.Object.FindObjectsByType<PortalEffect>(FindObjectsInactive.Include))
            if (e.name == "PortalEffect_" + PortalText.Number(portal))
                return e;
        return null;
    }

    private static int Number(QaStep step) => step.n > 0 ? step.n : 1;

    private static IEnumerator Dial(QaRunner runner, QaStep step)
    {
        int module = Number(step) - 1;
        yield return QaGame.Until(() => SwitchboardView.Current != null && HallGateFeed.Board != null, 30f, "the switchboard's day");
        SwitchboardView view = SwitchboardView.Current;
        Switchboard board = HallGateFeed.Board;
        int cells = FreeCells(board) + board.Modules[module].Cells;
        bool found = Pick(board, module, cells, true, out SwitchRoute route);
        bool ahead = false;
        if (!found && Pick(board, module, int.MaxValue, true, out _))
            found = ahead = Pick(board, module, cells, false, out route);
        if (!found)
        {
            if (string.Equals(step.text, "optional", StringComparison.OrdinalIgnoreCase))
            {
                runner.Assert($"gate.dial{module + 1:00}", "HALL-GATE", true, $"module {module + 1:00}: no call it can answer (every waiting call is held, or its jacks are taken); optional, skipped",
                    "an optional dial: the cheat's calls may all sit on one destination, whose PLACE jack takes one plug");
                yield break;
            }
            throw new InvalidOperationException("hallGateDial: no call the module can answer waits on the board (ring the cheat 'Switchboard: 3 test calls' first).");
        }
        bool place = board.PlugPlace(module, route.Destination);
        bool period = board.PlugPeriod(module, route);
        SwitchModule m = board.Modules[module];
        for (int c = FreeCord(board); !m.Powered && c >= 0; c = FreeCord(board))
            if (!board.PlugPower(c, module))
                break;
        MasterPull pull = board.PullMaster();
        Redraw(view);
        runner.Assert($"gate.dial{module + 1:00}", "HALL-GATE", place && period && m.State == SwitchModuleState.Committed && m.Powered,
            $"module {module + 1:00} on route {route}{(ahead ? " (dialled ahead: the rack's cells cannot power a waiting call's route)" : string.Empty)}: place {place}, period {period}, "
            + $"cells {m.Cells} of {m.Need}, powered {m.Powered}, {m.State} (committed {pull.Committed}, fused {pull.Fused}); {board.Waiting(route)} waiting there; rack {FreeCells(board)} of {board.PowerCords}",
            "the oldest call the rack can power answered through the board's rules (cells to the era's bars, then MASTER), as a hand on the board ends up (the drags are swb01-play's)");
        yield return QaGame.Frames(1);
    }

    /// <summary>True when a locked-in module holds <paramref name="route"/> (its calls are being answered).</summary>
    private static bool Served(Switchboard board, SwitchRoute route)
    {
        foreach (SwitchModule m in board.Modules)
            if (m.Holding && m.Route == route)
                return true;
        return false;
    }

    /// <summary>
    /// The first route module <paramref name="module"/> can take whose era's bars fit in <paramref name="cells"/>:
    /// with <paramref name="calls"/>, a waiting call no module holds; else a route dialled ahead (a destination with no
    /// waiting call, on the era with the fewest bars), which MASTER locks in as the board's rules allow (the dialer's
    /// rack powers one Ancient route and one 1-bar route at once, and day 1's calls may all be Ancient).
    /// </summary>
    private static bool Pick(Switchboard board, int module, int cells, bool calls, out SwitchRoute route)
    {
        route = default;
        int best = int.MaxValue;
        for (int d = 0; d < board.Destinations; d++)
        {
            if (!calls && board.WaitingAt(d) > 0)
                continue;
            for (int p = 0; p < board.Periods; p++)
            {
                var r = new SwitchRoute(d, p);
                int bars = board.Rules.BarsFor(p);
                if (bars > cells || (calls ? board.Waiting(r) <= 0 || Served(board, r) : bars >= best)
                    || !board.CanPlugPlace(module, d) || !board.CanPlugPeriod(module, r))
                    continue;
                route = r;
                if (calls)
                    return true;
                best = bars;
            }
        }
        return best != int.MaxValue;
    }

    /// <summary>A power cord (the dialer: a cell) parked on the power row, or -1.</summary>
    private static int FreeCord(Switchboard board)
    {
        for (int c = 0; c < board.PowerCords; c++)
            if (board.PowerCordModule(c) < 0)
                return c;
        return -1;
    }

    /// <summary>How many power cords (the dialer: cells in the rack) are parked.</summary>
    private static int FreeCells(Switchboard board)
    {
        int n = 0;
        for (int c = 0; c < board.PowerCords; c++)
            if (board.PowerCordModule(c) < 0)
                n++;
        return n;
    }

    private static IEnumerator Clear(QaRunner runner, QaStep step)
    {
        int module = Number(step) - 1;
        Switchboard board = HallGateFeed.Board ?? throw new InvalidOperationException("hallGateClear: the switchboard has no day.");
        board.UnplugPlace(module);
        Redraw(SwitchboardView.Current);
        runner.Assert($"gate.clear{module + 1:00}", "HALL-GATE", board.Modules[module].State != SwitchModuleState.Open && board.Modules[module].State != SwitchModuleState.Committed,
            $"module {module + 1:00} {board.Modules[module].State}", "the module's PLACE cord pulled: its route unlocks");
        yield return QaGame.Frames(1);
    }

    /// <summary>The board's cords and cells drawn where its rules have them (its own SyncCordsFromBoard and, on the dialer, SyncCellsFromBoard).</summary>
    private static void Redraw(SwitchboardView view)
    {
        if (view == null)
            return;
        foreach (string name in new[] { "SyncCordsFromBoard", "SyncCellsFromBoard" })
            typeof(SwitchboardView).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(view, null);
    }

    private static IEnumerator Wait(QaRunner runner, QaStep step)
    {
        int portal = Number(step);
        PortalGateState want = (PortalGateState)Enum.Parse(typeof(PortalGateState), step.name, true);
        PortalEffect ring = Ring(portal) ?? throw new InvalidOperationException($"hallGateWait: no ring {portal}.");
        SwitchModule module = HallGateFeed.Module(HallGateFeed.Board, portal);
        if ((want == PortalGateState.LockedIn || want == PortalGateState.Sending) && (module == null || !module.Holding))
            throw new InvalidOperationException($"hallGateWait: ring {portal}'s module is {(module != null ? module.State.ToString() : "missing")}, not locked in, so ring {portal} cannot reach {want}.");
        yield return QaGame.Until(() => ring.Gate.State == want, step.f > 0f ? step.f : 120f, $"ring {portal} {want}");
    }

    private static IEnumerator Track(QaRunner runner, QaStep step)
    {
        int portal = Number(step);
        PortalEffect ring = Ring(portal) ?? throw new InvalidOperationException($"hallGateTrack: no ring {portal}.");
        PortalGateTrack gate = ring.Gate;
        PortalGateTuning tuning = ring.Portals != null ? ring.Portals.gate : new PortalGateTuning();
        var times = new PortalGateTimes(tuning.chevrons, tuning.openSeconds, tuning.fadeSeconds, tuning.kawooshSeconds, tuning.kawooshPeak, tuning.dwellSeconds);
        string n = portal.ToString(CultureInfo.InvariantCulture);
        runner.Recorder.Column("gate" + n, () => gate.State.ToString());
        runner.Recorder.Column("lit" + n, () => gate.Lit.ToString(CultureInfo.InvariantCulture));
        runner.Recorder.Column("chev" + n, () => gate.Chevrons.ToString("0.0000", CultureInfo.InvariantCulture));
        runner.Recorder.Column("pool" + n, () => gate.Pool.ToString("0.0000", CultureInfo.InvariantCulture));
        runner.Recorder.Column("iris" + n, () => gate.Iris.ToString("0.0000", CultureInfo.InvariantCulture));
        runner.Recorder.Column("burst" + n, () => gate.Burst.ToString("0.0000", CultureInfo.InvariantCulture));
        runner.Recorder.Column("env" + n, () => gate.Envelope(times, MotionPreference.Reduced).ToString("0.0000", CultureInfo.InvariantCulture));
        yield return Quad(runner, ring, portal);
    }

    /// <summary>The ring's screen quad and the hall drawing's projection into qa_report.txt (as hallPortalsShot writes them).</summary>
    private static IEnumerator Quad(QaRunner runner, PortalEffect ring, int portal)
    {
        yield return QaGame.Frames(2);
        Camera camera = Camera.main;
        Transform t = ring.transform;
        Vector3 a = camera.WorldToScreenPoint(t.TransformPoint(new Vector3(-0.5f, -0.5f, 0f)));
        Vector3 b = camera.WorldToScreenPoint(t.TransformPoint(new Vector3(0.5f, 0.5f, 0f)));
        Vector3 c = camera.WorldToScreenPoint(t.position);
        string line = string.Format(CultureInfo.InvariantCulture, "== gate ring {0} at {1}x{2}\n   centre ({3:0.0},{4:0.0}) quad ({5:0.0},{6:0.0})-({7:0.0},{8:0.0})\n",
                                    portal, Screen.width, Screen.height, c.x, c.y, Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        File.AppendAllText(Path.Combine(runner.OutDir, "qa_report.txt"), line);
    }
}
#endif
