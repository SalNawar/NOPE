#if UNITY_EDITOR || TIMEDESK_QA
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// DOC-HANDLE's rig op <c>counterDrag</c> (FOLD-01 rounds 3 and 4; Papers,
/// Please: a paper pressed on the counter follows the pointer one to one onto
/// the desk). It turns the view as the step says (text "office" or
/// "reading"), waits until the camera is still (QaGame.UntilCameraStill: a
/// press point taken while the view still turns misses the paper), presses a
/// paper on its visible click box and drags it onto the desk:
/// <list type="bullet">
/// <item>the paper: <c>target</c> "index:N" (paper N, on the counter or the
/// desk), else the first counter paper the press can reach;</item>
/// <item>the press: (<c>x</c>, <c>y</c>) on the click box, shares of its half
/// width and half depth in the desk's frame (x to the right, y away from the
/// chair: 0, 0 its middle, y -0.95 its near edge, 0.9 its far edge), pulled
/// toward the middle until the press meets the paper (the round 3 verifier's
/// path 5: round 3 pressed the root, for the passport the one point of its
/// cover that could not reach the clamp);</item>
/// <item>the path: <c>path</c> (P-PLAIN by default; P-SLOW, P-FAST, P-FLICK
/// ...);</item>
/// <item>the release: <c>target2</c> "m:x,y" (a point of the desk's frame in
/// metres, DeskLayout.Frame, seen through the camera at the press), else
/// (<c>x2</c>, <c>y2</c>) screen fractions with y from the bottom, else the
/// mat's middle.</item>
/// </list>
/// The pressed point is the point of the paper the press met (the press's ray
/// on the sheet's plane as it lies), kept as a share of the paper's size (a
/// paper grows from its counter size to its desk size under the cursor).
/// Every frame from the press to the settle's end goes to the run's
/// counterdrag.tsv: the pointer, the paper's zone and size, its root, the root
/// the pointer asks for (the pressed point under the pointer plus the grown
/// grab offset), how far the paper was held back from it, how far the root lay
/// past the desk surface's clamp area, and the pressed point's distance from
/// the pointer on the screen (at the height it was pressed at, so the drag's
/// lift does not count; drawn_px1080 gives the same point as the sheet is
/// drawn, its lift and swing included, reported only).
/// Asserts (<c>f</c> px at 1080p, default 8: DOC-HANDLE D1's 8 px):
/// <list type="bullet">
/// <item><c>counterDrag.&lt;name&gt;.follow</c>: while the drag runs, the
/// pressed point stays at most <c>f</c> px from the pointer on every
/// frame.</item>
/// <item><c>counterDrag.&lt;name&gt;.hold</c>: no held frame has the root more
/// than 5 mm from where the pointer asks (FOLD-01 round 4: G1, the desk
/// surface's clamp; G2, the bound at the back step's lip while the size
/// grows).</item>
/// <item><c>counterDrag.&lt;name&gt;.rest</c>: after <c>n</c> settle frames
/// (default 45) the paper lies on the desk (its zone the desk's, not docked,
/// not sliding). Let go where it lies wholly on the desk once its size ends
/// (DeskDocument.SettledRoot at the last held frame: its far edge short of
/// the back step), the pressed point, as the sheet lies, is at most <c>f</c>
/// px from where the release's ray (the pointer through the camera of the
/// last held frame) meets the sheet (G4, FOLD-02). Let go before that, it
/// slides onto the desk, eased (G3, by design: the round 3 verifier's G3,
/// kept as built on 2026-10-30), and rests wholly short of the back step.
/// The root's move after the release, its largest step and whether the size
/// still eased at the release are reported.</item>
/// </list>
/// The drag plays at the fixed 30 fps step, as the drop op's does.
/// </summary>
public static class CounterDragOps
{
    private const string Owner = "DOC-HANDLE";

    /// <summary>How far the root may lie from where the pointer asks before a frame counts as held back (metres).</summary>
    private const float HeldBack = 0.005f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
#endif
    private static void Register() => QaRunner.RegisterOp("counterDrag", CounterDragOp);

