using System;
using System.Collections.Generic;

/// <summary>
/// The agency block of world_source.json (the traveller-types spec's F6; the
/// PC spec's §4.8): the agency's printed name and programme line, and the
/// date of the first day. Content, written into the content library by
/// Generate World, with the clerk's own account (phase 25; phase 13 adds its
/// debt); later phases add the agency's lists.
/// </summary>
[Serializable]
public sealed class AgencyContent
{
    /// <summary>The agency's printed name ("TEMPORAL CUSTOMS").</summary>
    public string name = string.Empty;

    /// <summary>The programme line printed under it ("Debt Relief Departures").</summary>
    public string programme = string.Empty;

    /// <summary>Day 1's date, written as BirthDates writes dates ("14 Mar 2150").</summary>
    public string firstDate = string.Empty;

    /// <summary>The clerk's own account as authored ("agency.clerk"; the Citizen Account app shows it, redesign phase 25; its checks are ClerkContent.Problems).</summary>
    public ClerkContent clerk = new();

    /// <summary>What Generate World and the validator refuse: a blank name or programme, a first date AgencyCalendar cannot count from. Empty when sound.</summary>
    public List<string> Problems()
    {
        var problems = new List<string>();
        if (string.IsNullOrWhiteSpace(name))
            problems.Add("agency.name is blank: the agency's printed name.");
        if (string.IsNullOrWhiteSpace(programme))
            problems.Add("agency.programme is blank: the programme line printed under the agency's name.");
        string date = AgencyCalendar.FirstDateProblem(firstDate);
        if (date != null)
            problems.Add(date);
        return problems;
    }
}

/// <summary>
/// The agency's calendar (F6): day 1 is the agency block's first date, and
/// today is firstDate + day - 1 in the Gregorian calendar, written as
/// BirthDates writes dates ("14 Mar 2150"). Pure, so the desk calendar's date
/// and the date checks later phases add are tested headless.
/// </summary>
public static class AgencyCalendar
{
    /// <summary>Today's date on shift day <paramref name="day"/> (1 = <paramref name="firstDate"/>); null when the first date is unreadable, not a calendar day of the common era, or the day is below 1.</summary>
    public static string Today(string firstDate, int day)
    {
        if (day < 1 || !TryDate(firstDate, out DateTime first))
            return null;

        DateTime today;
        try
        {
            today = first.AddDays(day - 1);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
        return BirthDates.Format(today.Day, today.Month - 1, today.Year);
    }

    /// <summary>Null when <paramref name="firstDate"/> is a readable calendar day of the common era (BirthDates' format), else the problem.</summary>
    public static string FirstDateProblem(string firstDate) =>
        TryDate(firstDate, out _)
            ? null
            : $"agency.firstDate '{firstDate}' is not a calendar day written as BirthDates writes dates (\"14 Mar 2150\", a year of the common era).";

    /// <summary>Reads a date written by BirthDates.Format that is a real calendar day of the common era.</summary>
    private static bool TryDate(string text, out DateTime date)
    {
        date = default;
        if (!BirthDates.TryParse(text, out int day, out int month, out int year) || year < 1 || year > 9999)
            return false;
        if (day < 1 || day > DateTime.DaysInMonth(year, month + 1))
            return false;
        date = new DateTime(year, month + 1, day);
        return true;
    }
}
