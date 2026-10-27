using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Today's bulletin (the plan's line, then the new desk hours when they change).
/// Carved out of GameManager.cs by P0-A (pure move). Owner: TEXT-LANG.
/// </summary>
public sealed partial class GameManager
{
    /// <summary>
    /// Today's bulletin: the plan's line, then, when today's desk hours differ
    /// from yesterday's (ShiftHours.Announces; the night shifts' days 8 and 12),
    /// the line naming the new hours (UI string briefing.newHours), in the
    /// player's language like the briefing it is printed in (the chrome, BUG-3a:
    /// it no longer turns into the world's language inside English content).
    /// </summary>
    private string TodaysBulletin()
    {
        string bulletin = dayPlan.Bulletin.Trim();
        ShiftHours today = dayPlan.Shift(_gameConfig);
        DayPlanSO yesterdayPlan = _worldState.day > 1 ? contentLibrary.GetDayPlan(_worldState.day - 1) : null;
        if (!ShiftHours.Announces(yesterdayPlan != null ? yesterdayPlan.Shift(_gameConfig) : (ShiftHours?)null, today))
            return bulletin;
        string hours = UiText.Format("briefing.newHours", today.Open, today.Close);
        return string.IsNullOrEmpty(bulletin) ? hours : bulletin + " " + hours;
    }
}
