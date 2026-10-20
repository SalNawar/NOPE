using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// QA-RIG's gate checks (P0-B; BUG-53, BUG-30, BUG-65a, BUG-73a; FIX_PLAN_v2
/// rules 9 and 13). Static: the player's quality level, previewHourOn off, the content
/// validator, scene integrity (missing scripts, broken materials, retired
/// objects) and LiberationSans in the shipped scenes, SO fields with a
/// reader, Phase B approvals and whole bundles. Play: the rig's self-test,
/// the smoke day and the GC baseline. Built player: the smoke day's
/// sequences (console, camera, flicker, flash, latency, GC). Other tracks add
/// theirs in their own Checks/&lt;Track&gt;Checks.cs.
/// </summary>
public static class QaRigChecks
{
    /// <summary>The SO-reader allowlist: known dead fields waiting for their owners' cleanup (BUG-73's list).</summary>
    public static string AllowlistPath => Path.Combine(GateResults.ProjectRoot, "tools", "qa", "so_reader_allowlist.txt");

    /// <summary>The Phase B approvals kept in the repository (INTEGRATION copies each approval line here at merge).</summary>
    public static string ApprovalsPath => Path.Combine(GateResults.ProjectRoot, "docs", "phaseb", "APPROVALS.md");

    /// <summary>A local pointer to the run's R7 folder (one line), whose PHASEB/APPROVALS.md is read when the repository has none.</summary>
    public static string R7PointerPath => Path.Combine(GateResults.ProjectRoot, "Library", "QA", "r7.txt");

    /// <summary>
    /// QA can play at the Standalone player's quality level (BUG-53): the
    /// settings name it. Every run then plays at it (QaPlayHarness sets it)
    /// and asserts so (&lt;scenario&gt;.quality, which qa.play.smoke reads); the
    /// editor's own level outside a run is not gated.
    /// </summary>
    [GateCheck("qa.quality.parity", "QA-RIG")]
    public static GateResult QualityParity()
    {
        QaQuality.Levels file = QaQuality.Read(GateResults.ProjectRoot);
        return GateResult.Of("qa.quality.parity", "QA-RIG", file.HasStandaloneLevel, QaQuality.Describe(file),
            file.HasStandaloneLevel
                ? $"QA runs play at {file.Name(file.StandaloneDefault)}; smoke_day1.quality (qa.play.smoke) measures it"
                : "QualitySettings.asset names no existing Standalone default level (m_PerPlatformDefaultQuality: Standalone), so QA cannot play at the player's level.");
    }

    /// <summary>No hall lighting asset ships with its preview hour on (it would freeze the hall's time).</summary>
    [GateCheck("qa.hall.previewHourOff", "QA-RIG")]
    public static GateResult PreviewHourOff()
    {
        var on = AssetDatabase.FindAssets("t:HallLightingSO").Select(AssetDatabase.GUIDToAssetPath)
                              .Where(p => AssetDatabase.LoadAssetAtPath<HallLightingSO>(p) is HallLightingSO s && s.previewHourOn).ToList();
        return GateResult.Of("qa.hall.previewHourOff", "QA-RIG", on.Count == 0, $"{on.Count} asset(s) with previewHourOn", on.Count == 0 ? string.Empty : string.Join(", ", on));
    }

    /// <summary>The content validator logs no warning or error.</summary>
    [GateCheck("qa.content.validator", "CONTENT")]
    public static GateResult ContentValidator()
    {
        var problems = new List<string>();
        void OnLog(string message, string stack, LogType type)
        {
            if (type != LogType.Log)
                problems.Add($"[{type}] {message.Split('\n')[0]}");
        }
        Application.logMessageReceived += OnLog;
        try
        {
            ContentLibraryValidator.Validate();
        }
        finally
        {
            Application.logMessageReceived -= OnLog;
        }
        return GateResult.Of("qa.content.validator", "CONTENT", problems.Count == 0, $"{problems.Count} warning(s)/error(s)", string.Join("; ", problems.Take(15)));
    }

