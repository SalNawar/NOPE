using UnityEngine;

/// <summary>
/// Creates the one persistent HoverHighlighter at startup, configured from
/// RunConfig.interactionFeedback, so every scene (Title, Office, Home) gets the
/// game cursor and hover outline without per-scene wiring, and the cursor does
/// not flip back to the OS arrow between scenes.
/// </summary>
public static class InteractionFeedbackBootstrap
{
    /// <summary>Name of the persistent host object.</summary>
    private const string HostName = "InteractionFeedback";

    /// <summary>Runs once after the first scene loads (play mode and builds).</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateHighlighter()
    {
        if (Object.FindFirstObjectByType<HoverHighlighter>() != null)
            return;

        var config = Resources.Load<RunConfigSO>(RunManager.ConfigResourcePath);
        InteractionFeedbackSO settings = config != null ? config.interactionFeedback : null;
        if (settings == null)
        {
            Debug.LogWarning("InteractionFeedbackBootstrap: RunConfig has no InteractionFeedbackSO, so there is no game cursor or hover outline. Run Tools > TimeDesk > Build Office UI (it creates and assigns one).");
            return;
        }

        // Configure before the component first enables, so it never runs unconfigured.
        var host = new GameObject(HostName);
        host.SetActive(false);
        host.AddComponent<HoverHighlighter>().Configure(settings);
        Object.DontDestroyOnLoad(host);
        host.SetActive(true);
    }
}
