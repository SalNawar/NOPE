using System;
using System.Collections.Generic;

/// <summary>
/// The agency block of world_source.json (the traveller-types spec's F6; the
/// PC spec's §4.8): the agency's printed name and programme line, and the
/// date of the first day, the day ranges a displaced person's file is
/// drawn from (phase 3), and the ranges and transponder models 2150
/// citizens' accounts are drawn from (phase 6). Content, written into the
/// content library by Generate World; later phases add the clerk's account
/// and the agency's other lists.
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

    /// <summary>The day ranges of a displaced person's file (agency.displaced: found within 30 days, a certificate valid 3 to 365 days; AgencyNumbers.Displaced).</summary>
    public DisplacementRanges displaced = new DisplacementRanges();

    /// <summary>The ranges a 2150 citizen's account is drawn from (agency.accounts: Valid Until, past trips, each status's debt and trips; AccountMaker.Make).</summary>
    public AccountRanges accounts = new AccountRanges();

    /// <summary>The transponder models citizens travel on (agency.transponders: a weighted list per class).</summary>
    public List<TransponderModel> transponders = new List<TransponderModel>();

    /// <summary>What Generate World and the validator refuse: a blank name or programme, a first date AgencyCalendar cannot count from, displaced ranges AgencyNumbers cannot draw from (found at least 1 day ago; valid from at least today, the least no more than the most), and the accounts' ranges and transponder models (AccountRanges.Problems). Empty when sound.</summary>
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
        if (displaced == null)
        {
            problems.Add("agency.displaced is missing: the day ranges of a displaced person's file.");
            return problems;
        }
        if (displaced.foundWithinDays < 1)
            problems.Add($"agency.displaced.foundWithinDays is {displaced.foundWithinDays}: a displaced person is found at least 1 day before today.");
        if (displaced.validDaysMin < 0 || displaced.validDaysMin > displaced.validDaysMax)
            problems.Add($"agency.displaced.validDaysMin {displaced.validDaysMin} and validDaysMax {displaced.validDaysMax}: a certificate is valid from 0 <= validDaysMin <= validDaysMax days after today.");
        if (accounts == null)
            problems.Add("agency.accounts is missing: the ranges a 2150 citizen's account is drawn from.");
        else
            problems.AddRange(accounts.Problems(transponders));
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
    public static string Today(string firstDate, int day) => TryToday(firstDate, day, out DateTime today) ? Write(today) : null;

    /// <summary>Today's date on shift day <paramref name="day"/> as a date (the agency's number and date makers count from it); false as <see cref="Today"/> gives null.</summary>
    public static bool TryToday(string firstDate, int day, out DateTime today)
    {
        today = default;
        if (day < 1 || !TryDate(firstDate, out DateTime first))
            return false;

        try
        {
            today = first.AddDays(day - 1);
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
        return true;
    }

    /// <summary>A date as the calendar, the papers and the records print it ("14 Mar 2150", BirthDates.Format).</summary>
    public static string Write(DateTime date) => BirthDates.Format(date.Day, date.Month - 1, date.Year);

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
