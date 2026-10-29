using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;

/// <summary>
/// DESK-01 (Saleh's playtest of 2026-10-28, point 3: "textures are terrible
/// when you go overhead, visual glitches everywhere"), round 3: the
/// director's decision of 2026-10-29, two texture sets. The office set is
/// v3's paint, byte for byte (the files at 32c54e8), on v3's meshes, so the
/// office view cannot change. The reading set (name_Read.png, the same UVs,
/// E:\unity\NOPE-tools\design\desk\pipeline\export\v6) is painted wholly
/// from the overhead plates. NOPE/Desk Painted lerps the two by one global,
/// _DeskReadingBlend, which DeskView sets from the Tab tilt (0 in the office
/// pose, 1 in the reading pose). Two gap strips beside the mat (DESK-02)
/// draw only in the reading blend. These checks read the installed files
/// as bytes and text (the house runner runs them outside Unity). Each fails
/// when its part is reverted.
/// </summary>
public class PaintedReadingBakeTests
{
    private const string Art = "Assets/Art/Office/DeskPainted/";
    private const string ImporterPath = "Assets/Editor/DeskPainted/DeskPaintedImporter.cs";
    private const string ShaderPath = Art + "Shaders/DeskPainted.shader";

    private static string Root([CallerFilePath] string here = "") => File.Exists(Art + "Data/anchors.json") ? "." : Path.Combine(Path.GetDirectoryName(here), "..", "..", "..");

    private static string Read(string relative) => File.ReadAllText(Path.Combine(Root(), relative)).Replace("\r\n", "\n");

    private static string GuidOf(string metaRelative)
    {
        Match m = Regex.Match(Read(metaRelative), @"^guid: ([0-9a-f]{32})$", RegexOptions.Multiline);
        Assert.IsTrue(m.Success, metaRelative);
        return m.Groups[1].Value;
    }

    /// <summary>The file's SHA-256 as git LFS names it: the file's own bytes, or the oid of an LFS pointer (a worktree without the LFS objects).</summary>
    private static string LfsOid(string relative)
    {
        byte[] bytes = File.ReadAllBytes(Path.Combine(Root(), relative));
        string head = Encoding.ASCII.GetString(bytes, 0, Math.Min(bytes.Length, 200));
        if (head.StartsWith("version https://git-lfs", StringComparison.Ordinal))
            return Regex.Match(head, @"oid sha256:([0-9a-f]{64})").Groups[1].Value;
        using (SHA256 sha = SHA256.Create())
            return string.Concat(sha.ComputeHash(bytes).Select(b => b.ToString("x2")));
    }

    /// <summary>A PNG's width and height from its header (null for an LFS pointer).</summary>
    private static (int w, int h)? PngSize(string relative)
    {
        byte[] head = new byte[24];
        using (FileStream f = File.OpenRead(Path.Combine(Root(), relative)))
            Assert.AreEqual(24, f.Read(head, 0, 24), relative);
        if (head[0] != 0x89)
            return null;
        int Big(int at) => (head[at] << 24) | (head[at + 1] << 16) | (head[at + 2] << 8) | head[at + 3];
        return (Big(16), Big(20));
    }

