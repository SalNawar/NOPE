/// <summary>
/// The one format for a traveller's origin, as the claim line, the reference
/// books and Citizen Records show it: "Abbasid Baghdad (Medieval)".
/// </summary>
public static class OriginLabels
{
    /// <summary>"Place (Era)", or just the place when the era is blank.</summary>
    public static string Format(string place, string era) =>
        string.IsNullOrWhiteSpace(era) ? place : $"{place} ({era})";

    /// <summary>The place of a label Format made ("Abbasid Baghdad" of "Abbasid Baghdad (Medieval)" and "Medieval"); a label not ending in that era is returned whole.</summary>
    public static string Place(string label, string era)
    {
        label ??= string.Empty;
        string suffix = string.IsNullOrWhiteSpace(era) ? null : $" ({era})";
        return suffix != null && label.EndsWith(suffix, System.StringComparison.Ordinal) ? label.Substring(0, label.Length - suffix.Length) : label;
    }
}
