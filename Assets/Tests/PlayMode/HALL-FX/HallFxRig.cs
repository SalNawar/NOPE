#if UNITY_EDITOR || TIMEDESK_QA
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// HALL-FX's ops for QA-RIG scenarios. BUG-26a ("the circular probe is replaced by the effect-on vs effect-off
/// diff"): <c>hallPortals</c> (n 0 hides every ring's vortex, 1 shows them again: their GameObjects, so nothing
/// the game drives is touched), and <c>hallPortalsShot</c> (name = the hour tag, e.g. "12"): a still with the
/// vortexes on and one with them off into shots/portals/h&lt;tag&gt;_&lt;width&gt;x&lt;height&gt;_on|off.png (the width
/// too: A1 and A3 are both 1080 tall), and each effect's screen quad appended to qa_report.txt in the run folder,
/// which tools/hall/portal_check.py reads (the painted hole registered on the off frame against the vortex, and
/// the effect's pixels outside the hole). BUG-13 on a running clock: <c>hallClock</c> (f = an hour of the day)
/// moves today's shift clock forward to that hour (ShiftClock.Spend: it never runs backwards, and closing time
/// still closes) and adds two columns to every captured frame from then on, hallHour (the hour the hall shows)
/// and cityLit (_HallCityLights.x, the city's lit share), so a dusk sequence shows its windows lighting while the
/// clock runs; it forces no hour (the scenario clears any with "hour" -1 first). BUG-71: <c>hallHitStop</c> is the game's
/// hit-stop alone (MotionKnobs.hitStopScale for hitStopSeconds of real time, as FeelDirector.HitStop does, then the
/// time scale back), without the citation punch's camera shake, which is FEEDBACK's and CAM's and would fail
/// hall_fx_crossfade's flicker check on their motion, not on the crossfade's. BUG-26a: <c>hallPortalsOpen</c> opens every
/// ring's vortex (PortalEffect.Show, the departure look, on the asset's tuning) and waits for the irises, so the portal
/// shots measure all five rings: on a day's plan most rings are shut (day 1 opens one), and an effect's placement and
/// mask do not depend on which day opens it. Each portal shot also writes the hall drawing's own projection (its canvas
/// corners on screen), where tools/hall/portal_check.py lays the rings' masks; <c>hallDrawing</c> writes it alone (the
/// crossfade's both-faces mask, tools/hall/slot_mask.py). The two portal stills are taken with LIGHT's office finish
/// (HallLightingSO.finishProfile's Volume) at weight 0: its bloom glows round each ring by design, and the halo count
/// is taken before LIGHT's Volume (HALL-FX.md step 10); the weight is put back after the off still.
/// </summary>
public static class HallFxRig
{
    /// <summary>
    /// Registers the ops before the first scene of a play run (the editor and the QA player) and, in the editor,
    /// also at every domain load, so the edit-mode scenario check (QaEditorGateTests.EveryScenario_Reads_WithKnownOps)
    /// knows them without a play run. Registering twice just replaces the same bodies.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
#endif
    private static void Register()
    {
        QaRunner.RegisterOp("hallPortals", (runner, step) => Show(step.n != 0));
        QaRunner.RegisterOp("hallPortalsShot", Shot);
        QaRunner.RegisterOp("hallClock", Clock);
        QaRunner.RegisterOp("hallHitStop", (runner, step) => HitStop());
        QaRunner.RegisterOp("hallPortalsOpen", (runner, step) => OpenAll());
        QaRunner.RegisterOp("hallDrawing", Drawing);
    }

    /// <summary>
    /// <c>hallDrawing</c>: appends the hall drawing's projection to qa_report.txt (<see cref="DrawingLine"/>), so an
    /// offline tool can lay canvas-space art (a slot's faces: tools/hall/slot_mask.py) on the frames of the screen
    /// size it was taken at.
    /// </summary>
    private static IEnumerator Drawing(QaRunner runner, QaStep step)
    {
        File.AppendAllText(Path.Combine(runner.OutDir, "qa_report.txt"),
                           $"== drawing at {Screen.width}x{Screen.height}\n" + DrawingLine());
        yield return null;
    }

    /// <summary>
    /// The hall drawing's own projection (the painting the rings and slots are drawn on, HallPortalsSO.drawing):
    /// its canvas corners top-left, top-right, bottom-left and bottom-right as the office camera projects its sprite
    /// (screen px, y up; HallBackdrop's camera copies that camera's pose, lens and screen). The sprite is flat, so the
    /// four corners give the exact canvas-to-screen map (a homography) the GPU draws the painting with, independent of
    /// how PortalEffect or HallSlotsLink place their own quads. Empty when the hall or the drawing is missing.
    /// </summary>
    private static string DrawingLine()
    {
        Camera camera = Camera.main;
        AnimeHallPresentation hall = Object.FindAnyObjectByType<AnimeHallPresentation>();
        HallPortalsSO portals = null;
        foreach (PortalEffect e in Object.FindObjectsByType<PortalEffect>(FindObjectsInactive.Include))
            if (e.Portals != null)
            {
                portals = e.Portals;
                break;
            }
        SpriteRenderer drawing = hall != null && portals != null ? hall.FindLayer(portals.drawing) : null;
        if (camera == null || drawing == null || drawing.sprite == null)
            return "   drawing missing\n";
        Sprite s = drawing.sprite;
        float ppu = s.pixelsPerUnit, w = s.rect.width, h = s.rect.height;
        Vector2 pivot = s.pivot;
        var line = new StringBuilder($"   drawing canvas {portals.canvas.x:0}x{portals.canvas.y:0} corners");
        foreach (Vector2 corner in new[] { new Vector2(0f, h), new Vector2(w, h), new Vector2(0f, 0f), new Vector2(w, 0f) })
        {
            Vector3 world = drawing.transform.TransformPoint(new Vector3((corner.x - pivot.x) / ppu, (corner.y - pivot.y) / ppu, 0f));
            Vector3 p = camera.WorldToScreenPoint(world);
            line.AppendFormat(CultureInfo.InvariantCulture, " ({0:0.000},{1:0.000})", p.x, p.y);
        }
        return line.Append('\n').ToString();
    }

