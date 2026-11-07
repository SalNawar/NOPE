#if UNITY_EDITOR || TIMEDESK_QA
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>
/// The Stargate dialer's ops for QA-RIG scenarios (docs/pending/SWITCHBOARD.md
/// "Stargate dialer"). Every turn, carry and pull is a real press and drag of
/// the rig's virtual mouse at the fixed 30 fps step; nothing is teleported.
/// <list type="bullet">
/// <item><c>boardOpen</c>: D, then waits for the board to be fully turned to.</item>
/// <item><c>boardCall</c> (text "ifNone": a call on today's first place only when none waits; "next": a call on today's next place at another nation; else "nation:era", the nation's id and the era's order): a call as an approval makes it (for a day whose decided traveller was denied).</item>
/// <item><c>boardDial</c> (n = the dial, 0 for 01; text = what to bring under its chevron: "call" (the waiting call's nation, top chevron), "callEra" (the era waiting at the dial's nation, chevron 2), "wrongEra" (an era nobody waits on there), "reset" (the origin under chevron 2), a nation id or an era id; prefix "refused:" (as "refused:callEra") for a lock the rules must refuse (another dial holds that route); f = seconds of the turn, default 0.9; name): presses on the ring and turns it along its arc to the glyph, lets go, waits for it to rest; asserts <c>dial.&lt;name&gt;</c> (the lock it should make, or for "refused:" the refusal: the period unchanged, the place kept, chevron 2's lens lit red).</item>
/// <item><c>boardCells</c> (n = the dial; f = how many cells, 0 to its mark; name): carries rack cells into its cradle one by one; asserts <c>cells.&lt;name&gt;</c>.</item>
/// <item><c>boardCellOut</c> (n = the dial; name): carries one of its cells back to the rack; asserts <c>cellOut.&lt;name&gt;</c> (the dial falls short of its mark at once).</item>
/// <item><c>boardMaster</c> (f = how far, default 1; n = frames to wait after, default 30): drags MASTER's handle down and lets go.</item>
/// <item><c>boardHold</c> (n = the dial; key = Escape or RightClick; name): turns its ring half a turn of a slot, holding, then the key: asserts <c>hold.&lt;name&gt;</c> (the ring goes back, the locks unchanged, the board still turned to).</item>
/// <item><c>boardCheat</c>: the cheat "Switchboard: 3 test calls". <c>boardRightClick</c>: a right-click where the pointer is.</item>
/// <item><c>boardWait</c> (text = a condition; f = timeout, default 30 s) and <c>boardAssert</c> (name; text = a condition): conditions "open:N", "committed:N", "blown:N", "place:N", "period:N", "powered:N", "unpowered:N", "sent&gt;=K", "calls&gt;=K", "calls==K", "inService==K", "cells==K", "rack==K", "turned", "closed", "hidden" (turned away and the turn done).</item>
/// <item><c>boardLog</c> (n 1 starts, 0 stops; name): every frame, dial 01 to 03's state, ring turn, chevron lenses, bars and calibration into board_&lt;name&gt;.tsv (the frame strips' numbers: ring steps, the strobe's rate, the lens holding).</item>
/// </list>
/// </summary>
public static class SwitchboardRig
{
    private const string Owner = "SWITCHBOARD";

    private static Switchboard _counted;
    private static int _sent;
    private static StreamWriter _log;
    private static bool _logging;

    /// <summary>Registers the ops before the first scene of a play run and, in the editor, at every domain load (so QaEditorGateTests sees them).</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
#endif
    private static void Register()
    {
        QaRunner.RegisterOp("boardOpen", (runner, step) => Open());
        QaRunner.RegisterOp("boardCall", Call);
        QaRunner.RegisterOp("boardDial", Dial);
        QaRunner.RegisterOp("boardCells", Cells);
        QaRunner.RegisterOp("boardCellOut", CellOut);
        QaRunner.RegisterOp("boardMaster", Master);
        QaRunner.RegisterOp("boardHold", Hold);
        QaRunner.RegisterOp("boardCheat", Cheat);
        QaRunner.RegisterOp("boardRightClick", (runner, step) => RightClick(runner));
        QaRunner.RegisterOp("boardWait", Wait);
        QaRunner.RegisterOp("boardAssert", AssertOp);
        QaRunner.RegisterOp("boardLog", Log);
    }

    private static SwitchboardView View() =>
        SwitchboardView.Current ?? throw new InvalidOperationException("No switchboard in this scene (open the office).");

