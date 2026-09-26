using System;

/// <summary>
/// Visitor birth dates as shown on papers and in Citizen Records: "12 Mar 830"
/// or "3 Jun 1450 BCE". Years are signed (negative = BCE); there is no year 0.
/// Pure, so generation, birth-date tells and ages are seeded and tested headless.
/// </summary>
public static class BirthDates
{
    /// <summary>Month abbreviations, index 0 = January.</summary>
    private static readonly string[] Months =
        { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

    /// <summary>Suffix for years before 1 CE.</summary>
    private const string Bce = "BCE";

    /// <summary>
    /// The travellers' age range (world_source.json's travellerAgeMin and
    /// travellerAgeMax, from which every place's birth years are written):
    /// null when 1 &lt;= min &lt;= max, else the problem (audit R6-001: an
    /// inverted range silently inverted every place's birth years).
    /// </summary>
    public static string AgeRangeProblem(int minAge, int maxAge) =>
        minAge >= 1 && minAge <= maxAge
            ? null
            : $"travellerAgeMin {minAge} and travellerAgeMax {maxAge}: travellers' ages need 1 <= travellerAgeMin <= travellerAgeMax.";

    /// <summary>Writes a date; negative years get a BCE suffix.</summary>
    public static string Format(int day, int monthIndex0, int year) =>
        year < 0 ? $"{day} {Months[monthIndex0]} {-year} {Bce}" : $"{day} {Months[monthIndex0]} {year}";

    /// <summary>Reads a date written by <see cref="Format"/>. Year 0 is invalid.</summary>
    public static bool TryParse(string text, out int day, out int monthIndex0, out int year)
    {
        day = 0;
        monthIndex0 = -1;
        year = 0;

        if (string.IsNullOrWhiteSpace(text))
            return false;

        string[] parts = text.Trim().Split(' ');
        bool bce = parts.Length == 4 && parts[3] == Bce;
        if (parts.Length != 3 && !bce)
            return false;

        if (!int.TryParse(parts[0], out day) || !int.TryParse(parts[2], out int absYear) || absYear <= 0)
            return false;

        monthIndex0 = Array.IndexOf(Months, parts[1]);
        if (monthIndex0 < 0)
            return false;

        year = bce ? -absYear : absYear;
        return true;
    }

    /// <summary>
    /// The age in <paramref name="atYear"/> of someone born on
    /// <paramref name="date"/> (years only; no year 0, so 10 BCE to 5 CE is 14).
    /// False when the date is unreadable or <paramref name="atYear"/> is 0.
    /// </summary>
    public static bool TryAgeAt(string date, int atYear, out int age)
    {
        age = 0;
        if (atYear == 0 || !TryParse(date, out _, out _, out int year))
            return false;

        age = atYear - year - (year < 0 && atYear > 0 ? 1 : 0);
        return true;
    }

    /// <summary>A date with a year in [yearMin, yearMax] (bounds may be reversed), never year 0.</summary>
    public static string Generate(int yearMin, int yearMax, IRandomSource rng)
    {
        if (yearMax < yearMin)
            (yearMin, yearMax) = (yearMax, yearMin);

        int year = rng.Range(yearMin, yearMax + 1);
        if (year == 0)
            year = yearMax >= 1 ? 1 : -1;

        int day = rng.Range(1, 29);
        int month = rng.Range(0, Months.Length);
        return Format(day, month, year);
    }

    /// <summary>
    /// True when <paramref name="coverDate"/> is readable and [yearMin, yearMax]
    /// (bounds may be reversed) holds a year that is neither 0 nor the cover
    /// date's year, so a birth-date tell can be drawn. Draws nothing.
    /// </summary>
    public static bool HasOtherYear(string coverDate, int yearMin, int yearMax) =>
        TryParse(coverDate, out _, out _, out int coverYear) && OtherYearCount(coverYear, yearMin, yearMax) > 0;

    /// <summary>
    /// A birth-date tell: the cover date's day and month with a year drawn
    /// uniformly from [yearMin, yearMax] (bounds may be reversed), never year 0
    /// and never the cover year. Exactly one Range draw: an index into those
    /// years in ascending order. Null, with no draw, when <see cref="HasOtherYear"/>
    /// is false or <paramref name="rng"/> is null.
    /// </summary>
    public static string PickOtherYear(string coverDate, int yearMin, int yearMax, IRandomSource rng)
    {
        if (rng == null || !TryParse(coverDate, out int day, out int month, out int coverYear))
            return null;

        int count = OtherYearCount(coverYear, yearMin, yearMax);
        if (count <= 0)
            return null;

        if (yearMax < yearMin)
            (yearMin, yearMax) = (yearMax, yearMin);

        // Step over the skipped years (0 and the cover year), smallest first.
        long year = (long)yearMin + rng.Range(0, count);
        foreach (int skipped in coverYear < 0 ? new[] { coverYear, 0 } : new[] { 0, coverYear })
            if (skipped >= yearMin && skipped <= year)
                year++;

        return Format(day, month, (int)year);
    }

    /// <summary>How many years of [yearMin, yearMax] (either order) are neither 0 nor <paramref name="coverYear"/>.</summary>
    private static int OtherYearCount(int coverYear, int yearMin, int yearMax)
    {
        if (yearMax < yearMin)
            (yearMin, yearMax) = (yearMax, yearMin);

        long count = (long)yearMax - yearMin + 1;
        if (yearMin <= 0 && 0 <= yearMax)
            count--;
        if (coverYear != 0 && yearMin <= coverYear && coverYear <= yearMax)
            count--;

        return count > int.MaxValue ? int.MaxValue : (int)count;
    }
}