    private static IEnumerator CounterDragOp(QaRunner runner, QaStep step)
    {
        string name = QaRunner.Safe(string.IsNullOrEmpty(step.name) ? "paper" : step.name);
        string follow = $"counterDrag.{name}.follow", hold = $"counterDrag.{name}.hold", rest = $"counterDrag.{name}.rest";
        float limit = step.f > 0f ? step.f : 8f;
        bool reading = string.Equals((step.text ?? string.Empty).Trim(), "reading", StringComparison.OrdinalIgnoreCase);
        DeskView view = QaGame.Find<DeskView>();
        QaInput input = runner.Input;
        if (view == null || input == null || !DeskLayout.Bound)
        {
            Fail(runner, follow, hold, rest, "no desk", "counterDrag needs the virtual mouse, the desk view and a bound desk.");
            yield break;
        }

        // The view the step asks for, and a still camera (a view key's turn blends over about 16 frames).
        if (view.IsOn != reading)
        {
            if (reading)
                view.TiltIn(CameraMoveCause.ViewKey);
            else
                view.Return();
        }
        bool still = false;
        yield return QaGame.UntilCameraStill(10f, ok => still = ok);

        int wanted = -1;
        string target = (step.target ?? string.Empty).Trim();
        if (target.StartsWith("index:", StringComparison.OrdinalIgnoreCase))
            int.TryParse(target.Substring(6), NumberStyles.Integer, CultureInfo.InvariantCulture, out wanted);
        Camera camera = QaGame.PointerCamera();
        DeskDocument paper = null;
        Vector2 from = default, used = default;
        float until = Time.realtimeSinceStartup + 10f;
        while (paper == null && Time.realtimeSinceStartup < until)
        {
            camera = QaGame.PointerCamera();
            foreach (DeskDocument p in Papers(wanted))
            {
                if (camera == null || !ClickBox(p, out DeskRect box, out float lift))
                    break;
                // The press point on the click box, pulled toward its middle until the press meets the paper.
                for (float k = 1f; k >= -0.001f && paper == null; k -= 0.1f)
                {
                    float sx = Mathf.Clamp(step.x, -1f, 1f) * k, sy = Mathf.Clamp(step.y, -1f, 1f) * k;
                    Vector3 s = camera.WorldToScreenPoint(DeskLayout.Frame.Point(new Vector2(box.CentreX + sx * box.Width / 2f, box.CentreY + sy * box.Height / 2f), lift));
                    GameObject top = s.z > 0f ? QaGame.TopAt(s) : null;
                    if (top != null && top.transform.IsChildOf(p.transform))
                    {
                        paper = p;
                        from = s;
                        used = new Vector2(sx, sy);
                    }
                }
                if (paper != null)
                    break;
            }
            if (paper == null)
                yield return null;
        }
        if (paper == null)
        {
            Fail(runner, follow, hold, rest, "no paper under the pointer", $"No {(wanted >= 0 ? $"paper {wanted}" : "counter paper")} lay where the pointer could press it in the {(reading ? "reading" : "office")} view within 10 s.");
            yield break;
        }
        DeskSurface surface = QaGame.Field(paper.Drag, "_surface") as DeskSurface;
        Vector2 to;
        string target2 = (step.target2 ?? string.Empty).Trim();
        if (target2.StartsWith("m:", StringComparison.OrdinalIgnoreCase))
        {
            string[] parts = target2.Substring(2).Split(',');
            Vector3 s = camera.WorldToScreenPoint(DeskLayout.Frame.Point(new Vector2(float.Parse(parts[0], CultureInfo.InvariantCulture), float.Parse(parts[1], CultureInfo.InvariantCulture))));
            to = new Vector2(s.x, s.y);
        }
        else if (step.x2 > 0f || step.y2 > 0f)
            to = new Vector2(step.x2 * Screen.width, step.y2 * Screen.height);
        else
        {
            (float x, float y) mid = DeskLayout.Mat.Rect.PointAt(0.5f, 0.45f);
            Vector3 s = camera.WorldToScreenPoint(DeskLayout.Frame.Point(new Vector2(mid.x, mid.y)));
            to = new Vector2(s.x, s.y);
        }
        to = new Vector2(Mathf.Clamp(to.x, 2f, Screen.width - 2f), Mathf.Clamp(to.y, 2f, Screen.height - 2f));

        // The pressed point: the point of the paper the press met (the press's ray on the sheet's plane), as a share of the paper's size in its root's frame.
        float size = SizeNow(paper);
        Ray pressRay = camera.ScreenPointToRay(from);
        if (surface == null || size <= 0f || !new Plane(surface.transform.up, paper.Sheet.position).Raycast(pressRay, out float enter))
        {
            Fail(runner, follow, hold, rest, "no desk plane", "The paper's drag has no desk surface, or the press does not meet its sheet.");
            yield break;
        }
        Vector3 onSheet = pressRay.GetPoint(enter);
        Vector3 sheetLocal = paper.Sheet.InverseTransformPoint(onSheet);
        Vector3 local = Quaternion.Inverse(paper.transform.rotation) * (onSheet - paper.transform.position);
        var share = new Vector2(local.x / size, local.z / size);
        float pressGap = OnPlane(surface, camera, from, out Vector3 pressedOnDesk) ? Vector3.ProjectOnPlane(pressedOnDesk - onSheet, Vector3.up).magnitude : float.NaN;
        float pressHeight = Vector3.Dot(onSheet - surface.transform.position, surface.transform.up);
        bool stepKnown = DeskLayout.TryBackStep(out float backStep);

        QaPathKind kind = QaRunner.ParseKind(step.path);
        QaPath path = QaPaths.Drag(kind, from, to, new Vector2(Screen.width, Screen.height));
        bool fixedHere = Time.captureFramerate != SeqRecorder.Fps;
        int rate = Time.captureFramerate;
        if (fixedHere)
            Time.captureFramerate = SeqRecorder.Fps;
        var rows = new StringBuilder();
        string tsv = Path.Combine(runner.OutDir, "counterdrag.tsv");
        if (!File.Exists(tsv))
            rows.AppendLine("step\ti\tframe\tptrX\tptrY_top\tdown\tdragging\tzone\tsize\tgrabScale\tsettledScale\trootX_m\trootY_m\twantX_m\twantY_m\theldBack_mm\tpastClamp_mm\tpointerX_m\tpointerY_m\theldX_m\theldY_m\terr_px1080\tdrawn_px1080");
        var corners = new Vector3[4];
        float worst = 0f, atRelease = float.NaN, drawnWorst = 0f, drawnAtRelease = float.NaN, worstBack = 0f, worstPast = 0f, farAtRelease = float.NaN;
        int worstFrame = -1, dragFrames = 0, firstDrag = -1, heldFrames = 0, firstHeld = -1, lastHeld = -1, pastFrames = 0;
        bool easingAtRelease = false;
        Ray? releaseRay = null;
        Matrix4x4 releaseView = Matrix4x4.identity;
        Vector3 rootAtRelease = paper.transform.position;
        var roots = new List<Vector3>();
        var end = new WaitForEndOfFrame();
        bool sampling = true;
        int index = 0;
        IEnumerator Sample()
        {
            while (sampling)
            {
                yield return end;
                if (paper == null)
                    continue;
                Camera eye = QaGame.PointerCamera();
                Vector2 pointer = input.Mouse.position.ReadValue();
                bool down = input.Mouse.leftButton.isPressed, dragging = paper.Drag != null && paper.Drag.IsDragging;
                float sizeNow = SizeNow(paper);
                Vector3 held = Held(paper, share, sizeNow) + surface.transform.up * pressHeight;
                bool onPlane = OnPlane(surface, eye, pointer, out Vector3 point);
                Vector3 grabbedAt = paper.Drag != null ? AtHeight(surface, eye, pointer, paper.Drag.GrabHeight, point) : point;
                Vector3 want = onPlane && paper.Drag != null ? grabbedAt + paper.Drag.GrabOffset : paper.transform.position;
                Vector3 root = paper.transform.position;
                float back = Vector3.ProjectOnPlane(root - want, Vector3.up).magnitude;
                float past = Vector3.ProjectOnPlane(root - surface.Clamp(root), Vector3.up).magnitude;
                float err = float.NaN, drawn = float.NaN;
                if (eye != null)
                {
                    Vector3 h = eye.WorldToScreenPoint(held), d = eye.WorldToScreenPoint(paper.Sheet.TransformPoint(sheetLocal));
                    err = h.z > 0f ? QaCameraMoves.To1080(new Vector2(h.x - pointer.x, h.y - pointer.y).magnitude, Screen.height) : float.NaN;
                    drawn = d.z > 0f ? QaCameraMoves.To1080(new Vector2(d.x - pointer.x, d.y - pointer.y).magnitude, Screen.height) : float.NaN;
                }
                if (dragging)
                {
                    if (firstDrag < 0)
                        firstDrag = index;
                    dragFrames++;
                    if (!float.IsNaN(err) && err > worst)
                    {
                        worst = err;
                        worstFrame = index;
                    }
                    atRelease = err;
                    drawnAtRelease = drawn;
                    if (!float.IsNaN(drawn))
                        drawnWorst = Mathf.Max(drawnWorst, drawn);
                    worstBack = Mathf.Max(worstBack, back);
                    if (back > HeldBack)
                    {
                        heldFrames++;
                        if (firstHeld < 0)
                            firstHeld = index;
                        lastHeld = index;
                    }
                    if (past > 0.001f)
                    {
                        pastFrames++;
                        worstPast = Mathf.Max(worstPast, past);
                    }
                    if (eye != null)
                    {
                        releaseRay = eye.ScreenPointToRay(pointer);
                        releaseView = eye.projectionMatrix * eye.worldToCameraMatrix;
                    }
                    rootAtRelease = root;
                    easingAtRelease = Mathf.Abs(paper.SettledGrabScale - paper.Drag.GrabScale) > 0.005f;
                    // Where its far edge lies once its size ends (the root the drop is judged at): past the back step, the release is G3's.
                    paper.RestCorners(paper.SettledRoot, corners);
                    farAtRelease = corners.Max(c => DeskLayout.Frame.Local(c).y);
                    roots.Clear();
                }
                else if (firstDrag >= 0)
                    roots.Add(root);
                Vector2 r = DeskLayout.Frame.Local(root), w = DeskLayout.Frame.Local(want), pd = DeskLayout.Frame.Local(point), hd = DeskLayout.Frame.Local(held);
                rows.Append(name).Append('\t').Append(index).Append('\t').Append(Time.frameCount).Append('\t').Append(F1(pointer.x)).Append('\t').Append(F1(Screen.height - pointer.y))
                    .Append('\t').Append(down ? 1 : 0).Append('\t').Append(dragging ? 1 : 0).Append('\t').Append(paper.Zone).Append('\t').Append(F4(sizeNow))
                    .Append('\t').Append(F4(paper.Drag != null ? paper.Drag.GrabScale : 1f)).Append('\t').Append(F4(paper.SettledGrabScale))
                    .Append('\t').Append(F4(r.x)).Append('\t').Append(F4(r.y)).Append('\t').Append(F4(w.x)).Append('\t').Append(F4(w.y)).Append('\t').Append(F1(back * 1000f)).Append('\t').Append(F1(past * 1000f))
                    .Append('\t').Append(F4(pd.x)).Append('\t').Append(F4(pd.y)).Append('\t').Append(F4(hd.x)).Append('\t').Append(F4(hd.y)).Append('\t').Append(F1(err)).Append('\t').Append(F1(drawn)).AppendLine();
                index++;
            }
        }
        runner.StartCoroutine(Sample());
        yield return input.Play(path);
        yield return QaGame.Frames(step.n > 0 ? step.n : 45);
        sampling = false;
        yield return null;
        if (fixedHere)
            Time.captureFramerate = rate;
        File.AppendAllText(tsv, rows.ToString());

        string how = FormattableString.Invariant($"{paper.name} (paper {paper.Index}), the {(reading ? "reading" : "office")} view{(still ? string.Empty : " (the camera never stood still for 3 frames in 10 s)")}, {kind}: pressed at {from.x:0},{from.y:0} (the click box at {used.x:0.00},{used.y:0.00} of its half size), let go at {path.End.x:0},{path.End.y:0} (screen px, y from the bottom), the pressed point {share.x:0.00},{share.y:0.00} of its size from its root (the press met the desk plane {pressGap * 100f:0.0} cm past it); counterdrag.tsv has every frame");
        runner.Assert(follow, Owner, dragFrames > 0 && worst <= limit,
                      dragFrames > 0 ? FormattableString.Invariant($"the pressed point {worst:0.0} px from the pointer at worst (frame {worstFrame}), {atRelease:0.0} px on the last held frame (max {limit:0.#}); {dragFrames} frames held from frame {firstDrag}; as drawn (the drag's lift and swing, not asserted) {drawnWorst:0.0} px at worst, {drawnAtRelease:0.0} px on the last held frame") : "the drag never began",
                      how);
        runner.Assert(hold, Owner, dragFrames > 0 && heldFrames == 0,
                      FormattableString.Invariant($"{heldFrames} held frames with the root more than {HeldBack * 1000f:0} mm from where the pointer asks (frames {firstHeld} to {lastHeld}), {worstBack * 1000f:0.0} mm at worst; the root lay past the desk surface's clamp on {pastFrames} frames, up to {worstPast * 1000f:0.0} mm (the clamp holds the pressed point, not the root: G1)"),
                      how);

        Camera now = QaGame.PointerCamera();
        float restPx = float.NaN, restNowPx = float.NaN, restHeight = float.NaN, movePx = float.NaN, stepPx = 0f;
        if (now != null && releaseRay != null && paper != null && new Plane(surface.transform.up, paper.Sheet.position).Raycast(releaseRay.Value, out float at))
        {
            // The spot under the pointer (the release's ray on the sheet as it lies) and the point the paper was pressed at, as it lies:
            // on the screen the release was seen on (the camera of the last held frame), and through the camera of now (a drop on the
            // desk tilts the reading view in, D1, which shows the desk larger).
            Vector3 spot = releaseRay.Value.GetPoint(at), pressedNow = paper.Sheet.TransformPoint(sheetLocal);
            restPx = QaCameraMoves.To1080((ScreenOf(releaseView, spot) - ScreenOf(releaseView, pressedNow)).magnitude, Screen.height);
            Vector3 a = now.WorldToScreenPoint(spot), b = now.WorldToScreenPoint(pressedNow);
            if (a.z > 0f && b.z > 0f)
                restNowPx = QaCameraMoves.To1080(new Vector2(b.x - a.x, b.y - a.y).magnitude, Screen.height);
            restHeight = Vector3.Dot(pressedNow - surface.transform.position, surface.transform.up);
            movePx = QaCameraMoves.To1080((ScreenOf(releaseView, paper.transform.position) - ScreenOf(releaseView, rootAtRelease)).magnitude, Screen.height);
            Vector3 was = rootAtRelease;
            foreach (Vector3 r in roots)
            {
                stepPx = Mathf.Max(stepPx, QaCameraMoves.To1080((ScreenOf(releaseView, r) - ScreenOf(releaseView, was)).magnitude, Screen.height));
                was = r;
            }
        }
        bool onDesk = paper != null && paper.Zone == DeskZone.Desk && !paper.IsDocked && !paper.IsSliding;
        bool overStep = stepKnown && !float.IsNaN(farAtRelease) && farAtRelease > backStep + 0.001f;
        float farAtRest = float.NaN;
        if (paper != null)
        {
            paper.RestCorners(paper.transform.position, corners);
            farAtRest = corners.Max(c => DeskLayout.Frame.Local(c).y);
        }
        string after = FormattableString.Invariant($"the root moved {movePx:0.0} px after the release (its largest step {stepPx:0.0} px; the size {(easingAtRelease ? "still eased" : "had ended its ease")} at the release); its far edge at {farAtRelease:0.000} m once grown at the release, {farAtRest:0.000} m at rest (the back step at {backStep:0.000} m)");
        bool restOk = onDesk && !float.IsNaN(restPx) && (overStep ? farAtRest <= backStep + 0.002f : restPx <= limit);
        runner.Assert(rest, Owner, restOk,
                      paper == null ? "the paper is gone" : !onDesk ? $"it lies in the {paper.Zone} zone{(paper.IsDocked ? ", docked" : string.Empty)}{(paper.IsSliding ? ", still sliding" : string.Empty)}, not on the desk"
                      : overStep
                          ? FormattableString.Invariant($"let go before it lay wholly on the desk (G3, by design): it slid onto the desk and rests wholly short of the back step; the pressed point rests {restPx:0.0} px from the spot under the pointer (not asserted); {after}")
                          : FormattableString.Invariant($"the point it was pressed at rests {restPx:0.0} px from the spot under the pointer that let it go, on the screen it was let go on (max {limit:0.#}); {restNowPx:0.0} px through the camera of now; the sheet lay {pressHeight * 1000f:0.0} mm over the desk where pressed, {restHeight * 1000f:0.0} mm at rest; {after}"),
                      how + "; the release's ray met on the sheet as it lies");
    }

