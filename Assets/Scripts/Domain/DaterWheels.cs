using System;

/// <summary>Where each of a dater's date wheels stands (notch indices), or how many ratchet clicks each turns.</summary>
public readonly struct WheelSetting
{
    /// <summary>The day wheel (0 is the 1st; 31 notches).</summary>
    public readonly int Day;

    /// <summary>The month wheel (0 is January; 12 notches).</summary>
    public readonly int Month;

    /// <summary>The year band (the year's last digit; a decade, 10 notches).</summary>
    public readonly int Year;

    /// <summary>A setting of the three wheels.</summary>
    public WheelSetting(int day, int month, int year)
    {
        Day = day;
        Month = month;
        Year = year;
    }

    /// <summary>Every wheel's clicks together (a roll's length in ratchet clicks).</summary>
    public int Clicks => Day + Month + Year;
}

/// <summary>
/// A dater's date wheels (the desk machine spec §1, the S-401 reference):
/// a day wheel (1 to 31), a month wheel (JAN to DEC) and a year band (the
/// decade's ten years), seen through the frame's side. Each morning the
/// daters still show yesterday (Shown); the first time one is picked up that
/// day its wheels roll forward to today, one ratchet click per notch (Steps;
/// a ratchet turns one way, so a short month rolls past its unused days).
/// Automatic, never a task the player can get wrong (Decision in the spec).
/// The spec's era code wheel is left out of the prototype (the year band
/// prints the whole year). Pure.
/// </summary>
public static class DaterWheels
{
    /// <summary>The day wheel's notches (1 to 31).</summary>
    public const int DayNotches = 31;

    /// <summary>The month wheel's notches.</summary>
    public const int MonthNotches = 12;

    /// <summary>The year band's notches (a decade).</summary>
    public const int YearNotches = 10;

    /// <summary>The wheels' notches for <paramref name="date"/>.</summary>
    public static WheelSetting Notches(DateTime date) => new WheelSetting(date.Day - 1, date.Month - 1, date.Year % YearNotches);

    /// <summary>The date the wheels show on the morning of <paramref name="today"/>: yesterday's, until the day's first pick-up rolls them.</summary>
    public static DateTime Shown(DateTime today) => today.AddDays(-1);

    /// <summary>The clicks each wheel turns forward from <paramref name="shown"/> to <paramref name="today"/>.</summary>
    public static WheelSetting Steps(DateTime shown, DateTime today)
    {
        WheelSetting from = Notches(shown), to = Notches(today);
        return new WheelSetting(Forward(from.Day, to.Day, DayNotches), Forward(from.Month, to.Month, MonthNotches), Forward(from.Year, to.Year, YearNotches));
    }

    /// <summary>The labels round each wheel's band, notch by notch, for a dater set in the decade of <paramref name="year"/>: the day wheel 1 to 31, the month wheel JAN to DEC, the year band the decade's years.</summary>
    public static string[] Labels(int wheel, int year)
    {
        int n = wheel == 0 ? DayNotches : wheel == 1 ? MonthNotches : YearNotches;
        var labels = new string[n];
        int decade = year - ((year % YearNotches) + YearNotches) % YearNotches;
        for (int i = 0; i < n; i++)
            labels[i] = wheel == 0 ? (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)
                : wheel == 1 ? BirthDates.MonthName(i).ToUpperInvariant()
                : BirthDates.FormatYear(decade + i);
        return labels;
    }

    /// <summary>The clicks a wheel of <paramref name="notches"/> turns forward from notch <paramref name="from"/> to <paramref name="to"/> (0 when it stands there).</summary>
    public static int Forward(int from, int to, int notches) => notches <= 0 ? 0 : ((to - from) % notches + notches) % notches;
}
