using System;

/// <summary>
/// Pure era-calendar conversion rules for birth-date reckoning.
/// An "era-native" date reads "day MonthName year" where MonthName belongs to
/// the era's calendar (Attic months for Classical Greece, Egyptian civil
/// months, Gregorian abbreviations for the imperial nations) and the year is
/// counted in that era's epoch (BCE eras author positive years that count
/// backward). Modern years use the convention: negative = BCE.
/// Pure C# over primitives so the decision table is unit-testable without
/// ScriptableObjects; EraSO supplies the data.
/// </summary>
public static class CalendarConverter
{
    /// <summary>Gregorian month abbreviations, in order.</summary>
    public static readonly string[] GregorianMonths =
        { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

    /// <summary>Parsed pieces of one date string.</summary>
    public readonly struct ParsedDate
    {
        /// <summary>Day of month (1-based).</summary>
        public readonly int day;

        /// <summary>Month index (0-based) in whichever table matched.</summary>
        public readonly int monthIndex;

        /// <summary>Year as authored (positive; epoch decided by the era).</summary>
        public readonly int year;

        /// <summary>True when the month token matched the era's native table.</summary>
        public readonly bool nativeMonth;

        public ParsedDate(int day, int monthIndex, int year, bool nativeMonth)
        {
            this.day = day;
            this.monthIndex = monthIndex;
            this.year = year;
            this.nativeMonth = nativeMonth;
        }
    }

    /// <summary>
    /// Parses "21 Elaphebolion 437" / "3 Mar 1908" against the era's native
    /// month table first, then the Gregorian abbreviations. Returns false for
    /// anything unparseable (forged "(?)" markers, free text).
    /// </summary>
    public static bool TryParse(string dateText, string[] nativeMonths, out ParsedDate parsed)
    {
        parsed = default;

        if (string.IsNullOrWhiteSpace(dateText))
            return false;

        string[] parts = dateText.Trim().Split(' ');
        if (parts.Length < 3)
            return false;

        if (!int.TryParse(parts[0], out int day) || day < 1 || day > 31)
            return false;

        if (!int.TryParse(parts[parts.Length - 1], out int year))
            return false;

        string monthToken = parts[1];
        int monthIndex = IndexOfMonth(nativeMonths, monthToken);

        if (monthIndex >= 0)
        {
            parsed = new ParsedDate(day, monthIndex, year, true);
            return true;
        }

        monthIndex = IndexOfMonth(GregorianMonths, monthToken);

        if (monthIndex >= 0)
        {
            parsed = new ParsedDate(day, monthIndex, year, false);
            return true;
        }

        return false;
    }

    /// <summary>True when the input is a bare integer (modern-year mode).</summary>
    public static bool IsBareYear(string text) =>
        int.TryParse((text ?? string.Empty).Trim(), out int _);

    /// <summary>Converts an authored era year to the modern convention.</summary>
    public static int ToModernYear(int eraYear, bool yearsAreBCE) => yearsAreBCE ? -eraYear : eraYear;

    /// <summary>Converts a modern year (negative = BCE) back to the era's authored form.</summary>
    public static int ToEraYear(int modernYear, bool yearsAreBCE) => yearsAreBCE ? -modernYear : modernYear;

    /// <summary>True when the modern year lies inside the era's plausible span (inclusive).</summary>
    public static bool WithinSpan(int modernYear, int spanStart, int spanEnd) =>
        spanStart <= modernYear && modernYear <= spanEnd;

    /// <summary>Formats a modern year for display: -437 -> "437 BCE", 1908 -> "1908 CE".</summary>
    public static string FormatModern(int modernYear) =>
        modernYear < 0 ? $"{-modernYear} BCE" : $"{modernYear} CE";

    /// <summary>
    /// Renders a modern year (and optional day/month) in the era's own
    /// reckoning. Month mapping is ordinal and marked approximate — a reckoner
    /// translates years exactly and months only roughly.
    /// </summary>
    public static string FormatEra(int modernYear, bool yearsAreBCE, string[] nativeMonths, int day = 0, int monthIndex = -1)
    {
        int eraYear = ToEraYear(modernYear, yearsAreBCE);
        string epoch = yearsAreBCE ? " BCE" : string.Empty;
        string month = null;

        if (monthIndex >= 0)
        {
            string[] table = nativeMonths != null && nativeMonths.Length > 0 ? nativeMonths : GregorianMonths;
            month = monthIndex < table.Length ? table[monthIndex] : null;
        }

        if (day > 0 && month != null)
            return $"{day} {month} {eraYear}{epoch}";

        return $"year {eraYear}{epoch}";
    }

    /// <summary>Case-insensitive month lookup; -1 when absent.</summary>
    private static int IndexOfMonth(string[] months, string token)
    {
        if (months == null || string.IsNullOrEmpty(token))
            return -1;

        for (int i = 0; i < months.Length; i++)
        {
            if (string.Equals(months[i], token, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }
}
