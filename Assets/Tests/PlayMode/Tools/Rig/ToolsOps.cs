#if UNITY_EDITOR || TIMEDESK_QA
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

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
/// translations, BUG-52); the lens's candidate set is logged
/// (lens_candidates_*.txt) and holds no PC text, swept with the PC open too
/// (text "pc": D7, the set excludes the PC entirely); text "reach" asks that
/// a D7 surface (the newspaper, a document) was translated at all.</item>
/// <item><c>toolsRevert</c>: hovers the target's centre (or (x, y)) until a
/// word there is translated, moves the pointer to (x2, y2) in one frame and
/// times the flip back (≤ TranslationLens.RevertSeconds).</item>
/// <item><c>toolsMicro</c>: the scan view's microprint text: the game's
/// measure (MicroprintView.CapPx ≥ ScanMicroprint.MinCapPx) and, independent
/// of it, the line's ink in a screenshot, ≥ the same cap and across the
/// line's width (BUG-2b; @#13).</item>
/// </list>
/// </summary>
public static class ToolsOps
{
    private const string Owner = "TOOLS";

    /// <summary>The patch's samples for the occlusion count: its centre and four rings.</summary>
    private static readonly (float ring, int count)[] PatchRings = { (0.25f, 8), (0.5f, 12), (0.75f, 16), (1f, 24) };

    /// <summary>Registers the ops before the first scene loads (editor and QA player alike), and in the editor also on every domain load, so the edit-mode scenario test (QaEditorGateTests) knows them without a play run. Registering twice just replaces the same bodies.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
#endif
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

