using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using NUnit.Framework;

/// <summary>
/// The enclosed booth (desk v3 round 13; Saleh on concept B: "not that
/// concept the one with the bars also where is the left wall"; export v3's
/// DeskBooth.fbx): the importer lays its five objects with the shell, at the
/// DeskFrame's origin, turned back only by the half turn every file needs,
/// casting no shadow, on the two booth materials whose textures import as
/// the desk's (paint sRGB, mask linear, BC7, clamped, trilinear); the lean is
/// the bake's (9 degrees in the meshes, checked at Build, never added again);
/// the bars stand behind and above the deal tray and the mat, so no sight
/// line from the chair to a paper meets one (the bake's check: 0 px covered
/// in 10 renders, the edge match 0.946, the traveller's face 10.4 px clear);
/// the glass and the hall's left city hide by flags, only under the painted
/// desk; and the hall's grade is the measured ratio, read with the
/// brightness. Data, the prefab and source text (Unity runs the look: R13).
/// Each check fails when its part is reverted.
/// </summary>
public class PaintedBoothTests
{
    private const string Art = "Assets/Art/Office/DeskPainted/";
    private const string ImporterPath = "Assets/Editor/DeskPainted/DeskPaintedImporter.cs";
    private const string BinderPath = "Assets/Scripts/Office/OfficeSceneBinder.cs";
    private const string HallScene = "Assets/Art/Office/AnimeHallLayers/AnimeHall.unity";

    /// <summary>The project's folder (the tests' working folder, or three up from this file's).</summary>
    private static string Root([CallerFilePath] string here = "") => File.Exists(Art + "Data/anchors.json") ? "." : Path.Combine(Path.GetDirectoryName(here), "..", "..", "..");

    private static string Read(string relative) => File.ReadAllText(Path.Combine(Root(), relative)).Replace("\r\n", "\n");

    private static ContentNode Json(string relative) => ContentJson.Parse(Read(relative));

    private static float Num(ContentNode node) => float.Parse(node.Text, CultureInfo.InvariantCulture);

    private static float F(string s) => float.Parse(s, CultureInfo.InvariantCulture);

    /// <summary>The guid of the asset whose .meta is <paramref name="metaRelative"/>.</summary>
    private static string GuidOf(string metaRelative)
    {
        Match m = Regex.Match(Read(metaRelative), @"^guid: ([0-9a-f]{32})$", RegexOptions.Multiline);
        Assert.IsTrue(m.Success, metaRelative);
        return m.Groups[1].Value;
    }

    /// <summary>A Unity YAML file's documents: the class id (1 GameObject, 4 Transform, 23 MeshRenderer), the anchor and the body of each.</summary>
    private static List<(int type, string id, string body)> Documents(string text)
    {
        var docs = new List<(int, string, string)>();
        MatchCollection heads = Regex.Matches(text, @"^--- !u!(\d+) &(-?\d+)", RegexOptions.Multiline);
        for (int i = 0; i < heads.Count; i++)
        {
            int start = heads[i].Index + heads[i].Length;
            int end = i + 1 < heads.Count ? heads[i + 1].Index : text.Length;
            docs.Add((int.Parse(heads[i].Groups[1].Value), heads[i].Groups[2].Value, text.Substring(start, end - start)));
        }
        return docs;
    }

    /// <summary>The anchor of the GameObject named <paramref name="name"/>.</summary>
    private static string ObjectNamed(List<(int type, string id, string body)> docs, string name)
    {
        foreach ((int type, string id, string body) in docs)
            if (type == 1 && Regex.IsMatch(body, @"^  m_Name: " + Regex.Escape(name) + "$", RegexOptions.Multiline))
                return id;
        Assert.Fail($"the prefab has no GameObject {name}");
        return null;
    }

    /// <summary>The component of class <paramref name="type"/> on the GameObject <paramref name="gameObject"/>: its anchor and body.</summary>
    private static (string id, string body) Component(List<(int type, string id, string body)> docs, int type, string gameObject)
    {
        foreach ((int t, string id, string body) in docs)
            if (t == type && body.Contains("m_GameObject: {fileID: " + gameObject + "}"))
                return (id, body);
        Assert.Fail($"no component of class {type} on {gameObject}");
        return default;
    }

