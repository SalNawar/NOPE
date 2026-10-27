#if UNITY_EDITOR || TIMEDESK_QA
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

/// <summary>
/// UI-PC's scenario ops for the QA rig (docs/pending/UI-PC.md, the PC
/// session; FIX_PLAN v1 §3 UI-PC "Verification": "Open 8 windows; minimise
/// and restore each app; double-click Order; press Enter after clicks; zoom
/// 150%; compare balances across Account, Orders and the HUD"; v2 delta).
/// Each plays the player's input through the rig's virtual mouse and
/// keyboard (clicks, the wheel, keys and chords as Input System events,
/// never a handler called directly) and asserts what the player would see.
/// <list type="bullet">
/// <item><c>uipcEsc</c>: Esc on the PC brings Settings to the front and the PC stays open (R3/R4).</item>
/// <item><c>uipcEscDark</c>: with the screen turned off by the frame's power button, Esc turns it on and brings Settings forward, a right-click does nothing, and the PC stays open (R3/R4).</item>
/// <item><c>uipcGate</c>: with Notes' field focused, the cheat panel opened over the PC by its key (F9; it holds the InputGate) leaves the field with its text kept, and a right-click and Esc, each delivered, reach nothing on the PC: neither is taken by the desktop, Settings does not come forward and the PC stays open; F9 closes the panel and frees the gate (RUN-SAFE's BUG-68a contract).</item>
/// <item><c>uipcRoom</c>: a click on the room beside the frame does not leave the PC (R3/R4: only Q and the bezel).</item>
/// <item><c>uipcBezel</c>: a click on the bezel leaves the PC (R3/R4: "press Q or click the bezel").</item>
/// <item><c>uipcPowerOn</c>: a right-click and Esc a few frames after Q, while the picture still powers on (the desktop's clicks wait for it): each is taken by the desktop, the PC stays open and Esc brings Settings forward, with full motion and with Reduced Motion, and the player's motion choice is put back whatever the passes did (R3/R4: "Esc = Settings everywhere").</item>
/// <item><c>uipcFullMotion</c>: before the session records the PC's moves (pc_open, pc_close, pc_reverse_open, pc_reverse_close), Reduced Motion is off, so the recordings show the CRT line.</item>
/// <item><c>uipcReverseOpen</c>: Q, then Q again 2 frames later, during the power-on: the frame fades out from where it is (BUG-32c, F1; CrtPower.Resume as PcFrame draws it).</item>
/// <item><c>uipcReverseClose</c>: Q, then a click on the office's PC 2 frames later, during the collapse: the closing frame takes no click, so the PC opens again, and the picture powers on from the same line (BUG-32c, F1).</item>
/// <item><c>uipcTray</c>: the tray clock reads the shift clock's time over today's date, the CRT clock plate is gone, no credits in the tray (BUG-20a, 21a).</item>
/// <item><c>uipcWindows</c>: every app opened from the Start menu, and the F1 card (at least 8 windows); the taskbar names each open window by a button or counts it behind the chevron, nothing over the tray, and the chevron brings each waiting window forward in turn (BUG-21a).</item>
/// <item><c>uipcOrder</c>: a double-click on an orderable Orders card places exactly one order, charged once (BUG-58a, UI-25).</item>
/// <item><c>uipcEnter</c>: Enter on the chosen card acts once per press, and two quick presses act once (BUG-58a, CODE-117).</item>
/// <item><c>uipcZoom</c>: Ctrl+= twice zooms the Orders tree to 150 %.</item>
/// <item><c>uipcRestore</c>: Orders scrolled by the wheel, minimised by its title bar's button and restored by its taskbar button keeps its scroll, its chosen card and its zoom (BUG-58a, CODE-114).</item>
/// <item><c>uipcBalance</c>: with the Citizen Account open and scrolled, money arriving redraws it in place, and the Account, Orders and the office's credits readout show the same balance (BUG-58a, CODE-121).</item>
/// </list>
/// Compiled only in the editor and QA player builds.
/// </summary>
public static class UiPcRigOps
{
    private const string Owner = "UI-PC";

