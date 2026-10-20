#if UNITY_EDITOR || TIMEDESK_QA
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// CAM's QA rig ops (docs/pending/QA-RIG.md §4 "Tracks' own ops", §8's CAM
/// recipe; docs/pending/CAM.md). Each asserts one thing the player sees:
/// <list type="bullet">
/// <item><c>camframe</c> (name; f = the least share, 0.98 when 0): in the
/// reading view, every point the reading pose frames (DeskViewPose.Targets:
/// the mat, the side homes' inner strips, the deal tray) is on the screen
/// through the live camera (<c>frame.&lt;name&gt;</c>, BUG-1b/7b/41), and the
/// screen's bottom row meets the desk at or past its front edge
/// (<c>frame.&lt;name&gt;.bottom</c>: under the desk never shows); in the office
/// view, the mat's corners and every shown home's centre are on the screen.</item>
/// <item><c>camfade</c> (target "reading" or "city"; name): starts the move
/// by its view key, then samples the destination's control (the "▲ Back"
/// control; the "Desk ▶" tab) every frame, and on the way back the office's
/// (the PC tab; "◀ City"), and asserts <c>fade.&lt;name&gt;</c>: 0 on frame
/// 0 and until 2/3 of the move, 1 by its end (BUG-32b; a cut under Reduced
/// Motion: 1 at once).</item>
/// <item><c>camhold</c> (name): holds a stand-in on the desk's hold
/// registry, presses every view change (the mat, the drop tilt, the
/// return, the city and back, the PC) and asserts <c>hold.&lt;name&gt;</c>:
/// no view changed (BUG-55c); then lets go.</item>
/// <item><c>camparallax</c> (name; n = frames, 90 when 0): in the city view,
/// sweeps the pointer edge to edge and back and asserts
/// <c>city.parallax.&lt;name&gt;</c>: the nearest layer moves at most 1 px at
/// 1080p a frame (BUG-69), and <c>city.nobars.&lt;name&gt;</c>: the panorama
/// covers the whole screen (BUG-42); then clicks the city and asserts
/// <c>city.click.&lt;name&gt;</c>: the view turned back to the desk.</item>
/// <item><c>cammotion</c> (n = 1 Reduced Motion, 0 full): sets the motion
/// choice as the Settings toggle does, for the Reduced Motion legs.</item>
/// </list>
/// Registered at load in the editor and at play start in the QA player.
/// </summary>
public static class CamOps
{
    private const string Owner = "CAM";

