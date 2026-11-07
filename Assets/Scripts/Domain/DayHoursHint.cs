using System.Collections.Generic;
using System.Text;

/// <summary>
/// The cheat menu's day buttons' hint (DAY-01, D13: "a day jump opens the day
/// at its own hours, as in play; the memo and the cheat's hint name the hour").
/// A jump lands on the day's briefing, and the day opens at its own hours
/// (DayPlanSO.Shift: 09:00 on days 1 to 7, 13:00 on 8 to 14, 16:00 on 15 to 21
/// in the current content), so the menu names each day's opening hour before
/// the click. Pure; tested headless.
/// </summary>
public static class DayHoursHint
{
    /// <summary>One day's hint: "Day 12 opens at 13:00 (closes 21:00)".</summary>
    public static string Hint(int day, ShiftHours hours) => $"Day {day} opens at {hours.Open} (closes {hours.Close})";

    /// <summary>
    /// The opening hours of days 1 to n as runs of days that open at the same
    /// hour (<paramref name="hoursByDay"/>[0] is day 1's): "Opens 09:00 days 1-7,
    /// 13:00 days 8-14, 16:00 days 15-21" ("day 3" for a run of one day).
    /// Empty for no days.
    /// </summary>
    public static string Summary(IReadOnlyList<ShiftHours> hoursByDay)
    {
        if (hoursByDay == null || hoursByDay.Count == 0)
            return string.Empty;
        var text = new StringBuilder("Opens ");
        int first = 0;
        for (int i = 1; i <= hoursByDay.Count; i++)
        {
            if (i < hoursByDay.Count && hoursByDay[i].StartMinute == hoursByDay[first].StartMinute)
                continue;
            if (first > 0)
                text.Append(", ");
            text.Append(hoursByDay[first].Open).Append(i - first == 1 ? $" day {first + 1}" : $" days {first + 1}-{i}");
            first = i;
        }
        return text.ToString();
    }
}
