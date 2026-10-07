using System;
using System.Collections.Generic;

/// <summary>
/// Records every verdict of the current shift for the end-of-day report, and
/// every narrative dialog completed this shift. Pure data — no Unity
/// dependencies. Rebuilt fresh each day.
/// </summary>
public sealed class ShiftLedger
{
    /// <summary>All verdicts issued this shift, in order.</summary>
    public readonly List<CaseVerdict> verdicts = new();

    /// <summary>Narrative dialogs completed this shift, in order (applied at the end of the shift; DialogOutcomes).</summary>
    public readonly List<DialogOutcome> dialogOutcomes = new();

    /// <summary>Total money earned this shift (pay only).</summary>
    public int TotalPay
    {
        get
        {
            int sum = 0;
            foreach (CaseVerdict v in verdicts) sum += v.payAwarded;
            return sum;
        }
    }

    /// <summary>Total money lost to citation penalties this shift.</summary>
    public int TotalPenalties
    {
        get
        {
            int sum = 0;
            foreach (CaseVerdict v in verdicts) sum += v.moneyPenalty;
            return sum;
        }
    }

    /// <summary>The clerk's Debt Relief instalment taken from this shift's pay at its end (ClerkDebt.Instalment; 0 until then).</summary>
    public int debtInstalment;

    /// <summary>The clerk's debt still owed after this shift's instalment (Account.Unknown until the shift's end, or when no source gives the debt).</summary>
    public int debtOwed = Account.Unknown;

    /// <summary>How many of this shift's accepted travellers were stranded at its end (Strandings.Roll; the traveller-types spec's S1).</summary>
    public int strandedCount;

    /// <summary>The stranding fines the failure reports charged at the shift's end (GameConfigSO.strandingFine per stranded traveller without a valid signed waiver; Saleh's Q10 = D).</summary>
    public int strandingFines;

    /// <summary>The stability the shift's strandings moved at its end (a tremor's loss; signed).</summary>
    public float strandingStabilityDelta;

    /// <summary>Every fine of the shift: the wrong-decision penalties and the stranding fines (the statement's FINES cell).</summary>
    public int TotalFines => TotalPenalties + strandingFines;

    /// <summary>Other money the shift moved at its end, signed: what its completed dialogs' effects added to the wallet (a bribe taken; DayCycle.CloseShift). The shift report shows it (lesson 5: no number hidden).</summary>
    public int otherMoney;

    /// <summary>Net money change for the shift: the pay less the fines (the wrong-decision penalties and the stranding fines) and the Debt Relief instalment, plus the other money.</summary>
    public int NetMoney => TotalPay - TotalFines - debtInstalment + otherMoney;

    /// <summary>The leisure departures the shift approved (traveller types §10): accepted rich and poor tourists.</summary>
    public int LeisureDepartures
    {
        get
        {
            int n = 0;
            foreach (CaseVerdict v in verdicts)
                if (v.accepted && (v.kind == TravellerKind.RichTourist || v.kind == TravellerKind.PoorTourist))
                    n++;
            return n;
        }
    }

    /// <summary>The Debt Relief departures the shift approved (§10): accepted labourers.</summary>
    public int DebtReliefDepartures
    {
        get
        {
            int n = 0;
            foreach (CaseVerdict v in verdicts)
                if (v.accepted && v.kind == TravellerKind.Labourer)
                    n++;
            return n;
        }
    }

    /// <summary>The debt put to work by the shift's Debt Relief departures, in cr (§10): the accepted labourers' debts summed.</summary>
    public int DebtPutToWork
    {
        get
        {
            int sum = 0;
            foreach (CaseVerdict v in verdicts)
                if (v.accepted && v.kind == TravellerKind.Labourer)
                    sum += v.debt;
            return sum;
        }
    }

    /// <summary>Number of correct sends.</summary>
    public int CorrectCount
    {
        get
        {
            int n = 0;
            foreach (CaseVerdict v in verdicts) if (v.correct) n++;
            return n;
        }
    }

    /// <summary>Number of wrong sends.</summary>
    public int WrongCount => verdicts.Count - CorrectCount;

