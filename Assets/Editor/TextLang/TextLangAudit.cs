using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// TEXT-LANG's font and language audit (FIX_PLAN TEXT-LANG verification;
/// QA-RIG's gate registers its checks from Checks/TextLangChecks.cs):
/// the scenes and prefabs that still set a text in LiberationSans (a scan of
/// their files: no editor needed), the kit faces' atlas coverage of today's
/// reading strings, and, in a play, the texts on screen in LiberationSans,
/// the player-language texts holding a culture label (the takeover grep,
/// BUG-3a) and the accented Greek capitals (BUG-38a). Tools &gt; TimeDesk &gt;
/// Text &gt; Audit Fonts logs the file and atlas checks.
/// </summary>
public static class TextLangAudit
{
    /// <summary>The scenes the game ships (Build Settings' scenes) and every prefab are scanned.</summary>
    private static readonly string[] ScannedExtensions = { ".unity", ".prefab" };

    /// <summary>A serialized text's font reference.</summary>
    private static readonly Regex FontRef = new Regex(@"m_fontAsset: \{fileID: \d+, guid: ([0-9a-f]{32})", RegexOptions.Compiled);

    /// <summary>The GUIDs of the project's LiberationSans font assets (TMP's default and its fallback), read from their .meta files.</summary>
    public static HashSet<string> LiberationSansGuids(string projectRoot)
    {
        var guids = new HashSet<string>();
        foreach (string meta in Directory.GetFiles(Path.Combine(projectRoot, "Assets"), "LiberationSans*.asset.meta", SearchOption.AllDirectories))
        {
            Match m = Regex.Match(File.ReadAllText(meta), @"guid: ([0-9a-f]{32})");
            if (m.Success)
                guids.Add(m.Groups[1].Value);
        }
        return guids;
    }

    /// <summary>Every scene and prefab under Assets that sets a text in LiberationSans, with how many ("Assets/Scenes/HomeScene.unity: 12"); BUG-38a's pass: none.</summary>
    public static List<string> LiberationSansInFiles(string projectRoot)
    {
        HashSet<string> liberation = LiberationSansGuids(projectRoot);
        var found = new List<string>();
        string assets = Path.Combine(projectRoot, "Assets");
        foreach (string file in Directory.GetFiles(assets, "*.*", SearchOption.AllDirectories).Where(f => ScannedExtensions.Contains(Path.GetExtension(f))))
        {
            int count = FontRef.Matches(File.ReadAllText(file)).Cast<Match>().Count(m => liberation.Contains(m.Groups[1].Value));
            if (count > 0)
                found.Add($"{file.Substring(projectRoot.Length).TrimStart('/', '\\').Replace('\\', '/')}: {count}");
        }
        return found;
    }