    /// <summary>The wheel's step as the Input System reports one notch.</summary>
    private const float WheelNotch = 120f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        QaRunner.RegisterOp("uipcEsc", Esc);
        QaRunner.RegisterOp("uipcEscDark", EscDark);
        QaRunner.RegisterOp("uipcGate", Gate);
        QaRunner.RegisterOp("uipcRoom", Room);
        QaRunner.RegisterOp("uipcBezel", Bezel);
        QaRunner.RegisterOp("uipcPowerOn", PowerOn);
        QaRunner.RegisterOp("uipcFullMotion", FullMotion);
        QaRunner.RegisterOp("uipcReverseOpen", ReverseOpen);
        QaRunner.RegisterOp("uipcReverseClose", ReverseClose);
        QaRunner.RegisterOp("uipcWindows", Windows);
        QaRunner.RegisterOp("uipcOrder", Order);
        QaRunner.RegisterOp("uipcEnter", Enter);
        QaRunner.RegisterOp("uipcZoom", Zoom);
        QaRunner.RegisterOp("uipcRestore", Restore);
        QaRunner.RegisterOp("uipcBalance", Balance);
        QaRunner.RegisterOp("uipcTray", Tray);
    }

    // -----------------------------
    // The exits (BUG-50a, R3/R4)
    // -----------------------------

    /// <summary>Esc with nothing open on the desktop: Settings comes forward, focused, and the PC stays open.</summary>
    private static IEnumerator Esc(QaRunner runner, QaStep step)
    {
        OfficeViewController view = QaGame.Find<OfficeViewController>();
        DesktopWindow settings = Window<SettingsWindowController>();
        yield return runner.Input.Tap(Key.Escape);
        yield return QaGame.Frames(3);
        bool open = PcOpen(view);
        bool front = settings != null && settings.IsOpen && !settings.IsMinimised;
        runner.Assert("esc", Owner, open && front, $"pc {(open ? "open" : "closed")}, settings {(front ? "in front" : "not shown")}",
            "Esc opens Settings everywhere (R3/R4); only Q and the bezel leave the PC");
    }

    /// <summary>The frame's power button darkens the screen; then Esc turns it on and brings Settings forward, and a right-click does nothing; the PC stays open throughout.</summary>
    private static IEnumerator EscDark(QaRunner runner, QaStep step)
    {
        OfficeViewController view = QaGame.Find<OfficeViewController>();
        PcFrame frame = QaGame.Find<PcFrame>();
        MonitorScreen screen = QaGame.Find<MonitorScreen>();
        DesktopWindow settings = Window<SettingsWindowController>();
        Transform power = frame != null ? FindDeep(frame.transform, "PowerButton") : null;
        Vector2? at = power != null ? QaGame.ScreenPoint(power.gameObject) : null;
        if (screen == null || settings == null || !at.HasValue)
        {
            runner.Assert("escdark", Owner, false, "no PC frame, screen or Settings", "the PC must be open in the office");
            yield break;
        }
        if (settings.IsOpen)
            settings.Close(); // Settings must come back by the Esc, not stay from the step before
        yield return QaGame.Frames(2);
        yield return runner.Input.Play(QaPaths.Click(at.Value));
        yield return QaGame.Frames(3);
        bool dark = !screen.IsOn;
        yield return RightClick(runner);
        yield return QaGame.Frames(3);
        bool afterRight = PcOpen(view) && !screen.IsOn;
        yield return runner.Input.Tap(Key.Escape);
        yield return QaGame.Frames(4);
        bool lit = screen.IsOn, front = settings.IsOpen && !settings.IsMinimised, open = PcOpen(view);
        runner.Assert("escdark", Owner, dark && afterRight && lit && front && open,
            $"power button: screen {(dark ? "off" : "still on")}; right-click: {(afterRight ? "nothing" : "changed something")}; Esc: screen {(lit ? "on" : "off")}, settings {(front ? "in front" : "not shown")}, pc {(open ? "open" : "closed")}",
            "with the screen off, Esc still opens Settings (and turns the screen on) and nothing but Q and the bezel leaves the PC (R3/R4)");
    }

    /// <summary>
    /// The cheat panel over the PC (asserts <c>gate</c>). With the PC open and
    /// Settings closed, Notes is opened from Start and its notes field takes
    /// the keyboard by a click (setup: the field the review of f6396c8 found
    /// reverted). F9 then opens the panel, which holds the InputGate
    /// (DebugPanelController). The field must lose the keyboard at once, with
    /// its text kept (<see cref="PcPoller.Gated"/>: the EventSystem still
    /// updates the selected field under the gate, and TMP's own Esc would
    /// cancel the edit and put back the text from when it was focused); it is
    /// measured before the right-click, which lands on the panel's blocker and
    /// would take the selection off by itself. The rig's keyboard makes no
    /// IMGUI events, so TMP's cancel cannot happen here: this measures its
    /// cause, and recipe 4's real-keyboard step measures the kept text. A
    /// right-click and then Esc are pressed under the panel. Each must reach
    /// the game in its own frame (the virtual mouse's right button and the
    /// keyboard's Esc read pressed in that frame, so a press the rig never
    /// delivered fails instead of passing as "not taken"), neither may be
    /// taken by the desktop in that frame
    /// (<see cref="DesktopKeyboard.EscapeTakenFrame"/>), Settings must not
    /// come forward behind the panel and the PC must stay open: while the gate
    /// is held, "the PC's input handlers stand still" (RUN-SAFE's BUG-68a
    /// contract). F9 then closes the panel, and the gate must be free. No wait
    /// here can throw, so the closing F9 always runs when F9 opened the panel.
    /// When it does not free the gate (the panel missed the toggle), a second
    /// F9 is tried, and the assert fails either way; if that one misses too,
    /// the gate stays held over every later step of the run, which then fail
    /// on their own (the line says so). Notes is closed again when this op
    /// opened it.
    /// </summary>
    private static IEnumerator Gate(QaRunner runner, QaStep step)
    {
        OfficeViewController view = QaGame.Find<OfficeViewController>();
        DesktopKeyboard keys = QaGame.Find<DesktopKeyboard>();
        DesktopWindow settings = Window<SettingsWindowController>();
        NotesWindow notes = QaGame.Find<NotesWindow>();
        DesktopWindow notesWindow = notes != null ? notes.GetComponent<DesktopWindow>() : null;
        var field = notes != null ? (TMP_InputField)QaGame.Field(notes, "notesField") : null;
        DesktopShell shell = QaGame.Find<DesktopShell>();
        Transform start = shell != null ? FindDeep(shell.transform, "StartButton") : null;
        Transform entry = shell != null ? FindDeep(shell.transform, "App_" + DesktopAppIds.Notes) : null;
        if (view == null || keys == null || settings == null || notesWindow == null || field == null || start == null || entry == null || !PcOpen(view) || InputGate.Blocked)
        {
            runner.Assert("gate", Owner, false, "no open PC, desktop keys, Settings, Notes' field or Start menu, or the gate already held", "run it with the PC open in the office and no overlay open");
            yield break;
        }
        if (settings.IsOpen)
            settings.Close(); // a press that reached the PC would bring it back
        bool notesWasOpen = notesWindow.IsOpen;
        yield return ClickObject(runner, start);
        yield return QaGame.Frames(3);
        yield return ClickObject(runner, entry); // Notes, from Start
        yield return QaGame.Frames(6);
        yield return ClickObject(runner, field.transform); // its notes field takes the keyboard
        yield return QaGame.Frames(3);
        bool typing = field.isFocused;
        string before = field.text ?? string.Empty;

        yield return runner.Input.Tap(Key.F9); // the cheat panel opens over the PC and holds the InputGate
        bool held = InputGate.Blocked;
        UnityEngine.EventSystems.EventSystem events = UnityEngine.EventSystems.EventSystem.current;
        bool left = !field.isFocused && (events == null || events.currentSelectedGameObject != field.gameObject);

        InputSystem.QueueStateEvent(runner.Input.Mouse, new MouseState { position = runner.Input.Position }.WithButton(MouseButton.Right));
        yield return null; // the game sees the right-click this frame
        bool rightSent = runner.Input.Mouse.rightButton.wasPressedThisFrame;
        bool rightTaken = keys.EscapeTakenFrame == Time.frameCount;
        InputSystem.QueueStateEvent(runner.Input.Mouse, new MouseState { position = runner.Input.Position });
        yield return null;

        InputSystem.QueueStateEvent(runner.Input.Keyboard, new KeyboardState(Key.Escape));
        yield return null; // the game sees the Esc this frame
        bool escSent = runner.Input.Keyboard.escapeKey.wasPressedThisFrame;
        bool escTaken = keys.EscapeTakenFrame == Time.frameCount;
        InputSystem.QueueStateEvent(runner.Input.Keyboard, new KeyboardState());
        yield return QaGame.Frames(3);
        string after = field.text ?? string.Empty;
        bool kept = after == before, shown = settings.IsOpen && !settings.IsMinimised, open = PcOpen(view);

        if (held)
            yield return runner.Input.Tap(Key.F9); // the panel closes and lets the gate go
        bool released = !InputGate.Blocked, freedLate = false;
        if (!released)
        {
            yield return runner.Input.Tap(Key.F9); // a missed toggle must not hold the gate over every later step
            freedLate = !InputGate.Blocked;
        }
        if (!notesWasOpen && notesWindow.IsOpen)
            notesWindow.Close(); // as the session had it
        yield return QaGame.Frames(2);

        string gate = released ? "free" : freedLate ? "still held, freed by a second F9" : "still held after a second F9: every later step runs under the gate";
        runner.Assert("gate", Owner, typing && held && left && kept && rightSent && !rightTaken && escSent && !escTaken && !shown && open && released,
            $"notes field {(typing ? "had the keyboard" : "never took the keyboard (measured nothing)")}; cheat panel {(held ? "open, gate held" : "not opened by F9")}: field {(left ? "left" : "still has the keyboard")}; right-click {(rightSent ? "delivered" : "not delivered")}, {(rightTaken ? "taken by the desktop" : "not taken")}; Esc {(escSent ? "delivered" : "not delivered")}, {(escTaken ? "taken by the desktop" : "not taken")}: field text {(kept ? "kept" : "changed")} ({before.Length} -> {after.Length} chars), settings {(shown ? "brought forward" : "not shown")}, pc {(open ? "open" : "closed")}; panel closed: gate {gate}",
            "while the cheat panel holds the InputGate the PC's input handlers stand still (RUN-SAFE BUG-68a): a focused desktop field is left with its text kept, an Esc must not bring Settings forward behind the panel, and nothing leaves the PC");
    }

    /// <summary>
    /// The back-out during the power-on, with full motion, then with Reduced
    /// Motion (a setting for the step, not the input under test). Each pass
    /// (<see cref="PowerOnPass"/>, asserts <c>poweron.full</c> and
    /// <c>poweron.reduced</c>) closes the PC with Q, opens it with Q, and
    /// sends a right-click and then Esc while the picture still moves; the
    /// review of fix round 2 found the keys gated on the raycaster the
    /// power-on holds off, so the office's back-out closed the PC.
    /// Reduced Motion is a saved player preference (its setter writes
    /// PlayerPrefs), so the passes run under <see cref="EachMotion"/>: a wait
    /// that times out (or anything else that throws) fails that pass's assert
    /// with the exception's text instead of ending the run inside the op, the
    /// other pass still runs, and the player's choice is always put back
    /// (tested offline with passes that throw: UiPcRigTests). Left on, it
    /// would make the next session record pc_open and pc_close as the Reduced
    /// fade (uipcFullMotion fails that). The step's <c>f</c>, when set, is
    /// each wait's timeout in seconds (<see cref="WaitSeconds"/> otherwise, as
    /// QaRunner's waiting ops read it): a tiny one forces the timeouts, for the
    /// one-off stopped-pass run (docs/pending/UI-PC.md, RESULT recipe 18).
    /// Not covered: a run stopped from outside during a pass (its deadline, a
    /// harness reset, play mode left, the player closed) stops this op
    /// without disposing it, so the restore does not run; that needs QA-RIG's
    /// run-level cleanup (docs/pending/UI-PC.md, Requests).
    /// </summary>
    private static IEnumerator PowerOn(QaRunner runner, QaStep step)
    {
        OfficeViewController view = QaGame.Find<OfficeViewController>();
        PcFrame frame = QaGame.Find<PcFrame>();
        DesktopKeyboard keys = QaGame.Find<DesktopKeyboard>();
        DesktopWindow settings = Window<SettingsWindowController>();
        var glass = frame != null ? (RectTransform)typeof(PcFrame).GetField("glass", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(frame) : null;
        if (view == null || keys == null || settings == null || glass == null || !PcOpen(view))
        {
            runner.Assert("poweron.full", Owner, false, "no open PC frame, desktop keys or Settings", "run it with the PC open in the office");
            yield break;
        }
        var g = new Vector3[4];
        glass.GetWorldCorners(g);
        yield return runner.Input.MoveTo((g[0] + g[2]) / 2f, 4); // over the glass's middle (the overlay canvas's world space is the screen)

        float timeout = step.f > 0f ? step.f : WaitSeconds;
        yield return EachMotion(() => MotionPreference.Reduced, reduced => MotionPreference.Reduced = reduced,
            reduced => PowerOnPass(runner, PowerOnId(reduced), view, keys, settings, timeout),
            (reduced, e) => runner.Assert(PowerOnId(reduced), Owner, false, $"stopped: {e.GetType().Name}: {e.Message}",
                "the pass did not finish (a wait timed out or it threw), so nothing was measured; Reduced Motion is put back all the same"));
    }

    /// <summary>The assert id of uipcPowerOn's pass at <paramref name="reduced"/>.</summary>
    private static string PowerOnId(bool reduced) => reduced ? "poweron.reduced" : "poweron.full";

    /// <summary>
    /// uipcPowerOn's two passes: <paramref name="pass"/> with full motion, then
    /// with Reduced Motion (<paramref name="write"/> sets the saved preference
    /// before each), and the setting <paramref name="read"/> gave at the start
    /// (the player's choice) always put back. Each pass runs under
    /// <see cref="QaRunner.Guard"/>, so an exception in it (QaGame.Until's
    /// TimeoutException) ends that pass only: it goes to
    /// <paramref name="stopped"/> with the pass's setting, and the next pass
    /// still runs, so both passes' asserts are always recorded. The restore is
    /// in a finally, so it also runs when the op is disposed during a pass.
    /// Pure (the preference and the asserts are the caller's), so UiPcRigTests
    /// drives it offline, as the runner does, with passes that throw.
    /// </summary>
    public static IEnumerator EachMotion(System.Func<bool> read, System.Action<bool> write, System.Func<bool, IEnumerator> pass, System.Action<bool, System.Exception> stopped)
    {
        bool chosen = read();
        try
        {
            foreach (bool reduced in new[] { false, true })
            {
                write(reduced);
                System.Exception error = null;
                yield return QaRunner.Guard(pass(reduced), e => error = e);
                if (error != null)
                    stopped(reduced, error);
            }
        }
        finally
        {
            write(chosen); // the player's choice, never left as a pass set it
        }
    }

    /// <summary>
    /// One pass of <see cref="PowerOn"/> at the motion setting in force,
    /// asserting <paramref name="id"/>: Q leaves the PC and the close is
    /// waited out, then Q opens it, and 2 frames later a right-click over the
    /// glass and then Esc arrive while the picture still moves
    /// (PcFrame.PictureMoving, about 300 ms: the desktop's clicks wait for the
    /// whole picture, its keys and back-out never do). Each press must be
    /// taken by the desktop in its own frame (DesktopKeyboard stamps every
    /// back-out with <see cref="DesktopKeyboard.EscapeTakenFrame"/>), so a
    /// press the rig failed to deliver fails instead of passing for nothing;
    /// the PC must stay open and Settings come forward. Each wait gives up
    /// after <paramref name="timeout"/> seconds (a TimeoutException).
    /// </summary>
    private static IEnumerator PowerOnPass(QaRunner runner, string id, OfficeViewController view, DesktopKeyboard keys, DesktopWindow settings, float timeout)
    {
        yield return runner.Input.Tap(Key.Q); // leave the PC (Q is one of its two ways out)
        yield return QaGame.Until(() => !PcOpen(view) && !PcFrame.PictureMoving, timeout, "the PC frame to close");
        if (settings.IsOpen)
            settings.Close(); // Settings must come forward by this Esc, not stay from an earlier step
        yield return runner.Input.Tap(Key.Q, 1); // open it: Tap returns on the frame after the one that took the press

        InputSystem.QueueStateEvent(runner.Input.Mouse, new MouseState { position = runner.Input.Position }.WithButton(MouseButton.Right));
        yield return null; // the game takes the right-click this frame, 2 frames after the Q
        bool rightEarly = PcFrame.PictureMoving;
        bool rightTaken = keys.EscapeTakenFrame == Time.frameCount; // the desktop's back-out took this right-click
        InputSystem.QueueStateEvent(runner.Input.Mouse, new MouseState { position = runner.Input.Position });
        yield return null;
        bool afterRight = PcOpen(view);

        InputSystem.QueueStateEvent(runner.Input.Keyboard, new KeyboardState(Key.Escape));
        yield return null; // the game takes the Esc this frame, 4 frames after the Q
        bool escEarly = PcFrame.PictureMoving;
        bool escTaken = keys.EscapeTakenFrame == Time.frameCount; // the desktop took this Esc
        InputSystem.QueueStateEvent(runner.Input.Keyboard, new KeyboardState());
        yield return QaGame.Until(() => !PcFrame.PictureMoving, timeout, "the PC's picture to be whole");
        yield return QaGame.Frames(2);

        bool open = PcOpen(view), front = settings.IsOpen && !settings.IsMinimised;
        runner.Assert(id, Owner, rightEarly && rightTaken && escEarly && escTaken && afterRight && open && front,
            $"right-click {(rightEarly ? "during" : "after")} the power-on, {(rightTaken ? "taken by the desktop" : "not taken by the desktop")}: pc {(afterRight ? "stayed open" : "closed")}; Esc {(escEarly ? "during" : "after")} it, {(escTaken ? "taken by the desktop" : "not taken by the desktop")}: pc {(open ? "open" : "closed")}, settings {(front ? "in front" : "not shown")}",
            "the keys and the back-out are the PC's from the press frame, so Esc opens Settings and nothing but Q and the bezel leaves (R3/R4); both presses must land while the picture moves, each taken by the desktop in its own frame");
    }

    /// <summary>
    /// Before a recording of the PC's open or close (the step's name, pc_open
    /// or pc_close): Reduced Motion is off, so the recording shows the CRT
    /// line that <c>uipc.transitions</c> judges, not the Reduced fade (asserts
    /// <c>fullmotion.&lt;name&gt;</c>). Reduced Motion is a saved preference:
    /// one set by hand in this project's prefs, or left on by a run stopped
    /// during uipcPowerOn's reduced pass, fails here instead of letting the
    /// cuts check pass on the fade.
    /// </summary>
    private static IEnumerator FullMotion(QaRunner runner, QaStep step)
    {
        bool full = !MotionPreference.Reduced;
        runner.Assert("fullmotion." + QaRunner.Safe(string.IsNullOrEmpty(step.name) ? "pc" : step.name), Owner, full,
            full ? "full motion" : "Reduced Motion is on",
            "the session records the PC's open and close with full motion (Settings > Motion: Full); with Reduced Motion they show the fade, and uipc.transitions would pass without measuring the CRT line");
        yield break;
    }

    // -----------------------------
    // A reversed power move (BUG-32c, F1)
    // -----------------------------

    /// <summary>The longest a UI-PC op waits for the PC frame by default, in seconds.</summary>
    private const float WaitSeconds = 3f;

    /// <summary>The most the frame's alpha may change from one frame's end to the next and stay under seqtools' cut against the room (PcDesktopRulesTests' CutStep: the frame is 63-77 % of the screen).</summary>
    private const float CutStep = 0.25f;

    /// <summary>How close two moments must be to show the same glass (CrtPower.Resume's promise on a reversed move, on float frame times).</summary>
    private const float SameGlassTolerance = 0.02f;

    /// <summary>A frame's end: PcFrame has drawn the frame by then (it steps in LateUpdate).</summary>
    private static readonly WaitForEndOfFrame FrameEnd = new WaitForEndOfFrame();

    /// <summary>
    /// A power-on turned back (the session records it as pc_reverse_open):
    /// from the office, Q opens the PC and a second Q 2 frames later closes it
    /// while the frame still fades in, before any picture. Asserts
    /// <c>reverse.open</c>: the second Q was taken while the picture moved,
    /// the glass on its frame is the frame before's (CrtPower.Resume: the
    /// frame fades out from its alpha, never whole for a frame), no step of
    /// the frame's alpha from one frame's end to the next is a cut's, and the
    /// frame ends gone with the PC closed. The review of 1eab563: with
    /// PcFrame.SetOpen back to replaying a move from its start, every offline
    /// test and uipc.transitions' plain open and close stayed green.
    /// </summary>
    private static IEnumerator ReverseOpen(QaRunner runner, QaStep step)
    {
        OfficeViewController view = QaGame.Find<OfficeViewController>();
        PcFrame frame = QaGame.Find<PcFrame>();
        if (view == null || frame == null || PcOpen(view) || PcFrame.PictureMoving)
        {
            runner.Assert("reverse.open", Owner, false, "no PC frame, or the PC is open or its frame still moves", "run it in the office with the PC closed and its frame gone");
            yield break;
        }
        var seen = new List<CrtFrame>();
        Keyboard keyboard = runner.Input.Keyboard;
        yield return Ends(frame, seen, 3); // the room before the press (and the recording's first frames)
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Q));
        yield return Ends(frame, seen, 1); // the game takes Q: the frame starts to fade in
        bool opened = PcOpen(view);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return Ends(frame, seen, 1);
        bool moving = PcFrame.PictureMoving;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Q));
        yield return Ends(frame, seen, 1); // the second Q, 2 frames after the first: the power-on turns back
        int turned = seen.Count - 1;
        bool closed = !PcOpen(view);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        bool done = false;
        yield return EndsUntil(frame, seen, () => !PcFrame.PictureMoving && !FrameShown(frame), step.f > 0f ? step.f : WaitSeconds, met => done = met);

        CrtFrame before = seen[turned - 1], at = seen[turned];
        bool same = SameGlass(before, at);
        float largest = LargestAlphaStep(seen);
        bool gone = done && !PcOpen(view);
        runner.Assert("reverse.open", Owner, opened && moving && closed && same && largest <= CutStep && gone,
            $"Q: pc {(opened ? "opened" : "not opened")}; Q 2 frames later, {(moving ? "during" : "after")} the power-on: {(closed ? "taken" : "not taken")}, glass {Describe(before)} -> {Describe(at)}; largest frame step {N(largest)}; {(gone ? "the frame faded out, pc closed" : done ? "pc open" : "still moving after the wait")}",
            "a power-on turned back fades the frame out from where it is, never the whole frame or picture for one frame (F1, CrtPower.Resume); seqtools cuts judges the recording pc_reverse_open under uipc.transitions");
    }

    /// <summary>
    /// A collapse turned back (the session records it as pc_reverse_close):
    /// with the PC open and whole, the pointer waits over the office's PC
    /// (BoothCoordinator's crt click box, under the open frame), Q closes the
    /// PC, and a click there is pressed 1 frame and released 2 frames after
    /// Q, during the collapse (at the session's 30 fps capture step, 67 ms
    /// into the 120 ms collapse, in its squeeze). The closing frame takes no
    /// click, so the click reaches the office's PC and opens it again.
    /// Asserts <c>reverse.close</c>: Q started the collapse, the press landed
    /// on the office's PC while the picture moved and the click opened the PC,
    /// the glass on its frame is the frame before's (the power-on goes on from
    /// the same line), no step of the frame's alpha is a cut's, and the
    /// picture ends whole with the PC open.
    /// </summary>
    private static IEnumerator ReverseClose(QaRunner runner, QaStep step)
    {
        OfficeViewController view = QaGame.Find<OfficeViewController>();
        PcFrame frame = QaGame.Find<PcFrame>();
        BoothCoordinator booth = QaGame.Find<BoothCoordinator>();
        var pc = booth != null ? QaGame.Field(booth, "crt") as Clickable : null;
        Vector2? at = pc != null ? QaGame.ScreenPoint(pc.gameObject) : null;
        if (view == null || frame == null || !at.HasValue || !PcOpen(view) || PcFrame.PictureMoving)
        {
            runner.Assert("reverse.close", Owner, false, at.HasValue ? "no PC frame, or the PC is closed or its picture still moves" : "the office's PC is not on the screen",
                "run it with the PC open and whole in the office");
            yield break;
        }
        yield return runner.Input.MoveTo(at.Value, 4); // the pointer waits over the office's PC, under the open frame
        var seen = new List<CrtFrame>();
        Keyboard keyboard = runner.Input.Keyboard;
        yield return Ends(frame, seen, 3); // the whole picture before the press
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Q));
        yield return Ends(frame, seen, 1); // the game takes Q: the picture starts to collapse, and the frame takes no click from now
        bool closing = !PcOpen(view);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        InputSystem.QueueStateEvent(runner.Input.Mouse, new MouseState { position = runner.Input.Position }.WithButton(MouseButton.Left));
        yield return Ends(frame, seen, 1); // the press, 1 frame after Q
        GameObject top = QaGame.TopAt(at.Value);
        bool onPc = top != null && top.transform.IsChildOf(pc.transform);
        bool moving = PcFrame.PictureMoving;
        InputSystem.QueueStateEvent(runner.Input.Mouse, new MouseState { position = runner.Input.Position });
        yield return Ends(frame, seen, 1); // the release, 2 frames after Q: the click opens the PC again during the collapse
        int turned = seen.Count - 1;
        bool reopened = PcOpen(view);
        bool done = false;
        yield return EndsUntil(frame, seen, () => !PcFrame.PictureMoving, step.f > 0f ? step.f : WaitSeconds, met => done = met);

        CrtFrame before = seen[turned - 1], now = seen[turned];
        bool same = SameGlass(before, now);
        float largest = LargestAlphaStep(seen);
        bool whole = done && PcOpen(view) && Whole(seen[seen.Count - 1]);
        runner.Assert("reverse.close", Owner, closing && onPc && moving && reopened && same && largest <= CutStep && whole,
            $"Q: pc {(closing ? "closing" : "still open")}; the click on the office's PC 2 frames later, {(moving ? "during" : "after")} the collapse, pressed on {(onPc ? "the PC" : top != null ? top.name : "nothing")}: pc {(reopened ? "opened again" : "not opened")}, glass {Describe(before)} -> {Describe(now)}; largest frame step {N(largest)}; {(whole ? "the picture whole, pc open" : done ? "the picture not whole" : "still moving after the wait")}",
            "a collapse turned back by a click on the PC powers on from where the glass is (F1, CrtPower.Resume), and the closing frame takes no click, so the click reaches the room; seqtools cuts judges the recording pc_reverse_close under uipc.transitions");
    }

    /// <summary>Waits for the end of each of the next <paramref name="frames"/> frames and adds what the glass showed then to <paramref name="seen"/>.</summary>
    private static IEnumerator Ends(PcFrame frame, List<CrtFrame> seen, int frames)
    {
        for (int i = 0; i < frames; i++)
        {
            yield return FrameEnd;
            seen.Add(Shown(frame));
        }
    }

    /// <summary>Adds what the glass shows at each frame's end to <paramref name="seen"/> until <paramref name="done"/> holds (<paramref name="met"/> gets true) or <paramref name="timeout"/> seconds pass (false). It never throws, so the op's assert is always recorded.</summary>
    private static IEnumerator EndsUntil(PcFrame frame, List<CrtFrame> seen, System.Func<bool> done, float timeout, System.Action<bool> met)
    {
        float end = Time.realtimeSinceStartup + timeout;
        while (!done())
        {
            if (Time.realtimeSinceStartup > end)
            {
                met(false);
                yield break;
            }
            yield return Ends(frame, seen, 1);
        }
        met(true);
    }

    /// <summary>What the glass shows now: the PC frame's last drawn moment while its overlay shows, else nothing (alpha 0, no picture).</summary>
    private static CrtFrame Shown(PcFrame frame) => FrameShown(frame) ? (CrtFrame)QaGame.Field(frame, "_drawn") : new CrtFrame(0f, 0f, 0f, 1f, false);

    /// <summary>True while the PC frame's overlay shows (its root is active).</summary>
    private static bool FrameShown(PcFrame frame) => QaGame.Field(frame, "root") is GameObject root && root != null && root.activeSelf;

    /// <summary>True when <paramref name="a"/> and <paramref name="b"/> show the same glass: the frame's alpha, the dark glass, the picture the eye sees and, both drawn, its line (PcDesktopRulesTests' AssertSameGlass, on the drawn frames).</summary>
    private static bool SameGlass(CrtFrame a, CrtFrame b)
    {
        bool same = Mathf.Abs(a.Bezel - b.Bezel) <= SameGlassTolerance && Mathf.Abs(a.Veil - b.Veil) <= SameGlassTolerance && Mathf.Abs(Seen(a) - Seen(b)) <= SameGlassTolerance;
        return same && (!a.Picture || !b.Picture || Mathf.Abs(a.X - b.X) <= SameGlassTolerance && Mathf.Abs(a.Y - b.Y) <= SameGlassTolerance);
    }

    /// <summary>How much of the desktop's picture the eye sees: the picture under the dark glass, which fades with the frame.</summary>
    private static float Seen(CrtFrame f) => f.Picture ? 1f - f.Bezel * f.Veil : 0f;

    /// <summary>True when <paramref name="f"/> is the whole picture: the full glass, the frame whole, the glass clear.</summary>
    private static bool Whole(CrtFrame f) => f.Picture && f.X >= 1f - SameGlassTolerance && f.Y >= 1f - SameGlassTolerance && f.Bezel >= 1f - SameGlassTolerance && f.Veil <= SameGlassTolerance;

    /// <summary>The largest change of the frame's alpha from one frame's end to the next in <paramref name="seen"/>.</summary>
    private static float LargestAlphaStep(List<CrtFrame> seen)
    {
        float largest = 0f;
        for (int i = 1; i < seen.Count; i++)
            largest = Mathf.Max(largest, Mathf.Abs(seen[i].Bezel - seen[i - 1].Bezel));
        return largest;
    }

    /// <summary>A moment of the glass for a measured line: the frame's alpha, then the picture's line or a dark glass.</summary>
    private static string Describe(CrtFrame f) => $"(alpha {N(f.Bezel)}, {(f.Picture ? "picture " + N(f.X) + " x " + N(f.Y) + ", dark " + N(f.Veil) : "no picture")})";

    /// <summary>A share with two decimals, the same in every culture.</summary>
    private static string N(float v) => v.ToString("0.00", CultureInfo.InvariantCulture);

    /// <summary>A click on the room right of the frame: the PC stays open.</summary>
    private static IEnumerator Room(QaRunner runner, QaStep step)
    {
        OfficeViewController view = QaGame.Find<OfficeViewController>();
        PcFrame frame = QaGame.Find<PcFrame>();
        Transform bezel = frame != null ? FindDeep(frame.transform, "Frame") : null;
        Rect? rect = bezel != null ? QaGame.ScreenRect(bezel.gameObject) : null;
        if (!rect.HasValue)
        {
            runner.Assert("room", Owner, false, "no PC frame", "the PC must be open in the office");
            yield break;
        }
        var point = new Vector2((rect.Value.xMax + Screen.width) / 2f, Screen.height * 0.5f); // halfway between the frame and the screen's right edge
        yield return runner.Input.Play(QaPaths.Click(point));
        yield return QaGame.Frames(3);
        bool open = PcOpen(view);
        runner.Assert("room", Owner, open, $"pc {(open ? "still open" : "closed")} after a click on the room at {point}",
            "only Q and the bezel leave the PC (R3/R4, 2026-10-20; the 2026-09-30 click-outside exit is overridden)");
    }

    /// <summary>A click on the bezel's left band (between its edge and the glass): the PC closes.</summary>
    private static IEnumerator Bezel(QaRunner runner, QaStep step)
    {
        OfficeViewController view = QaGame.Find<OfficeViewController>();
        PcFrame frame = QaGame.Find<PcFrame>();
        var glass = frame != null ? (RectTransform)typeof(PcFrame).GetField("glass", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(frame) : null;
        if (glass == null || !(glass.parent is RectTransform bezel))
        {
            runner.Assert("bezel", Owner, false, "no PC frame", "the frame's glass and bezel must be built");
            yield break;
        }
        var b = new Vector3[4];
        var g = new Vector3[4];
        bezel.GetWorldCorners(b);
        glass.GetWorldCorners(g);
        var point = new Vector2((b[0].x + g[0].x) / 2f, (g[0].y + g[1].y) / 2f); // the overlay canvas's world space is the screen
        yield return runner.Input.Play(QaPaths.Click(point));
        yield return QaGame.Frames(3);
        bool left = view != null && view.Current == OfficeView.OfficeFocus;
        runner.Assert("bezel", Owner, left, $"pc {(left ? "closed" : "still open")} after a click at {point}", "a click on the bezel leaves the PC");
    }

    // -----------------------------
    // The taskbar (BUG-20a, 21a)
    // -----------------------------

    /// <summary>Opens every app the Start menu lists today by clicking Start, then its entry; checks the taskbar's buttons and chevron against the open windows, then walks the chevron.</summary>
    private static IEnumerator Windows(QaRunner runner, QaStep step)
    {
        DesktopShell shell = QaGame.Find<DesktopShell>();
        DesktopWindowManager manager = QaGame.Find<DesktopWindowManager>();
        Transform start = shell != null ? FindDeep(shell.transform, "StartButton") : null;
        Transform menu = shell != null ? FindDeep(shell.transform, "StartMenu") : null;
        if (start == null || menu == null || manager == null)
        {
            runner.Assert("windows", Owner, false, "no Start button, menu or window manager", "the PC must be open in the office");
            yield break;
        }
        int opened = 0;
        foreach (string id in DesktopAppIds.DefaultOrder)
        {
            yield return ClickObject(runner, start);
            yield return QaGame.Frames(3);
            Transform entry = menu.Find("App_" + id);
            if (entry == null || !entry.gameObject.activeInHierarchy)
            {
                yield return runner.Input.Tap(Key.Escape); // not on the PC today: close the menu
                continue;
            }
            yield return ClickObject(runner, entry);
            yield return QaGame.Frames(6);
            opened++;
        }
        yield return runner.Input.Tap(Key.F1); // and the shortcut card, a window too
        yield return QaGame.Frames(5);

        int open = OpenWindows().Count;
        Transform strip = FindDeep(shell.transform, "WindowButtons");
        Transform tray = FindDeep(shell.transform, "Tray");
        Rect? trayRect = tray != null ? QaGame.ScreenRect(tray.gameObject) : null;
        int shown = 0, overTray = 0;
        foreach (Transform child in strip)
            if (child.name == "WindowButton" && child.gameObject.activeInHierarchy)
            {
                shown++;
                Rect? r = QaGame.ScreenRect(child.gameObject);
                if (r.HasValue && trayRect.HasValue && r.Value.xMax > trayRect.Value.xMin + 1f)
                    overTray++;
            }
        Transform chevron = strip.Find("OverflowButton");
        int waiting = chevron != null && chevron.gameObject.activeInHierarchy ? Count(chevron.GetComponentInChildren<TMP_Text>(true)) : 0;
        bool counted = open >= 8 && open >= opened && shown + waiting == open && overTray == 0;

        // The chevron brings each waiting window forward in turn: the focused window's button then shows.
        int walked = 0;
        for (int i = 0; i < waiting; i++)
        {
            yield return ClickObject(runner, chevron);
            yield return QaGame.Frames(5);
            DesktopWindow focused = manager.FocusedWindow;
            if (focused != null && ButtonOf(strip, focused) != null)
                walked++;
        }
        runner.Assert("windows", Owner, counted && walked == waiting,
            $"{opened} apps opened from Start and the F1 card, {open} windows open; {shown} buttons + {waiting} behind the chevron; {overTray} over the tray; the chevron brought {walked}/{waiting} forward",
            "8 windows: labelled buttons plus \"» N\", nothing over the tray, the chevron brings each waiting window forward (BUG-21a)");
    }

    /// <summary>The tray: the clock reads the shift clock (within a minute), the date line is today's, the CRT plate is not shown, and no credits are in the tray.</summary>
    private static IEnumerator Tray(QaRunner runner, QaStep step)
    {
        yield return QaGame.Frames(2);
        ShiftClockDriver driver = QaGame.Find<ShiftClockDriver>();
        OfficeUIController office = QaGame.Find<OfficeUIController>();
        TMP_Text clock = Text("ClockText"), date = Text("DayText");
        CrtClock plate = QaGame.Find<CrtClock>();
        string expected = driver != null && driver.Clock != null ? ShiftClock.Format(driver.Clock.CurrentMinute) : null;
        string shownTime = clock != null ? clock.text : null;
        bool timeOk = expected != null && shownTime != null && Minutes(shownTime) >= 0 && System.Math.Abs(Minutes(shownTime) - Minutes(expected)) <= 1;
        bool dateOk = date != null && !string.IsNullOrWhiteSpace(date.text) && date.text.IndexOf('·') < 0;
        bool plateGone = plate == null || !plate.gameObject.activeInHierarchy;
        bool noCredits = Text("MoneyText") == null;
        runner.Assert("tray", Owner, timeOk && dateOk && plateGone && noCredits && office != null,
            $"tray '{shownTime}' / '{(date != null ? date.text : "none")}', shift {expected}, plate {(plateGone ? "gone" : "shown")}, credits {(noCredits ? "out" : "in")} the tray",
            "a Windows-style tray clock, time over date, matching the shift clock; no CRT box; credits and the river out of the tray");
    }

    // -----------------------------
    // Orders (BUG-58a, 28d; UI-25)
    // -----------------------------

    /// <summary>Opens Orders, double-clicks the first orderable card: one order placed, its price charged once.</summary>
    private static IEnumerator Order(QaRunner runner, QaStep step)
    {
        WorldState world = RunManager.HasInstance ? RunManager.Instance.World : null;
        ContentLibrarySO lib = RunManager.HasInstance ? RunManager.Instance.Library : null;
        OrdersWindow orders = QaGame.Find<OrdersWindow>();
        DesktopApps apps = QaGame.Find<DesktopApps>();
        if (world == null || lib == null || orders == null || apps == null)
        {
            runner.Assert("order", Owner, false, "no run or no Orders app", "the PC session needs a run in the office");
            yield break;
        }
        if (!orders.Window.IsOpen || orders.Window.IsMinimised || orders.Window.Manager.FocusedWindow != orders.Window)
        {
            yield return ClickObject(runner, FindDeep(orders.Window.transform, "TitleText") ?? orders.Window.transform); // bring it forward as a player would: its title bar
            yield return QaGame.Frames(5);
        }
        UpgradeSO pick = null;
        foreach (UpgradeSO u in OrderBook.Catalogue(lib))
            if (OrderBook.StateOf(world, lib, u) == OrderState.Orderable)
            {
                pick = u;
                break;
            }
        Transform card = pick != null ? FindDeep(orders.transform, "Node_" + pick.id) : null;
        Vector2? at = card != null ? QaGame.ScreenPoint(card.gameObject) : null;
        if (!at.HasValue)
        {
            runner.Assert("order", Owner, false, pick == null ? "nothing orderable" : "the card is off screen",
                "give the run money (cheat AddMoney) and a day with Orders before the step");
            yield break;
        }
        int ordersBefore = world.orders.Count, moneyBefore = world.money, price = OrderBook.Price(world, lib, pick);
        yield return runner.Input.Play(QaPaths.Click(at.Value, 1));
        yield return runner.Input.Play(QaPaths.Click(at.Value, 1));
        yield return QaGame.Frames(5);
        int placed = world.orders.Count - ordersBefore, charged = moneyBefore - world.money;
        bool once = placed == 1 && charged == price && Orders.Pending(world.orders, pick.id) != null && orders.Chosen == pick.id;
        runner.Assert("order", Owner, once, $"orders +{placed}, charged {charged} (price {price}), chosen {orders.Chosen ?? "none"}",
            "a double-click on an orderable card places exactly one order (one-click ordering; never order-then-cancel)");
    }

    /// <summary>On the card ordered by uipcOrder, after the double-click time (uipcOrder's last click is about 17 frames back, inside it at an uncapped frame rate): Enter cancels it once (refunded once), Enter again after the double-click time orders it once, and two quick Enters act once.</summary>
    private static IEnumerator Enter(QaRunner runner, QaStep step)
    {
        WorldState world = RunManager.HasInstance ? RunManager.Instance.World : null;
        ContentLibrarySO lib = RunManager.HasInstance ? RunManager.Instance.Library : null;
        OrdersWindow orders = QaGame.Find<OrdersWindow>();
        UpgradeSO upgrade = lib != null && orders != null && orders.Chosen != null ? lib.GetUpgradeById(orders.Chosen) : null;
        if (world == null || upgrade == null || Orders.Pending(world.orders, upgrade.id) == null)
        {
            runner.Assert("enter", Owner, false, "no ordered card chosen", "run uipcOrder first");
            yield break;
        }
        float wait = orders.Window.Manager.DoubleClickSeconds + 0.15f;
        int money0 = world.money, price = OrderBook.Price(world, lib, upgrade);
        yield return new WaitForSecondsRealtime(wait); // clear of uipcOrder's clicks, so each Enter below is measured on its own
        yield return runner.Input.Tap(Key.Enter);
        yield return QaGame.Frames(3);
        bool cancelled = Orders.Pending(world.orders, upgrade.id) == null && world.money == money0 + price;
        yield return new WaitForSecondsRealtime(wait);
        yield return runner.Input.Tap(Key.Enter);
        yield return QaGame.Frames(3);
        bool reordered = Orders.Pending(world.orders, upgrade.id) != null && world.money == money0;
        yield return new WaitForSecondsRealtime(wait);
        yield return runner.Input.Tap(Key.Enter, 1);
        yield return runner.Input.Tap(Key.Enter, 1);
        yield return QaGame.Frames(3);
        bool once = Orders.Pending(world.orders, upgrade.id) == null && world.money == money0 + price;
        yield return new WaitForSecondsRealtime(wait);
        yield return runner.Input.Tap(Key.Enter); // ordered again, for the steps after
        yield return QaGame.Frames(3);
        bool back = Orders.Pending(world.orders, upgrade.id) != null && world.money == money0;
        runner.Assert("enter", Owner, cancelled && reordered && once && back,
            $"Enter: {(cancelled ? "cancelled, refunded once" : "not one cancel")}; Enter: {(reordered ? "ordered, charged once" : "not one order")}; two quick Enters: {(once ? "one action" : "not one action")}; money {world.money} (start {money0})",
            "one action per Enter, and two Enters within the double-click time act once (BUG-58a, CODE-117: Enter is the desktop's keys only)");
    }

    /// <summary>Ctrl+= twice with Orders focused: the tree is at 150 %.</summary>
    private static IEnumerator Zoom(QaRunner runner, QaStep step)
    {
        OrdersWindow orders = QaGame.Find<OrdersWindow>();
        TreeScrollRect view = orders != null ? orders.GetComponentInChildren<TreeScrollRect>(true) : null;
        if (view == null)
        {
            runner.Assert("zoom", Owner, false, "no Orders tree", "open Orders first (uipcOrder)");
            yield break;
        }
        yield return Chord(runner, Key.LeftCtrl, Key.Equals);
        yield return Chord(runner, Key.LeftCtrl, Key.Equals);
        yield return QaGame.Frames(5);
        runner.Assert("zoom", Owner, view.Level == 150, $"zoom {view.Level} %", "Ctrl+= steps the Orders tree 100 -> 125 -> 150 %");
    }

    /// <summary>Scrolls Orders' tree with the wheel, minimises it with its title bar's button and restores it with its taskbar button: the scroll, the chosen card and the zoom are kept.</summary>
    private static IEnumerator Restore(QaRunner runner, QaStep step)
    {
        OrdersWindow orders = QaGame.Find<OrdersWindow>();
        DesktopWindow window = orders != null ? orders.Window : null;
        TreeScrollRect view = orders != null ? orders.GetComponentInChildren<TreeScrollRect>(true) : null;
        DesktopShell shell = QaGame.Find<DesktopShell>();
        Transform strip = shell != null ? FindDeep(shell.transform, "WindowButtons") : null;
        Transform minimise = window != null ? FindDeep(window.transform, "MinBtn") : null;
        if (window == null || view == null || strip == null || minimise == null || !window.IsOpen)
        {
            runner.Assert("restore", Owner, false, "no open Orders window", "open Orders first (uipcOrder)");
            yield break;
        }
        Vector2? over = QaGame.ScreenPoint(view.gameObject);
        if (over.HasValue)
        {
            yield return runner.Input.MoveTo(over.Value, 5);
            yield return Wheel(runner, -3f);
        }
        yield return QaGame.Frames(5);
        Vector2 before = view.content.anchoredPosition;
        string chosen = orders.Chosen;
        int level = view.Level;
        yield return ClickObject(runner, minimise);
        yield return QaGame.Frames(15);
        bool minimised = window.IsMinimised;
        Transform button = ButtonOf(strip, window);
        if (button != null)
            yield return ClickObject(runner, button);
        yield return QaGame.Frames(15);
        Vector2 after = view.content.anchoredPosition;
        bool same = minimised && !window.IsMinimised && (after - before).sqrMagnitude < 1f && orders.Chosen == chosen && view.Level == level;
        runner.Assert("restore", Owner, same,
            $"wheel scroll {before}; minimised {minimised}; restored by its taskbar button {(button != null ? "yes" : "no button")}: scroll {after}, chosen {chosen ?? "none"} -> {orders.Chosen ?? "none"}, zoom {level} -> {view.Level} %",
            "an app keeps its state across minimise and restore");
    }

    /// <summary>Opens the Citizen Account from Start, scrolls it with the wheel, then money arrives (the AddMoney cheat): the Account redraws in place and the Account, Orders and the office's credits readout show the same balance. No credits readout bound (neither the art's nor the fallback HUD's) fails: the plan compares all three.</summary>
    private static IEnumerator Balance(QaRunner runner, QaStep step)
    {
        WorldState world = RunManager.HasInstance ? RunManager.Instance.World : null;
        DesktopShell shell = QaGame.Find<DesktopShell>();
        AccountWindow account = QaGame.Find<AccountWindow>();
        OrdersWindow orders = QaGame.Find<OrdersWindow>();
        Transform start = shell != null ? FindDeep(shell.transform, "StartButton") : null;
        Transform entry = shell != null ? FindDeep(shell.transform, "App_" + DesktopAppIds.CitizenAccount) : null;
        if (world == null || account == null || orders == null || start == null || entry == null)
        {
            runner.Assert("balance", Owner, false, "no run, Account, Orders or Start menu", "the PC must be open in the office with a run");
            yield break;
        }
        yield return ClickObject(runner, start);
        yield return QaGame.Frames(3);
        yield return ClickObject(runner, entry);
        yield return QaGame.Frames(8);
        ScrollRect scroll = account.GetComponentInChildren<ScrollRect>(true);
        Vector2? over = scroll != null ? QaGame.ScreenPoint(scroll.gameObject) : null;
        if (over.HasValue)
        {
            yield return runner.Input.MoveTo(over.Value, 5);
            yield return Wheel(runner, -4f);
        }
        yield return QaGame.Frames(5);
        Vector2 before = scroll != null ? scroll.content.anchoredPosition : Vector2.zero;
        DevCheats.Run(DevCheat.AddMoney, RunManager.Instance, 50, 0f, null);
        yield return QaGame.Frames(5);
        Vector2 after = scroll != null ? scroll.content.anchoredPosition : Vector2.zero;
        int money = world.money;
        bool kept = scroll != null && (after - before).sqrMagnitude < 1f;
        bool inAccount = Shows(account.transform, AccountWindow.Amount(money));
        TMP_Text wallet = (TMP_Text)QaGame.Field(orders, "walletText");
        bool inOrders = wallet != null && wallet.text == OrderLines.Wallet(world);
        OfficeReadouts readouts = QaGame.Find<OfficeReadouts>();
        TMP_Text credits = readouts != null ? (TMP_Text)QaGame.Field(readouts, "_creditsText") : null;
        bool inHud = credits != null && credits.text == money.ToString(CultureInfo.InvariantCulture);
        runner.Assert("balance", Owner, kept && inAccount && inOrders && inHud,
            $"money {money}: Account {(inAccount ? "shows it" : "stale")} (scroll {before} -> {after}), Orders '{(wallet != null ? wallet.text : "none")}', office credits '{(credits != null ? credits.text : "no readout bound: OfficeReadouts has no credits text (the art's ReadoutCredits anchor nor the fallback HUD's)")}'",
            "the wallet is the same everywhere and the Account follows it live without losing its place (BUG-58a, CODE-121)");
    }

    // -----------------------------
    // Input and lookups
    // -----------------------------

    /// <summary>A left click at the middle of <paramref name="target"/> (skipped when it is not on screen).</summary>
    private static IEnumerator ClickObject(QaRunner runner, Transform target)
    {
        Vector2? at = target != null ? QaGame.ScreenPoint(target.gameObject) : null;
        if (at.HasValue)
            yield return runner.Input.Play(QaPaths.Click(at.Value));
    }

    /// <summary>A right click where the pointer is (the virtual mouse's right button, two frames).</summary>
    private static IEnumerator RightClick(QaRunner runner)
    {
        InputSystem.QueueStateEvent(runner.Input.Mouse, new MouseState { position = runner.Input.Position }.WithButton(MouseButton.Right));
        yield return null;
        yield return null;
        InputSystem.QueueStateEvent(runner.Input.Mouse, new MouseState { position = runner.Input.Position });
        yield return null;
    }

    /// <summary>A chord: <paramref name="modifier"/> held with <paramref name="key"/> for two frames, then both let go.</summary>
    private static IEnumerator Chord(QaRunner runner, Key modifier, Key key)
    {
        InputSystem.QueueStateEvent(runner.Input.Keyboard, new KeyboardState(modifier, key));
        yield return null;
        yield return null;
        InputSystem.QueueStateEvent(runner.Input.Keyboard, new KeyboardState());
        yield return null;
    }

    /// <summary>The wheel turned <paramref name="notches"/> notches where the pointer is (negative: down), one notch a frame.</summary>
    private static IEnumerator Wheel(QaRunner runner, float notches)
    {
        int count = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(notches)));
        float sign = Mathf.Sign(notches);
        for (int i = 0; i < count; i++)
        {
            InputSystem.QueueStateEvent(runner.Input.Mouse, new MouseState { position = runner.Input.Position, scroll = new Vector2(0f, sign * WheelNotch) });
            yield return null;
            InputSystem.QueueStateEvent(runner.Input.Mouse, new MouseState { position = runner.Input.Position });
            yield return null;
        }
    }

    /// <summary>True while the PC is open (the office's view is on the monitor).</summary>
    private static bool PcOpen(OfficeViewController view) => view != null && view.Current == OfficeView.MonitorFocus;

    /// <summary>The desktop's open windows (minimised ones included).</summary>
    private static List<DesktopWindow> OpenWindows()
    {
        var open = new List<DesktopWindow>();
        foreach (DesktopWindow w in Object.FindObjectsByType<DesktopWindow>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (w.IsOpen)
                open.Add(w);
        return open;
    }

    /// <summary>The shown taskbar button that names <paramref name="window"/>, or null.</summary>
    private static Transform ButtonOf(Transform strip, DesktopWindow window)
    {
        foreach (Transform child in strip)
            if (child.name == "WindowButton" && child.gameObject.activeInHierarchy)
            {
                Transform label = child.Find("Label");
                if (label != null && label.GetComponent<TMP_Text>().text == window.Title)
                    return child;
            }
        return null;
    }

    /// <summary>The count in the chevron's label ("» 3" is 3), or 0.</summary>
    private static int Count(TMP_Text label)
    {
        string digits = string.Empty;
        foreach (char c in label != null ? label.text : string.Empty)
            if (char.IsDigit(c))
                digits += c;
        return int.TryParse(digits, out int n) ? n : 0;
    }

    /// <summary>True when a text under <paramref name="root"/> shows <paramref name="value"/>.</summary>
    private static bool Shows(Transform root, string value)
    {
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(false))
            if (text.text != null && text.text.Contains(value))
                return true;
        return false;
    }

    /// <summary>The desktop window of the app whose controller is <typeparamref name="T"/>.</summary>
    private static DesktopWindow Window<T>() where T : Component
    {
        T controller = QaGame.Find<T>();
        return controller != null ? controller.GetComponent<DesktopWindow>() : null;
    }

    /// <summary>A tray text by name under the desktop's taskbar, or null.</summary>
    private static TMP_Text Text(string name)
    {
        DesktopShell shell = QaGame.Find<DesktopShell>();
        Transform t = shell != null ? FindDeep(shell.transform.Find("Taskbar"), name) : null;
        return t != null ? t.GetComponent<TMP_Text>() : null;
    }

    /// <summary>The first descendant named <paramref name="name"/>, or null.</summary>
    private static Transform FindDeep(Transform root, string name)
    {
        if (root == null)
            return null;
        if (root.name == name)
            return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeep(root.GetChild(i), name);
            if (found != null)
                return found;
        }
        return null;
    }

    /// <summary>"HH:MM" as minutes of the day, or -1.</summary>
    private static int Minutes(string hhmm)
    {
        string[] parts = (hhmm ?? string.Empty).Split(':');
        return parts.Length == 2 && int.TryParse(parts[0], out int h) && int.TryParse(parts[1], out int m) ? h * 60 + m : -1;
    }
}
#endif
