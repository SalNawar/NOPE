#if UNITY_EDITOR || TIMEDESK_QA
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// FLOW's QA rig ops (docs/pending/FLOW.md; QA-RIG's recipe: "Esc in the
/// office, Home and Title reaches Settings; the clock does not advance while
/// Settings is open; the last traveller's payoff before the report"). Each
/// asserts "&lt;scenario&gt;.&lt;id&gt;" for FLOW; FlowChecks reads them.
/// <list type="bullet">
/// <item><c>flowSettings</c> (name, f seconds): Settings shows (after the
/// scenario's <c>key Escape</c>), and the shift clock, if there is one, does
/// not move over f seconds: <c>settings.&lt;name&gt;</c>.</item>
/// <item><c>flowFocus</c> (f seconds): the window's focus lost as the player
/// (runInBackground off for the step) holds the clock and pauses the audio,
/// and its return lets both go: <c>focus</c>.</item>
/// <item><c>flowPayoff</c> (f timeout): plays out the day's end, failing if the
/// report shows while the last traveller is still leaving: <c>payoff</c>.</item>
/// <item><c>flowEdition</c> (f timeout): the evening edition is laid over the
/// title after an ending, with its Title and Quit buttons: <c>edition</c>.</item>
/// <item><c>flowClosed</c> (name, text, f timeout): waits for the shift clock to
/// reach closing time, and the clock then reads <c>text</c> (the night
/// shift's "24:00"): <c>closed.&lt;name&gt;</c>.</item>
/// <item><c>flowEarlyFinish</c> (name): End Shift before closing time (the
/// cheat menu's) raises the clock's Closed once, so the closing bell
/// (FeelDirector) rings on an early finish: <c>bell.&lt;name&gt;</c>.</item>
/// <item><c>flowTransit</c> (name: a scene, or <c>office</c> for the art office
/// RunConfig names; f timeout): loads the scene through
/// SceneTransit.Load, as RunManager's loads do (RUN-SAFE)
/// (BUG-31a, the bible's J5): the booth's shutter is seen down, the scene
/// (and the office's gameplay layer) loads behind it, and the shutter is up
/// and off again: <c>transit.&lt;name&gt;</c>. Record it (recStart/recStop)
/// for seqtools' frozen and cuts analyses.</item>
/// <item><c>flowLoadClock</c> (name): starts the real-time load watch
/// (FlowLoadWatch) before the step that starts a scene change (newRun, a Go
/// Home button, RunManager.Sleep). Record nothing during the load: the
/// capture's own cost would be timed.</item>
/// <item><c>flowLoadTime</c> (name; n frame budget in ms, default 250; f the
/// visible budget in seconds, 0 to report only): waits (at most 60 s) for
/// that load's shutter to be off, then asserts
/// <c>load.&lt;name&gt;.frames</c> (no real frame from the click to the shutter
/// off is longer than n ms: the [BP] "no frame held over 0.25 s" in real
/// time, which a fixed-step capture cannot see) and
/// <c>load.&lt;name&gt;.time</c> (the new scene shows, the shutter starting
/// up, within f seconds of the click; BUG-31a's "load at most 1.0 s").</item>
/// </list>
/// </summary>
public static class FlowOps
{
    private const string Owner = "FLOW";

#if UNITY_EDITOR
    /// <summary>Also known in edit mode, so the scenario tests (QaEditorGateTests) read FLOW's recipes with known ops.</summary>
    [UnityEditor.InitializeOnLoadMethod]
    private static void RegisterInEditor() => Register();
#endif

