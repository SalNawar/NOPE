using UnityEngine;

/// <summary>
/// Drives the title scene added in Alpha Phase 5. Reached either as the
/// game's entry point or after GameManager/HomeManager detect a game-over
/// ending (WorldState.endingId set + saved).
///
/// - If WorldState.endingId is set: shows the ending panel (display name +
///   body from the matching EndingSO) with a New Run option that clears the
///   save and starts fresh.
/// - Otherwise: shows the title panel with Continue (only if a save exists)
///   and New Run.
///
/// If TitleUIController has no panels wired, degrades straight to the office
/// scene so the loop stays playable before the title UI is built.
/// </summary>
public sealed class TitleSceneController : MonoBehaviour
{
    /// <summary>UI controller for the title panels (optional).</summary>
    [SerializeField] private TitleUIController titleUI;

    /// <summary>Acquires the run and shows the appropriate panel.</summary>
    private void Start()
    {
        Debug.Log("[TitleSceneController] >>> Entering Start.");

        RunManager run = RunManager.GetOrCreate();

        if (run == null)
        {
            Debug.LogError("TitleSceneController could not acquire a RunManager (missing Resources/RunConfig).");
            return;
        }

        WorldState world = run.World;

        if (!string.IsNullOrEmpty(world.endingId) && titleUI != null && titleUI.HasEndingPanel)
        {
            EndingSO ending = run.Library != null ? run.Library.GetEndingById(world.endingId) : null;
            Debug.Log($"[TitleSceneController] <<< Exiting Start (showing ending panel for '{world.endingId}', resolved={ending != null}).");
            titleUI.ShowEnding(ending, HandleNewRun);
            return;
        }

        if (titleUI != null && titleUI.HasTitlePanel)
        {
            bool hasSave = SaveSystem.HasSave();
            Debug.Log($"[TitleSceneController] <<< Exiting Start (showing title panel, hasSave={hasSave}).");
            titleUI.ShowTitle(hasSave, HandleContinue, HandleNewRun);
            return;
        }

        // No title UI wired yet: keep the loop playable.
        Debug.Log("[TitleSceneController] <<< Exiting Start (no title UI wired, loading Office directly).");
        run.LoadOfficeScene();
    }

    /// <summary>Resumes the current (saved) run.</summary>
    private void HandleContinue()
    {
        Debug.Log("[TitleSceneController] >>> Entering HandleContinue.");

        if (RunManager.HasInstance)
        {
            Debug.Log("[TitleSceneController] <<< Exiting HandleContinue (loading Office).");
            RunManager.Instance.LoadOfficeScene();
        }
        else
        {
            Debug.LogWarning("[TitleSceneController] <<< Exiting HandleContinue — no RunManager instance.");
        }
    }

    /// <summary>Clears the save, starts a fresh run, and heads to the office.</summary>
    private void HandleNewRun()
    {
        Debug.Log("[TitleSceneController] >>> Entering HandleNewRun.");

        if (!RunManager.HasInstance)
        {
            Debug.LogWarning("[TitleSceneController] <<< Exiting HandleNewRun — no RunManager instance.");
            return;
        }

        RunManager.Instance.NewRun();

        Debug.Log("[TitleSceneController] <<< Exiting HandleNewRun (new run started, loading Office).");

        RunManager.Instance.LoadOfficeScene();
    }
}
