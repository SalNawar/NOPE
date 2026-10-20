using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

/// <summary>
/// FLOW's gate checks (docs/pending/FLOW.md). Static: the player does not run
/// in the background (BUG-34a: the focus lost pauses), MotionTuning_Default
/// holds the live knobs only (BUG-73c), and the desk clock has no tooltip
/// copying its readout (BUG-70b). Play (FLOW's recipes,
/// tools/qa/scenarios/flow_*.json, FlowOps): Esc reaches Settings with the
/// clock held, in the office, Home and Title, and Esc in the PC keeps it open
/// (BUG-34b, R3); the focus lost holds
/// the clock and the audio (BUG-34a); the last traveller's payoff before the
/// report and no single-frame cut at the day's end, Go Home and Sleep
/// (BUG-32a); an ending is the evening edition with Title and Quit (BUG-56a);
/// the night shift closes at 24:00 and an early finish rings the bell (BUG-72).
/// Built player: the loads hold no frame over 0.25 s (BUG-31a).
/// </summary>
public static class FlowChecks
{
    private const string Owner = "FLOW";

    /// <summary>The player does not run in the background, so the window's focus lost pauses it (PauseMenu; the QA rig and the editor job runner turn it on for their runs).</summary>
    [GateCheck("flow.runInBackground", Owner)]
    public static GateResult RunInBackground()
    {
        string path = Path.Combine(GateResults.ProjectRoot, "ProjectSettings", "ProjectSettings.asset");
        Match m = Regex.Match(File.Exists(path) ? File.ReadAllText(path) : string.Empty, @"^\s*runInBackground:\s*(\d)", RegexOptions.Multiline);
        bool off = m.Success && m.Groups[1].Value == "0";
        return GateResult.Of("flow.runInBackground", Owner, off, m.Success ? $"runInBackground: {m.Groups[1].Value}" : "runInBackground not found",
            "BUG-34a: ProjectSettings.asset runInBackground must be 0 (the player pauses when it loses the focus)");
    }

    /// <summary>MotionTuning_Default names no knob MotionKnobs no longer has (BUG-73c: "re-saved with the live knobs only").</summary>
    [GateCheck("flow.motionTuning.live", Owner)]
    public static GateResult MotionTuningLive()
    {
        string path = Path.Combine(GateResults.ProjectRoot, "Assets", "Data", "Config", "MotionTuning_Default.asset");
        if (!File.Exists(path))
            return GateResult.Of("flow.motionTuning.live", Owner, false, "MotionTuning_Default.asset is missing", path);
        var live = new HashSet<string>(typeof(MotionKnobs).GetFields(BindingFlags.Public | BindingFlags.Instance).Select(f => f.Name));
        List<string> stale = Regex.Matches(File.ReadAllText(path), @"^    (\w+):", RegexOptions.Multiline).Cast<Match>()
                                  .Select(x => x.Groups[1].Value).Where(name => !live.Contains(name)).ToList();
        return GateResult.Of("flow.motionTuning.live", Owner, stale.Count == 0, $"{stale.Count} stale knob(s)", string.Join(", ", stale));
    }

    /// <summary>
    /// The desk clock shows no tooltip that copies its readout (BUG-70b: the
    /// copy is taken on the click and goes stale as the minute moves on;
    /// BUGLIST's fix, "drop the clock tooltip (it duplicates the readout)"):
    /// Reaction_Clock's tooltipKey is blank, or the clock has no reaction asset.
    /// </summary>
    [GateCheck("flow.clock.tooltip", Owner)]
    public static GateResult ClockTooltip()
    {
        string path = Path.Combine(GateResults.ProjectRoot, "Assets", "Data", "Config", "DeskReactions", "Reaction_Clock.asset");
        if (!File.Exists(path))
            return GateResult.Of("flow.clock.tooltip", Owner, true, "no Reaction_Clock.asset: the clock has no tooltip", path);
        Match m = Regex.Match(File.ReadAllText(path), @"^\s*tooltipKey:[ \t]*(.*?)\s*$", RegexOptions.Multiline);
        string key = m.Success ? m.Groups[1].Value.Trim('\'', '"') : string.Empty;
        return GateResult.Of("flow.clock.tooltip", Owner, key.Length == 0, key.Length == 0 ? "the clock has no tooltip" : $"tooltipKey: {key}",
            "BUG-70b: Reaction_Clock.asset's tooltipKey must be blank (DESK-PLAN: the asset and OfficeSceneUIBuilder.Desk.cs's EnsureDeskReaction argument)");
    }

