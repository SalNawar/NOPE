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
/// the night shift closes at 24:00 and an early finish rings the bell (BUG-72);
/// a Skip day from the office mid-shift lands as one from the briefing, the
/// day closed once (round 4), and a second one under the shutter as one after
/// it lifted (round 5). Built player: the loads hold no frame over 0.25 s, in real time and in the
/// captures, and the office shows within 1.0 s (BUG-31a). A check that reads
/// several results names each by its exact id (<see cref="All"/>), so a run
/// that stopped part-way, its later results missing, is red (round 6).
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

    /// <summary>No single-frame full-screen change at the day's end, Go Home and Sleep: seqtools cuts on each of flow_transitions' three sequences (FLOW recipe 4 writes their results).</summary>
    [GateCheck("flow.transitions.cuts", Owner, GateKind.Play)]
    public static GateResult TransitionCuts() => All("flow.transitions.cuts", false,
        "flow_transitions.seq.dayend.cuts", "flow_transitions.seq.gohome.cuts", "flow_transitions.seq.sleep.cuts");

    /// <summary>
    /// A scene change through SceneTransit brings the booth's shutter down,
    /// loads behind it and lifts it (office to Title, Title to office), and a
    /// load asked for during a change (while the shutter comes down, while it
    /// rises) loads next, the last asked for winning: flow_shutter's three
    /// results, the queue leg's included. The seqtools frozen and cuts
    /// analyses of its two sequences are not read here: FLOW recipe 5 reads
    /// their output.
    /// </summary>
    [GateCheck("flow.transit.shutter", Owner, GateKind.Play)]
    public static GateResult SceneChangeShutter() => All("flow.transit.shutter", false,
        "flow_shutter.transit.TitleScene", "flow_shutter.transit.office", "flow_shutter.transit.queue.titleOfficeTitle");

    /// <summary>An ending is the evening edition, with Title and Quit, after the report.</summary>
    [GateCheck("flow.endings", Owner, GateKind.Play)]
    public static GateResult Endings() => GateResults.Require("flow.endings", Owner, "flow_endings.edition");

    /// <summary>The night shift's closing reads 24:00.</summary>
    [GateCheck("flow.night.close", Owner, GateKind.Play)]
    public static GateResult NightClose() => GateResults.Require("flow.night.close", Owner, "flow_night.closed.midnight");

    /// <summary>An early finish (End Shift) raises the clock's Closed once, so the closing bell rings.</summary>
    [GateCheck("flow.night.bell", Owner, GateKind.Play)]
    public static GateResult EarlyBell() => GateResults.Require("flow.night.bell", Owner, "flow_night.bell.early");

    /// <summary>
    /// A Skip day from the office mid-shift lands as a skip of the same day
    /// from the briefing (the same day, phase, money, debt paid, statement
    /// rows and save, apart from what presenting a traveller marks), and the
    /// skipped day is closed once: the office the cheat's route left behind
    /// the shutter handles no day's end of its own (flow_skip; FLOW round 4).
    /// A second Skip day under the shutter lands as one after it lifted: the
    /// office being replaced is no longer the live shift, so its ledger is
    /// never closed again on the landing day (FLOW round 5). Both legs'
    /// results are required: a run that stopped in the double skip (a
    /// flowSettled or waitOffice timeout) is red.
    /// </summary>
    [GateCheck("flow.skip.midshift", Owner, GateKind.Play)]
    public static GateResult SkipMidShift() => All("flow.skip.midshift", false,
        "flow_skip.same.midshift", "flow_skip.dayEnds.midshift", "flow_skip.same.doubleskip", "flow_skip.dayEnds.doubleskip");

    /// <summary>
    /// The loads, on the QA player: in real time (flow_load, FlowLoadWatch) no
    /// frame from the click to the shutter off is over 250 ms and the office
    /// shows within 1.0 s; in the captures (flow_load_seq, seqtools frozen) the
    /// shutter's light never stands still over 0.25 s. Both halves are needed:
    /// a fixed-step capture cannot see a long activation frame. Every load's
    /// frames, the timed loads' times (title to office, Home to office; office
    /// to Home's time is reported only) and both captures' frozen.
    /// </summary>
    [GateCheck("flow.bp.load", Owner, GateKind.BuiltPlayer, DemoOnly = true)]
    public static GateResult Load() => All("flow.bp.load", true,
        "bp.flow_load.load.title_office.frames", "bp.flow_load.load.office_home.frames", "bp.flow_load.load.home_office.frames",
        "bp.flow_load.load.title_office.time", "bp.flow_load.load.home_office.time",
        "bp.flow_load_seq.seq.title_office.frozen", "bp.flow_load_seq.seq.home_office.frozen");

    /// <summary>
    /// One check over several results, each named by its exact id: it passes
    /// when every id has a result and all of them passed at HEAD on a clean
    /// tree (and, when <paramref name="builtPlayer"/>, on a built player;
    /// GateResults.Require for each). A pattern such as "flow_skip.same.*"
    /// passes on whichever results exist, so a run that stopped before its
    /// last leg would pass on its first.
    /// </summary>
    private static GateResult All(string id, bool builtPlayer, params string[] ids)
    {
        List<GateResult> parts = ids.Select(r => GateResults.Require(id, Owner, r, builtPlayer)).ToList();
        return GateResult.Of(id, Owner, parts.All(r => r.Pass), string.Join("; ", ids.Zip(parts, (r, g) => $"{r}: {g.Measured}")),
            string.Join("; ", parts.Where(g => !g.Pass).Select(g => g.Detail)));
    }
}
