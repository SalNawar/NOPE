#if UNITY_EDITOR || TIMEDESK_QA
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEngine;

/// <summary>
/// The hall gates' ops for QA-RIG scenarios (the night of 2026-11-07, DECISIONS D6; tools/qa/scenarios/hall_portals_gate.json,
/// read by tools/hall/gate_check.py). <c>hallGateDial</c> (n = the module's number, 1 for 01) answers the oldest waiting
/// call on that module through the board's own rules, as a hand on the board ends up: its PLACE and PERIOD cords into the
/// call's route, a power cord into it if it has none, MASTER pulled (Switchboard.PlugPlace, PlugPeriod, PlugPower,
/// PullMaster), then the board's cords redrawn where the rules have them; the board's drags are swb01-play's to test.
/// <c>hallGateClear</c> (n = the module's number) pulls the module's PLACE cord (Switchboard.UnplugPlace): its route
/// unlocks and its ring closes. <c>hallGateWait</c> (n = the ring, name = a PortalGateState, f = a timeout in real
/// seconds, 120 by default) waits until the ring's gate is in that state. <c>hallGateTrack</c> (n = the ring) adds the
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
        SwitchRoute route = default;
        bool found = false;
        for (int d = 0; d < board.Destinations && !found; d++)
            for (int p = 0; p < board.Periods && !found; p++)
                if (board.Waiting(new SwitchRoute(d, p)) > 0)
                {
                    route = new SwitchRoute(d, p);
                    found = true;
                }
        if (!found)
            throw new InvalidOperationException("hallGateDial: no call waits on the board (ring the cheat 'Switchboard: 3 test calls' first).");
        bool place = board.PlugPlace(module, route.Destination);
        bool period = board.PlugPeriod(module, route);
        if (!board.Modules[module].Powered)
            board.PlugPower(0, module);
        MasterPull pull = board.PullMaster();
        Redraw(view);
        SwitchModule m = board.Modules[module];
        runner.Assert($"gate.dial{module + 1:00}", "HALL-GATE", place && period && m.State == SwitchModuleState.Committed && m.Powered,
            $"module {module + 1:00} on route {route}: place {place}, period {period}, powered {m.Powered}, {m.State} (committed {pull.Committed}, fused {pull.Fused}); {board.Waiting(route)} waiting there",
            "the oldest call answered through the board's rules, as a hand on the board ends up (the drags are swb01-play's)");
        yield return QaGame.Frames(1);
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

    /// <summary>The board's cords drawn where its rules have them (its own SyncCordsFromBoard).</summary>
    private static void Redraw(SwitchboardView view)
    {
        if (view == null)
            return;
        MethodInfo sync = typeof(SwitchboardView).GetMethod("SyncCordsFromBoard", BindingFlags.Instance | BindingFlags.NonPublic);
        sync?.Invoke(view, null);
    }

    private static IEnumerator Wait(QaRunner runner, QaStep step)
    {
        int portal = Number(step);
        PortalGateState want = (PortalGateState)Enum.Parse(typeof(PortalGateState), step.name, true);
        PortalEffect ring = Ring(portal) ?? throw new InvalidOperationException($"hallGateWait: no ring {portal}.");
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
