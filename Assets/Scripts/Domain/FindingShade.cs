/// <summary>How the PC colours a finding (FindingShades.Of): proofs alone look like proofs (BUG-47b). Not serialized.</summary>
public enum FindingShade
{
    /// <summary>Grey: a note, nothing logged.</summary>
    Info,

    /// <summary>Green: a match, a rule met, a date that holds.</summary>
    Match,

    /// <summary>Amber, dashed: a difference that proves nothing a citation can stand on ("Data differs" with no proved deviation, NO RECORD).</summary>
    Hint,

    /// <summary>Red: evidence: a proved deviation (DiscrepancyLog) or a directive fault's evidence (a broken rule, a date, a missing paper).</summary>
    Proof
}

/// <summary>
/// The proof and hint colours (BUG-47b, UI-PC; BUGLIST: "Red 'Data differs'
/// findings look like proof but earn unproven citations ... hints and proofs
/// share the colour red"): of the differences FindingRules logs, only a
/// proof is red; any other difference is an amber hint. The classification
/// is CONTENT's (FindingRules, DiscrepancyLog); this only maps it to a
/// shade. Pure; the workbench's line, status and findings column read it.
/// </summary>
public static class FindingShades
{
    /// <summary>The shade of a finding of <paramref name="kind"/>; <paramref name="proved"/> when the pair proved a deviation (Finding.Proof).</summary>
    public static FindingShade Of(FindingKind kind, bool proved)
    {
        switch (FindingRules.Look(kind))
        {
            case FindingLook.Match:
                return FindingShade.Match;
            case FindingLook.Differ:
                return proved || FindingRules.IsDirectiveEvidence(kind) ? FindingShade.Proof : FindingShade.Hint;
            default:
                return FindingShade.Info;
        }
    }
}