    /// <summary>Esc in the office opens the PC's Settings, and the clock holds while it shows.</summary>
    [GateCheck("flow.esc.office", Owner, GateKind.Play)]
    public static GateResult EscOffice() => GateResults.Require("flow.esc.office", Owner, "flow_pause.settings.office");

    /// <summary>Esc with the PC open (Settings in front) keeps the PC open and Settings showing: only Q or the bezel leave the PC (R3).</summary>
    [GateCheck("flow.esc.pc", Owner, GateKind.Play)]
    public static GateResult EscPc() => GateResults.Require("flow.esc.pc", Owner, "flow_pause.settings.pcEsc");

    /// <summary>Esc at Home opens Settings (red until Home has a Settings window: HOME, then HOME-3D's tablet).</summary>
    [GateCheck("flow.esc.home", Owner, GateKind.Play)]
    public static GateResult EscHome() => GateResults.Require("flow.esc.home", Owner, "flow_pause.settings.home");

    /// <summary>Esc on the title opens Settings (red until the title has a Settings window: HOME).</summary>
    [GateCheck("flow.esc.title", Owner, GateKind.Play)]
    public static GateResult EscTitle() => GateResults.Require("flow.esc.title", Owner, "flow_pause.settings.title");

    /// <summary>The focus lost holds the clock and pauses the audio; its return lets both go.</summary>
    [GateCheck("flow.focus", Owner, GateKind.Play)]
    public static GateResult Focus() => GateResults.Require("flow.focus", Owner, "flow_pause.focus");

    /// <summary>The last traveller's reaction and walk-off play before the report.</summary>
    [GateCheck("flow.payoff", Owner, GateKind.Play)]
    public static GateResult Payoff() => GateResults.Require("flow.payoff", Owner, "flow_transitions.payoff");

    /// <summary>No single-frame full-screen change at the day's end, Go Home and Sleep (seqtools cuts on flow_transitions' sequences).</summary>
    [GateCheck("flow.transitions.cuts", Owner, GateKind.Play)]
    public static GateResult TransitionCuts() => GateResults.Require("flow.transitions.cuts", Owner, "flow_transitions.seq.*.cuts");

    /// <summary>A scene change through SceneTransit brings the booth's shutter down, loads behind it and lifts it (flow_shutter; its frozen and cuts analyses by seqtools).</summary>
    [GateCheck("flow.transit.shutter", Owner, GateKind.Play)]
    public static GateResult SceneChangeShutter() => GateResults.Require("flow.transit.shutter", Owner, "flow_shutter.transit.*");

    /// <summary>An ending is the evening edition, with Title and Quit, after the report.</summary>
    [GateCheck("flow.endings", Owner, GateKind.Play)]
    public static GateResult Endings() => GateResults.Require("flow.endings", Owner, "flow_endings.edition");

    /// <summary>The night shift's closing reads 24:00.</summary>
    [GateCheck("flow.night.close", Owner, GateKind.Play)]
    public static GateResult NightClose() => GateResults.Require("flow.night.close", Owner, "flow_night.closed.midnight");

    /// <summary>An early finish (End Shift) raises the clock's Closed once, so the closing bell rings.</summary>
    [GateCheck("flow.night.bell", Owner, GateKind.Play)]
    public static GateResult EarlyBell() => GateResults.Require("flow.night.bell", Owner, "flow_night.bell.early");

    /// <summary>The loads (title to office, Home to office) never hold a frame over 0.25 s, on the QA player.</summary>
    [GateCheck("flow.bp.load", Owner, GateKind.BuiltPlayer, DemoOnly = true)]
    public static GateResult Load() => GateResults.Require("flow.bp.load", Owner, "bp.flow_load.seq.*.frozen", true);
}
