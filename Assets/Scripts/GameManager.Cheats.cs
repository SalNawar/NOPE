using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// The cheat menu's way into today's shift (Saleh 2026-10-06: "I want cheats
/// to test easy"; DevCheats runs them, the overlay DebugPanelController
/// draws them): decide the traveller correctly (calling the waiting one
/// first), decide every traveller as they arrive (DevToolsState.AutoDecide),
/// end the shift now, reveal the traveller's faults, skip or replay the desk
/// tutorial. Each starts what the game does anyway (the stamps' decision
/// through InvestigationUIController.Decide, closing time, a found
/// difference's mark, the guide's Skip and Replay), so the ledger, the
/// verdict line, the reaction and the shift report follow as in play. Each
/// returns a line for the overlay. RunManager's skips and jumps close the
/// live shift here (CloseShiftForCheat: play's close on the shift's own
/// ledger; RUN-SAFE, BUG-28a). End Shift never draws the report over an
/// open citation slip, and a run that has ended goes to its ending, never
/// Home (BUG-28b, the cheat side; FLOW makes CloseNow honour it in play).
/// </summary>
public sealed partial class GameManager
{
    /// <summary>How long a traveller decided by the cheat menu stands at the desk first (real seconds): long enough to be seen arriving.</summary>
    private const float CheatDecideDelay = 0.3f;

    /// <summary>The office's running GameManager: the one running today's shift (null outside the office, and for the art office's idle copy). The cheat menu reads it.</summary>
    public static GameManager Current { get; private set; }

    /// <summary>True while a cheat's decision is scored: a correct denial counts the evidence a denial needs (HandleDecision).</summary>
    private bool _cheatEvidence;

    /// <summary>True when the next traveller called is decided on arrival (a "decide correctly" with nobody at the desk yet).</summary>
    private bool _cheatDecideNext;

    /// <summary>True from the shift's start (after the briefing) until its end: closing time can be called.</summary>
    private bool _shiftRunning;

    /// <summary>True once this office listens to its own verdicts and slips for the cheat menu (EnsureCheatHooks).</summary>
    private bool _cheatHooked;

    /// <summary>True while a citation slip waits for Acknowledge (a verdict with a citation was resolved and not yet acknowledged).</summary>
    private bool _slipOpen;

    /// <summary>True when the cheat menu's End Shift waits for the open slip to be acknowledged.</summary>
    private bool _endShiftAfterSlip;

    /// <summary>
    /// The cheat menu's "Decide correctly": the traveller at the desk is
    /// decided as they should be (approved without a fault, denied with one,
    /// with the evidence a denial needs), as if the stamped passport had been
    /// handed back; with nobody at the desk the AVAILABLE sign turns on and
    /// the waiting traveller is decided as they arrive.
    /// </summary>
    public string CheatDecideCorrectly()
    {
        CaseInstance inst = ActiveCase;
        if (!_shiftRunning || inst == null || investigationUI == null)
            return "No traveller to decide (the shift has not started, or it is over).";
        if (_travellerAtDesk)
            return CheatDecide(inst);
        _cheatDecideNext = true;
        if (!_desk.IsAvailable)
            ToggleAvailable();
        return $"{inst.visitorDisplayName} is called and decided on arrival.";
    }

    /// <summary>The cheat menu's "Auto-decide": on, every traveller is decided correctly as they arrive and the AVAILABLE sign stays on (the one at the desk now too); off, play goes on by hand.</summary>
    public string CheatSetAutoDecide(bool on)
    {
        DevToolsState.AutoDecide = on;
        if (!on)
            return "Auto-decide off.";
        StartAutoDecide();
        CaseInstance inst = ActiveCase;
        if (_travellerAtDesk && inst != null && investigationUI != null)
            CheatDecide(inst);
        return "Auto-decide on: every traveller is decided correctly as they arrive.";
    }

    /// <summary>
    /// The cheat menu's "End shift": closing time now (the clock stops; nobody
    /// else is called; a traveller at the desk is still finished, then the
    /// shift report). A run that has ended (an ending set: fired, bankrupt)
    /// goes to its ending, never Home: after the open slip's Acknowledge (its
    /// own way there), else at once. With a citation slip open, closing time
    /// waits for its Acknowledge, so the report is never drawn over the slip
    /// (BUG-28b).
    /// </summary>
    public string CheatEndShift()
    {
        if (_worldState != null && !string.IsNullOrEmpty(_worldState.endingId))
        {
            if (_slipOpen)
                return $"The run has ended ('{_worldState.endingId}'): acknowledge the slip and the ending follows.";
            _shiftRunning = false;
            HandleEndingReached();
            return $"The run has ended ('{_worldState.endingId}'): the ending shows.";
        }
        if (!_shiftRunning || _desk.IsClosed)
            return "No shift to end (it has not started, or it is over).";
        if (_slipOpen)
        {
            _endShiftAfterSlip = true;
            return "Closing time once the citation slip is acknowledged.";
        }
        if (shiftClock != null)
            shiftClock.StopShift();
        bool atDesk = _travellerAtDesk;
        HandleShiftClosed();
        return atDesk ? "Closing time: decide the traveller at the desk to end the shift." : "Closing time: the shift ends.";
    }

