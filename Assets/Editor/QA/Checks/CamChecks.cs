using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// CAM's gate checks (BUG-17a, 1b, 7b, 32b, 41, 42, 55c, 59, 64b, 69, 73e;
/// FIX_PLAN_v2's CAM delta; docs/pending/CAM.md). Static: no close-up code
/// path is left in the game's scripts (D6: no machine close-ups; the camera
/// logger proves none happens); the reading pose solves at the four aspects
/// for the live desk config, every framed point (the big mat, the stamp rack
/// and the intercom whole, the other side homes' inner strips, the deal
/// tray) inside the margins under the overlay's top band and the bottom row
/// on the desk; the overlay's top chrome (the case HUD's strips, the Back
/// control) stays inside that band; the office pose's lens at 21:9 no wider
/// than the hall art. Play (read from the QA rig's runs of CAM's scenarios,
/// QaJobs.Scenario): every camera move tagged (the logger) and the drop tilt
/// at most once per paper landing; the scripted day 12 run whole (every
/// drop, the Auto-Feed, every scan and chip read, a ring question); the
/// framing at A1-A4 in both poses, per
/// home; the controls' fade over a move; no view change while holding, call
/// by call; the city's parallax and its full-screen fill; the shake table's
/// rows; no chrome over a held thing or the reading view's work, and the two
/// overlay plates CAM measured over it, each under its owner (CONTENT's guide
/// plate, STAMP's stamp hint); the cap heights measured at CAM's pose;
/// DESK-PLAN's homes at A3 and A4. Built player [BP] (with the demo gate, as
/// QA-RIG's qa.bp.*): no per-frame allocation over the reading toggle and the
/// city look, and that run's camera moves tagged. tools/qa/test_cam_gates.py
/// checks offline that every line CAM's scenarios write that CAM measures
/// (its ops' lines and the built-in ops' CAM lines: gc, cameraCheck, the
/// run's camera) sits under one of these checks, under the line's owner, and
/// the reverse: no check here reads a line of another owner (QA-RIG's,
/// TEXT-LANG's, a plate owner's) but cam.play.deskplan's listed DESK-PLAN and
/// QA-RIG lines.
/// </summary>
public static class CamChecks
{
    private const string Owner = "CAM";

    /// <summary>The aspects the plan names: A1 16:9, A2 16:9 at 720p (the same shape), 16:10, A3 21:9, A4 5:4.</summary>
    private static readonly (string name, float aspect)[] Aspects = { ("16:9", 16f / 9f), ("16:10", 16f / 10f), ("21:9", 2560f / 1080f), ("5:4", 5f / 4f) };

    /// <summary>An identifier naming a close-up (MachineCloseUp, CloseUpPolicy, closeUpDistance...) in code, comments and strings left out.</summary>
    private static readonly Regex CloseUp = new Regex(@"\b\w*[Cc]lose_?[Uu]p\w*\b", RegexOptions.Compiled);

