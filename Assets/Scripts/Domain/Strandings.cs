using System;
using System.Collections.Generic;

/// <summary>
/// A traveller stranded in the past (the traveller-types spec's S1; the
/// endings and strandings spec §6): who, where, the fate they met, whether a
/// valid signed waiver was on file and the fine it cost the desk. Kept for
/// the next morning's paper (HistoryState.pendingStrandings, then cleared) and
/// for good in the run's stranding log (HistoryState.strandingLog: the Mail
/// failure reports); the fields after day are additive (an old record reads
/// their defaults).
/// </summary>
[Serializable]
public sealed class StrandingRecord
{
    /// <summary>The traveller's display name.</summary>
    public string travellerName;

    /// <summary>The destination's label, where they are lost.</summary>
    public string placeLabel;

    /// <summary>The day of the shift that sent them.</summary>
    public int day;

    /// <summary>The fate they met (StrandingFates.Pick; a carry that could not be made reads News).</summary>
    public StrandingFate fate;

    /// <summary>True when a valid signed waiver was on file when they left (Waivers.OnFile).</summary>
    public bool waivered;

    /// <summary>The stranding fine charged to the desk for them, in cr (GameConfigSO.strandingFine when no valid signed waiver was on file; 0 otherwise).</summary>
    public int fine;

    /// <summary>Their Citizen ID (the account's), for the failure report.</summary>
    public string citizenId = string.Empty;

    /// <summary>The unit that failed (the account's transponder serial), for the failure report.</summary>
    public string transponder = string.Empty;

    /// <summary>The waiver number registered on their account, for the failure report.</summary>
    public string waiverNo = string.Empty;

    /// <summary>Their debt in cr (it passes to kin under a waiver), for the failure report.</summary>
    public int debt;

    /// <summary>The morning paper's line for them (StrandingFates.Line); blank for a forgotten traveller.</summary>
    public string line = string.Empty;
}

/// <summary>
/// Cheap transponders fail (the traveller-types spec's S1-S3; redesign phase
/// 13b): at the shift's end each accepted traveller whose real transponder
/// (the account's, whatever the manifest claims) is Economy is rolled on the
/// day's stranding stream (<see cref="Seeds.ForStrandings"/>), in queue order,
/// one draw each; a stranded traveller does not come back and meets one fate (StrandingFates, the endings and strandings spec §6). The
/// stranding fine (Saleh's Q10 = D, a knowing exception to the one-penalty
/// rule) is charged only when no valid signed waiver was on file on a day
/// the agency issues the waiver (<see cref="Fine"/>, ShiftStrandings). Pure:
/// the draws are tested headless.
/// </summary>
public static class Strandings
{
    /// <summary>
    /// The indices of the accepted travellers stranded tonight: one draw of
    /// <paramref name="rng"/> per traveller whose real class is Economy, in
    /// order, stranded when the draw falls under <paramref name="chance"/>
    /// (agency.strandChance); a Premium unit or no transponder (null: the
    /// displaced) draws nothing. Every Economy traveller draws, so a changed
    /// chance never shifts a later traveller's draw.
    /// </summary>
    public static List<int> Roll(IReadOnlyList<TransponderClass?> realClasses, float chance, IRandomSource rng)
    {
        var stranded = new List<int>();
        if (realClasses == null || rng == null)
            return stranded;

        for (int i = 0; i < realClasses.Count; i++)
        {
            if (realClasses[i] != TransponderClass.Economy)
                continue;
            if (rng.Value() < chance)
                stranded.Add(i);
        }
        return stranded;
    }

    /// <summary>
    /// The fine a stranding charges: <paramref name="strandingFine"/>
    /// (GameConfigSO.strandingFine; never below 0) when no valid signed
    /// waiver was on file and the day issued the Stranding Waiver
    /// (<paramref name="waiverIssued"/>, DayPapers; lesson D7: before the
    /// waiver arrives nobody could have checked one, so no approval was wrong
    /// and the desk is not liable); 0 otherwise.
    /// </summary>
    public static int Fine(bool waivered, bool waiverIssued, int strandingFine) =>
        waivered || !waiverIssued ? 0 : Math.Max(0, strandingFine);

    /// <summary>The next morning's stranding lines: each record's line, in order (a forgotten traveller's blank line and null records skipped).</summary>
    public static List<string> Lines(IReadOnlyList<StrandingRecord> strandings)
    {
        var lines = new List<string>();
        foreach (StrandingRecord s in strandings ?? Array.Empty<StrandingRecord>())
            if (s != null && !string.IsNullOrWhiteSpace(s.line))
                lines.Add(s.line);
        return lines;
    }
}