        int held = 0, under = 0, unmeasured = 0, poolFrames = 0, offDesk = 0;
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
                if (float.IsNaN(share))
                {
                    unmeasured++; // no body outline: the frame proves nothing, so it fails
                    return;
                }
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
        runner.Assert($"tools.torch.{name}.unoccluded", Owner, held > 0 && under == 0 && unmeasured == 0,
            $"held {held} frames; worst {F(worst * 100f)}% of the lit patch seen past the body ({worstAt}); {under} frames under {F(UvTorchHold.UnoccludedShare * 100f)}%; {unmeasured} frames with no body outline; body points: {PointSources(torch)}",
            "BUG-2c: the reveal is ≥95% unoccluded by the torch body on every held frame (its own meshes projected, the patch sampled on 61 points)");
        runner.Assert($"tools.torch.{name}.poolOnDesk", Owner, held > 0 && offDesk == 0, $"pool drawn {poolFrames} frames, {offDesk} off the desk",
            "BUG-2c: 0 frames of the pool drawn off the surface (on the desk plane, inside its corners)");
    }

    /// <summary>The share of the lit patch round torch.Aim the main camera sees outside the outline of the torch's own meshes (the beam left out); NaN when there is no camera or no outline to measure against (never a lenient 1).</summary>
    private static float Share(UvTorch torch, float radius)
    {
        Camera cam = Camera.main;
        if (cam == null)
            return float.NaN;
        List<Vector2> outline = Hull(BodyPoints(torch, cam));
        if (outline.Count < 3)
            return float.NaN;
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

    /// <summary>The torch's body on the screen (the art's UvTorch under the host, the one that lifts out of the cradle): its meshes' vertices (<see cref="LocalPoints"/>), the beam's cone left out.</summary>
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
            foreach (Vector3 v in LocalPoints(f.sharedMesh))
            {
                Vector3 sp = cam.WorldToScreenPoint(f.transform.TransformPoint(v));
                if (sp.z > 0f)
                    points.Add(sp);
            }
        }
        return points;
    }

    /// <summary>Each mesh's own-space points, read once per mesh.</summary>
    private static readonly Dictionary<Mesh, Vector3[]> MeshPoints = new Dictionary<Mesh, Vector3[]>();

    /// <summary>Where each mesh's points came from: "vertices", "gpu" or "box".</summary>
    private static readonly Dictionary<Mesh, string> MeshSource = new Dictionary<Mesh, string>();

    /// <summary>
    /// A mesh's points in its own space. A readable mesh gives Mesh.vertices.
    /// The torch's art (UvTorch.fbx) is imported without Read/Write, and play
    /// mode refuses its CPU vertex data even in the editor (Mesh.vertices logs
    /// an error and returns none; Mesh.AcquireReadOnlyMeshData throws), so its
    /// positions are read back from the GPU vertex buffer the renderer draws
    /// (<see cref="GpuPositions"/>). Only when that fails does it fall back to
    /// the mesh's box corners (a larger outline, so the measure errs strict).
    /// </summary>
    private static Vector3[] LocalPoints(Mesh mesh)
    {
        if (MeshPoints.TryGetValue(mesh, out Vector3[] cached))
            return cached;
        Vector3[] points = mesh.isReadable ? mesh.vertices : GpuPositions(mesh);
        string source = mesh.isReadable ? "vertices" : "gpu";
        if (points == null || points.Length == 0)
        {
            points = Corners(mesh.bounds).ToArray();
            source = "box";
        }
        MeshPoints[mesh] = points;
        MeshSource[mesh] = source;
        return points;
    }

    /// <summary>
    /// The mesh's vertex positions read back from its GPU vertex buffer (float3
    /// positions only), or null. The read-back is trusted only when its points
    /// span the mesh's own bounds (within 1 mm on every side), so a wrong
    /// layout or an empty buffer can never shrink the outline.
    /// </summary>
    private static Vector3[] GpuPositions(Mesh mesh)
    {
        if (!mesh.HasVertexAttribute(VertexAttribute.Position) || mesh.GetVertexAttributeFormat(VertexAttribute.Position) != VertexAttributeFormat.Float32
            || mesh.GetVertexAttributeDimension(VertexAttribute.Position) < 3)
            return null;
        int stream = mesh.GetVertexAttributeStream(VertexAttribute.Position);
        int offset = mesh.GetVertexAttributeOffset(VertexAttribute.Position);
        int stride = mesh.GetVertexBufferStride(stream);
        int count = mesh.vertexCount;
        using (GraphicsBuffer buffer = mesh.GetVertexBuffer(stream))
        {
            if (buffer == null || count == 0 || (long)buffer.count * buffer.stride < (long)count * stride)
                return null;
            var raw = new byte[buffer.count * buffer.stride];
            buffer.GetData(raw);
            var points = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                int at = i * stride + offset;
                points[i] = new Vector3(BitConverter.ToSingle(raw, at), BitConverter.ToSingle(raw, at + 4), BitConverter.ToSingle(raw, at + 8));
            }
            Vector3 min = points[0], max = points[0];
            foreach (Vector3 v in points)
            {
                min = Vector3.Min(min, v);
                max = Vector3.Max(max, v);
            }
            Bounds b = mesh.bounds;
            bool spans = (min - b.min).magnitude <= 1e-3f && (max - b.max).magnitude <= 1e-3f;
            return spans ? points : null;
        }
    }

    /// <summary>Where each body mesh's points came from ("vertices", "gpu" or "box") and how many, for the measured line.</summary>
    private static string PointSources(UvTorch torch)
    {
        Transform body = torch.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t != torch.transform && t.name == "UvTorch");
        if (body == null)
            return "no body";
        return string.Join(", ", body.GetComponentsInChildren<MeshFilter>(false)
            .Where(f => f.sharedMesh != null && !IsUnder(f.transform, "BeamFX") && MeshSource.ContainsKey(f.sharedMesh))
            .Select(f => $"{f.sharedMesh.name} {MeshSource[f.sharedMesh]} {MeshPoints[f.sharedMesh].Length}"));
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

    /// <summary>
    /// Sweeps (x, y) → (x2, y2) → (x, y) over f seconds (default 3; screen
    /// fractions). Every frame: every translated text must be drawn, not the
    /// PC's, and under the pointer (within 24 px of its rect); and the lens's
    /// candidate set (TranslationLensPresenter.Candidates, every text it was
    /// allowed to read there) is logged and must hold no PC text. With text
    /// "pc" the PC is open and the sweep runs over its windows: the pointer
    /// must have been over the PC's desktop (its layer the top UI hit) on
    /// some frame, else the 0 proves nothing. With text "reach" the sweep is
    /// over a surface that shows the world's language (the newspaper, a
    /// document): something must have been translated on it.
    /// </summary>
    private static IEnumerator Lens(QaRunner runner, QaStep s)
    {
        TranslationLensPresenter lens = TranslationLensPresenter.Instance;
        if (lens == null)
            throw new InvalidOperationException("No Translation Lens presenter.");
        var shown = new List<TMP_Text>();
        var candidates = new List<TMP_Text>();
        int frames = 0, translated = 0, pc = 0, phantom = 0, candidateFrames = 0, pcCandidates = 0, overPc = 0;
        var names = new HashSet<string>();
        var pcNames = new HashSet<string>();
        var log = new Dictionary<string, int>();
        float seconds = s.f > 0f ? s.f : 3f, begin = Time.unscaledTime, end = begin + seconds;
        Vector2 a = new Vector2(s.x * Screen.width, s.y * Screen.height), b = new Vector2(s.x2 * Screen.width, s.y2 * Screen.height);
        while (Time.unscaledTime < end)
        {
            float phase = (Time.unscaledTime - begin) / seconds;
            runner.Input.Set(Vector2.Lerp(a, b, 1f - Mathf.Abs(2f * phase - 1f)), false);
            yield return null;
            frames++;
            GameObject top = QaGame.TopAt(runner.Input.Position);
            if (top != null && UiText.SurfaceOf(top.transform) == TextSurface.Pc)
                overPc++;
            lens.Candidates(candidates);
            foreach (TMP_Text t in candidates)
            {
                candidateFrames++;
                string where = (UiText.SurfaceOf(t) == TextSurface.Pc ? "PC   " : "desk ") + QaGame.Where(t.gameObject);
                log[where] = log.TryGetValue(where, out int seen) ? seen + 1 : 1;
                if (UiText.SurfaceOf(t) == TextSurface.Pc)
                {
                    pcCandidates++;
                    pcNames.Add("candidate " + QaGame.Where(t.gameObject));
                }
            }
            lens.Translated(shown);
            foreach (TMP_Text t in shown)
            {
                translated++;
                if (UiText.SurfaceOf(t) == TextSurface.Pc)
                {
                    pc++;
                    pcNames.Add("translated " + QaGame.Where(t.gameObject));
                    continue;
                }
                Rect? rect = TextRect(t);
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
        // The candidate log: every text the lens was allowed to read during the sweep, its surface and its frames.
        string logFile = $"lens_candidates_{QaRunner.Safe(name)}.txt";
        File.WriteAllText(Path.Combine(runner.OutDir, logFile),
            $"# tools.lens.{name}: the Translation Lens's candidate set, {frames} frames, {candidateFrames} candidate text-frames; surface, text, frames\n"
            + string.Concat(log.OrderBy(e => e.Key, StringComparer.Ordinal).Select(e => $"{e.Key}\t{e.Value}\n")));
        runner.Assert($"tools.lens.{name}.phantoms", Owner, phantom == 0, $"{frames} frames, {translated} translated text-frames, {phantom} not under the pointer or not drawn",
            phantom == 0 ? "BUG-52: the log of translated texts is within the visible ones" : string.Join("; ", names));
        runner.Assert($"tools.lens.{name}.pc", Owner, pc == 0 && pcCandidates == 0,
            $"{pcCandidates} PC text-frames in the candidate log ({logFile}: {candidateFrames} candidate text-frames, {log.Count} texts), {pc} translated; pointer over the PC {overPc} frames",
            pcNames.Count == 0 ? "FIX_PLAN_v2 TOOLS: 0 PC texts in the candidate log (D7)" : string.Join("; ", pcNames));
        if (s.text == "pc")
            runner.Assert($"tools.lens.{name}.overPc", Owner, overPc > 0, $"pointer over the PC's desktop {overPc}/{frames} frames",
                "the PC open and under the sweep, so tools.lens.*.pc tests the candidate set's PC exclusion (open it with key Q first)");
        if (s.text == "reach")
            runner.Assert($"tools.lens.{name}.reach", Owner, translated > 0, $"{translated} translated text-frames, {log.Count} candidate texts",
                "D7 surfaces (the newspaper, the documents): the hover reaches their world-language words (a UI text needs a raycast-target graphic in its panel)");
    }

    private static Rect Grown(Rect r, float by) => Rect.MinMaxRect(r.xMin - by, r.yMin - by, r.xMax + by, r.yMax + by);

    /// <summary>
    /// A text's rect on the screen (pixels). A UI text: its RectTransform through
    /// its canvas (QaGame.ScreenRect). A desk word (a world TextMeshPro with no
    /// canvas, like a paper's printed title): its laid-out text bounds through
    /// the camera the office's pointer raycasts through. QaGame.ScreenRect
    /// projects a canvas-less RectTransform with no camera, which gives world
    /// units, not pixels, so every translated desk word would count as far from
    /// the pointer.
    /// </summary>
    private static Rect? TextRect(TMP_Text t)
    {
        if (!(t is TextMeshPro) || t.GetComponentInParent<Canvas>() != null)
            return QaGame.ScreenRect(t.gameObject);
        Camera cam = QaGame.PointerCamera();
        if (cam == null || !t.gameObject.activeInHierarchy)
            return null;
        Bounds b = t.textBounds;
        if (b.size.sqrMagnitude <= 0f)
            b = new Bounds(t.rectTransform.rect.center, t.rectTransform.rect.size);
        float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
        foreach (Vector3 corner in Corners(b))
        {
            Vector3 s = cam.WorldToScreenPoint(t.transform.TransformPoint(corner));
            if (s.z <= 0f)
                return null;
            x0 = Mathf.Min(x0, s.x);
            y0 = Mathf.Min(y0, s.y);
            x1 = Mathf.Max(x1, s.x);
            y1 = Mathf.Max(y1, s.y);
        }
        return Rect.MinMaxRect(x0, y0, x1, y1);
    }

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

    /// <summary>
    /// The scan view's microprint text (target), measured twice. The game's
    /// own measure (MicroprintView.CapPx, which the credit uses) and an
    /// independent one from the pixels (BUG-53: no self-referential probe): a
    /// screenshot's ink inside the text's screen rect, the rows that differ
    /// from the rect's border colour, whose height is the line's capitals
    /// (the microprint is capitals, digits and separators, no descender).
    /// Both must reach ScanMicroprint.MinCapPx, and the ink must span most of
    /// the line's laid-out width, so a clipped, off-screen or covered line
    /// fails here even if CapPx passed it. With text "credited", the paper (n)
    /// must also read as read (DeskReadings.MicroRead); with text
    /// "uncredited" it must not (the step before the layer is opened; the
    /// size checks are skipped there).
    /// </summary>
    private static IEnumerator Micro(QaRunner runner, QaStep s)
    {
        yield return null;
        TMP_Text text = QaGame.GameObjectAt(s.target).GetComponent<TMP_Text>();
        if (text == null)
            throw new InvalidOperationException($"{s.target} has no text.");
        float cap = MicroprintView.CapPx(text);
        string name = Name(s, "micro");
        if (s.text != "uncredited")
        {
            yield return new WaitForEndOfFrame();
            Ink ink = MeasureInk(text);
            runner.Assert($"tools.micro.{name}.cap", Owner, cap >= ScanMicroprint.MinCapPx, $"cap {F(cap)} px at {Screen.width}x{Screen.height} ('{text.text}')",
                $"BUG-2b: the microprint's capitals ≥{F(ScanMicroprint.MinCapPx)} px in the scan view at A2 (MicroprintView.CapPx: 0 when clipped, off screen or covered)");
            runner.Assert($"tools.micro.{name}.pixels", Owner, ink.Rows >= ScanMicroprint.MinCapPx && ink.WidthShare >= MinInkWidth,
                $"ink {ink.Rows} px tall, {F(ink.WidthShare * 100f)}% of the line's width, in the {ink.Rect.width:0}x{ink.Rect.height:0} px rect at {ink.Rect.x:0},{ink.Rect.y:0}",
                $"BUG-2b, independent of CapPx: the screenshot's ink is ≥{F(ScanMicroprint.MinCapPx)} px tall and spans ≥{F(MinInkWidth * 100f)}% of the line (a clipped, covered or off-screen line fails); the still is also read by eye");
        }
        if (s.text == "credited" || s.text == "uncredited")
        {
            DeskHardware hardware = QaGame.Find<DeskHardware>();
            bool read = hardware != null && hardware.Readings.MicroRead(s.n);
            bool want = s.text == "credited";
            runner.Assert($"tools.micro.{name}.{s.text}", Owner, read == want, $"paper {s.n} read: {read}",
                want ? "BUG-2b: the read is credited once the layer rendered the line" : "BUG-2b: no credit before the hidden layer is opened and drawn");
        }
    }

    /// <summary>The share of the line's laid-out width (its glyphs' screen span) the ink must cover.</summary>
    private const float MinInkWidth = 0.8f;

    /// <summary>A pixel is ink when its colour differs from the border's by this much (summed RGB, 0..3).</summary>
    private const float InkContrast = 0.3f;

    /// <summary>What a screenshot shows of a text: its screen rect (screenshot pixels), the height of its ink (screen pixels), and how much of its glyphs' width the ink covers.</summary>
    private struct Ink
    {
        public Rect Rect;
        public int Rows;
        public float WidthShare;
    }

    /// <summary>
    /// The text's ink in a screenshot taken now (call after WaitForEndOfFrame):
    /// inside its RectTransform's screen rect, a pixel is ink when it differs
    /// from the rect's median border colour by InkContrast; Rows is the span
    /// from the first to the last row with two ink pixels or more, WidthShare
    /// the ink's column span over the glyphs' laid-out span.
    /// </summary>
    private static Ink MeasureInk(TMP_Text text)
    {
        var ink = new Ink();
        Rect? found = QaGame.ScreenRect(text.gameObject);
        if (found == null)
            return ink;
        Texture2D shot = ScreenCapture.CaptureScreenshotAsTexture();
        try
        {
            float sx = shot.width / (float)Screen.width, sy = shot.height / (float)Screen.height;
            Rect r = found.Value;
            int x0 = Mathf.Clamp(Mathf.FloorToInt(r.xMin * sx), 0, shot.width - 1), x1 = Mathf.Clamp(Mathf.CeilToInt(r.xMax * sx), 0, shot.width - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(r.yMin * sy), 0, shot.height - 1), y1 = Mathf.Clamp(Mathf.CeilToInt(r.yMax * sy), 0, shot.height - 1);
            ink.Rect = Rect.MinMaxRect(x0, y0, x1, y1);
            if (x1 - x0 < 2 || y1 - y0 < 2)
                return ink;
            int w = x1 - x0 + 1, h = y1 - y0 + 1;
            Color[] px = shot.GetPixels(x0, y0, w, h);
            var border = new List<Color>();
            for (int x = 0; x < w; x++)
            {
                border.Add(px[x]);
                border.Add(px[(h - 1) * w + x]);
            }
            for (int y = 0; y < h; y++)
            {
                border.Add(px[y * w]);
                border.Add(px[y * w + w - 1]);
            }
            var bg = new Color(Median(border.Select(c => c.r)), Median(border.Select(c => c.g)), Median(border.Select(c => c.b)));
            int firstRow = -1, lastRow = -1, firstCol = int.MaxValue, lastCol = -1;
            for (int y = 0; y < h; y++)
            {
                int count = 0;
                for (int x = 0; x < w; x++)
                {
                    Color c = px[y * w + x];
                    if (Mathf.Abs(c.r - bg.r) + Mathf.Abs(c.g - bg.g) + Mathf.Abs(c.b - bg.b) < InkContrast)
                        continue;
                    count++;
                    firstCol = Mathf.Min(firstCol, x);
                    lastCol = Mathf.Max(lastCol, x);
                }
                if (count < 2)
                    continue;
                if (firstRow < 0)
                    firstRow = y;
                lastRow = y;
            }
            ink.Rows = firstRow < 0 ? 0 : Mathf.RoundToInt((lastRow - firstRow + 1) / sy);
            float span = GlyphSpan(text) * sx;
            ink.WidthShare = lastCol < 0 || span <= 0f ? 0f : (lastCol - firstCol + 1) / span;
        }
        finally
        {
            UnityEngine.Object.Destroy(shot);
        }
        return ink;
    }

    private static float Median(IEnumerable<float> values)
    {
        float[] v = values.OrderBy(x => x).ToArray();
        return v.Length == 0 ? 0f : v[v.Length / 2];
    }

    /// <summary>The screen width (pixels) from the first visible glyph's left edge to the last one's right edge, as laid out.</summary>
    private static float GlyphSpan(TMP_Text text)
    {
        Canvas canvas = text.canvas;
        Camera cam = canvas != null && canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.rootCanvas.worldCamera : null;
        float min = float.MaxValue, max = float.MinValue;
        TMP_TextInfo info = text.textInfo;
        for (int i = 0; info != null && i < info.characterCount; i++)
        {
            TMP_CharacterInfo c = info.characterInfo[i];
            if (!c.isVisible)
                continue;
            float a = RectTransformUtility.WorldToScreenPoint(cam, text.transform.TransformPoint(c.bottomLeft)).x;
            float b = RectTransformUtility.WorldToScreenPoint(cam, text.transform.TransformPoint(c.topRight)).x;
            min = Mathf.Min(min, a, b);
            max = Mathf.Max(max, a, b);
        }
        return max > min ? max - min : 0f;
    }
}
#endif