    private static Switchboard Board()
    {
        Switchboard board = View().Board ?? throw new InvalidOperationException("The switchboard has no day yet (start the shift first).");
        if (board != _counted)
        {
            _counted = board;
            _sent = 0;
            board.RouteSending += (m, r, k) => _sent += k;
        }
        return board;
    }

    private static IEnumerator Open()
    {
        SwitchboardView view = View();
        if (!view.IsOpen)
        {
            yield return QaGame.Until(() => view.Hidden, 3f, "the board to finish turning away");
            yield return QaGame.Frames(3);
            yield return QaRunner.Active.Input.Tap(Key.D);
        }
        yield return QaGame.Until(() => view.Turned, 5f, "the board to be turned to");
        yield return QaGame.Frames(5);
        Board();
    }

    private static IEnumerator Call(QaRunner runner, QaStep step)
    {
        Switchboard board = Board();
        string text = (step.text ?? string.Empty).Trim();
        SwitchRoute route;
        if (string.Equals(text, "ifNone", StringComparison.OrdinalIgnoreCase))
        {
            if (board.WaitingTotal > 0)
            {
                runner.Assert("call." + Name(step, "ifNone"), Owner, true, $"{board.WaitingTotal} waiting", "a call already waits (the decided traveller was approved)");
                yield break;
            }
            route = Place(0);
        }
        else if (string.Equals(text, "next", StringComparison.OrdinalIgnoreCase))
        {
            route = Place(1);
            for (int i = 1; i < View().TodaysRoutes.Count + 1; i++)
                if (Place(i).Destination != Place(0).Destination)
                {
                    route = Place(i);
                    break;
                }
        }
        else
        {
            string[] parts = text.Split(':');
            route = new SwitchRoute(View().NationSlot(parts[0]), int.Parse(parts[1], CultureInfo.InvariantCulture));
        }
        board.AddCall(route);
        runner.Assert("call." + Name(step, text), Owner, board.Waiting(route) > 0, $"a call on {route}", "the board's call (as an approval makes it)");
        yield return QaGame.Frames(5);
    }

    /// <summary>Today's place <paramref name="i"/> on the dials (its nation's slot and its era), else a made-up one (nation i, Modern).</summary>
    private static SwitchRoute Place(int i)
    {
        IReadOnlyList<SwitchRoute> today = View().TodaysRoutes;
        return i < today.Count ? today[i] : new SwitchRoute(i, 4);
    }

    // ------------------------------------------------------------------ the ring

