using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// TEXT-LANG's Editor-side tests (Assembly-CSharp-Editor; Unity's EditMode
/// suite): the font sweep (BUG-38a), the kit atlases' charset, the scenes'
/// zero-LiberationSans rule after Build Office UI, the Settings basics' rows
/// (BUG-62) and the chrome's language (BUG-3a). Tests in the "NeedsEditor"
/// category create Unity objects, load assets or read a rebuilt scene, and
/// run only inside Unity (the Unity pass); the others run anywhere.
/// </summary>
public class TextLangEditorTests
{
    [Test]
    public void KitCharset_HoldsLatin1_LatinExtendedA_AndGreek()
    {
        string charset = KitFontAtlases.Charset();
        foreach (char c in "ÉéÀçñÄöüßŁłŒœŠšŽžΑΩάώΪϋ")
            StringAssert.Contains(c.ToString(), charset);
        Assert.IsFalse(charset.Contains('·') || charset.Contains('΋') || charset.Contains('΢'), "no unassigned or non-letter Greek code");
        Assert.AreEqual(charset.Length, charset.Distinct().Count(), "each character once");
    }

    [Test, Category("NeedsEditor")]
    public void Refont_SetsADefaultFaceText_InTheKitsBodyFace_AndLeavesTheOthers()
    {
        Assert.IsNotNull(SceneUiKit.BodyFace, "the kit's body face (UiKitFonts.Body)");
        var root = new GameObject("RefontRoot");
        try
        {
            TextMeshProUGUI plain = new GameObject("Plain", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            plain.transform.SetParent(root.transform);
            plain.font = TMP_Settings.defaultFontAsset;
            TextMeshProUGUI bold = new GameObject("Bold", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            bold.transform.SetParent(root.transform);
            bold.font = TMP_Settings.defaultFontAsset;
            bold.fontStyle = FontStyles.Bold;
            TextMeshProUGUI kit = new GameObject("Kit", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            kit.transform.SetParent(root.transform);
            TMP_FontAsset label = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiKitFonts.Label);
            kit.font = label;

            Assert.IsTrue(SceneUiKit.IsDefaultFace(plain));
            Assert.AreEqual(2, SceneUiKit.Refont(root.transform));
            Assert.AreEqual(SceneUiKit.BodyFace, plain.font);
            Assert.AreEqual(SceneUiKit.BodyBoldFace, bold.font);
            Assert.AreEqual(label, kit.font, "a kit face is kept");
            Assert.IsFalse(SceneUiKit.IsDefaultFace(plain));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    /// <summary>After the Unity pass's Extend Kit Font Atlases: every kit face draws today's English, German and Italian strings itself (no fallback mid-word: "VISAS D'ENTRÉE" in one face).</summary>
    [Test, Category("NeedsEditor")]
    public void KitFaces_DrawTodaysLatinStrings_Themselves()
    {
        var config = Resources.Load<RunConfigSO>(RunManager.ConfigResourcePath);
        Assert.IsNotNull(config != null ? config.contentLibrary : null, "RunConfig's content library");
        List<string> gaps = TextLangAudit.KitAtlasGaps(config.contentLibrary);
        CollectionAssert.IsEmpty(gaps, string.Join(" | ", gaps));
        foreach (string path in KitFontAtlases.Faces)
            Assert.AreEqual(string.Empty, KitFontAtlases.Missing(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path), "VISAS D'ENTRÉE"), path);
    }

    /// <summary>After the Unity pass's Build Office UI: OfficeGameplay sets no text in LiberationSans (the builder's Text(), SceneUiKit and the kit pass's sweep).</summary>
    [Test, Category("NeedsEditor")]
    public void OfficeGameplay_SetsNoTextInLiberationSans()
    {
        List<string> files = TextLangAudit.LiberationSansInFiles(Path.GetDirectoryName(Application.dataPath));
        Assert.IsFalse(files.Any(f => f.StartsWith("Assets/Scenes/OfficeGameplay.unity")), string.Join(", ", files));
    }

    /// <summary>The chrome reads the reading table whatever the surface asks for while no culture applies; a paper falls back to it too (BUG-3a).</summary>
    [Test, Category("NeedsEditor")]
    public void UiText_TheChromeAndThePc_ReadTheReadingTable()
    {
        Assert.AreEqual("Language", UiText.Get("settings.language"));
        Assert.AreEqual(UiText.Get("settings.language"), UiText.Pc.Get("settings.language"));
        Assert.AreEqual(UiText.Get("settings.language"), UiText.Get("settings.language", TextSurface.Desk));
        Assert.AreEqual("Traveller", UiText.Get("case.roleUnknown"), "one spelling (BUG-43b)");
    }

    /// <summary>The Settings basics' builder (the seam UI-PC calls) builds the Sound and Display rows and wires every control to the window's controller (BUG-62).</summary>
    [Test, Category("NeedsEditor")]
    public void SettingsBasics_BuildTheRows_AndWireTheController()
    {
        var window = new GameObject("SettingsWindow", typeof(RectTransform));
        try
        {
            var column = new GameObject("Column", typeof(RectTransform), typeof(VerticalLayoutGroup)).transform;
            column.SetParent(window.transform);
            typeof(OfficeSceneUIBuilder).GetMethod("BuildSettingsBasics", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { column });

            var so = new SerializedObject(window.GetComponent<SettingsWindowController>());
            Assert.AreEqual(3, so.FindProperty("volumeSliders").arraySize, "Master, Music, Effects");
            Assert.AreEqual(3, so.FindProperty("windowModeButtons").arraySize, "Windowed, Borderless, Fullscreen");
            foreach (string field in new[] { "resolutionDownButton", "resolutionUpButton", "resolutionText", "vsyncOffButton", "vsyncOnButton", "brightnessSlider", "brightnessText", "reduceFlashingOffButton", "reduceFlashingOnButton" })
                Assert.IsNotNull(so.FindProperty(field).objectReferenceValue, field);
            Assert.IsNotNull(column.Find("SoundLabel"));
            Assert.IsNotNull(column.Find("DisplayLabel"));
            Assert.IsFalse(column.GetComponentsInChildren<TMP_Text>(true).Any(SceneUiKit.IsDefaultFace), "every new text in a kit face");
        }
        finally
        {
            Object.DestroyImmediate(window);
        }
    }
}