    [Test]
    public void TheBooth_IsLaidWithTheShell_AtTheOrigin_TurnedBackOnlyByTheHalfTurn_CastingNoShadow()
    {
        string importer = Read(ImporterPath);
        StringAssert.Contains("private static readonly string[] Models = { \"DeskShell\", \"DeskBooth\", \"DeskObjects\", \"Desk_PC\" };", importer, "the booth's file is laid after the shell's");
        StringAssert.Contains("anchors.Get(\"objects\")?.Get(name) ?? anchors.Get(\"booth\")?.Get(\"objects\")?.Get(name)", importer, "the booth's pivots and textures come from anchors.json's booth section");
        StringAssert.Contains("case PaintedRole.Shell:\n                mesh.SetParent(shell, false);", importer, "a Shell object goes under Shell");
        StringAssert.Contains("mesh.SetPositionAndRotation(Upright * source.position, Upright * source.rotation);", importer, "turned back by the half turn every file needs, and nothing more");
        Assert.IsFalse(Regex.IsMatch(importer, @"AngleAxis\(\s*(-?9f|-?DeskPaintedMap\.BoothLeanDegrees)"), "the lean is in the meshes (the bake's D4): the importer adds no turn");
        StringAssert.Contains("r.shadowCastingMode = ShadowCastingMode.Off;", importer, "nothing painted casts (round 12)");
        StringAssert.Contains("\n            CheckBooth(root.transform, parts, anchors, log);\n", importer, "Build checks the booth as laid (the call itself, not a comment)");
        StringAssert.Contains("if (part.mesh.parent == null || part.mesh.parent.name != \"Shell\")", importer, "under Shell");
        StringAssert.Contains("if (r.shadowCastingMode != ShadowCastingMode.Off)", importer, "none casting");
        StringAssert.Contains("if (worst > BoothTolerance)", importer, "the bars' mesh on anchors.json's feet and tops");
        StringAssert.Contains("if (Mathf.Abs(bars.size.z - depth) > BoothDepthTolerance)", importer, "the bars' depth is the lean's: a turn added on top would double it");
        Assert.AreEqual(9f, DeskPaintedMap.BoothLeanDegrees, "the bake's D4, accepted");
        foreach (string name in DeskPaintedMap.Booth)
            Assert.AreEqual(PaintedRole.Shell, DeskPaintedMap.Find(name).role, name);

        // The prefab the WIP's Unity pass built (the editor log: 30 objects, 6 lamps, worst pivot 0.01 mm).
        List<(int type, string id, string body)> docs = Documents(Read(Art + "Prefabs/DeskPainted.prefab"));
        string shell = Component(docs, 4, ObjectNamed(docs, "Shell")).id;
        string booth = GuidOf(Art + "Materials/Desk_Booth.mat.meta"), bars = GuidOf(Art + "Materials/Desk_BoothBars.mat.meta");
        foreach (string name in DeskPaintedMap.Booth)
        {
            string go = ObjectNamed(docs, name);
            string transform = Component(docs, 4, go).body;
            StringAssert.Contains("m_LocalPosition: {x: 0, y: 0, z: 0}", transform, $"{name} sits at the DeskFrame's origin");
            Match rotation = Regex.Match(transform, @"m_LocalRotation: \{x: ([-0-9.e]+), y: ([-0-9.e]+), z: ([-0-9.e]+), w: ([-0-9.e]+)\}");
            Assert.IsTrue(rotation.Success, name);
            Assert.AreEqual(1f, Math.Abs(F(rotation.Groups[4].Value)), 1e-4f, $"{name}: the file's half turn cancelled, no turn of its own (the lean is in the mesh)");
            StringAssert.Contains("m_Father: {fileID: " + shell + "}", transform, $"{name} under Shell");
            string renderer = Component(docs, 23, go).body;
            StringAssert.Contains("m_CastShadows: 0", renderer, $"{name} casts no shadow");
            StringAssert.Contains("guid: " + (name == DeskPaintedMap.BoothBars ? bars : booth), renderer, $"{name}'s material");
        }
    }

