using UnityEngine;

/// <summary>
/// The case's end: the player's decision scored, the verdict slip and its
/// continuation, and the run's ending once reached. Carved out of
/// GameManager.cs by FLOW (pure move; FIX_PLAN_v2 FLOW delta, R7 PHASEB
/// LEASES.md). Phase B holders: TIMELINE-FAIL (if needed), then LANGUAGE.
/// </summary>
public sealed partial class GameManager
{
    /// <summary>
    /// Resolves the player's decision, one of the three verdicts committed at
    /// the desk (the hand-back, the DETAIN button; the one decision handler, audit R3-017; the desk
    /// machine spec §2): scores it, dispatches timeline impacts only on
    /// accept, checks for an ending, then shows the verdict and advances the day.
    /// </summary>
    private void HandleDecision(DeskStamp decision)
    {
        bool accepted = decision == DeskStamp.Approved;
        Debug.Log($"[GameManager] >>> Entering HandleDecision (slot {_activeCaseIndex1Based}, verdict={decision}).");
        // The booth has no traveller from here (the wheel cannot open); the figure stays for their reaction (R4). The papers are handed back: the guide hears it.
        SetTravellerAtDesk(false, keepFigure: true);
        if (guide != null)
            guide.CaseDecided();

        int idx = _activeCaseIndex1Based - 1;

        if (_dayCases == null || idx < 0 || idx >= _dayCases.Count)
        {
            if (travellerView != null)
                travellerView.Clear();
            Debug.LogError("Player decided but the active case index is invalid.");
            orchestrator.MarkCaseResolved();
            return;
        }

        CaseInstance inst = _dayCases[idx];

        // No tuning config: keep a simple correct/wrong readout.
        if (_gameConfig == null)
        {
            bool simpleCorrect = accepted == inst.ShouldAccept;
            if (officeUI != null)
                officeUI.SetResultText(UiText.Get(simpleCorrect ? "verdict.simpleCorrect" : "verdict.simpleWrong"));
            React(inst, accepted);
            Debug.Log($"[GameManager] <<< Exiting HandleDecision (no GameConfig, simpleCorrect={simpleCorrect}).");
            orchestrator.MarkCaseResolved();
            return;
        }

        float stabilityBefore = _worldState.timelineStability;
        int moneyBefore = _worldState.money;

        // Evidence documented in the scanner gates deny decisions (-1 = the
        // evidence system is not active in this scene, gate skipped).
        int evidenceCount = investigationUI != null && investigationUI.EvidenceSystemActive
            ? investigationUI.EvidenceCount
            : -1;
        if (_cheatEvidence && evidenceCount == 0)
            evidenceCount = 1; // the cheat menu's "decide correctly" denies with the evidence a denial needs

        // The verdict onto the ledger; the traveler is only dispatched (and the
        // timeline moved) when accepted: an accepted liar also carries their true
        // home's fact into the claim, and an accepted costume error causes a
        // panic there (tomorrow's news). DayCycle holds the step, so the balance
        // simulation plays the same one.
        CaseVerdict verdict = DayCycle.Decide(inst, decision, _activeCaseIndex1Based, evidenceCount, _worldState, _today, _ledger, contentLibrary, _gameConfig);
        if (accepted)
            AnnounceDeparture(inst);

        if (officeUI != null)
            officeUI.UpdateHud(_worldState);

        Debug.Log($"[Result] Case {_activeCaseIndex1Based}: verdict={decision}, shouldAccept={inst.ShouldAccept}, fault='{inst.FaultReason}', home='{inst.HomeLabel}', directive={inst.directiveFault}, correct={verdict.correct}, pay={verdict.payAwarded}, penalty={verdict.moneyPenalty}, money {moneyBefore}->{_worldState.money}, stability {stabilityBefore:0.00}->{_worldState.timelineStability:0.00}, firedNow={verdict.firedNow}.");

        // The reaction (the personalities spec's R1-R5): presentation only, after the scoring, never changing it.
        React(inst, accepted);
        Resolved?.Invoke(inst, verdict, accepted, stabilityBefore);

        EndingSO ending = EndingService.Evaluate(_worldState, contentLibrary, _gameConfig, EndingMoment.Immediate);

        if (ending != null)
        {
            // The run ends at the day's end, not now (BUG-56a; TIMELINE-FAIL's Q4 = A): the verdict shows, the shift closes, the report, then the ending.
            Debug.Log($"[GameManager] Ending check: matched '{ending.id}' ({ending.displayName}); the shift closes and the run ends at the day's end.");
            _worldState.endingId = ending.id;

            if (RunManager.HasInstance)
                RunManager.Instance.SaveNow();

            ShowVerdictThen(verdict, () => orchestrator.MarkCaseResolved());
            CloseNow(ending.id);
            return;
        }

        ShowVerdictThen(verdict, () => orchestrator.MarkCaseResolved());
    }

    /// <summary>
    /// Shows the verdict slip if a UI is wired (holding the shift clock while a
    /// citation slip is up: PauseState's Citation source, so no other pause's
    /// release restarts it; and holding the PC screen on so the slip can never
    /// sit on a dark screen), then runs the continuation (Saleh 2026-10-07,
    /// after the 1007b demo: "I like the violation slip better": the slip is
    /// back and the Citation paper on the desk is gone). Until then the
    /// verdict counts as showing: closing finishes this slot first (BUG-28b).
    /// </summary>
    private void ShowVerdictThen(CaseVerdict verdict, System.Action onContinue)
    {
        if (officeUI == null)
        {
            onContinue?.Invoke();
            return;
        }

        // A citation slip holds the day, the shift clock and the screen until acknowledged.
        bool citation = verdict != null && verdict.citationIssued;
        if (citation)
            PauseState.Shared.Request(PauseState.Citation, PauseScope.Clock);
        if (citation && booth != null)
            booth.SetCitationPending(true);
        _verdictShowing = true;

        officeUI.ShowVerdict(verdict, () =>
        {
            _verdictShowing = false;
            if (citation && booth != null)
                booth.SetCitationPending(false);
            if (citation)
                PauseState.Shared.Release(PauseState.Citation);
            if (citation)
                CitationAcknowledged?.Invoke(verdict);
            onContinue?.Invoke();
        });
    }

    /// <summary>
    /// Called after the shift report of a day that ended the run (a verdict's
    /// CloseNow, or the day's close reaching an ending). Loads the title scene,
    /// where the ending shows (WorldState.endingId is already set and saved;
    /// the evening edition first: EveningEditionView).
    /// </summary>
    private void HandleEndingReached()
    {
        Debug.Log($"[GameManager] >>> Entering HandleEndingReached (endingId='{_worldState.endingId}').");

        if (RunManager.HasInstance)
        {
            Debug.Log("[GameManager] <<< Exiting HandleEndingReached (loading title scene).");
            RunManager.Instance.LoadTitleScene();
        }
        else
        {
            Debug.LogError("[GameManager] Ending reached but no RunManager instance — cannot load title scene.");
        }
    }
}
