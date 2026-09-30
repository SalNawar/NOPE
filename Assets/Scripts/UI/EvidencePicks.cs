/// <summary>
/// The one builder of compare picks (piece 10 E4): every pickable value, on
/// the PC or at the desk, becomes a ComparePick here: its key (PickKeys, so
/// the same value is one pick on every surface), the bar's label, the text the
/// bar shows (the canonical value, or piece 9's placeholder for an
/// untranslated answer) and the canonical evidence. Its labels come from
/// UiText and an answer's shown text from CaseTranslation, so it lives with
/// them; the keys and the evidence are the tested PickKeys and CompareEvidence.
/// </summary>
public static class EvidencePicks
{
    /// <summary>A document's field row (the scanned page's or the desk paper's): "Travel Passport · Coin of Issue", its value as it is (papers are always English); a photo shows as the photo (its value is who it shows, never printed; the document design spec, D8).</summary>
    public static ComparePick ForField(int document, DocumentRow row, string documentName)
    {
        DocumentField f = row.Field;
        return new ComparePick(PickKeys.Field(document, row.Index),
            UiText.Format("document.compareLabel", documentName, f.label),
            f.category == ClueCategory.Photo ? UiText.Get("compare.photoShown") : f.value,
            CompareEvidence.FromDocumentField(f, document));
    }

    /// <summary>The traveller's face (the look menu; the document design spec, D8): "Traveller · FACE", shown as the person at the desk, its evidence who they are (Looks.IdentityKey), the truth a photo is held against.</summary>
    public static ComparePick ForFace(TravellerLook look) =>
        new ComparePick(PickKeys.Face, UiText.Format("compare.travellerLabel", UiText.Get("compare.faceSlot")), UiText.Get("compare.faceShown"),
                        CompareEvidence.ForPerson(Looks.IdentityKey(look)));

    /// <summary>A Seal Register row (the document design spec, D4): "Seal Register · Visa Office", the office's true seal as its value (Seals.Describe's words), a truth source for that office's papers.</summary>
    public static ComparePick ForSeal(ReferenceBookSO book, AgencyOffice office, string seal) =>
        new ComparePick(PickKeys.Seal(office.id),
            UiText.Format("book.compareLabel", book != null ? book.displayName : UiText.Get("book.untitled"), office.name),
            seal,
            CompareEvidence.ForSealRow(seal, office.id, office.name));

    /// <summary>A traveller's answer (the transcript's row or the bubble's line <paramref name="lineIndex"/> of the transcript): "Traveller · CURRENCY"; an untranslated answer shows the placeholder.</summary>
    public static ComparePick ForAnswer(int lineIndex, DialogLine line, CaseTranslation tr)
    {
        tr = tr ?? CaseTranslation.None;
        return new ComparePick(PickKeys.Line(lineIndex),
            UiText.Format("compare.travellerLabel", UiText.Category(line.Category)),
            tr.Shown(line),
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

    /// <summary>An evidence row of a Citizen Record: "Records · Born", keyed by the record (PickKeys.Record), a truth source for that person only (DiscrepancyLog.Prove).</summary>
    public static ComparePick ForRecord(CitizenRecord record, RecordRow row) =>
        new ComparePick(PickKeys.Record(row.Category, record.Id), UiText.Format("records.compareLabel", row.Label), row.Value,
                        CompareEvidence.ForRecordField(row.Category, row.Value, record.FullName));
}