    /// <summary>The shipped scenes have no missing script, no broken material and no retired object (BUG-30).</summary>
    [GateCheck("qa.scenes.integrity", "QA-RIG")]
    public static IEnumerable<GateResult> SceneIntegrity()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            yield return GateResult.Fail("qa.scenes.integrity", "QA-RIG", "not run", "Scenes are scanned in edit mode only.");
            yield break;
        }
        QaSceneScan.Findings f = QaSceneScan.ScanScenes(QaSceneScan.ShippedScenes());
        yield return Count("qa.scenes.missingScripts", "QA-RIG", f.MissingScripts, "GameObject(s) with a missing script");
        yield return Count("qa.scenes.materials", "QA-RIG", f.BrokenMaterials, "renderer(s) with a null or error material");
        yield return Count("qa.scenes.retired", "QA-RIG", f.RetiredPresent, "retired object(s) still in a scene (rebuild with Build Office UI)");
        yield return Count("qa.scenes.liberationSans", "TEXT-LANG", f.LiberationSans, "text(s) set in LiberationSans");
    }

    private static GateResult Count(string id, string owner, List<string> found, string what) =>
        GateResult.Of(id, owner, found.Count == 0, $"{found.Count} {what}", string.Join("; ", found.Take(15)) + (found.Count > 15 ? $"; ... {found.Count - 15} more" : string.Empty));

    /// <summary>Every serialized ScriptableObject field has a reader, except the known dead ones on the allowlist (BUG-73a).</summary>
    [GateCheck("qa.so.readers", "QA-RIG")]
    public static GateResult SoReaders()
    {
        SoFieldReaders.Scan scan = SoFieldReaders.Run(SoFieldReaders.Sources(Application.dataPath));
        HashSet<string> allow = SoFieldReaders.ParseAllowlist(File.Exists(AllowlistPath) ? File.ReadAllText(AllowlistPath) : string.Empty);
        var fresh = scan.Unread.Where(f => !allow.Contains(f.Key)).ToList();
        return GateResult.Of("qa.so.readers", "QA-RIG", fresh.Count == 0,
            $"{scan.Fields.Count} fields in {scan.Classes.Count} SO classes; {scan.Unread.Count} unread, {fresh.Count} not on the allowlist",
            fresh.Count == 0 ? string.Empty : "Dead knobs (delete them or read them): " + string.Join(", ", fresh.Select(f => f.ToString())));
    }

    /// <summary>The dead-knob allowlist is empty by the demo (each owner deleted its fields, BUG-73).</summary>
    [GateCheck("qa.so.cleanup", "INTEGRATION", DemoOnly = true)]
    public static GateResult SoCleanup()
    {
        SoFieldReaders.Scan scan = SoFieldReaders.Run(SoFieldReaders.Sources(Application.dataPath));
        HashSet<string> allow = SoFieldReaders.ParseAllowlist(File.Exists(AllowlistPath) ? File.ReadAllText(AllowlistPath) : string.Empty);
        var still = scan.Unread.Where(f => allow.Contains(f.Key)).ToList();
        return GateResult.Of("qa.so.cleanup", "INTEGRATION", still.Count == 0, $"{still.Count} allowlisted dead field(s) remain",
            string.Join(", ", still.Select(f => f.Key)));
    }

    /// <summary>Every merged Phase B track has Saleh's spec approval on record (FIX_PLAN_v2 rule 9).</summary>
    [GateCheck("qa.phaseb.approvals", "INTEGRATION")]
    public static GateResult PhaseBApprovals()
    {
        List<string> merged = PhaseBGate.Merged(GateChecks.All().Select(c => c.Info.Owner));
        if (merged.Count == 0)
            return GateResult.Ok("qa.phaseb.approvals", "INTEGRATION", "no Phase B track merged");
        string file = File.Exists(ApprovalsPath) ? ApprovalsPath : R7Approvals();
        HashSet<string> approved = PhaseBGate.Approved(file != null && File.Exists(file) ? File.ReadAllText(file) : string.Empty);
        List<string> missing = PhaseBGate.Unapproved(merged, approved);
        return GateResult.Of("qa.phaseb.approvals", "INTEGRATION", missing.Count == 0, $"{merged.Count} merged, {missing.Count} without approval",
            missing.Count == 0 ? $"approvals: {file}" : $"No approval line in {file ?? ApprovalsPath} for: {string.Join(", ", missing)}");
    }

    private static string R7Approvals()
    {
        if (!File.Exists(R7PointerPath))
            return null;
        string r7 = File.ReadAllText(R7PointerPath).Trim();
        return r7.Length == 0 ? null : Path.Combine(r7, "PHASEB", "APPROVALS.md");
    }

    /// <summary>A bundle ships whole or not at all (FIX_PLAN_v2 rule 13).</summary>
    [GateCheck("qa.phaseb.bundles", "INTEGRATION", DemoOnly = true)]
    public static GateResult PhaseBBundles()
    {
        List<string> partial = PhaseBGate.PartialBundles(PhaseBGate.Merged(GateChecks.All().Select(c => c.Info.Owner)));
        return GateResult.Of("qa.phaseb.bundles", "INTEGRATION", partial.Count == 0, $"{partial.Count} partial bundle(s)", string.Join("; ", partial));
    }

    /// <summary>The rig's self-test passed at HEAD (QaJobs.SelfTest): every P-* path's press and release delivered within 1 px.</summary>
    [GateCheck("qa.play.selftest", "QA-RIG", GateKind.Play)]
    public static GateResult SelfTest() => GateResults.Require("qa.play.selftest", "QA-RIG", "selftest.selftest.*");

    /// <summary>The smoke day passed at HEAD (QaJobs.Smoke): a clean console, every press reached its target under the pointer, the player's quality level, and no missing script or broken material at any probe.</summary>
    [GateCheck("qa.play.smoke", "QA-RIG", GateKind.Play)]
    public static GateResult Smoke() =>
        All("qa.play.smoke", "QA-RIG", false, "smoke_day1.console", "smoke_day1.presses", "smoke_day1.quality", "smoke_day1.missingScripts.*", "smoke_day1.materials.*");

    /// <summary>In the smoke day every camera move had a tagged cause (a view key, the D1 tilt, the D6 focus, an impulse under its cap).</summary>
    [GateCheck("qa.play.camera", "CAM", GateKind.Play)]
    public static GateResult Camera() => GateResults.Require("qa.play.camera", "CAM", "smoke_day1.camera*");

    /// <summary>In the smoke day no visible text drew a LiberationSans glyph (BUG-3/37).</summary>
    [GateCheck("qa.play.liberationSans", "TEXT-LANG", GateKind.Play)]
    public static GateResult LiberationSans() => GateResults.Require("qa.play.liberationSans", "TEXT-LANG", "smoke_day1.liberationSans.*");

    /// <summary>No pixel of the smoke day's sequences toggles more than twice in 10 s outside the expected motion (the editor's run; the [BP] one is qa.bp.flicker).</summary>
    [GateCheck("qa.play.flicker", "HALL-FX", GateKind.Play)]
    public static GateResult Flicker() => GateResults.Require("qa.play.flicker", "HALL-FX", "smoke_day1.seq.*.flicker");

    /// <summary>At most 3 flashes a second anywhere in the smoke day's sequences, no red full-field flash (GAME_FEEL_BIBLE G3, gap 2).</summary>
    [GateCheck("qa.play.flash", "UI-PC", GateKind.Play, DemoOnly = true)]
    public static GateResult Flash() => GateResults.Require("qa.play.flash", "UI-PC", "smoke_day1.seq.*.flash");

    /// <summary>Every press in the smoke day's sequences shows its first visible change within one frame (GAME_FEEL_BIBLE check 2, gap 6).</summary>
    [GateCheck("qa.play.latency", "QA-RIG", GateKind.Play, DemoOnly = true)]
    public static GateResult Latency() => GateResults.Require("qa.play.latency", "QA-RIG", "smoke_day1.seq.*.latency");

    /// <summary>The per-call-site GC baseline was recorded at HEAD (QaJobs.GcSites; BUG-65a: it names each site's owner).</summary>
    [GateCheck("qa.perf.gcsites", "QA-RIG", GateKind.Play)]
    public static GateResult GcSites() => GateResults.Require("qa.perf.gcsites", "QA-RIG", "perf.gcsites");

    /// <summary>The built player's smoke day passed at HEAD (tools/qa/run_player.py): a clean console, every press reached its target under the pointer, no missing script or broken material.</summary>
    [GateCheck("qa.bp.smoke", "QA-RIG", GateKind.BuiltPlayer, DemoOnly = true)]
    public static GateResult BuiltPlayerSmoke() =>
        All("qa.bp.smoke", "QA-RIG", true, "bp.bp_smoke.console", "bp.bp_smoke.presses", "bp.bp_smoke.missingScripts.*", "bp.bp_smoke.materials.*");

    /// <summary>[BP] No pixel of the smoke day's sequences toggles more than twice in 10 s outside the expected motion (FIX_PLAN flicker pass).</summary>
    [GateCheck("qa.bp.flicker", "HALL-FX", GateKind.BuiltPlayer, DemoOnly = true)]
    public static GateResult BuiltPlayerFlicker() => GateResults.Require("qa.bp.flicker", "HALL-FX", "bp.bp_smoke.seq.*.flicker", true);

    /// <summary>[BP] At most 3 flashes a second anywhere, no red full-field flash (GAME_FEEL_BIBLE G3; FlashPolicy is UI-PC's).</summary>
    [GateCheck("qa.bp.flash", "UI-PC", GateKind.BuiltPlayer, DemoOnly = true)]
    public static GateResult BuiltPlayerFlash() => GateResults.Require("qa.bp.flash", "UI-PC", "bp.bp_smoke.seq.*.flash", true);

    /// <summary>[BP] Every press shows its first visible change within one frame (GAME_FEEL_BIBLE check 2, gap 6).</summary>
    [GateCheck("qa.bp.latency", "QA-RIG", GateKind.BuiltPlayer, DemoOnly = true)]
    public static GateResult BuiltPlayerLatency() => GateResults.Require("qa.bp.latency", "QA-RIG", "bp.bp_smoke.seq.*.latency", true);

    /// <summary>[BP] Zero steady-state GC allocation per frame, and no frame over one frame's time at call-next (BUG-65; the owners are in gc_sites.tsv).</summary>
    [GateCheck("qa.bp.perf", "QA-RIG", GateKind.BuiltPlayer, DemoOnly = true)]
    public static GateResult BuiltPlayerPerf() => All("qa.bp.perf", "QA-RIG", true, "bp.bp_smoke.gc.*", "bp.bp_smoke.seq.callnext.hitches");

    /// <summary>One verdict over several result patterns: each must be measured, green and current (GateResults.Require).</summary>
    private static GateResult All(string id, string owner, bool builtPlayer, params string[] patterns)
    {
        List<GateResult> parts = patterns.Select(p => GateResults.Require(id, owner, p, builtPlayer)).ToList();
        bool pass = parts.All(r => r.Pass);
        return GateResult.Of(id, owner, pass, string.Join("; ", patterns.Zip(parts, (p, r) => $"{p}: {r.Measured}")),
            string.Join("; ", parts.Where(r => !r.Pass).Select(r => r.Detail)));
    }
}
