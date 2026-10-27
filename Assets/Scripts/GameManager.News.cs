using UnityEngine;

/// <summary>
/// The News site's back issues: today's morning paper recorded at the day's
/// start. Carved out of GameManager.cs by FLOW (an extract of Start's
/// archive call; FIX_PLAN_v2 FLOW delta, R7 PHASEB LEASES.md). Phase B
/// holder: NEWS-WEB.
/// </summary>
public sealed partial class GameManager
{
    /// <summary>
    /// The morning paper is printed: its lines go to the News site's back issues (the night rebuilds them, so they are kept now).
    /// The paper is the world's: the clerk's bulletin is the Bureau memo's, never a back issue's.
    /// </summary>
    private void RecordTodaysPaper()
    {
        // The paper is the world's: the clerk's bulletin is the Bureau memo's, never a back issue's.
        if (desktopConfig != null)
            NewsArchive.Record(_worldState.newsArchive, _worldState.day, _worldState.tomorrow.briefingLines, _worldState.tomorrow.newsLines, desktopConfig.newsArchiveIssues,
                               _worldState.tomorrow.deskLines);
        else
            Debug.LogWarning("[GameManager] No DesktopConfigSO wired: today's paper is not kept for the News site. Run Tools > TimeDesk > Build Office UI.");
    }
}
