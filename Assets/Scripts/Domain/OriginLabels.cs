/// <summary>
/// The one format for a traveller's origin, as the claim line, the reference
/// books and Citizen Records show it: "Abbasid Baghdad (Medieval)".
/// </summary>
public static class OriginLabels
{
    /// <summary>"Place (Era)", or just the place when the era is blank.</summary>
    public static string Format(string place, string era) =>
        string.IsNullOrWhiteSpace(era) ? place : $"{place} ({era})";
}
