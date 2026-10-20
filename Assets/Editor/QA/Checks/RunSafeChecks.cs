using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

/// <summary>
/// RUN-SAFE's gate checks (FIX_PLAN §3 RUN-SAFE; BUG-27a, BUG-25b, BUG-68a,
/// BUG-28a, BUG-35a). Static: the three input consumers check the InputGate
/// and the cheat panel holds it; the cheat menu's Dusk is the day cycle's;
/// every cheat switch has one scope, and the one kept elsewhere (the
/// language lock) is on the badge. Play: the day_jumps scenario (each
/// cheat path lands at the day's briefing, console clean) and the overlay
/// block (0 game input behind the open panel). The play checks read RUN-SAFE's
/// own results of each run (console, presses, quality, the probes' scripts and
/// materials, one probe per landing); the probes' LiberationSans and the run's
/// camera results are TEXT-LANG's and CAM's, judged by qa.play.liberationSans
/// and qa.play.camera.
/// </summary>
public static class RunSafeChecks
{
    /// <summary>The files whose input handlers must stand still while an overlay holds the gate (the P0 carve's insertions, BUG-68b).</summary>
    private static readonly string[] Consumers =
    {
        "Assets/Scripts/Office/OfficeControls.cs",
        "Assets/Scripts/Office/Desk/DeskInspect.cs",
        "Assets/Scripts/UI/DesktopWindowManager.cs",
    };

    /// <summary>OfficeControls, DeskInspect and DesktopWindowManager check InputGate.Blocked, and the open cheat panel holds the gate (BUG-68).</summary>
    [GateCheck("runsafe.input.gate", "RUN-SAFE")]
    public static GateResult InputGateWired()
    {
        string root = GateResults.ProjectRoot;
        var missing = Consumers.Where(f => !File.Exists(Path.Combine(root, f)) || !File.ReadAllText(Path.Combine(root, f)).Contains("InputGate.Blocked")).ToList();
        string panel = Path.Combine(root, "Assets/Scripts/DevTools/DebugPanelController.cs");
        if (!File.Exists(panel) || !File.ReadAllText(panel).Contains("InputGate.Hold("))
            missing.Add("DebugPanelController (InputGate.Hold)");
        return GateResult.Of("runsafe.input.gate", "RUN-SAFE", missing.Count == 0, $"{Consumers.Length - missing.Count(m => m.EndsWith(".cs"))}/{Consumers.Length} consumers gated",
            missing.Count == 0 ? string.Empty : "Not gated: " + string.Join(", ", missing));
    }

    /// <summary>The cheat menu's Dusk is the one day cycle's dusk (BUG-25b).</summary>
    [GateCheck("runsafe.cheat.dusk", "RUN-SAFE")]
    public static GateResult CheatDusk()
    {
        float? dusk = DevCheats.HourOf(System.Array.IndexOf(DevCheats.TimeNames, "Dusk"));
        bool pass = dusk.HasValue && dusk.Value == HallDayCycle.DuskHour;
        return GateResult.Of("runsafe.cheat.dusk", "RUN-SAFE", pass, $"cheat {dusk}, cycle {HallDayCycle.DuskHour}", pass ? string.Empty : "DevCheats.HourOf must read HallDayCycle.DuskHour.");
    }

    /// <summary>Every cheat switch is day-scoped (cleared each morning), run-scoped or next-generation (shown on the badge) (BUG-27a).</summary>
    [GateCheck("runsafe.switch.scopes", "RUN-SAFE")]
    public static GateResult SwitchScopes()
    {
        var switches = typeof(DevToolsState).GetFields(BindingFlags.Public | BindingFlags.Static).Where(f => !f.IsInitOnly && !f.IsLiteral).Select(f => f.Name).ToList();
        var scoped = DevToolsState.DayScoped.Concat(DevToolsState.RunScoped).Concat(DevToolsState.NextGeneration).ToList();
        var unscoped = switches.Except(scoped).ToList();
        var twice = scoped.GroupBy(s => s).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        bool pass = unscoped.Count == 0 && twice.Count == 0;
        return GateResult.Of("runsafe.switch.scopes", "RUN-SAFE", pass, $"{switches.Count} switches, {unscoped.Count} unscoped, {twice.Count} in two scopes",
            pass ? string.Empty : $"Unscoped: {string.Join(", ", unscoped)}; in two scopes: {string.Join(", ", twice)}");
    }

