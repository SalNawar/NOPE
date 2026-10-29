using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using NUnit.Framework;

/// <summary>
/// DESK-01 (Saleh's playtest of 2026-10-28, point 3: "textures are terrible
/// when you go overhead, visual glitches everywhere"): bake v5 of the
/// painted desk (E:\unity\NOPE-tools\design\desk\pipeline\export\v5,
/// round 2). Painted plates of the reading views (16:9, 5:4, 21:9) repaint
/// only what no office camera sees and the audit's faults; every other
/// texel keeps v3's paint, at v3's size, so the office view does not
/// change. The shell's level faces keep their own 4096 texture
/// (DeskShellTop, the shell's second material slot), and two level strips
/// close the mat's side gaps (DESK-02). These checks read the
/// installed data, the importer, the material and the prefab as text (the
/// house runner runs them outside Unity). Each fails when its part is
/// reverted.
/// </summary>
public class PaintedReadingBakeTests
{
    private const string Art = "Assets/Art/Office/DeskPainted/";
    private const string ImporterPath = "Assets/Editor/DeskPainted/DeskPaintedImporter.cs";

    private static string Root([CallerFilePath] string here = "") => File.Exists(Art + "Data/anchors.json") ? "." : Path.Combine(Path.GetDirectoryName(here), "..", "..", "..");

    private static string Read(string relative) => File.ReadAllText(Path.Combine(Root(), relative)).Replace("\r\n", "\n");

    private static string GuidOf(string metaRelative)
    {
        Match m = Regex.Match(Read(metaRelative), @"^guid: ([0-9a-f]{32})$", RegexOptions.Multiline);
        Assert.IsTrue(m.Success, metaRelative);
        return m.Groups[1].Value;
    }

    /// <summary>A PNG's width and height from its header.</summary>
    private static (int w, int h) PngSize(string relative)
    {
        byte[] head = new byte[24];
        using (FileStream f = File.OpenRead(Path.Combine(Root(), relative)))
            Assert.AreEqual(24, f.Read(head, 0, 24), relative);
        int Big(int at) => (head[at] << 24) | (head[at + 1] << 16) | (head[at + 2] << 8) | head[at + 3];
        return (Big(16), Big(20));
    }

    [Test]
    public void TheShell_HasTwoSlots_ItsLevelFacesTakeTheirOwnTexture_AndThePrefabLaysBothMaterials()
    {
        ContentNode shell = ContentJson.Parse(Read(Art + "Data/anchors.json")).Get("objects").Get(DeskPaintedMap.Shell);
        Assert.IsNotNull(shell.Get("texture_slots"), "anchors.json names the shell's slots' textures");
        IReadOnlyList<ContentNode> slots = shell.Get("texture_slots").Items;
        Assert.AreEqual(2, slots.Count);
        Assert.AreEqual("DeskShell_Paint.png", slots[0].Text, "slot 0: the shell's v3 paint");
        Assert.AreEqual("DeskShellTop_Paint.png", slots[1].Text, "slot 1: the level faces in the reading frames");
        Assert.AreEqual("3840", shell.Get("tris").Text, "v3's 3836 triangles and the two gap strips' four");

        string importer = Read(ImporterPath);
        StringAssert.Contains("Material[] slots = SlotMaterialsFor(source.name, anchors, materials);", importer, "the importer reads the slots");
        StringAssert.Contains("r.sharedMaterials = slots;", importer, "and lays one material per slot");
        StringAssert.Contains("baseName == DeskShellTop ? \"DeskPainted_ShellTop\"", importer, "the new texture's material has the shell's family name");

        string mat = Read(Art + "Materials/DeskPainted_ShellTop.mat");
        StringAssert.Contains("m_Shader: {fileID: 4800000, guid: " + GuidOf(Art + "Shaders/DeskPainted.shader.meta"), mat, "on NOPE/Desk Painted");
        StringAssert.Contains("- _BaseMap:\n        m_Texture: {fileID: 2800000, guid: " + GuidOf(Art + "Textures/DeskShellTop_Paint.png.meta"), mat, "its paint");
        StringAssert.Contains("- _MaskMap:\n        m_Texture: {fileID: 2800000, guid: " + GuidOf(Art + "Textures/DeskShellTop_Mask.png.meta"), mat, "its mask (the shell's, carried over)");

        string prefab = Read(Art + "Prefabs/DeskPainted.prefab");
        string shellMat = GuidOf(Art + "Materials/DeskPainted_Shell.mat.meta"), topMat = GuidOf(Art + "Materials/DeskPainted_ShellTop.mat.meta");
        StringAssert.Contains("  m_Materials:\n  - {fileID: 2100000, guid: " + shellMat + ", type: 2}\n  - {fileID: 2100000, guid: " + topMat + ", type: 2}\n", prefab,
            "the shell's renderer: the shell's material, then the level faces'");
    }

