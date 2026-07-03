using System;
using System.Collections.Generic;

/// <summary>
/// Records every verdict of the current shift for the end-of-day report.
/// Pure data — no Unity dependencies. Rebuilt fresh each day.
/// </summary>
public sealed class ShiftLedger
{
    /// <summary>All verdicts issued this shift, in order.</summary>
    public readonly List<CaseVerdict> verdicts = new();

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

    /// <summary>Net money change for the shift.</summary>
    public int NetMoney => TotalPay - TotalPenalties;

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

    /// <summary>Denials of real forgers made without documented evidence.</summary>
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
/// The outcome of a single case decision, fully resolved.
/// </summary>
[Serializable]
public sealed class CaseVerdict
{
    /// <summary>1-based case slot index.</summary>
    public int caseIndex1Based;

    /// <summary>Visitor display name (for the report).</summary>
    public string visitorName;

    /// <summary>Era the player chose.</summary>
    public string chosenEraId;

    /// <summary>The correct era.</summary>
    public string trueEraId;

    /// <summary>True if the send was correct.</summary>
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

    /// <summary>True if accepting was the correct call (genuine + allowed).</summary>
    public bool shouldAccept;

    /// <summary>True if the case's documents were forged (had an anachronism).</summary>
    public bool wasForged;

    /// <summary>True if the claimed destination was permitted by today's rules.</summary>
    public bool claimAllowed = true;

    /// <summary>The visitor's stated travel claim, for the report.</summary>
    public string claimSummary = string.Empty;

    /// <summary>Discrepancies documented in the scanner when the decision was made.</summary>
    public int evidenceCount;

    /// <summary>True if a real forger was denied without documented evidence.</summary>
    public bool unprovenDenial;
}
