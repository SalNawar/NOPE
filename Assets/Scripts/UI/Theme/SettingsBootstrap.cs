using UnityEngine;

/// <summary>
/// Applies the player's stored Settings basics once at startup (BUG-62): the
/// volumes (SoundSettings) and the display (ScreenSettings), from Start, once
/// the mixer is ready to take its values; then it goes.
/// </summary>
public sealed class SettingsBootstrap : MonoBehaviour
{
    /// <summary>Makes the one-frame host after the first scene loads (play mode and builds).</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        var host = new GameObject("SettingsBootstrap");
        DontDestroyOnLoad(host);
        host.AddComponent<SettingsBootstrap>();
    }

    /// <summary>Applies the stored choices and removes the host.</summary>
    private void Start()
    {
        SoundSettings.Apply();
        ScreenSettings.Apply();
        Destroy(gameObject);
    }
}
