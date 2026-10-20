#if UNITY_EDITOR || TIMEDESK_QA
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using TMPro;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Plays a <see cref="QaScenario"/> like a player (P0-B QA-RIG, BUG-53): the
/// virtual mouse and keyboard (QaInput) press what is on the screen where it
/// is, the recorder (SeqRecorder) captures 30 fps sequences and stills, and
/// the camera logger (CameraMoveLog) attributes every camera move. The same
/// runner plays in the editor (QaPlayHarness for the job runner, QaPlayTest
/// for UTF play tests) and in a QA player build (started from the command
/// line: -qaScenario &lt;file&gt; [-qaOut &lt;folder&gt;]). It writes, into its
/// output folder: run.json (every step, every assertion; a
/// <see cref="QaRunReport"/>), warnings.txt (every warning and error logged),
/// camera.tsv, drags.tsv, seq/&lt;name&gt;/ and stills/, then done.txt. A step
/// that throws or times out stops the run and fails it. Every run asserts a
/// clean console, a camera that moved only for tagged causes, and presses
/// that reached their targets under the pointer. A run stopped before its
/// end puts the input, frame rate, seed and hour back and writes a failed
/// run.json (done.txt "aborted").
/// </summary>
[DefaultExecutionOrder(-32000)]
public sealed class QaRunner : MonoBehaviour
{
    /// <summary>The run playing now, or null.</summary>
    public static QaRunner Active { get; private set; }

    /// <summary>Raised when a run has written its report.</summary>
    public static event Action<QaRunReport> Finished;

    /// <summary>The scenario.</summary>
    public QaScenario Scenario { get; private set; }

    /// <summary>The output folder.</summary>
    public string OutDir { get; private set; }

    /// <summary>True once the report is written.</summary>
    public bool Done { get; private set; }

    /// <summary>The report (once <see cref="Done"/>).</summary>
    public QaRunReport Report { get; private set; }

    /// <summary>The virtual input.</summary>
    public QaInput Input { get; private set; }

    /// <summary>The recorder.</summary>
    public SeqRecorder Recorder { get; private set; }

    /// <summary>The camera logger.</summary>
    public CameraMoveLog Cameras { get; private set; }

    private readonly List<QaStepResult> _steps = new List<QaStepResult>();
    private readonly List<QaAssertion> _assertions = new List<QaAssertion>();
    private bool _quitWhenDone;
    private int _errors, _warnings;
    private StreamWriter _warningLog;
    private StreamWriter _drags;
    private int? _oldSeed;
    private bool _hourForced;
    private string _started;
    private bool _measuringGc;
    private int? _dragRestoreRate;
    private bool _cleanedUp;
    private string _expectedQuality;
    private string _startQuality;
    private int _presses;
    private readonly List<string> _blocked = new List<string>();
    private readonly List<string> _direct = new List<string>();

    /// <summary>The suffix of a QA player's product name (BuiltPlayerCapture builds it so): its saves, PlayerPrefs and window settings live apart from the game's, and a QA player of another name refuses to run.</summary>
    public const string QaProductSuffix = " QA";

    /// <summary>Every op a step may name (lower case; docs/pending/QA-RIG.md describes each).</summary>
    public static readonly string[] Ops =
    {
        "title", "newrun", "waitoffice", "startshift", "sign", "waittraveller", "hour", "jumpday", "cheat", "call", "button", "click",
        "drag", "key", "move", "wait", "waitscene", "res", "recstart", "recstop", "record", "still", "track", "probe", "gc", "selftest",
        "cameracheck", "mark", "note", "quit", "waitactive", "realtime", "drop",
    };

    private static readonly Dictionary<string, Func<QaRunner, QaStep, IEnumerator>> TrackOps = new Dictionary<string, Func<QaRunner, QaStep, IEnumerator>>();

