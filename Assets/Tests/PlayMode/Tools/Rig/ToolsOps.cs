#if UNITY_EDITOR || TIMEDESK_QA
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;

/// <summary>
/// TOOLS' scenario ops (FIX_PLAN TOOLS and its v2 delta; docs/pending/TOOLS.md
/// gives the recipes). Each measures what the player sees, not the code's own
/// numbers (BUG-53: no circular probe):
/// <list type="bullet">
/// <item><c>toolsTorch</c>: drags the UV torch along a P-* path and, on every
/// held frame, projects the torch's own meshes to the screen and counts the
/// lit patch's points inside their outline (the reveal ≥95% unoccluded,
/// BUG-2c), and checks the pool lies on the desk plane inside the desk's
/// corners whenever it is drawn (0 frames off the surface).</item>
/// <item><c>toolsClicks</c>: n tries at the target's visual centre (jittered
/// ±3 px), each asking the event system what a click there would press: the
/// target every time (BUG-4b: the torch's box no longer swallows the chip
/// reader's clicks).</item>
/// <item><c>toolsLens</c>: sweeps the pointer from (x, y) to (x2, y2) over f
/// seconds and back; every frame, every text the Translation Lens shows
/// translated must be drawn, not the PC's, and under the pointer (0 phantom
/// translations, BUG-52).</item>
/// <item><c>toolsRevert</c>: hovers the target's centre (or (x, y)) until a
/// word there is translated, moves the pointer to (x2, y2) in one frame and
/// times the flip back (≤ TranslationLens.RevertSeconds).</item>
/// <item><c>toolsMicro</c>: the scan view's microprint text, measured as
/// drawn (MicroprintView.CapPx ≥ ScanMicroprint.MinCapPx, BUG-2b; @#13).</item>
/// </list>
/// </summary>
public static class ToolsOps
{
    private const string Owner = "TOOLS";

    /// <summary>The patch's samples for the occlusion count: its centre and four rings.</summary>
    private static readonly (float ring, int count)[] PatchRings = { (0.25f, 8), (0.5f, 12), (0.75f, 16), (1f, 24) };

