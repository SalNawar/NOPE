#if UNITY_EDITOR || TIMEDESK_QA
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TextCore;
using UnityEngine.UI;

/// <summary>
/// CAM's QA rig ops (docs/pending/QA-RIG.md §4 "Tracks' own ops", §8's CAM
/// recipe; docs/pending/CAM.md's RESULT). Each asserts one thing the player
/// sees:
/// <list type="bullet">
/// <item><c>camres</c> (w, h): the rig's own resolution change, as <c>res</c>,
/// its frames tagged as the view change the rig asked for (a resize is not a
/// game move; QA-RIG hand-off: a frame whose aspect changed is a cut).</item>
/// <item><c>camframe</c> (name): home by home through the live camera, the
/// share of each footprint on screen (<c>frame.&lt;name&gt;</c>). The reading
/// view: the mat and the deal tray whole, the stamp rack and the intercom
/// whole (the desk plan's §6), the strip of each other in-use side home the
/// pose frames (at least 8 x 8 cm); the office view: the mat whole and every
/// shown home ≥98% (FIX_PLAN v1). Every home's real share is reported. The
/// reading view's bottom row on the desk (<c>frame.&lt;name&gt;.bottom</c>).</item>
/// <item><c>camfade</c> (target "reading" or "city"; name): the destination's
/// control's alpha per frame of a move and back (<c>fade.&lt;name&gt;</c>: 0
/// on frame 0 and until 2/3 of the move, 1 by its end; 1 at once when motion
/// is still).</item>
/// <item><c>camhold</c> (name): holds a stand-in on the desk's hold registry
/// and makes every view change one at a time (the mat, the drop tilt, a view
/// key's tilt, Return, the city's look and return, the PC), asserting after
/// each that nothing changed (<c>hold.&lt;name&gt;</c>, BUG-55c).</item>
/// <item><c>camparallax</c> (name; n = pointer frames, 90 when 0): at the
/// recorder's 30 fps step, A turns to the city (its sweep measured), the
/// pointer jumps edge to edge, and every frame's move of the nearest layer is
/// at most 1 px at 1080p (<c>city.parallax.&lt;name&gt;</c>, BUG-69); the
/// panorama covers the screen (<c>city.nobars</c>, BUG-42); a click on the
/// city returns (<c>city.click</c>); Q returns from it with the PC closed
/// (<c>city.q</c>).</item>
/// <item><c>cammotion</c> (n = 0 full motion, 1 Reduced Motion, 2 a Motion
/// intensity of 0): as the Settings toggle and slider set them.</item>
/// <item><c>camshake</c> (name; n = 1 when motion is still): fires each shake
/// moment through the game's own calls (FeelDirector) and asserts the shake
/// table's row it drew, or none (<c>shake.&lt;name&gt;.&lt;moment&gt;</c>), and
/// that the papers moved no more than the row's cap.</item>
/// <item><c>camchrome</c> (n = 1 starts, 0 stops; name): every frame between,
/// no shown chrome (the pull tabs, the Back control, "◀ City") is drawn over
/// a held thing or over the reading view's framed work
/// (<c>chrome.&lt;name&gt;</c>, BUG-59); the controls' alpha goes into the
/// recorder's rows.</item>
/// <item><c>camtilts</c> (n = 1 starts, 0 stops; name): between, the D1 drop
/// tilt is tagged at most once per paper landing and no view moves while
/// something is held (<c>tilts.&lt;name&gt;</c>).</item>
/// <item><c>camplay</c> (n = scans, 10 when 0; f = chip reads, 5 when 0):
/// the scripted day's desk work with the virtual mouse: every counter paper
/// dropped on the mat, scans on the scanner's bed, chip reads on the reader's
/// pad (a machine the reading view shows too little of reached from the
/// office view, as a player turns back), the intercom's wheel opened and
/// closed (<c>play.&lt;name&gt;</c>: it ran).</item>
/// <item><c>camcap</c> (name): in the reading view, the cap height in pixels
/// at 1080p of the text printed on the desk's papers; a report of the margin
/// to DOC-LOOK's floor (<c>capreport.&lt;name&gt;</c>; the pass is DOC-LOOK's at
/// #9).</item>
/// <item><c>camcycle</c> (n = cycles, 2 when 0): in the background, the
/// reading toggle and the city look and back, for a <c>gc</c> window.</item>
/// </list>
/// Registered at load in the editor and at play start in the QA player.
/// </summary>
public static class CamOps
{
    private const string Owner = "CAM";

    /// <summary>The flying traffic's parallax against the painting's (CityView.TrafficDepth).</summary>
    private const float TrafficDepth = 1.25f;

    /// <summary>A footprint is sampled on a grid this many cells a side, on its top and on the desk.</summary>
    private const int Samples = 12;

    /// <summary>A sampled share at or over this counts as whole (the grid's rounding).</summary>
    private const float Whole = 0.995f;