    /// <summary>
    /// A skip or a jump closes today's live shift (RunManager, RUN-SAFE BUG-28a):
    /// the booth shuts (the clock stops, nobody else is called, an open FTUE
    /// is over), then play's close on the shift's own ledger (DayCycle.EndShift:
    /// strandings, the instalment, the dialogs' outcomes, the statement's row,
    /// Home as the resume point, the tomorrow modifiers reset). True when the
    /// close moved the wallet or stability (RunManager checks the endings).
    /// </summary>
    public bool CloseShiftForCheat()
    {
        _travellerAtDesk = false;
        _shiftRunning = false;
        _endShiftAfterSlip = false;
        if (guide != null)
            guide.EndShift();
        if (shiftClock != null)
            shiftClock.StopShift();
        CloseDesk();
        return DayCycle.EndShift(_worldState, _ledger ?? new ShiftLedger(), (IReadOnlyList<CaseInstance>)_dayCases ?? System.Array.Empty<CaseInstance>(), _today, contentLibrary, _gameConfig);
    }

    /// <summary>
    /// The cheat menu's "Reveal faults": what is wrong with the traveller
    /// (the verdict they should get, the fault, the rule and the values a
    /// citation would name), and their papers' boxes that show it marked on
    /// the desk as a found difference is (FaultFields.Of; papers handed over
    /// only).
    /// </summary>
    public string CheatRevealFaults()
    {
        CaseInstance inst = ActiveCase;
        if (inst == null)
            return "No traveller.";
        if (inst.ShouldAccept)
            return $"{inst.visitorDisplayName}: APPROVE (no fault).";
        List<(int document, int field)> fields = FaultFields.Of(inst.documents.Select(d => d != null ? (IReadOnlyList<DocumentField>)d.fields : null).ToList(),
                                                                inst.recordTells, inst.citation != null ? inst.citation.Values : null);
        if (_travellerAtDesk && investigationUI != null)
            investigationUI.RevealFaults(fields);
        string rule = Citations.RuleLine(inst.citation, UiText.Get, UiText.Get("citation.rule.numbered"));
        string values = inst.citation != null ? string.Join("; ", inst.citation.Values.Select(v => $"{v.Label}: {v.Value}")) : string.Empty;
        return $"{inst.visitorDisplayName}: DENY ({inst.FaultReason}). {rule} {values} [{fields.Count} box(es) marked]".Trim();
    }

    /// <summary>The cheat menu's "Skip tutorial" (<paramref name="replay"/> false: an open FTUE is over) and "Replay tutorial" (the FTUE from its first step, as Settings' button does).</summary>
    public string CheatTutorial(bool replay)
    {
        if (guide == null)
            return "No desk guide in this office.";
        if (replay)
            guide.Replay();
        else
            guide.Skip();
        return replay ? "The desk tutorial starts over." : "The desk tutorial is skipped.";
    }

    /// <summary>The HUD shows the wallet and stability a cheat just changed.</summary>
    public void CheatRefreshHud()
    {
        if (officeUI != null && _worldState != null)
            officeUI.UpdateHud(_worldState);
    }

    /// <summary>A traveller just called: decided at once by the cheat menu when "decide correctly" waits for them or "Auto-decide" is on (ShowActiveCase).</summary>
    private void DecideOnArrivalIfCheated()
    {
        if (!_cheatDecideNext && !DevToolsState.AutoDecide)
            return;
        _cheatDecideNext = false;
        StartCoroutine(DecideSoon(ActiveCase));
    }

    /// <summary>The shift starts with "Auto-decide" on (BeginShift): the AVAILABLE sign turns on, so the queue keeps coming. The cheat menu's hooks start listening here, before any verdict.</summary>
    private void StartAutoDecide()
    {
        EnsureCheatHooks();
        if (DevToolsState.AutoDecide && _shiftRunning && !_desk.IsAvailable && !_desk.IsClosed)
            ToggleAvailable();
    }

    /// <summary>Listens (once) to this office's verdicts and slips, so End Shift knows whether a citation slip is open.</summary>
    private void EnsureCheatHooks()
    {
        if (_cheatHooked)
            return;
        _cheatHooked = true;
        Resolved += NoteSlip;
        CitationAcknowledged += SlipAcknowledged;
    }

    /// <summary>A verdict is resolved: a citation's slip opens.</summary>
    private void NoteSlip(CaseInstance inst, CaseVerdict verdict, bool accepted, float stabilityBefore)
    {
        if (verdict != null && verdict.citationIssued)
            _slipOpen = true;
    }

    /// <summary>The slip is acknowledged: an End Shift waiting for it runs on the next frame, after the slip's own continuation.</summary>
    private void SlipAcknowledged(CaseVerdict verdict)
    {
        _slipOpen = false;
        if (!_endShiftAfterSlip)
            return;
        _endShiftAfterSlip = false;
        StartCoroutine(EndShiftNextFrame());
    }

    /// <summary>The waiting End Shift, a frame after the slip closed (its continuation has resolved the case by then).</summary>
    private IEnumerator EndShiftNextFrame()
    {
        yield return null;
        Debug.Log("[GameManager] Cheat: " + CheatEndShift());
    }

    /// <summary>Decides <paramref name="inst"/> after CheatDecideDelay, when they still stand at the desk.</summary>
    private IEnumerator DecideSoon(CaseInstance inst)
    {
        yield return new WaitForSecondsRealtime(CheatDecideDelay);
        if (_travellerAtDesk && inst != null && ActiveCase == inst)
            CheatDecide(inst);
    }

    /// <summary>The correct verdict for <paramref name="inst"/>, through the desk's own decision (InvestigationUIController.Decide), scored with the evidence a denial needs.</summary>
    private string CheatDecide(CaseInstance inst)
    {
        bool accept = inst.ShouldAccept;
        _cheatEvidence = true;
        try
        {
            investigationUI.Decide(accept ? DeskStamp.Approved : DeskStamp.Denied);
        }
        finally
        {
            _cheatEvidence = false;
        }
        return $"{inst.visitorDisplayName}: {(accept ? "APPROVED" : "DENIED")} (correct).";
    }
}
