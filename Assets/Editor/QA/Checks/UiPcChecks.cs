using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

/// <summary>
/// UI-PC's gate checks (docs/pending/UI-PC.md, the RESULT recipes). Static:
/// the UI kit is compressed with its mips, and its textures as imported fit
/// FIX_PLAN's 25 MB (BUG-65i). Play, on the PC session
/// (tools/qa/scenarios/ui_pc_session.json, its ops in UiPcRigOps; FIX_PLAN's
/// "the PC session script as a standing gate"): Esc brings Settings forward
/// (also with the screen off, and during the power-on, when a right-click
/// does not leave either), a click on the room stays and the bezel
/// leaves the PC (BUG-50a, R3/R4), the tray clock (BUG-20a, 21a), 8+ windows
/// on the taskbar with the chevron (BUG-21a), one order per double-click,
/// one action per Enter, the 150 % zoom, Orders' state across minimise and
/// restore and one balance everywhere (BUG-58a), and the PC's open and close
/// with no single-frame full-screen jump, also when turned back under way
/// (BUG-32c; seqtools cuts on pc_open, pc_close, pc_reverse_open and
/// pc_reverse_close, all recorded with full motion, and the glass on each
/// reversing frame). A check that judges more than one result requires each
/// by its own id (<see cref="JudgeEach"/>), so a result never written fails
/// by name instead of the measured ones passing for it.
/// </summary>
public static class UiPcChecks
{
    private const string Owner = "UI-PC";

    /// <summary>The PC session's scenario name, the prefix of its result ids.</summary>
    private const string Session = "ui_pc_session";

    /// <summary>The PC session's recordings of the PC's moves, in its order: ui_pc_session.json's recStart names, each right after a uipcFullMotion of the same name (UiPcChecksTests keeps the two in step).</summary>
    public static readonly string[] Recordings = { "pc_open", "pc_close", "pc_reverse_open", "pc_reverse_close" };

    /// <summary>The results <c>uipc.transitions</c> requires: seqtools cuts on each of <see cref="Recordings"/> (recipe 3), each by its own id.</summary>
    public static IEnumerable<string> TransitionIds => Recordings.Select(r => $"{Session}.seq.{r}.cuts");

    /// <summary>The results <c>uipc.fullmotion</c> requires: uipcFullMotion's assert before each of <see cref="Recordings"/>.</summary>
    public static IEnumerable<string> FullMotionIds => Recordings.Select(r => $"{Session}.fullmotion.{r}");

    /// <summary>The results <c>uipc.reverse</c> requires: both moves turned back under way (uipcReverseOpen, uipcReverseClose).</summary>
    public static readonly string[] ReverseIds = { Session + ".reverse.open", Session + ".reverse.close" };

    /// <summary>The results <c>uipc.poweron</c> requires: uipcPowerOn's two passes (full motion, Reduced Motion).</summary>
    public static readonly string[] PowerOnIds = { Session + ".poweron.full", Session + ".poweron.reduced" };

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

    /// <summary>FIX_PLAN's pass line for the UI kit's texture memory (BUG-65i: "from ~92 MB to <= 25 MB"), in bytes (decimal MB, the stricter reading).</summary>
    private const long KitBudgetBytes = 25L * 1000L * 1000L;