    [Test]
    public void TheBoothsTextures_ImportAsTheDesks_AndItsTwoMaterialsTakeThem()
    {
        string importer = Read(ImporterPath);
        StringAssert.Contains("bool mask = name.EndsWith(\"_Mask\");", importer);
        StringAssert.Contains("importer.sRGBTexture = !mask;", importer, "paint sRGB, masks linear");
        StringAssert.Contains("importer.wrapMode = TextureWrapMode.Clamp;", importer);
        StringAssert.Contains("importer.filterMode = FilterMode.Trilinear;", importer);
        StringAssert.Contains("standalone.format = TextureImporterFormat.BC7;", importer);
        StringAssert.Contains("importer.mipmapEnabled = true;", importer);
        foreach (string texture in new[] { "Desk_Booth_Paint", "Desk_Booth_Mask", "Desk_BoothBars_Paint", "Desk_BoothBars_Mask" })
        {
            string meta = Read($"{Art}Textures/{texture}.png.meta");
            StringAssert.Contains(texture.EndsWith("_Mask") ? "sRGBTexture: 0" : "sRGBTexture: 1", meta, texture);
            StringAssert.Contains("enableMipMap: 1", meta, texture);
            StringAssert.Contains("wrapU: 1", meta, texture + " clamped");
            StringAssert.Contains("wrapV: 1", meta, texture + " clamped");
            StringAssert.Contains("filterMode: 2", meta, texture + " trilinear");
            Match standalone = Regex.Match(meta, @"buildTarget: Standalone\n\s+maxTextureSize: (\d+)\n(.*?)overridden: (\d)", RegexOptions.Singleline);
            Assert.IsTrue(standalone.Success, texture + " has a Standalone override");
            Assert.AreEqual("4096", standalone.Groups[1].Value, texture + " at its own size");
            StringAssert.Contains("textureFormat: 25", standalone.Groups[2].Value, texture + " BC7");
            Assert.AreEqual("1", standalone.Groups[3].Value, texture + " overridden");
        }
        string shader = GuidOf(Art + "Shaders/DeskPainted.shader.meta");
        foreach (string material in new[] { "Desk_Booth", "Desk_BoothBars" })
        {
            string mat = Read($"{Art}Materials/{material}.mat");
            StringAssert.Contains("m_Shader: {fileID: 4800000, guid: " + shader, mat, material + " on NOPE/Desk Painted");
            StringAssert.Contains("- _BaseMap:\n        m_Texture: {fileID: 2800000, guid: " + GuidOf($"{Art}Textures/{material}_Paint.png.meta"), mat, material + "'s paint");
            StringAssert.Contains("- _MaskMap:\n        m_Texture: {fileID: 2800000, guid: " + GuidOf($"{Art}Textures/{material}_Mask.png.meta"), mat, material + "'s mask");
            StringAssert.Contains("- _MASKMAP", mat, material + " reads its mask");
            StringAssert.Contains("- _UseMask: 1", mat, material);
            StringAssert.Contains("- _Locked: 0", mat, "no booth part is a locked home");
        }
        ContentNode booth = Json(Art + "Data/anchors.json").Get("booth").Get("objects");
        foreach (string name in DeskPaintedMap.Booth)
            Assert.AreEqual(name == DeskPaintedMap.BoothBars ? "Desk_BoothBars_Paint.png" : "Desk_Booth_Paint.png", booth.Get(name).Get("texture").Text, name);
    }

    /// <summary>The bake's check (export v3, check/check_report.json, 2026-10-21): the gates the booth passed, recorded here so a changed bake must pass them again.</summary>
    private const float EdgeMatchOverall = 0.9458f, EdgeMatchWorstRegion = 0.9281f, EdgeMatchGate = 0.85f, FaceClearancePx = 10.4f, FaceClearanceGate = 8f;

    /// <summary>Renders (office, reading, tilt 0.15, 0.40 and 0.70, each at 16:9 and 21:9) with a pixel of the mat or the deal tray's bed under a bar or the frame: the bake's check.</summary>
    private const int RendersWithTheMatOrTrayCovered = 0;

    /// <summary>The deal tray and the sill (the bake's geometry.json tray_bounds and check_report.json rail_over_tray; the bake's frame: y away from the chair, z above the floor, metres).</summary>
    private const float TrayFarEdgeY = 1.0f, TrayRimTopZ = 1.1412f, SillCapTopZ = 1.194f, SillBridgeTopZ = 1.179f;

    /// <summary>The office eye in the bake's frame (DeskPaintedImporter.OfficeEye, (0, 2.16, -2.10) in Unity's axes): 2.10 m before the mat's centre, 2.16 m over the floor.</summary>
    private const float EyeY = -2.10f, EyeZ = 2.16f;