    private static IEnumerator Dial(QaRunner runner, QaStep step)
    {
        SwitchboardView view = View();
        Switchboard board = Board();
        int m = step.n;
        GateBoardLayout layout = view.GateLayout ?? throw new InvalidOperationException("boardDial: the board is not the dialer.");
        GateGlyph[] ring = view.GateRing;
        int slots = ring.Length;
        SwitchModule module = board.Modules[m];
        string what = (step.text ?? string.Empty).Trim();
        const string RefusedPrefix = "refused:";
        bool refusal = what.StartsWith(RefusedPrefix, StringComparison.OrdinalIgnoreCase);
        if (refusal)
            what = what.Substring(RefusedPrefix.Length).Trim();
        GateGlyph glyph = Target(view, board, m, what, out int chevron);
        int slot = GateDial.SlotOf(ring, glyph.Kind, glyph.Index);
        if (slot < 0)
            throw new InvalidOperationException($"boardDial: no slot carries {glyph}.");
        float now = view.DialTurn(m, out _);
        float delta = GateDial.Delta(GateDial.Wrap360(now), GateDial.RotationFor(slot, chevron, slots));
        if (Mathf.Abs(delta) < 1f && what == "reset")
            delta = 360f; // already there: a full turn moves it on
        bool placeBefore = module.PlaceJack >= 0;
        int placeJackBefore = module.PlaceJack;
        bool periodBefore = module.HasPeriod;
        SwitchRoute periodJackBefore = module.PeriodJack;

        BoardPoint c = layout.DialCentre(m);
        float a0 = 250f;
        Vector2 At(float a)
        {
            float r = a * Mathf.Deg2Rad;
            return view.ScreenOf(new BoardPoint(c.X + GateBoardLayout.GlyphRadius * Mathf.Sin(r), c.Y - GateBoardLayout.GlyphRadius * Mathf.Cos(r)));
        }
        int frames = Mathf.Max(8, Mathf.RoundToInt((step.f > 0f ? step.f : 0.9f) * QaPaths.Fps));
        var samples = new List<QaPathSample>();
        for (int i = 0; i < QaPaths.HoverFrames; i++)
            samples.Add(new QaPathSample(At(a0), false));
        for (int i = 0; i <= QaPaths.GrabHoldFrames; i++)
            samples.Add(new QaPathSample(At(a0), true));
        for (int i = 1; i <= frames; i++)
        {
            float t = i / (float)frames;
            samples.Add(new QaPathSample(At(a0 + delta * t * t * (3f - 2f * t)), true));
        }
        for (int i = 0; i < QaPaths.EndHoldFrames + 3; i++)
            samples.Add(new QaPathSample(At(a0 + delta), true));
        for (int i = 0; i <= QaPaths.SettleFrames; i++)
            samples.Add(new QaPathSample(At(a0 + delta), false));
        // The refused chevron's red lasts 0.45 s from the detent: its lens is read on every frame of the turn, to the rest and 3 frames after.
        float lensPeak = 0f;
        void PeakLens() => lensPeak = Mathf.Max(lensPeak, view.DialLenses(m)[chevron]);
        IEnumerator turn = Fixed(runner.Input.Play(new QaPath(QaPathKind.Plain, samples)));
        while (turn.MoveNext())
        {
            yield return turn.Current;
            PeakLens();
        }
        yield return QaGame.Until(() =>
        {
            PeakLens();
            view.DialTurn(m, out bool resting);
            return resting;
        }, 3f, "the ring to rest in its detent");
        for (int i = 0; i < 3; i++)
        {
            yield return QaGame.Frames(1);
            PeakLens();
        }

        float rest = view.DialTurn(m, out _);
        int top = GateDial.SlotUnderChevron(rest, GateDial.PlaceChevron, slots), second = GateDial.SlotUnderChevron(rest, GateDial.PeriodChevron, slots);
        bool pass;
        string expect;
        switch (refusal ? RefusedPrefix : what)
        {
            case RefusedPrefix:
                pass = module.PlaceJack == placeJackBefore && module.HasPeriod == periodBefore && (!periodBefore || module.PeriodJack == periodJackBefore) && lensPeak >= 0.5f;
                expect = $"the lock refused on chevron {chevron + 1}: place kept, period unchanged, lens {chevron + 1} lit (peak {lensPeak:0.00})";
                break;
            case "reset":
                pass = module.PlaceJack < 0 && !module.HasPeriod;
                expect = "both locks open";
                break;
            default:
                if (chevron == GateDial.PlaceChevron)
                {
                    pass = module.PlaceJack == glyph.Index;
                    expect = "PLACE locked on nation " + glyph.Index;
                }
                else
                {
                    pass = module.HasPeriod && module.PeriodJack.Period == glyph.Index && module.PlaceJack >= 0 == placeBefore;
                    expect = "PERIOD locked on era " + glyph.Index;
                }
                break;
        }
        runner.Assert("dial." + Name(step, (refusal ? "refused_" : string.Empty) + what), Owner, pass,
            $"turned {delta:0.#} deg in {frames} frames; rests at {rest:0.##} ({ring[top]} under chevron 1, {ring[second]} under chevron 2); place {module.PlaceJack}, period {(module.HasPeriod ? module.PeriodJack.ToString() : "-")}, state {module.State}",
            expect);
    }

    /// <summary>What <paramref name="what"/> brings under which chevron of dial <paramref name="m"/>.</summary>
    private static GateGlyph Target(SwitchboardView view, Switchboard board, int m, string what, out int chevron)
    {
        SwitchModule module = board.Modules[m];
        chevron = GateDial.PeriodChevron;
        switch (what)
        {
            case "call":
                chevron = GateDial.PlaceChevron;
                for (int d = 0; d < board.Destinations; d++)
                    if (board.WaitingAt(d) > 0)
                        return new GateGlyph(GateGlyphKind.Nation, d);
                throw new InvalidOperationException("boardDial call: no call waits.");
            case "callEra":
            case "wrongEra":
                int at = module.PlaceJack;
                if (at < 0)
                    throw new InvalidOperationException($"boardDial {what}: dial {m} has no PLACE lock.");
                for (int e = 0; e < board.Periods; e++)
                    if (board.Waiting(new SwitchRoute(at, e)) > 0 == (what == "callEra"))
                        return new GateGlyph(GateGlyphKind.Era, e);
                throw new InvalidOperationException($"boardDial {what}: no such era at nation {at}.");
            case "reset":
                return new GateGlyph(GateGlyphKind.Origin, 0);
        }
        int nation = view.NationSlot(what);
        if (nation >= 0)
        {
            chevron = GateDial.PlaceChevron;
            return new GateGlyph(GateGlyphKind.Nation, nation);
        }
        string[] eras = { "ancient", "medieval", "earlymodern", "industrial", "modern" };
        int era = Array.IndexOf(eras, what.ToLowerInvariant());
        if (era < 0)
            throw new ArgumentException($"boardDial: unknown target '{what}'.");
        return new GateGlyph(GateGlyphKind.Era, era);
    }

