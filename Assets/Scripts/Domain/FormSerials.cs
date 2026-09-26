/// <summary>
/// A paper's serial (redesign phase 4, PC spec FO5): "{form number}/{6 digits}"
/// ("TC-610/583021"), the digits a value of the traveller's forms seed
/// (Seeds.ForForms) and the document's index, not a draw, so no stream moves.
/// CaseFactory stores it on the document; the form prints it with its barcode.
/// </summary>
public static class FormSerials
{
    /// <summary>The serial of document <paramref name="documentIndex"/> (0-based) of a traveller whose forms seed is <paramref name="formsSeed"/>; a blank form number gives the digits alone.</summary>
    public static string Make(string formNumber, int formsSeed, int documentIndex)
    {
        string digits = ((uint)Seeds.Mix(formsSeed, documentIndex + 1) % 1000000u).ToString("D6");
        return string.IsNullOrEmpty(formNumber) ? digits : formNumber + "/" + digits;
    }
}