    /// <summary>The office set at 32c54e8 (before DESK-01): each texture's git LFS oid (SHA-256 of its bytes).</summary>
    private static readonly Dictionary<string, string> V3 = new Dictionary<string, string>
    {
            ["DeskShell_Mask.png"] = "0ffc112215b4ed465ae8515cb5b093cfae750eb88670aceaf8b6878c3f3fbc59",
            ["DeskShell_Paint.png"] = "b205e507fb1e1718306566da45f15f25a9b1929ffff718348d42e227ba16edf5",
            ["Desk_AvailableSign_Mask.png"] = "063b4abb3e0a062f7bbd9fd419723a9ffe650ace9fa290ba0e00a111e0077133",
            ["Desk_AvailableSign_Paint.png"] = "3b99a757da8c2753f576f36389971ef9c9744518aeac83792cf84708ae30df9d",
            ["Desk_BabelHome_Mask.png"] = "eb60cea861ce9e326e9d82a147eda2eb75c789a06a2adbb195b4dea11988d9b9",
            ["Desk_BabelHome_Paint.png"] = "6bd4c5af5e643a728ade48715262338501cb298177f1891b08ea8109ee384b97",
            ["Desk_Bell_Mask.png"] = "48be9be0d872e15a6cbf3dca731181f95163cb83429cc5c78e0c240d71c99d97",
            ["Desk_Bell_Paint.png"] = "b0089599e6c8bcf7e71d127a586dd7620f57fea5abb0789218f79d5157741ec0",
            ["Desk_BoothBars_Mask.png"] = "5c8d6ea5d49cb09787c0d065ca019bd503f3b6419770431f3f786b02308ab786",
            ["Desk_BoothBars_Paint.png"] = "fabdb4e6c91e96575c9ff8303df6ddc42682f813169b56fc7a52bf3b85b7100c",
            ["Desk_Booth_Mask.png"] = "1d6727a3ef379b71ccf41c05bc968f8c684449404596ce37940b853529064402",
            ["Desk_Booth_Paint.png"] = "46ab04ffded962add05e75b5aab742d8fc8666e3b938922f20f9a4d7972e254b",
            ["Desk_CardsHome_Mask.png"] = "6f61ec055f753375ed49c1337bccd26ad4d2f8451d57b51af636083f3c02ba2b",
            ["Desk_CardsHome_Paint.png"] = "5ce20f1179210591d13be2d0025d9918ff645977906578d05aa71b0c434af82d",
            ["Desk_ChipReader_Mask.png"] = "30bbb31d2e04da32f68b2e89d01c8eaf68d6e353bd8d78817f71fb1f1b77378c",
            ["Desk_ChipReader_Paint.png"] = "119379ead59bc84cc4b0d799c1145305b2e51cae8f0eb908e7dea9dcf3844611",
            ["Desk_DetainGuard_Mask.png"] = "3cdef3ef88740f5c5416ceb6470328f25b8f692aabeb82518a319a87f6d271fc",
            ["Desk_DetainGuard_Paint.png"] = "f5d1d3329ee1fcfb881bb5bcf0dd1d19e58cc4fbe33c9277543afcdb976eaae5",
            ["Desk_Detain_Mask.png"] = "9f6f52c3980ffd0215b0b58430178cabe07298d3a0b40baa2e1dd446a48b3bd7",
            ["Desk_Detain_Paint.png"] = "c22ca391d07722b9fbcb25191e7f42f08f5a49497d1362489bdb8fd4b8404e68",
            ["Desk_GlassesStand_Mask.png"] = "4a73fc2839b1895c2a383c4c154277547314d49b346c9b6bb7446fad13e29740",
            ["Desk_GlassesStand_Paint.png"] = "a6a8feeb94fa5d9f4283f816af18d819558e7618c8633ef420ee0f4d3a755951",
            ["Desk_Glasses_Mask.png"] = "8e290312fd0fe38fb92eb03cb3b83ae9cbf9418b99d42dead797fc2952d3ff9b",
            ["Desk_Glasses_Paint.png"] = "3c8b712c576c75d6853bf070c5cf9b3bc56eaebcfa9c3d9efcf53d87c6101a02",
            ["Desk_Intercom_Mask.png"] = "5bd41e601a05ba558a1953662d78c37d7f7113ce894f564dbb1d45f3272f04cd",
            ["Desk_Intercom_Paint.png"] = "955012a1d1a30facc48745d795c3754185fe8a71913802f9f15b63026c321fdf",
            ["Desk_LampBezel_Mask.png"] = "5fb9aa7b28caebb0397016196fffeb84e01262a7c90a93ac0016cbaf0804f3b4",
            ["Desk_LampBezel_Paint.png"] = "e09ce1917fa871b1df1cdf74daf66cd6bd7c1e36b843ec54f8c06007af6f04ef",
            ["Desk_Mat_Mask.png"] = "fe34d1c8fd6c6bdb668a1b57440fa480dcf2174f55ec67c32d1800fe0c3c276c",
            ["Desk_Mat_Paint.png"] = "516a109e617f3458b4548d315ae96f2c486ba78d358e36e0a5bed9afb0a82f23",
            ["Desk_PC_Mask.png"] = "e05436a41d4bbe44733b20d2c3e1ee57a9876e43f954578936440be182bddd07",
            ["Desk_PC_Paint.png"] = "775cb8037fc18d3a5aa4a1298df2cce4adeb681f024cd10cfc3e07c8202188b0",
            ["Desk_Pocket_Mask.png"] = "e6c880a36e8b9c492066f1b8d66bfe1a21da4bd196de475ec8d5857c14a627ef",
            ["Desk_Pocket_Paint.png"] = "bee09aa71742cf5aeae8394de4a4ca9197e02d94381ad3581de246c28f7558a9",
            ["Desk_RolodexHome_Mask.png"] = "6eea61e401508706d3e54ac01b7679cb4f32a3ba1d420be4945d22a5c6ece1a9",
            ["Desk_RolodexHome_Paint.png"] = "89b2c8e3225e9518625a8227628d6a9b98df3c31651eee3ee3eda37396c31566",
            ["Desk_RulebookFolded_Mask.png"] = "39ffa5d269c396364dabf848c085e7d03a0db973ae4f4c4cb444e766ca7df888",
            ["Desk_RulebookFolded_Paint.png"] = "891e52224bce170c1beac1a33cc4cb0459337e99087b4c45afcb1ac725ed8bff",
            ["Desk_Scanner_Mask.png"] = "f6c3480dd154200a7e0d63376126ac3f8c1b05945ace74fa6a25efb3e90e60fd",
            ["Desk_Scanner_Paint.png"] = "2e64c007b7d48c9fb11d958793029d3e9f4cd68f31b3666c1817eaeb2fab246c",
            ["Desk_StampApprove_Mask.png"] = "61a9361163ca33387aead03c56111c8744cf6d7ed44ba25d895fdc0933e53149",
            ["Desk_StampApprove_Paint.png"] = "df38391b95944e9e25af03509f29594447fd96d52927e48a9bb67f990c2b5ac4",
            ["Desk_StampDeny_Mask.png"] = "d7992e984a9d1ee86a5670b0e81759118299bd90f0affcfa707285e48e7fe414",
            ["Desk_StampDeny_Paint.png"] = "40fda12e2e8481109e195e416fa50936dba5ab9fffc70767c59c519e9ac32546",
            ["Desk_StampRack_Mask.png"] = "7fef1fec5d4d3ebd9ccda299dae566351f2565fdda491d9ebbb4499806676346",
            ["Desk_StampRack_Paint.png"] = "3d2e62271aa122749f0d83be087703a47f9f6e56054467c4b822508b9c0182c5",
            ["Desk_UvPouch_Mask.png"] = "855f50d46814df3c151340fdbbedc28730210c2aca4894df40de2811dbdd92fe",
            ["Desk_UvPouch_Paint.png"] = "34350f62e706dc77ee529a56ef899f2f75867f4d17a8675839c5e3275f6f95f8",
            ["Desk_UvTorch_Mask.png"] = "fd2104736f96909057a9958bdcb60db93207dc69d77951848cad23a882469f48",
            ["Desk_UvTorch_Paint.png"] = "69ffa975300e08f289db7a19260c35fa9284ab5930d838f2d0cc968fb5cec3f8",
            ["Glass_Sheen.png"] = "af7fb8dfa45e205c20d3090359752029ae6d4f8e87bf7e477bb05e0cccb95505",
            ["Lamp_Jewel.png"] = "68acd9d0f82873d8b263543cf15189dfe6b11d92f8e899799a89d51cb0fb36c8",
    };

