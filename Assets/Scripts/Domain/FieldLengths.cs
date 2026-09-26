using System;

/// <summary>
/// The longest value a document field of a category can print (redesign phase
/// 4, PC spec FO6): a form's box reserves the lines this needs at the value
/// floor, and Build Office UI and the content validator check every form with
/// a probe of this length (FormLayout.Probe, FormLayout.Check). A place fact
/// and a name run to a book row (FactTable.MaxValueLength); an origin to the
/// longest of the content's origin labels (the caller's); a birth date to the
/// widest date BirthDates writes (a four-digit BCE year); the agency's numbers
/// and dates to their makers' fixed widths (AgencyNumbers, AgencyCalendar).
/// </summary>
public static class FieldLengths
{
    /// <summary>A source that draws the top of every range, so a maker writes its widest value.</summary>
    private sealed class Widest : IRandomSource
    {
        public int Range(int minInclusive, int maxExclusive) => maxExclusive - 1;

        public float Value() => 0.999f;
    }

    /// <summary>The widest day of the agency's year (a two-digit day).</summary>
    private static readonly DateTime WidestDay = new DateTime(2150, 9, 28);

    /// <summary>The longest value, in characters, a field of <paramref name="category"/> prints, where the longest origin label is <paramref name="longestOrigin"/> characters.</summary>
    public static int Longest(ClueCategory category, int longestOrigin)
    {
        switch (category)
        {
            case ClueCategory.BirthDate:
                return BirthDates.Format(28, 0, -9999).Length;
            case ClueCategory.Destination:
                return longestOrigin;
            case ClueCategory.CitizenId:
                return AgencyNumbers.DisplacementNumber(new Widest()).Length;
            case ClueCategory.Incident:
                return AgencyNumbers.IncidentNumber(WidestDay, new Widest()).Length;
            case ClueCategory.DepartureDate:
            case ClueCategory.Expiry:
                return AgencyCalendar.Write(WidestDay).Length;
            default:
                return FactTable.MaxValueLength;
        }
    }
}