    [Test]
    public void TheBars_StandBehindAndAboveTheDealTray_SoNoSightLineToAPaperMeetsOne_AndTheBakesGatesHold()
    {
        ContentNode booth = Json(Art + "Data/anchors.json").Get("booth");
        ContentNode bars = booth.Get("bars");
        IReadOnlyList<ContentNode> feet = bars.Get("foot_frame").Items, tops = bars.Get("top_frame").Items, centres = bars.Get("centres_x_frame").Items;
        Assert.AreEqual(12, feet.Count);
        Assert.AreEqual(12, tops.Count);
        Assert.AreEqual(12, centres.Count);
        float pitch = Num(bars.Get("pitch_m")), radius = Num(bars.Get("radius_m"));
        Assert.AreEqual(0.3352f, pitch, 1e-3f, "D2: even, 0.335 m apart");
        Assert.AreEqual(0.0192f, radius, 1e-4f, "19 mm: the painted brass core plus its ink lines");
        for (int i = 1; i < 12; i++)
            Assert.AreEqual(pitch, Num(centres[i]) - Num(centres[i - 1]), 1e-3f, "even");
        float footY = Num(feet[0].Items[1]), footZ = Num(feet[0].Items[2]), topY = Num(tops[0].Items[1]), topZ = Num(tops[0].Items[2]);
        foreach (ContentNode f in feet)
        {
            Assert.AreEqual(footY, Num(f.Items[1]), 1e-4f, "every foot on the rail");
            Assert.AreEqual(footZ, Num(f.Items[2]), 1e-4f);
        }
        foreach (ContentNode t in tops)
        {
            Assert.AreEqual(topY, Num(t.Items[1]), 1e-4f, "every top under the header");
            Assert.AreEqual(topZ, Num(t.Items[2]), 1e-4f);
        }

        // The lean (D4): in the data, once; the booth frame's up is the office camera's.
        double lean = Math.Atan2(topY - footY, topZ - footZ) * 180.0 / Math.PI;
        Assert.AreEqual(DeskPaintedMap.BoothLeanDegrees, lean, 0.1, "the tops stand behind the feet by the lean's tangent");
        ContentNode up = booth.Get("frame").Get("up_unity");
        Assert.AreEqual(DeskPaintedMap.BoothLeanDegrees, Math.Atan2(Num(up.Items[2]), Num(up.Items[1])) * 180.0 / Math.PI, 0.1, "anchors.json's booth frame leans the same");

        // Behind and above the tray: the bars' front past the tray's far edge, the feet on the rail over the sill cap, above the tray's rim; the paper path under the bridge.
        Assert.That(footY - radius, Is.GreaterThan(TrayFarEdgeY), "the bars' front stands behind the tray's far edge");
        Assert.That(footZ, Is.GreaterThan(TrayRimTopZ).And.GreaterThan(SillCapTopZ), "the feet stand on the rail over the sill cap, above the tray's rim");
        Assert.That(SillCapTopZ, Is.GreaterThan(SillBridgeTopZ), "the rail sits on the cap, above the bridge the papers pass under");

        // No sight line from the chair to a paper meets a bar: the line to the tray's far rim top passes the bars' front plane under their feet, and every line to a nearer or lower point (the whole tray, the mat) passes lower there.
        float frontY = footY - radius;
        float lineZ = EyeZ + (TrayRimTopZ - EyeZ) * (frontY - EyeY) / (TrayFarEdgeY - EyeY);
        Assert.That(lineZ, Is.LessThan(footZ - 0.05f), $"the sight line to the tray's far rim passes the bars' front at z {lineZ:0.000}, under the feet at {footZ:0.000}");
        float pad = F(Regex.Match(Read(Art + "Prefabs/DeskPainted.prefab"), @"paperPlane: ([0-9.]+)").Groups[1].Value);
        Assert.That(pad, Is.LessThan(TrayRimTopZ), "the mat's pad lies under the tray's rim");
        Assert.AreEqual(EyeZ, DeskHomeMap.OfficeEye.h + pad, 0.02f, "the office eye 1.09 m over the pad (DeskHomeMap) is 2.16 m over the floor (the importer's)");
        Assert.AreEqual(EyeY, DeskHomeMap.OfficeEye.y, 1e-3f);

        // The bake's gates, recorded.
        Assert.That(EdgeMatchOverall, Is.GreaterThanOrEqualTo(EdgeMatchGate), "the edge match against office_bars_v1.png");
        Assert.That(EdgeMatchWorstRegion, Is.GreaterThanOrEqualTo(EdgeMatchGate), "every region (the desk's 0.928 the lowest)");
        Assert.That(FaceClearancePx, Is.GreaterThanOrEqualTo(FaceClearanceGate), "D5: the traveller's face clear of the bars");
        Assert.AreEqual(0, RendersWithTheMatOrTrayCovered, "office, reading, tilt 0.15, 0.40 and 0.70, at 16:9 and 21:9");
    }

