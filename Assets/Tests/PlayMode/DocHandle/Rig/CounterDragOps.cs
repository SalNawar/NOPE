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
/// DOC-HANDLE's rig op <c>counterDrag</c> (FOLD-01 round 3; Papers, Please:
/// a paper pressed on the counter follows the pointer one to one onto the
/// desk). It turns the view as the step says (text "office" or "reading"),
/// waits until the camera is still (QaGame.UntilCameraStill: a press point
/// taken while the view still turns misses the paper), presses the first
/// counter paper the pointer can reach at its middle, and drags it with the
/// rig's normal drag (P-PLAIN, about 600 px/s at 30 fps) to (x2, y2), screen
/// fractions with y from the bottom, or to the mat's middle when both are 0.
/// The held point is where the press met the desk plane, kept as a share of
/// the paper's size (a paper grows from its counter size to its desk size
/// under the cursor). Every frame from the press to the settle's end goes to
/// the run's counterdrag.tsv: the pointer, the paper's zone and size, its
/// root, the root the pointer asks for (the pointer's point plus the grab
/// offset), how far the drag's bound held it back, and the held point's
/// distance from the pointer on the screen (the held point on the desk plane,
/// so a held paper's lift does not count; drawn_px1080 gives the same point
/// as the sheet is drawn, its lift and swing included, reported only).
/// Asserts:
/// <list type="bullet">
/// <item><c>counterDrag.&lt;name&gt;.follow</c>: while the drag runs, the held
/// point stays at most <c>f</c> px at 1080p (default 8, DOC-HANDLE D1's 8 px)
/// from the pointer on every frame.</item>
/// <item><c>counterDrag.&lt;name&gt;.rest</c>: after <c>n</c> settle frames
/// (default 45) the paper lies on the desk (its zone the desk's, not
/// docked, not sliding), and its held point lies at most <c>f</c> px at 1080p
/// from where the pointer let it go (both through the camera of now: a drop
/// on the desk tilts the reading view in, D1).</item>
/// </list>
/// The drag plays at the fixed 30 fps step, as the drop op's does.
/// </summary>
public static class CounterDragOps
{
    private const string Owner = "DOC-HANDLE";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
#endif
    private static void Register() => QaRunner.RegisterOp("counterDrag", CounterDragOp);