    [Test]
    public void TheOfficeSet_IsV3sPaint_ByteForByte_OnV3sShell()
    {
        string folder = Path.Combine(Root(), Art + "Textures");
        foreach (KeyValuePair<string, string> kv in V3)
            Assert.AreEqual(kv.Value, LfsOid(Art + "Textures/" + kv.Key), kv.Key + " is v3's (32c54e8)");
        string[] office = Directory.GetFiles(folder, "*.png").Select(Path.GetFileName).Where(f => !f.EndsWith("_Read.png")).ToArray();
        CollectionAssert.AreEquivalent(V3.Keys, office, "the office set is v3's files and no other (no DeskShellTop: rounds 1 and 2)");

        ContentNode shell = ContentJson.Parse(Read(Art + "Data/anchors.json")).Get("objects").Get(DeskPaintedMap.Shell);
        Assert.IsNull(shell.Get("texture_slots"), "v3's shell: one material slot");
        Assert.AreEqual("5285", shell.Get("tris").Text, "v3's anchors.json (bake v4's shell had 3840: its n-gons triangulated, the gap strips added)");
        Assert.IsFalse(File.Exists(Path.Combine(Root(), Art + "Materials/DeskPainted_ShellTop.mat")), "no second shell material");
    }

    /// <summary>The reading set's sizes: v3's but where v3's gives under a texel a screen pixel at 1080p in the reading view.</summary>
    private static readonly Dictionary<string, (int w, int h)> ReadingSizes = new Dictionary<string, (int, int)>
    {
        ["DeskShell"] = (4096, 4096), ["Desk_Mat"] = (2048, 2048), ["Desk_PC"] = (2048, 2048), ["Desk_Detain"] = (512, 512),
        ["Desk_GlassesStand"] = (256, 256), ["Desk_DetainGuard"] = (1024, 1024), ["Desk_AvailableSign"] = (1024, 1024),
        ["Desk_Bell"] = (1024, 1024), ["Desk_ChipReader"] = (1024, 1024), ["Desk_Glasses"] = (1024, 1024), ["Desk_Intercom"] = (1024, 1024),
        ["Desk_Pocket"] = (1024, 1024), ["Desk_RulebookFolded"] = (1024, 1024), ["Desk_Scanner"] = (1024, 1024),
        ["Desk_StampApprove"] = (1024, 1024), ["Desk_StampDeny"] = (1024, 1024), ["Desk_StampRack"] = (1024, 1024),
        ["Desk_UvPouch"] = (512, 512), ["Desk_UvTorch"] = (512, 512), ["Desk_Booth"] = (2048, 2048), ["Desk_BoothBars"] = (256, 1024),
        ["Desk_LampBezel"] = (256, 256), ["DeskGapStrips"] = (64, 1024),
    };

