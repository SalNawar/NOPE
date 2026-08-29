// ReSharper disable InconsistentNaming
using UnityEngine;

/// <summary>
/// An era/destination (e.g., Rome, Future, Medieval).
/// </summary>
[CreateAssetMenu(fileName = "Era_", menuName = "TimeDesk/Era", order = 10)]
public sealed class EraSO : ScriptableObject
{
    /// <summary>Stable ID used for save/load and lookups (e.g., "rome").</summary>
    public string id;

    /// <summary>Display name shown in UI (e.g., "Ancient Rome").</summary>
    public string displayName;

    [Header("Calendar (birth-date reckoning)")]
    /// <summary>
    /// Native month names in calendar order ("Hekatombaion", ...). Empty =
    /// Gregorian abbreviations. Read by the desktop Chrono Converter and by
    /// birth-date generation.
    /// </summary>
    [SerializeField] private string[] calendarMonths;

    /// <summary>True when authored years count backward (BCE epochs).</summary>
    [SerializeField] private bool calendarYearsAreBCE;

    /// <summary>Plausible birth-year span start, modern convention (negative = BCE).</summary>
    [SerializeField] private int calendarSpanStart;

    /// <summary>Plausible birth-year span end, modern convention.</summary>
    [SerializeField] private int calendarSpanEnd;

    /// <summary>Native month table (null-safe: Gregorian when unset).</summary>
    public string[] CalendarMonths => calendarMonths ?? System.Array.Empty<string>();

    /// <summary>True when this era's years count backward (BCE).</summary>
    public bool CalendarYearsAreBCE => calendarYearsAreBCE;

    /// <summary>Plausible span start in modern years (negative = BCE).</summary>
    public int CalendarSpanStart => calendarSpanStart;

    /// <summary>Plausible span end in modern years.</summary>
    public int CalendarSpanEnd => calendarSpanEnd;

    /// <summary>True when this era carries authored calendar data.</summary>
    public bool HasCalendar => calendarMonths != null && calendarMonths.Length > 0 || calendarSpanEnd > 0;
}
