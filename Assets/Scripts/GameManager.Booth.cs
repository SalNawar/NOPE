using UnityEngine;

/// <summary>
/// The booth: the queue's slot arming, the AVAILABLE sign's wiring (the shift's
/// opening, the toggle, the desk's close), calling and presenting the traveller,
/// and their reaction and walk-off. Carved out of GameManager.cs by FLOW (pure
/// move; FIX_PLAN_v2 FLOW delta, R7 PHASEB LEASES.md). Phase B holder: BELL-SIGN.
/// </summary>
public sealed partial class GameManager
{
    /// <summary>
    /// Called when the orchestrator starts case slot #N (1-based).
    /// Displays the generated case in the UI.
    /// </summary>
    private void HandleCaseSlotStarted(int caseIndex1Based)
    {
        Debug.Log($"[GameManager] >>> Entering HandleCaseSlotStarted (slot {caseIndex1Based}/{_dayCases?.Count ?? 0}).");

        _activeCaseIndex1Based = caseIndex1Based;

        int idx = caseIndex1Based - 1;

        if (_dayCases == null || idx < 0 || idx >= _dayCases.Count)
        {
            Debug.LogError($"GameManager could not find case for slot {caseIndex1Based}.");
            orchestrator.MarkCaseResolved();
            return;
        }

        CaseInstance inst = _dayCases[idx];

        string claimedEraId = inst.claimedEra != null ? inst.claimedEra.id : string.Empty;
        string archetypeName = inst.archetype != null ? inst.archetype.displayName : string.Empty;
        string nationName = inst.claimedNation != null ? inst.claimedNation.displayName : string.Empty;
        Debug.Log($"[GameManager] Case {caseIndex1Based}: visitor='{inst.visitorDisplayName}', claim='{inst.originLabel}', claimedEra='{claimedEraId}', archetype='{archetypeName}', nation='{nationName}', legendary={inst.isLegendary}, liar={inst.IsLiar}, home='{inst.HomeLabel}', gender={inst.gender}, documents={inst.documents.Count}.");

        // Clear the previous case's verdict line before showing the new case.
        if (officeUI != null)
            officeUI.SetResultText(string.Empty);

        // The traveller waits in the queue: called at once while the desk is
        // available and free, else when the AVAILABLE sign is clicked (or the
        // last traveller has left). With no sign wired, show immediately.
        if (readySign != null)
            _desk.Arm();
        else
            ShowActiveCase(inst);

        Debug.Log($"[GameManager] <<< Exiting HandleCaseSlotStarted (slot {caseIndex1Based}, awaiting player decision).");
    }

    /// <summary>The shift opens (the AVAILABLE sign's first press, DeskAvailability.Opened; at once without a sign): the shift clock starts from the opening hour, and with it what it times (the hall's light, closing time, the last-hour alarm); the booth's shutter rolls up (the bible's G3).</summary>
    private void OpenShift()
    {
        if (shiftClock != null)
            shiftClock.StartShift();
        if (_shutter != null)
            _shutter.Open();
        Debug.Log($"[GameManager] Shift started at {ShiftClock.Format(shiftClock != null ? shiftClock.MinuteOfDay : 0f)} on AVAILABLE (day {_worldState.day}).");
    }

    /// <summary>The AVAILABLE sign's click: turns the desk available (the waiting traveller is called once the desk is free) or pauses it (the traveller at the desk is finished normally; the shift clock keeps running).</summary>
    private void ToggleAvailable()
    {
        _desk.Toggle();
        Debug.Log($"[GameManager] Desk {(_desk.IsAvailable ? "available" : "paused")} (waiting={_desk.IsWaiting}, departing={_desk.IsDeparting}, travellerAtDesk={_travellerAtDesk}).");
    }

    /// <summary>The shift is over (closing time or the day's end): nobody else is called and the AVAILABLE sign goes off and takes no clicks.</summary>
    private void CloseDesk()
    {
        _desk.Close();
        if (readySign != null)
            readySign.Interactable = false;
    }

    /// <summary>The desk calls the waiting traveller (DeskAvailability.Called): the PC frame closes, so the arrival is seen, and the active slot's case is shown.</summary>
    private void CallTraveller()
    {
        int idx = _activeCaseIndex1Based - 1;
        if (_dayCases == null || idx < 0 || idx >= _dayCases.Count)
            return;

        if (officeView != null)
            officeView.FocusOffice();
        ShowActiveCase(_dayCases[idx]);
    }

    /// <summary>
    /// The one presence transition: whether a traveller is at the desk (and
    /// how they look) feeds the closing-time rule, the booth figure and the
    /// booth's input phase.
    /// </summary>
    private void SetTravellerAtDesk(bool at, TravellerLook look = null, bool keepFigure = false)
    {
        _travellerAtDesk = at;

        if (travellerView != null)
        {
            if (at)
                travellerView.Show(look, _characterArt);
            else if (!keepFigure)
                travellerView.Clear();
        }

        if (booth != null)
            booth.SetPhase(at ? BoothPhase.TravellerAtDesk : BoothPhase.NoTraveller);
    }

    /// <summary>
    /// Presents a case via the investigation UI: keeps only this traveller's
    /// art and picks its set (CharacterArt.PresentTraveller: 80s or classic, never a
    /// mix), shows them in the booth, and marks a once-per-run premade as met
    /// (DayCycle.Present: they never come back this run). Without the
    /// investigation UI no case can be shown: an error, and the slot resolves.
    /// </summary>
    private void ShowActiveCase(CaseInstance inst)
    {
        // Calling the next traveller ends the last one's linger at once (R4).
        if (travellerView != null)
            travellerView.EndLinger();
        EndReaction();
        _characterArt?.PresentTraveller(inst.look);
        SetTravellerAtDesk(true, inst.look);

        DayCycle.Present(_worldState, inst);

        if (investigationUI == null)
        {
            Debug.LogError("[GameManager] No InvestigationUIController is wired, so no case can be shown. Run Tools > TimeDesk > Build Office UI.");
            SetTravellerAtDesk(false);
            orchestrator.MarkCaseResolved();
            return;
        }

        investigationUI.ShowCase(inst, contentLibrary, HandleDecision);
        if (guide != null)
            guide.CaseShown(inst);
        DecideOnArrivalIfCheated();
    }

    /// <summary>
    /// The decided traveller's reaction (the personalities spec's R1-R4): the
    /// investigation UI says it and returns the linger; the figure leaves
    /// after it (at once for 0), and their bubble hides as they go. The desk
    /// calls nobody new until they have left.
    /// </summary>
    private void React(CaseInstance inst, bool accepted)
    {
        float linger = investigationUI != null ? investigationUI.React(inst, accepted) : 0f;
        if (travellerView == null)
        {
            EndReaction();
            return;
        }

        _desk.SetDeparting(true);
        travellerView.Leave(linger, () =>
        {
            EndReaction();
            _desk.SetDeparting(false);
        });
    }

    private void EndReaction()
    {
        if (investigationUI != null)
            investigationUI.EndReaction();
    }
}
