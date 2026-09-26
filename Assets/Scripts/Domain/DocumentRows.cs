/// <summary>One field of a document as a pick sees it (a box on the desk paper or on its scanned copy): the field and its index in the document's field list.</summary>
public readonly struct DocumentRow
{
    /// <summary>A row of field <paramref name="index"/>.</summary>
    public DocumentRow(int index, DocumentField field)
    {
        Index = index;
        Field = field;
    }

    /// <summary>The field's index in the document's field list (its pick key: PickKeys.Field).</summary>
    public int Index { get; }

    /// <summary>The field (never null).</summary>
    public DocumentField Field { get; }
}