    /// <summary>The least gap (reference px) between the overlay's top chrome and the first framed desk point.</summary>
    private const float BandGap = 8f;

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
    /// framed point inside the screen's margins under the overlay's top band
    /// (DeskViewPose.TopMargin) and the screen's bottom row on the desk's top
    /// (BUG-1b, 7b, 41); measured: the distance and how big papers show
    /// against the old reading view (DeskViewPose.PaperShare).
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
            float top = DeskViewPose.TopMargin(t, aspect);
            if (!LensFit.Reading(targets, pitch, t.fieldOfView, aspect, top, t.bottomMargin, t.sideMargin, front, out ReadingPose pose))
            {
                problems.Add($"{name}: no pose");
                continue;
            }
            int outside = 0;
            foreach ((float x, float y, float z) p in targets)
                if (!LensFit.Project(pose, pitch, t.fieldOfView, aspect, p, out (float x, float y) ndc) ||
                    ndc.y > 1f - top + 1e-3f || ndc.y < -(1f - t.bottomMargin) - 1e-3f || System.Math.Abs(ndc.x) > 1f - t.sideMargin + 1e-3f)
                    outside++;
            if (outside > 0)
                problems.Add($"{name}: {outside}/{targets.Count} framed points outside the margins");
            if (pose.BottomRowY < front - 1e-3f)
                problems.Add($"{name}: the bottom row meets the desk at {pose.BottomRowY:0.000} m, past its front edge {front:0.000}");
            measured.Add($"{name} {pose.Distance:0.00} m, papers {DeskViewPose.PaperShare(pose.Distance, t.fieldOfView):P0}");
        }
        return GateResult.Of("cam.lens.reading", Owner, problems.Count == 0, string.Join(", ", measured), string.Join("; ", problems));
    }

    /// <summary>
    /// The overlay's top chrome (the case HUD's compare and verdict strips,
    /// the desk view's "▲ Back" control) ends at least a gap above the
    /// reading view's top band (DeskViewTuning.topBand), and the band is read
    /// against the overlay's own reference resolution, so no chrome stands
    /// over a framed desk point (BUG-59).
    /// </summary>
    [GateCheck("cam.chrome.band", Owner)]
    public static GateResult ChromeBand()
    {
        DeskConfigSO config = AssetDatabase.LoadAssetAtPath<DeskConfigSO>(OfficeSceneUIBuilder.DeskConfigPath);
        if (config == null)
            return GateResult.Fail("cam.chrome.band", Owner, "no config", $"No DeskConfigSO at {OfficeSceneUIBuilder.DeskConfigPath}.");
        float chrome = OfficeSceneUIBuilder.TopChromeBottom, band = config.deskView.topBand;
        Vector2 reference = OfficeSceneUIBuilder.OverlayReference;
        bool sameReference = reference.x == DeskViewPose.ReferenceWidth && reference.y == DeskViewPose.ReferenceHeight;
        bool ok = chrome + BandGap <= band && sameReference;
        return GateResult.Of("cam.chrome.band", Owner, ok, $"top chrome ends at {chrome:0} px, the reading view's band at {band:0} px (reference {reference.x:0}x{reference.y:0})",
                             ok ? "every framed desk point lies under the overlay's top chrome" :
                                  sameReference ? $"the band must reach {chrome + BandGap:0} px (DeskViewTuning.topBand)" : "DeskViewPose's reference resolution is not the overlay's");
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

    /// <summary>
    /// Every camera move of every CAM run is tagged with its cause, and every
    /// impulse stays under its cap (G9; the QA camera logger): each run's own
    /// camera line and each cameraCheck's, in cam_views, cam_chrome, cam_desk
    /// and cam_gc (the editor runs; the [BP] run's is cam.bp.camera).
    /// </summary>
    [GateCheck("cam.play.tags", Owner, GateKind.Play)]
    public static GateResult Tags() => GateResults.Require("cam.play.tags", Owner, "cam_*.camera*");

    /// <summary>The scripted day 12 (drops, scans, chip reads, the intercom): the drop tilt at most once per paper landing, never while holding (v2's CAM pass at #6).</summary>
    [GateCheck("cam.play.tilts", Owner, GateKind.Play)]
    public static GateResult Tilts() => GateResults.Require("cam.play.tilts", Owner, "cam_views.tilts.*");

    /// <summary>
    /// The scripted day 12 ran whole (camplay, v2's CAM pass at #6): every
    /// counter paper dropped, the Auto-Feed tray clicked and its stack scanned,
    /// n of n scans and f of f chip reads finished, the intercom's wheel opened
    /// and a ring question answered. Without it the tilt count could pass on a
    /// day that ran only partly (the Rolodex's right-click is the one gap:
    /// QaInput has no right button).
    /// </summary>
    [GateCheck("cam.play.day12", Owner, GateKind.Play)]
    public static GateResult Day12() => GateResults.Require("cam.play.day12", Owner, "cam_views.play.*");

    /// <summary>Both poses frame what they must at A1-A4, home by home (the reading view: the mat and the deal tray whole, the stamp rack and the intercom 100%, at least 8 x 8 cm of the other in-use side homes; the office view: every shown home ≥98%; the reading view's bottom row on the desk).</summary>
    [GateCheck("cam.play.framing", Owner, GateKind.Play)]
    public static GateResult Framing() => GateResults.Require("cam.play.framing", Owner, "cam_views.frame.*");

    /// <summary>The controls of a view appear only over the last third of the move into it, and at once when motion is still (BUG-32b, 64b).</summary>
    [GateCheck("cam.play.fade", Owner, GateKind.Play)]
    public static GateResult ControlFade() => GateResults.Require("cam.play.fade", Owner, "cam_views.fade.*");

    /// <summary>No view changes while something is held, each view change refused on its own (BUG-55c).</summary>
    [GateCheck("cam.play.hold", Owner, GateKind.Play)]
    public static GateResult Hold() => GateResults.Require("cam.play.hold", Owner, "cam_views.hold.*");

    /// <summary>The city fills the screen, its parallax moves at most 1 px at 1080p a 30 fps frame, a click and Q return (BUG-42, BUG-69, v2's city exits).</summary>
    [GateCheck("cam.play.city", Owner, GateKind.Play)]
    public static GateResult City() => GateResults.Require("cam.play.city", Owner, "cam_views.city.*");

    /// <summary>Each shake moment draws its own row of the shake table and nothing else shakes; no shake when motion is still; the papers move no more than the cap (GAME_FEEL_BIBLE §1.3).</summary>
    [GateCheck("cam.play.shakes", Owner, GateKind.Play)]
    public static GateResult Shakes() => GateResults.Require("cam.play.shakes", Owner, "cam_views.shake.*");

    /// <summary>
    /// No chrome of CAM's (the pull tabs, the Back control, "◀ City") is drawn
    /// over a held thing or over the reading view's framed work, at A1-A4
    /// (BUG-59), with both halves seen in each leg (a held thing, a dragged
    /// decoration placed by its Root; the reading view at rest), every one of
    /// CAM's controls found, and every overlay plate a leg shows was drawn and
    /// measured (a plate never drawn is a red "not measured" line here). The
    /// plates' own verdicts are their owners' checks below, so this one goes
    /// green on CAM's work while they are red.
    /// </summary>
    [GateCheck("cam.play.chrome", Owner, GateKind.Play)]
    public static GateResult Chrome() => GateResults.Require("cam.play.chrome", Owner, "cam_chrome.chrome.*");

    /// <summary>
    /// CONTENT's guide prompt plate (day 1) covers neither a held thing nor the
    /// reading view's framed work (BUG-59; measured by CAM's camchrome: CAM.md's
    /// hand-off, the plate over the intercom's top). Red until CONTENT moves it
    /// (#17): INTEGRATION carries it on the known-red list until then.
    /// </summary>
    [GateCheck("cam.play.guidePlate", "CONTENT", GateKind.Play)]
    public static GateResult GuidePlate() => GateResults.Require("cam.play.guidePlate", "CONTENT", "cam_chrome.plate.*.guide");

    /// <summary>
    /// STAMP's stamp hint plate covers neither a held thing nor the reading
    /// view's framed work (BUG-59; measured by CAM's camchrome in each day-12
    /// leg: CAM.md's hand-off, the plate over the rack). Red until STAMP moves
    /// it (#7): INTEGRATION carries it on the known-red list until then.
    /// </summary>
    [GateCheck("cam.play.stampHint", "STAMP", GateKind.Play)]
    public static GateResult StampHintPlate() => GateResults.Require("cam.play.stampHint", "STAMP", "cam_chrome.plate.*.stamphint");

    /// <summary>The cap heights of the desk's printed text were measured at CAM's reading pose, A1 and A2 on day 12, once the scripted day has laid its papers on the desk (the desk plan's §6 smoke; a report: the pass on the heights is DOC-LOOK's at #9; red only when nothing could be measured).</summary>
    [GateCheck("cam.play.capreport", Owner, GateKind.Play)]
    public static GateResult CapReport() => GateResults.Require("cam.play.capreport", Owner, "cam_views.capreport.*");

    /// <summary>
    /// DESK-PLAN's homes at A3 and A4 (FIX_PLAN v1: "DESK-PLAN at A3 and A4
    /// (deferred to here)"): every kept object ≥90% seen (DESK-PLAN's
    /// visible lines) and its centre click reaching it (QA-RIG's presses), the
    /// intercom opening the wheel from the reading view with the view kept
    /// (DESK-PLAN's expect lines), and the run reaching its last probe (its
    /// scripts and materials, QA-RIG's), so a run that stops early fails. Only
    /// these: the run's other lines are their owners' (its camera lines
    /// cam.play.tags; its LiberationSans probe TEXT-LANG's, known red until
    /// #12), as qa.play.smoke and RUN-SAFE's checks (fa5a045) read theirs.
    /// </summary>
    [GateCheck("cam.play.deskplan", Owner, GateKind.Play)]
    public static GateResult DeskPlanWide() =>
        All("cam.play.deskplan", "cam_desk.visible.*", "cam_desk.expect.*", "cam_desk.presses", "cam_desk.missingScripts.end", "cam_desk.materials.end");

    /// <summary>
    /// [BP] No per-frame allocation from DeskView, CameraFeel or CityView: the
    /// reading toggle and the city look cycled under cam_gc's gc window on the
    /// QA player, 0 B a frame (BUG-65e, 73e; FIX_PLAN's "0 B/frame from ...
    /// DeskView [BP]"; recipe 10, run_player.py). The editor run's own gc line
    /// counts the editor's allocations and is no verdict. With the demo gate,
    /// as QA-RIG's qa.bp.* ([BP] gates run on the scratch build, not at each
    /// merge).
    /// </summary>
    [GateCheck("cam.bp.gc", Owner, GateKind.BuiltPlayer, DemoOnly = true)]
    public static GateResult BuiltPlayerGc() => GateResults.Require("cam.bp.gc", Owner, "bp.cam_gc.gc.*", true);

    /// <summary>[BP] The cam_gc run on the QA player tagged every camera move (its run's camera line; the editor runs' are cam.play.tags).</summary>
    [GateCheck("cam.bp.camera", Owner, GateKind.BuiltPlayer, DemoOnly = true)]
    public static GateResult BuiltPlayerCamera() => GateResults.Require("cam.bp.camera", Owner, "bp.cam_*.camera*", true);

    /// <summary>One play check over several result patterns: each must be measured, green and current (GateResults.Require for each).</summary>
    private static GateResult All(string id, params string[] patterns)
    {
        List<GateResult> parts = patterns.Select(p => GateResults.Require(id, Owner, p)).ToList();
        return GateResult.Of(id, Owner, parts.All(r => r.Pass), string.Join("; ", patterns.Zip(parts, (p, r) => $"{p}: {r.Measured}")),
                             string.Join("; ", parts.Where(r => !r.Pass).Select(r => r.Detail)));
    }
}
