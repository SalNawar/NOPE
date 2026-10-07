using UnityEditor;
using UnityEngine;

/// <summary>
/// The builders' shared game-feel part (Saleh 2026-10-07: "every single
/// button, every single action to feel this satisfying"): the motion tuning
/// asset (MotionTuning_Default under Assets/Data/Config: created once with
/// the defaults, never overwritten, its breathing noise Cinemachine's mild
/// handheld profile when unset, assigned to RunConfig so UiMotion finds it in
/// every scene; SceneUiKit.Skin and Build Office UI's interaction feedback
/// ensure it). The sound bank is the sound list importer's (SoundBankImporter).
/// </summary>
internal static class MotionAuthoring
{
    /// <summary>The motion tuning asset.</summary>
    public const string TuningPath = "Assets/Data/Config/MotionTuning_Default.asset";

    /// <summary>The desk camera's idle breathing: Cinemachine's mildest handheld noise profile (MotionTuningSO's breathingNoise when unset).</summary>
    public const string BreathingNoisePath = "Packages/com.unity.cinemachine/Presets/Noise/Handheld_normal_mild.asset";

    /// <summary>The motion tuning, created with the defaults when missing (an Inspector knob: never overwritten) and assigned to RunConfig.</summary>
    public static void EnsureAssets()
    {
        MotionTuningSO tuning = AssetDatabase.LoadAssetAtPath<MotionTuningSO>(TuningPath);
        if (tuning == null)
        {
            PlaceholderPng.EnsureFolderTree("Assets/Data/Config");
            tuning = ScriptableObject.CreateInstance<MotionTuningSO>();
            AssetDatabase.CreateAsset(tuning, TuningPath);
        }
        var config = Resources.Load<RunConfigSO>(RunManager.ConfigResourcePath);
        if (config == null)
        {
            Debug.LogWarning("[TimeDesk] No RunConfig asset, so the game feel uses its default springs. Create Assets/Resources/RunConfig.asset and rebuild.");
            return;
        }
        if (tuning.breathingNoise == null)
        {
            tuning.breathingNoise = AssetDatabase.LoadAssetAtPath<Unity.Cinemachine.NoiseSettings>(BreathingNoisePath);
            EditorUtility.SetDirty(tuning);
        }
        if (config.motionTuning == tuning)
            return;
        Undo.RecordObject(config, "Game feel assets");
        config.motionTuning = tuning;
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
    }
}
