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
    /// from the hours of the desk the run last left (ShiftHours.ComparedDay
    /// over RunManager.LeftDeskDay: yesterday's in play, so the night shifts'
    /// days 8 and 15; the day a cheat's jump or skip started from, so a jump
    /// from day 1 to day 11 names its 13:00 opening) (ShiftHours.Announces),
    /// the line naming the new hours (UI string briefing.newHours), in the
    /// player's language like the briefing it is printed in (the chrome, BUG-3a:
    /// it no longer turns into the world's language inside English content).
    /// </summary>
    private string TodaysBulletin()
    {
        string bulletin = dayPlan.Bulletin.Trim();
        ShiftHours today = dayPlan.Shift(_gameConfig);
        int compared = ShiftHours.ComparedDay(_worldState.day, RunManager.HasInstance ? RunManager.Instance.LeftDeskDay : 0);
        DayPlanSO earlierPlan = compared >= 1 ? contentLibrary.GetDayPlan(compared) : null;
        if (!ShiftHours.Announces(earlierPlan != null ? earlierPlan.Shift(_gameConfig) : (ShiftHours?)null, today))
            return bulletin;
        string hours = UiText.Format("briefing.newHours", today.Open, today.Close);
        return string.IsNullOrEmpty(bulletin) ? hours : bulletin + " " + hours;
    }
}
