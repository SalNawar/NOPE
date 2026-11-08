#if UNITY_EDITOR || TIMEDESK_QA
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// The scene pick (the night task pc-spike: name an object seen on a still):
/// <c>pick</c> (x, y: the still's pixel from the top left; name) lists every
/// drawn renderer whose bounds, projected through the pointer camera, hold
/// that pixel, nearest first, each with its distance, its path, its bounds,
/// its rect on the still, its first material with its base colour and its
/// layer, in pick_&lt;name&gt;_&lt;w&gt;x&lt;h&gt;.txt in the run folder. The
/// rect is the bounds', not the drawn shape, so the list names candidates:
/// the object drawn at the pixel is among them. <c>hide</c> (target: an
/// object's path from its scene root, or its name; n = 1 hides, 0 shows)
/// turns that object's enabled renderers off, or the ones it turned off back
/// on (never the renderers the game keeps off), so a still with one
/// candidate hidden says which one draws the pixel.
/// </summary>
public static class PickOps
{
    /// <summary>Registers the op before the first scene loads (editor and QA player alike), and in the editor on every domain load too, so the edit-mode scenario test knows it.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
#endif
    private static void Register()
    {
        QaRunner.RegisterOp("pick", Pick);
        QaRunner.RegisterOp("hide", Hide);
    }

    /// <summary>Per target, the renderers that <c>hide</c> turned off, so <c>hide</c> with n = 0 turns on only those and leaves the renderers the game keeps off (the CRT recess) alone.</summary>
    private static readonly Dictionary<string, List<Renderer>> Hidden = new Dictionary<string, List<Renderer>>();

    /// <summary>The enabled renderers under the target off (n = 1), or the ones this op turned off back on (n = 0).</summary>
    private static IEnumerator Hide(QaRunner runner, QaStep s)
    {
        if (s.n == 0)
        {
            if (Hidden.TryGetValue(s.target, out List<Renderer> off))
            {
                foreach (Renderer r in off)
                    if (r != null)
                        r.enabled = true;
                Hidden.Remove(s.target);
            }
        }
        else
        {
            GameObject go = QaGame.GameObjectAt(s.target);
            var off = new List<Renderer>();
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (!r.enabled)
                    continue;
                r.enabled = false;
                off.Add(r);
            }
            Hidden[s.target] = off;
        }
        yield return QaGame.Frames(3);
    }

    /// <summary>The renderers under the pixel (x, y from the top left), nearest first, to the run folder.</summary>
    private static IEnumerator Pick(QaRunner runner, QaStep s)
    {
        yield return null;
        Camera cam = QaGame.PointerCamera();
        if (cam == null)
        {
            Debug.LogWarning("[PickOps] No pointer camera: nothing picked.");
            yield break;
        }
        var at = new Vector2(s.x, Screen.height - s.y);
        var rows = new List<(float d, string line)>();
        foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
        {
            if (!r.enabled || r is ParticleSystemRenderer || r.transform is RectTransform)
                continue;
            Rect? rect = Project(cam, r.bounds);
            if (rect == null || !rect.Value.Contains(at))
                continue;
            float d = Vector3.Distance(cam.transform.position, r.bounds.center);
            Material m = r.sharedMaterial;
            string colour = m == null ? "-" : m.HasProperty("_BaseColor") ? ColorUtility.ToHtmlStringRGB(m.GetColor("_BaseColor")) : m.HasProperty("_Color") ? ColorUtility.ToHtmlStringRGB(m.color) : "-";
            Vector3 size = r.bounds.size;
            Rect v = rect.Value;
            rows.Add((d, $"{d:0.000} m\t{Path(r.transform)}\t{r.GetType().Name}\tbounds {size.x:0.000} x {size.y:0.000} x {size.z:0.000} m" +
                         $"\trect x {v.xMin:0} to {v.xMax:0}, y {Screen.height - v.yMax:0} to {Screen.height - v.yMin:0}\tmat {(m != null ? m.name : "-")} #{colour}\tlayer {LayerMask.LayerToName(r.gameObject.layer)}"));
        }
        rows.Sort((a, b) => a.d.CompareTo(b.d));
        string file = System.IO.Path.Combine(runner.OutDir, $"pick_{s.name}_{Screen.width}x{Screen.height}.txt");
        File.WriteAllLines(file, new[] { $"pick at ({s.x}, {s.y}) from the top left through {cam.name}: {rows.Count} renderers, nearest first" }.Concat(rows.Select(r => r.line)));
        Debug.Log($"[PickOps] {rows.Count} renderers under ({s.x}, {s.y}): {file}");
    }

    /// <summary>The screen rect (origin bottom-left) of the eight corners of <paramref name="b"/> through <paramref name="cam"/>; null when a corner is behind the camera.</summary>
    private static Rect? Project(Camera cam, Bounds b)
    {
        float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
            Vector3 p = cam.WorldToScreenPoint(corner);
            if (p.z <= 0f)
                return null;
            x0 = Mathf.Min(x0, p.x); y0 = Mathf.Min(y0, p.y); x1 = Mathf.Max(x1, p.x); y1 = Mathf.Max(y1, p.y);
        }
        return Rect.MinMaxRect(x0, y0, x1, y1);
    }

    /// <summary>The transform's path from its scene root, "/" between names.</summary>
    private static string Path(Transform t)
    {
        string path = t.name;
        for (Transform p = t.parent; p != null; p = p.parent)
            path = p.name + "/" + path;
        return path;
    }
}
#endif
