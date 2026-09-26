using TMPro;

/// <summary>
/// Writes one document row (DocumentRows) into its label and value texts, for
/// the scanned page on the PC and the desk paper alike (piece 10 E2), both as
/// they are: every document is filled in English (the redesign's F5), so a
/// paper never shows a tongue and never flips.
/// </summary>
public static class DocumentRowView
{
    /// <summary>Writes <paramref name="row"/>'s label and value; null texts are skipped.</summary>
    public static void Write(TMP_Text label, TMP_Text value, DocumentRow row)
    {
        DocumentField f = row.Field;
        if (label != null)
            label.text = f.label;
        if (value != null)
            value.text = f.value;
    }
}