    /// <summary>
    /// The UI kit's texture memory as imported (BUG-65i): every kit texture's
    /// GPU size, each mip in its imported format (GraphicsFormatUtility), at
    /// most 25 MB with the mips kept. From the 272 PNGs' sizes this is about
    /// 23.2 MB in BC7 (92.1 MB as RGBA32 with mips: the audit's ~92 MB). The
    /// memory profile in play (recipe 1) confirms it in the running game.
    /// </summary>
    [GateCheck("uipc.kit.memory", Owner)]
    public static GateResult KitMemory()
    {
        long bytes = 0;
        var largest = new List<(long size, string path)>();
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { UiKitImporter.Folder.TrimEnd('/') });
        foreach (string path in guids.Select(AssetDatabase.GUIDToAssetPath))
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
                continue;
            long size = 0;
            for (int mip = 0; mip < texture.mipmapCount; mip++)
                size += GraphicsFormatUtility.ComputeMipmapSize(Mathf.Max(1, texture.width >> mip), Mathf.Max(1, texture.height >> mip), texture.graphicsFormat);
            bytes += size;
            largest.Add((size, path));
        }
        string top = string.Join(", ", largest.OrderByDescending(t => t.size).Take(5).Select(t => $"{System.IO.Path.GetFileName(t.path)} {t.size / 1e6:0.00} MB"));
        return GateResult.Of("uipc.kit.memory", Owner, guids.Length > 0 && bytes <= KitBudgetBytes, $"{bytes / 1e6:0.0} MB in {guids.Length} kit textures (budget {KitBudgetBytes / 1e6:0} MB)",
            bytes <= KitBudgetBytes ? "the largest: " + top : "over budget; the largest: " + top + ". Reimport the kit (UiKitImporter v2), then atlas or drop mip 0 of the largest panels");
    }

    /// <summary>Esc on the PC brings Settings to the front and the PC stays open (R3/R4).</summary>
    [GateCheck("uipc.esc", Owner, GateKind.Play)]
    public static GateResult Esc() => GateResults.Require("uipc.esc", Owner, "ui_pc_session.esc");

    /// <summary>With the screen off, Esc turns it on and brings Settings forward; a right-click does nothing; the PC stays open (R3/R4).</summary>
    [GateCheck("uipc.escdark", Owner, GateKind.Play)]
    public static GateResult EscDark() => GateResults.Require("uipc.escdark", Owner, "ui_pc_session.escdark");

    /// <summary>A click on the room beside the frame does not leave the PC (R3/R4: only Q and the bezel).</summary>
    [GateCheck("uipc.room", Owner, GateKind.Play)]
    public static GateResult Room() => GateResults.Require("uipc.room", Owner, "ui_pc_session.room");

    /// <summary>8+ windows: a labelled button or a place behind the chevron for each, nothing over the tray, the chevron brings each forward (BUG-21a).</summary>
    [GateCheck("uipc.windows", Owner, GateKind.Play)]
    public static GateResult Windows() => GateResults.Require("uipc.windows", Owner, "ui_pc_session.windows");

    /// <summary>One action per Enter, and two quick Enters act once (BUG-58a, CODE-117).</summary>
    [GateCheck("uipc.enter", Owner, GateKind.Play)]
    public static GateResult Enter() => GateResults.Require("uipc.enter", Owner, "ui_pc_session.enter");

    /// <summary>Ctrl+= zooms the Orders tree to 150 %.</summary>
    [GateCheck("uipc.zoom", Owner, GateKind.Play)]
    public static GateResult Zoom() => GateResults.Require("uipc.zoom", Owner, "ui_pc_session.zoom");

    /// <summary>The balance is the same in the Account, Orders and the office's readout, and the Account follows it live in place (BUG-58a).</summary>
    [GateCheck("uipc.balance", Owner, GateKind.Play)]
    public static GateResult Balance() => GateResults.Require("uipc.balance", Owner, "ui_pc_session.balance");

    /// <summary>A right-click and Esc during the power-on (full motion and Reduced Motion): the PC stays open and Settings comes forward (R3/R4: the keys and the back-out are the PC's from the press frame).</summary>
    [GateCheck("uipc.poweron", Owner, GateKind.Play)]
    public static GateResult PowerOn() => RequireEach("uipc.poweron", PowerOnIds);

    /// <summary>A click on the bezel leaves the PC (R3/R4).</summary>
    [GateCheck("uipc.bezel", Owner, GateKind.Play)]
    public static GateResult Bezel() => GateResults.Require("uipc.bezel", Owner, "ui_pc_session.bezel");

    /// <summary>The tray clock reads the shift clock over today's date; no CRT box; no credits in the tray.</summary>
    [GateCheck("uipc.tray", Owner, GateKind.Play)]
    public static GateResult Tray() => GateResults.Require("uipc.tray", Owner, "ui_pc_session.tray");

    /// <summary>A double-click on an orderable card places exactly one order, charged once.</summary>
    [GateCheck("uipc.order", Owner, GateKind.Play)]
    public static GateResult Order() => GateResults.Require("uipc.order", Owner, "ui_pc_session.order");

    /// <summary>Orders keeps its scroll, its chosen card and its zoom across minimise and restore by its own buttons.</summary>
    [GateCheck("uipc.restore", Owner, GateKind.Play)]
    public static GateResult Restore() => GateResults.Require("uipc.restore", Owner, "ui_pc_session.restore");

    /// <summary>The PC's open and close have no single-frame full-screen jump, also when turned back under way (seqtools cuts on each of the session's sequences pc_open, pc_close, pc_reverse_open and pc_reverse_close; one not judged fails by name).</summary>
    [GateCheck("uipc.transitions", Owner, GateKind.Play)]
    public static GateResult Transitions() => RequireEach("uipc.transitions", TransitionIds);

    /// <summary>The session recorded the PC's moves (pc_open, pc_close, pc_reverse_open, pc_reverse_close) with full motion, so <c>uipc.transitions</c> judged the CRT line and not the Reduced fade (Reduced Motion is a saved preference: set by hand, or left on by a run stopped during uipcPowerOn's reduced pass).</summary>
    [GateCheck("uipc.fullmotion", Owner, GateKind.Play)]
    public static GateResult FullMotion() => RequireEach("uipc.fullmotion", FullMotionIds);

    /// <summary>A move turned back under way (Q 2 frames into the power-on; a click on the office's PC during the collapse, which the closing frame lets through) picks up where the glass is: on the reversing frame the glass is the frame before's, no step of the frame's alpha is a cut's, and it ends where the new move goes (BUG-32c, F1; CrtPower.Resume as PcFrame draws it).</summary>
    [GateCheck("uipc.reverse", Owner, GateKind.Play)]
    public static GateResult Reverse() => RequireEach("uipc.reverse", ReverseIds);

    /// <summary><see cref="JudgeEach"/> on the results folder at HEAD (GateResults.Require's sources).</summary>
    private static GateResult RequireEach(string checkId, IEnumerable<string> ids) =>
        JudgeEach(checkId, ids, GateResults.ReadAll(), GitHead.Read(GateResults.ProjectRoot));

    /// <summary>
    /// The verdict of check <paramref name="checkId"/> that requires every one
    /// of <paramref name="ids"/>: it passes only when each id has its own
    /// result, green and measured at <paramref name="head"/> on a clean tree
    /// (GateResults.Judge on each). A single wildcard goes green on any subset
    /// of its matches, so a result never written (cuts not run on one
    /// recording) would pass unseen; here it fails by name. Pure; UiPcChecksTests.
    /// </summary>
    public static GateResult JudgeEach(string checkId, IEnumerable<string> ids, IEnumerable<QaGateResult> results, string head)
    {
        List<QaGateResult> all = results.ToList();
        List<string> list = ids.ToList();
        List<GateResult> parts = list.Select(id => GateResults.Judge(checkId, Owner, id, all, head, false)).ToList();
        return GateResult.Of(checkId, Owner, list.Count > 0 && parts.All(r => r.Pass), string.Join("; ", list.Zip(parts, (id, r) => $"{id}: {r.Measured}")),
            string.Join("; ", parts.Where(r => !r.Pass).Select(r => r.Detail)));
    }
}