    /// <summary>Denials of deviation faults made without documented evidence.</summary>
    public int UnprovenDenialCount
    {
        get
        {
            int n = 0;
            foreach (CaseVerdict v in verdicts) if (v.unprovenDenial) n++;
            return n;
        }
    }

    /// <summary>Total stability change across the shift (signed): the verdicts' and the strandings'.</summary>
    public float TotalStabilityDelta
    {
        get
        {
            float sum = strandingStabilityDelta;
            foreach (CaseVerdict v in verdicts) sum += v.stabilityDelta;
            return sum;
        }
    }
}

/// <summary>
/// A narrative dialog completed this shift; its effect is applied at the end
/// of the shift (DialogOutcomes).
/// </summary>
[Serializable]
public sealed class DialogOutcome
{
    /// <summary>The completed dialog's id.</summary>
    public string dialogId;

    /// <summary>EffectSO asset name the ending choice named (empty = none).</summary>
    public string effectName;

    /// <summary>True when the dialog is one-shot per run (the end of the shift sets its done flag).</summary>
    public bool oneShot;
}

/// <summary>
/// The outcome of a single case decision, fully resolved.
/// </summary>
[Serializable]
public sealed class CaseVerdict
{
    /// <summary>1-based case slot index.</summary>
    public int caseIndex1Based;

    /// <summary>Visitor display name (for the report).</summary>
    public string visitorName;

    /// <summary>True if the decision was correct.</summary>
    public bool correct;

    /// <summary>True if this case was a legendary.</summary>
    public bool wasLegendary;

    /// <summary>Money earned (correct sends).</summary>
    public int payAwarded;

    /// <summary>Money lost to a citation penalty (wrong sends past free warnings).</summary>
    public int moneyPenalty;

    /// <summary>Signed stability change applied by this verdict.</summary>
    public float stabilityDelta;

    /// <summary>True if a citation slip was issued (warning or penalized).</summary>
    public bool citationIssued;

    /// <summary>True if the citation was a free warning (no money penalty).</summary>
    public bool wasFreeWarning;

    /// <summary>True if this verdict dropped stability to/below the firing threshold.</summary>
    public bool firedNow;

    /// <summary>The citation's text (Mail's copy of it; empty if none).</summary>
    public string citationText = string.Empty;

    /// <summary>The citation as the desk prints it (CitationTickets; null without one). Runtime only: never saved.</summary>
    [NonSerialized] public CitationTicket ticket;

    // -----------------------------
    // Investigation (accept/deny)
    // -----------------------------

    /// <summary>True if the player accepted (approved travel); false = denied.</summary>
    public bool accepted;

    /// <summary>The traveller's kind (the ledger's departure lines count leisure and Debt Relief departures by it; traveller types §10).</summary>
    public TravellerKind kind;

    /// <summary>What the traveller owed, in cr (the account's debt; 0 for the displaced): the ledger's "debt put to work" sums accepted labourers'.</summary>
    public int debt;

    /// <summary>True if accepting was the correct call (no fault of either kind, VerdictRules).</summary>
    public bool shouldAccept;

    /// <summary>Discrepancies documented in the scanner when the decision was made.</summary>
    public int evidenceCount;

    /// <summary>True if a deviation fault was denied without documented evidence.</summary>
    public bool unprovenDenial;

    /// <summary>
    /// The traveller's one fault (Faults.Reason), as a citation key suffix:
    /// "closed" for a closed destination, "panic" for a costume error
    /// (CostumeErrors.FaultReason), "forged" for a record lie, "disguised"
    /// for a place lie; empty for no fault.
    /// </summary>
    public string faultReason = string.Empty;

    /// <summary>The label of the place the traveller was to be sent (the claim; a panic citation names it).</summary>
    public string destinationLabel = string.Empty;

    /// <summary>
    /// The UI string key of a wrong decision's mistake: "citation.unproven"
    /// for an unproven denial, "citation.deniedWrong" for another denial,
    /// "citation.acceptedWrong" plus "." and the fault reason (when there is
    /// one) for an accept ({0} = the destination).
    /// </summary>
    public string MistakeKey =>
        unprovenDenial ? "citation.unproven"
        : !accepted ? "citation.deniedWrong"
        : string.IsNullOrEmpty(faultReason) ? "citation.acceptedWrong"
        : "citation.acceptedWrong." + faultReason;
}
