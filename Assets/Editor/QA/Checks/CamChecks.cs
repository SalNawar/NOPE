using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;

/// <summary>
/// CAM's gate checks (BUG-17a, 1b, 7b, 32b, 41, 42, 55c, 59, 64b, 69, 73e;
/// FIX_PLAN_v2's CAM delta; docs/pending/CAM.md). Static: no close-up code
/// path is left in the game's scripts (D6: no machine close-ups; the camera
/// logger proves none happens); the reading pose solves at the four aspects
/// for the live desk config, every framed point (the big mat, the side homes'
/// inner strips, the deal tray) inside the margins and the bottom row on the
/// desk; the office pose's lens at 21:9 no wider than the hall art. Play
/// (read from the QA rig's runs of tools/qa/scenarios/cam_views.json,
/// QaJobs.Scenario): every camera move tagged (the logger); the framing at
/// A1-A4 in both poses; the controls' fade over a move; no view change
/// while holding; the city's parallax and its full-screen fill.
/// </summary>
public static class CamChecks
{
    private const string Owner = "CAM";

    /// <summary>The aspects the plan names: A1 16:9, A2 16:9 at 720p (the same shape), 16:10, A3 21:9, A4 5:4.</summary>
    private static readonly (string name, float aspect)[] Aspects = { ("16:9", 16f / 9f), ("16:10", 16f / 10f), ("21:9", 2560f / 1080f), ("5:4", 5f / 4f) };

    /// <summary>An identifier naming a close-up (MachineCloseUp, CloseUpPolicy, closeUpDistance...) in code, comments and strings left out.</summary>
    private static readonly Regex CloseUp = new Regex(@"\b\w*[Cc]lose_?[Uu]p\w*\b", RegexOptions.Compiled);