    [Test]
    public void TheReadingSet_IsThere_AtItsSizes_AndEveryPaintedMaterialLerpsToIt()
    {
        foreach (KeyValuePair<string, (int w, int h)> kv in ReadingSizes)
        {
            string png = $"{Art}Textures/{kv.Key}_Read.png";
            Assert.IsTrue(File.Exists(Path.Combine(Root(), png)), png);
            (int w, int h)? size = PngSize(png);
            if (size != null)
                Assert.AreEqual(kv.Value, size.Value, kv.Key + "_Read's size");
            StringAssert.Contains("\n    aniso: 16\n", Read(png + ".meta"), kv.Key + "_Read: the most anisotropic taps");
            if (kv.Key == "DeskGapStrips")
                continue;
            string material = kv.Key == "DeskShell" ? "DeskPainted_Shell" : kv.Key == "Desk_Mat" ? "DeskPainted_Mat" : kv.Key;
            string mat = Read($"{Art}Materials/{material}.mat");
            StringAssert.Contains("- _BaseMap:\n        m_Texture: {fileID: 2800000, guid: " + GuidOf($"{Art}Textures/{kv.Key}_Paint.png.meta"), mat, material + ": the office paint is v3's");
            StringAssert.Contains("- _ReadingMap:\n        m_Texture: {fileID: 2800000, guid: " + GuidOf(png + ".meta"), mat, material + ": its reading paint");
            StringAssert.Contains("\n    - _Reading: 1\n", mat, material + " lerps to it");
        }
        foreach (string sharp in new[] { "DeskShell_Read", "Desk_Mat_Read" })
        {
            string meta = Read($"{Art}Textures/{sharp}.png.meta");
            StringAssert.Contains("mipMapMode: 1", meta, sharp + ": Kaiser mips, as the office paint of the shell and the mat");
            StringAssert.Contains("mipBias: -0.5", meta, sharp);
        }
        string importer = Read(ImporterPath);
        StringAssert.Contains("public const string ReadingSuffix = \"_Read\";", importer);
        StringAssert.Contains("Reading(m, AssetDatabase.LoadAssetAtPath<Texture2D>($\"{TextureFolder}/{baseName}{ReadingSuffix}.png\"));", importer, "the importer lays each material's reading paint");
    }

    [Test]
    public void TheShader_LerpsTheTwoSets_ByTheTiltsGlobal_AndKeepsTheSrpBatcher()
    {
        string shader = Read(ShaderPath);
        string cbuffer = shader.Substring(shader.IndexOf("CBUFFER_START(UnityPerMaterial)", StringComparison.Ordinal));
        cbuffer = cbuffer.Substring(0, cbuffer.IndexOf("CBUFFER_END", StringComparison.Ordinal));
        StringAssert.Contains("half _Reading, _ReadingOnly;", cbuffer, "the per-material switches sit in the one UnityPerMaterial buffer (SRP Batcher)");
        StringAssert.DoesNotContain("_DeskReadingBlend", cbuffer, "the tilt's share is a global, not a material property");
        StringAssert.Contains("float _DeskReadingBlend;", shader);
        StringAssert.Contains("half reading = (half)saturate(_DeskReadingBlend) * _Reading;", shader);
        StringAssert.Contains("if (reading > 0.0h)\n                    paint = lerp(paint, SAMPLE_TEXTURE2D_GRAD(_ReadingMap, sampler_ReadingMap, i.uv, uvDx, uvDy).rgb, reading);", shader,
            "at a share of 0 the office paint is v3's exactly (the reading paint is not sampled)");
        StringAssert.Contains("clip(_DeskReadingBlend - 0.001);", shader, "a reading-only material draws nothing in the office pose");
        Assert.AreEqual(3, Regex.Matches(shader, @"                ClipReadingOnly\(\);").Count, "in the forward, shadow and depth passes");
    }