    /// <summary>The flying traffic's parallax against the painting's (CityView.TrafficDepth).</summary>
    private const float TrafficDepth = 1.25f;

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    private static void RegisterInEditor() => Register();
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        QaRunner.RegisterOp("camframe", Frame);
        QaRunner.RegisterOp("camfade", Fade);
        QaRunner.RegisterOp("camhold", Hold);
        QaRunner.RegisterOp("camparallax", Parallax);
        QaRunner.RegisterOp("cammotion", Motion);
    }

    /// <summary>Sets Reduced Motion (n = 1) or full motion (n = 0), as the Settings toggle does (the QA player keeps its own preferences; a scenario sets it back before it ends).</summary>
    private static IEnumerator Motion(QaRunner runner, QaStep step)
    {
        MotionPreference.Reduced = step.n > 0;
        yield return QaGame.Frames(2);
    }

    private static string Name(QaStep step, string fallback) => string.IsNullOrEmpty(step.name) ? fallback : step.name;

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
        var points = new List<Vector3>();
        if (view.IsOn)
        {
            foreach ((float x, float y, float z) in DeskViewPose.Targets(config.deskView, DeskLayout.Mat.Rect, config.layout.homes))
                points.Add(frame.Point(new Vector2(x, y), z));
        }
        else
        {
            DeskRect mat = DeskLayout.Mat.Rect;
            foreach (float u in new[] { 0f, 1f })
                foreach (float v in new[] { 0f, 1f })
                {
                    (float x, float y) = mat.PointAt(u, v);
                    points.Add(frame.Point(new Vector2(x, y)));
                }
            foreach (DeskHome home in config.layout.homes)
                if (home != null && home.show == HomeShow.Always)
                    points.Add(frame.Point(new Vector2(home.x, home.y), home.height / 2f));
        }
        int inside = 0;
        var outside = new List<string>();
        foreach (Vector3 p in points)
        {
            Vector3 vp = cam.WorldToViewportPoint(p);
            if (vp.z > 0f && vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f)
                inside++;
            else if (outside.Count < 8)
                outside.Add($"({frame.Local(p).x:0.00}, {frame.Local(p).y:0.00}) at ({vp.x:0.00}, {vp.y:0.00})");
        }
        float share = points.Count > 0 ? (float)inside / points.Count : 0f;
        float least = step.f > 0f ? step.f : 0.98f;
        runner.Assert("frame." + name, Owner, share >= least, $"{inside}/{points.Count} on screen ({share:P0}), view {(view.IsOn ? "reading" : "office")}, {Screen.width}x{Screen.height}",
                      outside.Count > 0 ? "off screen: " + string.Join("; ", outside) : "every framed point on screen");
        if (!view.IsOn)
            yield break;
        Ray bottom = cam.ViewportPointToRay(new Vector3(0.5f, 0f, 0f));
        float front = config.layout.deskTop.Rect.CentreY - config.layout.deskTop.Rect.Height / 2f;
        bool hits = bottom.direction.y < -1e-5f;
        float rowY = hits ? frame.Local(bottom.GetPoint((frame.Origin.y - bottom.origin.y) / bottom.direction.y)).y : float.NegativeInfinity;
        runner.Assert("frame." + name + ".bottom", Owner, hits && rowY >= front - 0.005f, $"bottom row meets the desk at y {rowY:0.000} m (front edge {front:0.000})",
                      "the reading view's bottom row stays on the desk's top (under the desk never shows)");
    }

    private static IEnumerator Fade(QaRunner runner, QaStep step)
    {
        string name = Name(step, step.target);
        bool city = string.Equals(step.target, "city", System.StringComparison.OrdinalIgnoreCase);
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
        runner.Assert("fade." + name, Owner, failures.Count == 0, $"{frames} frames sampled over {seconds:0.00} s moves",
                      failures.Count > 0 ? string.Join("; ", failures) : "0 until 2/3 of each move, 1 by its end");
    }

    /// <summary>Samples <paramref name="group"/>'s alpha each frame of a move of <paramref name="seconds"/> (plus a settle), noting each breach of the 2/3 rule.</summary>
    private static IEnumerator Sample(CanvasGroup group, float seconds, string leg, List<string> failures, System.Action<int> count)
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
        MotionPreference.Reduced || !(QaGame.Field(view, "config") is DeskConfigSO c) ? 0f : CityLookTimeline.Total(c.citySeconds, c.cityFadeFrom, c.cityFadeSeconds);

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
        HoldRegistry.Desk.Begin(held);
        bool deskOn = desk != null && desk.IsOn, cityOn = city != null && city.IsOn;
        OfficeView view = office != null ? office.Current : OfficeView.OfficeFocus;
        if (desk != null)
        {
            desk.Toggle();
            desk.TiltIn();
            desk.Return();
        }
        if (city != null)
        {
            city.Look();
            city.Return();
        }
        if (office != null)
            office.FocusMonitor();
        yield return QaGame.Frames(2);
        bool same = (desk == null || desk.IsOn == deskOn) && (city == null || city.IsOn == cityOn) && (office == null || office.Current == view);
        HoldRegistry.Desk.End(held);
        runner.Assert("hold." + name, Owner, same, same ? "no view changed" : $"reading {deskOn}->{desk?.IsOn}, city {cityOn}->{city?.IsOn}, PC {view}->{office?.Current}",
                      "the mat, the drop tilt, Return, the city and the PC are refused while something is held (BUG-55c)");
    }

    private static IEnumerator Parallax(QaRunner runner, QaStep step)
    {
        string name = Name(step, "city");
        CityView city = QaGame.Find<CityView>();
        RawImage panorama = QaGame.GameObjectAt("Panorama") is GameObject go ? go.GetComponent<RawImage>() : null;
        if (city == null || !city.IsOn || panorama == null || QaInput.Current == null)
        {
            runner.Assert("city.parallax." + name, Owner, false, "not measured", "the city view is not on (or no panorama, or no virtual pointer)");
            yield break;
        }
        int frames = step.n > 0 ? step.n : 90;
        float worst = 0f, last = panorama.uvRect.x;
        Canvas canvas = panorama.canvas;
        float scale = canvas != null ? canvas.rootCanvas.scaleFactor : 1f;
        for (int i = 0; i < frames; i++)
        {
            // Edge to edge, a jump at a time: the real mouse's worst case.
            float x = (i / 15) % 2 == 0 ? 2f : Screen.width - 2f;
            QaInput.Current.Set(new Vector2(x, Screen.height * 0.5f), false);
            yield return null;
            float u = panorama.uvRect.x, span = panorama.uvRect.width;
            float px = span > 0f ? Mathf.Abs(u - last) / span * panorama.rectTransform.rect.width * scale * (1080f / Screen.height) * TrafficDepth : 0f;
            worst = Mathf.Max(worst, px);
            last = u;
        }
        runner.Assert("city.parallax." + name, Owner, worst <= CityParallax.MaxPixelsPerFrame + 1e-3f, $"worst {worst:0.000} px/frame at 1080p over {frames} frames",
                      "the nearest layer (traffic at 1.25x the painting) moves at most 1 px a frame (BUG-69)");
        Rect r = panorama.rectTransform.rect;
        float w = r.width * scale, h = r.height * scale;
        runner.Assert("city.nobars." + name, Owner, w >= Screen.width - 1f && h >= Screen.height - 1f, $"panorama {w:0}x{h:0} on a {Screen.width}x{Screen.height} screen",
                      "the city fills the screen, never letterboxed (BUG-42)");

        // A left click on the city turns back to the desk (FIX_PLAN_v2's city exits).
        Vector2 centre = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        QaInput.Current.Set(centre, false);
        yield return null;
        QaInput.Current.Set(centre, true);
        yield return QaGame.Frames(2);
        QaInput.Current.Set(centre, false);
        yield return QaGame.Frames(2);
        runner.Assert("city.click." + name, Owner, !city.IsOn, city.IsOn ? "still looking at the city" : "turned back", "a click on the city returns to the desk");
        yield return QaGame.Until(() => !(QaGame.GameObjectAt("CityScreen") is GameObject s && s.activeInHierarchy), 5f, "the city to fade out");
    }
}
#endif
