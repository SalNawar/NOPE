#if UNITY_EDITOR || TIMEDESK_QA
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// DESK-PLAN's QA rig op (docs/pending/QA-RIG.md §4 "Tracks' own ops";
/// FIX_PLAN v1 DESK-PLAN: "≥90% of its pixels are visible (ID-buffer
/// render)"). <c>deskvisible</c> (target = an object's path or name; f = the
/// least share, 0.9 when 0; name = the assertion's name) renders an ID buffer
/// from the pointer camera at half the screen's size: the object's own
/// renderers alone (its pixels), then every opaque renderer the camera draws
/// in black with the object's in white over the depth (its pixels left in
/// sight), and asserts <c>visible.&lt;name&gt;</c>: seen ÷ own ≥ f. A click
/// box with no renderers is measured through the renderers its Clickable
/// outlines (an art prop's). <c>deskclickboxes</c> (name = the assertion's
/// name; f = the tolerance in metres, 0.005 when 0) asserts
/// <c>clickboxes.&lt;name&gt;</c>: no two desk objects' real click boxes go
/// into each other (BUG-4a: "fails the build on ... overlapping click
/// boxes"): every shown Clickable's BoxCollider over the desk and off the
/// paper mat, grouped by the desk object it belongs to
/// (DeskLayout.HomeIdOf), measured by Physics.ComputePenetration and judged
/// by DeskArrangement.ClickBoxClashes. <c>deskexpect</c> asserts a bool
/// member reads true (the intercom's wheel open with the reading view still
/// on).
/// Registered at load in the editor and at play start in the QA player.
/// </summary>
public static class DeskPlanOps
{
    private const string Owner = "DESK-PLAN";

