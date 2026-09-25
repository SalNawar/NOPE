/// <summary>
/// The one source of the category words the player reads in reports (the
/// Deviation Report, the compare bar's notes and interview answer rows): a
/// key per category into the UI string tables, whose English words are
/// CAPITAL, RULER, DEVICE, BIRTH DATE and each other category's upper-case name.
/// </summary>
public static class ClueLabels
{
    /// <summary>The UI string key of a category's report word ("category.Geography"); the words live in world_source.json ui.strings.</summary>
    public static string Key(ClueCategory category) => "category." + category;
}
