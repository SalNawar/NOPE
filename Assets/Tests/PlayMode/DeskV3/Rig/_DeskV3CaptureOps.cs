#if UNITY_EDITOR || TIMEDESK_QA
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Local capture ops for the desk v3 review (never committed). v3still
/// (name): a full-resolution PNG into the report folder. v3dump (name): the
/// homes, the sliders' poses, the lit lamps and every renderer drawn over the
/// desk band that is not the painted desk. v3rec (name, n = fps): a
/// fixed-step capture of full-resolution JPGs with the virtual cursor drawn
/// in, and poses.tsv. v3moveto (target = a slider id or an object; n =
/// frames; y = a screen-height offset). v3stop. v3nobadge: from now on the
/// dev "CHEATS ON" badge is not drawn into any capture (round 10: the hide
/// lives here, in the local capture op, not in RUN-SAFE's DevToolsState;
/// V3NoBadge below, for the IMGUI badge of feat/desk-v3 and the uGUI
/// CheatBadge of integrate/a alike); v3still and v3rec turn it on too, and
/// v3still asserts that nothing would draw the badge.
/// </summary>
public static class _DeskV3CaptureOps
{
    public const string Out = @"E:\unity\NOPE-tools\reports\desk_v3_unity";

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    private static void RegisterInEditor() => Register();
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        QaRunner.RegisterOp("v3still", Still);
        QaRunner.RegisterOp("v3dump", Dump);
        QaRunner.RegisterOp("v3rec", Rec);
        QaRunner.RegisterOp("v3stop", Stop);
        QaRunner.RegisterOp("v3moveto", MoveTo);
        QaRunner.RegisterOp("v3at", At);
        QaRunner.RegisterOp("v3nobadge", NoBadge);
    }

    private static IEnumerator NoBadge(QaRunner runner, QaStep s)
    {
        V3NoBadge.Ensure().Hide();
        yield return null;
    }

    private static IEnumerator Still(QaRunner runner, QaStep s)
    {
        V3NoBadge hider = V3NoBadge.Ensure();
        hider.Hide();
        yield return null;
        string shown = hider.Shown();
        runner.Assert("v3.nobadge." + s.name, "DESK-HOMES", shown.Length == 0, shown.Length == 0 ? "no badge drawer on" : shown, "the dev badge must not land in a review capture");
        string path = Path.Combine(Out, s.name + ".png");
        runner.Recorder.Still(path);
        yield return QaGame.Until(() => !runner.Recorder.StillPending, 10f, "the still");
    }

    private static string V(Vector3 v) => string.Format(CultureInfo.InvariantCulture, "({0:0.000}, {1:0.000}, {2:0.000})", v.x, v.y, v.z);

    private static string PathOf(Transform t)
    {
        string p = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            p = t.name + "/" + p;
        }
        return p;
    }

    private static IEnumerator Dump(QaRunner runner, QaStep s)
    {
        yield return new WaitForEndOfFrame();
        var sb = new StringBuilder();
        Camera cam = QaGame.PointerCamera();
        sb.AppendLine($"day {QaGame.Day()} camera {(cam != null ? cam.name + " " + V(cam.transform.position) + " fov " + cam.fieldOfView.ToString("0.00", CultureInfo.InvariantCulture) : "none")} screen {Screen.width}x{Screen.height}");
        sb.AppendLine($"DeskLayout bound {DeskLayout.Bound} lamp lit '{DeskLayout.Lamps.Lit}'");
        foreach (HallLightingRig rig in Object.FindObjectsByType<HallLightingRig>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            HallBackdrop bd = rig.GetComponent<HallBackdrop>();
            AnimationCurve curve = rig.Settings != null ? rig.Settings.paintedDeskBrightness : null;
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "hall rig {0} hour {1:0.000} paintedDesk {2} knob {3:0.0000} backdrop active {4} brightness {5:0.0000}",
                PathOf(rig.transform), rig.Hour, rig.PaintedDesk, curve != null && curve.length > 0 ? curve.Evaluate(rig.Hour) : -1f, bd != null && bd.Active, bd != null ? bd.Brightness : -1f));
        }
        bool cheated = RunManager.HasInstance && RunManager.Instance.World != null && RunManager.Instance.World.cheated;
        sb.AppendLine($"run cheated {cheated}; badge drawers on: '{V3NoBadge.Ensure().Shown()}'");
        foreach (MachineSlideIn m in Object.FindObjectsByType<MachineSlideIn>(FindObjectsInactive.Include, FindObjectsSortMode.None).OrderBy(m => m.Id))
            sb.AppendLine($"slider {m.Id} on {PathOf(m.transform)} present {m.Present} busy {m.Busy} pose {m.Pose.Phase} {m.Pose.Position.ToString("0.000", CultureInfo.InvariantCulture)}");
        // Every renderer drawn in the desk band (lower 48% of the screen) that is not under the painted desk.
        int count = 0;
        var lines = new List<string>();
        foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (!r.enabled || !r.gameObject.activeInHierarchy || r is ParticleSystemRenderer || cam == null)
                continue;
            if ((cam.cullingMask & (1 << r.gameObject.layer)) == 0)
                continue;
            Vector3 c = cam.WorldToScreenPoint(r.bounds.center);
            if (c.z <= 0f || c.x < 0 || c.x > Screen.width || c.y < 0 || c.y > Screen.height * 0.48f)
                continue;
            string path = PathOf(r.transform);
            bool painted = path.Contains("/DeskPainted/") || r.name.StartsWith("Desk_") || r.name.StartsWith("PaintedBox") || r.name.StartsWith("StandIn");
            Material mat = r.sharedMaterial;
            string shader = mat != null && mat.shader != null ? mat.shader.name : "none";
            lines.Add($"{(painted ? "painted" : "OTHER  ")} {path} [{r.GetType().Name}] mat {(mat != null ? mat.name : "none")} shader {shader} screen ({c.x:0},{Screen.height - c.y:0})");
            count++;
        }
        lines.Sort();
        sb.AppendLine($"renderers in the desk band: {count}");
        foreach (string l in lines)
            sb.AppendLine(l);
        File.WriteAllText(Path.Combine(Out, "r13", "dump_" + s.name + ".txt"), sb.ToString());
    }

    // v3at (x, y = top-down screen px; name = the file): every enabled renderer whose projected bounds hold the point.
    private static IEnumerator At(QaRunner runner, QaStep s)
    {
        yield return new WaitForEndOfFrame();
        Camera cam = QaGame.PointerCamera();
        var sb = new StringBuilder();
        Vector2 pt = new Vector2(s.x, Screen.height - s.y);
        foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (!r.enabled || cam == null || (cam.cullingMask & (1 << r.gameObject.layer)) == 0)
                continue;
            Bounds b = r.bounds;
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            bool behind = false;
            for (int i = 0; i < 8; i++)
            {
                Vector3 c = cam.WorldToScreenPoint(new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z));
                if (c.z <= 0) { behind = true; break; }
                x0 = Mathf.Min(x0, c.x); x1 = Mathf.Max(x1, c.x); y0 = Mathf.Min(y0, c.y); y1 = Mathf.Max(y1, c.y);
            }
            if (behind || pt.x < x0 || pt.x > x1 || pt.y < y0 || pt.y > y1)
                continue;
            Material mat = r.sharedMaterial;
            sb.AppendLine($"{PathOf(r.transform)} [{r.GetType().Name}] layer {LayerMask.LayerToName(r.gameObject.layer)} mat {(mat != null ? mat.name : "none")} queue {(mat != null ? mat.renderQueue : 0)} rect ({x0:0},{Screen.height - y1:0})-({x1:0},{Screen.height - y0:0})");
        }
        if (cam != null)
        {
            sb.AppendLine("-- colliders under the point, nearest first:");
            foreach (RaycastHit h in Physics.RaycastAll(cam.ScreenPointToRay(pt), 20f, ~0, QueryTriggerInteraction.Collide).OrderBy(h => h.distance))
                sb.AppendLine($"{PathOf(h.collider.transform)} [{h.collider.GetType().Name}] layer {LayerMask.LayerToName(h.collider.gameObject.layer)} dist {h.distance:0.000} enabled {h.collider.enabled}");
        }
        File.WriteAllText(Path.Combine(Out, "r13", "at_" + s.name + ".txt"), sb.ToString());
    }

    private static V3Recorder _rec;

    private static IEnumerator Rec(QaRunner runner, QaStep s)
    {
        V3NoBadge.Ensure().Hide();
        var go = new GameObject("V3Recorder");
        Object.DontDestroyOnLoad(go);
        _rec = go.AddComponent<V3Recorder>();
        _rec.Begin(Path.Combine(Out, s.name), s.n > 0 ? s.n : 60);
        yield return null;
    }

    private static IEnumerator Stop(QaRunner runner, QaStep s)
    {
        if (_rec != null)
        {
            _rec.End();
            yield return null;
            Object.Destroy(_rec.gameObject);
            _rec = null;
        }
    }

    public static Vector2? Where(string target)
    {
        MachineSlideIn m = Object.FindObjectsByType<MachineSlideIn>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).FirstOrDefault(x => x.Id == target);
        GameObject go = m != null ? m.gameObject : null;
        if (go == null)
        {
            try { go = QaGame.GameObjectAt(target); } catch { return null; }
        }
        Camera cam = QaGame.PointerCamera();
        var rs = go.GetComponentsInChildren<Renderer>().Where(r => r.enabled && r.gameObject.activeInHierarchy && !(r is ParticleSystemRenderer)).ToList();
        if (rs.Count == 0 || cam == null)
            return QaGame.ScreenPoint(go);
        Bounds b = rs[0].bounds;
        foreach (Renderer r in rs)
            b.Encapsulate(r.bounds);
        Vector3 p = cam.WorldToScreenPoint(b.center);
        return p.z > 0 ? new Vector2(p.x, p.y) : (Vector2?)null;
    }

    private static IEnumerator MoveTo(QaRunner runner, QaStep s)
    {
        Vector2? p = Where(s.target);
        if (p == null)
        {
            runner.Assert("v3moveto." + s.target, "DESK-HOMES", false, "not found", s.target);
            yield break;
        }
        Vector2 to = p.Value + new Vector2(s.x * Screen.width, s.y * Screen.height);
        yield return runner.Input.MoveTo(to, s.n > 0 ? s.n : 30);
    }
}