    /// <summary>The shown homes that are no tool in use (the art's mouse, the decorations' zone, the delivery crate): reported, not held to the office view's ≥98%.</summary>
    private static readonly string[] NotTools = { DeskHomeMap.Mouse, DeskHomeMap.Decor, DeskHomeMap.Crate };

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    private static void RegisterInEditor() => Register();
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        QaRunner.RegisterOp("camres", Resolution);
        QaRunner.RegisterOp("camframe", Frame);
        QaRunner.RegisterOp("camfade", Fade);
        QaRunner.RegisterOp("camhold", Hold);
        QaRunner.RegisterOp("camparallax", Parallax);
        QaRunner.RegisterOp("cammotion", Motion);
        QaRunner.RegisterOp("camshake", Shakes);
        QaRunner.RegisterOp("camchrome", Chrome);
        QaRunner.RegisterOp("camtilts", Tilts);
        QaRunner.RegisterOp("camplay", Play);
        QaRunner.RegisterOp("camcap", Cap);
        QaRunner.RegisterOp("camcycle", Cycle);
    }

    private static string Name(QaStep step, string fallback) => string.IsNullOrEmpty(step.name) ? fallback : step.name;

    // ------------------------------------------------------------------ camres, cammotion

    /// <summary>The rig's resolution change (QaRunner's <c>res</c>), each frame until it holds and three more tagged as the view change the rig asked for, then 12 untagged frames.</summary>
    private static IEnumerator Resolution(QaRunner runner, QaStep step)
    {
#if UNITY_EDITOR
        UnityEditor.PlayModeWindow.SetCustomRenderingResolution((uint)step.w, (uint)step.h, "QA");
#else
        Screen.SetResolution(step.w, step.h, FullScreenMode.Windowed);
#endif
        float until = Time.realtimeSinceStartup + 10f;
        int held = 0;
        while (held < 3)
        {
            QaSignals.TagCamera(CameraMoveCause.ViewKey, 0.05f);
            if (Screen.width == step.w && Screen.height == step.h)
                held++;
            else if (Time.realtimeSinceStartup > until)
                throw new TimeoutException($"The screen never reached {step.w}x{step.h}.");
            yield return null;
        }
        yield return QaGame.Frames(12);
    }

    /// <summary>Full motion (n = 0), Reduced Motion (1) or a Motion intensity of 0 (2), as the Settings toggle and slider set them (the QA player keeps its own preferences; a scenario sets full motion back before it ends).</summary>
    private static IEnumerator Motion(QaRunner runner, QaStep step)
    {
        MotionPreference.Reduced = step.n == 1;
        MotionPreference.Intensity = step.n == 2 ? 0f : 1f;
        yield return QaGame.Frames(2);
    }

    // ------------------------------------------------------------------ camframe

    /// <summary>The share (0..1) of the rectangle <paramref name="x0"/>..<paramref name="x1"/> by <paramref name="y0"/>..<paramref name="y1"/> (desk frame), sampled on the desk and at <paramref name="height"/>, inside <paramref name="cam"/>'s screen.</summary>
    private static float Share(Camera cam, DeskFrame frame, float x0, float y0, float x1, float y1, float height)
    {
        int inside = 0, total = 0;
        for (int i = 0; i <= Samples; i++)
            for (int j = 0; j <= Samples; j++)
                foreach (float z in new[] { 0f, Mathf.Max(0f, height) })
                {
                    Vector3 vp = cam.WorldToViewportPoint(frame.Point(new Vector2(Mathf.Lerp(x0, x1, i / (float)Samples), Mathf.Lerp(y0, y1, j / (float)Samples)), z));
                    total++;
                    if (vp.z > 0f && vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f)
                        inside++;
                }
        return total > 0 ? inside / (float)total : 0f;
    }

    /// <summary>The homes whose object shows now: a tracked object (DeskLayout) with a renderer on.</summary>
    private static HashSet<string> ShownHomes()
    {
        var shown = new HashSet<string>();
        foreach (Renderer r in UnityEngine.Object.FindObjectsByType<Renderer>())
            if (r.enabled && r.gameObject.activeInHierarchy && DeskLayout.HomeIdOf(r.transform) is string id)
                shown.Add(id);
        return shown;
    }

    private static IEnumerator Frame(QaRunner runner, QaStep step)
    {
        yield return null;
        string name = Name(step, "view");
        DeskView view = QaGame.Find<DeskView>();
        Camera cam = Camera.main;
        if (view == null || cam == null || !DeskLayout.Bound || !(QaGame.Field(view, "config") is DeskConfigSO config))
        {
            runner.Assert("frame." + name, Owner, false, "not measured", "no reading view, camera or bound desk");
            yield break;
        }
        DeskFrame frame = DeskLayout.Frame;
        DeskViewTuning t = config.deskView;
        DeskRect mat = DeskLayout.Mat.Rect;
        float matLeft = mat.CentreX - mat.Width / 2f, matRight = mat.CentreX + mat.Width / 2f;
        HashSet<string> shown = ShownHomes();
        var report = new List<string>();
        var failures = new List<string>();

        float matShare = Share(cam, frame, matLeft, mat.CentreY - mat.Height / 2f, matRight, mat.CentreY + mat.Height / 2f, t.paperLift);
        report.Add($"mat {matShare:P0}");
        if (matShare < Whole)
            failures.Add($"the mat {matShare:P0} on screen (whole)");

        foreach (DeskHome home in config.layout.homes)
        {
            if (home == null || (!shown.Contains(home.id) && home.id != DeskHomeMap.DealTray))
                continue;
            DeskRect f = home.Footprint;
            float x0 = f.CentreX - f.Width / 2f, x1 = f.CentreX + f.Width / 2f, y0 = f.CentreY - f.Height / 2f, y1 = f.CentreY + f.Height / 2f;
            float share = Share(cam, frame, x0, y0, x1, y1, home.height);
            report.Add($"{home.id} {share:P0}");
            if (!view.IsOn)
            {
                if (share < 0.98f && Array.IndexOf(NotTools, home.id) < 0)
                    failures.Add($"{home.id} {share:P0} on screen (≥98% in the office view)");
                continue;
            }
            if (t.homes == null || Array.IndexOf(t.homes, home.id) < 0)
                continue; // shown, not framed by the reading view (DETAIN, the Rolodex: the office view's)
            bool beside = x0 >= matRight || x1 <= matLeft;
            if (!beside || DeskViewPose.IsWhole(t, home.id))
            {
                if (share < Whole)
                    failures.Add($"{home.id} {share:P0} on screen (whole)");
                continue;
            }
            float reach = Mathf.Max(0.08f, t.sideReach);
            float sx0 = x0 >= matRight ? x0 : Mathf.Max(x0, x1 - reach), sx1 = x0 >= matRight ? Mathf.Min(x1, x0 + reach) : x1;
            float strip = Share(cam, frame, sx0, y0, sx1, y1, home.height);
            report[report.Count - 1] += $" (inner {Mathf.RoundToInt((sx1 - sx0) * 100f)} cm strip {strip:P0})";
            if (strip < Whole)
                failures.Add($"{home.id}'s inner {Mathf.RoundToInt((sx1 - sx0) * 100f)} cm strip {strip:P0} on screen (≥8 x 8 cm)");
        }
        runner.Assert("frame." + name, Owner, failures.Count == 0, $"view {(view.IsOn ? "reading" : "office")}, {Screen.width}x{Screen.height}: {string.Join(", ", report)}",
                      failures.Count > 0 ? string.Join("; ", failures) : view.IsOn ? "the mat, the tray, the rack and the intercom whole; 8 cm of every other in-use side home" : "the mat whole; every shown home ≥98%");
        if (!view.IsOn)
            yield break;
        Ray bottom = cam.ViewportPointToRay(new Vector3(0.5f, 0f, 0f));
        float front = config.layout.deskTop.Rect.CentreY - config.layout.deskTop.Rect.Height / 2f;
        bool hits = bottom.direction.y < -1e-5f;
        float rowY = hits ? frame.Local(bottom.GetPoint((frame.Origin.y - bottom.origin.y) / bottom.direction.y)).y : float.NegativeInfinity;
        runner.Assert("frame." + name + ".bottom", Owner, hits && rowY >= front - 0.005f, $"bottom row meets the desk at y {rowY:0.000} m (front edge {front:0.000})",
                      "the reading view's bottom row stays on the desk's top (under the desk never shows)");
    }

    // ------------------------------------------------------------------ camfade

    private static IEnumerator Fade(QaRunner runner, QaStep step)
    {
        string name = Name(step, step.target);
        bool city = string.Equals(step.target, "city", StringComparison.OrdinalIgnoreCase);
        DeskView desk = QaGame.Find<DeskView>();
        CityView cityView = QaGame.Find<CityView>();
        if (city ? cityView == null : desk == null)
        {
            runner.Assert("fade." + name, Owner, false, "not measured", "no " + (city ? "city view" : "reading view"));
            yield break;
        }
        var failures = new List<string>();
        int frames = 0;
        // In: the destination's control.
        float seconds = city ? CityTotal(cityView) : 0f;
        if (city)
            cityView.Look();
        else
            desk.TiltIn(CameraMoveCause.ViewKey);
        if (!city)
            seconds = (float)QaGame.Field(desk, "_moveSeconds");
        yield return Sample(city ? Group("CityBack") : Group(desk.BackControl), seconds, "in", failures, n => frames += n);
        // Back: the office's control.
        if (city)
            cityView.Return();
        else
            desk.Return();
        if (!city)
            seconds = (float)QaGame.Field(desk, "_moveSeconds");
        yield return Sample(city ? Group("CityLook") : Group("PcTab"), seconds, "back", failures, n => frames += n);
        runner.Assert("fade." + name, Owner, failures.Count == 0, $"{frames} frames sampled over {seconds:0.00} s moves (motion {(UiMotion.Amount.Still ? "still" : "on")})",
                      failures.Count > 0 ? string.Join("; ", failures) : "0 until 2/3 of each move, 1 by its end (a cut: 1 at once)");
    }

    /// <summary>Samples <paramref name="group"/>'s alpha each frame of a move of <paramref name="seconds"/> (plus a settle), noting each breach of the 2/3 rule.</summary>
    private static IEnumerator Sample(CanvasGroup group, float seconds, string leg, List<string> failures, Action<int> count)
    {
        float start = Time.unscaledTime;
        int frame = 0;
        while (true)
        {
            float elapsed = Time.unscaledTime - start;
            float alpha = group != null && group.gameObject.activeInHierarchy ? group.alpha : -1f;
            if (alpha >= 0f)
            {
                if (seconds <= 0f)
                {
                    if (frame > 0 && alpha < 0.99f && failures.Count < 12)
                        failures.Add($"{leg}: a cut move left the control at {alpha:0.00}");
                }
                else if ((frame == 0 || elapsed < seconds * DeskViewPose.ControlsFrom - 0.02f) && alpha > 0f && failures.Count < 12)
                    failures.Add($"{leg}: alpha {alpha:0.00} at {elapsed / seconds:P0} of the move (frame {frame})");
                else if (elapsed > seconds + 0.1f && alpha < 0.99f && failures.Count < 12)
                    failures.Add($"{leg}: alpha {alpha:0.00} after the move's end");
            }
            frame++;
            if (elapsed > seconds + 0.2f && frame > 2)
                break;
            yield return null;
        }
        if (group == null && failures.Count < 12)
            failures.Add($"{leg}: no control to sample");
        count(frame);
    }

    private static CanvasGroup Group(string pathOrName) => Group(QaGame.GameObjectAt(pathOrName) is GameObject go ? go.transform : null);

    private static CanvasGroup Group(Transform t) => t != null && t.TryGetComponent(out CanvasGroup g) ? g : null;

    private static float CityTotal(CityView view) =>
        UiMotion.Amount.Still || !(QaGame.Field(view, "config") is DeskConfigSO c) ? 0f : CityLookTimeline.Total(c.citySeconds, c.cityFadeFrom, c.cityFadeSeconds);

    // ------------------------------------------------------------------ camhold

    /// <summary>A stand-in for something held on the desk.</summary>
    private sealed class Held : IHoldable
    {
        public void CancelHold() { }
    }

    private static IEnumerator Hold(QaRunner runner, QaStep step)
    {
        string name = Name(step, "views");
        DeskView desk = QaGame.Find<DeskView>();
        CityView city = QaGame.Find<CityView>();
        OfficeViewController office = QaGame.Find<OfficeViewController>();
        var held = new Held();
        var calls = new List<string>();
        var moved = new List<string>();
        HoldRegistry.Desk.Begin(held);

        // Each view change alone, each checked before the next (a pair would hide a missing guard: an on undone by the next off).
        IEnumerator Try(string what, Action call)
        {
            bool reading = desk != null && desk.IsOn, looking = city != null && city.IsOn;
            OfficeView pc = office != null ? office.Current : OfficeView.OfficeFocus;
            call();
            yield return QaGame.Frames(2);
            calls.Add(what);
            if ((desk != null && desk.IsOn != reading) || (city != null && city.IsOn != looking) || (office != null && office.Current != pc))
                moved.Add($"{what}: reading {reading}->{desk?.IsOn}, city {looking}->{city?.IsOn}, PC {pc}->{office?.Current}");
        }

        if (desk != null)
        {
            yield return Try("the mat's click", desk.Toggle);
            yield return Try("the drop tilt", desk.TiltIn);
            yield return Try("a view key's tilt", () => desk.TiltIn(CameraMoveCause.ViewKey));
            yield return Try("Return", desk.Return);
        }
        if (city != null)
        {
            yield return Try("the city's look", city.Look);
            yield return Try("the city's return", city.Return);
        }
        if (office != null)
            yield return Try("the PC (Q)", office.FocusMonitor);
        HoldRegistry.Desk.End(held);
        runner.Assert("hold." + name, Owner, moved.Count == 0 && calls.Count > 0, $"{calls.Count} view changes made while holding, {moved.Count} went through",
                      moved.Count > 0 ? string.Join("; ", moved) : "each refused on its own: " + string.Join(", ", calls) + " (BUG-55c)");
    }

    // ------------------------------------------------------------------ camparallax

    private static IEnumerator Parallax(QaRunner runner, QaStep step)
    {
        string name = Name(step, "city");
        CityView city = QaGame.Find<CityView>();
        OfficeViewController office = QaGame.Find<OfficeViewController>();
        RawImage panorama = QaGame.GameObjectAt("Panorama") is GameObject go ? go.GetComponent<RawImage>() : null;
        QaInput input = QaInput.Current;
        if (city == null || panorama == null || input == null)
        {
            runner.Assert("city.parallax." + name, Owner, false, "not measured", "no city view, panorama or virtual pointer");
            yield break;
        }
        int restore = Time.captureFramerate;
        Time.captureFramerate = SeqRecorder.Fps;
        try
        {
            if (city.IsOn)
            {
                city.Return();
                yield return QaGame.Until(() => !(QaGame.GameObjectAt("CityScreen") is GameObject s && s.activeInHierarchy), 5f, "the city to fade out");
            }
            input.Set(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f), false);
            yield return input.Tap(Key.A);
            Canvas canvas = panorama.canvas;
            float worst = 0f, last = float.NaN;
            int frames = 0;
            // The sweep as the city comes into view, then the pointer jumping edge to edge.
            int sweepFrames = SeqRecorder.Fps * 3, pointerFrames = step.n > 0 ? step.n : 90;
            for (int i = 0; i < sweepFrames + pointerFrames; i++)
            {
                if (i >= sweepFrames)
                    input.Set(new Vector2(((i - sweepFrames) / 15) % 2 == 0 ? 2f : Screen.width - 2f, Screen.height * 0.5f), false);
                yield return null;
                if (!panorama.gameObject.activeInHierarchy || panorama.color.a <= 0f)
                {
                    last = float.NaN;
                    continue;
                }
                float scale = canvas != null ? canvas.rootCanvas.scaleFactor : 1f;
                float u = panorama.uvRect.x, span = panorama.uvRect.width;
                if (!float.IsNaN(last) && span > 0f)
                {
                    worst = Mathf.Max(worst, Mathf.Abs(u - last) / span * panorama.rectTransform.rect.width * scale * (1080f / Screen.height) * TrafficDepth);
                    frames++;
                }
                last = u;
            }
            runner.Assert("city.parallax." + name, Owner, frames > 0 && worst <= CityParallax.MaxPixelsPerFrame + 1e-3f,
                          $"worst {worst:0.000} px a 30 fps frame at 1080p over {frames} frames (the sweep, then the pointer edge to edge)",
                          "the nearest layer (traffic at 1.25x the painting) moves at most 1 px a frame (BUG-69)");
            Rect r = panorama.rectTransform.rect;
            float k = canvas != null ? canvas.rootCanvas.scaleFactor : 1f;
            runner.Assert("city.nobars." + name, Owner, r.width * k >= Screen.width - 1f && r.height * k >= Screen.height - 1f,
                          $"panorama {r.width * k:0}x{r.height * k:0} on a {Screen.width}x{Screen.height} screen", "the city fills the screen, never letterboxed (BUG-42)");

            // A left click on the city turns back to the desk (FIX_PLAN_v2's city exits).
            Vector2 centre = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            yield return input.Play(QaPaths.Click(centre));
            yield return QaGame.Frames(2);
            runner.Assert("city.click." + name, Owner, !city.IsOn, city.IsOn ? "still looking at the city" : "turned back", "a click on the city returns to the desk");
            yield return QaGame.Until(() => !(QaGame.GameObjectAt("CityScreen") is GameObject s && s.activeInHierarchy), 5f, "the city to fade out");

            // Q turns back too, and opens no PC (v2: "Q or a click on the city, the same as the PC").
            yield return input.Tap(Key.A);
            yield return QaGame.Until(() => city.IsOn && panorama.color.a >= 1f, 5f, "the city to show");
            yield return input.Tap(Key.Q);
            yield return QaGame.Frames(2);
            bool pcClosed = office == null || office.Current == OfficeView.OfficeFocus;
            runner.Assert("city.q." + name, Owner, !city.IsOn && pcClosed, $"city {(city.IsOn ? "on" : "off")}, PC {(pcClosed ? "closed" : "open")}", "Q leaves the city view as it leaves the PC");
            yield return QaGame.Until(() => !(QaGame.GameObjectAt("CityScreen") is GameObject s && s.activeInHierarchy), 5f, "the city to fade out");
        }
        finally
        {
            Time.captureFramerate = restore;
        }
    }

    // ------------------------------------------------------------------ camshake

    /// <summary>One shake call of the game's and the row it must draw (null: none).</summary>
    private readonly struct ShakeCall
    {
        public ShakeCall(string what, Action call, CameraShakeMoment? row)
        {
            What = what;
            Call = call;
            Row = row;
        }

        public string What { get; }
        public Action Call { get; }
        public CameraShakeMoment? Row { get; }
    }

    private static IEnumerator Shakes(QaRunner runner, QaStep step)
    {
        string name = Name(step, "shakes");
        bool still = step.n == 1;
        MotionKnobs knobs = UiMotion.Knobs;
        var calls = new[]
        {
            new ShakeCall("stamp", () => FeelDirector.Punch(FeelHit.Stamp), CameraShakeMoment.Stamp),
            new ShakeCall("detain", () => FeelDirector.Hit(knobs.detainHit), CameraShakeMoment.Detain),
            new ShakeCall("cited", () => FeelDirector.Hit(knobs.citedHit), CameraShakeMoment.Cited),
            new ShakeCall("breach", () => FeelDirector.Punch(FeelHit.Breach), CameraShakeMoment.Breach),
            new ShakeCall("citation_verdict", () => FeelDirector.Punch(FeelHit.Citation), null),
            new ShakeCall("approval", () => FeelDirector.Hit(knobs.approveHit), null),
            new ShakeCall("chip_insert", () => FeelDirector.Hit(0.15f), null),
            new ShakeCall("chip_fault", () => FeelDirector.Hit(0.3f), null),
        };
        var tags = new List<(CameraMoveCause cause, float seconds, float cap)>();
        void Tagged(CameraMoveCause cause, float seconds, float cap) => tags.Add((cause, seconds, cap));
        QaSignals.CameraTagged += Tagged;
        try
        {
            foreach (ShakeCall c in calls)
            {
                yield return QaGame.Frames(3);
                tags.Clear();
                Camera cam = Camera.main;
                (Vector3 world, Vector2 start)? paper = NearestPaperCorner(cam);
                c.Call();
                float worst = 0f;
                for (float t = 0f; t < 1.2f; t += Time.unscaledDeltaTime)
                {
                    yield return null;
                    if (paper.HasValue && cam != null)
                    {
                        Vector3 s = cam.WorldToScreenPoint(paper.Value.world);
                        worst = Mathf.Max(worst, QaCameraMoves.To1080(((Vector2)s - paper.Value.start).magnitude, Screen.height));
                    }
                }
                var impulses = tags.Where(x => x.cause == CameraMoveCause.Impulse).ToList();
                string got = impulses.Count == 0 ? "no shake" : string.Join(", ", impulses.Select(x => $"{x.cap:0.#} px / {x.seconds * 1000f:0} ms"));
                CameraShakeMoment? want = still ? null : c.Row;
                bool ok;
                string expect;
                if (want == null)
                {
                    ok = impulses.Count == 0;
                    expect = still ? "no shake while motion is still (G2)" : "no shake (§1.3: not on the list)";
                }
                else
                {
                    CameraShakeRow row = CameraShakeTable.Row(want.Value);
                    ok = impulses.Count == 1 && Mathf.Approximately(impulses[0].cap, row.CapPx) && Mathf.Approximately(impulses[0].seconds, row.Seconds) && worst <= row.CapPx + 0.05f;
                    expect = $"the {want.Value} row: {row.CapPx:0.#} px / {row.Seconds * 1000f:0} ms, the papers moving no more than {row.CapPx:0.#} px";
                }
                runner.Assert($"shake.{name}.{c.What}", Owner, ok, $"{got}; papers moved {worst:0.00} px at 1080p", expect);
                yield return new WaitForSecondsRealtime(0.5f);
            }
        }
        finally
        {
            QaSignals.CameraTagged -= Tagged;
        }
    }

    /// <summary>The paper mat's corner nearest <paramref name="cam"/> (the papers: the eye's work) and where it shows now, or null.</summary>
    private static (Vector3 world, Vector2 start)? NearestPaperCorner(Camera cam)
    {
        if (cam == null || !DeskLayout.Bound)
            return null;
        DeskRect mat = DeskLayout.Mat.Rect;
        (Vector3, Vector2)? best = null;
        float nearest = float.MaxValue;
        for (int corner = 0; corner < 4; corner++)
        {
            (float x, float y) = mat.PointAt(corner % 2, corner / 2);
            Vector3 world = DeskLayout.Frame.Point(new Vector2(x, y));
            Vector3 s = cam.WorldToScreenPoint(world);
            if (s.z > 0f && s.z < nearest && s.x >= 0f && s.y >= 0f && s.x <= Screen.width && s.y <= Screen.height)
            {
                nearest = s.z;
                best = (world, new Vector2(s.x, s.y));
            }
        }
        return best;
    }

    // ------------------------------------------------------------------ camchrome

    /// <summary>A running chrome watch (camchrome n = 1 to n = 0).</summary>
    private sealed class ChromeWatch
    {
        public readonly List<(string name, GameObject go, CanvasGroup group)> Controls = new List<(string, GameObject, CanvasGroup)>();
        public readonly List<string> Violations = new List<string>();
        public int Frames, HoldingFrames, ReadingFrames, Over;
        public bool Running;
    }

    private static ChromeWatch _chrome;

    /// <summary>The chrome a view shows (BUG-59's): the PC and STAMPS pull tabs, the reading view's Back control, "◀ City".</summary>
    private static readonly string[] ChromeNames = { "PcTab", "StampBar/Tab", "DeskViewBack", "CityLook" };

    private static IEnumerator Chrome(QaRunner runner, QaStep step)
    {
        string name = Name(step, "chrome");
        if (step.n > 0)
        {
            if (_chrome != null)
                _chrome.Running = false;
            var watch = new ChromeWatch { Running = true };
            foreach (string control in ChromeNames)
            {
                GameObject go = TryAt(control);
                if (go == null)
                    continue;
                CanvasGroup group = Group(go.transform);
                watch.Controls.Add((control, go, group));
                runner.Recorder.Column("alpha:" + control, () => AlphaOf(go, group).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
            }
            _chrome = watch;
            runner.StartCoroutine(Watch(watch));
            yield break;
        }
        ChromeWatch done = _chrome;
        _chrome = null;
        foreach (string control in ChromeNames)
            runner.Recorder.Forget("alpha:" + control);
        if (done == null)
        {
            runner.Assert("chrome." + name, Owner, false, "not measured", "no camchrome watch was started (n = 1)");
            yield break;
        }
        done.Running = false;
        runner.Assert("chrome." + name, Owner, done.Over == 0 && done.Frames > 0,
                      $"{done.Frames} frames watched ({done.HoldingFrames} holding, {done.ReadingFrames} in the reading view), {done.Over} with chrome over the work, {Screen.width}x{Screen.height}",
                      done.Violations.Count > 0 ? string.Join("; ", done.Violations) : "no tab, Back control or city tab drawn over a held thing or the reading view's framed work (BUG-59)");
    }

    private static GameObject TryAt(string pathOrName)
    {
        try
        {
            return QaGame.GameObjectAt(pathOrName);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static float AlphaOf(GameObject go, CanvasGroup group) => go == null || !go.activeInHierarchy ? 0f : group != null ? group.alpha : 1f;

    /// <summary>Each frame's end: every shown chrome control's screen rect against the held thing's and, in the reading view, the framed work's.</summary>
    private static IEnumerator Watch(ChromeWatch watch)
    {
        var eof = new WaitForEndOfFrame();
        while (watch.Running)
        {
            yield return eof;
            if (!watch.Running)
                yield break;
            watch.Frames++;
            Camera cam = Camera.main;
            var work = new List<(string what, Rect rect)>();
            if (HoldRegistry.Desk.Current is Component held && QaGame.ScreenRect(held.gameObject) is Rect heldRect)
            {
                watch.HoldingFrames++;
                work.Add(("the held " + held.name, heldRect));
            }
            DeskView view = QaGame.Find<DeskView>();
            // The reading view's framed work, once its tilt has come to rest (mid-blend the camera frames a wider view).
            if (view != null && view.IsOn && cam != null && DeskLayout.Bound && QaGame.Field(view, "config") is DeskConfigSO config &&
                Time.unscaledTime - (float)QaGame.Field(view, "_movedAt") >= (float)QaGame.Field(view, "_moveSeconds") + 0.05f)
            {
                watch.ReadingFrames++;
                Rect? framed = Enclose(cam, DeskViewPose.Targets(config.deskView, DeskLayout.Mat.Rect, config.layout.homes));
                if (framed.HasValue)
                    work.Add(("the reading view's work", framed.Value));
            }
            bool over = false;
            foreach ((string control, GameObject go, CanvasGroup group) in watch.Controls)
            {
                if (AlphaOf(go, group) <= 0f || !(QaGame.ScreenRect(go) is Rect rect))
                    continue;
                foreach ((string what, Rect area) in work)
                    if (rect.Overlaps(area))
                    {
                        over = true;
                        if (watch.Violations.Count < 12)
                            watch.Violations.Add($"frame {Time.frameCount}: {control} over {what}");
                    }
            }
            if (over)
                watch.Over++;
        }
    }

    /// <summary>The screen rect enclosing <paramref name="points"/> (desk frame) through <paramref name="cam"/>, or null when one is behind it.</summary>
    private static Rect? Enclose(Camera cam, List<(float x, float y, float z)> points)
    {
        float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
        foreach ((float x, float y, float z) in points)
        {
            Vector3 s = cam.WorldToScreenPoint(DeskLayout.Frame.Point(new Vector2(x, y), z));
            if (s.z <= 0f)
                return null;
            x0 = Mathf.Min(x0, s.x);
            y0 = Mathf.Min(y0, s.y);
            x1 = Mathf.Max(x1, s.x);
            y1 = Mathf.Max(y1, s.y);
        }
        return points.Count > 0 ? Rect.MinMaxRect(x0, y0, x1, y1) : (Rect?)null;
    }

    // ------------------------------------------------------------------ camtilts

    /// <summary>A running tilt count (camtilts n = 1 to n = 0).</summary>
    private sealed class TiltCount
    {
        public DeskController Desk;
        public int Landings, DropTilts, ViewKeys, WhileHolding;
        public readonly List<string> Notes = new List<string>();

        public void Landed(int paper) => Landings++;

        public void Tagged(CameraMoveCause cause, float seconds, float cap)
        {
            if (cause == CameraMoveCause.DeskDropTilt)
                DropTilts++;
            else if (cause == CameraMoveCause.ViewKey)
                ViewKeys++;
            if (cause != CameraMoveCause.Impulse && HoldRegistry.Desk.IsHolding)
            {
                WhileHolding++;
                if (Notes.Count < 8)
                    Notes.Add($"{cause} at frame {Time.frameCount} while holding");
            }
        }
    }

    private static TiltCount _tilts;

    private static IEnumerator Tilts(QaRunner runner, QaStep step)
    {
        string name = Name(step, "tilts");
        if (step.n > 0)
        {
            Stop(_tilts);
            _tilts = new TiltCount { Desk = QaGame.Find<DeskController>() };
            if (_tilts.Desk != null)
                _tilts.Desk.PaperExamined += _tilts.Landed;
            QaSignals.CameraTagged += _tilts.Tagged;
            yield break;
        }
        TiltCount done = _tilts;
        _tilts = null;
        if (done == null)
        {
            runner.Assert("tilts." + name, Owner, false, "not measured", "no camtilts count was started (n = 1)");
            yield break;
        }
        Stop(done);
        bool ok = done.Desk != null && done.Landings > 0 && done.DropTilts <= done.Landings && done.WhileHolding == 0;
        runner.Assert("tilts." + name, Owner, ok, $"{done.Landings} paper landings, {done.DropTilts} drop tilts, {done.ViewKeys} view-key moves, {done.WhileHolding} while holding",
                      done.Notes.Count > 0 ? string.Join("; ", done.Notes) : "the D1 tilt at most once per drop, never while holding (v2's CAM pass at #6); 0 untagged moves is the run's camera assertion");
    }

    private static void Stop(TiltCount count)
    {
        if (count == null)
            return;
        if (count.Desk != null)
            count.Desk.PaperExamined -= count.Landed;
        QaSignals.CameraTagged -= count.Tagged;
    }

    // ------------------------------------------------------------------ camplay

    /// <summary>A plain drag with the virtual mouse from <paramref name="from"/> to <paramref name="to"/> (screen pixels) at the recorder's 30 fps step, as the runner's <c>drag</c> plays it.</summary>
    private static IEnumerator DragAt(QaInput input, Vector2 from, Vector2 to)
    {
        int restore = Time.captureFramerate;
        Time.captureFramerate = SeqRecorder.Fps;
        try
        {
            yield return input.Play(QaPaths.Drag(QaPathKind.Plain, from, to, new Vector2(Screen.width, Screen.height)));
        }
        finally
        {
            Time.captureFramerate = restore;
        }
    }

    /// <summary>The screen point of the world point <paramref name="world"/> through the main camera, or null when off the screen.</summary>
    private static Vector2? ScreenOf(Vector3 world)
    {
        Camera cam = Camera.main;
        if (cam == null)
            return null;
        Vector3 s = cam.WorldToScreenPoint(world);
        return s.z > 0f && s.x >= 0f && s.y >= 0f && s.x <= Screen.width && s.y <= Screen.height ? new Vector2(s.x, s.y) : (Vector2?)null;
    }

    private static List<DeskDocument> Papers(DeskZone zone) =>
        UnityEngine.Object.FindObjectsByType<DeskDocument>()
                      .Where(p => p.isActiveAndEnabled && p.Zone == zone && !p.IsSliding).OrderBy(p => p.Index).ToList();

    private static IEnumerator Play(QaRunner runner, QaStep step)
    {
        QaInput input = QaInput.Current;
        if (input == null || !DeskLayout.Bound)
            throw new InvalidOperationException("camplay needs the virtual mouse and a bound desk.");
        var notes = new List<string>();
        yield return QaGame.Until(() => Papers(DeskZone.Counter).Count > 0, 10f, "papers on the counter");

        // Every counter paper dropped on the mat, each at its own spot (the D1 drop tilt comes with the first).
        DeskRect mat = DeskLayout.Mat.Rect;
        int drops = 0;
        for (int guard = 0; guard < 8; guard++)
        {
            List<DeskDocument> counter = Papers(DeskZone.Counter);
            if (counter.Count == 0)
                break;
            Vector2? from = QaGame.ScreenPoint(counter[0].gameObject);
            (float x, float y) spot = mat.PointAt(0.3f + 0.13f * (drops % 4), 0.35f + 0.15f * (drops / 4 % 2));
            Vector2? to = ScreenOf(DeskLayout.Frame.Point(new Vector2(spot.x, spot.y)));
            if (from == null || to == null)
            {
                notes.Add($"paper {counter[0].Index} or its spot is off the screen");
                break;
            }
            yield return DragAt(input, from.Value, to.Value);
            drops++;
            yield return new WaitForSecondsRealtime(0.8f); // the tilt's blend and the paper's slide settle
        }

        // A machine the reading view shows too little of is reached from the office view: Back, a view key, as a player turns.
        DeskView view = QaGame.Find<DeskView>();
        IEnumerator Reach(Vector3 world)
        {
            if (ScreenOf(world) == null && view != null && view.IsOn)
            {
                view.Return();
                yield return new WaitForSecondsRealtime(1f);
            }
        }

        // Scans: a desk paper onto the scanner's bed.
        int scans = 0, scanTarget = step.n > 0 ? step.n : 10;
        DeskScanner scanner = QaGame.Find<DeskScanner>();
        for (int i = 0; scanner != null && scanner.OnDesk && i < scanTarget; i++)
        {
            List<DeskDocument> desk = Papers(DeskZone.Desk);
            if (desk.Count == 0)
                break;
            DeskDocument paper = desk[i % desk.Count];
            yield return Reach(scanner.BedPoint);
            Vector2? from = QaGame.ScreenPoint(paper.gameObject), to = ScreenOf(scanner.BedPoint);
            if (from == null || to == null)
            {
                notes.Add("the scanner's bed is off the screen");
                break;
            }
            yield return DragAt(input, from.Value, to.Value);
            scans++;
            yield return new WaitForSecondsRealtime(2.5f);
        }

        // Chip reads: a desk paper onto the reader's pad (a passport reads; anything else bounces back).
        int reads = 0, readTarget = step.f > 0f ? Mathf.RoundToInt(step.f) : 5;
        ChipReader reader = QaGame.Find<ChipReader>();
        for (int i = 0; reader != null && reader.isActiveAndEnabled && i < readTarget; i++)
        {
            List<DeskDocument> desk = Papers(DeskZone.Desk);
            if (desk.Count == 0)
                break;
            DeskDocument paper = desk[i % desk.Count];
            yield return Reach(reader.DockPoint(MachineDock.Pad));
            Vector2? from = QaGame.ScreenPoint(paper.gameObject), to = ScreenOf(reader.DockPoint(MachineDock.Pad));
            if (from == null || to == null)
            {
                notes.Add("the chip reader's pad is off the screen");
                break;
            }
            yield return DragAt(input, from.Value, to.Value);
            reads++;
            yield return new WaitForSecondsRealtime(2f);
        }

        // The intercom: its wheel opens in the view the player is in (no camera move), and Esc closes it.
        bool wheel = false;
        if (DeskLayout.TryPose(DeskHomeMap.Intercom, out Vector3 intercom, out _) && ScreenOf(intercom) is Vector2 at)
        {
            yield return input.Play(QaPaths.Click(at));
            yield return new WaitForSecondsRealtime(0.5f);
            wheel = QaGame.Find<TravellerWheel>() is TravellerWheel w && w.IsOpen;
            yield return input.Tap(Key.Escape);
            yield return new WaitForSecondsRealtime(0.5f);
        }
        runner.Assert("play." + Name(step, "desk"), Owner, drops > 0, $"{drops} drops, {scans} scans, {reads} chip reads, the intercom's wheel {(wheel ? "opened" : "not opened")}",
                      notes.Count > 0 ? string.Join("; ", notes) : "the scripted desk work ran (its camera moves: the run's camera assertion and camtilts)");
    }

    // ------------------------------------------------------------------ camcap

    private static IEnumerator Cap(QaRunner runner, QaStep step)
    {
        yield return null;
        string name = Name(step, "reading");
        Camera cam = Camera.main;
        DeskView view = QaGame.Find<DeskView>();
        float floor = Screen.height >= 1000 ? 10f : 7f;
        var caps = new List<(float px, string what)>();
        if (cam != null)
            foreach (DeskDocument paper in UnityEngine.Object.FindObjectsByType<DeskDocument>())
            {
                if (!paper.isActiveAndEnabled || paper.Zone != DeskZone.Desk)
                    continue;
                foreach (TMP_Text text in paper.GetComponentsInChildren<TMP_Text>(false))
                    if (CapPixels(cam, text) is float px)
                        caps.Add((px, $"{paper.Index}/{text.name}"));
            }
        if (caps.Count == 0)
        {
            runner.Assert("capreport." + name, Owner, false, "not measured", view != null && view.IsOn ? "no printed text on a desk paper" : "not in the reading view");
            yield break;
        }
        caps.Sort((a, b) => a.px.CompareTo(b.px));
        float median = caps[caps.Count / 2].px;
        string smallest = string.Join(", ", caps.Take(5).Select(c => $"{c.what} {c.px:0.0}"));
        runner.Assert("capreport." + name, Owner, true,
                      $"cap height at 1080p: least {caps[0].px:0.0} px, median {median:0.0} px over {caps.Count} texts; margin to DOC-LOOK's floor ({floor:0} px at {Screen.height} rows): {caps[0].px - floor:+0.0;-0.0} px",
                      $"a report at CAM's pose (the desk plan's §6 smoke; the pass is DOC-LOOK's at #9); smallest: {smallest}");
    }

    /// <summary>The cap height of <paramref name="text"/>'s largest visible letter, in pixels at 1080p through <paramref name="cam"/> (its font's cap line over its ascent line, at the letter's baseline), or null with none shown.</summary>
    private static float? CapPixels(Camera cam, TMP_Text text)
    {
        if (text == null || !text.isActiveAndEnabled || text.font == null || string.IsNullOrEmpty(text.text))
            return null;
        text.ForceMeshUpdate();
        TMP_TextInfo info = text.textInfo;
        FaceInfo face = text.font.faceInfo;
        if (info == null || face.ascentLine <= 0f)
            return null;
        float best = 0f;
        for (int i = 0; i < info.characterCount; i++)
        {
            TMP_CharacterInfo c = info.characterInfo[i];
            if (!c.isVisible || char.IsWhiteSpace(c.character))
                continue;
            float cap = (c.ascender - c.baseLine) * face.capLine / face.ascentLine;
            Vector3 a = cam.WorldToScreenPoint(text.transform.TransformPoint(new Vector3(c.origin, c.baseLine, 0f)));
            Vector3 b = cam.WorldToScreenPoint(text.transform.TransformPoint(new Vector3(c.origin, c.baseLine + cap, 0f)));
            if (a.z <= 0f || b.z <= 0f)
                continue;
            best = Mathf.Max(best, QaCameraMoves.To1080(((Vector2)b - (Vector2)a).magnitude, Screen.height));
        }
        return best > 0f ? best : (float?)null;
    }

    // ------------------------------------------------------------------ camcycle

    /// <summary>Starts the background cycle and returns at once, so a following <c>gc</c> step measures it.</summary>
    private static IEnumerator Cycle(QaRunner runner, QaStep step)
    {
        runner.StartCoroutine(Cycling(step.n > 0 ? step.n : 2));
        yield break;
    }

    /// <summary>The reading view toggled in and out, then the city looked at and back, <paramref name="cycles"/> times.</summary>
    private static IEnumerator Cycling(int cycles)
    {
        DeskView desk = QaGame.Find<DeskView>();
        CityView city = QaGame.Find<CityView>();
        for (int i = 0; i < cycles; i++)
        {
            if (desk != null)
            {
                desk.TiltIn(CameraMoveCause.ViewKey);
                yield return new WaitForSecondsRealtime(0.8f);
                desk.Return();
                yield return new WaitForSecondsRealtime(0.8f);
            }
            if (city != null)
            {
                city.Look();
                yield return new WaitForSecondsRealtime(2f);
                city.Return();
                yield return new WaitForSecondsRealtime(2f);
            }
        }
    }
}
#endif
