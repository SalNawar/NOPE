#if UNITY_EDITOR || TIMEDESK_QA
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

/// <summary>
/// The cheat menu's QA rig ops (cheats-fix, the night of 2026-11-07; Saleh:
/// "fix cheats being broken"; tools/qa/scenarios/cheats_all.json): every line
/// of docs/CHEATS.md is a claim, and each op asserts one cheat's effect as
/// "&lt;scenario&gt;.&lt;check&gt;.&lt;label&gt;" for CHEATS. The cheats run
/// through the runner's own <c>cheat</c> op (DevCheats.Run, as the menu's
/// buttons queue them); these ops record the run before and check it after.
/// <list type="bullet">
/// <item><c>cheatSnap</c> (name, default "last"): records the run as it
/// stands (day, money, stability, today's decisions, the ending, the
/// switchboard's waiting calls) and starts counting the camera's impulse
/// tags and FeelDirector's hit-stops from here.</item>
/// <item><c>cheatCheck</c> (name: the check; target: the label of its
/// assertion; n, f, text: what is expected; w: seconds to wait for it, polled
/// each frame): day (n the day, the briefing up, the run cheated), undo (the
/// money of the record named in text, no decision today), report (the shift
/// report up), decided (n more decisions than the last record, each correct),
/// money (n more than the last record), stability (f), ending (text the
/// ending's id, "" none), unlock (n 1: the scanner introduced today and the
/// switch on; 0: neither), items (every Orders and House item owned),
/// tutorial (n 1: the FTUE open and a step shown; 0: closed), hour (f the
/// forced hour, -1 the clock's; text the clock's text: the CRT and the
/// readouts, the hall rig's hour, the city's tint in the hall windows (the
/// window layer's state weights against HallBakedCycle at that hour) and a
/// still whose hall luma is kept; n 1: darker than the still labelled
/// target2), feel (n 1: an impulse tag or a hit-stop since the record),
/// intensity (f; -1: the cheat's next step from the record), hallslots (n 1
/// an override set, 2 the stand-ins on, 0 none), lens (n levels owned), language (text lock, unlock or
/// ramp), dust (n 1 hidden), calls (n more waiting calls on the switchboard
/// than the record), mode (text plain, pc, switchboard, city or inspect; n
/// 1 Settings shown, 0 not, -1 either), first (the forced appearance stands
/// first: cheatForce's), lawbreaker (today's cases hold someone else's photo
/// and the switch is used up).</item>
/// <item><c>cheatForce</c> (text: "famous" or "D&lt;day&gt;"): picks the menu's
/// first famous premade, or that day's first story beat, notes what must
/// stand first and where, and runs "First traveller of the shift" on it.</item>
/// <item><c>cheatFaults</c> (target: label; n: the most travellers to try):
/// "Reveal faults" on the traveller at the desk until one is denied with at
/// least one box named and marked in red on the desk's papers (a still),
/// deciding each other one correctly.</item>
/// </list>
/// </summary>
public static class CheatOps
{
    private const string Owner = "CHEATS";
    private static readonly int StateWeightsId = Shader.PropertyToID("_StateWeights");

    /// <summary>A record of the run (cheatSnap).</summary>
    private sealed class Record
    {
        public int Day, Money, Decisions, Waiting;
        public float Stability, Unscaled, Intensity;
        public string Ending;
    }

    private static readonly Dictionary<string, Record> Records = new Dictionary<string, Record>();
    private static readonly Dictionary<string, float> Lumas = new Dictionary<string, float>();
    private static int _impulses;
    private static string _forcedKey, _forcedLabel;
    private static int _forcedDay;

#if UNITY_EDITOR
    /// <summary>Also known in edit mode, so the scenario tests read cheats_all with known ops.</summary>
    [UnityEditor.InitializeOnLoadMethod]
    private static void RegisterInEditor() => Register();
#endif

