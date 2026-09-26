using System;
using System.Collections.Generic;

/// <summary>
/// The neutral present as authored (world_source.json "present"; traveller
/// types H1), written by Generate World: the present while no nation leads
/// history (ContentLibrarySO.BuildPresent). Its facts are unique among the
/// places, and its Culture fact is derived from its wardrobe's signature
/// items, as a place's is.
/// </summary>
[Serializable]
public sealed class PresentContent
{
    /// <summary>Its name ("Temporal Customs Zone"); its label adds the office's era, as a place's does.</summary>
    public string displayName = string.Empty;

    /// <summary>Its year (2150): a 2150 citizen's age is counted from it.</summary>
    public int year;

    /// <summary>The earliest birth year of a 2150 citizen: the year minus the oldest traveller age (as a place's).</summary>
    public int birthYearMin;

    /// <summary>The latest birth year of a 2150 citizen: the year minus the youngest traveller age.</summary>
    public int birthYearMax;

    /// <summary>Its facts, one per category (Currency, Language, Technology, Geography, Politics, and the derived Culture).</summary>
    public List<ProfileFact> facts = new();
}
