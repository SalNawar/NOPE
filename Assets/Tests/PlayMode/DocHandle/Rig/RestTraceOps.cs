#if UNITY_EDITOR || TIMEDESK_QA
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// DOC-HANDLE's rig op <c>restTrace</c> (FOLD-01, Saleh's playtest of
/// 2026-10-28, point 4: "You move the folder and it snaps somewhere else"):
/// it reads the desk's rest trace (DropTrace), never a shipped log. A step
/// with text "on" starts the trace (and forgets older entries), "off" stops
/// it. Any other step writes every rest recorded since the last write to the
/// run's rests.tsv under the step's name: per rest the footprint at the
/// release, the area it must lie in, the rest, the move, and every keep-out
/// with its source, whether it moved the rest, and its overlap with the
/// footprint at the release and at the rest. Each rectangle is given in the
/// view's frame (metres) and on the screen (pixels, origin TOP-left, as the
/// stills; the pointer camera of the moment of the write). With
/// <c>f</c> over 0 the step asserts <c>restTrace.&lt;name&gt;</c>: at least
/// one rest was recorded, none moved more than <c>f</c> pixels at 1080p from
/// its release (DOC-HANDLE D1's 8 px), and each lies wholly in its area and
/// over no keep-out.
/// </summary>
public static class RestTraceOps
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
#endif
    private static void Register() => QaRunner.RegisterOp("restTrace", RestTraceOp);

    private static IEnumerator RestTraceOp(QaRunner runner, QaStep step)
    {
        yield return null;
        string text = (step.text ?? string.Empty).Trim().ToLowerInvariant();
        if (text == "on")
        {
            DropTrace.Entries.Clear();
            DropTrace.On = true;
            yield break;
        }
        if (text == "off")
        {
            DropTrace.On = false;
            yield break;
        }
        string name = QaRunner.Safe(string.IsNullOrEmpty(step.name) ? "rests" : step.name);
        Camera camera = QaGame.PointerCamera();
        string path = Path.Combine(runner.OutDir, "rests.tsv");
        bool header = !File.Exists(path);
        var sb = new StringBuilder();
        if (header)
            sb.AppendLine("step\twhat\tframe\trow\tsource\tblocks\tcx_m\tcy_m\tw_m\th_m\tx0_px\ty0_px\tx1_px\ty1_px\tatRelease_m2\tatRest_m2\tnote");
        float worst = 0f;
        int count = DropTrace.Entries.Count;
        var wrong = new List<string>();
        foreach (DropTrace e in DropTrace.Entries)
        {
            var rest = new DeskRect(e.RestX, e.RestY, e.Footprint.Width, e.Footprint.Height);
            float px = MovePx(e, camera);
            worst = Mathf.Max(worst, px);
            float over = 0f;
            foreach ((string source, DeskRect rect, bool blocks) in e.KeepOut)
                if (blocks && Shared(rest, rect) > 1e-6f)
                {
                    over += Shared(rest, rect);
                    wrong.Add($"{e.What} rests over {source}");
                }
            if (!Within(rest, e.Area))
                wrong.Add($"{e.What} rests partly outside the rest area");
            Row(sb, name, e, "release", "footprint", true, e.Footprint, camera, 0f, 0f, string.Empty);
            Row(sb, name, e, "area", "restArea", true, e.Area, camera, 0f, 0f,
                FormattableString.Invariant($"surfaceY {e.SurfaceY:0.000}, rootY {e.Origin.y:0.000}; view NL {e.View[0].x:0.000},{e.View[0].y:0.000} NR {e.View[1].x:0.000},{e.View[1].y:0.000} FL {e.View[2].x:0.000},{e.View[2].y:0.000} FR {e.View[3].x:0.000},{e.View[3].y:0.000}"));
            Row(sb, name, e, "clamp", "surfaceClamp", true, e.Clamp, camera, 0f, 0f, string.Empty);
            Row(sb, name, e, "rest", "footprint", true, rest, camera, 0f, 0f, $"move {e.Move:0.0000} m, {px:0.0} px at 1080p");
            foreach ((string source, DeskRect rect, bool blocks) in e.KeepOut)
                Row(sb, name, e, "keepOut", source, blocks, rect, camera, Shared(e.Footprint, rect), Shared(rest, rect), string.Empty);
        }
        File.AppendAllText(path, sb.ToString());
        DropTrace.Entries.Clear();
        if (step.f > 0f)
            runner.Assert("restTrace." + name, "DOC-HANDLE", count > 0 && worst <= step.f && wrong.Count == 0,
                          count > 0 ? $"{count} rest(s), the largest move {worst:0.0} px at 1080p (max {step.f:0.#}), {wrong.Count} over a keep-out or out of the area" : "no rest recorded",
                          count == 0 ? "Nothing was let go on the desk since the last write (or the trace is off)."
                                     : wrong.Count > 0 ? string.Join("; ", wrong) : "rests.tsv lists each rest's keep-outs (DropTrace)");
    }

    /// <summary>True when <paramref name="inner"/> lies wholly inside <paramref name="outer"/> (0.1 mm of grace).</summary>
    private static bool Within(DeskRect inner, DeskRect outer) =>
        inner.CentreX - inner.Width / 2f >= outer.CentreX - outer.Width / 2f - 1e-4f && inner.CentreX + inner.Width / 2f <= outer.CentreX + outer.Width / 2f + 1e-4f &&
        inner.CentreY - inner.Height / 2f >= outer.CentreY - outer.Height / 2f - 1e-4f && inner.CentreY + inner.Height / 2f <= outer.CentreY + outer.Height / 2f + 1e-4f;

    /// <summary>How far the rest's centre lies from the release's on the screen, in pixels at 1080p (0 without a camera).</summary>
    private static float MovePx(DropTrace e, Camera camera)
    {
        if (camera == null)
            return 0f;
        Vector3 a = camera.WorldToScreenPoint(e.World(e.Footprint.CentreX, e.Footprint.CentreY));
        Vector3 b = camera.WorldToScreenPoint(e.World(e.RestX, e.RestY));
        return QaCameraMoves.To1080(new Vector2(b.x - a.x, b.y - a.y).magnitude, Screen.height);
    }

    private static void Row(StringBuilder sb, string step, DropTrace e, string row, string source, bool blocks, DeskRect r, Camera camera, float atRelease, float atRest, string note)
    {
        float x0 = float.NaN, y0 = float.NaN, x1 = float.NaN, y1 = float.NaN;
        if (camera != null && r.Width < 50f)
        {
            x0 = y0 = float.MaxValue;
            x1 = y1 = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                Vector3 s = camera.WorldToScreenPoint(e.World(r.CentreX + ((i & 1) == 0 ? -1f : 1f) * r.Width / 2f, r.CentreY + ((i & 2) == 0 ? -1f : 1f) * r.Height / 2f));
                float top = Screen.height - s.y; // origin top-left, as the stills
                x0 = Mathf.Min(x0, s.x);
                x1 = Mathf.Max(x1, s.x);
                y0 = Mathf.Min(y0, top);
                y1 = Mathf.Max(y1, top);
            }
        }
        sb.Append(step).Append('\t').Append(e.What).Append('\t').Append(e.Frame).Append('\t').Append(row).Append('\t').Append(source).Append('\t').Append(blocks ? "1" : "0")
          .Append('\t').Append(F(r.CentreX)).Append('\t').Append(F(r.CentreY)).Append('\t').Append(F(r.Width)).Append('\t').Append(F(r.Height))
          .Append('\t').Append(P(x0)).Append('\t').Append(P(y0)).Append('\t').Append(P(x1)).Append('\t').Append(P(y1))
          .Append('\t').Append(F(atRelease)).Append('\t').Append(F(atRest)).Append('\t').Append(note).AppendLine();
    }

    /// <summary>The area two rectangles share (square metres).</summary>
    private static float Shared(DeskRect a, DeskRect b)
    {
        float x = Mathf.Min(a.CentreX + a.Width / 2f, b.CentreX + b.Width / 2f) - Mathf.Max(a.CentreX - a.Width / 2f, b.CentreX - b.Width / 2f);
        float y = Mathf.Min(a.CentreY + a.Height / 2f, b.CentreY + b.Height / 2f) - Mathf.Max(a.CentreY - a.Height / 2f, b.CentreY - b.Height / 2f);
        return x > 0f && y > 0f ? x * y : 0f;
    }

    private static string F(float v) => v.ToString("0.0000", CultureInfo.InvariantCulture);

    private static string P(float v) => float.IsNaN(v) ? "-" : v.ToString("0", CultureInfo.InvariantCulture);
}
#endif
