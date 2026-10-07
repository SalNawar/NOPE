using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// One visit to the desk this run (the scanner app spec §2.6, seen-before
/// history; WorldState.visits): whose record it was (CitizenRecord.Id: the
/// agency number, else the registered name), the day and its date as the
/// papers print it, the claimed place, the verdict as a word and the citation
/// it cost the clerk (blank: none). The verdict is a word, not a flag
/// ("Accepted", "Denied", and the desk machine's coming "Detained"), so a new
/// verdict slots in without a save change. Additive: an old save loads none.
/// </summary>
[Serializable]
public sealed class VisitEntry
{
    /// <summary>The record's identity (Visits.Key: the trimmed agency number, else the registered name).</summary>
    public string record = string.Empty;

    /// <summary>The day of the visit.</summary>
    public int day;

    /// <summary>The day's date as the papers print it ("14 Mar 2150"; blank when the calendar could not count it).</summary>
    public string date = string.Empty;

    /// <summary>The claimed place's label ("Periclean Athens (Ancient)").</summary>
    public string place = string.Empty;

    /// <summary>The verdict's word ("Accepted", "Denied", "Detained").</summary>
    public string verdict = string.Empty;

    /// <summary>The citation's reason line the decision cost (blank: none).</summary>
    public string citation = string.Empty;
}

/// <summary>
/// The seen-before rules (the scanner app spec §2.6; Saleh: "seen-before
/// history"), pure: the run's visits are recorded at each verdict under the
/// record's identity; a record shows its earlier visits, and a recurring face
/// carries a flag of its latest verdict ("DENIED 3 DAYS AGO"). This run only
/// (the spec's Decision).
/// </summary>
public static class Visits
{
    /// <summary>The verdict word of an approval.</summary>
    public const string Accepted = "Accepted";

    /// <summary>The verdict word of a denial.</summary>
    public const string Denied = "Denied";

    /// <summary>A record's identity in the visit log: the trimmed agency number (a Citizen ID or a Displacement No.), else the trimmed name; blank when both are.</summary>
    public static string Key(string number, string name) =>
        !string.IsNullOrWhiteSpace(number) ? number.Trim() : (name ?? string.Empty).Trim();

    /// <summary>
    /// Records a visit in <paramref name="log"/> (in verdict order); nothing
    /// for a blank record or verdict, or a log that is null. A second verdict
    /// for the same record on the same day replaces the first (one visit a day).
    /// </summary>
    public static void Record(List<VisitEntry> log, string record, int day, string date, string place, string verdict, string citation)
    {
        if (log == null || string.IsNullOrWhiteSpace(record) || string.IsNullOrWhiteSpace(verdict))
            return;
        log.RemoveAll(v => v != null && v.day == day && string.Equals(v.record, record.Trim(), StringComparison.Ordinal));
        log.Add(new VisitEntry
        {
            record = record.Trim(),
            day = day,
            date = date ?? string.Empty,
            place = place ?? string.Empty,
            verdict = verdict.Trim(),
            citation = citation ?? string.Empty
        });
    }

    /// <summary>The visits of <paramref name="record"/> before <paramref name="day"/>, oldest first (none for a blank record).</summary>
    public static List<VisitEntry> Before(IReadOnlyList<VisitEntry> log, string record, int day)
    {
        var visits = new List<VisitEntry>();
        if (log == null || string.IsNullOrWhiteSpace(record))
            return visits;
        string key = record.Trim();
        foreach (VisitEntry v in log)
            if (v != null && v.day < day && string.Equals(v.record, key, StringComparison.Ordinal))
                visits.Add(v);
        visits.Sort((a, b) => a.day.CompareTo(b.day));
        return visits;
    }

    /// <summary>
    /// The flag a recurring face carries on <paramref name="today"/>: the latest
    /// earlier visit's verdict and how many days ago it was; null with no
    /// earlier visit.
    /// </summary>
    public static SeenBefore? Flag(IReadOnlyList<VisitEntry> earlier, int today)
    {
        if (earlier == null || earlier.Count == 0)
            return null;
        VisitEntry last = null;
        foreach (VisitEntry v in earlier)
            if (v != null && v.day < today && (last == null || v.day >= last.day))
                last = v;
        return last == null ? (SeenBefore?)null : new SeenBefore(last.verdict, today - last.day);
    }

    /// <summary>
    /// The flag's words ("DENIED 3 DAYS AGO", "ACCEPTED YESTERDAY"): the
    /// verdict's word in capitals, then <paramref name="daysAgo"/> through
    /// <paramref name="days"/> ("{0} {1} DAYS AGO") or, for one day,
    /// <paramref name="yesterday"/> ("{0} YESTERDAY"); {0} is the verdict and
    /// {1} the days. Blank templates read as the defaults.
    /// </summary>
    public static string FlagText(SeenBefore flag, string verdictWord, string days, string yesterday)
    {
        string word = (string.IsNullOrWhiteSpace(verdictWord) ? flag.Verdict : verdictWord).ToUpperInvariant();
        string template = flag.DaysAgo == 1
            ? (string.IsNullOrWhiteSpace(yesterday) ? "{0} YESTERDAY" : yesterday)
            : (string.IsNullOrWhiteSpace(days) ? "{0} {1} DAYS AGO" : days);
        return template.Replace("{0}", word).Replace("{1}", flag.DaysAgo.ToString(CultureInfo.InvariantCulture));
    }
}

/// <summary>A recurring face's flag: their latest earlier verdict's word and how many days ago it was.</summary>
public readonly struct SeenBefore
{
    /// <summary>A flag of <paramref name="verdict"/>, <paramref name="daysAgo"/> days ago.</summary>
    public SeenBefore(string verdict, int daysAgo)
    {
        Verdict = verdict ?? string.Empty;
        DaysAgo = daysAgo;
    }

    /// <summary>The latest earlier verdict's word ("Denied").</summary>
    public string Verdict { get; }

    /// <summary>How many days before today it was (1 = yesterday).</summary>
    public int DaysAgo { get; }
}
