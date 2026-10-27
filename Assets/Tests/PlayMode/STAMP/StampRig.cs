#if UNITY_EDITOR || TIMEDESK_QA
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

/// <summary>
/// STAMP's ops for QA-RIG scenarios (docs/pending/STAMP.md R2, R4, R6; QA-RIG §8's STAMP row). Every press and drag
/// goes through the rig's virtual mouse and keyboard along a P-* path at the fixed 30 fps step; nothing is
/// teleported and no handler is called behind the player's back (the hand-back's "cheat" mode, which ends a case
/// through the cheat menu's decision when a real hand-back would be a wrong call, is the only shortcut, and it
/// says so in its note).
/// <list type="bullet">
/// <item><c>stampRackWatch</c> (n 1 starts, n 0 stops and asserts <c>rackOverPaper.&lt;name&gt;</c>): every
/// frame, the rack's pixels over any paper (BUG-15a, FIX_PLAN_v2's pass line "rack pixels over any paper = 0 in
/// every frame"): a grid of rays (6 px at 1080p) over where the rack's and each paper's screen rects meet, a pixel
/// counted when a ray meets a rack part (its meshes' boxes) nearer than the paper. The daters count while they
/// stand in their cradles. It also tracks the rack and the papers (the flicker mask) and logs a
/// <c>rackOverPaper</c> column.</item>
/// <item><c>stampPaper</c> (n = paper index, -1 the passport; x2, y2 = where on the screen, fractions; path) and
/// <c>stampFolderOver</c> (f = how far the rulebook's edge goes over the passport, metres, default 0.03): the
/// set-ups, by real drags (the last paper touched lies on top).</item>
/// <item><c>stampAim</c> (text = the dater: Approved, Denied or correct; n = the aimed paper, -1 the passport; x,
/// y = where on it, fractions of its face, default its middle; target "folderEdge" aims f (default 8 mm) inside
/// the rulebook's edge where it lies over the passport, "stack" also asserts the passport lies on at least two
/// papers there; path): one try, asserted as <c>aim.&lt;name&gt;</c> (R4): the stamp lands on the aimed document
/// within 3 mm of the pointer's spot on it; an accepted press prints exactly once, in its verdict's ink, and the
/// die's face stops within 0.5 mm of the surface it met (no sink); a refused one prints nothing.</item>
/// <item><c>stampRegrab</c> (as stampAim): the same try, then the dater pressed again 2 frames after the let-go
/// and dragged (R4 c): asserted as <c>aim.&lt;name&gt;</c>, also that the re-grab carried nothing until the stroke
/// ended and the print landed.</item>
/// <item><c>stampLetGo</c> (text = the dater; target = rack, chip, scanner or desk; x, y for desk): R4 (d), asserted
/// as <c>letGo.&lt;name&gt;</c>.</item>
/// <item><c>stampTab</c>: TAB, the reading view, the rack in frame and the daters' answer, TAB back (R2.3, R2.4),
/// asserted as <c>tab.&lt;name&gt;.view</c>, <c>.rackInFrame</c> (CAM's request 6) and <c>.daters</c>.</item>
/// <item><c>stampCradles</c> (n 1 starts, n 0 asserts <c>cradles.&lt;name&gt;</c>): the case opens and the DENIED
/// cradle starts on that frame and locks within 0.3 s (R6, BUG-73b).</item>
/// <item><c>stampHandBack</c> (n 0: a real drag of the passport onto the counter, acknowledging a citation slip if
/// one shows; n 1: the cheat menu's correct decision): ends the case and waits for it to go.</item>
/// <item><c>stampView</c> (n 1 the reading view, n 0 the office view; TAB when needed) and <c>stampStack</c> (x2, y2:
/// every paper, the passport last, dragged onto one spot in the office view): the set-ups of R4.</item>
/// <item><c>stampHold</c> (text = the dater; key = Escape, Q, A, Tab or RightClick): R5 (BUG-55b), the dater held over
/// the passport, the key, then the button let go; asserted as <c>hold.&lt;name&gt;</c> (nothing stamped, the hold
/// ended, the dater home).</item>
/// <item><c>stampDump</c>: the desk's state (rack, papers, folder, daters, machines, view) into stamp_report.txt.</item>
/// </list>
/// </summary>
public static class StampRig
{
    private const string Owner = "STAMP";

    /// <summary>Registers the ops before the first scene of a play run and, in the editor, at every domain load (so QaEditorGateTests sees them).</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
#endif
    private static void Register()
    {
        QaRunner.RegisterOp("stampDump", Dump);
        QaRunner.RegisterOp("stampRackWatch", RackWatch);
        QaRunner.RegisterOp("stampPaper", MovePaper);
        QaRunner.RegisterOp("stampFolderOver", FolderOver);
        QaRunner.RegisterOp("stampAim", (runner, step) => Aim(runner, step, false));
        QaRunner.RegisterOp("stampRegrab", (runner, step) => Aim(runner, step, true));
        QaRunner.RegisterOp("stampLetGo", LetGo);
        QaRunner.RegisterOp("stampTab", Tab);
        QaRunner.RegisterOp("stampCradles", Cradles);
        QaRunner.RegisterOp("stampHandBack", HandBack);
        QaRunner.RegisterOp("stampView", View);
        QaRunner.RegisterOp("stampStack", Stack);
        QaRunner.RegisterOp("stampHold", Hold);
    }

    // ---------------- The game's parts ----------------

    private static DeskStampTray Tray() => QaGame.Find<DeskStampTray>() ?? throw new InvalidOperationException("No DeskStampTray is loaded.");

    private static Camera Cam() => QaGame.PointerCamera() ?? Camera.main;

    private static Transform Surface(DeskStampTray tray) => QaGame.Field(tray, "surface") is Component c && c != null ? c.transform : null;

    private static Vector3 Up(DeskStampTray tray) => Surface(tray) is Transform s ? s.up : Vector3.up;

    private static float Height(DeskStampTray tray, Vector3 world) => Surface(tray) is Transform s ? Vector3.Dot(world - s.position, s.up) : world.y;

    private static bool IsDenied(string which) => !string.IsNullOrEmpty(which) && which.Trim().StartsWith("d", StringComparison.OrdinalIgnoreCase);

    /// <summary>"correct": the dater of the active case's correct verdict; else Approved or Denied by name.</summary>
    private static string Which(string text)
    {
        if (string.Equals(text, "correct", StringComparison.OrdinalIgnoreCase))
        {
            GameManager game = QaGame.Find<GameManager>();
            CaseInstance inst = game != null ? game.ActiveCase : null;
            if (inst == null)
                throw new InvalidOperationException("stamp: no active case for the correct verdict.");
            return inst.ShouldAccept ? "Approved" : "Denied";
        }
        return string.IsNullOrEmpty(text) ? "Approved" : text;
    }

    private static Clickable Dater(DeskStampTray tray, string which) => (Clickable)QaGame.Field(tray, IsDenied(which) ? "deniedStamp" : "approvedStamp");

    private static Transform Die(DeskStampTray tray, string which) => (Transform)QaGame.Field(tray, IsDenied(which) ? "deniedDie" : "approvedDie");

    /// <summary>The tray's private handle of <paramref name="which"/> dater (read only: its springs and home).</summary>
    private static object Handle(DeskStampTray tray, string which)
    {
        var handles = (Array)QaGame.Field(tray, "_handles");
        Clickable click = Dater(tray, which);
        foreach (object h in handles)
            if (ReferenceEquals(QaGame.Field(h, "Click"), click))
                return h;
        throw new InvalidOperationException($"stamp: no handle for the {which} dater.");
    }

    private static List<DeskDocument> Papers() =>
        Object.FindObjectsByType<DeskDocument>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(p => p.isActiveAndEnabled).OrderBy(p => p.Index).ToList();

    private static DeskDocument Paper(DeskStampTray tray, int index)
    {
        int want = index < 0 ? tray.Passport : index;
        return Papers().FirstOrDefault(p => p.Index == want) ?? throw new InvalidOperationException($"stamp: no paper {want} lies on the desk (passport {tray.Passport}).");
    }

    private static DeskRulebook Folder() => QaGame.Find<DeskRulebook>();

    private static int Prints(DeskDocument paper) => paper != null ? (int)QaGame.Field(paper, "_stamps") : 0;

    private static int AllPrints() => Papers().Sum(Prints);

    // ---------------- Geometry ----------------

    /// <summary>Where <paramref name="ray"/> first meets the box (<paramref name="center"/>, <paramref name="size"/>) in <paramref name="t"/>'s space (world metres along the ray).</summary>
    private static bool RayBox(Ray ray, Transform t, Vector3 center, Vector3 size, out float distance)
    {
        distance = float.MaxValue;
        Vector3 o = t.InverseTransformPoint(ray.origin), d = t.InverseTransformVector(ray.direction);
        Vector3 min = center - size * 0.5f, max = center + size * 0.5f;
        float t0 = float.MinValue, t1 = float.MaxValue;
        for (int a = 0; a < 3; a++)
        {
            if (Mathf.Abs(d[a]) < 1e-9f)
            {
                if (o[a] < min[a] - 1e-6f || o[a] > max[a] + 1e-6f)
                    return false;
                continue;
            }
            float ta = (min[a] - o[a]) / d[a], tb = (max[a] - o[a]) / d[a];
            if (ta > tb)
                (ta, tb) = (tb, ta);
            t0 = Mathf.Max(t0, ta);
            t1 = Mathf.Min(t1, tb);
            if (t0 > t1)
                return false;
        }
        if (t1 < 0f)
            return false;
        distance = Mathf.Max(0f, t0);
        return true;
    }

    /// <summary>Where <paramref name="ray"/> first meets <paramref name="thing"/>'s box colliders (enabled or not: a carried paper's are off).</summary>
    private static bool HitBoxes(Component thing, Ray ray, out float distance)
    {
        distance = float.MaxValue;
        bool hit = false;
        foreach (BoxCollider box in thing.GetComponentsInChildren<BoxCollider>(true))
            if (box.gameObject.activeInHierarchy && RayBox(ray, box.transform, box.center, box.size, out float d) && d < distance)
            {
                distance = d;
                hit = true;
            }
        return hit;
    }

    /// <summary>The rack's visible parts: its meshes but the contact shadow and, unless <paramref name="daters"/>, the daters.</summary>
    private static List<MeshFilter> RackParts(DeskStampTray tray, bool daters)
    {
        var parts = new List<MeshFilter>();
        Transform rack = tray.Rack;
        if (rack == null || !rack.gameObject.activeInHierarchy)
            return parts;
        Transform a = Dater(tray, "Approved") is Clickable ca && ca != null ? ca.transform : null;
        Transform d = Dater(tray, "Denied") is Clickable cd && cd != null ? cd.transform : null;
        foreach (MeshFilter mf in rack.GetComponentsInChildren<MeshFilter>(false))
        {
            if (mf.sharedMesh == null || mf.name == "ContactShadow")
                continue;
            Renderer r = mf.GetComponent<Renderer>();
            if (r == null || !r.enabled)
                continue;
            if (!daters && (a != null && mf.transform.IsChildOf(a) || d != null && mf.transform.IsChildOf(d)))
                continue;
            parts.Add(mf);
        }
        return parts;
    }