    /// <summary>
    /// Registers FLOW's ops, each once (the editor without a domain reload
    /// keeps the rig's table). Each is a literal <c>QaRunner.RegisterOp("name"</c>
    /// call: tools/qa/test_seqlib.py reads the known ops from those calls, so a
    /// recipe's op is checked against what the rig really has.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        if (!QaRunner.IsKnownOp("flowSettings"))
            QaRunner.RegisterOp("flowSettings", Settings);
        if (!QaRunner.IsKnownOp("flowFocus"))
            QaRunner.RegisterOp("flowFocus", Focus);
        if (!QaRunner.IsKnownOp("flowPayoff"))
            QaRunner.RegisterOp("flowPayoff", Payoff);
        if (!QaRunner.IsKnownOp("flowEdition"))
            QaRunner.RegisterOp("flowEdition", Edition);
        if (!QaRunner.IsKnownOp("flowClosed"))
            QaRunner.RegisterOp("flowClosed", Closed);
        if (!QaRunner.IsKnownOp("flowEarlyFinish"))
            QaRunner.RegisterOp("flowEarlyFinish", EarlyFinish);
        if (!QaRunner.IsKnownOp("flowTransit"))
            QaRunner.RegisterOp("flowTransit", SceneChange);
        if (!QaRunner.IsKnownOp("flowLoadClock"))
            QaRunner.RegisterOp("flowLoadClock", LoadClock);
        if (!QaRunner.IsKnownOp("flowLoadTime"))
            QaRunner.RegisterOp("flowLoadTime", LoadTime);
    }

    /// <summary>The load watch flowLoadClock started (one at a time).</summary>
    private static FlowLoadWatch _loadWatch;

    private static IEnumerator LoadClock(QaRunner runner, QaStep step)
    {
        if (_loadWatch != null)
            _loadWatch.End();
        _loadWatch = FlowLoadWatch.Begin();
        if (step.n > 0)
            _loadWatch.LongFrameMs = step.n;
        yield break;
    }

    private static IEnumerator LoadTime(QaRunner runner, QaStep step)
    {
        FlowLoadWatch watch = _loadWatch;
        string name = string.IsNullOrEmpty(step.name) ? "load" : step.name;
        double budgetMs = step.n > 0 ? step.n : 250.0;
        if (watch == null)
        {
            runner.Assert($"load.{name}.frames", Owner, false, "no load watch", "flowLoadClock must come before the step that starts the scene change");
            yield break;
        }
        float end = Time.realtimeSinceStartup + 60f;
        while (Time.realtimeSinceStartup < end && !watch.Done)
            yield return null;
        // One more frame, so the frame the shutter went off in is timed too.
        yield return null;

        bool started = watch.Started >= 0, done = watch.Done;
        string scene = started ? $"'{watch.Scene}'" : "no scene change";
        runner.Assert($"load.{name}.frames", Owner, started && done && watch.WorstFrameMs <= budgetMs,
            started
                ? $"{scene}: worst real frame {watch.WorstFrameMs:0} ms of {watch.Frames} frames from the click to the shutter off, {watch.LongFrames} over {budgetMs:0} ms; shutter off {done}"
                : "no SceneTransit load was seen (no 'transit' start mark): the load did not go through the shutter",
            $"BUG-31a [BP]: no frame held over {budgetMs:0} ms in real time across the load (a fixed-step capture's 'frozen' cannot see a long activation frame)");
        double visible = started && watch.Rising >= 0 ? watch.Rising - watch.Started : double.NaN;
        double off = started && done ? watch.Up - watch.Started : double.NaN;
        bool judged = step.f > 0f;
        runner.Assert($"load.{name}.time", Owner, !double.IsNaN(visible) && (!judged || visible <= step.f),
            $"{scene}: shown {(double.IsNaN(visible) ? "never" : visible.ToString("0.00") + " s")} after the click (the shutter starting up), off at {(double.IsNaN(off) ? "never" : off.ToString("0.00") + " s")}"
                + (judged ? $"; budget {step.f:0.0#} s" : "; reported only"),
            "BUG-31a [BP]: the scene is shown within the budget of the click (FIX_PLAN: load at most 1.0 s)");
        watch.End();
        if (_loadWatch == watch)
            _loadWatch = null;
    }

    /// <summary>The shift clock's minute now (NaN without a shift).</summary>
    private static float Minute() => ShiftClockDriver.Live != null ? ShiftClockDriver.Live.MinuteOfDay : float.NaN;

    private static IEnumerator Settings(QaRunner runner, QaStep step)
    {
        yield return QaGame.Frames(2);
        bool showing = PauseMenu.SettingsShowing;
        float before = Minute();
        float end = Time.realtimeSinceStartup + Mathf.Max(0.5f, step.f);
        while (Time.realtimeSinceStartup < end)
            yield return null;
        float after = Minute();
        bool still = float.IsNaN(before) || Mathf.Approximately(before, after);
        runner.Assert($"settings.{step.name}", Owner, showing && still && PauseMenu.SettingsShowing,
            $"Settings shown {showing}, clock {before:0.00} -> {after:0.00} over {step.f:0.#} s",
            "D20/R3-R4: Esc opens Settings here, and the shift clock does not advance while it is open (BUG-34b)");
    }

    private static IEnumerator Focus(QaRunner runner, QaStep step)
    {
        bool background = Application.runInBackground;
        Application.runInBackground = false;
        PauseMenu.FocusChanged(false);
        float before = Minute();
        bool held = PauseState.Shared.Holds(PauseState.Focus), muted = AudioListener.pause;
        float end = Time.realtimeSinceStartup + Mathf.Max(0.5f, step.f);
        while (Time.realtimeSinceStartup < end)
            yield return null;
        float after = Minute();
        PauseMenu.FocusChanged(true);
        Application.runInBackground = background;
        bool released = !PauseState.Shared.Holds(PauseState.Focus) && !AudioListener.pause;
        bool still = float.IsNaN(before) || Mathf.Approximately(before, after);
        runner.Assert("focus", Owner, held && muted && still && released,
            $"held {held}, audio paused {muted}, clock {before:0.00} -> {after:0.00} over {step.f:0.#} s, released {released}",
            "BUG-34a: the focus lost (the player, runInBackground off) holds the clock and pauses the audio; its return lets both go");
    }

    private static IEnumerator Payoff(QaRunner runner, QaStep step)
    {
        GameManager game = GameManager.Current;
        DayFlowUIController flow = QaGame.Find<DayFlowUIController>();
        float end = Time.realtimeSinceStartup + (step.f > 0f ? step.f : 60f);
        bool early = false, reported = false, sawDeparture = false;
        while (Time.realtimeSinceStartup < end && !reported)
        {
            bool departing = game != null && game.Desk.IsDeparting;
            sawDeparture |= departing;
            reported = flow != null && QaGame.IsShown(QaGame.Field(flow, "resultsPanel"));
            early |= reported && departing;
            yield return null;
        }
        runner.Assert("payoff", Owner, reported && !early,
            $"report shown {reported}, a departure seen {sawDeparture}, report over a leaving traveller {early}",
            "BUG-32a: the last traveller's reaction and walk-off play before the report");
    }

    private static IEnumerator Edition(QaRunner runner, QaStep step)
    {
        EveningEditionView edition = null;
        float end = Time.realtimeSinceStartup + (step.f > 0f ? step.f : 30f);
        while (Time.realtimeSinceStartup < end && (edition = QaGame.Find<EveningEditionView>()) == null)
            yield return null;
        yield return QaGame.Frames(3);
        bool title = edition != null && edition.transform.Find("Page/Buttons/TitleButton") != null;
        bool quit = edition != null && edition.transform.Find("Page/Buttons/QuitButton") != null;
        runner.Assert("edition", Owner, edition != null && title && quit,
            $"edition {(edition != null)}, Title {title}, Quit {quit}",
            "BUG-56a: an ending is the evening edition, with Title and Quit");
    }

    private static IEnumerator Closed(QaRunner runner, QaStep step)
    {
        float end = Time.realtimeSinceStartup + (step.f > 0f ? step.f : 600f);
        ShiftClockDriver driver = ShiftClockDriver.Live as ShiftClockDriver;
        while (Time.realtimeSinceStartup < end && (driver == null || driver.Clock == null || !driver.Clock.IsClosed))
        {
            yield return null;
            driver = ShiftClockDriver.Live as ShiftClockDriver;
        }
        bool closed = driver != null && driver.Clock != null && driver.Clock.IsClosed;
        string shows = closed ? ShiftClock.Format(driver.Clock.CurrentMinute) : "--:--";
        bool reads = string.IsNullOrEmpty(step.text) || shows == step.text;
        runner.Assert($"closed.{step.name}", Owner, closed && reads,
            $"closed {closed}, the clock reads {shows}" + (string.IsNullOrEmpty(step.text) ? string.Empty : $" (expected {step.text})"),
            "BUG-72: the shift closes at its closing time, and the night shift's midnight reads 24:00");
    }

    private static IEnumerator SceneChange(QaRunner runner, QaStep step)
    {
        string scene = step.name == "office" ? OfficeScenes.ArtSceneName : step.name;
        SceneTransit.Load(scene);
        bool down = false;
        float end = Time.realtimeSinceStartup + (step.f > 0f ? step.f : 60f);
        while (Time.realtimeSinceStartup < end && SceneTransit.Loading)
        {
            down |= ShutterShown();
            yield return null;
        }
        float raised = Time.realtimeSinceStartup + 2f;
        while (Time.realtimeSinceStartup < raised && ShutterShown())
            yield return null;
        Scene loaded = SceneManager.GetSceneByName(scene);
        bool there = loaded.IsValid() && loaded.isLoaded, up = !ShutterShown();
        runner.Assert($"transit.{step.name}", Owner, down && there && up && !SceneTransit.Loading,
            $"shutter seen down {down}, '{scene}' loaded {there}, shutter up and off {up}, still loading {SceneTransit.Loading}",
            "BUG-31a / J5: a scene change brings the booth's shutter down, loads behind it and lifts it once the scenes have settled");
    }

    /// <summary>True while the scene change's shutter (TransitShutter) is shown.</summary>
    private static bool ShutterShown()
    {
        TransitShutter veil = QaGame.Find<TransitShutter>();
        return veil != null && veil.gameObject.activeInHierarchy;
    }

    private static IEnumerator EarlyFinish(QaRunner runner, QaStep step)
    {
        ShiftClockDriver driver = ShiftClockDriver.Live as ShiftClockDriver;
        GameManager game = GameManager.Current;
        int raised = 0;
        void Count() => raised++;
        if (driver != null)
            driver.Closed += Count;
        float before = driver != null && driver.Clock != null ? driver.Clock.CurrentMinute : float.NaN;
        string said = game != null ? game.CheatEndShift() : "no GameManager";
        yield return QaGame.Frames(3);
        if (driver != null)
            driver.Closed -= Count;
        bool early = driver != null && driver.Clock != null && driver.Clock.IsClosed && driver.Clock.CurrentMinute < driver.Clock.EndMinute;
        runner.Assert($"bell.{step.name}", Owner, raised == 1 && early,
            $"Closed raised {raised} time(s), the clock stopped at {(float.IsNaN(before) ? "--:--" : ShiftClock.Format(before))} before closing {early} ({said})",
            "BUG-72: an early finish (End Shift, the queue run out) raises Closed once, so the closing bell rings");
    }
}
#endif