    /// <summary>
    /// Adds a track's own op (lower case; not one of <see cref="Ops"/>): its
    /// body runs as a step, with the runner's input, recorder, camera log and
    /// <see cref="Assert"/>. A track registers its ops from its own rig file
    /// (Assets/Tests/PlayMode/&lt;Track&gt;/Rig/, inside
    /// <c>#if UNITY_EDITOR || TIMEDESK_QA</c>) in a
    /// [RuntimeInitializeOnLoadMethod(BeforeSceneLoad)] method, so the ops
    /// exist in the editor and in the QA player alike.
    /// </summary>
    public static void RegisterOp(string name, Func<QaRunner, QaStep, IEnumerator> body)
    {
        string key = (name ?? string.Empty).Trim().ToLowerInvariant();
        if (key.Length == 0 || Ops.Contains(key))
            throw new ArgumentException($"'{name}' is empty or a built-in op.");
        TrackOps[key] = body ?? throw new ArgumentNullException(nameof(body));
    }

    /// <summary>True when <paramref name="op"/> is a built-in or registered op.</summary>
    public static bool IsKnownOp(string op)
    {
        string key = (op ?? string.Empty).Trim().ToLowerInvariant();
        return Ops.Contains(key) || TrackOps.ContainsKey(key);
    }

    /// <summary>The steps of <paramref name="scenario"/> whose op is unknown, as "step i: op" (empty when every op is known).</summary>
    public static List<string> UnknownOps(QaScenario scenario) =>
        scenario.steps.Select((s, i) => (s, i)).Where(p => !IsKnownOp(p.s.op))
                      .Select(p => $"step {p.i}: '{p.s.op}'").ToList();