/// <summary>
/// Keeps the dev "CHEATS ON" badge out of the review's captures, from the
/// capture op (local, never committed; desk v3 round 10): each LateUpdate (after
/// a scene load's re-enable, before the frame's OnGUI) it turns off the cheat
/// panel's component while its overlay is closed (feat/desk-v3: the badge is
/// its IMGUI) and the canvases under RUN-SAFE's CheatBadge (integrate/a: the
/// badge is uGUI). The run stays cheated; nothing of RUN-SAFE's is changed.
/// </summary>
public sealed class V3NoBadge : MonoBehaviour
{
    private static V3NoBadge _instance;
    private static readonly System.Type BadgeType = System.Type.GetType("CheatBadge, Assembly-CSharp");
    private static readonly FieldInfo PanelOpen = typeof(DebugPanelController).GetField("_open", BindingFlags.NonPublic | BindingFlags.Instance);

    public static V3NoBadge Ensure()
    {
        if (_instance != null)
            return _instance;
        var go = new GameObject("V3NoBadge");
        Object.DontDestroyOnLoad(go);
        _instance = go.AddComponent<V3NoBadge>();
        return _instance;
    }

    private static bool Open(DebugPanelController p) => PanelOpen != null && (bool)PanelOpen.GetValue(p);

    private void LateUpdate() => Hide();

