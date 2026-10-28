// TEMPORARY automation for SL (never committed): the Night Slots machine in play. Title -> New Run (seed 12345) -> Home's slots:
// captures at 1920x1080 and 1280x720 (idle, the lever half pulled, spin frames, landed, a win, can't afford, Reduced Motion),
// a frame sequence of a full spin for the GIF, and the A/B audit of days 1-15: every spin's outcome and wallet against main's
// own draw (WeightedRandom over the library's outcomes on Seeds.ForSlot(Seeds.Day(seed, day)), one draw per paid spin).
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class _TimeDeskSLShots
{
    private const string ViewName = "TimeDeskSLShots";
    private const string Out = @"C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\d63602d5-a5a2-4440-a4b8-42c6496b21ab\scratchpad\run7\SL\shots";
    private const string StepKey = "SLShots.Step", FailsKey = "SLShots.Fails", HadSaveKey = "SLShots.HadSave";
    private const string ViewWidthKey = "SLShots.ViewWidth", ViewHeightKey = "SLShots.ViewHeight", ReducedKey = "SLShots.Reduced", IntensityKey = "SLShots.Intensity";
    private const int Playing = 1, Finishing = 2, Seed = 12345;

    private static readonly Stack<IEnumerator> Script = new Stack<IEnumerator>();
    private static string ReportPath => Path.Combine(Out, "report.txt");
    private static string SaveBackup => Path.Combine(Out, "save_backup");

    static _TimeDeskSLShots()
    {
        if (SessionState.GetInt(StepKey, 0) == 0)
            return;
        EditorApplication.update += Tick;
        Application.logMessageReceived += OnLog;
    }

    public static void Run()
    {
        Directory.CreateDirectory(Out);
        Directory.CreateDirectory(Path.Combine(Out, "gif"));
        foreach (string f in Directory.GetFiles(Out))
            File.Delete(f);
        foreach (string f in Directory.GetFiles(Path.Combine(Out, "gif")))
            File.Delete(f);
        File.WriteAllText(ReportPath, $"start {DateTime.Now:s}\n");
        int hadSave = 0;
        for (int i = 0; i < SaveSystem.Files.Count; i++)
        {
            if (!File.Exists(SaveSystem.Files[i]))
                continue;
            hadSave |= 1 << i;
            File.Copy(SaveSystem.Files[i], SaveBackup + i, true);
            File.Delete(SaveSystem.Files[i]);
        }
        SessionState.SetInt(HadSaveKey, hadSave);
        SessionState.SetInt(ReducedKey, MotionPreference.Reduced ? 1 : 0);
        SessionState.SetFloat(IntensityKey, MotionPreference.Intensity);
        MotionPreference.Reduced = false;
        MotionPreference.Intensity = 1f;
        PlayModeWindow.GetRenderingResolution(out uint width, out uint height);
        SessionState.SetInt(ViewWidthKey, (int)width);
        SessionState.SetInt(ViewHeightKey, (int)height);
        PlayModeWindow.SetCustomRenderingResolution(1920, 1080, ViewName);
        SessionState.SetInt(FailsKey, 0);
        SessionState.SetInt(StepKey, Playing);
        EditorSceneManager.OpenScene("Assets/Scenes/TitleScene.unity", OpenSceneMode.Single);
        EditorApplication.update += Tick;
        Application.logMessageReceived += OnLog;
        EditorApplication.EnterPlaymode();
    }

    private static void Tick()
    {
        int step = SessionState.GetInt(StepKey, 0);
        if (step == Finishing)
        {
            if (!EditorApplication.isPlaying)
                Finish();
            return;
        }
        if (step != Playing || !EditorApplication.isPlaying)
            return;
        try
        {
            if (Script.Count == 0)
                Script.Push(Play());
            while (Script.Count > 0)
            {
                IEnumerator top = Script.Peek();
                if (!top.MoveNext())
                {
                    Script.Pop();
                    continue;
                }
                if (top.Current is IEnumerator nested)
                {
                    Script.Push(nested);
                    continue;
                }
                return;
            }
            EndPlay();
        }
        catch (Exception e)
        {
            Check(false, $"exception: {e}");
            Script.Clear();
            EndPlay();
        }
    }

    private static void EndPlay()
    {
        Time.captureDeltaTime = 0f;
        SessionState.SetInt(StepKey, Finishing);
        EditorApplication.ExitPlaymode();
    }

    private static T One<T>() where T : Object => Object.FindFirstObjectByType<T>();

    // ------------------------------------------------------------------ the play
    private static IEnumerator Play()
    {
        yield return Until(() => One<TitleUIController>() != null && RunManager.HasInstance, 60f, "the title");
        RunManager.Instance.Config.fixedRunSeed = Seed;
        yield return Seconds(1f);
        TitleSceneController controller = One<TitleSceneController>();
        Invoke(controller, "Start");
        yield return Frames(2);
        var title = One<TitleUIController>();
        Click((Button)Field(title, "newRunButton"));
        yield return Frames(3);
        ((TMP_InputField)Field(title, "adoptNameInput")).text = "Mochi";
        Click((Button)Field(title, "adoptButton"));
        yield return Until(() => RunManager.Instance.World != null && RunManager.Instance.World.pet != null && RunManager.Instance.World.pet.name == "Mochi", 30f, "the adoption");
        yield return Seconds(2f);
        RunManager run = RunManager.Instance;
        Report($"   run seed {run.World.runSeed}, day {run.World.day}");
        DevCheats.Run(DevCheat.AddMoney, run, 400, 0f, null);

        // Home, through the evening to the slots (the real flow).
        SceneManager.LoadScene(run.Config.homeSceneName);
        yield return Until(() => One<HomeUIController>() != null && ((GameObject)Field(One<HomeUIController>(), "expensesPanel")).activeInHierarchy, 30f, "the bills");
        var ui = One<HomeUIController>();
        Click((Button)Field(ui, "expensesContinueButton"));
        yield return Seconds(0.5f);
        Click((Button)Field(ui, "petContinueButton"));
        yield return Seconds(0.5f);
        if (((GameObject)Field(ui, "shopPanel")).activeInHierarchy)
            Click((Button)Field(ui, "shopContinueButton"));
        yield return Until(() => ((GameObject)Field(ui, "slotPanel")).activeInHierarchy, 10f, "the slots");
        yield return Seconds(1.2f);
        var view = One<SlotMachineView>();
        Check(view != null, "the slot panel carries the machine (SlotMachineView)");
        var expected = new Expect(run, Seed);
        CheckWalletsAgree(ui, view, "idle");
        yield return Shots("40_slots_idle");
        Report($"   idle reels: {Payline(view)} lcd '{Lcd(view)}'");

        // The lever: half a drag holds it there (the ratchet), no spin.
        var handle = (SlotLeverHandle)Field(view, "leverHandle");
        float scale = handle.GetComponentInParent<Canvas>().rootCanvas.scaleFactor;
        Vector2 grip = RectTransformUtility.WorldToScreenPoint(null, handle.transform.position);
        var data = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, position = grip };
        handle.OnPointerDown(data);
        int moneyBefore = run.World.money;
        for (int k = 1; k <= 10; k++)
        {
            data.position = grip + new Vector2(0f, -k * 10f * scale);
            handle.OnDrag(data);
            yield return Frames(1);
        }
        yield return Seconds(0.2f);
        Check(run.World.money == moneyBefore && !view.Spinning, "half a pull spins nothing");
        yield return Shots("41_lever_half");

        // ... and the rest of the way: it fires one spin; the frames of the spin.
        float travel = UiMotion.Knobs.slots.leverTravel;
        for (int k = 11; k <= 24; k++)
        {
            data.position = grip + new Vector2(0f, -k * 10f * scale);
            handle.OnDrag(data);
            yield return Frames(1);
        }
        Check(view.Spinning, $"a full drag ({travel} px) fires a spin");
        expected.Spin(moneyBefore, out string want);
        handle.OnPointerUp(data);
        CheckWalletsAgree(ui, view, "as the spin starts (the cost shows in both)");
        int frame = 0;
        foreach (float at in new[] { 0.12f, 0.45f, 0.9f, 1.35f, 1.75f })
        {
            yield return SecondsGame(at, view);
            yield return Shot($"42_spin_{frame++}_{at:0.00}s_1080.png");
            CheckWalletsAgree(ui, view, $"mid-spin {at:0.00} s");
        }
        yield return Until(() => !view.Spinning, 10f, "the reels to settle");
        yield return Seconds(0.4f);
        CheckSpin(view, ui, want, "the dragged spin");
        yield return Shots("43_slots_landed");

        // A win: spin (the SPIN plate) until one lands (each a real draw, checked against main's).
        bool won = false;
        for (int n = 0; n < 25 && !won; n++)
        {
            if (run.World.money < 60)
                DevCheats.Run(DevCheat.AddMoney, run, 200, 0f, null);
            int before = run.World.money;
            Click((Button)Field(view, "spinButton"));
            yield return Until(() => view.Spinning, 5f, "the SPIN plate's pull");
            expected.Spin(before, out want);
            bool win = ((HomeUIController.SpinView)Field(view, "_result")).Win;
            if (win)
            {
                yield return Until(() => (bool)Field(view, "_paid"), 10f, "the payout");
                yield return Seconds(0.25f);
                yield return Shot("44_win_coins_falling_1080.png");
                yield return Seconds(0.6f);
                yield return Shots("44_slots_win");
                won = true;
            }
            yield return Until(() => !view.Spinning, 10f, "the reels to settle");
            CheckSpin(view, ui, want, $"spin {n + 2}");
        }
        Check(won, "a win landed within 25 spins");

        // The GIF: a full lever pull and spin at 25 frames a second of game time.
        yield return Resolution(1280, 720);
        DevCheats.Run(DevCheat.AddMoney, run, 100, 0f, null);
        Time.captureDeltaTime = 1f / 25f;
        yield return Frames(3);
        grip = RectTransformUtility.WorldToScreenPoint(null, handle.transform.position);
        scale = handle.GetComponentInParent<Canvas>().rootCanvas.scaleFactor;
        data = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, position = grip };
        int g = 0, gifBefore = run.World.money;
        for (int k = 0; k < 5; k++)
            yield return GifFrame(g++);
        handle.OnPointerDown(data);
        for (int k = 1; k <= 24; k++)
        {
            data.position = grip + new Vector2(0f, -k * 10f * scale);
            handle.OnDrag(data);
            if (view.Spinning && k < 30)
            {
                expected.Spin(gifBefore, out want);
                handle.OnPointerUp(data);
                break;
            }
            yield return GifFrame(g++);
        }
        for (int k = 0; k < 110 && (view.Spinning || k < 40); k++)
            yield return GifFrame(g++);
        for (int k = 0; k < 20; k++)
            yield return GifFrame(g++);
        Time.captureDeltaTime = 0f;
        CheckSpin(view, ui, want, "the GIF's spin");
        Report($"   gif frames {g}");
        yield return Resolution(1920, 1080);

        // Reduced Motion: the reels cut straight to the result.
        MotionPreference.Reduced = true;
        int reducedBefore = run.World.money;
        Click((Button)Field(view, "spinButton"));
        yield return Frames(2);
        expected.Spin(reducedBefore, out want);
        Check(!view.Spinning, "under Reduced Motion the reels cut to the result at once");
        CheckSpin(view, ui, want, "the Reduced Motion spin");
        yield return Shots("46_reduced_landed");
        MotionPreference.Reduced = false;

        // Can't afford: the chain, the blinking plate, the locked SPIN; a pull says no and draws nothing.
        run.World.money = 4;
        Invoke(One<HomeManager>(), "ShowSlot");
        yield return Seconds(0.6f);
        Check(!((Button)Field(view, "spinButton")).interactable && ((GameObject)Field(view, "leverLock")).activeSelf, "without the credits the SPIN plate is locked and the lever chained");
        Check(Lcd(view).StartsWith("Not enough"), $"the LCD says why ('{Lcd(view)}')");
        CheckWalletsAgree(ui, view, "broke");
        yield return Shots("45_slots_broke");
        handle.OnPointerDown(new PointerEventData(EventSystem.current) { position = grip });
        Click((Button)Field(view, "spinButton"));
        yield return Seconds(0.2f);
        yield return Shot("45_broke_refused_1080.png");
        Check(run.World.money == 4 && !view.Spinning, "a pull without the credits draws nothing");
        handle.OnPointerUp(new PointerEventData(EventSystem.current) { position = grip });
        yield return Seconds(0.6f);

        // The A/B audit: days 1-15, three paid spins a night (Reduced Motion to keep it short), against main's draw.
        MotionPreference.Reduced = true;
        for (int day = 1; day <= 15; day++)
        {
            run.World.day = day;
            run.World.money = Math.Max(run.World.money, 300);
            SceneManager.LoadScene(run.Config.homeSceneName);
            yield return Until(() => One<HomeUIController>() != null && ((GameObject)Field(One<HomeUIController>(), "expensesPanel")).activeInHierarchy, 30f, $"day {day}'s Home");
            yield return Frames(2);
            ui = One<HomeUIController>();
            ((GameObject)Field(ui, "expensesPanel")).SetActive(false);
            Invoke(One<HomeManager>(), "ShowSlot");
            yield return Frames(2);
            view = One<SlotMachineView>();
            var night = new Expect(run, Seed);
            var outcomes = new List<string>();
            for (int n = 0; n < 3; n++)
            {
                int before = run.World.money;
                Click((Button)Field(view, "spinButton"));
                yield return Frames(3);
                night.Spin(before, out want);
                string got = OutcomeId(view, run);
                outcomes.Add($"{got}:{before}->{run.World.money}");
                Check(got == want && run.World.money == night.Money, $"day {day} spin {n + 1}: {got} (main draws {want}), wallet {run.World.money} (main {night.Money})", quiet: true);
                CheckWalletsAgree(ui, view, $"day {day} spin {n + 1}", quiet: true);
            }
            Report($"   AB day {day:00}: {string.Join(", ", outcomes)}");
        }
        MotionPreference.Reduced = false;
    }

    // ------------------------------------------------------------------ main's draw, computed on its own (HomeManager.HandleSpin's rule)
    private sealed class Expect
    {
        private readonly SeededRandom _rng;
        private readonly IReadOnlyList<SlotOutcomeSO> _outcomes;
        private readonly int _cost;
        public int Money;

        public Expect(RunManager run, int seed)
        {
            _rng = new SeededRandom(Seeds.ForSlot(Seeds.Day(run.World.runSeed, run.World.day)));
            _outcomes = run.Library.SlotOutcomes;
            _cost = run.Config.gameConfig != null ? run.Config.gameConfig.slotSpinCost : 0;
        }

        /// <summary>The next spin as main draws it from <paramref name="start"/> credits (the wallet before the spin): refused below the cost, else one draw.</summary>
        public void Spin(int start, out string id)
        {
            if (start < _cost)
            {
                id = "(refused)";
                Money = start;
                return;
            }
            SlotOutcomeSO o = WeightedRandom.Pick(_outcomes, x => x != null ? x.weight : 0f, _rng);
            id = o != null ? o.id : "(none)";
            Money = start - _cost + (o != null ? o.moneyDelta : 0);
        }
    }

    private static string OutcomeId(SlotMachineView view, RunManager run)
    {
        var r = (HomeUIController.SpinView)Field(view, "_result");
        return r.Outcome >= 0 && r.Outcome < run.Library.SlotOutcomes.Count ? run.Library.SlotOutcomes[r.Outcome].id : "(none)";
    }

    private static void CheckSpin(SlotMachineView view, HomeUIController ui, string want, string what)
    {
        RunManager run = RunManager.Instance;
        string got = OutcomeId(view, run);
        var faces = (int[])Field(view, "_faces");
        var r = (HomeUIController.SpinView)Field(view, "_result");
        int[] should = SlotReels.Faces(r.Outcome, r.Win, run.Library.SlotOutcomes.Count);
        string shown = Payline(view);
        string meant = string.Join(",", should.Select(f => "slot_sym_" + run.Library.SlotOutcomes[f].id));
        Check(faces.SequenceEqual(should) && shown == meant, $"{what}: the reels show SlotReels' faces on the payline ({shown})");
        Check(want == null || got == want, $"{what}: outcome {got} is main's draw {want}");
        CheckWalletsAgree(ui, view, what);
        Report($"   {what}: {got}, line '{Lcd(view)}', wallet {run.World.money}");
    }

    private static string Payline(SlotMachineView view)
    {
        var faces = (Image[])Field(view, "cellFaces");
        int per = SlotMachineView.CellsPerReel;
        return string.Join(",", Enumerable.Range(0, faces.Length / per).Select(r => faces[r * per + per / 2].sprite != null ? faces[r * per + per / 2].sprite.name : "-"));
    }

    private static string Lcd(SlotMachineView view) => ((TMP_Text)Field(view, "lcdText")).text;

    private static void CheckWalletsAgree(HomeUIController ui, SlotMachineView view, string what, bool quiet = false)
    {
        string hud = ((TMP_Text)Field(ui, "moneyText")).text, machine = ((TMP_Text)Field(view, "creditsText")).text;
        Check(hud == machine, $"{what}: the HUD's wallet '{hud}' and the machine's credits '{machine}' agree", quiet);
    }

    // ------------------------------------------------------------------ helpers
    private static void Invoke(object target, string method)
    {
        MethodInfo m = target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (m != null)
            m.Invoke(target, null);
    }

    private static void Click(Button b)
    {
        if (b == null)
            return;
        var data = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
        ExecuteEvents.Execute(b.gameObject, data, ExecuteEvents.pointerClickHandler);
    }

    private static IEnumerator GifFrame(int n)
    {
        string path = Path.Combine(Out, "gif", $"f{n:000}.png");
        ScreenCapture.CaptureScreenshot(path);
        yield return Frames(1);
    }

    private static IEnumerator Shots(string name)
    {
        yield return Shot(name + "_1080.png");
        yield return Resolution(1280, 720);
        yield return Seconds(0.3f);
        yield return Shot(name + "_720.png");
        yield return Resolution(1920, 1080);
        yield return Seconds(0.3f);
    }

    private static IEnumerator Frames(int n)
    {
        int target = Time.frameCount + n;
        while (Time.frameCount < target)
            yield return null;
    }

    private static IEnumerator Seconds(float seconds)
    {
        float end = Time.realtimeSinceStartup + seconds;
        while (Time.realtimeSinceStartup < end)
            yield return null;
    }

    /// <summary>Waits until the spin's own clock reads <paramref name="at"/> seconds.</summary>
    private static IEnumerator SecondsGame(float at, SlotMachineView view)
    {
        float end = Time.realtimeSinceStartup + 10f;
        while ((float)Field(view, "_clock") < at && view.Spinning && Time.realtimeSinceStartup < end)
            yield return null;
    }

    private static IEnumerator Until(Func<bool> condition, float timeout, string what)
    {
        float end = Time.realtimeSinceStartup + timeout;
        while (!condition())
        {
            if (Time.realtimeSinceStartup > end)
            {
                yield return Shot("diag_timeout.png");
                throw new TimeoutException($"waited {timeout} s for {what}");
            }
            yield return null;
        }
    }

    private static IEnumerator Shot(string file)
    {
        string path = Path.Combine(Out, file);
        if (File.Exists(path))
            File.Delete(path);
        yield return Frames(2);
        ScreenCapture.CaptureScreenshot(path);
        float end = Time.realtimeSinceStartup + 10f;
        while (!File.Exists(path) && Time.realtimeSinceStartup < end)
            yield return null;
        Report($"screenshot {file} (screen {Screen.width}x{Screen.height})");
    }

    private static IEnumerator Resolution(int width, int height)
    {
        PlayModeWindow.SetCustomRenderingResolution((uint)width, (uint)height, ViewName);
        yield return Until(() => Screen.width == width && Screen.height == height, 10f, $"a {width}x{height} game view");
        yield return Frames(5);
    }

    private static void Finish()
    {
        Time.captureDeltaTime = 0f;
        PlayModeWindow.SetCustomRenderingResolution((uint)SessionState.GetInt(ViewWidthKey, 1920), (uint)SessionState.GetInt(ViewHeightKey, 1080), ViewName);
        int hadSave = SessionState.GetInt(HadSaveKey, 0);
        for (int i = 0; i < SaveSystem.Files.Count; i++)
        {
            bool had = (hadSave & (1 << i)) != 0;
            if (had && File.Exists(SaveBackup + i))
            {
                File.Copy(SaveBackup + i, SaveSystem.Files[i], true);
                File.Delete(SaveBackup + i);
            }
            else if (!had && File.Exists(SaveSystem.Files[i]))
                File.Delete(SaveSystem.Files[i]);
        }
        MotionPreference.Reduced = SessionState.GetInt(ReducedKey, 0) == 1;
        MotionPreference.Intensity = SessionState.GetFloat(IntensityKey, 1f);
        Resources.Load<RunConfigSO>(RunManager.ConfigResourcePath).fixedRunSeed = 0;
        DevToolsState.ResetAll();
        Report($"fails={SessionState.GetInt(FailsKey, 0)}");
        SessionState.SetInt(StepKey, 0);
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= OnLog;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Report($"done {DateTime.Now:s}");
    }

    private static void OnLog(string message, string stack, LogType type)
    {
        if (type == LogType.Log)
            return;
        if (stack != null && stack.Contains("UnityEditor.Search"))
            return;
        try { File.AppendAllText(Path.Combine(Out, "warnings.txt"), $"[{type}] {message}\n{(type == LogType.Exception || type == LogType.Error ? stack : "")}\n"); } catch (IOException) { }
    }

    private static object Field(object target, string name)
    {
        for (Type t = target.GetType(); t != null; t = t.BaseType)
        {
            FieldInfo f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (f != null)
                return f.GetValue(target);
        }
        throw new MissingFieldException(target.GetType().Name, name);
    }

    private static void Report(string line) => File.AppendAllText(ReportPath, line + "\n");

    private static void Check(bool ok, string what, bool quiet = false)
    {
        if (!ok || !quiet)
            Report((ok ? "PASS " : "FAIL ") + what);
        if (!ok)
            SessionState.SetInt(FailsKey, SessionState.GetInt(FailsKey, 0) + 1);
    }
}
