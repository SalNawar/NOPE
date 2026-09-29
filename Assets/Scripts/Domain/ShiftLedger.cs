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

    /// <summary>Net money change for the shift: the pay less the wrong-decision penalties and the Debt Relief instalment (a stranding fines nothing; redesign phase 23).</summary>
    public int NetMoney => TotalPay - TotalPenalties - debtInstalment;

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

    /// <summary>Total stability change across the shift (signed).</summary>
    public float TotalStabilityDelta
    {
        get
        {
            float sum = 0f;
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

    /// <summary>Citation slip text shown to the player (empty if none).</summary>
    public string citationText = string.Empty;

    // -----------------------------
    // Investigation (accept/deny)
    // -----------------------------

    /// <summary>True if the player accepted (approved travel); false = denied.</summary>
    public bool accepted;

    /// <summary>True if accepting was the correct call (no fault of either kind, VerdictRules).</summary>
    public bool shouldAccept;

    /// <summary>The visitor's stated travel claim, for the report.</summary>
    public string claimSummary = string.Empty;

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
