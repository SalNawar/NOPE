using UnityEditor;
using UnityEngine;

/// <summary>
/// The builders' shared game-feel parts (Saleh 2026-10-07: "every single
/// button, every single action to feel this satisfying"): the motion tuning
/// and the UI sounds assets (MotionTuning_Default, UiSounds_Default under
/// Assets/Data/Config: created once with the defaults, never overwritten,
/// assigned to RunConfig so UiMotion and UiSounds find them in every scene;
/// SceneUiKit.Skin and Build Office UI's interaction feedback ensure them).
/// </summary>
internal static class MotionAuthoring
{
    /// <summary>The motion tuning asset.</summary>
    public const string TuningPath = "Assets/Data/Config/MotionTuning_Default.asset";

    /// <summary>The UI sounds asset.</summary>
    public const string SoundsPath = "Assets/Data/Config/UiSounds_Default.asset";

    /// <summary>The motion tuning and the UI sounds (one slot per cue, no clips: silent), created with the defaults when missing (Inspector knobs: never overwritten) and assigned to RunConfig.</summary>
    public static void EnsureAssets()
    {
        MotionTuningSO tuning = AssetDatabase.LoadAssetAtPath<MotionTuningSO>(TuningPath);
        if (tuning == null)
        {
            PlaceholderPng.EnsureFolderTree("Assets/Data/Config");
            tuning = ScriptableObject.CreateInstance<MotionTuningSO>();
            AssetDatabase.CreateAsset(tuning, TuningPath);
        }
        UiSoundSO sounds = AssetDatabase.LoadAssetAtPath<UiSoundSO>(SoundsPath);
        if (sounds == null)
        {
            PlaceholderPng.EnsureFolderTree("Assets/Data/Config");
            sounds = ScriptableObject.CreateInstance<UiSoundSO>();
            var cues = (UiSoundCue[])System.Enum.GetValues(typeof(UiSoundCue));
            sounds.entries = new UiSoundSO.Entry[cues.Length];
            for (int i = 0; i < cues.Length; i++)
                sounds.entries[i] = new UiSoundSO.Entry { cue = cues[i] };
            AssetDatabase.CreateAsset(sounds, SoundsPath);
        }
        var config = Resources.Load<RunConfigSO>(RunManager.ConfigResourcePath);
        if (config == null)
        {
            Debug.LogWarning("[TimeDesk] No RunConfig asset, so the game feel uses its default springs and no sounds. Create Assets/Resources/RunConfig.asset and rebuild.");
            return;
        }
        if (config.motionTuning == tuning && config.uiSounds == sounds)
            return;
        Undo.RecordObject(config, "Game feel assets");
        config.motionTuning = tuning;
        config.uiSounds = sounds;
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
    }
}
