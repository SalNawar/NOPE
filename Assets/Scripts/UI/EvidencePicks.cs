/// <summary>
/// The one builder of compare picks (piece 10 E4): every pickable value, on
/// the PC or at the desk, becomes a ComparePick here: its key (PickKeys, so
/// the same value is one pick on every surface), the bar's label, the text the
/// bar shows (the canonical value, or piece 9's placeholder for an
/// untranslated statement) and the canonical evidence. Its labels come from
/// UiText and its shown text from CaseTranslation, so it lives with them;
/// the keys and the evidence are the tested PickKeys and CompareEvidence.
/// </summary>
public static class EvidencePicks
{
    /// <summary>A document's field row (the scanned page's or the desk paper's): "Travel Passport · Coin of Issue"; an untranslated value shows the placeholder.</summary>
    public static ComparePick ForField(int document, DocumentRow row, string documentName, CaseTranslation tr)
    {
        tr = tr ?? CaseTranslation.None;
        DocumentField f = row.Field;
        return new ComparePick(PickKeys.Field(document, row.Index),
            UiText.Format("document.compareLabel", documentName, f.label),
            tr.Shown(Translation.InTongue(f.category), tr.PapersTranslated, f.value),
            CompareEvidence.FromDocumentField(f));
    }

    /// <summary>A traveller's answer (the transcript's row or the bubble's line <paramref name="lineIndex"/> of the transcript): "Traveller · CURRENCY"; an untranslated answer shows the placeholder.</summary>
    public static ComparePick ForAnswer(int lineIndex, DialogLine line, CaseTranslation tr)
    {
        tr = tr ?? CaseTranslation.None;
        return new ComparePick(PickKeys.Line(lineIndex),
            UiText.Format("compare.travellerLabel", UiText.Category(line.Category)),
            tr.Shown(Translation.InTongue(line.Speaker), tr.SpeechTranslated, line.Value),
            CompareEvidence.ForAnswer(line.Category, line.Value, line.IsTell));
    }

    /// <summary>A garment the player looked at (the wheel's Look menu): its slot as the label, its item as the shown value, its place's Culture value as the evidence.</summary>
    public static ComparePick ForGarment(int index, Garment garment) =>
        new ComparePick(PickKeys.Garment(index),
            UiText.Format("compare.travellerLabel", UiText.Slot(garment.Slot)),
            garment.Label,
            CompareEvidence.ForAppearance(Looks.EvidenceCategory, garment.Value, garment.IsTell));

    /// <summary>A reference book's row: "Currency Ledger · Periclean Athens (Ancient)", a truth source.</summary>
    public static ComparePick ForBookRow(ReferenceBookSO book, FactRow fact) =>
        new ComparePick(PickKeys.BookRow(fact.Category, fact.NationId, fact.EraId),
            UiText.Format("book.compareLabel", book != null ? book.displayName : UiText.Get("book.untitled"), fact.OriginLabel),
            fact.Value,
            fact.ToEvidence());

    /// <summary>A Citizen Records row (its label from the app), a truth source for the name and the date of birth.</summary>
    public static ComparePick ForRecord(ClueCategory category, string label, string value) =>
        new ComparePick(PickKeys.Record(category), label, value, CompareEvidence.ForRecordField(category, value));
}
