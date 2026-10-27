#if UNITY_EDITOR || TIMEDESK_QA
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Local ops for desk v3 round 13 (never committed). v3hallrects (name): every
/// renderer under the anime hall with its enabled state, sorting order and
/// screen rectangle through each camera that draws its layer, into
/// r13\hall_rects_name.txt. v3hallshow (target: an object's name under the
/// hall; n = 1 shows its renderers, 0 hides them). v3booth (name): the
/// booth's renderers, materials and bounds, the hall's brightness, into
/// r13\booth_name.txt.
/// </summary>
public static class _DeskV3R13Ops
{
    private const string Out = @"E:\unity\NOPE-tools\reports\desk_v3_unity\r13";

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    private static void RegisterInEditor() => Register();
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        QaRunner.RegisterOp("v3hallrects", HallRects);
        QaRunner.RegisterOp("v3hallshow", HallShow);
        QaRunner.RegisterOp("v3booth", Booth);
    }

    private static string F(float v) => v.ToString("0.0", CultureInfo.InvariantCulture);

    private static IEnumerator HallRects(QaRunner runner, QaStep s)
    {
        yield return new WaitForEndOfFrame();
        var sb = new StringBuilder();
        AnimeHallPresentation hall = Object.FindAnyObjectByType<AnimeHallPresentation>();
        Camera[] cams = Camera.allCameras;
        foreach (Camera c in cams)
            sb.AppendLine($"camera {c.name} pos {c.transform.position} fov {F(c.fieldOfView)} ortho {c.orthographic} mask {c.cullingMask} target {(c.targetTexture != null ? c.targetTexture.width + "x" + c.targetTexture.height : "screen")} pixel {c.pixelWidth}x{c.pixelHeight}");
        if (hall != null)
            foreach (Renderer r in hall.GetComponentsInChildren<Renderer>(true))
            {
                string rect = "";
                foreach (Camera c in cams)
                {
                    if ((c.cullingMask & (1 << r.gameObject.layer)) == 0)
                        continue;
                    Bounds b = r.bounds;
                    float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
                    for (int i = 0; i < 8; i++)
                    {
                        Vector3 p = new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
                        Vector3 v = c.WorldToViewportPoint(p);
                        x0 = Mathf.Min(x0, v.x); x1 = Mathf.Max(x1, v.x); y0 = Mathf.Min(y0, v.y); y1 = Mathf.Max(y1, v.y);
                    }
                    // In 1920x1080 screen pixels, y from the top.
                    rect += $" [{c.name}: x {F(x0 * 1920)}..{F(x1 * 1920)} y {F((1 - y1) * 1080)}..{F((1 - y0) * 1080)}]";
                }
                string order = r is SpriteRenderer sr ? sr.sortingOrder.ToString() : r.sortingOrder.ToString();
                sb.AppendLine($"{(r.enabled && r.gameObject.activeInHierarchy ? "ON " : "off")} {r.name} layer {r.gameObject.layer} order {order} mat {(r.sharedMaterial != null ? r.sharedMaterial.name : "-")}{rect}");
            }
        Directory.CreateDirectory(Out);
        File.WriteAllText(Path.Combine(Out, "hall_rects_" + s.name + ".txt"), sb.ToString());
    }

    private static IEnumerator HallShow(QaRunner runner, QaStep s)
    {
        AnimeHallPresentation hall = Object.FindAnyObjectByType<AnimeHallPresentation>();
        int n = 0;
        if (hall != null)
            foreach (Transform t in hall.GetComponentsInChildren<Transform>(true))
                if (t.name == s.target)
                    foreach (Renderer r in t.GetComponentsInChildren<Renderer>(true))
                    {
                        r.enabled = s.n != 0;
                        n++;
                    }
        runner.Assert("v3.hallshow." + s.target, "DESK-HOMES", n > 0, n + " renderer(s) set " + (s.n != 0 ? "on" : "off"), "the named hall layer exists");
        yield return null;
    }

    private static IEnumerator Booth(QaRunner runner, QaStep s)
    {
        yield return new WaitForEndOfFrame();
        var sb = new StringBuilder();
        DeskPainted desk = Object.FindAnyObjectByType<DeskPainted>();
        foreach (string name in DeskPaintedMap.Booth)
        {
            Transform m = desk != null ? desk.Mesh(name) : null;
            Renderer r = m != null ? m.GetComponent<Renderer>() : null;
            sb.AppendLine(r == null ? $"{name}: MISSING" : $"{name}: enabled {r.enabled && r.gameObject.activeInHierarchy} mat {r.sharedMaterial?.name} shader {r.sharedMaterial?.shader.name} bounds {r.bounds.center} {r.bounds.size} shadows {r.shadowCastingMode}");
        }
        Transform glass = desk != null ? desk.Mesh(DeskPaintedMap.Glass) : null;
        sb.AppendLine($"DeskGlass drawn: {(glass != null && glass.gameObject.activeInHierarchy && glass.GetComponent<Renderer>() is Renderer g && g.enabled)}");
        HallBackdrop backdrop = Object.FindAnyObjectByType<HallBackdrop>();
        HallLightingRig rig = Object.FindAnyObjectByType<HallLightingRig>();
        sb.AppendLine($"hall brightness {(backdrop != null ? backdrop.Brightness.ToString("0.0000", CultureInfo.InvariantCulture) : "-")} hour {(rig != null ? rig.Hour.ToString("0.000", CultureInfo.InvariantCulture) : "-")} paintedDesk {(rig != null && rig.PaintedDesk)}");
        foreach (string name in DeskPaintedMap.HallClosedByBooth)
        {
            AnimeHallPresentation hall = Object.FindAnyObjectByType<AnimeHallPresentation>();
            if (hall == null)
                continue;
            foreach (Transform t in hall.GetComponentsInChildren<Transform>(true))
                if (t.name == name)
                    foreach (Renderer r in t.GetComponentsInChildren<Renderer>(true))
                        sb.AppendLine($"hall {name}/{r.name}: enabled {r.enabled}");
        }
        Directory.CreateDirectory(Out);
        File.WriteAllText(Path.Combine(Out, "booth_" + s.name + ".txt"), sb.ToString());
        runner.Assert("v3.booth." + s.name, "DESK-ART", desk != null && sb.ToString().IndexOf("MISSING", System.StringComparison.Ordinal) < 0, desk != null ? "5 booth objects" : "no painted desk", "the booth is installed");
    }
}
#endif