    /// <summary>No game script names a close-up in code (D6, BUG-17a: "A grep check finds 0 close-up code paths").</summary>
    [GateCheck("cam.closeups.none", Owner)]
    public static GateResult NoCloseUps()
    {
        var hits = new List<string>();
        foreach (string file in Directory.GetFiles("Assets/Scripts", "*.cs", SearchOption.AllDirectories))
        {
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                string code = CodeOf(lines[i]);
                if (CloseUp.IsMatch(code))
                    hits.Add($"{file.Replace('\\', '/')}:{i + 1}");
            }
        }
        return GateResult.Of("cam.closeups.none", Owner, hits.Count == 0, $"{hits.Count} close-up code line(s)", string.Join(", ", hits.Take(20)));
    }

    /// <summary>A line with its comment and string literals taken out.</summary>
    private static string CodeOf(string line)
    {
        string code = Regex.Replace(line, "\"(?:[^\"\\\\]|\\\\.)*\"", "\"\"");
        int comment = code.IndexOf("//", System.StringComparison.Ordinal);
        return comment >= 0 ? code.Substring(0, comment) : code;
    }

    /// <summary>
    /// The reading pose solves at every aspect for the live desk config (the
    /// approved home map's mat and homes, DeskConfigSO.deskView), with every
    /// framed point inside the screen's margins and the screen's bottom row
    /// on the desk's top (BUG-1b, 7b, 41).
    /// </summary>
    [GateCheck("cam.lens.reading", Owner)]
    public static GateResult ReadingPose()
    {
        DeskConfigSO config = AssetDatabase.LoadAssetAtPath<DeskConfigSO>(OfficeSceneUIBuilder.DeskConfigPath);
        if (config == null)
            return GateResult.Fail("cam.lens.reading", Owner, "no config", $"No DeskConfigSO at {OfficeSceneUIBuilder.DeskConfigPath}.");
        DeskViewTuning t = config.deskView;
        List<(float x, float y, float z)> targets = DeskViewPose.Targets(t, config.layout.mat.Rect, config.layout.homes);
        DeskRect deskTop = config.layout.deskTop.Rect;
        float front = deskTop.CentreY - deskTop.Height / 2f;
        float pitch = DeskViewPose.Pitch(t);
        var problems = new List<string>();
        var measured = new List<string>();
        foreach ((string name, float aspect) in Aspects)
        {
            if (!LensFit.Reading(targets, pitch, t.fieldOfView, aspect, t.topMargin, t.bottomMargin, t.sideMargin, front, out ReadingPose pose))
            {
                problems.Add($"{name}: no pose");
                continue;
            }
            int outside = 0;
            foreach ((float x, float y, float z) p in targets)
                if (!LensFit.Project(pose, pitch, t.fieldOfView, aspect, p, out (float x, float y) ndc) ||
                    ndc.y > 1f - t.topMargin + 1e-3f || ndc.y < -(1f - t.bottomMargin) - 1e-3f || System.Math.Abs(ndc.x) > 1f - t.sideMargin + 1e-3f)
                    outside++;
            if (outside > 0)
                problems.Add($"{name}: {outside}/{targets.Count} framed points outside the margins");
            if (pose.BottomRowY < front - 1e-3f)
                problems.Add($"{name}: the bottom row meets the desk at {pose.BottomRowY:0.000} m, past its front edge {front:0.000}");
            measured.Add($"{name} {pose.Distance:0.00} m");
        }
        return GateResult.Of("cam.lens.reading", Owner, problems.Count == 0, string.Join(", ", measured), string.Join("; ", problems));
    }

    /// <summary>The office pose at 21:9 shows no more width than the hall art covers, and at 16:9 is the plan's lens (BUG-41).</summary>
    [GateCheck("cam.lens.office", Owner)]
    public static GateResult OfficePose()
    {
        DeskConfigSO config = AssetDatabase.LoadAssetAtPath<DeskConfigSO>(OfficeSceneUIBuilder.DeskConfigPath);
        if (config == null)
            return GateResult.Fail("cam.lens.office", Owner, "no config", $"No DeskConfigSO at {OfficeSceneUIBuilder.DeskConfigPath}.");
        float fov = config.layout.officeFieldOfView;
        (float wideFov, float wideTilt) = LensFit.Fit(fov, LensFit.DesignAspect, 2560f / 1080f, config.officeViewBottom);
        double art = System.Math.Tan(fov * System.Math.PI / 360.0) * LensFit.ArtAspect;
        double shown = System.Math.Tan(wideFov * System.Math.PI / 360.0) * (2560.0 / 1080.0);
        (float designFov, float designTilt) = LensFit.Fit(fov, LensFit.DesignAspect, LensFit.DesignAspect, config.officeViewBottom);
        bool ok = shown <= art + 1e-4 && designFov == fov && designTilt == 0f;
        return GateResult.Of("cam.lens.office", Owner, ok, $"21:9: {wideFov:0.0} deg, {wideTilt:0.0} down; 16:9: {designFov:0.0} deg",
                             ok ? "the hall art covers the 21:9 width" : "the 21:9 office view is wider than the hall art, or 16:9 is not the plan's lens");
    }

    /// <summary>Every camera move of the CAM runs is tagged with its cause, and every impulse stays under its cap (G9; the QA camera logger).</summary>
    [GateCheck("cam.play.tags", Owner, GateKind.Play)]
    public static GateResult Tags() => GateResults.Require("cam.play.tags", Owner, "cam_views.camera*");

    /// <summary>Both poses frame what they must at A1-A4 (≥98% of the framed points on screen; the reading view's bottom row on the desk).</summary>
    [GateCheck("cam.play.framing", Owner, GateKind.Play)]
    public static GateResult Framing() => GateResults.Require("cam.play.framing", Owner, "cam_views.frame.*");

    /// <summary>The controls of a view appear only over the last third of the move into it (BUG-32b).</summary>
    [GateCheck("cam.play.fade", Owner, GateKind.Play)]
    public static GateResult ControlFade() => GateResults.Require("cam.play.fade", Owner, "cam_views.fade.*");

    /// <summary>No view changes while something is held (BUG-55c).</summary>
    [GateCheck("cam.play.hold", Owner, GateKind.Play)]
    public static GateResult Hold() => GateResults.Require("cam.play.hold", Owner, "cam_views.hold.*");

    /// <summary>The city fills the screen and its parallax moves at most 1 px at 1080p a frame (BUG-42, BUG-69).</summary>
    [GateCheck("cam.play.city", Owner, GateKind.Play)]
    public static GateResult City() => GateResults.Require("cam.play.city", Owner, "cam_views.city.*");
}