    /// <summary>Each kit face's missing characters for today's reading strings (the English table and the culture tables printed in Latin: German, Italian), "face: U+00C9 …"; empty when every face covers them.</summary>
    public static List<string> KitAtlasGaps(ContentLibrarySO library)
    {
        var text = new System.Text.StringBuilder();
        foreach (string language in new[] { library.CultureUi.readingLanguage, "de", "it" })
        {
            UiStringTableSO table = library.GetStringTable(language);
            if (table != null)
                foreach (UiStringEntry e in table.entries)
                    text.Append(Regex.Replace(e.text ?? string.Empty, @"</?[A-Za-z#][^<>]*>|\{[^{}]*\}", string.Empty));
        }
        var gaps = new List<string>();
        foreach (string path in KitFontAtlases.Faces)
        {
            string missing = KitFontAtlases.Missing(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path), text.ToString());
            if (missing.Length > 0)
                gaps.Add($"{Path.GetFileNameWithoutExtension(path)}: {missing}");
        }
        return gaps;
    }

    /// <summary>Logs the file scan and the kit atlases' gaps (Tools &gt; TimeDesk &gt; Text &gt; Audit Fonts).</summary>
    [MenuItem("Tools/TimeDesk/Text/Audit Fonts")]
    public static void AuditFonts()
    {
        string root = Path.GetDirectoryName(Application.dataPath);
        List<string> files = LiberationSansInFiles(root);
        Debug.Log($"[TextLang] {files.Count} scene/prefab file(s) set a text in LiberationSans{(files.Count > 0 ? ": " + string.Join(", ", files) : ".")}");
        var config = Resources.Load<RunConfigSO>(RunManager.ConfigResourcePath);
        if (config != null && config.contentLibrary != null)
        {
            List<string> gaps = KitAtlasGaps(config.contentLibrary);
            Debug.Log($"[TextLang] Kit atlas gaps for today's strings: {(gaps.Count == 0 ? "none" : string.Join(" | ", gaps))}");
        }
    }

    /// <summary>In a play: every active, non-empty text of the loaded scenes still in LiberationSans ("path: text"); BUG-38a's pass: none.</summary>
    public static List<string> LiberationSansOnScreen() =>
        ShownTexts().Where(t => SceneUiKit.IsDefaultFace(t)).Select(t => $"{PathOf(t.transform)}: {t.text}").ToList();

    /// <summary>
    /// In a play: the texts on a player-language surface (the chrome, the PC)
    /// that hold one of the culture labels shown so far
    /// (TextAudit.CultureOnPlayerSurfaces; the takeover grep, BUG-3a). A text
    /// on a paper and a text of the morning newspaper (FIX_PLAN_v2: "Paper:
    /// documents and the newspaper"; the Bureau memo on it is chrome) are
    /// Paper texts; a citation, Mail's copy or any other chrome text quoting a
    /// paper's word in the culture's language is a hit.
    /// </summary>
    public static List<string> CultureOnPlayerSurfaces()
    {
        CultureThemeService service = CultureThemeService.Instance;
        if (service == null || service.Strings == null)
            return new List<string>();
        IEnumerable<(string, string)> labels = service.Strings.Phrases.SelectMany(p => new[] { (p.Visual, p.English), (p.Logical, p.English) });
        List<(Transform paper, Transform memo)> newspapers = Newspapers();
        IEnumerable<(string, TextSurface)> shown = ShownTexts().Select(t => (t.text, SurfaceOf(t, newspapers)));
        return TextAudit.CultureOnPlayerSurfaces(shown, labels);
    }

    /// <summary>A shown text's surface for the takeover grep: a paper's (DeskDocument) or the newspaper's (outside its memo) is Paper, else UiText.SurfaceOf (the PC's layer or the chrome).</summary>
    private static TextSurface SurfaceOf(TMP_Text text, List<(Transform paper, Transform memo)> newspapers)
    {
        if (text.GetComponentInParent<DeskDocument>(true) != null)
            return TextSurface.Paper;
        Transform t = text.transform;
        if (newspapers.Any(n => n.paper != null && t.IsChildOf(n.paper) && (n.memo == null || !t.IsChildOf(n.memo))))
            return TextSurface.Paper;
        return UiText.SurfaceOf(text);
    }

    /// <summary>Every loaded morning newspaper (DayFlowUIController's briefing panel) with its Bureau memo, read through the controller's serialized fields (CONTENT's file, read only).</summary>
    private static List<(Transform paper, Transform memo)> Newspapers()
    {
        var found = new List<(Transform, Transform)>();
        foreach (DayFlowUIController flow in Object.FindObjectsByType<DayFlowUIController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var so = new SerializedObject(flow);
            var paper = so.FindProperty("briefingPanel")?.objectReferenceValue as GameObject;
            var memo = so.FindProperty("briefingMemo")?.objectReferenceValue as GameObject;
            if (paper != null)
                found.Add((paper.transform, memo != null ? memo.transform : null));
        }
        return found;
    }

    /// <summary>In a play: the texts drawn with an accented Greek capital (TMP's UpperCase applied on the text's own letters, or the letters themselves; BUG-38a's pass: none).</summary>
    public static List<string> AccentedGreekCapitalsOnScreen() =>
        ShownTexts().Where(t => TextAudit.AccentedGreekCapitals((t.fontStyle & FontStyles.UpperCase) != 0 ? t.text.ToUpperInvariant() : t.text).Count > 0)
                    .Select(t => $"{PathOf(t.transform)}: {t.text}").ToList();

    /// <summary>The active, non-empty texts of every loaded scene.</summary>
    private static IEnumerable<TMP_Text> ShownTexts()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded)
                continue;
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(false))
                    if (!string.IsNullOrWhiteSpace(text.text))
                        yield return text;
        }
    }

    /// <summary>A transform's path from its root ("Canvas/Panel/Label").</summary>
    private static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
}