    /// <summary>The papers to press: paper <paramref name="wanted"/> wherever it lies, or (-1) every counter paper in order.</summary>
    private static IEnumerable<DeskDocument> Papers(int wanted) =>
        UnityEngine.Object.FindObjectsByType<DeskDocument>(FindObjectsInactive.Exclude)
                   .Where(d => d.isActiveAndEnabled && !d.IsSliding && !d.IsDocked && (wanted >= 0 ? d.Index == wanted : d.Zone == DeskZone.Counter)).OrderBy(d => d.Index);

    /// <summary>The paper's click box in the desk's frame (its sheet's box collider, else its renderers) and the box's height over the desk's top.</summary>
    private static bool ClickBox(DeskDocument paper, out DeskRect box, out float lift)
    {
        box = default;
        lift = 0f;
        if (paper == null || paper.Sheet == null)
            return false;
        Bounds b;
        BoxCollider c = paper.Sheet.GetComponent<BoxCollider>();
        if (c != null && c.enabled && c.bounds.size.sqrMagnitude > 1e-10f)
            b = c.bounds;
        else
        {
            Renderer r = paper.Sheet.GetComponentInChildren<Renderer>();
            if (r == null)
                return false;
            b = r.bounds;
        }
        box = DeskLayout.Frame.Footprint(b);
        lift = b.center.y - DeskLayout.Frame.Origin.y;
        return true;
    }