    private static IEnumerable<Vector3> Corners(MeshFilter mf)
    {
        Bounds b = mf.sharedMesh.bounds;
        for (int i = 0; i < 8; i++)
            yield return mf.transform.TransformPoint(new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z));
    }

    private static bool HitParts(List<MeshFilter> parts, Ray ray, out float distance)
    {
        distance = float.MaxValue;
        bool hit = false;
        foreach (MeshFilter mf in parts)
        {
            Bounds b = mf.sharedMesh.bounds;
            if (RayBox(ray, mf.transform, b.center, b.size, out float d) && d < distance)
            {
                distance = d;
                hit = true;
            }
        }
        return hit;
    }

    private static Rect? ScreenRectOf(IEnumerable<Vector3> world, Camera cam)
    {
        float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
        bool any = false;
        foreach (Vector3 w in world)
        {
            Vector3 s = cam.WorldToScreenPoint(w);
            if (s.z <= 0f)
                return null;
            x0 = Mathf.Min(x0, s.x); y0 = Mathf.Min(y0, s.y);
            x1 = Mathf.Max(x1, s.x); y1 = Mathf.Max(y1, s.y);
            any = true;
        }
        return any ? Rect.MinMaxRect(x0, y0, x1, y1) : (Rect?)null;
    }

    private static IEnumerable<Vector3> BoxCorners(Component thing)
    {
        foreach (BoxCollider box in thing.GetComponentsInChildren<BoxCollider>(true))
        {
            if (!box.gameObject.activeInHierarchy)
                continue;
            Vector3 h = box.size * 0.5f;
            for (int i = 0; i < 8; i++)
                yield return box.transform.TransformPoint(box.center + new Vector3((i & 1) == 0 ? -h.x : h.x, (i & 2) == 0 ? -h.y : h.y, (i & 4) == 0 ? -h.z : h.z));
        }
    }

    /// <summary>A point on <paramref name="paper"/>'s face: (<paramref name="fx"/>, <paramref name="fy"/>) across its largest box's two widest sides, on the side facing the desk's up.</summary>
    private static Vector3 FacePoint(DeskStampTray tray, DeskDocument paper, float fx, float fy)
    {
        BoxCollider box = paper.GetComponentsInChildren<BoxCollider>(true).Where(b => b.gameObject.activeInHierarchy)
                               .OrderByDescending(b => Mathf.Abs(b.size.x * b.size.y * b.size.z) + Mathf.Abs(b.size.x * b.size.y) + Mathf.Abs(b.size.x * b.size.z) + Mathf.Abs(b.size.y * b.size.z))
                               .FirstOrDefault() ?? throw new InvalidOperationException($"stamp: paper {paper.Index} has no box collider.");
        Vector3 s = box.size;
        int thin = Mathf.Abs(s.x * box.transform.lossyScale.x) <= Mathf.Abs(s.y * box.transform.lossyScale.y) && Mathf.Abs(s.x * box.transform.lossyScale.x) <= Mathf.Abs(s.z * box.transform.lossyScale.z) ? 0
                 : Mathf.Abs(s.y * box.transform.lossyScale.y) <= Mathf.Abs(s.z * box.transform.lossyScale.z) ? 1 : 2;
        int u = thin == 0 ? 1 : 0, v = thin == 2 ? 1 : 2;
        Vector3 local = box.center;
        local[u] += (fx - 0.5f) * s[u];
        local[v] += (fy - 0.5f) * s[v];
        Vector3 up = Up(tray);
        Vector3 axis = Vector3.zero;
        axis[thin] = 1f;
        float sign = Vector3.Dot(box.transform.TransformVector(axis), up) >= 0f ? 1f : -1f;
        local[thin] += sign * 0.5f * s[thin];
        return box.transform.TransformPoint(local);
    }

    /// <summary>Where a player would press <paramref name="paper"/> to pick it up: its middle when it shows on top there, else the nearest point of its face that is on the screen with nothing over it.</summary>
    private static Vector2 GrabPoint(DeskStampTray tray, DeskDocument paper, Camera cam)
    {
        Vector2? best = null;
        float bestCost = float.MaxValue;
        for (int i = 1; i < 20; i++)
            for (int j = 1; j < 20; j++)
            {
                float u = i / 20f, v = j / 20f, cost = Mathf.Abs(u - 0.5f) + Mathf.Abs(v - 0.5f);
                if (cost >= bestCost)
                    continue;
                Vector3 sp = cam.WorldToScreenPoint(FacePoint(tray, paper, u, v));
                if (sp.z <= 0f || sp.x < 12f || sp.y < 12f || sp.x > Screen.width - 12f || sp.y > Screen.height - 12f)
                    continue;
                Ray r = cam.ScreenPointToRay(sp);
                if (!HitBoxes(paper, r, out float mine))
                    continue;
                bool covered = Papers().Any(o => o != paper && HitBoxes(o, r, out float d) && d < mine) || Folder() is DeskRulebook rb && HitBoxes(rb, r, out float fd) && fd < mine;
                if (covered)
                    continue;
                best = new Vector2(sp.x, sp.y);
                bestCost = cost;
            }
        return best ?? Screen2(FacePoint(tray, paper, 0.5f, 0.5f), cam);
    }

    /// <summary>The desk-plane axes of the rulebook (its right, and the matching forward) and its footprint on them.</summary>
    private static (Vector3 across, Vector3 along, float minA, float maxA, float minB, float maxB, float top) Footprint(DeskStampTray tray, Component thing, Vector3? axesOf = null)
    {
        Vector3 up = Up(tray);
        Vector3 across = Vector3.ProjectOnPlane(axesOf ?? Folder().transform.right, up).normalized;
        Vector3 along = Vector3.Cross(across, up);
        float minA = float.MaxValue, maxA = float.MinValue, minB = float.MaxValue, maxB = float.MinValue, top = float.MinValue;
        foreach (Vector3 c in BoxCorners(thing))
        {
            float a = Vector3.Dot(c, across), b = Vector3.Dot(c, along);
            minA = Mathf.Min(minA, a); maxA = Mathf.Max(maxA, a);
            minB = Mathf.Min(minB, b); maxB = Mathf.Max(maxB, b);
            top = Mathf.Max(top, Height(tray, c));
        }
        return (across, along, minA, maxA, minB, maxB, top);
    }

    /// <summary>A point on the rulebook <paramref name="inset"/> inside its edge where that edge lies across the passport (the day-12 case of BUG-40), at its top; throws when no edge of it lies over the passport.</summary>
    private static Vector3 FolderEdgePoint(DeskStampTray tray, DeskDocument passport, float inset, float t = 0.5f)
    {
        DeskRulebook folder = Folder() ?? throw new InvalidOperationException("stamp: no rulebook.");
        var f = Footprint(tray, folder);
        var p = Footprint(tray, passport, folder.transform.right);
        float bestLength = 0f, a = 0f, b = 0f;
        void Edge(float edge, float into, bool onA)
        {
            float pMin = onA ? p.minA : p.minB, pMax = onA ? p.maxA : p.maxB;
            float oMin = Mathf.Max(onA ? p.minB : p.minA, onA ? f.minB : f.minA), oMax = Mathf.Min(onA ? p.maxB : p.maxA, onA ? f.maxB : f.maxA);
            if (edge <= pMin || edge >= pMax || oMax - oMin <= bestLength)
                return;
            bestLength = oMax - oMin;
            float inside = edge + into * inset, mid = Mathf.Lerp(oMin, oMax, t);
            a = onA ? inside : mid;
            b = onA ? mid : inside;
        }
        Edge(f.minA, 1f, true);
        Edge(f.maxA, -1f, true);
        Edge(f.minB, 1f, false);
        Edge(f.maxB, -1f, false);
        if (bestLength <= 0f)
            throw new InvalidOperationException($"stamp: no edge of the rulebook lies over the passport (rulebook a {f.minA:0.000}..{f.maxA:0.000} b {f.minB:0.000}..{f.maxB:0.000}; passport a {p.minA:0.000}..{p.maxA:0.000} b {p.minB:0.000}..{p.maxB:0.000}).");
        Vector3 up = Up(tray);
        Transform s = Surface(tray);
        float c = (s != null ? Vector3.Dot(s.position, up) : 0f) + f.top;
        return f.across * a + f.along * b + up * c;
    }

    private static string V(Vector3 v) => string.Format(CultureInfo.InvariantCulture, "({0:0.0000},{1:0.0000},{2:0.0000})", v.x, v.y, v.z);

    private static string R(Rect? r) => r is Rect x ? string.Format(CultureInfo.InvariantCulture, "[{0:0},{1:0} .. {2:0},{3:0}]", x.xMin, x.yMin, x.xMax, x.yMax) : "off screen";

    private static string Mm(float metres) => (metres * 1000f).ToString("0.00", CultureInfo.InvariantCulture) + " mm";

    private static void Report(QaRunner runner, string text) => File.AppendAllText(Path.Combine(runner.OutDir, "stamp_report.txt"), text + "\n");

    /// <summary>Runs the drag at the fixed 30 fps game step (as the rig's own drag op does), then puts the frame rate back.</summary>
    private static IEnumerator FixedStep(IEnumerator body)
    {
        int old = Time.captureFramerate;
        bool here = old != SeqRecorder.Fps;
        if (here)
            Time.captureFramerate = SeqRecorder.Fps;
        try
        {
            while (body.MoveNext())
                yield return body.Current;
        }
        finally
        {
            if (here)
                Time.captureFramerate = old;
        }
    }

    private static Vector2 Screen2(Vector3 world, Camera cam)
    {
        Vector3 s = cam.WorldToScreenPoint(world);
        if (s.z <= 0f)
            throw new InvalidOperationException($"stamp: {V(world)} is behind the camera.");
        return new Vector2(s.x, s.y);
    }

    private static Vector2 Frac(float x, float y) => new Vector2(x * Screen.width, y * Screen.height);

    // ---------------- stampDump ----------------

    private static IEnumerator Dump(QaRunner runner, QaStep step)
    {
        DeskStampTray tray = Tray();
        Camera cam = Cam();
        var sb = new StringBuilder();
        sb.AppendLine($"== {step.name} at {Screen.width}x{Screen.height}, frame {Time.frameCount}, day {QaGame.Day()}");
        var view = QaGame.Field(tray, "deskView") as DeskView;
        sb.AppendLine($"   view on {(view != null && view.IsOn)}; camera {cam.name} at {V(cam.transform.position)} fov {cam.fieldOfView:0.0}");
        sb.AppendLine($"   tray: passport {tray.Passport}, verdict {tray.HasVerdict}, carrying {tray.IsCarrying}, moving {tray.IsMoving}, last {tray.LastPress} at {V(tray.LastPressPoint)}");
        List<MeshFilter> box = RackParts(tray, false), all = RackParts(tray, true);
        sb.AppendLine($"   rack {(tray.Rack != null ? QaGame.Where(tray.Rack.gameObject) : "none")} active {(tray.Rack != null && tray.Rack.gameObject.activeInHierarchy)}: box {R(ScreenRectOf(box.SelectMany(Corners), cam))}, with daters {R(ScreenRectOf(all.SelectMany(Corners), cam))}, parts {all.Count}");
        foreach (string which in new[] { "Denied", "Approved" })
        {
            Clickable c = Dater(tray, which);
            var drag = c != null ? c.GetComponent<DeskDraggable>() : null;
            sb.AppendLine($"   dater {which}: at {(c != null ? V(c.transform.position) : "-")} screen {(c != null ? QaGame.ScreenPoint(c.gameObject)?.ToString() : "-")} drag {(drag != null && drag.enabled)} die h {(Die(tray, which) is Transform d ? Mm(Height(tray, d.position)) : "-")}");
        }
        foreach (DeskDocument p in Papers())
        {
            Vector3 face = FacePoint(tray, p, 0.5f, 0.5f);
            sb.AppendLine($"   paper {p.Index} {p.Kind} zone {p.Zone} prints {Prints(p)} screen {R(QaGame.ScreenRect(p.gameObject))} face {V(face)} h {Mm(Height(tray, face))} colliders {string.Join(",", p.GetComponentsInChildren<Collider>(true).Select(c => $"{c.name}:{(c.enabled && c.gameObject.activeInHierarchy ? "on" : "off")}/{LayerMask.LayerToName(c.gameObject.layer)}{(c.isTrigger ? "/trigger" : string.Empty)}"))}");
        }
        DeskRulebook folder = Folder();
        if (folder != null)
        {
            sb.AppendLine($"   rulebook screen {R(QaGame.ScreenRect(folder.gameObject))} at {V(folder.transform.position)} top {Mm(Footprint(tray, folder).top)}");
            if (string.Equals(step.text, "folder", StringComparison.OrdinalIgnoreCase))
            {
                Vector3 up = Up(tray);
                Vector3 across = Vector3.ProjectOnPlane(folder.transform.right, up).normalized, along = Vector3.Cross(across, up);
                string Span(IEnumerable<Vector3> pts)
                {
                    float a0 = float.MaxValue, a1 = float.MinValue, b0 = float.MaxValue, b1 = float.MinValue;
                    foreach (Vector3 c in pts)
                    {
                        a0 = Mathf.Min(a0, Vector3.Dot(c, across)); a1 = Mathf.Max(a1, Vector3.Dot(c, across));
                        b0 = Mathf.Min(b0, Vector3.Dot(c, along)); b1 = Mathf.Max(b1, Vector3.Dot(c, along));
                    }
                    return $"a {a0:0.000}..{a1:0.000} b {b0:0.000}..{b1:0.000}";
                }
                IEnumerable<Vector3> Box(BoxCollider c)
                {
                    Vector3 h = c.size * 0.5f;
                    for (int i = 0; i < 8; i++)
                        yield return c.transform.TransformPoint(c.center + new Vector3((i & 1) == 0 ? -h.x : h.x, (i & 2) == 0 ? -h.y : h.y, (i & 4) == 0 ? -h.z : h.z));
                }
                foreach (BoxCollider bc in folder.GetComponentsInChildren<BoxCollider>(true))
                    sb.AppendLine($"      box {QaGame.Where(bc.gameObject).Split(new[] { ": " }, StringSplitOptions.None).Last()} active {bc.gameObject.activeInHierarchy} enabled {bc.enabled}: {Span(Box(bc))}");
                foreach (Renderer r in folder.GetComponentsInChildren<Renderer>(false))
                    if (r.enabled && r.GetComponent<TMPro.TMP_Text>() == null)
                    {
                        Bounds b = r.bounds;
                        sb.AppendLine($"      renderer {r.name}: {Span(Enumerable.Range(0, 8).Select(i => new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z)))}");
                    }
            }
        }
        foreach (Component m in Object.FindObjectsByType<ChipReader>(FindObjectsSortMode.None).Cast<Component>().Concat(Object.FindObjectsByType<DeskScanner>(FindObjectsSortMode.None)))
        {
            sb.AppendLine($"   {m.GetType().Name} {QaGame.Where(m.gameObject)} screen {R(QaGame.ScreenRect(m.gameObject))} top {Mm(Top(tray, m.gameObject))}");
            foreach (Collider c in m.GetComponentsInChildren<Collider>(true))
            {
                var cp = OnPlane(tray, c.bounds);
                sb.AppendLine($"      collider {c.name} {c.GetType().Name} enabled {c.enabled} active {c.gameObject.activeInHierarchy} trigger {c.isTrigger} layer {LayerMask.LayerToName(c.gameObject.layer)} x {cp.x0:0.000}..{cp.x1:0.000} z {cp.z0:0.000}..{cp.z1:0.000} top {Mm(cp.top)}");
            }
            if (string.Equals(step.text, "parts", StringComparison.OrdinalIgnoreCase))
                foreach (Renderer r in m.GetComponentsInChildren<Renderer>(false))
                {
                    var rp = OnPlane(tray, r.bounds);
                    sb.AppendLine($"      renderer {QaGame.Where(r.gameObject)} enabled {r.enabled} x {rp.x0:0.000}..{rp.x1:0.000} z {rp.z0:0.000}..{rp.z1:0.000} low {Mm(rp.low)} top {Mm(rp.top)}");
                }
        }
        if (string.Equals(step.text, "cases", StringComparison.OrdinalIgnoreCase) && QaGame.Find<GameManager>() is GameManager game && QaGame.Field(game, "_dayCases") is List<CaseInstance> cases)
            for (int i = 0; i < cases.Count; i++)
                sb.AppendLine($"   case {i + 1}: {cases[i].visitorDisplayName} accept {cases[i].ShouldAccept}, papers {cases[i].documents.Count} ({cases[i].documents.Count(d => d != null && DocumentHandOvers.IsRequested(d.handOver))} on request)");
        Report(runner, sb.ToString());
        yield return null;
    }

    // ---------------- stampRackWatch ----------------

    private sealed class Watch
    {
        public bool On;
        public string Name;
        public int Frames, Over;
        public float Max, Last;
        public string MaxDetail = "none";
    }

    private static Watch _watch;

    private static IEnumerator RackWatch(QaRunner runner, QaStep step)
    {
        string name = QaRunner.Safe(string.IsNullOrEmpty(step.name) ? "case" : step.name);
        if (step.n != 0)
        {
            if (_watch != null)
                _watch.On = false;
            var watch = new Watch { On = true, Name = name };
            _watch = watch;
            DeskStampTray tray = Tray();
            runner.Recorder.Track("StampRack", () => tray != null ? ScreenRectOf(RackParts(tray, true).SelectMany(Corners), Cam()) : null);
            for (int i = 0; i < 8; i++)
            {
                int index = i;
                runner.Recorder.Track("paper" + index, () => Papers().FirstOrDefault(p => p.Index == index) is DeskDocument p ? QaGame.ScreenRect(p.gameObject) : null);
            }
            runner.Recorder.Column("rackOverPaper", () => (_watch != null ? _watch.Last : 0f).ToString("0", CultureInfo.InvariantCulture));
            runner.StartCoroutine(WatchLoop(watch, tray));
            yield return null;
            yield break;
        }
        if (_watch == null || _watch.Name != name)
            throw new InvalidOperationException($"stampRackWatch: no watch named '{name}' runs.");
        Watch w = _watch;
        w.On = false;
        _watch = null;
        yield return null;
        runner.Assert($"rackOverPaper.{name}", Owner, w.Frames > 0 && w.Over == 0,
            $"{w.Over} of {w.Frames} frames, max {w.Max:0} px",
            $"rack pixels over a paper (rays every {Step():0.#} px; a ray meeting the rack nearer than the paper): worst {w.MaxDetail}");
    }

    private static float Step() => Mathf.Max(2f, Screen.height / 180f);

    private static IEnumerator WatchLoop(Watch w, DeskStampTray tray)
    {
        var end = new WaitForEndOfFrame();
        while (w.On)
        {
            yield return end;
            if (!w.On || tray == null)
                break;
            Camera cam = Cam();
            string detail;
            float px = RackOverPapers(tray, cam, out detail);
            w.Frames++;
            w.Last = px;
            if (px > 0f)
            {
                w.Over++;
                if (px > w.Max)
                {
                    w.Max = px;
                    w.MaxDetail = $"frame {Time.frameCount} at {Screen.width}x{Screen.height}: {detail}";
                }
            }
        }
    }

    /// <summary>The rack's pixels over the papers this frame (approximate pixel area), with which papers.</summary>
    private static float RackOverPapers(DeskStampTray tray, Camera cam, out string detail)
    {
        detail = string.Empty;
        // The daters belong to the rack while they stand in it; one carried or stroking is the player's hand.
        List<MeshFilter> parts = RackParts(tray, !tray.IsCarrying && !tray.IsMoving);
        Rect? rack = ScreenRectOf(parts.SelectMany(Corners), cam);
        if (rack == null)
            return 0f;
        float step = Step(), area = 0f;
        var sb = new StringBuilder();
        foreach (DeskDocument paper in Papers())
        {
            Rect? pr = ScreenRectOf(BoxCorners(paper), cam);
            if (pr == null || !pr.Value.Overlaps(rack.Value))
                continue;
            Rect i = Rect.MinMaxRect(Mathf.Max(pr.Value.xMin, rack.Value.xMin), Mathf.Max(pr.Value.yMin, rack.Value.yMin), Mathf.Min(pr.Value.xMax, rack.Value.xMax), Mathf.Min(pr.Value.yMax, rack.Value.yMax));
            int covered = 0;
            for (float y = i.yMin + step / 2f; y < i.yMax; y += step)
                for (float x = i.xMin + step / 2f; x < i.xMax; x += step)
                {
                    Ray ray = cam.ScreenPointToRay(new Vector3(x, y, 0f));
                    if (!HitBoxes(paper, ray, out float pd))
                        continue;
                    if (HitParts(parts, ray, out float rd) && rd < pd - 1e-4f)
                        covered++;
                }
            if (covered > 0)
            {
                float a = covered * step * step;
                area += a;
                sb.Append($"paper {paper.Index} ({paper.Zone}) {a:0} px; ");
            }
        }
        detail = sb.Length > 0 ? sb.ToString() : "none";
        return area;
    }

    // ---------------- set-ups: stampPaper, stampFolderOver ----------------

    private static IEnumerator MovePaper(QaRunner runner, QaStep step)
    {
        DeskStampTray tray = Tray();
        Camera cam = Cam();
        DeskDocument paper = Paper(tray, step.n);
        yield return QaGame.Until(() => !paper.IsSliding, 10f, $"paper {paper.Index} to settle");
        Vector2 from = GrabPoint(tray, paper, cam);
        Vector2 to = Frac(step.x2, step.y2);
        QaPath path = QaPaths.Drag(QaRunner.ParseKind(step.path), from, to, new Vector2(Screen.width, Screen.height));
        yield return FixedStep(Settle(runner.Input.Play(path), 45));
        Report(runner, $"== stampPaper {step.name}: paper {paper.Index} from {from} released at {path.End}; now {R(QaGame.ScreenRect(paper.gameObject))} zone {paper.Zone}");
    }

    private static IEnumerator Settle(IEnumerator drag, int frames)
    {
        yield return drag;
        yield return QaGame.Frames(frames);
    }

    /// <summary>
    /// Drags the rulebook (the last thing touched lies on top) so its edge facing the passport lies f (default 3 cm)
    /// over it. Its grab is a point of its top no paper covers; the drags are P-SLOW (the carried rulebook trails a
    /// fast pointer and lands short). A move too short to be a drag (the EventSystem's threshold) goes out 6 cm first
    /// and back; the drop is checked (the edge 15-60 mm over the passport, the rulebook on top there) and corrected up
    /// to three times.
    /// </summary>
    private static IEnumerator FolderOver(QaRunner runner, QaStep step)
    {
        DeskStampTray tray = Tray();
        DeskRulebook folder = Folder() ?? throw new InvalidOperationException("stampFolderOver: no rulebook.");
        DeskDocument passport = Paper(tray, -1);
        float overlap = step.f > 0f ? step.f : 0.03f;
        var log = new StringBuilder($"== stampFolderOver {step.name}: ");
        string state = "not checked";
        for (int round = 0; round < 4; round++)
        {
            Camera cam = Cam();
            var f = Footprint(tray, folder);
            var p = Footprint(tray, passport, folder.transform.right);
            float fa = (f.minA + f.maxA) / 2f, fb = (f.minB + f.maxB) / 2f, pa = (p.minA + p.maxA) / 2f, pb = (p.minB + p.maxB) / 2f;
            bool onA = Mathf.Abs(pa - fa) - (f.maxA - f.minA + p.maxA - p.minA) / 2f >= Mathf.Abs(pb - fb) - (f.maxB - f.minB + p.maxB - p.minB) / 2f;
            float sign = onA ? Mathf.Sign(pa - fa) : Mathf.Sign(pb - fb);
            // How far the rulebook's edge facing the passport reaches over it now, and the move that makes it overlap.
            float now = onA ? (sign > 0f ? f.maxA - p.minA : p.maxA - f.minA) : (sign > 0f ? f.maxB - p.minB : p.maxB - f.minB);
            float da = onA ? sign * (overlap - now) : pa - fa, db = onA ? pb - fb : sign * (overlap - now);
            Vector3 delta = f.across * da + f.along * db;
            Vector3 edgeMid = f.across * (onA ? (sign > 0f ? f.maxA - now / 2f : f.minA + now / 2f) : (Mathf.Max(f.minA, p.minA) + Mathf.Min(f.maxA, p.maxA)) / 2f)
                              + f.along * (onA ? (Mathf.Max(f.minB, p.minB) + Mathf.Min(f.maxB, p.maxB)) / 2f : (sign > 0f ? f.maxB - now / 2f : f.minB + now / 2f))
                              + Up(tray) * ((Surface(tray) is Transform s ? Vector3.Dot(s.position, Up(tray)) : 0f) + f.top);
            bool onTop = OnTopAt(folder, passport, edgeMid, cam);
            state = $"overlap {Mm(now)} (asked {Mm(overlap)}), rulebook on top {onTop}";
            if (round > 0 && now >= 0.015f && now <= 0.06f && onTop)
                break;
            // The drag keeps the grab's offset on the desk's plane (DeskDraggable): the pointer's point on the plane moves with the rulebook.
            (Vector2 from, Vector3 grab) = FolderGrab(tray, folder, cam);
            grab = OnDeskPlane(tray, cam, from) ?? grab;
            Vector2 to = Screen2(grab + delta, cam);
            if ((to - from).magnitude < 40f)
            {
                // Out 6 cm, away from the passport, and back: a short move is a click, not a drag.
                Vector3 away = (onA ? f.across : f.along) * (-sign * 0.06f);
                QaPath outPath = QaPaths.Drag(QaPathKind.Slow, from, Screen2(grab + away, cam), new Vector2(Screen.width, Screen.height));
                yield return FixedStep(Settle(runner.Input.Play(outPath), 20));
                yield return StandsStill(folder.transform);
                log.Append($"out by {V(away)}; ");
                (from, grab) = FolderGrab(tray, folder, cam);
                grab = OnDeskPlane(tray, cam, from) ?? grab;
                to = Screen2(grab - away + delta, cam);
            }
            QaPath path = QaPaths.Drag(QaPathKind.Slow, from, to, new Vector2(Screen.width, Screen.height));
            yield return FixedStep(Settle(runner.Input.Play(path), 45));
            yield return StandsStill(folder.transform);
            log.Append($"round {round + 1}: {state}, moved by {V(delta)} ({from} to {path.End}); ");
        }
        Report(runner, log + "then " + state);
    }

    /// <summary>Where the pointer at <paramref name="screen"/> meets the desk's plane (DeskSurface.TryProject), or null.</summary>
    private static Vector3? OnDeskPlane(DeskStampTray tray, Camera cam, Vector2 screen) =>
        Surface(tray) is Transform s && s.GetComponent<DeskSurface>() is DeskSurface ds && ds.TryProject(cam, screen, out Vector3 w) ? w : (Vector3?)null;

    /// <summary>Waits until <paramref name="t"/> stands still (10 frames without a move over 0.05 mm or 0.05 degree), at most <paramref name="seconds"/>: a dropped rulebook keeps sliding a little after it lands.</summary>
    private static IEnumerator StandsStill(Transform t, float seconds = 4f)
    {
        float until = Time.realtimeSinceStartup + seconds;
        Vector3 p = t.position;
        Quaternion r = t.rotation;
        int still = 0;
        while (still < 10 && Time.realtimeSinceStartup < until)
        {
            yield return null;
            bool moved = (t.position - p).sqrMagnitude > 2.5e-9f || Quaternion.Angle(t.rotation, r) > 0.05f;
            still = moved ? 0 : still + 1;
            p = t.position;
            r = t.rotation;
        }
    }

    /// <summary>True when along the camera's ray through <paramref name="world"/> the rulebook comes before the passport (it lies over it there).</summary>
    private static bool OnTopAt(DeskRulebook folder, DeskDocument passport, Vector3 world, Camera cam)
    {
        Ray r = cam.ScreenPointToRay(cam.WorldToScreenPoint(world));
        return HitBoxes(folder, r, out float fd) && HitBoxes(passport, r, out float pd) && fd < pd;
    }

    /// <summary>A point of the rulebook's top a player would press to pick it up (no paper over it, on the screen; its middle when that is one) and where it is in the world.</summary>
    private static (Vector2 screen, Vector3 world) FolderGrab(DeskStampTray tray, DeskRulebook folder, Camera cam)
    {
        var f = Footprint(tray, folder);
        float c = (Surface(tray) is Transform s ? Vector3.Dot(s.position, Up(tray)) : 0f) + f.top;
        (Vector2, Vector3)? best = null;
        float bestCost = float.MaxValue;
        for (int i = 1; i < 10; i++)
            for (int j = 1; j < 10; j++)
            {
                float u = i / 10f, v = j / 10f, cost = Mathf.Abs(u - 0.5f) + Mathf.Abs(v - 0.5f);
                if (cost >= bestCost)
                    continue;
                Vector3 w = f.across * Mathf.Lerp(f.minA, f.maxA, u) + f.along * Mathf.Lerp(f.minB, f.maxB, v) + Up(tray) * c;
                Vector3 sp = cam.WorldToScreenPoint(w);
                if (sp.z <= 0f || sp.x < 12f || sp.y < 12f || sp.x > Screen.width - 12f || sp.y > Screen.height - 12f)
                    continue;
                Ray r = cam.ScreenPointToRay(sp);
                if (!HitBoxes(folder, r, out float mine) || Papers().Any(p => HitBoxes(p, r, out float d) && d < mine))
                    continue;
                best = (new Vector2(sp.x, sp.y), r.GetPoint(mine));
                bestCost = cost;
            }
        return best ?? throw new InvalidOperationException("stampFolderOver: no part of the rulebook shows uncovered on the screen.");
    }

    // ---------------- stampAim, stampRegrab ----------------

    private static IEnumerator Aim(QaRunner runner, QaStep step, bool regrab)
    {
        DeskStampTray tray = Tray();
        Camera cam = Cam();
        string name = QaRunner.Safe(string.IsNullOrEmpty(step.name) ? "try" : step.name);
        string which = Which(step.text);
        Clickable dater = Dater(tray, which);
        Transform die = Die(tray, which);
        DeskDraggable drag = dater.GetComponent<DeskDraggable>();
        yield return QaGame.Until(() => drag.enabled && !tray.IsMoving && !tray.IsCarrying, 10f, $"the {which} dater to take input");
        DeskDocument passport = Paper(tray, -1);
        DeskDocument aimed = Paper(tray, step.n);
        yield return QaGame.Until(() => Papers().All(x => !x.IsSliding), 10f, "the papers to settle");
        if (Folder() is DeskRulebook settling)
            yield return StandsStill(settling.transform);

        bool folderEdge = string.Equals(step.target, "folderEdge", StringComparison.OrdinalIgnoreCase);
        bool stack = string.Equals(step.target, "stack", StringComparison.OrdinalIgnoreCase);
        float fx = step.x == 0f && step.y == 0f ? 0.5f : step.x, fy = step.x == 0f && step.y == 0f ? 0.5f : step.y;
        float inset = step.f > 0f ? step.f : 0.008f;
        Vector3 aimWorld;
        try
        {
            aimWorld = folderEdge ? FolderEdgePoint(tray, aimed, inset) : FacePoint(tray, aimed, fx, fy);
        }
        catch (InvalidOperationException e)
        {
            runner.Assert($"aim.{name}", Owner, false, "set-up failed", e.Message);
            Report(runner, $"== aim.{name}: FAIL set-up: {e.Message}");
            yield break;
        }
        // A point the player can press: on the screen, and there the aimed paper on top (on a stack, over two more papers;
        // at the rulebook's edge, under it). The requested point when it is one, else the nearest that is.
        bool Pressable(Vector3 w)
        {
            Vector3 sp = cam.WorldToScreenPoint(w);
            const float margin = 24f;
            if (sp.z <= 0f || sp.x < margin || sp.y < margin || sp.x > Screen.width - margin || sp.y > Screen.height - margin)
                return false;
            Ray r = cam.ScreenPointToRay(sp);
            var hits = new List<(DeskDocument paper, float distance)>();
            foreach (DeskDocument p in Papers())
                if (HitBoxes(p, r, out float d))
                    hits.Add((p, d));
            hits.Sort((x, y) => x.distance.CompareTo(y.distance));
            float folderAt = Folder() is DeskRulebook rb2 && HitBoxes(rb2, r, out float fd2) ? fd2 : float.MaxValue;
            if (hits.Count == 0 || hits[0].paper != aimed)
                return false;
            if (folderEdge)
                return folderAt < hits[0].distance;
            return folderAt > hits[0].distance && (!stack || hits.Count >= 3);
        }
        string moved = string.Empty;
        if (!Pressable(aimWorld))
        {
            var options = new List<(float cost, Vector3 world, string what)>();
            if (folderEdge)
                for (int i = 0; i <= 20; i++)
                {
                    float t = i / 20f;
                    options.Add((Mathf.Abs(t - 0.5f), FolderEdgePoint(tray, aimed, inset, t), $"t {t:0.00} along the edge"));
                }
            else
                for (int i = 1; i < 20; i++)
                    for (int j = 1; j < 20; j++)
                    {
                        float u = i / 20f, v = j / 20f;
                        options.Add((Mathf.Abs(u - fx) + Mathf.Abs(v - fy), FacePoint(tray, aimed, u, v), $"({u:0.00}, {v:0.00}) of its face"));
                    }
            foreach (var o in options.OrderBy(o => o.cost))
                if (Pressable(o.world))
                {
                    aimWorld = o.world;
                    moved = $"; the requested point was not pressable, aimed at {o.what}";
                    break;
                }
        }
        if (folderEdge && Folder() is DeskRulebook edgeBook)
        {
            // The rulebook's top is not level (it may lie on a pile) nor at its tallest box's height: move the aim until the
            // ray through it meets the rulebook at the planned place on the desk's plane (the inset from its edge holds).
            Vector3 up = Up(tray), across = Vector3.ProjectOnPlane(edgeBook.transform.right, up).normalized, along = Vector3.Cross(across, up);
            float wantA = Vector3.Dot(aimWorld, across), wantB = Vector3.Dot(aimWorld, along);
            var fp = Footprint(tray, edgeBook);
            var pp = Footprint(tray, aimed, edgeBook.transform.right);
            moved += $"; rulebook a {fp.minA:0.0000}..{fp.maxA:0.0000} b {fp.minB:0.0000}..{fp.maxB:0.0000}, passport a {pp.minA:0.0000}..{pp.maxA:0.0000} b {pp.minB:0.0000}..{pp.maxB:0.0000}, aimed at a {wantA:0.0000} b {wantB:0.0000}";
            for (int it = 0; it < 5; it++)
            {
                Ray r = cam.ScreenPointToRay(cam.WorldToScreenPoint(aimWorld));
                if (!HitBoxes(edgeBook, r, out float d))
                    break;
                Vector3 q = r.GetPoint(d);
                float ea = wantA - Vector3.Dot(q, across), eb = wantB - Vector3.Dot(q, along);
                if (Mathf.Abs(ea) + Mathf.Abs(eb) < 0.0002f)
                    break;
                aimWorld += across * ea + along * eb;
            }
            Ray last = cam.ScreenPointToRay(cam.WorldToScreenPoint(aimWorld));
            if (HitBoxes(edgeBook, last, out float ld))
            {
                Vector3 q = last.GetPoint(ld);
                moved += $", the ray meets it at a {Vector3.Dot(q, across):0.0000} b {Vector3.Dot(q, along):0.0000} (the tray's edge distance there {Mm(TrayEdgeDistance(tray, edgeBook, q))})";
            }
        }
        Vector2 to = Screen2(aimWorld, cam);
        Vector2 from = QaGame.ScreenPoint(dater.gameObject) ?? throw new InvalidOperationException($"the {which} dater is off the screen.");
        QaPath path = QaPaths.Drag(QaRunner.ParseKind(step.path), from, to, new Vector2(Screen.width, Screen.height));

        // What lies under the pointer's spot at the release, as the player sees it.
        Ray look = cam.ScreenPointToRay(path.End);
        var under = new List<(DeskDocument paper, float distance)>();
        foreach (DeskDocument p in Papers())
            if (HitBoxes(p, look, out float d))
                under.Add((p, d));
        under.Sort((x, y) => x.distance.CompareTo(y.distance));
        float folderDistance = Folder() is DeskRulebook rb && HitBoxes(rb, look, out float fdist) ? fdist : float.MaxValue;
        var setup = new List<string>();
        bool aimedThere = HitBoxes(aimed, look, out float aimedDistance);
        var seen = new StringBuilder();
        foreach (RaycastHit h in Physics.RaycastAll(look, 10f, ((LayerMask)QaGame.Field(tray, "paperLayers")).value, QueryTriggerInteraction.Collide).OrderBy(h => h.distance))
            seen.Append($"{h.collider.name}@{h.distance:0.0000}{(h.collider.GetComponentInParent<DeskDocument>() is DeskDocument dd ? $"(paper {dd.Index})" : h.collider.GetComponentInParent<DeskRulebook>() != null ? "(rulebook)" : string.Empty)} ");
        if (folderEdge && !(aimedThere && folderDistance < aimedDistance))
            setup.Add("the rulebook does not lie over the passport at the aim point");
        if (stack && (under.Count < 3 || under[0].paper != aimed))
            setup.Add($"not a 3-paper stack with the aimed paper on top at the aim point ({under.Count} papers: {string.Join(",", under.Select(u => u.paper.Index))})");
        Vector3 expected = look.GetPoint(aimedDistance);

        bool hadVerdict = tray.HasVerdict;
        int passportPrints = Prints(passport), allPrints = AllPrints();
        var sounds = new List<string>();
        Action<string, bool> hear = (id, clip) => sounds.Add(id);
        Sounds.Fired += hear;
        float minFace = float.MaxValue, minBody = float.MaxValue;
        string dieAtRelease = "-";
        int carriedDuringStroke = 0, regrabbed = 0;
        bool strokeSeen = false;
        string dragged = "nothing";
        try
        {
            yield return FixedStep(TryStroke());
        }
        finally
        {
            Sounds.Fired -= hear;
        }

        IEnumerator TryStroke()
        {
            yield return runner.Input.Play(path, i =>
            {
                if (i == path.ReleaseIndex && die != null)
                {
                    dieAtRelease = $"{V(die.position)} ({Mm(Vector3.ProjectOnPlane(die.position - expected, Up(tray)).magnitude)} beside the expected point)";
                    if (folderEdge && Folder() is DeskRulebook rbNow)
                    {
                        var fr = Footprint(tray, rbNow);
                        dieAtRelease += $"; the rulebook then a {fr.minA:0.0000}..{fr.maxA:0.0000} b {fr.minB:0.0000}..{fr.maxB:0.0000} at {V(rbNow.transform.position)}";
                    }
                }
            });
            // The let-go is seen on the frame after the release sample; then the stroke (rise, slam, return).
            int frames = 0;
            int regrabAt = regrab ? 2 : -1;
            while (frames < 240)
            {
                frames++;
                if (tray.IsMoving)
                    strokeSeen = true;
                minFace = Mathf.Min(minFace, DieLow(tray, die));
                foreach (Renderer r in dater.GetComponentsInChildren<Renderer>(false))
                    if (r.enabled && r.GetComponent<TMPro.TMP_Text>() == null)
                        minBody = Mathf.Min(minBody, Lowest(tray, r.bounds));
                if (strokeSeen && tray.IsCarrying)
                    carriedDuringStroke++;
                if (frames == regrabAt)
                {
                    // R4 (c): the player presses the dater again within 100 ms of the let-go and drags.
                    Vector2 at = QaGame.ScreenPoint(dater.gameObject) ?? path.End;
                    QaPath again = QaPaths.Drag(QaPathKind.Fast, at, at + new Vector2(90f, 40f), new Vector2(Screen.width, Screen.height));
                    regrabbed++;
                    IEnumerator play = runner.Input.Play(again);
                    while (play.MoveNext())
                    {
                        yield return play.Current;
                        frames++;
                        if (tray.IsMoving)
                            strokeSeen = true;
                        minFace = Mathf.Min(minFace, DieLow(tray, die));
                        if (tray.IsCarrying && tray.IsMoving)
                            carriedDuringStroke++;
                        if (QaGame.Find<DeskController>() is DeskController dc && dc.IsDragging)
                            dragged = "a paper (the press fell through the dater to the paper under it)";
                    }
                    continue;
                }
                if (frames > 3 && !tray.IsMoving && !tray.IsCarrying)
                    break;
                yield return null;
            }
        }

        StampPress press = tray.LastPress;
        Vector3 landed = tray.LastPressPoint;
        string edge = Folder() is DeskRulebook book ? $"; the tray's folder edge distance at the landing {Mm(TrayEdgeDistance(tray, book, landed))}, at the expected point {Mm(TrayEdgeDistance(tray, book, look.GetPoint(folderDistance < float.MaxValue ? folderDistance : aimedDistance)))}; die at the let-go {dieAtRelease}" : string.Empty;
        float off = (landed - expected).magnitude;
        int newPassportPrints = Prints(passport) - passportPrints, newPrints = AllPrints() - allPrints;
        float surface = Height(tray, landed);
        float sink = surface - minFace;
        var fails = new List<string>(setup);
        if (!aimedThere)
            fails.Add("the pointer's spot was not on the aimed paper");
        string picked = Nearest(landed, out float nearest);
        bool aimedIsPassport = aimed == passport;
        StampPress want = !aimedIsPassport ? StampPress.NotPassport : hadVerdict ? StampPress.AlreadyStamped : StampPress.Stamped;
        if (press != want)
            fails.Add($"press {press}, expected {want}");
        if (off > 0.003f)
            fails.Add($"landed {Mm(off)} from the pointer's spot on the aimed paper (max 3 mm)");
        if (picked != $"paper {aimed.Index}")
            fails.Add($"the press point lies on {picked}, not paper {aimed.Index}");
        string ink = "-";
        if (press == StampPress.Stamped)
        {
            if (newPassportPrints != 1 || newPrints != 1)
                fails.Add($"{newPassportPrints} print(s) on the passport, {newPrints} in all (exactly 1 expected)");
            ink = InkOf(passport);
            string expectInk = IsDenied(which) ? "denied" : "approved";
            if (ink != expectInk)
                fails.Add($"ink {ink}, verdict {expectInk}");
            if (Mathf.Abs(sink) > 0.0005f)
                fails.Add($"the die's face stopped {Mm(-sink)} from the surface (max 0.5 mm)");
        }
        else if (newPrints != 0)
            fails.Add($"{newPrints} print(s) from a refused press");
        if (regrab)
        {
            if (carriedDuringStroke > 0)
                fails.Add($"the re-grab carried the dater during the stroke ({carriedDuringStroke} frames)");
            if (regrabbed == 0)
                fails.Add("the re-grab did not happen");
        }
        string measured = $"{press}, {Mm(off)} off, sink {(press == StampPress.Stamped ? Mm(-sink) : "-")}, prints +{newPrints}, ink {ink}";
        string detail = $"{which} dater, {QaRunner.ParseKind(step.path)} path to paper {aimed.Index}{(folderEdge ? " at the rulebook's edge" : stack ? " on a stack" : string.Empty)} at {path.End} ({Screen.width}x{Screen.height}){moved}; under the spot: {string.Join(", ", under.Select(u => $"paper {u.paper.Index} at {u.distance:0.0000}"))}{(folderDistance < float.MaxValue ? $", rulebook at {folderDistance:0.0000}" : string.Empty)}; physics along the ray: {seen}; landed {V(landed)} on {picked} ({Mm(nearest)}), expected {V(expected)}; die face min {Mm(minFace)}, body min {Mm(minBody)}, surface {Mm(surface)}{edge}{(regrab ? $"; re-grab: {dragged}, carried during the stroke {carriedDuringStroke} frames" : string.Empty)}; cues {string.Join(",", sounds.Distinct())}" +
                        (fails.Count > 0 ? "; FAIL: " + string.Join("; ", fails) : string.Empty);
        runner.Assert($"aim.{name}", Owner, fails.Count == 0, measured, detail);
        Report(runner, $"== aim.{name}: {(fails.Count == 0 ? "PASS" : "FAIL")} {measured}; {detail}");
    }

    /// <summary>The tray's own measure (DeskStampTray.EdgeDistance, the enabled boxes only): how far inside the rulebook's footprint <paramref name="world"/> lies, metres.</summary>
    private static float TrayEdgeDistance(DeskStampTray tray, Component thing, Vector3 world)
    {
        object v = typeof(DeskStampTray).GetMethod("EdgeDistance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(tray, new object[] { thing, world });
        return v is float f ? f : float.NaN;
    }

    /// <summary>Which paper (or the rulebook) <paramref name="point"/> lies on: the one whose box is nearest.</summary>
    private static string Nearest(Vector3 point, out float distance)
    {
        string best = "nothing";
        distance = float.MaxValue;
        foreach (DeskDocument p in Papers())
        {
            float d = BoxDistance(p, point);
            if (d < distance)
            {
                distance = d;
                best = $"paper {p.Index}";
            }
        }
        if (Folder() is DeskRulebook rb)
        {
            float d = BoxDistance(rb, point);
            if (d < distance)
            {
                distance = d;
                best = "the rulebook";
            }
        }
        return best;
    }

    private static float BoxDistance(Component thing, Vector3 point)
    {
        float best = float.MaxValue;
        foreach (BoxCollider box in thing.GetComponentsInChildren<BoxCollider>(true))
        {
            if (!box.gameObject.activeInHierarchy)
                continue;
            Vector3 local = box.transform.InverseTransformPoint(point) - box.center;
            Vector3 h = box.size * 0.5f;
            Vector3 clamped = new Vector3(Mathf.Clamp(local.x, -h.x, h.x), Mathf.Clamp(local.y, -h.y, h.y), Mathf.Clamp(local.z, -h.z, h.z));
            float d = (box.transform.TransformPoint(clamped + box.center) - point).magnitude;
            best = Mathf.Min(best, d);
        }
        return best;
    }

    /// <summary>The die's lowest point over the desk's plane (its rubber's bounds; the die's own point without a renderer), metres.</summary>
    private static float DieLow(DeskStampTray tray, Transform die)
    {
        if (die == null)
            return float.MaxValue;
        float low = float.MaxValue;
        foreach (Renderer r in die.GetComponentsInChildren<Renderer>(false))
            if (r.enabled)
                low = Mathf.Min(low, Lowest(tray, r.bounds));
        return low < float.MaxValue ? low : Height(tray, die.position);
    }

    private static float Lowest(DeskStampTray tray, Bounds b)
    {
        float low = float.MaxValue;
        for (int i = 0; i < 8; i++)
            low = Mathf.Min(low, Height(tray, new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z)));
        return low;
    }

    private static float Top(DeskStampTray tray, GameObject go)
    {
        float top = float.MinValue;
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>(false))
        {
            if (!r.enabled || r.GetComponent<TMPro.TMP_Text>() != null)
                continue;
            Bounds b = r.bounds;
            for (int i = 0; i < 8; i++)
                top = Mathf.Max(top, Height(tray, new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z)));
        }
        return top;
    }

    /// <summary>The ink of <paramref name="paper"/>'s last print: "approved" or "denied" by which ink most of its pixels are (the impression is not readable, so it is drawn into a small render texture first).</summary>
    private static string InkOf(DeskDocument paper)
    {
        var impressions = QaGame.Field(paper, "_impressions") as List<Texture2D>;
        Texture2D tex = impressions != null && impressions.Count > 0 ? impressions[impressions.Count - 1] : null;
        if (tex == null)
            return "none";
        RenderTexture rt = RenderTexture.GetTemporary(128, 64, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        RenderTexture old = RenderTexture.active;
        var read = new Texture2D(128, 64, TextureFormat.RGBA32, false);
        try
        {
            Graphics.Blit(tex, rt);
            RenderTexture.active = rt;
            read.ReadPixels(new Rect(0, 0, 128, 64), 0, 0);
            read.Apply();
        }
        finally
        {
            RenderTexture.active = old;
            RenderTexture.ReleaseTemporary(rt);
        }
        int approved = 0, denied = 0;
        foreach (Color32 c in read.GetPixels32())
        {
            if (c.a < 128)
                continue;
            int da = Dist(c, DaterImpressionArt.ApprovedInk), dd = Dist(c, DaterImpressionArt.DeniedInk);
            if (Mathf.Min(da, dd) > 90)
                continue;
            if (da < dd) approved++; else denied++;
        }
        Object.Destroy(read);
        return approved == 0 && denied == 0 ? "unreadable" : approved > denied ? "approved" : "denied";
    }

    private static int Dist(Color32 a, Color32 b) => Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);

    // ---------------- stampLetGo ----------------

    /// <summary>The parts of a prop that the let-go is judged against: the rack's box (no daters), or a machine's renderers.</summary>
    private static List<Bounds> PropParts(DeskStampTray tray, string over, out GameObject prop)
    {
        var parts = new List<Bounds>();
        prop = null;
        if (over == "rack")
        {
            prop = tray.Rack != null ? tray.Rack.gameObject : null;
            foreach (MeshFilter mf in RackParts(tray, false))
                parts.Add(mf.GetComponent<Renderer>().bounds);
        }
        else if (over == "chip" || over == "scanner")
        {
            Component m = over == "chip" ? (Component)Object.FindAnyObjectByType<ChipReader>() : Object.FindAnyObjectByType<DeskScanner>();
            if (m == null || !m.gameObject.activeInHierarchy)
                throw new InvalidOperationException($"stampLetGo: no {over} on the desk (give the items first).");
            prop = m.gameObject;
            foreach (Renderer r in m.GetComponentsInChildren<Renderer>(false))
                if (r.enabled && r.GetComponent<TMPro.TMP_Text>() == null)
                    parts.Add(r.bounds);
        }
        return parts;
    }

    /// <summary>A world box's extent on the desk plane (along the desk's right and forward), its top and its bottom over the plane.</summary>
    private static (float x0, float x1, float z0, float z1, float top, float low) OnPlane(DeskStampTray tray, Bounds b)
    {
        Transform s = Surface(tray);
        Vector3 right = s != null ? s.right : Vector3.right, fwd = s != null ? s.forward : Vector3.forward;
        float x0 = float.MaxValue, x1 = float.MinValue, z0 = float.MaxValue, z1 = float.MinValue, top = float.MinValue, low = float.MaxValue;
        for (int i = 0; i < 8; i++)
        {
            var c = new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
            float x = Vector3.Dot(c, right), z = Vector3.Dot(c, fwd), h = Height(tray, c);
            x0 = Mathf.Min(x0, x); x1 = Mathf.Max(x1, x);
            z0 = Mathf.Min(z0, z); z1 = Mathf.Max(z1, z);
            top = Mathf.Max(top, h); low = Mathf.Min(low, h);
        }
        return (x0, x1, z0, z1, top, low);
    }

    /// <summary>
    /// R4 (d): a dater dragged past the threshold and let go over the rack's box, the chip reader, the scanner or the
    /// bare desk. letGo.&lt;name&gt; judges the let-go (finding 1): over a prop no stroke starts (the handle's stroke
    /// stays None, the die does not go down from where it was let go before it heads home), no cue, LastPress Nothing,
    /// and the dater seats home; over the bare desk stamp_miss plays once, the die's face reaches the desk's plane
    /// within 0.5 mm, the B4 bounce peaks at 6 ±1 mm, no print. letGoCarry.&lt;name&gt; is the recipe's literal line:
    /// in no frame from the press to the seat (the seated dater in its own cradle left out) does a part of the dater
    /// go below the top of the prop's parts under its footprint.
    /// </summary>
    private static IEnumerator LetGo(QaRunner runner, QaStep step)
    {
        DeskStampTray tray = Tray();
        Camera cam = Cam();
        string name = QaRunner.Safe(string.IsNullOrEmpty(step.name) ? "letgo" : step.name);
        string which = Which(step.text);
        Clickable dater = Dater(tray, which);
        Transform die = Die(tray, which);
        DeskDraggable drag = dater.GetComponent<DeskDraggable>();
        yield return QaGame.Until(() => drag.enabled && !tray.IsMoving && !tray.IsCarrying, 10f, $"the {which} dater to take input");
        object handle = Handle(tray, which);
        Vector3 home = (Vector3)QaGame.Field(handle, "Home");
        float perMetre = PerMetre(dater.transform);
        string over = (step.target ?? "desk").Trim().ToLowerInvariant();
        List<Bounds> parts = PropParts(tray, over, out GameObject prop);
        // The dater's own place in its cradle (its footprint seated): over it, the dater is home, not over the prop.
        float hx0 = float.MaxValue, hx1 = float.MinValue, hz0 = float.MaxValue, hz1 = float.MinValue;
        foreach (Renderer r in dater.GetComponentsInChildren<Renderer>(false))
        {
            if (!r.enabled || r.GetComponent<TMPro.TMP_Text>() != null)
                continue;
            var p = OnPlane(tray, r.bounds);
            hx0 = Mathf.Min(hx0, p.x0); hx1 = Mathf.Max(hx1, p.x1);
            hz0 = Mathf.Min(hz0, p.z0); hz1 = Mathf.Max(hz1, p.z1);
        }
        Vector3 up = Up(tray);
        Vector3 Floor(Vector3 w) => w - up * Height(tray, w);
        Vector3 floorAim;
        if (prop != null)
        {
            if (parts.Count == 0)
                throw new InvalidOperationException($"stampLetGo: the {over} has no parts.");
            Bounds all = parts[0];
            foreach (Bounds b in parts)
                all.Encapsulate(b);
            floorAim = Floor(all.center);
        }
        else if (!(Surface(tray) is Transform s && s.GetComponent<DeskSurface>() is DeskSurface ds && ds.TryProject(cam, Frac(step.x, step.y), out floorAim)))
            throw new InvalidOperationException("stampLetGo: the point is not on the desk.");
        Vector2 to = Screen2(floorAim, cam);
        Vector2 from = QaGame.ScreenPoint(dater.gameObject) ?? throw new InvalidOperationException($"the {which} dater is off the screen.");
        QaPath path = QaPaths.Drag(QaRunner.ParseKind(step.path), from, to, new Vector2(Screen.width, Screen.height));
        // What stands at the let-go point (colliders over 2 mm in a 3 cm column), for the report.
        var standing = Physics.OverlapBox(floorAim + up * 0.02f, new Vector3(0.015f, 0.018f, 0.015f), Surface(tray) is Transform st ? st.rotation : Quaternion.identity, Physics.AllLayers, QueryTriggerInteraction.Ignore)
                              .Where(c => !c.transform.IsChildOf(dater.transform)).Select(c => QaGame.Where(c.gameObject)).ToList();

        var sounds = new List<string>();
        Action<string, bool> hear = (id, clip) => sounds.Add(id);
        Sounds.Fired += hear;
        int prints = AllPrints();
        float minClear = float.MaxValue, minFace = float.MaxValue, peakSway = 0f, letGoFace = float.NaN, descent = 0f;
        int framesBelow = 0, strokeFrames = 0, frame = 0, released = -1;
        Vector3 letGoAt = Vector3.zero;
        string worstFrame = "none";
        IEnumerator Watch()
        {
            IEnumerator play = runner.Input.Play(path, i => { if (i == path.ReleaseIndex) released = frame + 1; });
            while (play.MoveNext())
            {
                yield return play.Current;
                frame++;
                Sample();
            }
            for (int i = 0; i < 240; i++)
            {
                yield return null;
                frame++;
                Sample();
                if (i > 3 && !tray.IsMoving && !tray.IsCarrying)
                    break;
            }
        }
        void Sample()
        {
            bool after = released >= 0 && frame >= released;
            float atHome = (dater.transform.localPosition - home).magnitude / Mathf.Max(1e-6f, perMetre);
            // The dater's footprint and lowest point this frame, against the prop's parts under that footprint.
            float low = float.MaxValue, fx0 = float.MaxValue, fx1 = float.MinValue, fz0 = float.MaxValue, fz1 = float.MinValue;
            foreach (Renderer r in dater.GetComponentsInChildren<Renderer>(false))
            {
                if (!r.enabled || r.GetComponent<TMPro.TMP_Text>() != null)
                    continue;
                var p = OnPlane(tray, r.bounds);
                low = Mathf.Min(low, p.low);
                fx0 = Mathf.Min(fx0, p.x0); fx1 = Mathf.Max(fx1, p.x1);
                fz0 = Mathf.Min(fz0, p.z0); fz1 = Mathf.Max(fz1, p.z1);
            }
            bool overHome = fx1 > hx0 && fx0 < hx1 && fz1 > hz0 && fz0 < hz1;
            if (prop != null && !overHome)
            {
                float topUnder = float.MinValue;
                foreach (Bounds b in parts)
                {
                    var p = OnPlane(tray, b);
                    if (p.x1 > fx0 && p.x0 < fx1 && p.z1 > fz0 && p.z0 < fz1)
                        topUnder = Mathf.Max(topUnder, p.top);
                }
                if (topUnder > float.MinValue)
                {
                    float clear = low - topUnder;
                    if (clear < minClear)
                    {
                        minClear = clear;
                        worstFrame = $"frame {frame} ({(after ? "after the let-go" : "carried")}), dater {Mm(atHome)} from home";
                    }
                    if (clear < -0.0005f)
                        framesBelow++;
                }
            }
            string stroke = QaGame.Field(handle, "Stroke")?.ToString() ?? "None";
            if (after && stroke != "None")
                strokeFrames++;
            if (die != null && after)
            {
                float face = DieLow(tray, die);
                if (float.IsNaN(letGoFace))
                {
                    letGoFace = face;
                    letGoAt = die.position;
                }
                // Down from the let-go height while still where it was let go (before it heads home): a stroke into the prop.
                if (Vector3.ProjectOnPlane(die.position - letGoAt, up).magnitude < 0.005f)
                    descent = Mathf.Max(descent, letGoFace - face);
                minFace = Mathf.Min(minFace, face);
            }
            var sway = (Spring)QaGame.Field(handle, "Sway");
            peakSway = Mathf.Max(peakSway, sway.Value);
        }
        try
        {
            yield return FixedStep(Watch());
        }
        finally
        {
            Sounds.Fired -= hear;
        }
        yield return QaGame.Until(() => !tray.IsMoving, 5f, "the dater to seat");
        float fromHome = (dater.transform.localPosition - home).magnitude / Mathf.Max(1e-6f, perMetre);
        int miss = sounds.Count(x => x == SoundCues.StampMiss), error = sounds.Count(x => x == SoundCues.UiError);
        var fails = new List<string>();
        if (tray.LastPress != StampPress.Nothing)
            fails.Add($"press {tray.LastPress}");
        if (AllPrints() != prints)
            fails.Add("a print landed");
        if (fromHome > 0.0005f)
            fails.Add($"the dater sits {Mm(fromHome)} from home");
        string measured;
        if (prop == null)
        {
            if (miss != 1)
                fails.Add($"stamp_miss played {miss} times (once expected)");
            if (Mathf.Abs(minFace) > 0.0005f)
                fails.Add($"the die's face reached {Mm(minFace)} over the desk's plane (within 0.5 mm expected)");
            if (Mathf.Abs(peakSway - 0.006f) > 0.001f)
                fails.Add($"the bounce peaked at {Mm(peakSway)} (6 ±1 mm expected)");
            measured = $"miss {miss}, face min {Mm(minFace)}, bounce {Mm(peakSway)}, home {Mm(fromHome)}";
        }
        else
        {
            if (miss + error > 0)
                fails.Add($"cues: stamp_miss {miss}, ui_error {error}");
            if (strokeFrames > 0)
                fails.Add($"a stroke ran over the {over} ({strokeFrames} frames)");
            if (descent > 0.0005f)
                fails.Add($"the die went down {Mm(descent)} where it was let go");
            measured = $"press {tray.LastPress}, stroke frames {strokeFrames}, descent {Mm(descent)}, cues {miss + error}, home {Mm(fromHome)}";
        }
        string detail = $"{which} dater, {QaRunner.ParseKind(step.path)} path let go over the {over} at {path.End} ({Screen.width}x{Screen.height}); colliders standing there: {(standing.Count > 0 ? string.Join(", ", standing) : "none")}; cues {string.Join(",", sounds)}; LastPressPoint {V(tray.LastPressPoint)}" +
                        (fails.Count > 0 ? "; FAIL: " + string.Join("; ", fails) : string.Empty);
        runner.Assert($"letGo.{name}", Owner, fails.Count == 0, measured, detail);
        Report(runner, $"== letGo.{name}: {(fails.Count == 0 ? "PASS" : "FAIL")} {measured}; {detail}");
        if (prop != null)
        {
            string carry = $"{framesBelow} frames below, worst {Mm(Mathf.Min(0f, minClear))} ({worstFrame})";
            runner.Assert($"letGoCarry.{name}", Owner, framesBelow == 0, carry,
                $"R4 (d)'s literal line: no part of the {which} dater below the top of the {over}'s parts under its footprint in any frame from the press to the seat (frames with the dater over its own cradle's place left out). The carry hangs the die DeskConfigSO.stampHover over the desk's plane wherever it is (DeskDraggable clamps the aim to the plane), so a prop taller than that is carried through; keeping carried things over props is DOC-HANDLE's DeskObstacles seam (STAMP.md request 2).");
            Report(runner, $"== letGoCarry.{name}: {(framesBelow == 0 ? "PASS" : "FAIL")} {carry}");
        }
    }

    private static float PerMetre(Transform root) => root.parent != null && root.parent.lossyScale.y > 1e-4f ? 1f / root.parent.lossyScale.y : 1f;

    // ---------------- stampTab ----------------

    private static IEnumerator Tab(QaRunner runner, QaStep step)
    {
        DeskStampTray tray = Tray();
        string name = QaRunner.Safe(string.IsNullOrEmpty(step.name) ? "tab" : step.name);
        var view = QaGame.Field(tray, "deskView") as DeskView ?? throw new InvalidOperationException("stampTab: the tray has no DeskView.");
        // The pointer away from the rack, so no dater lifts for the hover.
        yield return runner.Input.MoveTo(new Vector2(Screen.width * 0.5f, Screen.height * 0.97f), 6);
        yield return QaGame.Frames(20);
        bool before = view.IsOn;
        Transform a = Dater(tray, "Approved").transform, d = Dater(tray, "Denied").transform;
        Vector3 ha = a.position, hd = d.position;
        Vector3 up = Up(tray);
        float peak = 0f, settled = -1f;
        yield return FixedStep(TapAndWatch());
        IEnumerator TapAndWatch()
        {
            // The motion clock (UiMotion.Delta: the fixed step here) from the frame the key goes down.
            float elapsed = 0f;
            IEnumerator tap = runner.Input.Tap(Key.Tab);
            bool tapping = true;
            while (elapsed < 1.5f)
            {
                if (tapping)
                    tapping = tap.MoveNext();
                float lift = Mathf.Max(Vector3.Dot(a.position - ha, up), Vector3.Dot(d.position - hd, up));
                peak = Mathf.Max(peak, lift);
                if (lift > 0.0002f)
                    settled = -1f;
                else if (settled < 0f && peak > 0.0005f)
                    settled = elapsed;
                yield return null;
                elapsed += UiMotion.Delta(Time.unscaledDeltaTime);
            }
        }
        bool on = view.IsOn;
        // The camera's blend: wait until the main camera stands still for 10 frames (3 s at most).
        Camera cam = Cam();
        Vector3 lastPos = cam.transform.position;
        Quaternion lastRot = cam.transform.rotation;
        int still = 0;
        float until = Time.realtimeSinceStartup + 3f;
        while (still < 10 && Time.realtimeSinceStartup < until)
        {
            yield return null;
            cam = Cam();
            bool moved = (cam.transform.position - lastPos).sqrMagnitude > 1e-10f || Quaternion.Angle(cam.transform.rotation, lastRot) > 0.001f;
            still = moved ? 0 : still + 1;
            lastPos = cam.transform.position;
            lastRot = cam.transform.rotation;
        }
        runner.Recorder.Still(Path.Combine(runner.OutDir, "stills", $"tab_{name}_{Screen.width}x{Screen.height}.png"));
        yield return QaGame.Until(() => !runner.Recorder.StillPending, 10f, "the reading view's still");
        List<MeshFilter> parts = RackParts(tray, true);
        int corners = 0, outside = 0;
        float worst = 0f;
        foreach (Vector3 c in parts.SelectMany(Corners))
        {
            Vector3 vp = cam.WorldToViewportPoint(c);
            corners++;
            float beyond = Mathf.Max(Mathf.Max(-vp.x, vp.x - 1f), Mathf.Max(-vp.y, vp.y - 1f));
            if (vp.z <= 0f || beyond > 0f)
            {
                outside++;
                worst = Mathf.Max(worst, vp.z <= 0f ? 1f : beyond);
            }
        }
        Rect? rackRect = ScreenRectOf(parts.SelectMany(Corners), cam);
        yield return FixedStep(TapBack());
        IEnumerator TapBack()
        {
            yield return runner.Input.Tap(Key.Tab);
            yield return QaGame.Frames(45);
        }
        bool back = view.IsOn;
        runner.Assert($"tab.{name}.view", Owner, !before && on && !back, $"before {before}, after TAB {on}, after TAB again {back}", "TAB takes the office view to the reading view and back (R2.3, R2.4)");
        runner.Assert($"tab.{name}.rackInFrame", "CAM", parts.Count > 0 && outside == 0,
            parts.Count == 0 ? "no rack" : $"{outside} of {corners} rack corners outside the frame (worst {worst:0.000} of the screen)",
            $"the rack's screen rect in the reading view {R(rackRect)} at {Screen.width}x{Screen.height} (STAMP R2.3, the gate of request 6 to CAM: the rack in the reading-pose LensFit)");
        runner.Assert($"tab.{name}.daters", Owner, peak >= 0.002f && settled > 0f && settled <= 0.5f,
            $"lift {Mm(peak)}, settled at {(settled > 0f ? settled.ToString("0.00", CultureInfo.InvariantCulture) + " s" : "never")}",
            "both daters lift to answer TAB (Present: the hover lift's kick) and settle within 0.5 s (motion clock)");
    }

    // ---------------- stampCradles ----------------

    private sealed class CradleWatch
    {
        public bool On;
        public string Name;
        public int OpenFrame = -1, StartFrame = -1;
        public float Elapsed, LockedAt = -1f;
        public string Detail = string.Empty;
    }

    private static CradleWatch _cradles;

    private static IEnumerator Cradles(QaRunner runner, QaStep step)
    {
        string name = QaRunner.Safe(string.IsNullOrEmpty(step.name) ? "case" : step.name);
        if (step.n != 0)
        {
            var w = new CradleWatch { On = true, Name = name };
            _cradles = w;
            runner.StartCoroutine(CradleLoop(w, Tray()));
            yield return null;
            yield break;
        }
        CradleWatch c = _cradles ?? throw new InvalidOperationException("stampCradles: no watch runs.");
        c.On = false;
        _cradles = null;
        float locked = c.LockedAt;
        bool pass = c.OpenFrame >= 0 && c.StartFrame >= 0 && c.StartFrame - c.OpenFrame <= 1 && locked >= 0f && locked < 0.3f;
        runner.Assert($"cradles.{name}", Owner, pass,
            c.OpenFrame < 0 ? "no case opened" : $"starts {c.StartFrame - c.OpenFrame} frame(s) after the case opens, DENIED locked at {(locked >= 0f ? locked.ToString("0.000", CultureInfo.InvariantCulture) + " s" : "never")}",
            $"R6 (BUG-73b): the cradle starts on the case's first frame and locks in under 0.3 s (motion clock). {c.Detail}");
    }

    private static IEnumerator CradleLoop(CradleWatch w, DeskStampTray tray)
    {
        var end = new WaitForEndOfFrame();
        while (w.On)
        {
            yield return end;
            if (!w.On || tray == null)
                break;
            var seq = QaGame.Field(tray, "_drawer") as DrawerSequence;
            if (seq == null)
                continue;
            if (w.OpenFrame < 0)
            {
                if (tray.Passport >= 0)
                    w.OpenFrame = Time.frameCount;
                else
                    continue;
            }
            else
                w.Elapsed += UiMotion.Delta(Time.unscaledDeltaTime);
            float raise = seq.Raise(DrawerSequence.Denied);
            if (w.StartFrame < 0 && raise > 0f)
            {
                w.StartFrame = Time.frameCount;
                w.Detail += $"raise {raise:0.000} at frame {Time.frameCount}; ";
            }
            if (w.LockedAt < 0f && seq.Locked(DrawerSequence.Denied))
            {
                w.LockedAt = w.Elapsed;
                w.Detail += $"DENIED locked at frame {Time.frameCount}; ";
            }
            if (w.StartFrame >= 0 && w.LockedAt >= 0f)
                w.On = false;
        }
    }

    // ---------------- stampHandBack ----------------

    private static IEnumerator HandBack(QaRunner runner, QaStep step)
    {
        DeskStampTray tray = Tray();
        Camera cam = Cam();
        GameManager game = QaGame.Find<GameManager>();
        CaseInstance inst = game != null ? game.ActiveCase : null;
        if (step.n != 0)
        {
            DevCheats.Run(DevCheat.DecideCorrectly, RunManager.Instance, 0, 0f, null);
            Report(runner, $"== stampHandBack {step.name}: the cheat menu's correct decision ({DevCheats.LastResult})");
        }
        else
        {
            DeskDocument passport = Paper(tray, -1);
            var desk = QaGame.Find<DeskController>();
            var counter = desk != null ? QaGame.Field(desk, "counter") as DeskCounter : null;
            if (counter == null)
                throw new InvalidOperationException("stampHandBack: no counter.");
            Vector2 from = GrabPoint(tray, passport, cam);
            // The counter's strip where it shows (the reading view's COUNTER band), else its first spot.
            Vector2 to = QaGame.ScreenPoint(counter.Strip.gameObject) ?? Screen2(counter.Spot(0), cam);
            QaPath path = QaPaths.Drag(QaRunner.ParseKind(step.path), from, to, new Vector2(Screen.width, Screen.height));
            yield return FixedStep(Settle(runner.Input.Play(path), 10));
            Report(runner, $"== stampHandBack {step.name}: passport {passport.Index} from {from} to the counter at {path.End}");
        }
        // The case ends: the papers go; a citation slip (a wrong call) shows after it and holds the day until the player acknowledges it.
        float until = Time.realtimeSinceStartup + 25f;
        int quiet = 0;
        while (Time.realtimeSinceStartup < until)
        {
            var office = QaGame.Find<OfficeUIController>();
            var ack = office != null ? QaGame.Field(office, "citationContinueButton") as UnityEngine.UI.Button : null;
            if (ack != null && ack.isActiveAndEnabled && ack.interactable && QaGame.ScreenPoint(ack.gameObject) is Vector2 at)
            {
                // The slip drops in and prints first; the button takes the click once it shows.
                yield return QaGame.Frames(20);
                Report(runner, $"   a citation slip: {QaGame.Where(ack.gameObject)} acknowledged");
                yield return runner.Input.Play(QaPaths.Click(QaGame.ScreenPoint(ack.gameObject) ?? at));
                yield return QaGame.Frames(10);
                quiet = 0;
                continue;
            }
            bool ended = game == null || game.ActiveCase != inst || !QaGame.TravellerAtDesk();
            if (ended && ++quiet > 60)
                break;
            yield return null;
        }
        if (game != null && game.ActiveCase == inst && QaGame.TravellerAtDesk())
            throw new TimeoutException("stampHandBack: the case did not end.");
    }

    // ---------------- stampView, stampStack ----------------

    /// <summary>n 1: the reading view (TAB when it is not on), n 0: the office view; then the camera's blend (1 s).</summary>
    private static IEnumerator View(QaRunner runner, QaStep step)
    {
        var view = QaGame.Field(Tray(), "deskView") as DeskView ?? throw new InvalidOperationException("stampView: no DeskView.");
        bool want = step.n != 0;
        if (view.IsOn != want)
        {
            yield return runner.Input.Tap(Key.Tab);
            yield return QaGame.Until(() => view.IsOn == want, 3f, want ? "the reading view" : "the office view");
        }
        float until = Time.realtimeSinceStartup + (step.f > 0f ? step.f : 1f);
        while (Time.realtimeSinceStartup < until)
            yield return null;
    }

    /// <summary>
    /// Builds a stack at the screen point (x2, y2) in the office view: every paper but the passport, then the
    /// passport, each dragged from where it lies onto the spot (a few pixels apart), so the passport lies on top
    /// (last touched on top); the office view is restored between drops (a drop tilts into the reading view).
    /// </summary>
    private static IEnumerator Stack(QaRunner runner, QaStep step)
    {
        DeskStampTray tray = Tray();
        var order = Papers().Where(p => p.Index != tray.Passport).Select(p => p.Index).ToList();
        order.Add(tray.Passport);
        var log = new StringBuilder($"== stampStack {step.name}: ");
        for (int k = 0; k < order.Count; k++)
        {
            yield return View(runner, new QaStep { n = 0, f = 0.6f });
            DeskDocument paper = Paper(tray, order[k]);
            yield return QaGame.Until(() => !paper.IsSliding, 10f, $"paper {paper.Index} to settle");
            Camera cam = Cam();
            Vector2 from = GrabPoint(tray, paper, cam);
            Vector2 to = Frac(step.x2, step.y2) + new Vector2(6f * k, -4f * k);
            QaPath path = QaPaths.Drag(QaPathKind.Plain, from, to, new Vector2(Screen.width, Screen.height));
            yield return FixedStep(Settle(runner.Input.Play(path), 30));
            log.Append($"paper {paper.Index} to {path.End}; ");
        }
        Report(runner, log.ToString());
    }

    // ---------------- stampHold ----------------

    /// <summary>
    /// R5 (BUG-55b): the dater (text) is picked up and held over the passport with the left button down, then key
    /// (Escape, Q, A, Tab; "RightClick" for the right button) is pressed while it is held, then the left button is let
    /// go there. Asserted as hold.&lt;name&gt;: the key ended the hold (nothing carried after it), nothing was stamped
    /// (no print, the verdict and LastPress as before) and the dater sits home. The view the key opened is put back
    /// afterwards (Esc out of the PC or the city view; the reading view again).
    /// </summary>
    private static IEnumerator Hold(QaRunner runner, QaStep step)
    {
        DeskStampTray tray = Tray();
        Camera cam = Cam();
        string name = QaRunner.Safe(string.IsNullOrEmpty(step.name) ? step.key : step.name);
        string which = Which(step.text);
        Clickable dater = Dater(tray, which);
        DeskDraggable drag = dater.GetComponent<DeskDraggable>();
        yield return QaGame.Until(() => drag.enabled && !tray.IsMoving && !tray.IsCarrying, 10f, $"the {which} dater to take input");
        DeskDocument passport = Paper(tray, -1);
        object handle = Handle(tray, which);
        Vector3 home = (Vector3)QaGame.Field(handle, "Home");
        var view = QaGame.Field(tray, "deskView") as DeskView;
        bool readingBefore = view != null && view.IsOn;
        Vector2 from = QaGame.ScreenPoint(dater.gameObject) ?? throw new InvalidOperationException($"the {which} dater is off the screen.");
        Vector2 to = GrabPoint(tray, passport, cam);
        QaPath path = QaPaths.Drag(QaPathKind.Plain, from, to, new Vector2(Screen.width, Screen.height));
        int prints = AllPrints();
        bool verdict = tray.HasVerdict;
        StampPress press = tray.LastPress;
        bool carriedBefore = false, carriedAfter = true;
        string afterKey = "-";
        string key = (step.key ?? string.Empty).Trim();
        var office = QaGame.Find<OfficeViewController>();
        var city = QaGame.Find<CityView>();
        yield return FixedStep(HoldThenKey());
        IEnumerator HoldThenKey()
        {
            // The drag up to its release, the button still down: the dater hangs over the passport.
            for (int i = 0; i < path.ReleaseIndex; i++)
            {
                runner.Input.Set(path.Samples[i].Position, path.Samples[i].Down);
                yield return null;
            }
            yield return QaGame.Frames(3);
            carriedBefore = tray.IsCarrying;
            if (string.Equals(key, "RightClick", StringComparison.OrdinalIgnoreCase))
            {
                var state = new UnityEngine.InputSystem.LowLevel.MouseState { position = to }.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left).WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Right);
                InputSystem.QueueStateEvent(runner.Input.Mouse, state);
                yield return QaGame.Frames(2);
                runner.Input.Set(to, true);
                yield return null;
            }
            else
                yield return runner.Input.Tap((Key)Enum.Parse(typeof(Key), key, true));
            yield return QaGame.Frames(5);
            carriedAfter = tray.IsCarrying;
            afterKey = $"city {(city != null && city.IsOn)}, PC {(office != null && office.Current == OfficeView.MonitorFocus)}, reading view {(view != null && view.IsOn)}";
            // The left button let go where the dater hung; a stroke it starts runs out on this clock (UiMotion.Now changes clocks with the capture rate).
            runner.Input.Set(to, false);
            yield return QaGame.Frames(3);
            for (int i = 0; i < 150 && tray.IsMoving && !(city != null && city.IsOn) && !(office != null && office.Current == OfficeView.MonitorFocus); i++)
                yield return null;
        }
        // Back to the desk as it was first (out of the PC or the city, where the desk's daters stand still), then the seat.
        for (int i = 0; i < 2 && (office != null && office.Current == OfficeView.MonitorFocus || city != null && city.IsOn); i++)
        {
            yield return runner.Input.Tap(Key.Escape);
            yield return QaGame.Frames(45);
        }
        if (view != null && view.IsOn != readingBefore)
            yield return View(runner, new QaStep { n = readingBefore ? 1 : 0 });
        float seatBy = Time.realtimeSinceStartup + 5f;
        while (tray.IsMoving && Time.realtimeSinceStartup < seatBy)
            yield return null;
        float fromHome = (dater.transform.localPosition - home).magnitude / Mathf.Max(1e-6f, PerMetre(dater.transform));
        var fails = new List<string>();
        if (tray.IsMoving)
            fails.Add($"the dater did not seat within 5 s (stroke {QaGame.Field(handle, "Stroke")}, returning {QaGame.Field(handle, "Returning")}, sway at rest {((Spring)QaGame.Field(handle, "Sway")).AtRest}, seat at rest {((Spring)QaGame.Field(handle, "Seat")).AtRest}, squash at rest {((Spring)QaGame.Field(handle, "Squash")).AtRest}, tray active {tray.isActiveAndEnabled}, city on {city != null && city.IsOn}, PC {(office != null ? office.Current.ToString() : "-")}, reading view {(view != null && view.IsOn)})");
        if (!carriedBefore)
            fails.Add("the dater was not carried before the key");
        if (carriedAfter)
            fails.Add($"{key} did not end the hold");
        if (AllPrints() != prints)
            fails.Add("a print landed");
        if (tray.HasVerdict != verdict || tray.LastPress != press)
            fails.Add($"the press changed: {press} to {tray.LastPress}, verdict {verdict} to {tray.HasVerdict}");
        if (fromHome > 0.0005f)
            fails.Add($"the dater sits {Mm(fromHome)} from home");
        string measured = $"carried {carriedBefore} then {carriedAfter} ({afterKey}), prints +{AllPrints() - prints}, press {tray.LastPress}, home {Mm(fromHome)}";
        runner.Assert($"hold.{name}", Owner, fails.Count == 0, measured,
            $"{which} dater held over the passport, then {key}, then the left button let go there" + (fails.Count > 0 ? "; FAIL: " + string.Join("; ", fails) : string.Empty));
        Report(runner, $"== hold.{name}: {(fails.Count == 0 ? "PASS" : "FAIL")} {measured}");
        yield return QaGame.Frames(15);
    }
}
#endif
