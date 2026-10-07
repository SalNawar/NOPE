using UnityEngine;

/// <summary>
/// Drives the title scene added in Alpha Phase 5. Reached either as the
/// game's entry point or after GameManager/HomeManager detect a game-over
/// ending (WorldState.endingId set + saved).
///
/// - If WorldState.endingId is set: shows the ending panel (display name +
///   body from the matching EndingSO) with a New Run option that clears the
///   save and starts fresh; on the Debt Relief ending (the bankrupt one), the
///   clerk's own Labour Contract beside the clerk's account, now Frozen. The
///   world's outcomes (the endings spec E0; ContentLibrarySO.WorldOutcomes)
///   and the END OF DEMO card: on the run's last day (EndingKind.Milestone)
///   the world panel shows instead, under the ending's name; after a failure
///   the ending panel's button opens it as "The world you leave behind".
/// - Otherwise: shows the title panel with Continue (only if a save exists;
///   it resumes where the save was made: Home after the end-of-shift save,
///   otherwise the Office) and New Run.
///
/// New Run (from the title or an ending) goes through the adoption panel
/// (the Home pet spec PS1: a dog or a cat, named), then the office; without
/// the panel the run keeps the run config's default pet.
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
            if (ShowLastDay(run, ending))
                return;

            string leftBehind = UiText.Get(WorldFactors.LeftBehindKey);
            PetContent words = run.Library != null ? run.Library.Home.pet : new PetContent();
            titleUI.ShowEnding(ending, HandleNewRun, leftBehind, titleUI.HasWorldPanel && run.Library != null ? () => ShowWorld(run, leftBehind) : (System.Action)null,
                               text => words.Fill(text, world.pet));
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
    /// The run's last day (EndingKind.Milestone: the "world you made" ending):
    /// the world panel under the ending's name instead of the ending panel.
    /// False for any other ending, or without a world panel.
    /// </summary>
    private bool ShowLastDay(RunManager run, EndingSO ending)
    {
        if (ending == null || EndingRules.KindOf(ending.conditionType) != EndingKind.Milestone || !titleUI.HasWorldPanel || run.Library == null)
            return false;
        ShowWorld(run, ending.displayName);
        return true;
    }

    /// <summary>The world panel under <paramref name="heading"/>: the world's outcomes as the run left them and the END OF DEMO card (the run's last day's closing card).</summary>
    private void ShowWorld(RunManager run, string heading)
    {
        EndingSO lastDay = run.Library.LastDayEnding;
        titleUI.ShowWorld(heading, run.Library.WorldOutcomes(run.World, run.Config != null ? run.Config.gameConfig : null), lastDay != null ? lastDay.closingCard : null, HandleNewRun);
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

    /// <summary>New Run: the adoption panel when it is wired (Back returns to what the Title showed), else a fresh run with the default pet; then the office.</summary>
    private void HandleNewRun()
    {
        if (!RunManager.HasInstance)
        {
            Debug.LogWarning("[TitleSceneController] New Run with no RunManager instance: nothing to start.");
            return;
        }

        RunManager run = RunManager.Instance;
        if (titleUI != null && titleUI.HasAdoptPanel)
        {
            titleUI.ShowAdopt(run.Library != null ? run.Library.Home.pet : null, HandleAdopt, Start);
            return;
        }

        run.NewRun();
        run.LoadOfficeScene();
    }

    /// <summary>Adopt clicked with a sound name: clears the save, starts a fresh run with that pet, and heads to the office.</summary>
    private void HandleAdopt(PetKind kind, string petName, string coat)
    {
        RunManager.Instance.NewRun(kind, petName, coat);
        RunManager.Instance.LoadOfficeScene();
    }
}