    public void Hide()
    {
        foreach (DebugPanelController p in Object.FindObjectsByType<DebugPanelController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (p.enabled && !Open(p))
                p.enabled = false;
        if (BadgeType == null)
            return;
        foreach (Object b in Object.FindObjectsByType(BadgeType, FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            foreach (Canvas c in ((Component)b).GetComponentsInChildren<Canvas>(true))
                c.enabled = false;
    }

    /// <summary>What would still draw the badge ("" when nothing).</summary>
    public string Shown()
    {
        var on = new List<string>();
        foreach (DebugPanelController p in Object.FindObjectsByType<DebugPanelController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (p.enabled)
                on.Add("DebugPanelController(open " + Open(p) + ")");
        if (BadgeType != null)
            foreach (Object b in Object.FindObjectsByType(BadgeType, FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                foreach (Canvas c in ((Component)b).GetComponentsInChildren<Canvas>(true))
                    if (c.enabled && c.gameObject.activeInHierarchy)
                        on.Add("CheatBadge canvas " + c.name);
        return string.Join(", ", on);
    }
}

public sealed class V3Recorder : MonoBehaviour
{
    private string _dir;
    private int _index;
    private int _oldRate;
    private StreamWriter _poses;
    private MachineSlideIn[] _sliders;
    private bool _on;

    public void Begin(string dir, int fps)
    {
        if (Directory.Exists(dir))
            foreach (string f in Directory.GetFiles(dir))
                File.Delete(f);
        Directory.CreateDirectory(dir);
        _dir = dir;
        _oldRate = Time.captureFramerate;
        Time.captureFramerate = fps;
        _sliders = Object.FindObjectsByType<MachineSlideIn>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).OrderBy(m => m.Id).ToArray();
        _poses = new StreamWriter(Path.Combine(dir, "poses.tsv"));
        var head = new StringBuilder("i\ttime\tmx\tmy");
        foreach (MachineSlideIn m in _sliders)
            head.Append($"\t{m.Id}.phase\t{m.Id}.pos\t{m.Id}.sx\t{m.Id}.sy");
        _poses.WriteLine(head.ToString());
        _on = true;
        StartCoroutine(Loop());
    }

    public void End()
    {
        _on = false;
        Time.captureFramerate = _oldRate;
        _poses?.Dispose();
        _poses = null;
    }

    private void OnDestroy() => End();

    private IEnumerator Loop()
    {
        var eof = new WaitForEndOfFrame();
        Camera cam = QaGame.PointerCamera();
        while (_on)
        {
            yield return eof;
            if (!_on)
                yield break;
            Texture2D tex = ScreenCapture.CaptureScreenshotAsTexture();
            Vector2 m = QaInput.Current != null ? QaInput.Current.Position : Vector2.zero;
            DrawCursor(tex, Mathf.RoundToInt(m.x), Mathf.RoundToInt(m.y));
            File.WriteAllBytes(Path.Combine(_dir, $"f{_index:0000}.jpg"), tex.EncodeToJPG(88));
            Destroy(tex);
            var row = new StringBuilder();
            row.Append(_index).Append('\t').Append(Time.time.ToString("0.0000", CultureInfo.InvariantCulture)).Append('\t')
               .Append(m.x.ToString("0.0", CultureInfo.InvariantCulture)).Append('\t').Append((Screen.height - m.y).ToString("0.0", CultureInfo.InvariantCulture));
            foreach (MachineSlideIn s in _sliders)
            {
                Vector3 p = s != null && cam != null ? cam.WorldToScreenPoint(s.transform.position) : Vector3.zero;
                SlidePose pose = s != null ? s.Pose : default;
                row.Append('\t').Append(pose.Phase).Append('\t').Append(pose.Position.ToString("0.0000", CultureInfo.InvariantCulture))
                   .Append('\t').Append(p.x.ToString("0.00", CultureInfo.InvariantCulture)).Append('\t').Append((Screen.height - p.y).ToString("0.00", CultureInfo.InvariantCulture));
            }
            _poses?.WriteLine(row.ToString());
            _index++;
        }
    }

    // A plain arrow cursor (tip at the pointer), white with a black edge.
    private static void DrawCursor(Texture2D tex, int x, int y)
    {
        const int w = 18, h = 26;
        int x0 = x, y1 = y, x1 = x + w, y0 = y - h;
        int cx0 = Mathf.Clamp(x0, 0, tex.width), cx1 = Mathf.Clamp(x1, 0, tex.width), cy0 = Mathf.Clamp(y0, 0, tex.height), cy1 = Mathf.Clamp(y1, 0, tex.height);
        if (cx1 <= cx0 || cy1 <= cy0)
            return;
        Color[] px = tex.GetPixels(cx0, cy0, cx1 - cx0, cy1 - cy0);
        for (int j = cy0; j < cy1; j++)
            for (int i = cx0; i < cx1; i++)
            {
                float u = i - x + 0.5f, v = y - j + 0.5f; // u right, v down from the tip
                int k = Inside(u, v);
                if (k == 0)
                    continue;
                px[(j - cy0) * (cx1 - cx0) + (i - cx0)] = k == 2 ? Color.white : Color.black;
            }
        tex.SetPixels(cx0, cy0, cx1 - cx0, cy1 - cy0, px);
        tex.Apply(false);
    }

    // 0 outside, 1 the edge, 2 the fill: the triangle (0,0) (0,22) (15,16) with a 2 px edge.
    private static int Inside(float u, float v)
    {
        float d = Dist(u, v);
        if (d < -1.8f) return 2;
        if (d <= 0f) return 1;
        return 0;
    }

    private static float Dist(float u, float v)
    {
        Vector2 a = new Vector2(0, 0), b = new Vector2(0, 22), c = new Vector2(15, 16), p = new Vector2(u, v);
        float e1 = Edge(a, b, p), e2 = Edge(b, c, p), e3 = Edge(c, a, p);
        return Mathf.Max(e1, Mathf.Max(e2, e3));
    }

    // Signed distance to the line ab, positive outside (the triangle wound clockwise in screen-down space).
    private static float Edge(Vector2 a, Vector2 b, Vector2 p)
    {
        Vector2 d = (b - a).normalized;
        Vector2 n = new Vector2(-d.y, d.x);
        return Vector2.Dot(p - a, n);
    }
}
#endif