    [Test]
    public void TheGlassAndTheHallsLeftCity_HideByFlags_OnlyUnderThePaintedDesk_NothingDeleted()
    {
        Assert.AreEqual(PaintedRole.Hidden, DeskPaintedMap.Find(DeskPaintedMap.Glass).role, "open bars, no glass (the quad would stand in front of the bars' upper half)");
        string binder = Read(BinderPath);
        StringAssert.Contains("if (o.role == PaintedRole.Hidden)\n                paintedDesk.Hide(o.name);", binder, "the glass goes off at install");
        StringAssert.Contains("public void Hide(string name)\n    {\n        Transform mesh = Mesh(name);\n        if (mesh != null)\n            mesh.gameObject.SetActive(false);", Read("Assets/Scripts/Office/Desk/DeskPainted.cs"), "a flag, not a delete");
        StringAssert.Contains("if (_painted)\n                CloseHallBehindBooth(hall.transform);", binder, "the hall's left city goes off only under the painted desk");
        Match close = Regex.Match(binder, @"private static void CloseHallBehindBooth\(Transform hall\)\n    \{(.*?)\n    \}", RegexOptions.Singleline);
        Assert.IsTrue(close.Success, "the binder closes the hall behind the booth");
        StringAssert.Contains("Array.IndexOf(DeskPaintedMap.HallClosedByBooth, t.name) >= 0", close.Groups[1].Value, "by the map's names");
        StringAssert.Contains("r.enabled = false;", close.Groups[1].Value, "the renderers' flag, as Unshow's");
        StringAssert.DoesNotContain("Destroy", close.Groups[1].Value, "no scene object is deleted");
        StringAssert.Contains("paintedDesk.gameObject.SetActive(_painted);", binder, "the booth (in the prefab) is off with the painted desk");
        string hall = Read(HallScene);
        foreach (string name in DeskPaintedMap.HallClosedByBooth)
            StringAssert.Contains("m_Name: " + name + "\n", hall, $"the hall scene has {name}");
        CollectionAssert.DoesNotContain(DeskPaintedMap.HallClosedByBooth, "03 Exterior placeholder single layer", "the back windows' city stays: it shows through the bars as the painting's does");
    }

    /// <summary>The painting's hall between the bars (bars_reg.png: office_bars_v1.png registered to the office camera; the three gaps left of the traveller) and the game's hall there at paintedDeskBrightness (round 12's day 1 still, 09:00): mean sRGB (r13\measure_r13.py, 2026-10-21).</summary>
    private static readonly float[] PaintingGaps = { 46f, 38f, 27f }, GameGaps = { 94f, 83f, 75f };

    [Test]
    public void TheBoothsGrade_IsThePaintingsHallOverTheGames_ReadWithTheBrightness_WhiteOffThePaintedDesk()
    {
        string asset = Read("Assets/Data/Config/HallLighting_Default.asset");
        Match value = Regex.Match(asset, @"\n  paintedBoothGrade: \{r: ([0-9.]+), g: ([0-9.]+), b: ([0-9.]+), a: 1\}\n");
        Assert.IsTrue(value.Success, "HallLighting_Default.asset carries the booth's grade");
        for (int c = 0; c < 3; c++)
            Assert.AreEqual(PaintingGaps[c] / GameGaps[c], F(value.Groups[c + 1].Value), 0.015f, "r, g, b: the measured ratio, to the nearest hundredth");
        Assert.That(PaintingGaps[2] / GameGaps[2], Is.LessThan(PaintingGaps[0] / GameGaps[0] * 0.8f), "a colour, not one scale: the blue falls a quarter further than the red");
        string rig = Read("Assets/Scripts/Office/HallLightingRig.cs");
        StringAssert.Contains("backdrop.Brightness = PaintedDesk && painted != null && painted.length > 0 ? painted.Evaluate(Hour) : 1f;\n            backdrop.Grade = PaintedDesk ? settings.paintedBoothGrade : Color.white;", rig,
                              "read where paintedDeskBrightness is read, white off the painted desk");
        StringAssert.Contains("public Color Grade", Read("Assets/Scripts/Office/HallBackdrop.cs"), "the backdrop carries it to the full-screen draw");
    }
}
