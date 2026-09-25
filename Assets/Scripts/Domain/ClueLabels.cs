/// <summary>
/// The one set of upper-case category labels the player reads in reports:
/// the Deviation Report, the compare bar's notes and interview answer rows.
/// </summary>
public static class ClueLabels
{
    /// <summary>
    /// Report label of a category: Geography is CAPITAL, Politics RULER,
    /// Technology DEVICE, BirthDate BIRTH DATE, Culture DRESS, and every other
    /// category its upper-case name.
    /// </summary>
    public static string Report(ClueCategory category)
    {
        switch (category)
        {
            case ClueCategory.Geography: return "CAPITAL";
            case ClueCategory.Politics: return "RULER";
            case ClueCategory.Technology: return "DEVICE";
            case ClueCategory.BirthDate: return "BIRTH DATE";
            case ClueCategory.Culture: return "DRESS";
            default: return category.ToString().ToUpperInvariant();
        }
    }
}
