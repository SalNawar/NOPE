using UnityEngine;

/// <summary>
/// Creates the one persistent CultureThemeService at startup (the
/// InteractionFeedbackBootstrap pattern: configured before it first enables),
/// from RunConfig's content library, and themes the first scene at once (its
/// sceneLoaded has already fired). Without generated themes it warns and the
/// UI keeps its built look.
/// </summary>
public static class CultureThemeBootstrap
{
    /// <summary>Name of the persistent host object.</summary>
    private const string HostName = "CultureTheme";

    /// <summary>Runs once after the first scene loads (play mode and builds).</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateService()
    {
        if (CultureThemeService.Instance != null)
            return;

        var config = Resources.Load<RunConfigSO>(RunManager.ConfigResourcePath);
        ContentLibrarySO library = config != null ? config.contentLibrary : null;
        if (library == null || library.NeutralTheme == null)
        {
            Debug.LogWarning("CultureThemeBootstrap: RunConfig's content library has no neutral theme, so the UI is not themed. Run Tools > TimeDesk > Generate World.");
            return;
        }

        var host = new GameObject(HostName);
        host.SetActive(false);
        host.AddComponent<CultureThemeService>().Configure(library);
        Object.DontDestroyOnLoad(host);
        host.SetActive(true);
        CultureThemeService.RefreshActive();
    }
}
