using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The office is two scenes: the art office (RunConfig.officeSceneName, owned
/// by the art side, the active scene for its lighting) and the gameplay layer
/// on top (RunConfig.officeGameplaySceneName, built by Build Office UI). Loading
/// the art office by any path (RunManager, or pressing Play on it in the
/// editor) loads the gameplay layer additively; the art scene's leftover
/// gameplay objects (the contract's legacy roots) are switched off the moment
/// it loads, after their Awake and before any Start. Pressing Play on the
/// gameplay layer alone loads the art office under it and makes it active.
/// </summary>
public static class OfficeScenes
{
    private static RunConfigSO _config;
    private static bool _gameplayRequested;
    private static bool _artRequested;

    /// <summary>The run config (Resources), loaded once.</summary>
    private static RunConfigSO Config
    {
        get
        {
            if (_config == null)
                _config = Resources.Load<RunConfigSO>(RunManager.ConfigResourcePath);
            return _config;
        }
    }

    /// <summary>The art office's scene name.</summary>
    public static string ArtSceneName => Config != null ? Config.officeSceneName : "OfficeScene";

    /// <summary>The gameplay layer's scene name.</summary>
    public static string GameplaySceneName => Config != null ? Config.officeGameplaySceneName : "OfficeGameplay";

    /// <summary>The office scene contract (null when RunConfig has none).</summary>
    public static OfficeSceneContractSO Contract => Config != null ? Config.officeContract : null;

    /// <summary>True for the art office: our components living there are its leftover copies and must do nothing.</summary>
    public static bool IsArtOffice(Scene scene) => scene.IsValid() && scene.name == ArtSceneName;

    /// <summary>The loaded art office (invalid when it is not loaded).</summary>
    public static Scene ArtScene
    {
        get
        {
            Scene scene = SceneManager.GetSceneByName(ArtSceneName);
            return scene.IsValid() && scene.isLoaded ? scene : default;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Hook()
    {
        _gameplayRequested = false;
        _artRequested = false;
        SceneManager.sceneLoaded -= HandleLoaded;
        SceneManager.sceneLoaded += HandleLoaded;
    }

    private static void HandleLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == ArtSceneName)
        {
            _artRequested = false;
            SwitchOffLegacy(scene);
            if (mode == LoadSceneMode.Additive)
                SceneManager.SetActiveScene(scene);

            Scene gameplay = SceneManager.GetSceneByName(GameplaySceneName);
            if (!_gameplayRequested && !(gameplay.IsValid() && gameplay.isLoaded))
            {
                _gameplayRequested = true;
                SceneManager.LoadScene(GameplaySceneName, LoadSceneMode.Additive);
            }
        }
        else if (scene.name == GameplaySceneName)
        {
            _gameplayRequested = false;
            if (!_artRequested && !ArtScene.IsValid())
            {
                Debug.LogWarning($"[OfficeScenes] '{GameplaySceneName}' was loaded without the art office; loading '{ArtSceneName}' under it. Open the art office to play (it loads the gameplay layer itself).");
                _artRequested = true;
                SceneManager.LoadScene(ArtSceneName, LoadSceneMode.Additive);
            }
        }
    }

    /// <summary>Switches the art office's leftover gameplay objects off (the contract's legacy roots); a missing one is skipped.</summary>
    public static void SwitchOffLegacy(Scene art)
    {
        OfficeSceneContractSO contract = Contract;
        if (contract == null || contract.legacyRoots == null)
        {
            Debug.LogWarning("[OfficeScenes] RunConfig has no office scene contract, so the art office's leftover gameplay objects stay on. Run Tools > TimeDesk > Build Office UI.");
            return;
        }

        foreach (string path in contract.legacyRoots)
        {
            Transform legacy = OfficeAnchors.Find(art, path, includeInactive: true);
            if (legacy != null)
                legacy.gameObject.SetActive(false);
        }
    }
}
