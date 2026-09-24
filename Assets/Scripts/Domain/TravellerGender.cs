using System.Collections.Generic;

/// <summary>A traveller's gender, as far as the agency can tell from their name.</summary>
public enum TravellerGender
{
    /// <summary>Not derivable: legendaries, "Subject #n", or a name on both lists or neither.</summary>
    Unknown,

    /// <summary>The name is on the claimed place's male list.</summary>
    Male,

    /// <summary>The name is on the claimed place's female list.</summary>
    Female
}

/// <summary>
/// Derives a traveller's gender from which of the claimed place's name lists
/// their given name came from, so it costs no draw. Pure, so it is tested headless.
/// </summary>
public static class TravellerGenders
{
    /// <summary>
    /// Male or Female when <paramref name="givenName"/> is on exactly one list:
    /// first as written, then without the numeral suffix NameRoster adds
    /// ("Marcus II" counts as Marcus). Both passes use the scanner comparison
    /// (trimmed, case-insensitive). A null list counts as empty; a null or blank
    /// name, or a name on both lists or on neither, is Unknown.
    /// </summary>
    public static TravellerGender FromNameLists(string givenName, IReadOnlyList<string> maleNames, IReadOnlyList<string> femaleNames)
    {
        if (string.IsNullOrWhiteSpace(givenName))
            return TravellerGender.Unknown;

        TravellerGender exact = Classify(givenName, maleNames, femaleNames, out bool found);
        if (found)
            return exact;

        return Classify(NameRoster.BaseName(givenName), maleNames, femaleNames, out _);
    }

    /// <summary>Gender by list membership; <paramref name="found"/> is true when the name is on at least one list.</summary>
    private static TravellerGender Classify(string name, IReadOnlyList<string> maleNames, IReadOnlyList<string> femaleNames, out bool found)
    {
        bool male = Contains(maleNames, name);
        bool female = Contains(femaleNames, name);
        found = male || female;
        return male == female ? TravellerGender.Unknown : male ? TravellerGender.Male : TravellerGender.Female;
    }

    /// <summary>True when the list holds the name under the scanner comparison (a null list holds nothing).</summary>
    private static bool Contains(IReadOnlyList<string> names, string name)
    {
        if (names == null)
            return false;

        foreach (string candidate in names)
            if (DiscrepancyLog.ValuesMatch(candidate, name))
                return true;

        return false;
    }
}
