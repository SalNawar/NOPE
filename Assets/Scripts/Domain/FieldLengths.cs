/// <summary>
/// The longest value a document field of a category can print (redesign phase
/// 4, PC spec FO6): a form's box reserves the lines this needs at the value
/// floor, and Build Office UI and the content validator check every form with
/// a probe of this length (FormLayout.Probe, FormLayout.Check). A place fact
/// and a name run to a book row (FactTable.MaxValueLength); a birth date to
/// the widest date BirthDates writes (a four-digit BCE year).
/// </summary>
public static class FieldLengths
{
    /// <summary>The longest value, in characters, a field of <paramref name="category"/> prints.</summary>
    public static int Longest(ClueCategory category) =>
        category == ClueCategory.BirthDate ? BirthDates.Format(28, 0, -9999).Length : FactTable.MaxValueLength;
}
