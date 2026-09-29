using UnityEngine;

/// <summary>
/// Drives the title scene added in Alpha Phase 5. Reached either as the
/// game's entry point or after GameManager/HomeManager detect a game-over
/// ending (WorldState.endingId set + saved).
///
/// - If WorldState.endingId is set: shows the ending panel (display name +
///   body from the matching EndingSO) with a New Run option that clears the
///   save and starts fresh; on the Debt Relief ending (the bankrupt one), the
///   clerk's own Labour Contract beside the clerk's account, now Frozen.
/// - Otherwise: shows the title panel with Continue (only if a save exists;
///   it resumes where the save was made: Home after the end-of-shift save,
///   otherwise the Office) and New Run.
///
/// If TitleUIController has no panels wired, degrades straight to the office
/// scene so the loop stays playable before the title UI is built.
/// </summary>
public sealed class TitleSceneController : MonoBehaviour
{
    /// <summary>UI controller for the title panels (optional).</summary>
    [SerializeField] private TitleUIController titleUI;

    /// <summary>
    /// Acquires the run and shows the appropriate panel: an ended run's ending
    /// (never continued: without the ending panel the title offers New Run
    /// only, and without any title UI a new run starts; audit R3-005), else
    /// the title with Continue when a save can be continued.
    /// </summary>
    private void Start()
    {
        RunManager run = RunManager.GetOrCreate();

        if (run == null)
        {
            Debug.LogError("TitleSceneController could not acquire a RunManager (missing Resources/RunConfig).");
            return;
        }

        WorldState world = run.World;
        bool ended = !string.IsNullOrEmpty(world.endingId);

        if (ended && titleUI != null && titleUI.HasEndingPanel)
        {
            EndingSO ending = run.Library != null ? run.Library.GetEndingById(world.endingId) : null;
            titleUI.ShowEnding(ending, HandleNewRun);
            ShowClerkPapers(run, ending);
            return;
        }

        if (ended)
            Debug.LogWarning($"[TitleSceneController] The run ended ('{world.endingId}') but no ending panel is wired: it cannot be continued. Run Tools > TimeDesk > Build Title UI (Panels + Wiring) in the title scene.");

        if (titleUI != null && titleUI.HasTitlePanel)
        {
            bool hasSave = !ended && SaveSystem.HasSave();
            titleUI.ShowTitle(hasSave, HandleContinue, HandleNewRun);
            return;
        }

        if (ended)
        {
            HandleNewRun();
            return;
        }

        // No title UI wired yet: keep the loop playable.
        Debug.Log("[TitleSceneController] No title UI wired: resuming the run directly.");
        run.ResumeRun();
    }

    /// <summary>
    /// The Debt Relief ending (the one that freezes the clerk's account,
    /// ClerkDebt.Freezes; redesign phase 13, the traveller-types spec's D3)
    /// shows the clerk's own Labour Contract (TC-520: the debt still owed
    /// worked off at the contract's day wage) beside the clerk's account, now
    /// Frozen with the Debt Relief departure booked; every other ending hides them.
    /// </summary>
    private void ShowClerkPapers(RunManager run, EndingSO ending)
    {
        if (ending == null || !ClerkDebt.Freezes(ending.conditionType))
        {
            titleUI.ShowClerkPapers(null, null, null, null);
            return;
        }

        var clerk = new ClerkAccountSource(run.World, run.Library);
        ClerkContent profile = clerk.Profile;
        titleUI.ShowClerkPapers(
            UiText.Get("contract.title"), ClerkDebt.ContractRows(profile, clerk.Debt, UiText.Get, AccountWindow.Amount),
            UiText.Get("account.form.title") + "\n" + UiText.Format("account.query", profile.name, profile.citizenId),
            Account.ExtractRows(clerk, UiText.Get, AccountWindow.Amount));
    }

    /// <summary>Resumes the current (saved) run where the save was made (RunManager.ResumeRun).</summary>
    private void HandleContinue()
    {
        if (RunManager.HasInstance)
            RunManager.Instance.ResumeRun();
        else
            Debug.LogWarning("[TitleSceneController] Continue with no RunManager instance: nothing to resume.");
    }

    /// <summary>Clears the save, starts a fresh run, and heads to the office.</summary>
    private void HandleNewRun()
    {
        if (!RunManager.HasInstance)
        {
            Debug.LogWarning("[TitleSceneController] New Run with no RunManager instance: nothing to start.");
            return;
        }

        RunManager.Instance.NewRun();
        RunManager.Instance.LoadOfficeScene();
    }
}