    // ------------------------------------------------------------------ the cells

    private static IEnumerator Cells(QaRunner runner, QaStep step)
    {
        SwitchboardView view = View();
        Switchboard board = Board();
        int m = step.n;
        SwitchModule module = board.Modules[m];
        int want = step.f > 0f ? Mathf.RoundToInt(step.f) : Mathf.Max(0, module.Need - module.Cells);
        int carried = 0;
        for (int k = 0; k < want; k++)
        {
            int cell = -1;
            for (int c = 0; c < board.PowerCords && cell < 0; c++)
                if (board.PowerCordModule(c) < 0)
                    cell = c;
            if (cell < 0)
                break;
            BoardPoint from = view.CellPoint(cell, out _);
            BoardRect slot = view.GateLayout.CradleSlot(m, Mathf.Min(module.Cells, view.GateLayout.CradleSlots - 1));
            yield return Carry(runner, view.ScreenOf(from), view.ScreenOf(slot.Centre));
            carried++;
        }
        yield return QaGame.Frames(10);
        runner.Assert("cells." + Name(step, "dial" + m), Owner, carried == want && (step.f > 0f || module.Powered),
            $"{carried} cell(s) carried; dial {m}: {module.Cells} bar(s) of {module.Need}; powered {module.Powered}; rack {view.CellsInRack()}",
            step.f > 0f ? $"{want} cell(s) into the cradle" : "the bars reach the era's mark");
    }

    private static IEnumerator CellOut(QaRunner runner, QaStep step)
    {
        SwitchboardView view = View();
        Switchboard board = Board();
        int m = step.n;
        SwitchModule module = board.Modules[m];
        int cell = -1;
        for (int c = 0; c < board.PowerCords && cell < 0; c++)
            if (board.PowerCordModule(c) == m)
                cell = c;
        if (cell < 0)
            throw new InvalidOperationException($"boardCellOut: dial {m} holds no cell.");
        bool before = module.Powered;
        BoardPoint from = view.CellPoint(cell, out _);
        BoardRect rack = view.GateLayout.Rack;
        yield return Carry(runner, view.ScreenOf(from), view.ScreenOf(new BoardPoint(rack.X + rack.W - 30f, rack.Y - 40f)));
        yield return QaGame.Frames(10);
        runner.Assert("cellOut." + Name(step, "dial" + m), Owner, board.PowerCordModule(cell) < 0 && (!before || module.Cells < module.Need ? !module.Powered : module.Powered),
            $"cell {cell + 1} out; dial {m}: {module.Cells} bar(s) of {module.Need}; powered {before} -> {module.Powered}", "the cell goes home and the dial darkens at once when short of its mark");
    }

    private static IEnumerator Carry(QaRunner runner, Vector2 from, Vector2 to)
    {
        QaPath path = QaPaths.Drag(QaPathKind.Plain, from, to, new Vector2(Screen.width, Screen.height));
        yield return Fixed(runner.Input.Play(path));
        yield return QaGame.Frames(12);
    }

    // ------------------------------------------------------------------ MASTER

    private static IEnumerator Master(QaRunner runner, QaStep step)
    {
        SwitchboardView view = View();
        Board();
        float f = step.f > 0f ? Mathf.Clamp01(step.f) : 1f;
        Vector2 from = view.ScreenOf(view.MasterFrom), to = view.ScreenOf(view.MasterFrom.Lerp(view.MasterTo, f));
        QaPath path = QaPaths.Drag(QaPathKind.Plain, from, to, new Vector2(Screen.width, Screen.height));
        yield return Fixed(runner.Input.Play(path));
        yield return QaGame.Frames(step.n > 0 ? step.n : 30);
    }

    // ------------------------------------------------------------------ Esc and right-click

