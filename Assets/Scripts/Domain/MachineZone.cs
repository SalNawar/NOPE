using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

/// <summary>
/// A passport's machine-readable zone (the travel documents spec, TD3): two
/// lines of 36 characters in the style of ICAO 9303's TD2 zone, built from what
/// the page prints (its holder's name, the passport number, the birth date and
/// the expiry), so the zone repeats the page and never contradicts it (it is
/// not a check of its own: a forger who changes a printed value changes the
/// zone with it). Line 1: P, a filler, the issuing nation's three-letter code
/// and the name. Line 2: the number and its check digit, the nationality, the
/// birth date (YYMMDD) and its check digit, the sex (unspecified), the expiry
/// and its check digit, filler and the closing check digit. Letters are
/// capitals without accents; every other character is the filler '&lt;'.
/// Pure, so it is tested headless.
/// </summary>
public static class MachineZone
{
    /// <summary>A line's length (TD2).</summary>
    public const int LineLength = 36;

    /// <summary>The filler.</summary>
    public const char Filler = '<';

    /// <summary>The passport number's room on line 2.</summary>
    private const int NumberLength = 9;

    /// <summary>A date's six digits when it does not read as a date.</summary>
    private const string NoDate = "<<<<<<";

    /// <summary>The two lines for a passport of nation <paramref name="code"/> ("EGY") held by <paramref name="name"/>, numbered <paramref name="number"/>, born <paramref name="birth"/> and valid until <paramref name="expiry"/> (dates as the papers print them: "3 May 2101").</summary>
    public static IReadOnlyList<string> Lines(string code, string name, string number, string birth, string expiry)
    {
        string nation = Fit(code, 3);
        string line1 = Fit("P" + Filler + nation + Letters(name, LineLength), LineLength);

        string doc = Fit(Alnum(number), NumberLength);
        string born = Date(birth), until = Date(expiry);
        var line2 = new StringBuilder();
        line2.Append(doc).Append(Check(doc));
        line2.Append(nation);
        line2.Append(born).Append(Check(born));
        line2.Append(Filler);
        line2.Append(until).Append(Check(until));
        string body = line2.ToString();
        string closing = body.Substring(0, NumberLength + 1) + born + Check(born) + until + Check(until);
        string full = Fit(body, LineLength - 1) + Check(closing);
        return new[] { line1, full };
    }

    /// <summary>
    /// The check digit of <paramref name="field"/> (ICAO 9303): digits count
    /// as themselves, A to Z as 10 to 35, the filler as 0, weighted 7, 3, 1 in
    /// turn; the sum's last digit.
    /// </summary>
    public static char Check(string field)
    {
        int[] weights = { 7, 3, 1 };
        int sum = 0;
        for (int i = 0; i < (field ?? string.Empty).Length; i++)
        {
            char c = field[i];
            int v = c >= '0' && c <= '9' ? c - '0' : c >= 'A' && c <= 'Z' ? c - 'A' + 10 : 0;
            sum += v * weights[i % 3];
        }
        return (char)('0' + sum % 10);
    }

    /// <summary>A date the papers print ("3 May 2101", "8 Feb 466 BCE") as YYMMDD (the year's last two digits); the filler for one that does not read.</summary>
    public static string Date(string text)
    {
        string[] parts = (text ?? string.Empty).Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3 || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int day)
            || !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int year))
            return NoDate;
        int month = Array.FindIndex(CultureInfo.InvariantCulture.DateTimeFormat.AbbreviatedMonthNames,
                                    m => m.Length > 0 && parts[1].StartsWith(m, StringComparison.OrdinalIgnoreCase)) + 1;
        if (month < 1 || day < 1 || day > 31)
            return NoDate;
        return (Math.Abs(year) % 100).ToString("00", CultureInfo.InvariantCulture) + month.ToString("00", CultureInfo.InvariantCulture) + day.ToString("00", CultureInfo.InvariantCulture);
    }

    /// <summary>A name in the zone's letters: capitals without accents, every word parted by the filler, up to <paramref name="max"/> characters.</summary>
    private static string Letters(string name, int max)
    {
        var sb = new StringBuilder();
        foreach (char c in (name ?? string.Empty).Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;
            char u = char.ToUpperInvariant(c);
            sb.Append(u >= 'A' && u <= 'Z' ? u : Filler);
            if (sb.Length >= max)
                break;
        }
        return sb.ToString();
    }

    /// <summary>The capitals and digits of <paramref name="text"/> (the passport number without its dashes).</summary>
    private static string Alnum(string text)
    {
        var sb = new StringBuilder();
        foreach (char c in (text ?? string.Empty).ToUpperInvariant())
            if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))
                sb.Append(c);
        return sb.ToString();
    }

    /// <summary><paramref name="text"/> cut or filled with the filler to exactly <paramref name="length"/> characters.</summary>
    private static string Fit(string text, int length)
    {
        text ??= string.Empty;
        return text.Length >= length ? text.Substring(0, length) : text + new string(Filler, length - text.Length);
    }
}