    /// <summary>Opaque geometry only occludes (glass, sprites and other transparent layers would count their clear pixels as solid).</summary>
    private const int OpaqueQueueLimit = 2450;

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    private static void RegisterInEditor() => Register();
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        QaRunner.RegisterOp("deskvisible", Visible);
        QaRunner.RegisterOp("deskexpect", Expect);
        QaRunner.RegisterOp("deskclickboxes", ClickBoxes);
    }

    /// <summary>How high over the desk's top a click box's centre may stand and still count as the desk's (metres): the PC and the lamp, not the hall.</summary>
    private const float DeskReach = 0.8f;

    private static IEnumerator ClickBoxes(QaRunner runner, QaStep step)
    {
        yield return new WaitForFixedUpdate();
        string name = "clickboxes." + (string.IsNullOrEmpty(step.name) ? "desk" : step.name);
        float tolerance = step.f > 0f ? step.f : 0.005f;
        if (!DeskLayout.Bound)
        {
            runner.Assert(name, Owner, false, "not measured", "the desk is not bound (DeskLayout.Bound)");
            yield break;
        }
        DeskFrame frame = DeskLayout.Frame;
        DeskRect desk = DeskHomeMap.DeskTop;
        var boxes = new List<BoxCollider>();
        var names = new List<string>();
        var owners = new List<string>();
        foreach (Clickable click in Object.FindObjectsByType<Clickable>(FindObjectsSortMode.None))
        {
            if (!click.isActiveAndEnabled || !click.TryGetComponent(out BoxCollider box) || !box.enabled)
                continue;
            Vector3 centre = box.bounds.center;
            Vector2 at = frame.Local(centre);
            float over = centre.y - frame.Origin.y;
            if (!desk.Contains(at.x, at.y) || DeskLayout.Mat.IsOnMat(at.x, at.y) || over < -0.05f || over > DeskReach)
                continue; // the hall, the traveller's side, the papers and the mat's own toggle
            boxes.Add(box);
            names.Add(Path(click.transform));
            owners.Add(DeskLayout.HomeIdOf(click.transform));
        }
        float Depth(int i, int j)
        {
            BoxCollider a = boxes[i], b = boxes[j];
            if (!a.bounds.Intersects(b.bounds))
                return 0f;
            return Physics.ComputePenetration(a, a.transform.position, a.transform.rotation, b, b.transform.position, b.transform.rotation, out _, out float d) ? d : 0f;
        }
        List<string> clashes = DeskArrangement.ClickBoxClashes(names, owners, Depth, tolerance);
        runner.Assert(name, Owner, clashes.Count == 0, $"{boxes.Count} desk click box(es), {clashes.Count} clash(es)",
                      clashes.Count > 0 ? string.Join("; ", clashes) : $"tolerance {tolerance * 1000f:0} mm (BUG-4a)");
    }

    /// <summary>A click box's path for the report (its object and up to two parents).</summary>
    private static string Path(Transform t)
    {
        string path = t.name;
        for (int i = 0; i < 2 && t.parent != null; i++)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }

    /// <summary><c>deskexpect</c> (target = a bool "Type.Member"; name = the assertion's name): asserts <c>expect.&lt;name&gt;</c>, the member reads true now (the reading view still on, the wheel open).</summary>
    private static IEnumerator Expect(QaRunner runner, QaStep step)
    {
        object value = QaGame.Member(step.target);
        bool on = value is bool b && b;
        runner.Assert("expect." + (string.IsNullOrEmpty(step.name) ? step.target : step.name), Owner, on, $"{step.target} = {value ?? "null"}", "expected true");
        yield break;
    }

    private static IEnumerator Visible(QaRunner runner, QaStep step)
    {
        yield return new WaitForEndOfFrame();
        GameObject go = QaGame.GameObjectAt(step.target);
        Camera cam = QaGame.PointerCamera();
        float least = step.f > 0f ? step.f : 0.9f;
        string name = string.IsNullOrEmpty(step.name) ? go.name : step.name;
        List<Renderer> own = Own(go);
        if (cam == null || own.Count == 0)
        {
            runner.Assert("visible." + name, Owner, false, "not measured", cam == null ? "no pointer camera" : $"{step.target} has no renderer showing");
            yield break;
        }
        Shader shader = Shader.Find("Hidden/Internal-Colored");
        if (shader == null)
        {
            runner.Assert("visible." + name, Owner, false, "not measured", "Hidden/Internal-Colored is not in this build");
            yield break;
        }
        var black = new Material(shader) { color = Color.black };
        var white = new Material(shader) { color = Color.white };
        foreach (Material m in new[] { black, white })
        {
            m.SetInt("_ZWrite", 1);
            m.SetInt("_ZTest", (int)CompareFunction.LessEqual);
            m.SetInt("_Cull", (int)CullMode.Off);
        }
        int w = Mathf.Max(64, cam.pixelWidth / 2), h = Mathf.Max(64, cam.pixelHeight / 2);
        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
        var read = new Texture2D(w, h, TextureFormat.RGBA32, false);
        try
        {
            var mine = new HashSet<Renderer>(own);
            List<Renderer> others = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)
                                          .Where(r => !mine.Contains(r) && Draws(r, cam)).ToList();
            int total = WhitePixels(Draw(cam, rt, null, black, own, white), read);
            int seen = WhitePixels(Draw(cam, rt, others, black, own, white), read);
            float share = total > 0 ? (float)seen / total : 0f;
            runner.Assert("visible." + name, Owner, total > 0 && share >= least, $"{share * 100f:0.0}% seen ({seen} of {total} px at {w}x{h})",
                          $"least {least * 100f:0}% (BUG-4a); {others.Count} opaque renderer(s) could cover it");
        }
        finally
        {
            Object.Destroy(rt);
            Object.Destroy(read);
            Object.Destroy(black);
            Object.Destroy(white);
        }
    }

    /// <summary>The object's own renderers that show: under it, else those its Clickable outlines (an art prop's click box).</summary>
    private static List<Renderer> Own(GameObject go)
    {
        var own = go.GetComponentsInChildren<Renderer>(false).Where(r => r.enabled).ToList();
        if (own.Count == 0 && go.TryGetComponent(out Clickable click))
            own = click.Outline.Where(r => r != null && r.enabled && r.gameObject.activeInHierarchy).ToList();
        return own;
    }

    /// <summary>True for a mesh the camera draws as opaque geometry.</summary>
    private static bool Draws(Renderer r, Camera cam) =>
        r.enabled && r.gameObject.activeInHierarchy && (r is MeshRenderer || r is SkinnedMeshRenderer) && (cam.cullingMask & (1 << r.gameObject.layer)) != 0 &&
        r.sharedMaterials.Length > 0 && r.sharedMaterials.All(m => m != null && m.renderQueue < OpaqueQueueLimit);

    /// <summary>Renders <paramref name="occluders"/> (none: the object alone) in <paramref name="black"/> then <paramref name="own"/> in <paramref name="white"/> over the depth, from <paramref name="cam"/>, into <paramref name="rt"/>.</summary>
    private static RenderTexture Draw(Camera cam, RenderTexture rt, List<Renderer> occluders, Material black, List<Renderer> own, Material white)
    {
        var cmd = new CommandBuffer { name = "DeskPlanOps.Visible" };
        cmd.SetRenderTarget(rt);
        cmd.ClearRenderTarget(true, true, Color.black);
        cmd.SetViewProjectionMatrices(cam.worldToCameraMatrix, cam.projectionMatrix);
        foreach (Renderer r in occluders ?? new List<Renderer>())
            for (int i = 0; i < r.sharedMaterials.Length; i++)
                cmd.DrawRenderer(r, black, i);
        foreach (Renderer r in own)
            for (int i = 0; i < Mathf.Max(1, r.sharedMaterials.Length); i++)
                cmd.DrawRenderer(r, white, i);
        Graphics.ExecuteCommandBuffer(cmd);
        cmd.Release();
        return rt;
    }

    private static int WhitePixels(RenderTexture rt, Texture2D read)
    {
        RenderTexture was = RenderTexture.active;
        RenderTexture.active = rt;
        read.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        read.Apply(false);
        RenderTexture.active = was;
        int n = 0;
        foreach (Color32 c in read.GetPixels32())
            if (c.r > 127)
                n++;
        return n;
    }
}
#endif