    private static IEnumerator Hold(QaRunner runner, QaStep step)
    {
        SwitchboardView view = View();
        Switchboard board = Board();
        int m = step.n;
        SwitchModule module = board.Modules[m];
        GateBoardLayout layout = view.GateLayout;
        float before = view.DialTurn(m, out _);
        int place = module.PlaceJack;
        bool period = module.HasPeriod;
        BoardPoint c = layout.DialCentre(m);
        Vector2 At(float a)
        {
            float r = a * Mathf.Deg2Rad;
            return view.ScreenOf(new BoardPoint(c.X + GateBoardLayout.GlyphRadius * Mathf.Sin(r), c.Y - GateBoardLayout.GlyphRadius * Mathf.Cos(r)));
        }
        runner.Input.Set(At(250f), false);
        yield return QaGame.Frames(2);
        runner.Input.Set(At(250f), true);
        yield return QaGame.Frames(3);
        for (int i = 1; i <= 10; i++)
        {
            runner.Input.Set(At(250f + 40f * i / 10f), true);
            yield return null;
        }
        yield return QaGame.Frames(3);
        bool held = view.IsDragging;
        string key = step.key ?? "Escape";
        if (string.Equals(key, "RightClick", StringComparison.OrdinalIgnoreCase))
        {
            Vector2 at = At(290f);
            InputSystem.QueueStateEvent(runner.Input.Mouse, new MouseState { position = at }.WithButton(MouseButton.Left).WithButton(MouseButton.Right));
            yield return QaGame.Frames(2);
            runner.Input.Set(at, true);
            yield return null;
        }
        else
            yield return runner.Input.Tap((Key)Enum.Parse(typeof(Key), key, true));
        yield return QaGame.Frames(3);
        runner.Input.Set(At(290f), false);
        yield return QaGame.Until(() =>
        {
            view.DialTurn(m, out bool rest);
            return rest;
        }, 3f, "the ring to rest after the let-go");
        yield return QaGame.Frames(3);
        float after = view.DialTurn(m, out bool resting);
        bool pass = held && resting && Mathf.Abs(GateDial.Delta(GateDial.Wrap360(before), GateDial.Wrap360(after))) < 0.5f
                    && module.PlaceJack == place && module.HasPeriod == period && view.Turned;
        runner.Assert("hold." + Name(step, key), Owner, pass,
            $"held {held}; ring {before:0.##} -> {after:0.##} (resting {resting}); place {place} -> {module.PlaceJack}; period {period} -> {module.HasPeriod}; turned {view.Turned}",
            "the key lets go of the ring with nothing changed; the board stays turned to");
    }

    /// <summary>A right-click where the pointer is (a press and a release of the right button).</summary>
    private static IEnumerator RightClick(QaRunner runner)
    {
        Vector2 at = runner.Input.Position;
        InputSystem.QueueStateEvent(runner.Input.Mouse, new MouseState { position = at }.WithButton(MouseButton.Right));
        yield return QaGame.Frames(2);
        runner.Input.Set(at, false);
        yield return QaGame.Frames(20);
    }

    private static IEnumerator Cheat(QaRunner runner, QaStep step)
    {
        string line = View().AddTestCalls();
        runner.Assert("cheat." + Name(step, "calls"), Owner, line.StartsWith("Switchboard: 3 test calls", StringComparison.Ordinal), line, "the cheat's three calls");
        yield return QaGame.Frames(5);
    }

    // ------------------------------------------------------------------ conditions

    private static IEnumerator Wait(QaRunner runner, QaStep step)
    {
        string text = step.text ?? string.Empty;
        yield return QaGame.Until(() => Holds(text, out _), step.f > 0f ? step.f : 30f, "the board: " + text);
    }

    private static IEnumerator AssertOp(QaRunner runner, QaStep step)
    {
        bool pass = Holds(step.text ?? string.Empty, out string measured);
        runner.Assert("board." + Name(step, step.text), Owner, pass, measured, step.text);
        yield return null;
    }

