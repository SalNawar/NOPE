#if UNITY_EDITOR || TIMEDESK_QA
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
/// and off again: <c>transit.&lt;name&gt;</c>. Record it with
/// <c>flowRecLoad</c> for seqtools' frozen and cuts analyses.</item>
/// <item><c>flowRecLoad</c> (name): records the next scene change alone
/// (FLOW round 7), for seqtools' frozen and cuts analyses: the sequence
/// seq/&lt;name&gt; starts on the frame the load is accepted (SceneTransit's
/// start mark: the click's frame, or the call's) and stops on the frame its
/// shutter is off (the up mark). A Title or a settled scene that holds still
/// before the click or after the shutter is not in it, so seqtools frozen
/// reads the load alone. Put it before the step that starts the scene change,
/// and wait for the shutter after that step (<c>flowSettled</c>;
/// <c>flowTransit</c> waits by itself). It asserts nothing: a load that
/// never went through the shutter leaves no sequence, so the analyses'
/// results are missing and a check that requires them is "not measured".
/// The frame before the click is not in the sequence either: a cut on the
/// click's own frame needs a recording that starts earlier (recStart, as
/// flow_transitions' do).</item>
/// <item><c>flowQueue</c> (name; text: three scenes, comma-separated, each a
/// scene's name or <c>office</c>; f timeout, default 120 s): TransitRunner's
/// queue in play (FLOW round 5). It loads the first scene through
/// SceneTransit.Load, asks for the second a frame later while the shutter
/// comes down, and for the third as soon as the shutter starts up over the
/// second, then waits for the change to end:
/// <c>transit.queue.&lt;name&gt;</c>, true when both asks came at those
/// moments, the first scene never loaded (the second replaced it before any
/// load), the shutter came all the way down again after the second scene's
/// rise, the third scene is the one loaded, SceneTransit.Changing ended
/// false with the veil off, and the transit marks came in start/rise/up order
/// (the first's start, the second's start, rise and up, then the third's
/// start, rise and up).</item>
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
/// <item><c>flowSettled</c> (f timeout, default 120 s): waits until no scene
/// change runs (SceneTransit.Changing: from the accepted load until the
/// shutter is off). Put it after a cheat that reloads the office (jumpDay,
/// RestartShift, SkipDay) and before <c>waitOffice</c>: the office being
/// replaced stays loaded under the shutter, its briefing up, while
/// RunManager's world already shows the new day, so <c>waitOffice</c> alone
/// passes on the old office. QA-RIG's own waits take the same condition at
/// INTEGRATION (docs/pending/FLOW.md, section 4).</item>
/// <item><c>flowRunState</c> (name; text: an earlier record's name; n 1:
/// without the back issues too): records the run as it stands (RunManager's
/// world: day, phase, money, clerkDebtPaid, the statement's rows; and the
/// save file). With <c>text</c>, asserts <c>same.&lt;name&gt;</c>: all of them
/// match that record, the saves compared without what presenting a traveller
/// may legitimately mark (a premade met, a returning traveller come back, the
/// desk guide's progress) and, with n 1, without the News site's back issues
/// (each office records its morning paper as it opens: a day a second skip
/// passed over under the shutter never had its office open).</item>
/// <item><c>flowWatchDayEnds</c> (name): starts counting the day ends the
/// offices handle (GameManager's "Entering HandleDayCompleted" lines).</item>
/// <item><c>flowDayEnds</c> (name; n the count expected; text the days
/// expected, comma-separated): stops the count and asserts
/// <c>dayEnds.&lt;name&gt;</c>: exactly n day ends, on those days.</item>
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
        if (!QaRunner.IsKnownOp("flowQueue"))
            QaRunner.RegisterOp("flowQueue", Queue);
        if (!QaRunner.IsKnownOp("flowRecLoad"))
            QaRunner.RegisterOp("flowRecLoad", RecordLoad);
        if (!QaRunner.IsKnownOp("flowLoadClock"))
            QaRunner.RegisterOp("flowLoadClock", LoadClock);
        if (!QaRunner.IsKnownOp("flowLoadTime"))
            QaRunner.RegisterOp("flowLoadTime", LoadTime);
        if (!QaRunner.IsKnownOp("flowSettled"))
            QaRunner.RegisterOp("flowSettled", Settled);
        if (!QaRunner.IsKnownOp("flowRunState"))
            QaRunner.RegisterOp("flowRunState", RunStateOp);
        if (!QaRunner.IsKnownOp("flowWatchDayEnds"))
            QaRunner.RegisterOp("flowWatchDayEnds", WatchDayEnds);
        if (!QaRunner.IsKnownOp("flowDayEnds"))
            QaRunner.RegisterOp("flowDayEnds", DayEndsOp);
        // A new play session starts with no records, no count and no load recording armed (the editor without a domain reload keeps statics).
        States.Clear();
        StopDayEnds();
        DisarmLoadRecording();
    }

    private static IEnumerator Settled(QaRunner runner, QaStep step)
    {
        yield return QaGame.Until(() => !SceneTransit.Changing, step.f > 0f ? step.f : 120f, "the scene change's shutter to be off");
    }

    /// <summary>What flowRunState records: the run's world as it stands and its save file (as read; masked when compared).</summary>
    private sealed class RunRecord
    {
        public int Day;
        public RunPhase Phase;
        public int Money;
        public int DebtPaid;
        public int StatementRows;
        public string Save;

        public override string ToString() =>
            $"day {Day}, phase {Phase}, money {Money}, clerkDebtPaid {DebtPaid}, statement rows {StatementRows}, save {(Save != null ? Save.Length + " chars" : "missing")}";
    }

    /// <summary>The save file read for comparison (the world without its version wrapper's private type).</summary>
    [System.Serializable]
    private sealed class SaveCopy
    {
        public int version;
        public WorldState world;
    }

    /// <summary>flowRunState's records of this play session, by name.</summary>
    private static readonly Dictionary<string, RunRecord> States = new Dictionary<string, RunRecord>();

    private static IEnumerator RunStateOp(QaRunner runner, QaStep step)
    {
        yield return QaGame.Frames(2);
        WorldState world = RunManager.HasInstance ? RunManager.Instance.World : null;
        var now = new RunRecord
        {
            Day = world != null ? world.day : 0,
            Phase = world != null ? world.phase : RunPhase.Office,
            Money = world != null ? world.money : 0,
            DebtPaid = world != null ? world.clerkDebtPaid : 0,
            StatementRows = world != null && world.accountDays != null ? world.accountDays.Count : 0,
            Save = File.Exists(SaveSystem.SavePath) ? File.ReadAllText(SaveSystem.SavePath) : null,
        };
        string name = string.IsNullOrEmpty(step.name) ? "state" : step.name;
        States[name] = now;
        if (string.IsNullOrEmpty(step.text))
            yield break;

        bool papers = step.n == 1;
        bool known = States.TryGetValue(step.text, out RunRecord then);
        string mine = Comparable(now.Save, papers), theirs = known ? Comparable(then.Save, papers) : null;
        bool fields = known && now.Day == then.Day && now.Phase == then.Phase && now.Money == then.Money && now.DebtPaid == then.DebtPaid && now.StatementRows == then.StatementRows;
        bool saves = known && mine != null && theirs != null && mine == theirs;
        runner.Assert($"same.{name}", Owner, known && fields && saves && world != null,
            known ? $"{name}: {now}; {step.text}: {then}; saves {(saves ? "match" : "differ")}{(papers ? " (back issues aside)" : string.Empty)}" : $"no record named '{step.text}'",
            "Rounds 4 and 5: a day is closed once; a Skip day from the office mid-shift lands as a skip of the same day from the briefing, and a second Skip day under the shutter as one after it lifted (no second strandings, instalment, dialog outcomes, statement row, Home phase or overwritten morning save)"
                + (known && !saves ? "; " + FirstDifference(theirs, mine) : string.Empty));
    }

    /// <summary>
    /// The save's world as JSON without what presenting a traveller may
    /// legitimately mark (DayCycle.Present: a once-per-run premade met, a
    /// returning traveller come back; the desk guide's progress through the
    /// shift), so a run that played part of a shift compares with one that
    /// never opened it; with <paramref name="withoutPapers"/>, without the
    /// News site's back issues too (GameManager records each morning paper as
    /// its office opens, so a day passed over under the shutter has none).
    /// Null for a missing file or one that does not read as a save.
    /// </summary>
    private static string Comparable(string json, bool withoutPapers)
    {
        if (json == null)
            return null;
        SaveCopy copy;
        try
        {
            copy = JsonUtility.FromJson<SaveCopy>(json);
        }
        catch (System.ArgumentException)
        {
            return null;
        }
        if (copy == null || copy.world == null)
            return null;
        WorldState w = copy.world;
        string marker = FlagKeys.PremadeMet("\u0001");
        int at = marker.IndexOf('\u0001');
        string prefix = marker.Substring(0, at), suffix = marker.Substring(at + 1);
        w.flags?.RemoveAll(f => f != null && f.StartsWith(prefix, System.StringComparison.Ordinal) && f.EndsWith(suffix, System.StringComparison.Ordinal));
        if (w.returns != null)
            foreach (ReturningTraveller r in w.returns)
                if (r != null)
                    r.returned = false;
        w.guide = new GuideState();
        if (withoutPapers)
            w.newsArchive = new List<NewsIssue>();
        return copy.version + "|" + JsonUtility.ToJson(w);
    }

    /// <summary>Where two compared saves first differ, with a little of each around it.</summary>
    private static string FirstDifference(string a, string b)
    {
        if (a == null || b == null)
            return a == null ? "the first save is missing or unreadable" : "the second save is missing or unreadable";
        int i = 0;
        while (i < a.Length && i < b.Length && a[i] == b[i])
            i++;
        int from = System.Math.Max(0, i - 40);
        string Around(string s) => i >= s.Length ? "(end)" : s.Substring(from, System.Math.Min(s.Length - from, 120));
        return $"the saves first differ at character {i}: '{Around(a)}' against '{Around(b)}'";
    }

    /// <summary>The line GameManager logs as an office handles its day's end.</summary>
    private const string DayEndLine = ">>> Entering HandleDayCompleted (day ";

    /// <summary>The days of the day ends counted since flowWatchDayEnds.</summary>
    private static readonly List<int> DayEnds = new List<int>();

    /// <summary>True while the log is watched for day ends.</summary>
    private static bool _watchingDayEnds;

    private static IEnumerator WatchDayEnds(QaRunner runner, QaStep step)
    {
        StopDayEnds();
        Application.logMessageReceived += OnDayEndLog;
        _watchingDayEnds = true;
        yield break;
    }

    private static void StopDayEnds()
    {
        if (_watchingDayEnds)
            Application.logMessageReceived -= OnDayEndLog;
        _watchingDayEnds = false;
        DayEnds.Clear();
    }

    private static void OnDayEndLog(string message, string stack, LogType type)
    {
        int at = message != null ? message.IndexOf(DayEndLine, System.StringComparison.Ordinal) : -1;
        if (at < 0)
            return;
        int start = at + DayEndLine.Length, end = message.IndexOf(')', start);
        DayEnds.Add(end > start && int.TryParse(message.Substring(start, end - start), out int day) ? day : 0);
    }

    private static IEnumerator DayEndsOp(QaRunner runner, QaStep step)
    {
        yield return QaGame.Frames(2);
        bool watched = _watchingDayEnds;
        var seen = new List<int>(DayEnds);
        StopDayEnds();
        var expected = new List<int>();
        foreach (string part in (step.text ?? string.Empty).Split(','))
            if (int.TryParse(part.Trim(), out int day))
                expected.Add(day);
        bool count = seen.Count == step.n;
        bool days = expected.Count == 0 || string.Join(",", seen) == string.Join(",", expected);
        runner.Assert($"dayEnds.{step.name}", Owner, watched && count && days,
            watched ? $"{seen.Count} day end(s) handled (expected {step.n}), on day(s) [{string.Join(", ", seen)}]" + (expected.Count > 0 ? $" (expected [{string.Join(", ", expected)}])" : string.Empty)
                    : "not watched: flowWatchDayEnds must come first",
            "Round 4, S2: a day is closed once; the office a cheat's route closed and left behind the shutter handles no day's end of its own");
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

    /// <summary>The runner whose recorder flowRecLoad drives (null when no load recording is armed).</summary>
    private static QaRunner _loadRunner;

    /// <summary>The armed load recording's sequence folder.</summary>
    private static string _loadSeq;

    /// <summary>True from the armed load's start mark until its up mark: its sequence records.</summary>
    private static bool _loadRecording;

    private static IEnumerator RecordLoad(QaRunner runner, QaStep step)
    {
        // An earlier load recording still waiting for its up mark ends here.
        if (_loadRecording && _loadRunner != null)
            _loadRunner.Recorder.Stop();
        DisarmLoadRecording();
        _loadRunner = runner;
        _loadSeq = Path.Combine(runner.OutDir, "seq", QaRunner.Safe(string.IsNullOrEmpty(step.name) ? "load" : step.name));
        QaSignals.Raised += OnLoadSignal;
        yield break;
    }

    /// <summary>The armed load recording follows the scene change's marks: its sequence starts on the start mark and stops on the up mark.</summary>
    private static void OnLoadSignal(string channel, string name)
    {
        if (channel != TransitRunner.TransitSignal || string.IsNullOrEmpty(name))
            return;
        if (_loadRunner == null)
        {
            DisarmLoadRecording();
            return;
        }
        if (!_loadRecording && name.StartsWith("start:"))
        {
            _loadRunner.Recorder.Begin(_loadSeq);
            // The recorder heard this mark before its sequence began: it is written as the sequence's first event.
            _loadRunner.Recorder.Mark(channel, name);
            _loadRecording = true;
        }
        else if (_loadRecording && name.StartsWith("up:"))
        {
            _loadRunner.Recorder.Stop();
            DisarmLoadRecording();
        }
    }

    /// <summary>Forgets the armed load recording (a sequence it started and did not stop ends with the run).</summary>
    private static void DisarmLoadRecording()
    {
        QaSignals.Raised -= OnLoadSignal;
        _loadRunner = null;
        _loadSeq = null;
        _loadRecording = false;
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

    /// <summary>How far down the shutter is (0 up out of view, 1 covering the screen), or -1 without one.</summary>
    private static float ShutterCover()
    {
        TransitShutter veil = QaGame.Find<TransitShutter>();
        return veil != null ? (float)QaGame.Field(veil, "_cover") : -1f;
    }

    private static IEnumerator Queue(QaRunner runner, QaStep step)
    {
        string id = $"transit.queue.{(string.IsNullOrEmpty(step.name) ? "queue" : step.name)}";
        string[] scenes = (step.text ?? string.Empty).Split(',').Select(s => s.Trim()).Select(s => s == "office" ? OfficeScenes.ArtSceneName : s).ToArray();
        if (scenes.Length != 3 || scenes.Any(string.IsNullOrEmpty))
        {
            runner.Assert(id, Owner, false, $"text '{step.text}'", "flowQueue needs three scenes in text: the first, the one asked for while the shutter comes down, the one asked for while it rises");
            yield break;
        }
        yield return QaGame.Until(() => !SceneTransit.Changing, 60f, "no scene change before the queue's first load");

        var marks = new List<string>();
        void Mark(string channel, string name)
        {
            if (channel == TransitRunner.TransitSignal)
                marks.Add(name);
        }
        int Count(string kind) => marks.Count(m => m.StartsWith(kind + ":"));
        QaSignals.Raised += Mark;
        // The first scene must never load (the second replaces it before any load): judged only when it is not loaded already.
        bool firstAtStart = Loaded(scenes[0]), firstLoaded = false, downAgain = false, lowering, rising;
        float cover;
        try
        {
            SceneTransit.Load(scenes[0]);
            yield return null;
            // The second ask, while the shutter comes down: only the first start marked, the shutter shown and not yet down.
            cover = ShutterCover();
            lowering = ShutterShown() && cover >= 0f && cover < 1f && marks.Count == 1;
            SceneTransit.Load(scenes[1]);

            float end = Time.realtimeSinceStartup + (step.f > 0f ? step.f : 120f);
            while (Time.realtimeSinceStartup < end && Count("rise") == 0)
            {
                firstLoaded |= Loaded(scenes[0]);
                yield return null;
            }
            // The third ask, while the shutter rises over the second scene: no up mark yet, the shutter still shown.
            rising = Count("rise") == 1 && Count("up") == 0 && ShutterShown();
            SceneTransit.Load(scenes[2]);

            while (Time.realtimeSinceStartup < end && SceneTransit.Changing)
            {
                // Between the second scene's up and the third's rise, the shutter must cover the screen again.
                if (Count("up") == 1 && Count("rise") == 1)
                    downAgain |= ShutterShown() && ShutterCover() >= 0.999f;
                yield return null;
            }
            yield return QaGame.Frames(2);
        }
        finally
        {
            QaSignals.Raised -= Mark;
        }

        var expected = new List<string>
        {
            "start:" + scenes[0], "start:" + scenes[1], "rise:" + scenes[1], "up:" + scenes[1],
            "start:" + scenes[2], "rise:" + scenes[2], "up:" + scenes[2],
        };
        bool ordered = marks.SequenceEqual(expected);
        bool there = Loaded(scenes[2]);
        bool settled = !SceneTransit.Changing && !SceneTransit.Loading, off = !ShutterShown();
        bool skipped = firstAtStart || !firstLoaded;
        runner.Assert(id, Owner, lowering && rising && skipped && downAgain && ordered && there && settled && off,
            $"'{scenes[1]}' asked for while the shutter came down {lowering} (cover {cover:0.00}), '{scenes[0]}' never loaded {skipped}{(firstAtStart ? " (loaded before: not judged)" : string.Empty)}; "
                + $"'{scenes[2]}' asked for while it rose {rising}, the shutter down again {downAgain}; '{scenes[2]}' loaded {there}; "
                + $"change over {settled}, veil off {off}; marks [{string.Join(", ", marks)}]{(ordered ? string.Empty : $" (expected [{string.Join(", ", expected)}])")}",
            "Round 5, S4: TransitRunner's queue in play: a load asked for while the shutter comes down replaces the scene before any load, one asked for while it rises brings it down again, and the last scene asked for is the one loaded");
    }

    /// <summary>True while the scene named <paramref name="scene"/> is loaded.</summary>
    private static bool Loaded(string scene)
    {
        Scene s = SceneManager.GetSceneByName(scene);
        return s.IsValid() && s.isLoaded;
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