    /// <summary>Registers the ops before the first scene loads (editor and QA player alike).</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        QaRunner.RegisterOp("toolsTorch", Torch);
        QaRunner.RegisterOp("toolsClicks", Clicks);
        QaRunner.RegisterOp("toolsLens", Lens);
        QaRunner.RegisterOp("toolsRevert", Revert);
        QaRunner.RegisterOp("toolsMicro", Micro);
    }

    private static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);

    private static string Name(QaStep s, string fallback) => string.IsNullOrEmpty(s.name) ? fallback : s.name;

    /// <summary>The torch drag: target (default the UV torch), path, to target2 or (x2, y2) as screen fractions.</summary>
    private static IEnumerator Torch(QaRunner runner, QaStep s)
    {
        UvTorch torch = UnityEngine.Object.FindObjectsByType<UvTorch>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).FirstOrDefault();
        if (torch == null)
            throw new InvalidOperationException("No UV torch on the desk (it comes on day 7, Feature.UvTorch).");
        DeskHardware hardware = torch.GetComponentInParent<DeskHardware>();
        DeskSurface surface = UnityEngine.Object.FindAnyObjectByType<DeskSurface>(FindObjectsInactive.Exclude);
        float radius = hardware != null && hardware.Config != null ? hardware.Config.uvConeRadius : 0.075f;
        Transform pool = torch.transform.Find("UvPool");
        GameObject start = string.IsNullOrEmpty(s.target) ? torch.gameObject : QaGame.GameObjectAt(s.target);
        Vector2 from = QaGame.ScreenPoint(start) ?? throw new InvalidOperationException("The torch is not on the screen.");
        Vector2 to = string.IsNullOrEmpty(s.target2) ? new Vector2(s.x2 * Screen.width, s.y2 * Screen.height)
                                                     : QaGame.ScreenPoint(QaGame.GameObjectAt(s.target2)) ?? throw new InvalidOperationException($"{s.target2} is not on the screen.");
        QaPath path = QaPaths.Drag(QaRunner.ParseKind(s.path), from, to, new Vector2(Screen.width, Screen.height));
        string name = Name(s, "torch");

        int held = 0, under = 0, poolFrames = 0, offDesk = 0;
        float worst = 1f;
        string worstAt = string.Empty;
        int restore = Time.captureFramerate;
        Time.captureFramerate = SeqRecorder.Fps;
        runner.Recorder.Column("torchShare", () => torch.Held ? F(Share(torch, radius)) : string.Empty);
        try
        {
            yield return runner.Input.Play(path, i =>
            {
                if (!torch.Held)
                    return;
                held++;
                float share = Share(torch, radius);
                if (share < UvTorchHold.UnoccludedShare)
                    under++;
                if (share < worst)
                {
                    worst = share;
                    worstAt = $"frame {Time.frameCount} at {F(runner.Input.Position.x)},{F(runner.Input.Position.y)}";
                }
                if (pool != null && pool.gameObject.activeInHierarchy)
                {
                    poolFrames++;
                    if (!OnDesk(surface, pool.position))
                        offDesk++;
                }
            });
        }
        finally
        {
            Time.captureFramerate = restore;
        }
        runner.Assert($"tools.torch.{name}.unoccluded", Owner, held > 0 && under == 0,
            $"held {held} frames; worst {F(worst * 100f)}% of the lit patch seen past the body ({worstAt}); {under} frames under {F(UvTorchHold.UnoccludedShare * 100f)}%",
            "BUG-2c: the reveal is ≥95% unoccluded by the torch body on every held frame (its own meshes projected, the patch sampled on 61 points)");
        runner.Assert($"tools.torch.{name}.poolOnDesk", Owner, held > 0 && offDesk == 0, $"pool drawn {poolFrames} frames, {offDesk} off the desk",
            "BUG-2c: 0 frames of the pool drawn off the surface (on the desk plane, inside its corners)");
    }

    /// <summary>The share of the lit patch round torch.Aim the main camera sees outside the outline of the torch's own meshes (the beam left out).</summary>
    private static float Share(UvTorch torch, float radius)
    {
        Camera cam = Camera.main;
        if (cam == null)
            return 1f;
        List<Vector2> outline = Hull(BodyPoints(torch, cam));
        Vector3 centre = torch.Aim;
        int total = 0, hidden = 0;
        foreach (Vector3 p in PatchPoints(centre, radius))
        {
            Vector3 sp = cam.WorldToScreenPoint(p);
            total++;
            if (sp.z > 0f && Inside(outline, sp))
                hidden++; // the body is above the desk, so always nearer the camera than the patch it outlines
        }
        return total == 0 ? 1f : 1f - hidden / (float)total;
    }

    private static IEnumerable<Vector3> PatchPoints(Vector3 centre, float radius)
    {
        yield return centre;
        foreach ((float ring, int count) in PatchRings)
            for (int k = 0; k < count; k++)
            {
                float a = 2f * Mathf.PI * k / count;
                yield return centre + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (radius * ring);
            }
    }

    /// <summary>The torch's body on the screen (the art's UvTorch under the host, the one that lifts out of the cradle): its meshes' vertices (their boxes' corners for a mesh the player cannot read), the beam's cone left out.</summary>
    private static List<Vector2> BodyPoints(UvTorch torch, Camera cam)
    {
        var points = new List<Vector2>();
        Transform body = torch.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t != torch.transform && t.name == "UvTorch");
        if (body == null)
            return points;
        foreach (MeshFilter f in body.GetComponentsInChildren<MeshFilter>(false))
        {
            if (f.sharedMesh == null || IsUnder(f.transform, "BeamFX"))
                continue;
            Renderer r = f.GetComponent<Renderer>();
            if (r == null || !r.enabled)
                continue;
            IEnumerable<Vector3> local = f.sharedMesh.isReadable ? f.sharedMesh.vertices : Corners(f.sharedMesh.bounds);
            foreach (Vector3 v in local)
            {
                Vector3 sp = cam.WorldToScreenPoint(f.transform.TransformPoint(v));
                if (sp.z > 0f)
                    points.Add(sp);
            }
        }
        return points;
    }

    private static bool IsUnder(Transform t, string name)
    {
        for (Transform w = t; w != null; w = w.parent)
            if (w.name == name)
                return true;
        return false;
    }

    private static IEnumerable<Vector3> Corners(Bounds b)
    {
        for (int i = 0; i < 8; i++)
            yield return new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
    }

    /// <summary>The convex outline of the points (monotone chain), counter-clockwise.</summary>
    private static List<Vector2> Hull(List<Vector2> points)
    {
        var p = points.Distinct().OrderBy(v => v.x).ThenBy(v => v.y).ToList();
        if (p.Count < 3)
            return p;
        var hull = new List<Vector2>();
        foreach (Vector2 v in p)
        {
            while (hull.Count >= 2 && Cross(hull[hull.Count - 2], hull[hull.Count - 1], v) <= 0f)
                hull.RemoveAt(hull.Count - 1);
            hull.Add(v);
        }
        int lower = hull.Count + 1;
        for (int i = p.Count - 2; i >= 0; i--)
        {
            while (hull.Count >= lower && Cross(hull[hull.Count - 2], hull[hull.Count - 1], p[i]) <= 0f)
                hull.RemoveAt(hull.Count - 1);
            hull.Add(p[i]);
        }
        hull.RemoveAt(hull.Count - 1);
        return hull;
    }

    private static float Cross(Vector2 a, Vector2 b, Vector2 c) => (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);

    private static bool Inside(List<Vector2> hull, Vector2 p)
    {
        if (hull.Count < 3)
            return false;
        for (int i = 0; i < hull.Count; i++)
            if (Cross(hull[i], hull[(i + 1) % hull.Count], p) < 0f)
                return false;
        return true;
    }

    /// <summary>True when a pool centre lies on the desk: within 2 mm above the desk plane's pool height and inside the desk's four corners (seen from above).</summary>
    private static bool OnDesk(DeskSurface surface, Vector3 at)
    {
        if (surface == null)
            return false;
        Vector3[] c = surface.Corners();
        float lift = at.y - surface.transform.position.y;
        if (lift < 0f || lift > 0.02f)
            return false;
        float minX = c.Min(v => v.x), maxX = c.Max(v => v.x), minZ = c.Min(v => v.z), maxZ = c.Max(v => v.z);
        return at.x >= minX - 1e-3f && at.x <= maxX + 1e-3f && at.z >= minZ - 1e-3f && at.z <= maxZ + 1e-3f;
    }

    /// <summary>n tries (default 10) at the target's visual centre, jittered ±3 px: each must press the target (a part of it), never another object.</summary>
    private static IEnumerator Clicks(QaRunner runner, QaStep s)
    {
        GameObject target = QaGame.GameObjectAt(s.target);
        Vector2 centre = QaGame.ScreenPoint(target) ?? throw new InvalidOperationException($"{s.target} is not on the screen.");
        int tries = s.n > 0 ? s.n : 10, hits = 0;
        var wrong = new List<string>();
        for (int i = 0; i < tries; i++)
        {
            float a = 2f * Mathf.PI * i / tries;
            Vector2 at = centre + (i == 0 ? Vector2.zero : new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 3f);
            yield return runner.Input.MoveTo(at, 3);
            yield return null;
            GameObject top = QaGame.TopAt(at);
            if (top != null && top.transform.IsChildOf(target.transform))
                hits++;
            else
                wrong.Add(top != null ? QaGame.Where(top) : "nothing");
        }
        runner.Assert($"tools.clicks.{Name(s, target.name)}", Owner, hits == tries, $"{hits}/{tries} on {target.name}",
            wrong.Count == 0 ? "BUG-4b: every press lands on the target" : "pressed instead: " + string.Join(", ", wrong.Distinct()));
    }

    /// <summary>Sweeps (x, y) → (x2, y2) → (x, y) over f seconds (default 3; screen fractions); every frame every translated text must be drawn, not the PC's, and under the pointer (within 24 px of its rect).</summary>
    private static IEnumerator Lens(QaRunner runner, QaStep s)
    {
        TranslationLensPresenter lens = TranslationLensPresenter.Instance;
        if (lens == null)
            throw new InvalidOperationException("No Translation Lens presenter.");
        var shown = new List<TMP_Text>();
        int frames = 0, translated = 0, pc = 0, phantom = 0;
        var names = new HashSet<string>();
        float seconds = s.f > 0f ? s.f : 3f, begin = Time.unscaledTime, end = begin + seconds;
        Vector2 a = new Vector2(s.x * Screen.width, s.y * Screen.height), b = new Vector2(s.x2 * Screen.width, s.y2 * Screen.height);
        while (Time.unscaledTime < end)
        {
            float phase = (Time.unscaledTime - begin) / seconds;
            runner.Input.Set(Vector2.Lerp(a, b, 1f - Mathf.Abs(2f * phase - 1f)), false);
            yield return null;
            frames++;
            lens.Translated(shown);
            foreach (TMP_Text t in shown)
            {
                translated++;
                if (UiText.SurfaceOf(t) == TextSurface.Pc)
                {
                    pc++;
                    names.Add("PC " + QaGame.Where(t.gameObject));
                    continue;
                }
                Rect? rect = QaGame.ScreenRect(t.gameObject);
                bool drawn = t.isActiveAndEnabled && (!(t is TextMeshPro desk) || desk.renderer.isVisible);
                // At the Object level the whole hovered object turns, far from the pointer too: only drawn is asked there.
                bool near = TranslationLensPresenter.CurrentReach == LensReach.Object || (rect != null && Grown(rect.Value, 24f).Contains(runner.Input.Position));
                if (!drawn || rect == null || !near)
                {
                    phantom++;
                    names.Add(QaGame.Where(t.gameObject));
                }
            }
        }
        string name = Name(s, "lens");
        runner.Assert($"tools.lens.{name}.phantoms", Owner, phantom == 0, $"{frames} frames, {translated} translated text-frames, {phantom} not under the pointer or not drawn",
            phantom == 0 ? "BUG-52: the log of translated texts is within the visible ones" : string.Join("; ", names));
        runner.Assert($"tools.lens.{name}.pc", Owner, pc == 0, $"{pc} PC text-frames translated", "FIX_PLAN_v2 TOOLS: 0 PC texts in the candidate log (D7)");
    }

    private static Rect Grown(Rect r, float by) => Rect.MinMaxRect(r.xMin - by, r.yMin - by, r.xMax + by, r.yMax + by);

    /// <summary>From (x, y) (a word to hover, screen fractions): waits until something is translated, moves to (x2, y2) in one frame and times how long until nothing is.</summary>
    private static IEnumerator Revert(QaRunner runner, QaStep s)
    {
        TranslationLensPresenter lens = TranslationLensPresenter.Instance;
        if (lens == null)
            throw new InvalidOperationException("No Translation Lens presenter.");
        var shown = new List<TMP_Text>();
        Vector2 start = string.IsNullOrEmpty(s.target) ? new Vector2(s.x * Screen.width, s.y * Screen.height)
                                                       : QaGame.ScreenPoint(QaGame.GameObjectAt(s.target)) ?? throw new InvalidOperationException($"{s.target} is not on the screen.");
        yield return runner.Input.MoveTo(start, 6);
        float wait = Time.unscaledTime + 2f;
        do
        {
            yield return null;
            lens.Translated(shown);
        }
        while (shown.Count == 0 && Time.unscaledTime < wait);
        string name = Name(s, "revert");
        if (shown.Count == 0)
        {
            runner.Assert($"tools.lens.{name}.revert", Owner, false, "nothing translated under the start point", "hover a foreign word first (the lens's day, a planted paper)");
            yield break;
        }
        yield return new WaitForSecondsRealtime(0.5f); // landed in English
        runner.Input.Set(new Vector2(s.x2 * Screen.width, s.y2 * Screen.height), false);
        float left = Time.unscaledTime, limit = left + 2f;
        do
        {
            yield return null;
            lens.Translated(shown, true);
        }
        while (shown.Count > 0 && Time.unscaledTime < limit);
        float seconds = Time.unscaledTime - left;
        runner.Assert($"tools.lens.{name}.revert", Owner, shown.Count == 0 && seconds <= TranslationLens.RevertSeconds + 1f / SeqRecorder.Fps,
            $"back in {F(seconds)} s", $"BUG-52: revert ≤{F(TranslationLens.RevertSeconds)} s after leaving (one frame's slack)");
    }

    /// <summary>The scan view's microprint text (target): its capitals as drawn reach ScanMicroprint.MinCapPx; with text "credited", the paper (n) also reads as read (DeskReadings.MicroRead).</summary>
    private static IEnumerator Micro(QaRunner runner, QaStep s)
    {
        yield return null;
        TMP_Text text = QaGame.GameObjectAt(s.target).GetComponent<TMP_Text>();
        if (text == null)
            throw new InvalidOperationException($"{s.target} has no text.");
        float cap = MicroprintView.CapPx(text);
        string name = Name(s, "micro");
        runner.Assert($"tools.micro.{name}.cap", Owner, cap >= ScanMicroprint.MinCapPx, $"cap {F(cap)} px at {Screen.width}x{Screen.height} ('{text.text}')",
            $"BUG-2b: the microprint's capitals ≥{F(ScanMicroprint.MinCapPx)} px in the scan view at A2");
        if (s.text == "credited")
        {
            DeskHardware hardware = QaGame.Find<DeskHardware>();
            bool read = hardware != null && hardware.Readings.MicroRead(s.n);
            runner.Assert($"tools.micro.{name}.credited", Owner, read, $"paper {s.n} read: {read}", "BUG-2b: the read is credited once the layer rendered the line");
        }
    }
}
#endif
