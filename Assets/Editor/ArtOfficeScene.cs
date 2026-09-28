using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// The art office's scene asset for the editor tools, as the knob names it:
/// RunConfig.officeSceneName (docs/SCENE_CONTRACT_GAMEPLAY.md). The scene may
/// live anywhere under Assets (Assets/Scenes/OfficeScene.unity, the art side's
/// Assets/Art/Office/AnimeHallLayers/AnimeHall.unity); it is found by its file
/// name. Build Office UI keeps it enabled in the build list right after the
/// title, and Check Office Scene Contract and Add Gameplay Anchors open it.
/// </summary>
public static class ArtOfficeScene
{
    /// <summary>The art office before the knob existed, used when no RunConfig names one.</summary>
    private const string DefaultPath = "Assets/Scenes/OfficeScene.unity";

    /// <summary>The art office's scene name (RunConfig.officeSceneName, or OfficeScene without a RunConfig).</summary>
    public static string Name
    {
        get
        {
            var config = Resources.Load<RunConfigSO>(RunManager.ConfigResourcePath);
            return config != null && !string.IsNullOrWhiteSpace(config.officeSceneName) ? config.officeSceneName.Trim() : "OfficeScene";
        }
    }

    /// <summary>The scene asset's path for <see cref="Name"/>: the scene file of that name under Assets (one under Assets/Scenes first); the old art office, with an error logged, when none exists.</summary>
    public static string Path
    {
        get
        {
            string name = Name;
            string found = null;
            foreach (string guid in AssetDatabase.FindAssets($"t:Scene {name}"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) != name)
                    continue;
                if (path.StartsWith("Assets/Scenes/"))
                    return path;
                found ??= path;
            }

            if (found != null)
                return found;
            Debug.LogError($"[TimeDesk] RunConfig.officeSceneName names '{name}', but no scene of that name exists under Assets: the tools use {DefaultPath}. Fix the knob in Assets/Resources/RunConfig.asset.");
            return DefaultPath;
        }
    }
}