    /// <summary>True when <paramref name="text"/> holds on the board now (the conditions of boardWait and boardAssert).</summary>
    private static bool Holds(string text, out string measured)
    {
        SwitchboardView view = View();
        Switchboard board = Board();
        string t = text.Trim();
        int colon = t.IndexOf(':');
        if (colon > 0)
        {
            string what = t.Substring(0, colon);
            SwitchModule m = board.Modules[int.Parse(t.Substring(colon + 1), CultureInfo.InvariantCulture)];
            measured = $"dial {m.Index}: {m.State}, place {m.PlaceJack}, period {(m.HasPeriod ? m.PeriodJack.ToString() : "-")}, bars {m.Cells}/{m.Need}, calibration left {m.CalibrationLeft:0.0}, sent {m.Sent}";
            switch (what)
            {
                case "open": return m.State == SwitchModuleState.Open;
                case "committed": return m.State == SwitchModuleState.Committed;
                case "blown": return m.State == SwitchModuleState.Blown;
                case "place": return m.PlaceJack >= 0;
                case "period": return m.HasPeriod;
                case "powered": return m.Powered;
                case "unpowered": return !m.Powered;
                default: throw new ArgumentException($"Unknown board condition '{text}'.");
            }
        }
        int inService = 0;
        foreach (SwitchModule m in board.Modules)
            if (m.State != SwitchModuleState.Closed)
                inService++;
        measured = $"sent {_sent}, waiting {board.WaitingTotal}, in service {inService}, cells {board.PowerCords}, rack {view.CellsInRack()}, turned {view.Turned}, open {view.IsOpen}";
        switch (t)
        {
            case "turned": return view.Turned;
            case "closed": return !view.IsOpen;
            case "hidden": return view.Hidden;
        }
        foreach (string op in new[] { ">=", "==" })
        {
            int at = t.IndexOf(op, StringComparison.Ordinal);
            if (at <= 0)
                continue;
            string name = t.Substring(0, at);
            int k = int.Parse(t.Substring(at + 2), CultureInfo.InvariantCulture);
            int v = name switch
            {
                "sent" => _sent,
                "calls" => board.WaitingTotal,
                "inService" => inService,
                "cells" => board.PowerCords,
                "rack" => view.CellsInRack(),
                _ => throw new ArgumentException($"Unknown board count '{name}'."),
            };
            return op == ">=" ? v >= k : v == k;
        }
        throw new ArgumentException($"Unknown board condition '{text}'.");
    }

    // ------------------------------------------------------------------ the frame log

    private static IEnumerator Log(QaRunner runner, QaStep step)
    {
        if (step.n == 0)
        {
            _logging = false;
            yield return null;
            _log?.Dispose();
            _log = null;
            yield break;
        }
        _log?.Dispose();
        _log = new StreamWriter(Path.Combine(runner.OutDir, "board_" + Name(step, "dial") + ".tsv"));
        _log.WriteLine("frame\ttime\tdial\tstate\tturn\tlenses\tbars\tneed\tleft\tsent");
        _logging = true;
        runner.StartCoroutine(Sample());
        yield return null;
    }

    private static IEnumerator Sample()
    {
        while (_logging && _log != null)
        {
            yield return new WaitForEndOfFrame();
            SwitchboardView view = SwitchboardView.Current;
            Switchboard board = view != null ? view.Board : null;
            if (board == null || _log == null)
                continue;
            for (int m = 0; m < Math.Min(GateBoardLayout.BigDials, board.Modules.Count); m++)
            {
                SwitchModule module = board.Modules[m];
                if (module.State == SwitchModuleState.Closed)
                    continue;
                float turn = view.DialTurn(m, out _);
                var lenses = new StringBuilder();
                foreach (float a in view.DialLenses(m))
                    lenses.Append(a.ToString("0.00", CultureInfo.InvariantCulture)).Append(',');
                _log.WriteLine(string.Join("\t", Time.frameCount, Time.unscaledTime.ToString("0.000", CultureInfo.InvariantCulture), m, module.State,
                    turn.ToString("0.000", CultureInfo.InvariantCulture), lenses.ToString().TrimEnd(','), module.Cells, module.Need,
                    module.CalibrationLeft.ToString("0.000", CultureInfo.InvariantCulture), _sent));
            }
        }
    }

    // ------------------------------------------------------------------ helpers

    /// <summary>Plays <paramref name="body"/> at the fixed 30 fps game step (as the rig's drags do), the capture rate put back after.</summary>
    private static IEnumerator Fixed(IEnumerator body)
    {
        int was = Time.captureFramerate;
        bool set = was != SeqRecorder.Fps;
        if (set)
            Time.captureFramerate = SeqRecorder.Fps;
        try
        {
            while (body.MoveNext())
                yield return body.Current;
        }
        finally
        {
            if (set)
                Time.captureFramerate = was;
        }
    }

    private static string Name(QaStep step, string fallback) => string.IsNullOrEmpty(step.name) ? (fallback ?? "step").Replace(' ', '_') : step.name;
}
#endif
