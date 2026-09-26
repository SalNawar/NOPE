using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Renders a reference book as a flippable window. Its rows are today's facts
/// for the book's category (FactTable.Rows), each a clickable "place : value"
/// row the player can compare against a traveller's statement; a row whose
/// value history changed shows "[revised]" after its place (FactTable.IsChanged;
/// the value and the evidence stay canonical). A book grouped by era (the
/// Costume Guide, traveller types C3) lists the current traveller's claimed
/// row first, tagged "(claimed)", then its places under era headings, the
/// claimed era first (BookLines.Arrange); a heading is not clickable. Paging
/// and row cloning are PagedRowsWindow's.
/// </summary>
public sealed class ReferenceBookWindowController : PagedRowsWindow
{
    private ReferenceBookSO _book;
    private FactTable _facts;
    private CompareController _compare;

    /// <summary>The library's eras in chronological order (the era headings' names).</summary>
    private IReadOnlyList<EraSO> _eras = System.Array.Empty<EraSO>();

    /// <summary>The current traveller's claimed place (a grouped book lists its row first); null before the first traveller.</summary>
    private string _claimNationId, _claimEraId;

    /// <summary>The page's lines (rows and era headings), rebuilt when the book or the claim changes.</summary>
    private List<BookLine> _lines = new List<BookLine>();

    /// <summary>Binds a book cover to today's facts and the library's eras (the Costume Guide's headings) and renders its first page.</summary>
    public void SetBook(ReferenceBookSO book, FactTable facts, CompareController compare, IReadOnlyList<EraSO> eras)
    {
        _book = book;
        _facts = facts;
        _compare = compare;
        _eras = (eras ?? System.Array.Empty<EraSO>()).Where(e => e != null).OrderBy(e => e.order).ToList();

        SetTitle(book != null ? book.displayName : UiText.Get("book.untitled"));
        Arrange();
    }

    /// <summary>Sets the traveller's claimed place; a book grouped by era re-lists its rows with the claimed one first.</summary>
    public void SetClaim(string nationId, string eraId)
    {
        _claimNationId = nationId;
        _claimEraId = eraId;
        if (_book != null && _book.groupByEra)
            Arrange();
    }

    /// <summary>Rebuilds the lines from today's rows (BookLines.Arrange) and shows the first page.</summary>
    private void Arrange()
    {
        IReadOnlyList<FactRow> rows = _book != null && _facts != null ? _facts.Rows(_book.category) : System.Array.Empty<FactRow>();
        _lines = BookLines.Arrange(rows, _book != null && _book.groupByEra, _eras.Select(e => e.id).ToList(), _claimNationId, _claimEraId);
        ShowPage(0);
    }

    /// <inheritdoc />
    protected override int RowCount => _lines.Count;

    /// <summary>Shows an era heading (not clickable), or a place and its value, the claimed one tagged; a row's click puts the entry into the compare bar as a truth source.</summary>
    protected override void FillRow(int index, GameObject row, TMP_Text[] texts, Image background, Button button)
    {
        BookLine line = _lines[index];
        if (line.IsHeading)
        {
            EraSO era = _eras.FirstOrDefault(e => e.id == line.HeadingEraId);
            if (texts.Length > 0 && texts[0] != null)
                texts[0].text = UiText.Format("book.eraHeading", (era != null ? era.displayName : line.HeadingEraId).ToUpperInvariant());
            if (texts.Length > 1 && texts[1] != null)
                texts[1].text = string.Empty;
            if (button != null)
                button.interactable = false;
            return;
        }

        FactRow fact = line.Row;
        string place = _facts.IsChanged(fact.NationId, fact.EraId, fact.Category)
            ? UiText.Format("book.revisedLabel", fact.OriginLabel)
            : fact.OriginLabel;
        if (texts.Length > 0 && texts[0] != null)
            texts[0].text = line.IsClaimed ? UiText.Format("book.claimedLabel", place) : place;
        if (texts.Length > 1 && texts[1] != null)
            texts[1].text = fact.Value;

        ComparePick pick = EvidencePicks.ForBookRow(_book, fact);
        if (button != null && _compare != null)
            button.onClick.AddListener(() => _compare.Select(pick, new ImageHighlight(background)));
    }
}