    private static IEnumerator Clock(QaRunner runner, QaStep step)
    {
        ShiftClockDriver driver = Object.FindAnyObjectByType<ShiftClockDriver>();
        if (driver == null || driver.Clock == null || !driver.Clock.IsStarted)
            throw new System.InvalidOperationException("hallClock: no running shift clock (press the AVAILABLE sign first).");
        float minutes = step.f * 60f - driver.MinuteOfDay;
        if (minutes > 0f)
            driver.Spend(minutes);
        HallLightingRig lights = Object.FindAnyObjectByType<HallLightingRig>();
        runner.Recorder.Column("hallHour", () => lights != null ? lights.Hour.ToString("0.000", CultureInfo.InvariantCulture) : "-");
        runner.Recorder.Column("cityLit", () => Shader.GetGlobalVector("_HallCityLights").x.ToString("0.0000", CultureInfo.InvariantCulture));
        yield return QaGame.Frames(5);
    }

    private static IEnumerator OpenAll()
    {
        foreach (PortalEffect e in Object.FindObjectsByType<PortalEffect>(FindObjectsInactive.Include))
            if (e.Portals != null)
                e.Show(true, false, e.Portals.vortex, e.Portals.pulseSeconds);
        yield return new WaitForSecondsRealtime(2f);
    }

    private static IEnumerator HitStop()
    {
        MotionKnobs knobs = UiMotion.Knobs;
        float scale = Time.timeScale;
        Time.timeScale = Mathf.Clamp(knobs.hitStopScale, 0f, 1f);
        float until = Time.unscaledTime + knobs.hitStopSeconds;
        while (Time.unscaledTime < until)
            yield return null;
        Time.timeScale = scale;
    }

    private static IEnumerator Show(bool on)
    {
        foreach (PortalEffect e in Object.FindObjectsByType<PortalEffect>(FindObjectsInactive.Include))
            e.gameObject.SetActive(on);
        yield return QaGame.Frames(3);
    }

    private static IEnumerator Shot(QaRunner runner, QaStep step)
    {
        string dir = Path.Combine(runner.OutDir, "shots", "portals");
        Directory.CreateDirectory(dir);
        string tag = string.IsNullOrEmpty(step.name) ? "12" : step.name;
        int hour = Mathf.RoundToInt(float.Parse(tag, CultureInfo.InvariantCulture));
        var report = new StringBuilder($"== portals at {hour} {Screen.width}x{Screen.height}\n");
        Camera camera = Camera.main;
        foreach (PortalEffect e in Object.FindObjectsByType<PortalEffect>(FindObjectsInactive.Include))
        {
            Transform t = e.transform;
            Vector3 a = camera.WorldToScreenPoint(t.TransformPoint(new Vector3(-0.5f, -0.5f, 0f)));
            Vector3 b = camera.WorldToScreenPoint(t.TransformPoint(new Vector3(0.5f, 0.5f, 0f)));
            Vector3 c = camera.WorldToScreenPoint(t.position);
            report.AppendFormat(CultureInfo.InvariantCulture, "   {0} showing {1} night {2:0.00} centre ({3:0.0},{4:0.0}) quad ({5:0.0},{6:0.0})-({7:0.0},{8:0.0})\n",
                                e.name, e.Showing, e.Night, c.x, c.y, Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }
        report.Append(DrawingLine());
        List<(Volume volume, float weight)> finish = FinishOff();
        report.AppendFormat(CultureInfo.InvariantCulture, "   office finish off for the stills: {0} volume(s)\n", finish.Count);
        File.AppendAllText(Path.Combine(runner.OutDir, "qa_report.txt"), report.ToString());
        yield return QaGame.Frames(2);
        runner.Recorder.Still(Path.Combine(dir, $"h{hour:00}_{Screen.width}x{Screen.height}_on.png"));
        yield return QaGame.Until(() => !runner.Recorder.StillPending, 10f, "the on still");
        yield return Show(false);
        runner.Recorder.Still(Path.Combine(dir, $"h{hour:00}_{Screen.width}x{Screen.height}_off.png"));
        yield return QaGame.Until(() => !runner.Recorder.StillPending, 10f, "the off still");
        foreach ((Volume volume, float weight) in finish)
            if (volume != null)
                volume.weight = weight;
        yield return Show(true);
    }

    /// <summary>Sets LIGHT's office finish Volume(s) (the profile HallLightingRig puts on a global Volume at play) to weight 0; returns each with its weight, to put back.</summary>
    private static List<(Volume volume, float weight)> FinishOff()
    {
        var off = new List<(Volume volume, float weight)>();
        HallLightingRig rig = Object.FindAnyObjectByType<HallLightingRig>();
        VolumeProfile profile = rig != null && rig.Settings != null ? rig.Settings.finishProfile : null;
        if (profile == null)
            return off;
        foreach (Volume volume in Object.FindObjectsByType<Volume>(FindObjectsInactive.Exclude))
            if (volume.sharedProfile == profile)
            {
                off.Add((volume, volume.weight));
                volume.weight = 0f;
            }
        return off;
    }
}
#endif