    /// <summary>The step's three assertions red with the same reason.</summary>
    private static void Fail(QaRunner runner, string follow, string hold, string rest, string measured, string detail)
    {
        runner.Assert(follow, Owner, false, measured, detail);
        runner.Assert(hold, Owner, false, measured, detail);
        runner.Assert(rest, Owner, false, measured, detail);
    }

    /// <summary>The paper's sheet scale now (its shown size over its size).</summary>
    private static float SizeNow(DeskDocument paper) => paper.Size.x > 0f ? paper.ShownSize.x / paper.Size.x : 0f;

    /// <summary>The pressed point straight over the root's plane: <paramref name="share"/> of the paper's size at <paramref name="size"/> from its root, in its root's frame (the sheet's lift, swing and the held drawing toward the camera left out).</summary>
    private static Vector3 Held(DeskDocument paper, Vector2 share, float size) =>
        paper.transform.position + paper.transform.rotation * new Vector3(share.x * size, 0f, share.y * size);

    /// <summary>The screen point (pixels, y from the bottom) of <paramref name="world"/> through the view-projection <paramref name="viewProjection"/> (a camera's pose at an earlier frame).</summary>
    private static Vector2 ScreenOf(Matrix4x4 viewProjection, Vector3 world)
    {
        Vector4 c = viewProjection * new Vector4(world.x, world.y, world.z, 1f);
        if (Mathf.Abs(c.w) < 1e-6f)
            return new Vector2(float.NaN, float.NaN);
        return new Vector2((c.x / c.w * 0.5f + 0.5f) * Screen.width, (c.y / c.w * 0.5f + 0.5f) * Screen.height);
    }

    /// <summary>Where <paramref name="screen"/> meets the plane <paramref name="height"/> over the desk through <paramref name="camera"/> (<paramref name="onDesk"/> for none, or when it does not meet it).</summary>
    private static Vector3 AtHeight(DeskSurface surface, Camera camera, Vector2 screen, float height, Vector3 onDesk)
    {
        if (height <= 0f || camera == null)
            return onDesk;
        Ray ray = camera.ScreenPointToRay(screen);
        Vector3 up = surface.transform.up;
        return new Plane(up, surface.transform.position + up * height).Raycast(ray, out float enter) ? ray.GetPoint(enter) : onDesk;
    }

    /// <summary>Where <paramref name="screen"/> meets the desk plane through <paramref name="camera"/>.</summary>
    private static bool OnPlane(DeskSurface surface, Camera camera, Vector2 screen, out Vector3 point)
    {
        point = Vector3.zero;
        return surface != null && camera != null && surface.TryProject(camera, screen, out point);
    }

    private static string F1(float v) => float.IsNaN(v) ? "-" : v.ToString("0.0", CultureInfo.InvariantCulture);

    private static string F4(float v) => v.ToString("0.0000", CultureInfo.InvariantCulture);
}
#endif
