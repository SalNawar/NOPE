#if UNITY_EDITOR || TIMEDESK_QA
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
/// <item><c>camframe</c> (target "office" or "reading", the view the leg
/// frames; name): home by home through the live camera, the share of each
/// footprint on screen (<c>frame.&lt;name&gt;</c>). The reading
/// view: the mat and the deal tray whole, the stamp rack and the intercom
/// whole (the desk plan's §6), the strip of each other in-use side home the
/// pose frames (at least 8 x 8 cm); the office view: the mat whole and every
/// shown home ≥98% (FIX_PLAN v1). Every home's real share is reported. The
/// reading view's bottom row on the desk (<c>frame.&lt;name&gt;.bottom</c>).
/// A leg whose view is not the one on is red "not measured", both lines of a
/// reading leg (<see cref="ReadingLeg"/>).</item>
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
/// at most 1 px at 1080p (<c>city.parallax.&lt;name&gt;</c>, BUG-69); held at
/// the other edge, the look lands within 2 s (<c>city.look</c>: no crawl);
/// the panorama covers the screen (<c>city.nobars</c>, BUG-42); a click on
/// the city returns (<c>city.click</c>); Q returns from it with the PC
/// closed (<c>city.q</c>); F1 opens the PC with its keys card as on main
/// (<c>city.f1</c>).</item>
/// <item><c>cammotion</c> (n = 0 full motion, 1 Reduced Motion, 2 a Motion
/// intensity of 0): as the Settings toggle and slider set them.</item>
/// <item><c>camshake</c> (name; n = 1 when motion is still): fires each shake
/// moment through the game's own calls (FeelDirector) and asserts the shake
/// table's row it drew, or none (<c>shake.&lt;name&gt;.&lt;moment&gt;</c>), and
/// that the papers moved no more than the row's cap.</item>
/// <item><c>camchrome</c> (n = 1 starts, its target naming what the leg
/// shows: "held" for its drag's held decoration, "reading" for the reading
/// view at rest, a plate's key, "guide" on day 1; 0 stops; 2 shows the stamp
/// hint plate as the counter's bounce does; name): every frame between, no
/// shown chrome (the pull tabs, the Back control, "◀ City") is drawn over a
/// held thing or over the reading view's framed work
/// (<c>chrome.&lt;name&gt;</c>, BUG-59; red when a half the leg shows was
/// never seen, a held thing found by its Root when it is no Component, or
/// when one of CAM's controls was not found), and each overlay plate CAM
/// measured over the work that was
/// drawn gets its own line under its owner (<c>plate.&lt;name&gt;.guide</c>,
/// CONTENT; <c>.stamphint</c>, STAMP); a plate the leg showed that was never
/// drawn gets a red "not measured" line under CAM
/// (<c>chrome.&lt;name&gt;.&lt;key&gt;</c>). The lines come from the pure
/// <see cref="ShownBy"/> and <see cref="LegLines"/>; the alphas go into the
/// recorder's rows.</item>
/// <item><c>camtilts</c> (n = 1 starts, 0 stops; name): between, from the
/// views' own turns (not the push-in's re-tags), every paper landing with
/// the reading view off and live tilts in once in its frame, none without a
/// landing, and nothing turns while something is held
/// (<c>tilts.&lt;name&gt;</c>).</item>
/// <item><c>camplay</c> (n = scans, 10 when 0; f = chip reads, 5 when 0):
/// the scripted day 12 with the virtual mouse: every counter paper dropped on
/// the mat (every other one from the office view), the Auto-Feed tray
/// clicked and its stack scanned, n scans finished on the scanner's bed, f
/// chip reads on the reader, each counted only once its paper came back out
/// (a stuck read ends the reads: the reader refuses the next paper while it
/// holds one), the intercom's wheel opened and a ring question
/// answered (a home the reading view shows too little of reached from the
/// office view, as a player turns back); <c>play.&lt;name&gt;</c> passes on
/// exactly those counts. The Rolodex's right-click is the one gap (QaInput
/// has no right button).</item>
/// <item><c>camcap</c> (name): in the reading view, the cap height in pixels
/// at 1080p of the text printed on the desk's papers; a report of the margin
/// to DOC-LOOK's floor (<c>capreport.&lt;name&gt;</c>; the pass is DOC-LOOK's at
/// #9); red "not measured" outside the reading view (CAM's pose), and with
/// no paper on the desk, so cam_views runs it
/// only after camplay has laid the day's papers there (a handed paper waits
/// on the counter).</item>
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

    /// <summary>The city's pointer look lands within this many seconds of an edge-to-edge pointer move (CityParallax.LookShare: about 1.2 s at any frame rate; the knob's own range crawled about 7 s at the speed limit).</summary>
    private const float LookSettleSeconds = 2f;

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

    /// <summary>A camframe step's target naming the office view (the reading view's is <see cref="ShowsReading"/>).</summary>
    public const string FramesOffice = "office";

    /// <summary>
    /// The view a camframe step's leg names (pure; its target): true for the
    /// reading view (<see cref="ShowsReading"/>), false for the office view
    /// (<see cref="FramesOffice"/>). The op judges a leg only while that view
    /// is the one on (DeskView.Toggle does nothing unless its toggle is live,
    /// BoothRules.DeskViewAllowed), so a reading leg is never judged on the
    /// office view's criteria without its bottom line. A missing or unknown
    /// view throws: no leg is judged on whichever view happens to be on.
    /// </summary>
    public static bool ReadingLeg(string target)
    {
        string view = (target ?? string.Empty).Trim();
        if (view == ShowsReading)
            return true;
        if (view == FramesOffice)
            return false;
        throw new InvalidOperationException($"camframe: a leg names the view it frames in its target, {FramesOffice} or {ShowsReading} (not '{target}').");
    }

    /// <summary>A framing leg that could not be measured: its line red, and a reading leg's bottom line too, so neither goes missing in silence.</summary>
    private static void NotMeasured(QaRunner runner, string name, bool reading, string why)
    {
        runner.Assert("frame." + name, Owner, false, "not measured", why);
        if (reading)
            runner.Assert("frame." + name + ".bottom", Owner, false, "not measured", why);
    }

    private static IEnumerator Frame(QaRunner runner, QaStep step)
    {
        bool reading = ReadingLeg(step.target);
        yield return null;
        string name = Name(step, "view");
        DeskView view = QaGame.Find<DeskView>();
        Camera cam = Camera.main;
        if (view == null || cam == null || !DeskLayout.Bound || !(QaGame.Field(view, "config") is DeskConfigSO config))
        {
            NotMeasured(runner, name, reading, "no reading view, camera or bound desk");
            yield break;
        }
        if (view.IsOn != reading)
        {
            NotMeasured(runner, name, reading, reading
                ? "the leg frames the reading view, but the office view is on (DeskView.Toggle does nothing unless its toggle is live: BoothRules.DeskViewAllowed, the PC closed and no newsletter up)"
                : "the leg frames the office view, but the reading view is on (it never returned: nothing turns the view while something is held)");
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
            if (!reading)
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
        runner.Assert("frame." + name, Owner, failures.Count == 0, $"view {(reading ? "reading" : "office")}, {Screen.width}x{Screen.height}: {string.Join(", ", report)}",
                      failures.Count > 0 ? string.Join("; ", failures) : reading ? "the mat, the tray, the rack and the intercom whole; 8 cm of every other in-use side home" : "the mat whole; every shown home ≥98%");
        if (!reading)
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

            // The look after the last jump: the pointer held at the other edge, the seconds until the pan lands (a step under 0.01 px for 3 frames).
            float TrafficPx(float du) => panorama.uvRect.width > 0f
                ? Mathf.Abs(du) / panorama.uvRect.width * panorama.rectTransform.rect.width * (canvas != null ? canvas.rootCanvas.scaleFactor : 1f) * (1080f / Screen.height) * TrafficDepth
                : 0f;
            float lookFrom = panorama.uvRect.x, settled = -1f;
            int stillFrames = 0;
            bool toRight = ((pointerFrames - 1) / 15) % 2 == 0;
            input.Set(new Vector2(toRight ? Screen.width - 2f : 2f, Screen.height * 0.5f), false);
            last = panorama.uvRect.x;
            for (int i = 0; i < SeqRecorder.Fps * 4 && settled < 0f; i++)
            {
                yield return null;
                float px = TrafficPx(panorama.uvRect.x - last);
                last = panorama.uvRect.x;
                worst = Mathf.Max(worst, px);
                frames++;
                stillFrames = px < 0.01f ? stillFrames + 1 : 0;
                if (stillFrames >= 3)
                    settled = (i - 2) / (float)SeqRecorder.Fps;
            }
            float lookPx = TrafficPx(panorama.uvRect.x - lookFrom);
            runner.Assert("city.parallax." + name, Owner, frames > 0 && worst <= CityParallax.MaxPixelsPerFrame + 1e-3f,
                          $"worst {worst:0.000} px a 30 fps frame at 1080p over {frames} frames (the sweep, the pointer edge to edge, the look settling)",
                          "the nearest layer (traffic at 1.25x the painting) moves at most 1 px a frame (BUG-69)");
            runner.Assert("city.look." + name, Owner, settled >= 0f && settled <= LookSettleSeconds,
                          settled >= 0f ? $"the look landed {settled:0.00} s after the pointer crossed the screen, the traffic moving {lookPx:0.0} px" : $"the look still moving after 4 s ({lookPx:0.0} px so far)",
                          $"an edge-to-edge pointer move eases in within {LookSettleSeconds:0.0} s, never a crawl at the limit (CityParallax.LookShare; the knob's own range took about 7 s: Needs Saleh 4)");
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

            // F1 still opens the PC with its keys card from the city, as on main (only Q's press leaves the city: OfficeViewController).
            DesktopKeyboard keys = QaGame.Find<DesktopKeyboard>();
            yield return input.Tap(Key.A);
            yield return QaGame.Until(() => city.IsOn && panorama.color.a >= 1f, 5f, "the city to show");
            yield return input.Tap(Key.F1);
            yield return QaGame.Frames(4);
            bool pcOpen = office != null && office.Current == OfficeView.MonitorFocus, card = keys != null && keys.CardOpen;
            runner.Assert("city.f1." + name, Owner, pcOpen && card && !city.IsOn,
                          $"PC {(pcOpen ? "open" : "closed")}, keys card {(card ? "shown" : "hidden")}, city {(city.IsOn ? "on" : "off")}",
                          "F1 in the city view opens the PC with the keys card, the city turning back as the PC takes over (as on main)");
            if (card)
                keys.ToggleCard(); // as F1 on the PC hides it
            if (office != null && office.Current == OfficeView.MonitorFocus)
            {
                yield return input.Tap(Key.Q);
                yield return QaGame.Until(() => office.Current == OfficeView.OfficeFocus, 5f, "the PC to close");
            }
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

    /// <summary>One watched piece of the overlay: its path, object and fade, its plate's key (null: CAM's own chrome), and its counts.</summary>
    private sealed class Watched
    {
        public string Path, Key;
        public GameObject Go;
        public CanvasGroup Group;
        public int Shown, Over;
        public readonly List<string> Notes = new List<string>();
    }

    /// <summary>A running chrome watch (camchrome n = 1 to n = 0), with what its leg shows (<see cref="ShownBy"/>: each must be seen).</summary>
    private sealed class ChromeWatch
    {
        public readonly List<Watched> Controls = new List<Watched>();
        public readonly List<string> Violations = new List<string>();
        public readonly HashSet<string> Shows = new HashSet<string>();
        public int Frames, HoldingFrames, UnplacedFrames, ReadingFrames, Over;
        public string Unplaced;
        public bool Running;
    }

    private static ChromeWatch _chrome;

    /// <summary>The chrome a view shows (BUG-59's, CAM's): the PC and STAMPS pull tabs, the reading view's Back control, "◀ City".</summary>
    private static readonly string[] ChromeNames = { "PcTab", "StampBar/Tab", "DeskViewBack", "CityLook" };

    /// <summary>
    /// The overlay's plates CAM measured over the reading view's work (CAM.md's
    /// hand-offs), each its own line under its owner until it moves: the
    /// guide's prompt plate (CONTENT; day 1, over the intercom's top) and the
    /// stamp hint plate (STAMP; over the rack). Their lines
    /// (<c>plate.&lt;leg&gt;.&lt;key&gt;</c>) are outside CAM's
    /// <c>cam_chrome.chrome.*</c>: each owner's own gate reads them
    /// (CamChecks' <c>cam.play.guidePlate</c>, CONTENT;
    /// <c>cam.play.stampHint</c>, STAMP), so <c>cam.play.chrome</c> goes
    /// green on CAM's own chrome while they are red. A plate a leg shows that
    /// is never drawn is CAM's red "not measured" line
    /// (<c>chrome.&lt;leg&gt;.&lt;key&gt;</c>, <see cref="LegLines"/>).
    /// </summary>
    private static readonly (string path, string key, string owner)[] Plates = { ("GuidePrompt/Plate", "guide", "CONTENT"), ("StampBar/Hint", StampHint, "STAMP") };

    /// <summary>The stamp hint plate's key (its line: <c>plate.&lt;leg&gt;.stamphint</c>); camchrome n = 2 shows it.</summary>
    private const string StampHint = "stamphint";

    /// <summary>What a chrome leg shows besides its plates (camchrome n = 1's target): a held thing (the leg's drag holds a decoration).</summary>
    public const string ShowsHeld = "held";

    /// <summary>What a chrome leg shows besides its plates (camchrome n = 1's target): the reading view at rest.</summary>
    public const string ShowsReading = "reading";

    /// <summary>The note the counter's bounce shows on the stamp hint plate (DeskController: a paper dropped back on the counter before the verdict).</summary>
    private const string BounceNote = "stamp.refused.handBackFirst";

    /// <summary>
    /// What a camchrome step makes its leg show (pure). n = 1 (the leg's
    /// start): its target's keys, split on commas and spaces: a plate's key
    /// ("guide" on day 1, where the guide's prompt plate shows),
    /// <see cref="ShowsHeld"/> (the leg's drag holds a decoration) and
    /// <see cref="ShowsReading"/> (the reading view comes to rest). n = 2: the
    /// stamp hint plate, shown as the counter's bounce shows it. Every shown
    /// thing is due in the leg's report (<see cref="LegLines"/>), seen or not.
    /// An unknown key or step throws, so a typo cannot drop an expectation in
    /// silence.
    /// </summary>
    public static List<string> ShownBy(int n, string target)
    {
        if (n == 2)
            return new List<string> { StampHint };
        if (n != 1)
            throw new InvalidOperationException($"camchrome: a step with n = {n} shows nothing (1 starts a leg, 2 shows the stamp hint, 0 ends the leg).");
        var keys = new List<string>();
        foreach (string key in (target ?? string.Empty).Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (key != ShowsHeld && key != ShowsReading && Array.FindIndex(Plates, p => p.key == key) < 0)
                throw new InvalidOperationException($"camchrome: a leg cannot show '{key}' (the plates: {string.Join(", ", Plates.Select(p => p.key))}; or {ShowsHeld}, {ShowsReading}).");
            keys.Add(key);
        }
        return keys;
    }

    /// <summary>What a chrome watch counted over its leg (the input of <see cref="LegLines"/>).</summary>
    public struct ChromeTally
    {
        /// <summary>Frames watched.</summary>
        public int Frames;

        /// <summary>Frames with something held (HoldRegistry.Desk) that the watch placed on screen (<see cref="HeldMember"/>).</summary>
        public int HoldingFrames;

        /// <summary>Frames with something held that the watch could not place on screen (no Transform or GameObject to read, or off the screen).</summary>
        public int UnplacedFrames;

        /// <summary>The type of the first held thing the watch could not place (null: none).</summary>
        public string Unplaced;

        /// <summary>Frames with the reading view on and at rest.</summary>
        public int ReadingFrames;

        /// <summary>Frames with CAM's own chrome over the held thing or the framed work.</summary>
        public int Over;

        /// <summary>CAM's controls not found when the watch began, comma-separated (empty: none): each makes the leg's line red.</summary>
        public string Missing;

        /// <summary>The screen the leg ran at ("1920x1080").</summary>
        public string ScreenSize;

        /// <summary>The first frames CAM's chrome covered the work (at most 12).</summary>
        public IReadOnlyList<string> Violations;
    }

    /// <summary>What a chrome watch saw of one plate it found when it began: frames drawn, frames over the work and their notes.</summary>
    public readonly struct PlateSeen
    {
        /// <summary>The plate's key (<see cref="Plates"/>).</summary>
        public readonly string Key;

        /// <summary>Frames the plate was drawn.</summary>
        public readonly int Shown;

        /// <summary>Of those, frames over the held thing or the framed work.</summary>
        public readonly int Over;

        /// <summary>The first frames it covered the work (at most 12).</summary>
        public readonly IReadOnlyList<string> Notes;

        /// <summary>The plate <paramref name="key"/>, drawn <paramref name="shown"/> frames, <paramref name="over"/> of them over the work.</summary>
        public PlateSeen(string key, int shown, int over, IReadOnlyList<string> notes)
        {
            Key = key;
            Shown = shown;
            Over = over;
            Notes = notes ?? Array.Empty<string>();
        }
    }

    /// <summary>One line of a chrome leg's report: its id in the scenario, its owner, its verdict and its texts.</summary>
    public readonly struct ChromeLine
    {
        /// <summary>The assertion's id under the scenario (<c>chrome.&lt;leg&gt;</c>, <c>chrome.&lt;leg&gt;.&lt;key&gt;</c>, <c>plate.&lt;leg&gt;.&lt;key&gt;</c>).</summary>
        public readonly string Id;

        /// <summary>Whose line it is: CAM's for its own chrome and for a plate it could not measure, the plate's owner for a drawn plate.</summary>
        public readonly string Owner;

        /// <summary>The verdict.</summary>
        public readonly bool Pass;

        /// <summary>The measured text.</summary>
        public readonly string Measured;

        /// <summary>What was checked, and what failed.</summary>
        public readonly string Detail;

        /// <summary>A line <paramref name="id"/> of <paramref name="owner"/>.</summary>
        public ChromeLine(string id, string owner, bool pass, string measured, string detail)
        {
            Id = id;
            Owner = owner;
            Pass = pass;
            Measured = measured;
            Detail = detail;
        }
    }

    /// <summary>
    /// A chrome leg's report (pure; camchrome n = 0), from what the leg
    /// <paramref name="shows"/> (<see cref="ShownBy"/> of its steps), the
    /// watch's <paramref name="tally"/> and each plate it found
    /// (<paramref name="seen"/>; a plate missing from it was not found).
    /// <list type="bullet">
    /// <item><c>chrome.&lt;leg&gt;</c>, CAM's: no tab, Back control or city tab
    /// drawn over a held thing or the reading view's framed work, at least one
    /// frame watched, every one of CAM's controls found (a renamed or unbuilt
    /// control is never watched, so it cannot pass by absence), and each half
    /// the leg shows seen: a held thing placed on screen on at least one frame
    /// (<see cref="ShowsHeld"/>; a hold the watch could not place is named as
    /// such, not blamed on the drag), the reading view at rest on at least one
    /// (<see cref="ShowsReading"/>), so neither half passes untested.</item>
    /// <item><c>plate.&lt;leg&gt;.&lt;key&gt;</c>, the plate's owner's, for each
    /// plate drawn in the leg: red while it covered the work.</item>
    /// <item><c>chrome.&lt;leg&gt;.&lt;key&gt;</c>, CAM's red "not measured"
    /// line, for each plate the leg shows that was drawn 0 frames or not found:
    /// silence would let the plate's gate pass by absence.</item>
    /// </list>
    /// A plate neither drawn nor shown reports nothing.
    /// </summary>
    public static List<ChromeLine> LegLines(string leg, ICollection<string> shows, ChromeTally tally, IEnumerable<PlateSeen> seen)
    {
        var lines = new List<ChromeLine>();
        List<PlateSeen> plates = (seen ?? Enumerable.Empty<PlateSeen>()).ToList();
        var unseen = new List<string>();
        if (tally.Frames == 0)
            unseen.Add("no frame was watched");
        if (!string.IsNullOrEmpty(tally.Missing))
            unseen.Add($"CAM's control(s) {tally.Missing} not found when the watch began (renamed, or not built: Build Office UI), so 'no chrome over the work' went untested for them");
        if (shows.Contains(ShowsHeld) && tally.HoldingFrames == 0)
            unseen.Add(tally.UnplacedFrames > 0
                ? $"something was held on {tally.UnplacedFrames} frames ({tally.Unplaced}) but the watch could not place it on screen (no Transform or GameObject to read on it, or off the screen), so 'no chrome over the held thing' went untested"
                : "the leg's drag held nothing on any frame (a blocked press, or the decoration not draggable at this aspect), so 'no chrome over the held thing' went untested");
        if (shows.Contains(ShowsReading) && tally.ReadingFrames == 0)
            unseen.Add("the reading view never came to rest, so 'no chrome over its framed work' went untested");
        string drawn = string.Join(", ", plates.Select(p => $"{PathOf(p.Key)} shown {p.Shown} frames"));
        string why = string.Join("; ", unseen.Concat(tally.Violations ?? Array.Empty<string>()));
        string unplaced = tally.UnplacedFrames > 0 ? $", {tally.UnplacedFrames} held but not placed ({tally.Unplaced})" : "";
        lines.Add(new ChromeLine($"chrome.{leg}", Owner, tally.Over == 0 && unseen.Count == 0,
                                 $"{tally.Frames} frames watched ({tally.HoldingFrames} holding{unplaced}, {tally.ReadingFrames} in the reading view), {tally.Over} with chrome over the work, {tally.ScreenSize}" +
                                 (string.IsNullOrEmpty(tally.Missing) ? "" : $"; not found: {tally.Missing}") + (drawn.Length > 0 ? $"; plates: {drawn}" : ""),
                                 why.Length > 0 ? why : "no tab, Back control or city tab drawn over a held thing or the reading view's framed work (BUG-59)"));
        foreach ((string path, string key, string owner) in Plates)
        {
            int at = plates.FindIndex(p => p.Key == key);
            if (at >= 0 && plates[at].Shown > 0)
            {
                PlateSeen p = plates[at];
                lines.Add(new ChromeLine($"plate.{leg}.{key}", owner, p.Over == 0, $"{path} shown {p.Shown} frames, {p.Over} over the work",
                                         p.Notes.Count > 0 ? string.Join("; ", p.Notes) + $" (expected red until {owner} moves it: CAM.md's hand-offs)"
                                                           : "the plate never covers a held thing or the reading view's framed work (BUG-59)"));
            }
            else if (shows.Contains(key))
            {
                lines.Add(new ChromeLine($"chrome.{leg}.{key}", Owner, false, at >= 0 ? $"not measured: {path} shown 0 frames" : $"not measured: {path} not found",
                                         $"the leg showed the plate ({(key == StampHint ? "camchrome n = 2, as the counter's bounce does" : "camchrome n = 1's target")}) and it was drawn 0 frames, so nothing was measured: a red line, never silence (DeskStampTray hides its hint while no passport is in play; the guide's plate shows on day 1). If {owner} now hides the plate where the leg shows it, show it where it does show, or take the expectation out of the scenario"));
            }
        }
        return lines;
    }

    /// <summary>The overlay path of the plate <paramref name="key"/>.</summary>
    private static string PathOf(string key) => Plates.First(p => p.key == key).path;

    private static IEnumerator Chrome(QaRunner runner, QaStep step)
    {
        string name = Name(step, "chrome");
        if (step.n > 0)
        {
            List<string> shows = ShownBy(step.n, step.target);
            if (step.n == 1)
            {
                if (_chrome != null)
                    _chrome.Running = false;
                var watch = new ChromeWatch { Running = true };
                foreach (string control in ChromeNames)
                    Add(runner, watch, control, null);
                foreach ((string path, string key, _) in Plates)
                    Add(runner, watch, path, key);
                _chrome = watch;
                runner.StartCoroutine(Watch(watch));
            }
            else if (_chrome == null || !_chrome.Running)
            {
                throw new InvalidOperationException("camchrome n = 2 shows the stamp hint to a running watch: start one first (n = 1).");
            }
            // What this step makes the leg show is due in the leg's report, seen or not (LegLines).
            _chrome.Shows.UnionWith(shows);
            if (step.n == 2)
            {
                // The stamp hint plate shown as the game shows it (DeskStampTray.Note, the counter's bounce).
                if (QaGame.Find<DeskStampTray>() is DeskStampTray stamps)
                    stamps.Note(BounceNote);
                yield return QaGame.Frames(2);
            }
            yield break;
        }
        ChromeWatch done = _chrome;
        _chrome = null;
        foreach (string control in ChromeNames)
            runner.Recorder.Forget("alpha:" + control);
        foreach ((string path, _, _) in Plates)
            runner.Recorder.Forget("alpha:" + path);
        if (done == null)
        {
            runner.Assert("chrome." + name, Owner, false, "not measured", "no camchrome watch was started (n = 1)");
            yield break;
        }
        done.Running = false;
        var tally = new ChromeTally
        {
            Frames = done.Frames,
            HoldingFrames = done.HoldingFrames,
            UnplacedFrames = done.UnplacedFrames,
            Unplaced = done.Unplaced,
            ReadingFrames = done.ReadingFrames,
            Over = done.Over,
            Missing = string.Join(", ", ChromeNames.Where(c => done.Controls.All(w => w.Path != c))),
            ScreenSize = $"{Screen.width}x{Screen.height}",
            Violations = done.Violations,
        };
        foreach (ChromeLine line in LegLines(name, done.Shows, tally, done.Controls.Where(w => w.Key != null).Select(w => new PlateSeen(w.Key, w.Shown, w.Over, w.Notes))))
            runner.Assert(line.Id, line.Owner, line.Pass, line.Measured, line.Detail);
    }

    /// <summary>Watches the overlay object at <paramref name="path"/> (skipped when not loaded), its alpha in the recorder's rows.</summary>
    private static void Add(QaRunner runner, ChromeWatch watch, string path, string key)
    {
        GameObject go = TryAt(path);
        if (go == null)
            return;
        var w = new Watched { Path = path, Key = key, Go = go, Group = Group(go.transform) };
        watch.Controls.Add(w);
        runner.Recorder.Column("alpha:" + path, () => AlphaOf(w.Go, w.Group).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <summary>A loaded object by its path or name (QaGame.GameObjectAt), or by a path under any object named its first part ("StampBar/Tab": the overlay's StampBar's Tab), or null.</summary>
    private static GameObject TryAt(string pathOrName)
    {
        try
        {
            return QaGame.GameObjectAt(pathOrName);
        }
        catch (InvalidOperationException)
        {
            int slash = pathOrName.IndexOf('/');
            if (slash <= 0)
                return null;
            string first = pathOrName.Substring(0, slash), rest = pathOrName.Substring(slash + 1);
            foreach (Transform t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
                if (t.name == first && t.Find(rest) is Transform found)
                    return found.gameObject;
            return null;
        }
    }

    private static float AlphaOf(GameObject go, CanvasGroup group) => go == null || !go.activeInHierarchy ? 0f : group != null ? group.alpha : 1f;

    /// <summary>The member that places each holdable type on screen (<see cref="HeldMember"/>), read once per type.</summary>
    private static readonly Dictionary<Type, MemberInfo> HeldMembers = new Dictionary<Type, MemberInfo>();

    /// <summary>
    /// How the chrome watch finds a held thing that is no Component on screen,
    /// by its type (pure): its first instance field or property of a Transform
    /// or GameObject (a dragged decoration registers DeskDecor's plain Item,
    /// whose Root carries its parts), else its first of another Component (a
    /// Transform comes first: an owner component is the holder, not the held
    /// thing). Null for a Component (it is placed by itself: the Rolodex) and
    /// for a type with no such member (the leg then names the hold it could
    /// not place, <see cref="LegLines"/>).
    /// </summary>
    public static MemberInfo HeldMember(Type holdable)
    {
        if (holdable == null || typeof(Component).IsAssignableFrom(holdable) || typeof(GameObject).IsAssignableFrom(holdable))
            return null;
        const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var members = new List<(MemberInfo member, Type type)>();
        for (Type t = holdable; t != null && t != typeof(object); t = t.BaseType)
        {
            // An auto-property's backing field is read through the property itself.
            members.AddRange(t.GetFields(Instance | BindingFlags.DeclaredOnly).Where(f => !f.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false))
                              .Select(f => ((MemberInfo)f, f.FieldType)));
            members.AddRange(t.GetProperties(Instance | BindingFlags.DeclaredOnly).Where(p => p.CanRead && p.GetIndexParameters().Length == 0).Select(p => ((MemberInfo)p, p.PropertyType)));
        }
        bool Places(Type type) => typeof(Transform).IsAssignableFrom(type) || typeof(GameObject).IsAssignableFrom(type);
        return members.FirstOrDefault(m => Places(m.type)).member ?? members.FirstOrDefault(m => typeof(Component).IsAssignableFrom(m.type)).member;
    }

    /// <summary>The object of the held thing <paramref name="held"/> the watch measures on screen: a Component's own, else the one its <see cref="HeldMember"/> holds now (null when none).</summary>
    private static GameObject HeldObject(IHoldable held)
    {
        if (held is Component component)
            return component != null ? component.gameObject : null;
        Type type = held.GetType();
        if (!HeldMembers.TryGetValue(type, out MemberInfo member))
            HeldMembers[type] = member = HeldMember(type);
        object value = member is FieldInfo f ? f.GetValue(held) : member is PropertyInfo p ? p.GetValue(held) : null;
        return value is GameObject go ? go : value is Component c && c != null ? c.gameObject : null;
    }

    /// <summary>Each frame's end: every shown chrome control's and plate's screen rect against the held thing's and, in the reading view, the framed work's.</summary>
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
            if (HoldRegistry.Desk.Current is IHoldable holding)
            {
                // A dragged decoration registers DeskDecor's plain Item, no Component: placed by its Root (HeldMember).
                if (HeldObject(holding) is GameObject held && QaGame.ScreenRect(held) is Rect heldRect)
                {
                    watch.HoldingFrames++;
                    work.Add(("the held " + held.name, heldRect));
                }
                else
                {
                    watch.UnplacedFrames++;
                    watch.Unplaced = watch.Unplaced ?? holding.GetType().Name;
                }
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
            foreach (Watched w in watch.Controls)
            {
                if (AlphaOf(w.Go, w.Group) <= 0f || !(QaGame.ScreenRect(w.Go) is Rect rect))
                    continue;
                w.Shown++;
                bool covers = false;
                foreach ((string what, Rect area) in work)
                    if (rect.Overlaps(area))
                    {
                        covers = true;
                        List<string> notes = w.Key == null ? watch.Violations : w.Notes;
                        if (notes.Count < 12)
                            notes.Add($"frame {Time.frameCount}: {w.Path} over {what}");
                    }
                if (!covers)
                    continue;
                w.Over++;
                over |= w.Key == null;
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

    /// <summary>
    /// A running tilt count (camtilts n = 1 to n = 0). It counts the views'
    /// own turns (DeskView.Changed, CityView.Changed, the PC's ViewChanged),
    /// never the camera tags: the push-in that follows a tilt re-tags itself
    /// with the tilt's cause about every 0.2 s while it settles
    /// (CameraFeel.TickMotion), so one drop tilt is several DeskDropTilt tags.
    /// A paper landing with the reading view off and the desk taking input
    /// (DeskView's toggle live) expects the D1 tilt in the same frame
    /// (DeskController.Read tilts right after PaperExamined); a landing with
    /// the view on expects none.
    /// </summary>
    private sealed class TiltCount
    {
        public DeskController Desk;
        public DeskView View;
        public CityView City;
        public OfficeViewController Office;
        public int Landings, Expected, Met, DropTilts, ViewKeys, Untagged, WhileHolding;
        public readonly List<string> Notes = new List<string>();

        /// <summary>The landings still waiting for their tilt: the frame, the paper and whether something was held then.</summary>
        public readonly List<(int frame, int paper, bool holding)> Pending = new List<(int, int, bool)>();

        public void Note(string text)
        {
            if (Notes.Count < 8)
                Notes.Add(text);
        }

        public void Landed(int paper)
        {
            Landings++;
            if (View == null || View.IsOn || !(QaGame.Field(View, "_toggleLive") is bool live) || !live)
                return;
            Expected++;
            Pending.Add((Time.frameCount, paper, HoldRegistry.Desk.IsHolding));
        }

        public void ViewChanged()
        {
            Moved(View.IsOn ? "the reading view's tilt in" : "the reading view's return");
            if (!View.IsOn)
            {
                ViewKeys++;
                return;
            }
            switch (View.LastCause)
            {
                case CameraMoveCause.DeskDropTilt:
                    DropTilts++;
                    int i = Pending.FindIndex(p => p.frame == Time.frameCount);
                    if (i >= 0)
                    {
                        Met++;
                        Pending.RemoveAt(i);
                    }
                    else
                    {
                        Note($"a drop tilt at frame {Time.frameCount} with no paper landing that frame");
                    }
                    break;
                case CameraMoveCause.Untagged:
                    Untagged++;
                    Note($"an untagged tilt at frame {Time.frameCount} (TAB or the guide's day-start page: STAMP's and CONTENT's hand-offs; the logger fails it)");
                    break;
                default:
                    ViewKeys++;
                    break;
            }
        }

        public void CityChanged()
        {
            ViewKeys++;
            Moved(City.IsOn ? "the turn to the city" : "the turn back from the city");
        }

        public void PcChanged(OfficeView view)
        {
            ViewKeys++;
            Moved(view == OfficeView.MonitorFocus ? "the PC opening" : "the PC closing");
        }

        private void Moved(string what)
        {
            if (!HoldRegistry.Desk.IsHolding)
                return;
            WhileHolding++;
            Note($"{what} at frame {Time.frameCount} while holding");
        }
    }

    private static TiltCount _tilts;

    private static IEnumerator Tilts(QaRunner runner, QaStep step)
    {
        string name = Name(step, "tilts");
        if (step.n > 0)
        {
            Stop(_tilts);
            var count = new TiltCount
            {
                Desk = QaGame.Find<DeskController>(),
                View = QaGame.Find<DeskView>(),
                City = QaGame.Find<CityView>(),
                Office = QaGame.Find<OfficeViewController>()
            };
            if (count.Desk != null)
                count.Desk.PaperExamined += count.Landed;
            if (count.View != null)
                count.View.Changed += count.ViewChanged;
            if (count.City != null)
                count.City.Changed += count.CityChanged;
            if (count.Office != null)
                count.Office.ViewChanged += count.PcChanged;
            _tilts = count;
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
        foreach ((int frame, int paper, bool holding) in done.Pending)
            done.Note($"paper {paper} landed at frame {frame} with the view off and no tilt followed" + (holding ? " (something was held then: DOC-HANDLE ends a paper's hold before the drop's tilt)" : ""));
        if (done.Expected == 0)
            done.Note("no landing found the view off and the desk live: the drop tilt was never played (camplay turns the view back before every other drop)");
        bool ok = done.Desk != null && done.View != null && done.Landings > 0 && done.Expected > 0 && done.Met == done.Expected && done.DropTilts == done.Met && done.WhileHolding == 0;
        runner.Assert("tilts." + name, Owner, ok,
                      $"{done.Landings} paper landings ({done.Expected} with the view off and live), {done.DropTilts} drop tilts ({done.Met} on those landings), {done.ViewKeys} view-key turns, {done.Untagged} untagged tilts, {done.WhileHolding} turns while holding",
                      done.Notes.Count > 0 ? string.Join("; ", done.Notes) : "each landing with the view off tilts in once, in its own frame; none with the view on, none without a landing, nothing turns while holding (v2's CAM pass at #6; the views' own turns, not the push-in's re-tags); 0 untagged moves is the run's camera assertion");
    }

    private static void Stop(TiltCount count)
    {
        if (count == null)
            return;
        if (count.Desk != null)
            count.Desk.PaperExamined -= count.Landed;
        if (count.View != null)
            count.View.Changed -= count.ViewChanged;
        if (count.City != null)
            count.City.Changed -= count.CityChanged;
        if (count.Office != null)
            count.Office.ViewChanged -= count.PcChanged;
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

    /// <summary>Waits (real time) until <paramref name="condition"/> holds or <paramref name="timeout"/> s pass, then tells <paramref name="held"/> which (no throw: camplay notes a miss and goes on).</summary>
    private static IEnumerator Wait(Func<bool> condition, float timeout, Action<bool> held)
    {
        float end = Time.realtimeSinceStartup + timeout;
        while (!condition())
        {
            if (Time.realtimeSinceStartup > end)
            {
                held(false);
                yield break;
            }
            yield return null;
        }
        held(true);
    }

    /// <summary>The count of a private collection field of <paramref name="target"/> (0 without it): DeskController's <c>_inReader</c> (a HashSet), TravellerWheel's <c>_said</c> (a List).</summary>
    private static int CountOf(object target, string field) => target != null ? Count(QaGame.Field(target, field)) : 0;

    /// <summary>
    /// The number of items in <paramref name="collection"/> (0 for null or
    /// anything not enumerable). A <c>HashSet&lt;T&gt;</c> is no non-generic
    /// <see cref="ICollection"/> (a List is), so it is counted by walking it:
    /// read as an ICollection, the chip reader's set always counted 0 and the
    /// chip read's "came back out" wait was only <c>!reader.Busy</c>.
    /// </summary>
    public static int Count(object collection)
    {
        if (collection is ICollection c)
            return c.Count;
        if (!(collection is IEnumerable items))
            return 0;
        int n = 0;
        for (IEnumerator e = items.GetEnumerator(); e.MoveNext();)
            n++;
        return n;
    }

    /// <summary>How one of camplay's chip read attempts ends (<see cref="ChipReadOf"/>).</summary>
    public enum ChipRead
    {
        /// <summary>The reader never began (a paper with no chip): it bounces back, the next paper is tried.</summary>
        Refused,

        /// <summary>The reader began but its paper never came back out: not a read, and the reads end (while the reader holds a paper DeskController refuses every next one, <c>_inReader</c>).</summary>
        Stuck,

        /// <summary>The reader finished and its paper is out again: one read.</summary>
        Read,
    }

    /// <summary>
    /// A chip read attempt's end (pure): <paramref name="began"/> (the reader
    /// went busy) and <paramref name="cameOut"/> (it finished and its paper
    /// left the reader's set). A read counts only once its paper is out, so a
    /// read that never comes out is no read and ends the reads (play.&lt;name&gt;
    /// red: reads &lt; f), and the note and the counts agree.
    /// </summary>
    public static ChipRead ChipReadOf(bool began, bool cameOut) => !began ? ChipRead.Refused : cameOut ? ChipRead.Read : ChipRead.Stuck;

    /// <summary>The traveller wheel's ring pills that take a click now (its RadialLayoutGroup's children; "&lt; Back" in the centre slot is not one).</summary>
    private static List<Button> RingPills(TravellerWheel wheel)
    {
        var pills = new List<Button>();
        if (wheel == null || !(QaGame.Field(wheel, "layout") is Component layout))
            return pills;
        foreach (Transform child in layout.transform)
            if (child.gameObject.activeInHierarchy && child.TryGetComponent(out Button b) && b.interactable)
                pills.Add(b);
        return pills;
    }

    /// <summary>
    /// The scripted day 12 at the desk (v1's scripted day minus the loupe,
    /// FIX_PLAN_v2's CAM delta) with the virtual mouse: every counter paper
    /// dropped on the mat (every other one from the office view, the view
    /// turned back first as a player does, so the D1 drop tilt plays on a
    /// landing with the view off and none on a landing with it on); a click
    /// on the scanner's Auto-Feed tray and its stack scanned; n scans on the
    /// scanner's bed; f chip reads on the reader (a paper it refuses bounces
    /// back and the next is tried; the one it reads is read again; a read
    /// counts once its paper came back out, and one that never does ends the
    /// reads); the
    /// intercom's wheel opened, a question asked on its ring (a submenu
    /// opened on the way) and answered, then Esc. A home the reading view
    /// shows too little of is reached from the office view (Back, a view
    /// key). The Rolodex's right-click is the one known gap (QaInput has no
    /// right button: QA-RIG's hand-off).
    /// </summary>
    private static IEnumerator Play(QaRunner runner, QaStep step)
    {
        QaInput input = QaInput.Current;
        if (input == null || !DeskLayout.Bound)
            throw new InvalidOperationException("camplay needs the virtual mouse and a bound desk.");
        var notes = new List<string>();
        yield return QaGame.Until(() => Papers(DeskZone.Counter).Count > 0, 10f, "papers on the counter");
        DeskView view = QaGame.Find<DeskView>();
        DeskController desk = QaGame.Find<DeskController>();
        DeskScanner scanner = QaGame.Find<DeskScanner>();

        // Back to the office view (a view key), as a player turns: the drop tilt's start, and a home the reading view shows too little of.
        IEnumerator Office()
        {
            if (view != null && view.IsOn)
            {
                view.Return();
                yield return new WaitForSecondsRealtime(1f);
            }
        }

        IEnumerator Reach(Vector3 world)
        {
            if (ScreenOf(world) == null)
                yield return Office();
        }

        // Every counter paper dropped on the mat, each at its own spot.
        if (view != null && view.IsOn)
            notes.Add("the reading view was on before the first drop (a tilt at the day's start; turned back)");
        DeskRect mat = DeskLayout.Mat.Rect;
        int drops = 0;
        for (int guard = 0; guard < 12; guard++)
        {
            List<DeskDocument> counter = Papers(DeskZone.Counter);
            if (counter.Count == 0)
                break;
            if (drops % 2 == 0)
                yield return Office();
            (float x, float y) spot = mat.PointAt(0.3f + 0.13f * (drops % 4), 0.35f + 0.15f * (drops / 4 % 2));
            Vector3 onMat = DeskLayout.Frame.Point(new Vector2(spot.x, spot.y));
            Vector2? from = QaGame.ScreenPoint(counter[0].gameObject), to = ScreenOf(onMat);
            if ((from == null || to == null) && view != null && view.IsOn)
            {
                yield return Office(); // the reading view shows too little of the counter: turned back, as a player does
                from = QaGame.ScreenPoint(counter[0].gameObject);
                to = ScreenOf(onMat);
            }
            if (from == null || to == null)
            {
                notes.Add($"paper {counter[0].Index} or its spot is off the screen");
                break;
            }
            DeskDocument dropped = counter[0];
            yield return DragAt(input, from.Value, to.Value);
            drops++;
            yield return new WaitForSecondsRealtime(0.8f); // the tilt's blend and the paper's slide settle
            if (dropped != null && dropped.Zone == DeskZone.Counter && notes.Count < 12)
                notes.Add($"paper {dropped.Index} stayed on the counter after its drop");
        }
        int left = Papers(DeskZone.Counter).Count;

        int scanned = 0;
        void Finished(int paper, ScanPass pass) => scanned++;
        if (desk != null)
            desk.ScanFinished += Finished;
        int fed = 0, scans = 0, scanTarget = step.n > 0 ? step.n : 10;
        int reads = 0, refused = 0, readTarget = step.f > 0f ? Mathf.RoundToInt(step.f) : 5;
        bool wheelOpened = false, asked = false;
        try
        {
            // The Auto-Feed: a click on the scanner's tray feeds every paper not scanned yet through it, one after another.
            ScannerAddOns addOns = QaGame.Find<ScannerAddOns>();
            Component tray = addOns != null ? QaGame.Field(addOns, "trayClick") as Component : null;
            DeskPapers papers = desk != null ? QaGame.Field(desk, "_state") as DeskPapers : null;
            if (scanner == null || tray == null || !tray.gameObject.activeInHierarchy)
            {
                notes.Add("no Auto-Feed tray on the scanner (the upgrade owned: GiveAllItems)");
            }
            else
            {
                yield return Reach(tray.transform.position);
                Vector2? at = QaGame.ScreenPoint(tray.gameObject);
                if (at == null)
                {
                    notes.Add("the Auto-Feed tray is off the screen");
                }
                else
                {
                    bool clicked = false;
                    void Tray() => clicked = true;
                    scanner.TrayClicked += Tray;
                    int before = scanned;
                    yield return input.Play(QaPaths.Click(at.Value));
                    yield return QaGame.Frames(3);
                    scanner.TrayClicked -= Tray;
                    bool emptied = false;
                    yield return Wait(() => papers == null || (!papers.ScannerBusy && papers.QueuedCount == 0), 60f, ok => emptied = ok);
                    fed = scanned - before;
                    if (!clicked)
                        notes.Add("the click on the Auto-Feed tray did not reach it");
                    if (!emptied)
                        notes.Add("the Auto-Feed's stack was still scanning after 60 s");
                    yield return new WaitForSecondsRealtime(0.8f); // the last paper slides back
                }
            }

            // Scans: a desk paper onto the scanner's bed, each scan finished before the next.
            for (int i = 0; scanner != null && scanner.OnDesk && i < scanTarget; i++)
            {
                List<DeskDocument> onDesk = Papers(DeskZone.Desk);
                if (onDesk.Count == 0)
                {
                    notes.Add("no paper lies on the desk to scan");
                    break;
                }
                DeskDocument paper = onDesk[i % onDesk.Count];
                yield return Reach(scanner.BedPoint);
                Vector2? from = QaGame.ScreenPoint(paper.gameObject), to = ScreenOf(scanner.BedPoint);
                if (from == null || to == null)
                {
                    notes.Add("the scanner's bed is off the screen");
                    break;
                }
                int before = scanned;
                yield return DragAt(input, from.Value, to.Value);
                bool done = false;
                yield return Wait(() => scanned > before, 12f, ok => done = ok);
                if (done)
                    scans++;
                else if (notes.Count < 12)
                    notes.Add($"scan {i + 1} never finished (the drop missed the bed?)");
                yield return new WaitForSecondsRealtime(0.8f); // the scanned paper slides back
            }

            // Chip reads: a desk paper onto the reader; one it refuses (no chip) bounces back and the next is tried, the one it reads is read again.
            ChipReader reader = QaGame.Find<ChipReader>();
            int card = -1;
            var tried = new HashSet<int>();
            for (int attempt = 0; reader != null && reader.OnDesk && reads < readTarget && attempt < readTarget + 8; attempt++)
            {
                List<DeskDocument> onDesk = Papers(DeskZone.Desk);
                DeskDocument paper = card >= 0 ? onDesk.FirstOrDefault(p => p.Index == card) : onDesk.FirstOrDefault(p => !tried.Contains(p.Index));
                if (paper == null)
                {
                    if (card >= 0)
                    {
                        yield return new WaitForSecondsRealtime(0.5f); // the read paper is still on its way back
                        continue;
                    }
                    notes.Add("no paper on the desk the chip reader takes");
                    break;
                }
                tried.Add(paper.Index);
                yield return Reach(reader.DockPoint(MachineDock.Pad));
                Vector2? from = QaGame.ScreenPoint(paper.gameObject), to = ScreenOf(reader.DockPoint(MachineDock.Pad));
                if (from == null || to == null)
                {
                    notes.Add("the chip reader is off the screen");
                    break;
                }
                yield return DragAt(input, from.Value, to.Value);
                bool began = false, back = false;
                yield return Wait(() => reader.Busy, 4f, ok => began = ok);
                if (began)
                    yield return Wait(() => !reader.Busy && CountOf(desk, "_inReader") == 0, 12f, ok => back = ok);
                ChipRead read = ChipReadOf(began, back);
                if (read == ChipRead.Refused)
                {
                    refused++;
                    yield return new WaitForSecondsRealtime(1f); // it bounces back to where it was picked up
                    continue;
                }
                if (read == ChipRead.Stuck)
                {
                    notes.Add($"chip read {reads + 1} never came back out (paper {paper.Index} still in the reader after 12 s)");
                    break;
                }
                reads++;
                card = reader.Document >= 0 ? reader.Document : paper.Index;
                yield return new WaitForSecondsRealtime(0.8f);
            }

            // The intercom: its wheel opens in the view the player is in (no camera move); a question asked on its ring and answered (the bubble says a line), then Esc.
            TravellerWheel wheel = QaGame.Find<TravellerWheel>();
            if (wheel == null || !DeskLayout.TryPose(DeskHomeMap.Intercom, out Vector3 intercom, out _))
            {
                notes.Add("no traveller wheel or intercom");
            }
            else
            {
                yield return Reach(intercom);
                int said = CountOf(wheel, "_said"), closedSilent = 0;
                for (int attempt = 0; attempt < 5 && !asked; attempt++)
                {
                    if (!wheel.IsOpen)
                    {
                        if (!(ScreenOf(intercom) is Vector2 button))
                        {
                            notes.Add("the intercom is off the screen");
                            break;
                        }
                        yield return input.Play(QaPaths.Click(button));
                        yield return new WaitForSecondsRealtime(0.8f); // the pills fly out
                        wheelOpened |= wheel.IsOpen;
                        if (!wheel.IsOpen)
                        {
                            notes.Add("the intercom's click opened no wheel");
                            break;
                        }
                    }
                    List<Button> pills = RingPills(wheel);
                    if (pills.Count == 0)
                    {
                        notes.Add("the wheel's ring shows no question");
                        break;
                    }
                    Button pill = pills[Mathf.Min(closedSilent, pills.Count - 1)];
                    if (!(QaGame.ScreenPoint(pill.gameObject) is Vector2 p))
                    {
                        notes.Add("a ring pill is off the screen");
                        break;
                    }
                    yield return input.Play(QaPaths.Click(p));
                    yield return new WaitForSecondsRealtime(0.8f);
                    asked = CountOf(wheel, "_said") > said;
                    if (!asked && !wheel.IsOpen)
                        closedSilent++; // a look or a paper asked for: the wheel closed with nothing said; the next pill on the next opening
                }
                if (wheel.IsOpen)
                {
                    yield return input.Tap(Key.Escape);
                    yield return new WaitForSecondsRealtime(0.5f);
                }
            }
        }
        finally
        {
            if (desk != null)
                desk.ScanFinished -= Finished;
        }

        bool ran = drops > 0 && left == 0 && fed > 0 && scans == scanTarget && reads == readTarget && wheelOpened && asked;
        runner.Assert("play." + Name(step, "desk"), Owner, ran,
                      $"{drops} drops ({left} left on the counter), the Auto-Feed scanned {fed}, {scans} of {scanTarget} scans, {reads} of {readTarget} chip reads ({refused} papers refused), the wheel {(wheelOpened ? "opened" : "not opened")}, a question {(asked ? "answered" : "not answered")}",
                      (notes.Count > 0 ? string.Join("; ", notes) + "; " : "") +
                      "the scripted day 12 ran whole: every counter paper dropped, the Auto-Feed's stack scanned, each scan and chip read finished, a ring question answered (its camera moves: the run's camera assertion and camtilts); the Rolodex's right-click is the one known gap (QaInput has no right button: QA-RIG)");
    }

    // ------------------------------------------------------------------ camcap

    private static IEnumerator Cap(QaRunner runner, QaStep step)
    {
        yield return null;
        string name = Name(step, "reading");
        Camera cam = Camera.main;
        DeskView view = QaGame.Find<DeskView>();
        float floor = Screen.height >= 1000 ? 10f : 7f;
        if (cam == null || view == null || !view.IsOn)
        {
            runner.Assert("capreport." + name, Owner, false, "not measured",
                          cam == null || view == null ? "no camera or reading view" : "not in the reading view: the report is at CAM's reading pose (DeskView.Toggle does nothing unless its toggle is live)");
            yield break;
        }
        var caps = new List<(float px, string what)>();
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
            runner.Assert("capreport." + name, Owner, false, "not measured", "no printed text on a desk paper");
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