    /// <summary>
    /// Starts <paramref name="scenario"/>, writing into <paramref name="outDir"/>;
    /// a player quits at the end when <paramref name="quitWhenDone"/>. With
    /// <paramref name="expectedQuality"/> (the editor harness: the Standalone
    /// player's level name) the run asserts <c>quality</c>: it started and
    /// ended at that level (BUG-53: QA plays at the shipping level). Refuses a
    /// scenario with an unknown op before anything runs.
    /// </summary>
    public static QaRunner Begin(QaScenario scenario, string outDir, bool quitWhenDone = false, string expectedQuality = null)
    {
        if (Active != null && !Active.Done)
            throw new InvalidOperationException("A QA run is already playing.");
        List<string> unknown = UnknownOps(scenario);
        if (unknown.Count > 0)
            throw new ArgumentException($"Scenario {scenario.name} has unknown ops: {string.Join(", ", unknown)}.");
        var go = new GameObject("QaRunner");
        DontDestroyOnLoad(go);
        QaRunner runner = go.AddComponent<QaRunner>();
        runner.Scenario = scenario;
        runner.OutDir = outDir;
        runner._quitWhenDone = quitWhenDone;
        runner._expectedQuality = expectedQuality;
        Active = runner;
        runner.StartCoroutine(runner.Run());
        return runner;
    }

#if TIMEDESK_QA && !UNITY_EDITOR
    /// <summary>
    /// A QA player build: starts the scenario named by -qaScenario (into
    /// -qaOut, else persistentDataPath/QA) and quits when it ends. It refuses
    /// (exit code 3) unless its product name ends in
    /// <see cref="QaProductSuffix"/>: a run plays New Run, saves and sets the
    /// window, so in a player named like the game it would overwrite the
    /// player's real save and settings.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void FromCommandLine()
    {
        string file = Argument("-qaScenario");
        if (file == null)
            return;
        if (!Application.productName.EndsWith(QaProductSuffix, StringComparison.Ordinal))
        {
            UnityEngine.Debug.LogError($"[QA] Refused: this player is '{Application.productName}', not a QA player (a name ending in '{QaProductSuffix}'), so a run would play over the game's own save and settings. Build it with QaJobs.BuildQaPlayer.");
            Application.Quit(3);
            return;
        }
        QaScenario scenario = QaScenario.Load(file);
        string outDir = Argument("-qaOut") ?? Path.Combine(Application.persistentDataPath, "QA", scenario.name + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        Begin(scenario, outDir, true);
    }

    private static string Argument(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
#endif

    private void Update() => Input?.Poll();

    private IEnumerator Run()
    {
        Directory.CreateDirectory(OutDir);
        _started = DateTime.Now.ToString("s");
        _startQuality = QualityName();
        _warningLog = new StreamWriter(Path.Combine(OutDir, "warnings.txt"));
        _drags = new StreamWriter(Path.Combine(OutDir, "drags.tsv"));
        _drags.WriteLine("name\tpath\tpressX\tpressY\tendX\tendY\tpressFrame\treleaseFrame\tscreenW\tscreenH");
        Application.logMessageReceived += OnLog;
        Application.runInBackground = true;
        Input = new QaInput();
        Recorder = gameObject.AddComponent<SeqRecorder>();
        Cameras = gameObject.AddComponent<CameraMoveLog>();
        // The rig's own logging stays out of what a GC window and a timing sequence measure.
        Cameras.QuietWhen = () => _measuringGc || Recorder != null && Recorder.Timing;
        Cameras.WriteTo(OutDir);

        for (int i = 0; i < Scenario.steps.Length; i++)
        {
            QaStep step = Scenario.steps[i];
            var result = new QaStepResult { i = i, op = step.op, ok = true };
            var watch = Stopwatch.StartNew();
            bool quit = false;
            yield return Guard(Do(step, note => result.note = note, () => quit = true), e =>
            {
                result.ok = false;
                result.note = $"{e.GetType().Name}: {e.Message}";
            });
            result.seconds = (float)watch.Elapsed.TotalSeconds;
            _steps.Add(result);
            if (!result.ok || quit)
                break;
        }
        Finish();
    }

    /// <summary>Runs <paramref name="body"/> (nested enumerators included) and stops at the first exception, handing it to <paramref name="onError"/>.</summary>
    public static IEnumerator Guard(IEnumerator body, Action<Exception> onError)
    {
        var stack = new Stack<IEnumerator>();
        stack.Push(body);
        while (stack.Count > 0)
        {
            IEnumerator top = stack.Peek();
            bool moved;
            try
            {
                moved = top.MoveNext();
            }
            catch (Exception e)
            {
                onError(e);
                yield break;
            }
            if (!moved)
            {
                stack.Pop();
                continue;
            }
            if (top.Current is IEnumerator nested)
            {
                stack.Push(nested);
                continue;
            }
            yield return top.Current;
        }
    }

    private IEnumerator Do(QaStep s, Action<string> note, Action quit)
    {
        switch ((s.op ?? string.Empty).Trim().ToLowerInvariant())
        {
            case "title":
                yield return QaGame.Until(() => QaGame.Find<TitleUIController>() != null && RunManager.HasInstance, Timeout(s, 60f), "the title");
                if (Scenario.seed != 0 && RunManager.Instance.Config != null)
                {
                    _oldSeed = _oldSeed ?? RunManager.Instance.Config.fixedRunSeed;
                    RunManager.Instance.Config.fixedRunSeed = Scenario.seed;
                }
                yield return QaGame.Frames(10);
                break;
            case "newrun":
            {
                TitleUIController title = QaGame.Find<TitleUIController>() ?? throw new InvalidOperationException("No title is loaded.");
                yield return Press((Button)QaGame.Field(title, "newRunButton"), s, note);
                yield return QaGame.Frames(5);
                ((TMP_InputField)QaGame.Field(title, "adoptNameInput")).text = string.IsNullOrEmpty(s.text) ? "Biscuit" : s.text;
                yield return Press((Button)QaGame.Field(title, "adoptButton"), s, note);
                break;
            }
            case "waitoffice":
                yield return QaGame.Until(() => QaGame.OfficeLoaded() && (s.n <= 0 || QaGame.Day() == s.n) && QaGame.BriefingShown(),
                    Timeout(s, 120f), s.n > 0 ? $"day {s.n}'s briefing in the office" : "the office's briefing");
                yield return QaGame.Frames(30);
                break;
            case "startshift":
                yield return Press(QaGame.ButtonAt("DayFlowUIController.startShiftButton"), s, note);
                yield return QaGame.Until(() => !QaGame.BriefingShown(), 10f, "the briefing to close");
                break;
            case "sign":
            {
                var sign = (Component)QaGame.Field(QaGame.Find<GameManager>(), "readySign");
                yield return ClickObject(sign.gameObject, s, note);
                break;
            }
            case "waittraveller":
                yield return QaGame.Until(QaGame.TravellerAtDesk, Timeout(s, 60f), "a traveller at the desk");
                break;
            case "hour":
                DevToolsState.ForcedHour = s.f < 0f ? (float?)null : s.f;
                _hourForced = true;
                yield return QaGame.Frames(5);
                break;
            case "jumpday":
                RunManager.Instance.JumpToDay(s.n);
                yield return QaGame.Frames(5);
                break;
            case "cheat":
                DevCheats.Run((DevCheat)Enum.Parse(typeof(DevCheat), s.name, true), RunManager.Instance, s.n, s.f, s.text);
                note(DevCheats.LastResult);
                yield return QaGame.Frames(5);
                break;
            case "call":
                QaGame.Call(s.target);
                yield return QaGame.Frames(2);
                break;
            case "button":
                yield return Press(QaGame.ButtonAt(s.target), s, note);
                break;
            case "click":
                if (!string.IsNullOrEmpty(s.target))
                    yield return ClickObject(QaGame.GameObjectAt(s.target), s, note);
                else
                    yield return Input.Play(QaPaths.Click(Frac(s.x, s.y)));
                break;
            case "drag":
                yield return Drag(s, note, false);
                break;
            case "drop":
                yield return Drag(s, note, true);
                break;
            case "key":
                yield return Input.Tap((Key)Enum.Parse(typeof(Key), s.key, true), Math.Max(1, s.n));
                break;
            case "move":
                yield return Input.MoveTo(Frac(s.x, s.y), s.n > 0 ? s.n : 10);
                break;
            case "wait":
                if (s.n > 0)
                    yield return QaGame.Frames(s.n);
                else
                {
                    float end = Time.unscaledTime + s.f;
                    while (Time.unscaledTime < end)
                        yield return null;
                }
                break;
            case "waitscene":
                yield return QaGame.Until(() => SceneManager.GetSceneByName(s.name).isLoaded, Timeout(s, 60f), $"the scene {s.name}");
                yield return QaGame.Frames(10);
                break;
            case "res":
                yield return Resolution(s.w, s.h);
                break;
            case "recstart":
                Recorder.Begin(Path.Combine(OutDir, "seq", Safe(s.name)));
                break;
            case "recstop":
                Recorder.Stop();
                break;
            case "record":
            {
                Recorder.Begin(Path.Combine(OutDir, "seq", Safe(s.name)));
                int frames = Mathf.CeilToInt(Mathf.Max(0.1f, s.f) * SeqRecorder.Fps);
                yield return QaGame.Frames(frames);
                Recorder.Stop();
                break;
            }
            case "still":
                Recorder.Still(Path.Combine(OutDir, "stills", $"{Safe(s.name)}_{Screen.width}x{Screen.height}.png"));
                yield return QaGame.Until(() => !Recorder.StillPending, 10f, "the still");
                break;
            case "track":
            {
                GameObject go = QaGame.GameObjectAt(s.target);
                Recorder.Track(string.IsNullOrEmpty(s.name) ? go.name : s.name, () => go != null ? QaGame.ScreenRect(go) : null);
                break;
            }
            case "probe":
                Probe(string.IsNullOrEmpty(s.name) ? $"step{_steps.Count}" : s.name);
                break;
            case "gc":
                yield return Gc(s);
                break;
            case "selftest":
                yield return QaSelfTest.Run(Input, (id, pass, measured, detail) => Assert(id, "QA-RIG", pass, measured, detail));
                break;
            case "cameracheck":
                AssertCamera("camera." + Safe(s.name));
                break;
            case "waitactive":
                yield return QaGame.Until(() => QaGame.IsShown(QaGame.Member(s.target)), Timeout(s, 60f), $"{s.target} to show");
                yield return QaGame.Frames(10);
                break;
            case "realtime":
                // n = 1: the next sequences are timing ones (real frame times, no capture, nothing allocated per frame), for the hitch and frame-time measurements.
                Recorder.FixedStep = s.n == 0;
                break;
            case "mark":
                QaSignals.Event("qa", s.name);
                yield return null;
                break;
            case "note":
                note(s.text);
                break;
            case "quit":
                quit();
                break;
            default:
                if (!TrackOps.TryGetValue((s.op ?? string.Empty).Trim().ToLowerInvariant(), out Func<QaRunner, QaStep, IEnumerator> body))
                    throw new ArgumentException($"Unknown op '{s.op}'.");
                yield return body(this, s);
                break;
        }
    }

    private static float Timeout(QaStep s, float fallback) => s.f > 0f ? s.f : fallback;

    private static Vector2 Frac(float x, float y) => new Vector2(x * Screen.width, y * Screen.height);

    /// <summary>
    /// Clicks <paramref name="button"/> with the virtual mouse where it shows.
    /// When something else is on top there (an overlay, BUG-68's class) it
    /// still invokes it so the run goes on, but unless the step says
    /// <see cref="QaStep.direct"/> the press is counted as blocked and the
    /// run's <c>presses</c> assertion fails (BUG-53: no handler is called
    /// directly behind the player's back).
    /// </summary>
    private IEnumerator Press(Button button, QaStep step, Action<string> note)
    {
        if (button == null)
            throw new ArgumentNullException(nameof(button));
        yield return QaGame.Until(() => button.isActiveAndEnabled && button.interactable, 10f, $"{button.name} to be clickable");
        _presses++;
        Vector2? p = QaGame.ScreenPoint(button.gameObject);
        GameObject top = p.HasValue ? QaGame.TopAt(p.Value) : null;
        if (top != null && top.transform.IsChildOf(button.transform))
        {
            yield return Input.Play(QaPaths.Click(p.Value));
            yield break;
        }
        Unreached(step, button.name, top, "invoked directly", note);
        button.onClick.Invoke();
        yield return null;
    }

    /// <summary>Clicks <paramref name="go"/> with the virtual mouse at its screen point; when it is off screen or covered, sends the click event and counts the press as blocked as <see cref="Press"/> does.</summary>
    private IEnumerator ClickObject(GameObject go, QaStep step, Action<string> note)
    {
        _presses++;
        Vector2? p = QaGame.ScreenPoint(go);
        GameObject top = p.HasValue ? QaGame.TopAt(p.Value) : null;
        if (top != null && (top.transform.IsChildOf(go.transform) || go.transform.IsChildOf(top.transform)))
        {
            yield return Input.Play(QaPaths.Click(p.Value));
            yield break;
        }
        Unreached(step, go.name, top, "click event sent", note);
        ExecuteEvents.Execute(go, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
        yield return null;
    }

    /// <summary>Records a press the virtual mouse could not deliver: blocked (fails <c>presses</c>) unless the step allows it with <see cref="QaStep.direct"/>.</summary>
    private void Unreached(QaStep step, string target, GameObject top, string how, Action<string> note)
    {
        string what = $"step {_steps.Count} {step.op}: {target} was not under the pointer at its screen point ({(top != null ? QaGame.Where(top) : "nothing")} was)";
        if (step.direct)
        {
            _direct.Add(what);
            note($"{what}: {how} (direct: true)");
        }
        else
        {
            _blocked.Add(what);
            note($"BLOCKED {what}: {how} so the run goes on; the presses assertion fails (set \"direct\": true only for a target meant to be reached so)");
        }
    }

    /// <summary>
    /// A drag along a P-* path from <c>target</c> (its screen point, so the
    /// grab offset is the real one) or the point (x, y), to <c>target2</c> or
    /// (x2, y2). With <paramref name="checkRest"/> (the drop op) it asserts
    /// where the dragged object comes to rest: its screen centre after
    /// <c>n</c> settle frames (default 45) may be at most <c>f</c> pixels at
    /// 1080p (default 8, D1's "8 px in the release frame") from where it was
    /// on the release frame (BUG-53's per-drop assertion; a push after the
    /// drop, like 1019a's ClearOfBlockers, fails it). The path plays one
    /// sample per frame, so the whole drag and its settle run at the fixed
    /// 30 fps game step (Time.captureFramerate, as a recording does): the
    /// P-* speeds (QaPaths, in px per second at 30 fps) and the settle's
    /// seconds hold at any real frame rate, in the editor and the player.
    /// </summary>
    private IEnumerator Drag(QaStep s, Action<string> note, bool checkRest)
    {
        GameObject dragged = string.IsNullOrEmpty(s.target) ? null : QaGame.GameObjectAt(s.target);
        if (checkRest && dragged == null)
            throw new ArgumentException("drop needs a target: the object dragged.");
        Vector2 from = dragged == null ? Frac(s.x, s.y) : QaGame.ScreenPoint(dragged) ?? throw new InvalidOperationException($"{s.target} is not on the screen.");
        Vector2 to = string.IsNullOrEmpty(s.target2) ? Frac(s.x2, s.y2) : QaGame.ScreenPoint(QaGame.GameObjectAt(s.target2)) ?? throw new InvalidOperationException($"{s.target2} is not on the screen.");
        QaPathKind kind = ParseKind(s.path);
        QaPath path = QaPaths.Drag(kind, from, to, new Vector2(Screen.width, Screen.height));
        int pressFrame = 0, releaseFrame = 0;
        Rect? atRelease = null;
        bool fixedHere = Time.captureFramerate != SeqRecorder.Fps;
        if (fixedHere)
        {
            _dragRestoreRate = Time.captureFramerate;
            Time.captureFramerate = SeqRecorder.Fps;
        }
        yield return Input.Play(path, i =>
        {
            // The sample queued on this frame is seen by the game on the next one.
            if (i == path.PressIndex) pressFrame = Time.frameCount + 1;
            if (i == path.ReleaseIndex)
            {
                releaseFrame = Time.frameCount + 1;
                atRelease = dragged != null ? QaGame.ScreenRect(dragged) : null;
            }
        });
        string name = Safe(string.IsNullOrEmpty(s.name) ? "drag" : s.name);
        _drags.WriteLine(string.Join("\t", name, kind, F(path.Start.x), F(path.Start.y), F(path.End.x), F(path.End.y),
            pressFrame, releaseFrame, Screen.width, Screen.height));
        _drags.Flush();
        note($"{kind} from {F(path.Start.x)},{F(path.Start.y)} released at {F(path.End.x)},{F(path.End.y)}");
        if (checkRest)
            yield return QaGame.Frames(s.n > 0 ? s.n : 45);
        if (fixedHere)
        {
            Time.captureFramerate = _dragRestoreRate ?? 0;
            _dragRestoreRate = null;
        }
        if (!checkRest)
            yield break;
        Rect? rest = QaGame.ScreenRect(dragged);
        float limit = s.f > 0f ? s.f : 8f;
        if (atRelease == null || rest == null)
        {
            Assert($"drop.{name}", Scenario.owner, false, "not on screen", $"{dragged.name} was not on the screen at the release or at rest ({kind}).");
            yield break;
        }
        float moved = QaCameraMoves.To1080((rest.Value.center - atRelease.Value.center).magnitude, Screen.height);
        Assert($"drop.{name}", Scenario.owner, moved <= limit, $"{moved:0.0} px after the release (max {limit:0.#})",
            $"{dragged.name} ({kind}): released with its centre at {atRelease.Value.center}, at rest at {rest.Value.center}");
    }

    /// <summary>A drag path's kind from its name ("Fast" or "P-FAST"; empty: Plain).</summary>
    public static QaPathKind ParseKind(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return QaPathKind.Plain;
        string n = name.Trim();
        if (n.StartsWith("P-", StringComparison.OrdinalIgnoreCase))
            n = n.Substring(2);
        return (QaPathKind)Enum.Parse(typeof(QaPathKind), n, true);
    }

    private IEnumerator Resolution(int w, int h)
    {
#if UNITY_EDITOR
        UnityEditor.PlayModeWindow.SetCustomRenderingResolution((uint)w, (uint)h, "QA");
#else
        Screen.SetResolution(w, h, FullScreenMode.Windowed);
#endif
        yield return QaGame.Until(() => Screen.width == w && Screen.height == h, 10f, $"the screen at {w}x{h}");
        yield return QaGame.Frames(15);
    }

    /// <summary>The still-time probes: no missing script, no broken material (QA-RIG's gate), no LiberationSans glyph on screen (TEXT-LANG's).</summary>
    private void Probe(string name)
    {
        List<string> missing = QaGame.MissingScripts();
        Assert($"missingScripts.{Safe(name)}", "QA-RIG", missing.Count == 0, missing.Count.ToString(), Join(missing));
        List<string> broken = QaGame.BrokenMaterials();
        Assert($"materials.{Safe(name)}", "QA-RIG", broken.Count == 0, broken.Count.ToString(), Join(broken));
        List<string> liberation = QaGame.LiberationSansGlyphs();
        Assert($"liberationSans.{Safe(name)}", "TEXT-LANG", liberation.Count == 0, liberation.Count.ToString(), Join(liberation));
    }

    private IEnumerator Gc(QaStep s)
    {
        int frames = Mathf.Max(10, Mathf.CeilToInt((s.f > 0f ? s.f : 3f) * SeqRecorder.Fps));
        // Reserved up front, and the camera log goes quiet: the window measures the game, not the rig.
        var samples = new List<long>(frames);
        _measuringGc = true;
        using (ProfilerRecorder gc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame"))
        {
            // The frame the window opened on (the recorder's start, the log's reservation) is not counted.
            yield return null;
            for (int i = 0; i < frames; i++)
            {
                yield return null;
                if (gc.Valid)
                    samples.Add(gc.LastValue);
            }
        }
        _measuringGc = false;
        string id = "gc." + Safe(string.IsNullOrEmpty(s.name) ? "steady" : s.name);
        if (samples.Count == 0)
        {
            Assert(id, Scenario.owner, false, "n/a", "GC Allocated In Frame is not readable here (a release player: capture on a development build).");
            yield break;
        }
        samples.Sort();
        long median = samples[samples.Count / 2], max = samples[samples.Count - 1];
        Assert(id, Scenario.owner, max <= s.n, $"median {median} B, max {max} B over {samples.Count} frames",
            $"budget {s.n} B/frame{(Application.isEditor ? "; editor numbers include the editor's own allocations (the gate is [BP])" : string.Empty)}");
    }

    private void AssertCamera(string id)
    {
        QaCameraReport r = Cameras.Report();
        Assert(id, "CAM", r.Pass, $"untagged {r.UntaggedFrames.Count}, over cap {r.OverCap.Count}", r.Summary());
    }

    /// <summary>Records an assertion "&lt;scenario&gt;.&lt;id&gt;" for <paramref name="owner"/> (tracks' scenarios add their own through ops; code may call it during a run).</summary>
    public void Assert(string id, string owner, bool pass, string measured, string detail)
    {
        _assertions.Add(new QaAssertion { id = $"{Scenario.name}.{id}", owner = string.IsNullOrEmpty(owner) ? Scenario.owner : owner, pass = pass, measured = measured, detail = detail });
    }

    private void Finish()
    {
        Recorder.Stop();
        Cameras.Flush();
        Assert("console", "QA-RIG", _errors == 0, $"errors {_errors}, warnings {_warnings}", "warnings.txt lists every warning and error logged during the run");
        AssertCamera("camera");
        AssertPresses();
        if (!string.IsNullOrEmpty(_expectedQuality))
        {
            string end = QualityName();
            Assert("quality", "QA-RIG", _startQuality == _expectedQuality && end == _expectedQuality, $"started at {_startQuality}, ended at {end}",
                $"the Standalone player's level is {_expectedQuality} (BUG-53: QA plays at the shipping level; QaPlayHarness sets it for the run)");
        }
        CleanUp();
        Report = BuildReport();
        bool complete = _steps.Count == Scenario.steps.Length || _steps.Count > 0 && string.Equals(_steps[_steps.Count - 1].op, "quit", StringComparison.OrdinalIgnoreCase);
        Report.ok = complete && _steps.All(x => x.ok) && _assertions.All(a => a.pass);
        File.WriteAllText(Path.Combine(OutDir, "run.json"), JsonUtility.ToJson(Report, true));
        File.WriteAllText(Path.Combine(OutDir, "done.txt"), Report.ok ? "ok" : "failed");
        Destroy(Recorder);
        Destroy(Cameras);
        Done = true;
        Finished?.Invoke(Report);
        if (_quitWhenDone && !Application.isEditor)
            Application.Quit(Report.ok ? 0 : 1);
    }

    /// <summary>Every press was delivered by the virtual mouse to the target under it, except those a step allowed with <see cref="QaStep.direct"/>.</summary>
    private void AssertPresses() =>
        Assert("presses", "QA-RIG", _blocked.Count == 0, $"{_presses} presses, {_blocked.Count} blocked, {_direct.Count} direct by step",
            _blocked.Count > 0 ? "Not under the pointer (an overlay or the target off screen): " + Join(_blocked)
                : _direct.Count > 0 ? "Allowed direct: " + Join(_direct) : "every press reached its target under the pointer");

    /// <summary>
    /// A run that ends without <see cref="Finish"/> (its deadline, the
    /// harness's Reset, play mode left by hand, the player closed): puts back
    /// everything the run changed (the real input devices, the frame rate,
    /// the seed, the hour) and writes a failed run.json with what it did so
    /// far, done.txt "aborted".
    /// </summary>
    private void OnDestroy()
    {
        if (Done || Scenario == null)
            return;
        try
        {
            if (Cameras != null)
                Cameras.Flush();
            _steps.Add(new QaStepResult { i = _steps.Count, op = "aborted", ok = false, note = "The run was stopped before its end (its deadline, a harness reset, play mode left or the player closed)." });
            Assert("console", "QA-RIG", _errors == 0, $"errors {_errors}, warnings {_warnings}", "warnings.txt lists every warning and error logged before the stop");
            AssertPresses();
            CleanUp();
            Report = BuildReport();
            Report.ok = false;
            File.WriteAllText(Path.Combine(OutDir, "run.json"), JsonUtility.ToJson(Report, true));
            File.WriteAllText(Path.Combine(OutDir, "done.txt"), "aborted");
        }
        catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is NullReferenceException)
        {
        }
        finally
        {
            CleanUp();
        }
    }

    /// <summary>Puts back what the run changed (once): stops the recorder (its frame rate), a drag's frame rate, the log hook and files, the seed, the hour, and the virtual input (the real devices come back).</summary>
    private void CleanUp()
    {
        if (_cleanedUp)
            return;
        _cleanedUp = true;
        if (Recorder != null)
            Recorder.Stop();
        if (_dragRestoreRate.HasValue)
        {
            Time.captureFramerate = _dragRestoreRate.Value;
            _dragRestoreRate = null;
        }
        _measuringGc = false;
        Application.logMessageReceived -= OnLog;
        _warningLog?.Dispose();
        _drags?.Dispose();
        if (_oldSeed.HasValue && RunManager.HasInstance && RunManager.Instance.Config != null)
            RunManager.Instance.Config.fixedRunSeed = _oldSeed.Value;
        if (_hourForced)
            DevToolsState.ForcedHour = null;
        Input?.Dispose();
    }

    private QaRunReport BuildReport()
    {
        return new QaRunReport
        {
            scenario = Scenario.name,
            owner = Scenario.owner,
            player = !Application.isEditor,
            unity = Application.unityVersion,
            quality = QualityName(),
            started = _started,
            finished = DateTime.Now.ToString("s"),
            errors = _errors,
            warnings = _warnings,
            // The log's lists outlive the component (OnDestroy may run after it is gone).
            camera = (object)Cameras != null ? Cameras.Report().Summary() : string.Empty,
            steps = _steps.ToArray(),
            assertions = _assertions.ToArray(),
        };
    }

    private void OnLog(string message, string stack, LogType type)
    {
        if (type == LogType.Log)
            return;
        if (type == LogType.Warning)
            _warnings++;
        else
            _errors++;
        try
        {
            _warningLog?.WriteLine($"[{type}] {message}");
            if (type == LogType.Exception)
                _warningLog?.WriteLine(stack);
            _warningLog?.Flush();
        }
        catch (IOException)
        {
        }
    }

    private static string Join(List<string> items) => items.Count == 0 ? "none" : string.Join("; ", items.Take(20)) + (items.Count > 20 ? $"; ... {items.Count - 20} more" : string.Empty);

    private static string F(float v) => v.ToString("0.#", CultureInfo.InvariantCulture);

    private static string QualityName() => QualitySettings.names[QualitySettings.GetQualityLevel()];

    /// <summary>A name made safe for a file or an id (letters, digits, '-' and '_').</summary>
    public static string Safe(string s) => new string((s ?? string.Empty).Select(ch => char.IsLetterOrDigit(ch) || ch == '-' || ch == '_' ? ch : '_').ToArray());
}
#endif
