#if UNITY_EDITOR || TIMEDESK_QA
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

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
/// clock runs; it forces no hour (the scenario clears any with "hour" -1 first).
/// </summary>
public static class HallFxRig
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        QaRunner.RegisterOp("hallPortals", (runner, step) => Show(step.n != 0));
        QaRunner.RegisterOp("hallPortalsShot", Shot);
        QaRunner.RegisterOp("hallClock", Clock);
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
        File.AppendAllText(Path.Combine(runner.OutDir, "qa_report.txt"), report.ToString());
        runner.Recorder.Still(Path.Combine(dir, $"h{hour:00}_{Screen.width}x{Screen.height}_on.png"));
        yield return QaGame.Until(() => !runner.Recorder.StillPending, 10f, "the on still");
        yield return Show(false);
        runner.Recorder.Still(Path.Combine(dir, $"h{hour:00}_{Screen.width}x{Screen.height}_off.png"));
        yield return QaGame.Until(() => !runner.Recorder.StillPending, 10f, "the off still");
        yield return Show(true);
    }
}
#endif
