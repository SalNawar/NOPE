using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Renders a reference book as a flippable window. Its rows are today's facts
/// for the book's category (FactTable.Rows), each a clickable "place : value"
/// row the player can compare against a traveller's statement; a row whose
/// value history changed shows "[revised]" after its place (FactTable.IsChanged;
/// the value and the evidence stay canonical). Paging and row cloning are
/// PagedRowsWindow's.
/// </summary>
public sealed class ReferenceBookWindowController : PagedRowsWindow
{
    private ReferenceBookSO _book;
    private FactTable _facts;
    private CompareController _compare;

    /// <summary>Binds a book cover to today's facts and renders its first page.</summary>
    public void SetBook(ReferenceBookSO book, FactTable facts, CompareController compare)
    {
        _book = book;
        _facts = facts;
        _compare = compare;

        SetTitle(book != null ? book.displayName : UiText.Get("book.untitled"));
        ShowPage(0);
    }

    /// <summary>Today's rows for this book (empty when unbound).</summary>
    private IReadOnlyList<FactRow> Rows() =>
        _book != null && _facts != null ? _facts.Rows(_book.category) : System.Array.Empty<FactRow>();

    /// <inheritdoc />
    protected override int RowCount => Rows().Count;

    /// <summary>Shows a place and its value; a click puts the entry into the compare bar as a truth source.</summary>
    protected override void FillRow(int index, GameObject row, TMP_Text[] texts, Image background, Button button)
    {
        FactRow fact = Rows()[index];

        if (texts.Length > 0 && texts[0] != null)
            texts[0].text = _facts.IsChanged(fact.NationId, fact.EraId, fact.Category)
                ? UiText.Format("book.revisedLabel", fact.OriginLabel)
                : fact.OriginLabel;
        if (texts.Length > 1 && texts[1] != null)
            texts[1].text = fact.Value;

        string label = UiText.Format("book.compareLabel", _book != null ? _book.displayName : UiText.Get("book.untitled"), fact.OriginLabel);
        string value = fact.Value;
        CompareEvidence evidence = fact.ToEvidence();

        if (button != null && _compare != null)
            button.onClick.AddListener(() => _compare.Select(label, value, background, evidence));
    }
}
