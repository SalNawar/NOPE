using System;
using System.Collections.Generic;

/// <summary>
/// Visitor birth dates as shown on papers and in Citizen Records: "12 Mar 830"
/// or "3 Jun 1450 BCE". Years are signed (negative = BCE); there is no year 0.
/// Pure, so generation and forgery are seeded and tested headless.
/// </summary>
public static class BirthDates
{
    /// <summary>Month abbreviations, index 0 = January.</summary>
    private static readonly string[] Months =
        { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

    /// <summary>Suffix for years before 1 CE.</summary>
    private const string Bce = "BCE";

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
    /// A plausible but wrong date: same day and month, year shifted by
    /// <paramref name="shiftMin"/>..<paramref name="shiftMax"/> years either way,
    /// skipping year 0. The forged year stays inside [yearMin, yearMax] (the
    /// place's birth years, so the traveller is never born after their own
    /// moment) whenever such a shift exists; otherwise any shift in range is used.
    /// Unreadable input is returned with "(?)".
    /// </summary>
    public static string Forge(string trueDate, int shiftMin, int shiftMax, int yearMin, int yearMax, IRandomSource rng)
    {
        if (!TryParse(trueDate, out int day, out int month, out int year))
            return trueDate + " (?)";

        shiftMin = Math.Max(1, shiftMin);
        shiftMax = Math.Max(shiftMin, shiftMax);
        if (yearMax < yearMin)
            (yearMin, yearMax) = (yearMax, yearMin);

        var inRange = new List<int>();
        var any = new List<int>();
        for (int shift = shiftMin; shift <= shiftMax; shift++)
        {
            foreach (int forged in new[] { AddYears(year, -shift), AddYears(year, shift) })
            {
                any.Add(forged);
                if (forged >= yearMin && forged <= yearMax)
                    inRange.Add(forged);
            }
        }

        List<int> pool = inRange.Count > 0 ? inRange : any;
        return Format(day, month, pool[rng.Range(0, pool.Count)]);
    }

    /// <summary>Adds years to a signed year, passing over the missing year 0.</summary>
    private static int AddYears(int year, int offset)
    {
        int result = year + offset;
        if (year > 0 && result <= 0)
            result -= 1;
        else if (year < 0 && result >= 0)
            result += 1;
        return result;
    }
}