    [Test]
    public void TheReadingShare_FollowsTheCamerasBlend()
    {
        Assert.AreEqual(0f, DeskViewPose.BlendShare(0f, 0f, 1f), 1e-6f, "the office pose");
        Assert.AreEqual(1f, DeskViewPose.BlendShare(1f, 0f, 1f), 1e-6f, "the reading pose");
        Assert.AreEqual(0.25f, DeskViewPose.BlendShare(0.25f, 0f, 1f), 1e-6f, "the tilt's eased weight, on the way in");
        Assert.AreEqual(0.75f, DeskViewPose.BlendShare(0.25f, 1f, 0f), 1e-6f, "and on the way out");
        Assert.AreEqual(0.6f, DeskViewPose.BlendShare(0.5f, 0.2f, 1f), 1e-6f, "a blend from the middle of another (nested)");
        Assert.AreEqual(1f, DeskViewPose.BlendShare(2f, 0f, 1f), 1e-6f, "kept in 0..1");
        string view = Read("Assets/Scripts/Office/DeskView.cs");
        StringAssert.Contains("CinemachineCore.CameraUpdatedEvent.AddListener(HandleCameraUpdated);", view, "set once a frame, after the brain moves the camera");
        StringAssert.Contains("SetReadingBlend(blend != null ? ShareOf(blend, 0) : ReferenceEquals(brain.ActiveVirtualCamera, deskCamera) ? 1f : 0f);", view,
            "from the brain's own blend: a cut (Reduced Motion) cuts it too, with no motion of its own");
        StringAssert.Contains("Shader.SetGlobalFloat(ReadingBlendId, share);", view);
        StringAssert.Contains("Shader.PropertyToID(\"_DeskReadingBlend\")", view);
    }

    [Test]
    public void TheGapStrips_CloseTheMatsSideGaps_OnlyInTheReadingBlend()
    {
        string desk = Read("Assets/Scripts/Office/Desk/DeskPainted.cs");
        StringAssert.Contains("CloseDealTraySlot();\n        CloseGapStrips();", desk, "laid at install (Align)");
        StringAssert.Contains("Resources.Load<Material>(GapStripsMaterial)", desk);
        string mat = Read(Art + "Resources/DeskGapStrips.mat");
        StringAssert.Contains("m_Shader: {fileID: 4800000, guid: " + GuidOf(ShaderPath + ".meta"), mat, "on NOPE/Desk Painted");
        StringAssert.Contains("\n    - _ReadingOnly: 1\n", mat, "drawn only in the reading blend");
        StringAssert.Contains("\n    - _ZWrite: 1\n", mat, "it writes depth once drawn");
        StringAssert.Contains("m_CustomRenderQueue: 2450", mat, "after the opaque desk, before the transparent counter strip");
        StringAssert.Contains("- _BaseMap:\n        m_Texture: {fileID: 2800000, guid: " + GuidOf(Art + "Textures/DeskGapStrips_Read.png.meta"), mat);
    }

    /// <summary>
    /// DESK-04 (the slate-blue hairline along the paper slot, and the blue block at its right end): the counter's see-through
    /// strip stays where it lay at 32c54e8 (the office view keeps its look) and fades out with the reading view on the painted
    /// desk. Rounds 1 and 2 sank it under the mat, which also took its line out of the office view.
    /// </summary>
    [Test]
    public void TheCountersStrip_KeepsItsHeight_AndFadesOutWithTheReadingView_OnThePaintedDesk()
    {
        string counter = Read("Assets/Scripts/Office/Desk/DeskCounter.cs");
        StringAssert.Contains("private const float ZoneLift = 0.0004f;", counter, "the strip's height at 32c54e8");
        StringAssert.Contains("tint.a *= 1f - DeskView.ReadingBlend;", counter, "faded by the reading view's share");
        StringAssert.Contains("DeskView.ReadingBlendChanged += HandleReadingBlend;", counter, "and it follows the share as it changes");
        string binder = Read("Assets/Scripts/Office/OfficeSceneBinder.cs");
        StringAssert.Contains("counter.Bind(_office.transform.forward);\n            counter.FadeInReading(_painted);", binder, "only on the painted desk");
        StringAssert.DoesNotContain("CounterStripLift", binder, "no sinking under the mat (it changed the office view)");
        StringAssert.Contains("ReadingBlendChanged?.Invoke(share);", Read("Assets/Scripts/Office/DeskView.cs"));
    }
}