    private static IEnumerator CounterDragOp(QaRunner runner, QaStep step)
    {
        string name = QaRunner.Safe(string.IsNullOrEmpty(step.name) ? "paper" : step.name);
        string follow = $"counterDrag.{name}.follow", rest = $"counterDrag.{name}.rest";
        float limit = step.f > 0f ? step.f : 8f;
        bool reading = string.Equals((step.text ?? string.Empty).Trim(), "reading", StringComparison.OrdinalIgnoreCase);
        DeskView view = QaGame.Find<DeskView>();
        QaInput input = runner.Input;
        if (view == null || input == null || !DeskLayout.Bound)
        {
            Fail(runner, follow, rest, "no desk", "counterDrag needs the virtual mouse, the desk view and a bound desk.");
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

        Camera camera = QaGame.PointerCamera();
        DeskDocument paper = null;
        Vector2 from = default;
        float until = Time.realtimeSinceStartup + 10f;
        while (paper == null && Time.realtimeSinceStartup < until)
        {
            camera = QaGame.PointerCamera();
            foreach (DeskDocument p in UnityEngine.Object.FindObjectsByType<DeskDocument>(FindObjectsInactive.Exclude).Where(d => d.isActiveAndEnabled && d.Zone == DeskZone.Counter && !d.IsSliding).OrderBy(d => d.Index))
            {
                if (camera == null || p.Sheet == null)
                    break;
                Vector3 s = camera.WorldToScreenPoint(p.Sheet.position);
                GameObject top = s.z > 0f ? QaGame.TopAt(s) : null;
                if (top != null && top.transform.IsChildOf(p.transform))
                {
                    paper = p;
                    from = s;
                    break;
                }
            }
            if (paper == null)
                yield return null;
        }
        if (paper == null)
        {
            Fail(runner, follow, rest, "no counter paper under the pointer", $"No paper lay on the counter where the pointer could press it in the {(reading ? "reading" : "office")} view within 10 s.");
            yield break;
        }
        DeskSurface surface = QaGame.Field(paper.Drag, "_surface") as DeskSurface;
        Vector2 to;
        if (step.x2 > 0f || step.y2 > 0f)
            to = new Vector2(step.x2 * Screen.width, step.y2 * Screen.height);
        else
        {
            (float x, float y) mid = DeskLayout.Mat.Rect.PointAt(0.5f, 0.45f);
            Vector3 s = camera.WorldToScreenPoint(DeskLayout.Frame.Point(new Vector2(mid.x, mid.y)));
            to = new Vector2(s.x, s.y);
        }

        // The held point: where the press meets the desk plane, as a share of the paper's size in its root's frame.
        float size = SizeNow(paper);
        if (surface == null || !OnPlane(surface, camera, from, out Vector3 pressed) || size <= 0f)
        {
            Fail(runner, follow, rest, "no desk plane", "The paper's drag has no desk surface, or the press does not meet its plane.");
            yield break;
        }
        Vector3 local = Quaternion.Inverse(paper.transform.rotation) * (pressed - paper.transform.position);
        var share = new Vector2(local.x / size, local.z / size);
        // The same point on the sheet as it is drawn (its lift, swing and the held drawing toward the camera included): reported only.
        Ray pressRay = camera.ScreenPointToRay(from);
        var sheetPlane = new Plane(surface.transform.up, paper.Sheet.position);
        Vector3 onSheet = sheetPlane.Raycast(pressRay, out float enter) ? pressRay.GetPoint(enter) : pressed;
        Vector3 sheetLocal = paper.Sheet.InverseTransformPoint(onSheet);

        QaPath path = QaPaths.Drag(QaPathKind.Plain, from, to, new Vector2(Screen.width, Screen.height));
        bool fixedHere = Time.captureFramerate != SeqRecorder.Fps;
        int rate = Time.captureFramerate;
        if (fixedHere)
            Time.captureFramerate = SeqRecorder.Fps;
        var rows = new StringBuilder();
        string tsv = Path.Combine(runner.OutDir, "counterdrag.tsv");
        if (!File.Exists(tsv))
            rows.AppendLine("step\ti\tframe\tptrX\tptrY_top\tdown\tdragging\tzone\tsize\tgrabScale\trootX_m\trootY_m\twantX_m\twantY_m\theldBack_mm\tpointerX_m\tpointerY_m\theldX_m\theldY_m\terr_px1080\tdrawn_px1080");
        float worst = 0f, atRelease = float.NaN, drawnWorst = 0f, drawnAtRelease = float.NaN;
        int worstFrame = -1, dragFrames = 0, firstDrag = -1;
        Vector3? releasedAt = null;
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
                Vector3 held = Held(paper, share, sizeNow);
                bool onPlane = OnPlane(surface, eye, pointer, out Vector3 point);
                Vector3 want = onPlane && paper.Drag != null ? point + paper.Drag.GrabOffset : paper.transform.position;
                Vector3 root = paper.transform.position;
                float back = Vector3.ProjectOnPlane(root - want, Vector3.up).magnitude;
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
                    if (onPlane)
                        releasedAt = point;
                }
                Vector2 r = DeskLayout.Frame.Local(root), w = DeskLayout.Frame.Local(want), pd = DeskLayout.Frame.Local(point), hd = DeskLayout.Frame.Local(held);
                rows.Append(name).Append('\t').Append(index).Append('\t').Append(Time.frameCount).Append('\t').Append(F1(pointer.x)).Append('\t').Append(F1(Screen.height - pointer.y))
                    .Append('\t').Append(down ? 1 : 0).Append('\t').Append(dragging ? 1 : 0).Append('\t').Append(paper.Zone).Append('\t').Append(F4(sizeNow))
                    .Append('\t').Append(F4(paper.Drag != null ? paper.Drag.GrabScale : 1f))
                    .Append('\t').Append(F4(r.x)).Append('\t').Append(F4(r.y)).Append('\t').Append(F4(w.x)).Append('\t').Append(F4(w.y)).Append('\t').Append(F1(back * 1000f))
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

        string how = FormattableString.Invariant($"{paper.name} (paper {paper.Index}), the {(reading ? "reading" : "office")} view{(still ? string.Empty : " (the camera never stood still for 3 frames in 10 s)")}: pressed at {from.x:0},{from.y:0}, let go at {path.End.x:0},{path.End.y:0} (screen px, y from the bottom), the held point {share.x:0.00},{share.y:0.00} of its size from its centre; counterdrag.tsv has every frame");
        runner.Assert(follow, Owner, dragFrames > 0 && worst <= limit,
                      dragFrames > 0 ? FormattableString.Invariant($"the held point {worst:0.0} px from the pointer at worst (frame {worstFrame}), {atRelease:0.0} px on the last held frame (max {limit:0.#}); {dragFrames} frames held from frame {firstDrag}; as drawn (the drag's lift and swing, not asserted) {drawnWorst:0.0} px at worst, {drawnAtRelease:0.0} px on the last held frame") : "the drag never began",
                      how);

        Camera now = QaGame.PointerCamera();
        float restPx = float.NaN;
        if (now != null && releasedAt != null && paper != null)
        {
            Vector3 a = now.WorldToScreenPoint(releasedAt.Value), b = now.WorldToScreenPoint(Held(paper, share, SizeNow(paper)));
            if (a.z > 0f && b.z > 0f)
                restPx = QaCameraMoves.To1080(new Vector2(b.x - a.x, b.y - a.y).magnitude, Screen.height);
        }
        bool onDesk = paper != null && paper.Zone == DeskZone.Desk && !paper.IsDocked && !paper.IsSliding;
        runner.Assert(rest, Owner, onDesk && !float.IsNaN(restPx) && restPx <= limit,
                      paper == null ? "the paper is gone" : !onDesk ? $"it lies in the {paper.Zone} zone{(paper.IsDocked ? ", docked" : string.Empty)}{(paper.IsSliding ? ", still sliding" : string.Empty)}, not on the desk"
                      : FormattableString.Invariant($"its held point rests {restPx:0.0} px from where the pointer let it go (max {limit:0.#})"),
                      how + "; both places through the camera of now");
    }

    /// <summary>Both of a step's assertions red with the same reason.</summary>
    private static void Fail(QaRunner runner, string follow, string rest, string measured, string detail)
    {
        runner.Assert(follow, Owner, false, measured, detail);
        runner.Assert(rest, Owner, false, measured, detail);
    }

    /// <summary>The paper's sheet scale now (its shown size over its size).</summary>
    private static float SizeNow(DeskDocument paper) => paper.Size.x > 0f ? paper.ShownSize.x / paper.Size.x : 0f;

    /// <summary>The held point on the desk plane: <paramref name="share"/> of the paper's size at <paramref name="size"/> from its root, in its root's frame (the sheet's lift, swing and the held drawing toward the camera left out).</summary>
    private static Vector3 Held(DeskDocument paper, Vector2 share, float size) =>
        paper.transform.position + paper.transform.rotation * new Vector3(share.x * size, 0f, share.y * size);

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