    /// <summary>Registers the ops once (each a literal <c>QaRunner.RegisterOp("name"</c> call: tools/qa/test_seqlib.py reads them).</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        if (!QaRunner.IsKnownOp("cheatSnap"))
            QaRunner.RegisterOp("cheatSnap", Snap);
        if (!QaRunner.IsKnownOp("cheatCheck"))
            QaRunner.RegisterOp("cheatCheck", Check);
        if (!QaRunner.IsKnownOp("cheatForce"))
            QaRunner.RegisterOp("cheatForce", Force);
        if (!QaRunner.IsKnownOp("cheatFaults"))
            QaRunner.RegisterOp("cheatFaults", Faults);
        Records.Clear();
        Lumas.Clear();
        QaSignals.CameraTagged -= CountImpulse;
        QaSignals.CameraTagged += CountImpulse;
    }

    private static void CountImpulse(CameraMoveCause cause, float seconds, float capPx)
    {
        if (cause == CameraMoveCause.Impulse)
            _impulses++;
    }

    private static WorldState World => RunManager.HasInstance ? RunManager.Instance.World : null;

    private static IEnumerator Snap(QaRunner runner, QaStep step)
    {
        yield return QaGame.Frames(2);
        Records[string.IsNullOrEmpty(step.name) ? "last" : step.name] = Now();
        _impulses = 0;
    }

    private static Record Now()
    {
        WorldState w = World;
        GameManager game = GameManager.Current;
        return new Record
        {
            Day = w != null ? w.day : 0,
            Money = w != null ? w.money : 0,
            Stability = w != null ? w.timelineStability : 0f,
            Ending = w != null ? w.endingId ?? string.Empty : string.Empty,
            Decisions = game != null && game.Ledger != null ? game.Ledger.verdicts.Count : 0,
            Waiting = Waiting(),
            Unscaled = Time.unscaledTime,
            Intensity = MotionPreference.Intensity,
        };
    }

    private static Record Last(string name = null) => Records.TryGetValue(string.IsNullOrEmpty(name) ? "last" : name, out Record r) ? r : null;

    /// <summary>The switchboard's waiting calls (Switchboard.WaitingTotal), or -1 without a board.</summary>
    private static int Waiting()
    {
        SwitchboardView view = SwitchboardView.Current;
        if (view == null)
            return -1;
        object board = view.GetType().GetField("_board", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(view);
        PropertyInfo total = board?.GetType().GetProperty("WaitingTotal");
        return total != null ? (int)total.GetValue(board) : -1;
    }

    /// <summary>Polls <paramref name="test"/> each frame for up to <paramref name="seconds"/> of real time (at least once).</summary>
    private static IEnumerator Poll(Func<bool> test, float seconds)
    {
        float end = Time.realtimeSinceStartup + Mathf.Max(0f, seconds);
        while (!test() && Time.realtimeSinceStartup < end)
            yield return null;
    }

    private static IEnumerator Check(QaRunner runner, QaStep step)
    {
        string kind = (step.name ?? string.Empty).Trim().ToLowerInvariant();
        string id = kind + (string.IsNullOrEmpty(step.target) ? string.Empty : "." + step.target);
        float wait = step.w > 0 ? step.w : 3f;
        bool pass = false;
        string measured = string.Empty, detail = string.Empty;
        Func<bool> test;
        switch (kind)
        {
            case "day":
                test = () =>
                {
                    WorldState w = World;
                    bool briefing = QaGame.BriefingShown();
                    measured = $"day {QaGame.Day()}, briefing {briefing}, cheated {w != null && w.cheated}";
                    return QaGame.Day() == step.n && briefing && w != null && w.cheated;
                };
                detail = $"CHEATS.md: the cheat lands on day {step.n}'s briefing (the start of its shift) and marks the run cheated";
                break;
            case "undo":
                test = () =>
                {
                    Record morning = Last(step.text);
                    WorldState w = World;
                    GameManager game = GameManager.Current;
                    int decisions = game != null && game.Ledger != null ? game.Ledger.verdicts.Count : -1;
                    measured = $"money {(w != null ? w.money : 0)} (the morning's {(morning != null ? morning.Money : -1)}), day {QaGame.Day()}, decisions today {decisions}, briefing {QaGame.BriefingShown()}";
                    return morning != null && w != null && w.money == morning.Money && w.day == morning.Day && decisions == 0 && QaGame.BriefingShown();
                };
                detail = "CHEATS.md Restart shift: today again from its morning snapshot; today's decisions, pay and cheat money are undone";
                break;
            case "report":
                test = () =>
                {
                    bool shown = QaGame.IsShown(QaGame.Member("DayFlowUIController.resultsPanel"));
                    measured = $"shift report shown {shown}";
                    return shown;
                };
                detail = "CHEATS.md End shift: closing time now; the traveller at the desk is finished; then the shift report";
                break;
            case "decided":
                test = () =>
                {
                    Record then = Last();
                    GameManager game = GameManager.Current;
                    List<CaseVerdict> verdicts = game != null && game.Ledger != null ? game.Ledger.verdicts : new List<CaseVerdict>();
                    int added = then != null ? verdicts.Count - then.Decisions : 0;
                    bool correct = added > 0 && verdicts.Skip(then.Decisions).All(v => v.correct);
                    measured = $"{added} decided since the record, all correct {correct}";
                    return added >= Math.Max(1, step.n) && correct;
                };
                detail = $"CHEATS.md Decide correctly or Auto-decide: at least {Math.Max(1, step.n)} traveller(s) decided, each correctly";
                break;
            case "money":
                test = () =>
                {
                    Record then = Last();
                    int now = World != null ? World.money : 0;
                    measured = $"money {(then != null ? then.Money : -1)} -> {now}";
                    return then != null && now - then.Money == step.n;
                };
                detail = $"CHEATS.md Money: the wallet gains {step.n}";
                break;
            case "stability":
                test = () =>
                {
                    float now = World != null ? World.timelineStability : -1f;
                    measured = $"stability {now.ToString("0.00", CultureInfo.InvariantCulture)}";
                    return Mathf.Abs(now - step.f) < 0.011f;
                };
                detail = $"CHEATS.md Stab.: the timeline stability is set to {step.f}";
                break;
            case "ending":
                test = () =>
                {
                    string ending = World != null ? World.endingId ?? string.Empty : "?";
                    measured = $"ending '{ending}', day {QaGame.Day()}";
                    return ending == (step.text ?? string.Empty);
                };
                detail = string.IsNullOrEmpty(step.text) ? "CHEATS.md Restart shift: an ending reached today is lifted" : $"CHEATS.md Stab. 0: the clerk is fired at the next decision (ending '{step.text}')";
                break;
            case "unlock":
                test = () =>
                {
                    ContentLibrarySO lib = RunManager.HasInstance ? RunManager.Instance.Library : null;
                    bool has = lib != null && lib.Introductions.Has(QaGame.Day(), Feature.Scanner);
                    measured = $"switch {DevToolsState.UnlockEverything}, the scanner introduced on day {QaGame.Day()}: {has}";
                    return has == (step.n != 0) && DevToolsState.UnlockEverything == (step.n != 0);
                };
                detail = step.n != 0 ? "CHEATS.md Unlock everything: every feature the ramp introduces counts as introduced today (the scanner on day 1)" : "the ramp alone: the scanner is not introduced on day 1";
                break;
            case "items":
                test = () =>
                {
                    ContentLibrarySO lib = RunManager.HasInstance ? RunManager.Instance.Library : null;
                    List<string> missing = lib == null || World == null ? new List<string> { "no run" }
                        : lib.Upgrades.Where(u => u != null && !World.HasUpgrade(u.id)).Select(u => u.id).ToList();
                    measured = missing.Count == 0 ? $"all {lib.Upgrades.Count} owned" : $"{missing.Count} not owned: {string.Join(", ", missing.Take(8))}";
                    return missing.Count == 0;
                };
                detail = "CHEATS.md Give all items: every Orders and House item is owned";
                break;
            case "tutorial":
                test = () =>
                {
                    GuideDirector guide = QaGame.Find<GuideDirector>();
                    bool open = World != null && Guide.FtueOpen(World.guide);
                    string shown = guide != null ? guide.StepShown : "?";
                    measured = $"FTUE open {open}, step shown '{shown}'";
                    return step.n != 0 ? open && !string.IsNullOrEmpty(shown) : !open && string.IsNullOrEmpty(shown);
                };
                detail = step.n != 0 ? "CHEATS.md Replay tutorial: the desk tutorial starts from step 1" : "CHEATS.md Skip tutorial: the desk tutorial ends";
                break;
            case "feel":
                test = () =>
                {
                    Record then = Last();
                    float stopUntil = HitStopUntil();
                    bool stopped = then != null && stopUntil > then.Unscaled;
                    measured = $"impulse tags {_impulses}, hit-stop since the record {stopped}, motion still {UiMotion.Amount.Still}";
                    return _impulses > 0 || stopped;
                };
                detail = "CHEATS.md Feel: stamp, citation, famous, breach fire that moment's hit-stop or camera impulse (FeelDirector.Punch)";
                break;
            case "intensity":
                test = () =>
                {
                    Record then = Last();
                    // f below 0: the step after the record's intensity (FeelCheats: above 75 % to 50, above 25 % to 0, else to 100).
                    float want = step.f >= 0f ? step.f : then == null ? -1f : then.Intensity > 0.75f ? 0.5f : then.Intensity > 0.25f ? 0f : 1f;
                    measured = $"Motion intensity {(then != null ? then.Intensity : -1f).ToString("0.00", CultureInfo.InvariantCulture)} -> {MotionPreference.Intensity.ToString("0.00", CultureInfo.InvariantCulture)} (want {want.ToString("0.00", CultureInfo.InvariantCulture)})";
                    return want >= 0f && Mathf.Abs(MotionPreference.Intensity - want) < 0.01f;
                };
                detail = "CHEATS.md Feel: intensity steps the Motion intensity through 100, 50 and 0 %";
                break;
            case "lens":
                test = () =>
                {
                    ContentLibrarySO lib = RunManager.HasInstance ? RunManager.Instance.Library : null;
                    List<string> ids = lib != null ? lib.Translation.lens.rules.levelIds : new List<string>();
                    int owned = World != null ? ids.Count(World.HasUpgrade) : -1;
                    measured = $"{owned} of {ids.Count} lens level(s) owned";
                    return owned == step.n;
                };
                detail = $"CHEATS.md Lens: the lens owned up to that level ({step.n} level(s)); remove takes them away";
                break;
            case "language":
                test = () =>
                {
                    bool? lockNow = CultureThemeService.LockOverride;
                    measured = $"LockOverride {(lockNow.HasValue ? lockNow.Value.ToString() : "null")}, locked now {CultureThemeService.LanguageLocked}";
                    switch (step.text)
                    {
                        case "lock": return lockNow == true && CultureThemeService.LanguageLocked;
                        case "unlock": return lockNow == false && !CultureThemeService.LanguageLocked;
                        default: return !lockNow.HasValue;
                    }
                };
                detail = "CHEATS.md Language: lock now, unlock, follow the ramp";
                break;
            case "dust":
                test = () =>
                {
                    int shown = 0, all = 0;
                    foreach (HallLightingRig rig in UnityEngine.Object.FindObjectsByType<HallLightingRig>())
                        foreach (ParticleSystemRenderer r in rig.GetComponentsInChildren<ParticleSystemRenderer>(true))
                        {
                            all++;
                            if (r.enabled)
                                shown++;
                        }
                    measured = $"{shown} of {all} dust emitter(s) drawn";
                    return all > 0 && (step.n != 0 ? shown == 0 : shown == all);
                };
                detail = "Hall FX: dust hides or shows the hall's dust motes";
                break;
            case "hallslots":
                test = () =>
                {
                    bool any = HallSlotsDev.Culture != null || HallSlotsDev.Tier.HasValue || HallSlotsDev.Phase.HasValue || HallSlotsDev.Event.HasValue || HallSlotsDev.Exhibit != null;
                    measured = $"overrides: culture {HallSlotsDev.Culture ?? "run"}, tier {(HallSlotsDev.Tier.HasValue ? HallSlotsDev.Tier.Value.ToString() : "run")}, phase {(HallSlotsDev.Phase.HasValue ? HallSlotsDev.Phase.Value.ToString() : "run")}, event {(HallSlotsDev.Event.HasValue ? HallSlotsDev.Event.Value.ToString() : "run")}, exhibit {HallSlotsDev.Exhibit ?? "run"}, stand-ins {HallSlotsDev.ShowAllStandIns}";
                    return step.n == 2 ? HallSlotsDev.ShowAllStandIns : step.n == 1 ? any : !any && !HallSlotsDev.ShowAllStandIns;
                };
                detail = "Hall: a variable steps to its next value (n 1); Hall: stand-ins on (n 2); Hall: the run's clears them (n 0)";
                break;
            case "calls":
                test = () =>
                {
                    Record then = Last();
                    int now = Waiting();
                    measured = $"waiting calls {(then != null ? then.Waiting : -1)} -> {now}";
                    return then != null && now >= 0 && then.Waiting >= 0 && now - then.Waiting == step.n;
                };
                detail = $"CHEATS.md Switchboard: 3 test calls rings {step.n} calls on today's board (their lamps)";
                break;
            case "mode":
                test = () =>
                {
                    OfficeControls controls = UnityEngine.Object.FindObjectsByType<OfficeControls>().FirstOrDefault(c => c.isActiveAndEnabled);
                    if (controls == null)
                    {
                        measured = "no office controls";
                        return false;
                    }
                    ControlState s = controls.State();
                    bool settings = PauseMenu.SettingsShowing;
                    measured = $"back-out {ControlRules.BackOut(s)}, pc {s.PcOpen}, switchboard {s.Switchboard}, city {s.CityView}, inspect {s.Inspecting}, wheel {s.WheelOpen}, reading view {s.DeskView}, Settings {settings}";
                    bool mode;
                    switch (step.text)
                    {
                        case "pc": mode = s.PcOpen; break;
                        case "switchboard": mode = s.Switchboard && !s.PcOpen; break;
                        case "city": mode = s.CityView && !s.PcOpen; break;
                        case "inspect": mode = s.Inspecting && !s.PcOpen; break;
                        default: mode = !s.PcOpen && !s.Switchboard && !s.CityView && !s.Inspecting && !s.WheelOpen && !s.ValueHeld; break;
                    }
                    return mode && (step.n < 0 || settings == (step.n == 1));
                };
                detail = $"D13, UI-23: the view is {step.text}{(step.n < 0 ? string.Empty : step.n == 1 ? " with Settings shown" : " and Settings is not shown")} (the first Esc backs out of a mode; Esc opens Settings from the plain desk; on the PC Esc closes the front window)";
                break;
            case "first":
                test = () =>
                {
                    CaseInstance inst = GameManager.Current != null ? GameManager.Current.ActiveCase : null;
                    bool at = QaGame.TravellerAtDesk();
                    string who = inst == null ? "nobody" : $"{inst.visitorDisplayName} (premade '{(inst.legendarySource != null ? inst.legendarySource.id : string.Empty)}', appearance '{(inst.forcedAppearance != null ? inst.forcedAppearance.id : string.Empty)}')";
                    measured = $"day {QaGame.Day()}, at the desk {at}: {who}; forced {_forcedKey} ({_forcedLabel}) on day {_forcedDay}";
                    return at && QaGame.Day() == _forcedDay && Stands(inst);
                };
                detail = "CHEATS.md First traveller of the shift: that premade or story beat stands first, on its own day";
                break;
            case "lawbreaker":
                test = () =>
                {
                    GameManager game = GameManager.Current;
                    var cases = game != null ? QaGame.Field(game, "_dayCases") as IList<CaseInstance> : null;
                    int liars = cases != null ? cases.Count(c => c != null && c.lie == LieKind.SwappedPhoto) : 0;
                    measured = $"{liars} traveller(s) today with someone else's photo, the switch {(DevToolsState.ForcedLie.HasValue ? DevToolsState.ForcedLie.Value.ToString() : "used up")}";
                    return liars >= 1 && !DevToolsState.ForcedLie.HasValue;
                };
                detail = "CHEATS.md Law-breaker next: the next generated traveller carries someone else's photo, and today's shift starts over";
                break;
            case "hour":
                yield return Hour(runner, step, id);
                yield break;
            default:
                throw new ArgumentException($"cheatCheck: unknown check '{step.name}'.");
        }
        yield return Poll(() => pass = test(), wait);
        runner.Assert(id, Owner, pass, measured, detail);
    }

    /// <summary>FeelDirector's hit-stop end (unscaled seconds), or -1 without the office's director.</summary>
    private static float HitStopUntil()
    {
        FieldInfo current = typeof(FeelDirector).GetField("_current", BindingFlags.NonPublic | BindingFlags.Static);
        object director = current?.GetValue(null);
        FieldInfo until = typeof(FeelDirector).GetField("_stopUntil", BindingFlags.NonPublic | BindingFlags.Instance);
        return director != null && until != null ? (float)until.GetValue(director) : -1f;
    }

    /// <summary>True when <paramref name="inst"/> is the appearance cheatForce forced.</summary>
    private static bool Stands(CaseInstance inst)
    {
        if (inst == null || string.IsNullOrEmpty(_forcedKey))
            return false;
        ContentLibrarySO lib = RunManager.HasInstance ? RunManager.Instance.Library : null;
        ForcedCaseSlot slot = DevCheats.Appearance(lib, _forcedKey);
        if (slot == null)
            return false;
        if (_forcedKey.StartsWith("premade:", StringComparison.Ordinal))
            return inst.legendarySource != null && slot.legendary != null && inst.legendarySource.id == slot.legendary.id;
        return inst.forcedAppearance != null && (inst.forcedAppearance.id == slot.id || (slot.legendary != null && inst.forcedAppearance.legendary == slot.legendary));
    }

    /// <summary>
    /// The hour check (GATE-22): the clock's readouts show the forced hour
    /// (or the clock's own), the hall rig's hour is it, the city in the hall's
    /// windows takes the state weights of that hour, and a still's hall luma
    /// is kept (and, with n 1, is darker than target2's).
    /// </summary>
    private static IEnumerator Hour(QaRunner runner, QaStep step, string id)
    {
        yield return QaGame.Frames(10);
        string label = string.IsNullOrEmpty(step.target) ? "hour" : step.target;
        bool forced = step.f >= 0f;
        var driver = ShiftClockDriver.Live as ShiftClockDriver;
        string shownByDriver = driver != null && driver.Clock != null ? ShiftClock.Format(driver.ShownMinute) : "no clock";
        string trueClock = driver != null && driver.Clock != null ? ShiftClock.Format(driver.MinuteOfDay) : "no clock";
        List<string> readouts = Readouts();
        string expected = forced ? step.text : trueClock;
        bool clockOk = shownByDriver == expected && readouts.Count > 0 && readouts.All(r => r == expected);
        runner.Assert(id + ".clock", Owner, clockOk, $"shown {shownByDriver} (the shift clock {trueClock}); readouts [{string.Join(", ", readouts)}]",
            $"GATE-22: the clock shows {(forced ? "the forced hour" : "its own hour")} ({expected}) on every readout (the desk's CRT, the wall clock, the PC's tray)");

        HallLightingRig rig = UnityEngine.Object.FindObjectsByType<HallLightingRig>().FirstOrDefault(r => r.isActiveAndEnabled);
        float rigHour = rig != null ? rig.Hour : -1f;
        float clockHour = driver != null ? driver.MinuteOfDay / 60f : -1f;
        bool hallOk = rig != null && (forced ? Mathf.Abs(rigHour - step.f) < 0.01f : Mathf.Abs(rigHour - clockHour) < 0.1f);
        runner.Assert(id + ".hall", Owner, hallOk, $"hall rig hour {rigHour.ToString("0.00", CultureInfo.InvariantCulture)}, evening {(rig != null ? rig.Evening : -1f).ToString("0.00", CultureInfo.InvariantCulture)}",
            "GATE-22: the hall's light shows the same hour (HallLightingRig.Hour)");

        HallCityExterior exterior = QaGame.Find<HallCityExterior>();
        Vector4 weights = Vector4.zero;
        if (exterior != null && exterior.window != null)
        {
            var block = new MaterialPropertyBlock();
            exterior.window.GetPropertyBlock(block);
            weights = block.GetVector(StateWeightsId);
        }
        float sum = weights.x + weights.y + weights.z + weights.w;
        HallBakedCycle.Weights4 want = rig != null && rig.Settings != null ? HallBakedCycle.Weights(rigHour, rig.Settings.Cycle) : default;
        float night = sum > 1e-4f ? weights.w / sum : -1f;
        bool cityOk = exterior != null && sum > 1e-4f && Mathf.Abs(night - want.w) < 0.05f && (!forced || step.f < 20f || night > 0.5f);
        runner.Assert(id + ".city", Owner, cityOk,
            $"window state weights ({weights.x:0.00}, {weights.y:0.00}, {weights.z:0.00}, {weights.w:0.00}): night share {night:0.00}, the hour's {want.w:0.00}",
            "GATE-22: the city in the hall's windows is tinted by the same hour (the window layer's _StateWeights, the hall's state weights at the rig's hour)");

        string path = Path.Combine(runner.OutDir, "stills", $"cheat_{label}_{Screen.width}x{Screen.height}.png");
        runner.Recorder.Still(path);
        yield return QaGame.Until(() => !runner.Recorder.StillPending, 10f, "the still");
        float luma = HallLuma(path);
        Lumas[label] = luma;
        if (step.n == 1)
        {
            bool known = Lumas.TryGetValue(step.target2 ?? string.Empty, out float then);
            runner.Assert(id + ".luma", Owner, known && luma >= 0f && luma < then,
                $"hall luma {luma:0.0} at {label}, {(known ? then.ToString("0.0", CultureInfo.InvariantCulture) : "none")} at {step.target2}",
                "GATE-22: the hall seen through the booth window is darker at the forced night than at the forced morning (mean Rec. 709 luma of the hall either side of the traveller)");
        }
    }

    /// <summary>The texts of the clock's readouts that are shown (the desk's CRT; ShiftClockReadouts' tray, CRT plate and office clock).</summary>
    private static List<string> Readouts()
    {
        var texts = new List<string>();
        void Add(object o)
        {
            if (o is TMP_Text t && t != null && t.isActiveAndEnabled && !string.IsNullOrEmpty(t.text))
                texts.Add(t.text.Trim());
        }
        foreach (PaintedCrtReadout crt in UnityEngine.Object.FindObjectsByType<PaintedCrtReadout>())
            if (crt.isActiveAndEnabled)
                Add(QaGame.Field(crt, "_time"));
        foreach (ShiftClockReadouts r in UnityEngine.Object.FindObjectsByType<ShiftClockReadouts>())
            if (r.isActiveAndEnabled)
            {
                Add(QaGame.Field(r, "trayClockText"));
                Add(QaGame.Field(r, "crtClockText"));
                Add(QaGame.Field(r, "_officeClockText"));
            }
        return texts;
    }

    /// <summary>The mean Rec. 709 luma (0-255) of the hall in a still: the booth window's two halves either side of the traveller; -1 when it cannot be read.</summary>
    private static float HallLuma(string path)
    {
        if (!File.Exists(path))
            return -1f;
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            if (!tex.LoadImage(File.ReadAllBytes(path)))
                return -1f;
            double sum = 0;
            long count = 0;
            // Fractions from the top (the window between the booth's frame and the counter), as the still shows the hall at every aspect.
            foreach ((float x0, float x1) in new[] { (0.23f, 0.43f), (0.57f, 0.80f) })
                for (float fy = 0.15f; fy <= 0.50f; fy += 0.01f)
                    for (float fx = x0; fx <= x1; fx += 0.005f)
                    {
                        Color c = tex.GetPixel(Mathf.RoundToInt(fx * (tex.width - 1)), Mathf.RoundToInt((1f - fy) * (tex.height - 1)));
                        sum += 255.0 * (0.2126 * c.r + 0.7152 * c.g + 0.0722 * c.b);
                        count++;
                    }
            return count > 0 ? (float)(sum / count) : -1f;
        }
        finally
        {
            UnityEngine.Object.Destroy(tex);
        }
    }

    /// <summary>How many boxes of the papers on the desk carry a mark (DeskDocument.Marked: Reveal faults' red, a logged difference's).</summary>
    private static int MarkedBoxes()
    {
        int marked = 0;
        foreach (DeskController desk in UnityEngine.Object.FindObjectsByType<DeskController>())
        {
            if (!desk.isActiveAndEnabled || !(QaGame.Field(desk, "_papers") is List<DeskDocument> papers))
                continue;
            foreach (DeskDocument paper in papers)
                if (paper != null)
                    for (int slot = 0; slot < paper.SlotCount; slot++)
                        if (paper.Marked(slot))
                            marked++;
        }
        return marked;
    }

    private static IEnumerator Force(QaRunner runner, QaStep step)
    {
        RunManager run = RunManager.Instance;
        ContentLibrarySO lib = run.Library;
        List<(string key, string label)> choices = DevCheats.AppearanceChoices(lib);
        string want = (step.text ?? string.Empty).Trim();
        (string key, string label) pick = default;
        if (want.Equals("famous", StringComparison.OrdinalIgnoreCase))
            pick = choices.FirstOrDefault(c => c.key.StartsWith("premade:", StringComparison.Ordinal)
                                               && lib.Legendaries.Any(l => l != null && "premade:" + l.id == c.key && Premades.IsFamous(l.kind)));
        else
            pick = choices.FirstOrDefault(c => c.key.StartsWith("beat:", StringComparison.Ordinal) && c.label.StartsWith(want + " ", StringComparison.Ordinal));
        if (pick.key == null)
            throw new InvalidOperationException($"cheatForce: no appearance '{want}' among the menu's {choices.Count} ({string.Join(", ", choices.Where(c => c.key.StartsWith("beat:", StringComparison.Ordinal)).Select(c => c.label).Take(12))}).");
        _forcedKey = pick.key;
        _forcedLabel = pick.label;
        _forcedDay = DevCheats.ForceDay(lib, pick.key, run.World.day);
        DevCheats.Run(DevCheat.ForceAppearance, run, 0, 0f, pick.key);
        runner.Assert("force." + (string.IsNullOrEmpty(step.target) ? want : step.target), Owner, !DevCheats.LastResult.StartsWith("No appearance", StringComparison.Ordinal),
            $"{pick.label} ({pick.key}) on day {_forcedDay}: {DevCheats.LastResult}", "CHEATS.md First traveller of the shift: the menu lists it and the cheat takes it");
        yield return QaGame.Frames(5);
    }

    private static IEnumerator Faults(QaRunner runner, QaStep step)
    {
        string label = string.IsNullOrEmpty(step.target) ? "faults" : step.target;
        var seen = new List<string>();
        bool pass = false;
        int tries = step.n > 0 ? step.n : 6;
        var marked = new Regex(@"\[(\d+) box\(es\) marked\]");
        for (int i = 0; i < tries && !pass; i++)
        {
            yield return Poll(QaGame.TravellerAtDesk, 60f);
            if (!QaGame.TravellerAtDesk())
            {
                seen.Add("nobody came to the desk");
                break;
            }
            // The papers are handed over as the traveller steps up: the marks show on the papers on the desk.
            float handed = Time.realtimeSinceStartup + 2.5f;
            while (Time.realtimeSinceStartup < handed)
                yield return null;
            DevCheats.Run(DevCheat.RevealFaults, RunManager.Instance, 0, 0f, null);
            string said = DevCheats.LastResult;
            Match m = marked.Match(said);
            int boxes = m.Success ? int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) : 0;
            bool deny = said.Contains(": DENY (");
            yield return QaGame.Frames(2);
            int red = MarkedBoxes();
            seen.Add(deny ? $"DENY, {boxes} box(es) named, {red} box(es) marked on the desk's papers" : said.Contains("APPROVE (no fault)") ? "APPROVE (no fault)" : said);
            if (deny && boxes > 0 && red > 0)
            {
                pass = true;
                yield return QaGame.Frames(15);
                runner.Recorder.Still(Path.Combine(runner.OutDir, "stills", $"cheat_{label}_{Screen.width}x{Screen.height}.png"));
                yield return QaGame.Until(() => !runner.Recorder.StillPending, 10f, "the still");
                break;
            }
            CaseInstance inst = GameManager.Current != null ? GameManager.Current.ActiveCase : null;
            DevCheats.Run(DevCheat.DecideCorrectly, RunManager.Instance, 0, 0f, null);
            yield return Poll(() => !QaGame.TravellerAtDesk() || (GameManager.Current != null && GameManager.Current.ActiveCase != inst), 20f);
        }
        runner.Assert("faults." + label, Owner, pass, string.Join("; ", seen),
            "CHEATS.md Reveal faults: the verdict the traveller should get, and their papers' boxes that show the fault marked in red");
    }
}
#endif
