using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Renders one reference book's register in the Investigation app's Reference
/// tab, a page at a time (PagedRowsWindow's paging and row cloning): the
/// lines ReferenceView arranges (ReferenceRows) from today's facts for the
/// book's category. A row is a clickable "place : value" row the player can
/// compare against a traveller's statement (the claimed place's reads
/// "(claimed)"; a row whose value history changed shows "[revised]" after its
/// place: FactTable.IsChanged; the value and the evidence stay canonical); an
/// era heading line (the Costume Guide) shows the era's name, with no
/// background and no click.
/// </summary>
public sealed class ReferenceBookWindowController : PagedRowsWindow
{
    private ReferenceBookSO _book;
    private FactTable _facts;
    private CompareController _compare;
    private IReadOnlyList<ReferenceLine> _lines = System.Array.Empty<ReferenceLine>();
    private IReadOnlyDictionary<string, string> _eraNames;

    /// <summary>Binds a book to today's facts and the compare its rows pick into, and titles the page.</summary>
    public void SetBook(ReferenceBookSO book, FactTable facts, CompareController compare)
    {
        _book = book;
        _facts = facts;
        _compare = compare;
        SetTitle(book != null ? book.displayName : UiText.Get("book.untitled"));
    }

    /// <summary>Shows the register's <paramref name="lines"/> from the first page (an era heading reads its name from <paramref name="eraNames"/>).</summary>
    public void ShowLines(IReadOnlyList<ReferenceLine> lines, IReadOnlyDictionary<string, string> eraNames)
    {
        _lines = lines ?? System.Array.Empty<ReferenceLine>();
        _eraNames = eraNames;
        ShowPage(0);
    }

    /// <inheritdoc />
    protected override int RowCount => _lines.Count;

    /// <summary>A heading shows its era's name; a row its place and value, and a click puts the entry into the compare bar as a truth source.</summary>
    protected override void FillRow(int index, GameObject row, TMP_Text[] texts, Image background, Button button)
    {
        ReferenceLine line = _lines[index];
        if (line.IsHeading)
        {
            string era = _eraNames != null && _eraNames.TryGetValue(line.EraHeading, out string name) ? name : line.EraHeading;
            if (texts.Length > 0 && texts[0] != null)
            {
                texts[0].text = era.ToUpperInvariant();
                texts[0].fontStyle = FontStyles.Bold;
            }
            if (texts.Length > 1 && texts[1] != null)
                texts[1].text = string.Empty;
            if (background != null)
                background.enabled = false;
            if (button != null)
                button.enabled = false;
            return;
        }

        FactRow fact = line.Row;
        if (texts.Length > 0 && texts[0] != null)
        {
            string place = _facts != null && _facts.IsChanged(fact.NationId, fact.EraId, fact.Category)
                ? UiText.Format("book.revisedLabel", fact.OriginLabel)
                : fact.OriginLabel;
            texts[0].text = line.Claimed ? UiText.Format("book.claimedLabel", place) : place;
            texts[0].fontStyle = line.Claimed ? FontStyles.Bold : FontStyles.Normal;
        }
        if (texts.Length > 1 && texts[1] != null)
            texts[1].text = fact.Value;

        ComparePick pick = EvidencePicks.ForBookRow(_book, fact);
        if (button != null && _compare != null)
            button.onClick.AddListener(() => _compare.Select(pick, new ImageHighlight(background)));
    }
}