    [Test]
    public void TheReadingViewsTextures_AreAtTheirMeasuredSizes_AndTheNewOneKeepsTheShellsLineWork()
    {
        var sizes = new Dictionary<string, int>
        {
            // round 2: v3's sizes (an upsampled v3 texel samples otherwise through the office view's mips), but the new DeskShellTop
            ["Desk_Mat_Paint"] = 2048, ["DeskShellTop_Paint"] = 4096, ["DeskShell_Paint"] = 4096,
            ["Desk_Scanner_Paint"] = 1024, ["Desk_ChipReader_Paint"] = 1024, ["Desk_StampRack_Paint"] = 1024, ["Desk_PC_Paint"] = 1024,
            ["Desk_Detain_Paint"] = 256, ["Desk_GlassesStand_Paint"] = 128,
        };
        foreach (KeyValuePair<string, int> kv in sizes)
        {
            (int w, int h) = PngSize($"{Art}Textures/{kv.Key}.png");
            Assert.AreEqual(kv.Value, w, kv.Key);
            Assert.AreEqual(kv.Value, h, kv.Key);
            (int mw, int _) = PngSize($"{Art}Textures/{kv.Key.Replace("_Paint", "_Mask")}.png");
            Assert.AreEqual(kv.Value / 2, mw, kv.Key + "'s mask: half the paint");
        }
        string meta = Read(Art + "Textures/DeskShellTop_Paint.png.meta");
        StringAssert.Contains("mipMapMode: 1", meta, "Kaiser mips, as the shell and the mat");
        StringAssert.Contains("mipBias: -0.5", meta);
        StringAssert.Contains("\n    aniso: 16\n", meta);
        StringAssert.Contains("name == \"DeskShellTop_Paint\"", Read(ImporterPath), "the importer keeps them on the next import");
    }

    /// <summary>
    /// DESK-04 (the audit of 2026-10-28: "a slate-blue hairline along the paper slot"): the counter's see-through strip
    /// (Office/Desk/Counter/Zone/Strip, found at the hairline's pixels by the desk01_hairline_probe run) lay 0.4 mm over the
    /// pad, under the painted counter strip but in front of its bevel's foot. On the painted desk it lies under the mat (and
    /// over the well floor), so nothing of it draws.
    /// </summary>
    [Test]
    public void OnThePaintedDesk_TheCountersStrip_LiesUnderTheMat_OverTheWellFloor()
    {
        const float Pad = 1.075f;   // the painted desk's pad (the importer's log: "the pad at 1.0750 m"), the mat's top
        const float GapStrips = 1.0688f;   // the shell's level strips in the gaps beside the mat (bake v4, DESK-02)
        Assert.Less(DeskPaintedMap.CounterStripLift, 0f, "under the pad");
        Assert.Less(Pad + DeskPaintedMap.CounterStripLift, GapStrips - 0.001f,
            "under the gap strips: at 1.072 m its right end showed in the gap beside the mat as a blue block (DESK-01 round 2)");
        Assert.Greater(Pad + DeskPaintedMap.CounterStripLift, DeskPaintedMap.WellFloorY, "over the well floor");
        string binder = Read("Assets/Scripts/Office/OfficeSceneBinder.cs");
        StringAssert.Contains("counter.Bind(_office.transform.forward, DeskPaintedMap.CounterStripLift);", binder, "the binder sinks it on the painted desk");
        StringAssert.Contains("if (_painted)\n                counter.Bind(_office.transform.forward, DeskPaintedMap.CounterStripLift);", binder, "only there");
        StringAssert.Contains("Vector3.up * lift;", Read("Assets/Scripts/Office/Desk/DeskCounter.cs"), "the counter lays its zone at the lift it is given");
    }
}