    /// <summary>The switch kept outside DevToolsState (the language lock, CultureThemeService.LockOverride) is on the badge while set and cleared by a new run (BUG-27a: "any sticky switch is shown loudly"); the field reflection above cannot see it.</summary>
    [GateCheck("runsafe.switch.elsewhere", "RUN-SAFE")]
    public static GateResult SwitchElsewhere()
    {
        bool? was = CultureThemeService.LockOverride;
        try
        {
            CultureThemeService.LockOverride = true;
            bool shown = DevToolsState.StickyLine(false).Contains("Language locked");
            DevToolsState.ResetAll();
            bool cleared = CultureThemeService.LockOverride == null;
            bool listed = System.Array.IndexOf(DevToolsState.Elsewhere, "CultureThemeService.LockOverride") >= 0;
            bool pass = shown && cleared && listed;
            return GateResult.Of("runsafe.switch.elsewhere", "RUN-SAFE", pass, $"badge {shown}, cleared by ResetAll {cleared}, listed {listed}",
                pass ? string.Empty : "The language lock must be named on the badge (StickyLine), cleared by ResetAll and listed in DevToolsState.Elsewhere.");
        }
        finally
        {
            CultureThemeService.LockOverride = was;
        }
    }

    /// <summary>The landings of the day_jumps scenario, in order: each one's probe runs at that day's briefing (after its waitOffice step), so a probe's result proves the landing was reached.</summary>
    private static readonly string[] DayJumpLandings = { "d1_restart", "d5_jump", "d7_jump", "d7_restart", "d8_skip", "d12_jump", "d12_restart", "d15_jump" };

    /// <summary>The day_jumps scenario passed at HEAD: Restart d1, Jump d5, d7, Restart, Skip to d8, Jump d12, Restart, Jump d15, each at the day's briefing (every landing's probe present and green) with a clean console (QaJobs.DayJumps).</summary>
    [GateCheck("runsafe.play.dayJumps", "RUN-SAFE", GateKind.Play)]
    public static GateResult DayJumps() =>
        All("runsafe.play.dayJumps", RunPatterns("day_jumps").Concat(DayJumpLandings.Select(l => "day_jumps.missingScripts." + l)));

    /// <summary>The overlay block passed at HEAD: the cheat panel open, clicks and keys over the desk and the PC, 0 game actions, the run reaching its last probe (the overlay_block scenario, docs/pending/RUN-SAFE.md).</summary>
    [GateCheck("runsafe.play.overlayBlock", "RUN-SAFE", GateKind.Play)]
    public static GateResult OverlayBlock() =>
        All("runsafe.play.overlayBlock", RunPatterns("overlay_block").Append("overlay_block.missingScripts.after"));

    /// <summary>A run's own results that RUN-SAFE judges: its console, its presses, its quality level, and every probe's missing scripts and materials.</summary>
    private static IEnumerable<string> RunPatterns(string scenario) =>
        new[] { "console", "presses", "quality", "missingScripts.*", "materials.*" }.Select(r => scenario + "." + r);

    /// <summary>One check over several result patterns: it passes when every pattern has results and all of them pass at HEAD (GateResults.Require for each).</summary>
    private static GateResult All(string id, IEnumerable<string> patterns)
    {
        List<string> list = patterns.ToList();
        List<GateResult> parts = list.Select(p => GateResults.Require(id, "RUN-SAFE", p)).ToList();
        return GateResult.Of(id, "RUN-SAFE", parts.All(r => r.Pass), string.Join("; ", list.Zip(parts, (p, r) => $"{p}: {r.Measured}")),
            string.Join("; ", parts.Where(r => !r.Pass).Select(r => r.Detail)));
    }
}
