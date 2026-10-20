using System.Collections.Generic;
using System.Linq;
using UnityEditor;

/// <summary>
/// UI-PC's gate checks (docs/pending/UI-PC.md, the RESULT recipes). Static:
/// the UI kit is compressed with its mips (BUG-65i). Play, on the PC session
/// (tools/qa/scenarios/ui_pc_session.json, its ops in UiPcRigOps): Esc brings
/// Settings forward and the bezel leaves the PC (BUG-50a, R3/R4), the tray
/// clock (BUG-20a, 21a), one order per double-click and Orders' state across
/// minimise and restore (BUG-58a), and the PC's open and close with no
/// single-frame full-screen jump (BUG-32c; seqtools cuts on pc_open and
/// pc_close).
/// </summary>
public static class UiPcChecks
{
    private const string Owner = "UI-PC";

    /// <summary>Every kit texture is block-compressed for desktop (BC7) and keeps its mips (BUG-65i: the kit was about 92 MB uncompressed; the mips stop the shimmer).</summary>
    [GateCheck("uipc.kit.compressed", Owner)]
    public static GateResult KitCompressed()
    {
        var wrong = new List<string>();
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { UiKitImporter.Folder.TrimEnd('/') });
        foreach (string path in guids.Select(AssetDatabase.GUIDToAssetPath))
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer))
                continue;
            TextureImporterPlatformSettings desktop = importer.GetPlatformTextureSettings("Standalone");
            if (!importer.mipmapEnabled || !desktop.overridden || desktop.format != TextureImporterFormat.BC7)
                wrong.Add(path);
        }
        return GateResult.Of("uipc.kit.compressed", Owner, guids.Length > 0 && wrong.Count == 0, $"{guids.Length - wrong.Count}/{guids.Length} kit textures BC7 with mips",
            wrong.Count == 0 ? "reimport the kit after UiKitImporter changes (its version bumps it)" : string.Join(", ", wrong.Take(10)));
    }

    /// <summary>Esc on the PC brings Settings to the front and the PC stays open (R3/R4).</summary>
    [GateCheck("uipc.esc", Owner, GateKind.Play)]
    public static GateResult Esc() => GateResults.Require("uipc.esc", Owner, "ui_pc_session.esc");

    /// <summary>A click on the bezel leaves the PC (R3/R4).</summary>
    [GateCheck("uipc.bezel", Owner, GateKind.Play)]
    public static GateResult Bezel() => GateResults.Require("uipc.bezel", Owner, "ui_pc_session.bezel");

    /// <summary>The tray clock reads the shift clock over today's date; no CRT box; no credits in the tray.</summary>
    [GateCheck("uipc.tray", Owner, GateKind.Play)]
    public static GateResult Tray() => GateResults.Require("uipc.tray", Owner, "ui_pc_session.tray");

    /// <summary>A double-click on an orderable card places exactly one order, charged once.</summary>
    [GateCheck("uipc.order", Owner, GateKind.Play)]
    public static GateResult Order() => GateResults.Require("uipc.order", Owner, "ui_pc_session.order");

    /// <summary>Orders keeps its scroll and selection across minimise and restore.</summary>
    [GateCheck("uipc.restore", Owner, GateKind.Play)]
    public static GateResult Restore() => GateResults.Require("uipc.restore", Owner, "ui_pc_session.restore");

    /// <summary>The PC's open and close have no single-frame full-screen jump (seqtools cuts on the session's pc_open and pc_close sequences).</summary>
    [GateCheck("uipc.transitions", Owner, GateKind.Play)]
    public static GateResult Transitions() => GateResults.Require("uipc.transitions", Owner, "ui_pc_session.seq.pc_*.cuts");
}
