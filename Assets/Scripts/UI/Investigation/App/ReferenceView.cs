using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Investigation app's Reference tab (the PC redesign AP5, §2.6): a chip
/// per reference book of the library and the chosen book's register of
/// today's places (ReferenceRows): the claimed place's row first, flagged; the
/// Costume Guide (a book with groupByEra) under era headings, the claimed era
/// first, the present last. "Claimed place only" (on at each case's start,
/// AP8) shows the claimed row alone. The books are built on the first case;
/// the register works between travellers too (a day source). Today each
/// register is drawn by the book page component (ReferenceBookWindowController,
/// one clone of the page template per book); phase 5's FormView takes its place.
/// DayReference fills it.
/// </summary>
public sealed class ReferenceView : AppView
{
    /// <summary>A book's register page (inactive), cloned per book.</summary>
    [SerializeField] private ReferenceBookWindowController pageTemplate;

    /// <summary>"Claimed place only": on, the register shows the claimed row alone.</summary>
    [SerializeField] private Toggle claimedOnly;

    private readonly List<ReferenceBookSO> _books = new List<ReferenceBookSO>();
    private readonly List<ReferenceBookWindowController> _pages = new List<ReferenceBookWindowController>();
    private readonly List<AppChip> _chips = new List<AppChip>();
    private readonly List<string> _eraOrder = new List<string>();
    private readonly Dictionary<string, string> _eraNames = new Dictionary<string, string>();
    private FactTable _facts;
    private CompareController _compare;
    private string _presentEra;
    private string _claimedNation;
    private string _claimedEra;
    private int _selected = -1;
    private bool _built;

    /// <inheritdoc />
    public override AppTab Tab => AppTab.Reference;

    /// <inheritdoc />
    public override IReadOnlyList<AppChip> Chips => _chips;

    /// <inheritdoc />
    public override int Selected => _selected;

    /// <summary>Today's facts and the compare the rows pick into; a built register redraws.</summary>
    public void SetFacts(FactTable facts, CompareController compare)
    {
        _facts = facts;
        _compare = compare;
        for (int i = 0; i < _pages.Count; i++)
            _pages[i].SetBook(_books[i], _facts, _compare);
        Redraw();
    }

    /// <summary>The first time only: a page and a chip per reference book of the library, the eras' order and names, the first book chosen.</summary>
    public void BuildBooks(ContentLibrarySO library)
    {
        if (_built || library == null || pageTemplate == null)
            return;
        _built = true;

        foreach (EraSO era in library.Eras.Where(e => e != null).OrderBy(e => e.order))
        {
            _eraOrder.Add(era.id);
            _eraNames[era.id] = era.displayName;
        }
        _presentEra = library.FutureEra != null ? library.FutureEra.id : null;

        foreach (ReferenceBookSO book in library.ReferenceBooks)
        {
            if (book == null)
                continue;
            ReferenceBookWindowController page = Instantiate(pageTemplate, pageTemplate.transform.parent);
            page.gameObject.name = "Book_" + book.category;
            page.gameObject.SetActive(false);
            page.SetBook(book, _facts, _compare);
            _books.Add(book);
            _pages.Add(page);
            _chips.Add(new AppChip(book.displayName, true));
        }

        if (claimedOnly != null)
            claimedOnly.onValueChanged.AddListener(_ => Redraw());
        Redraw();
        Select(_pages.Count > 0 ? 0 : -1);
    }

    /// <summary>A new case's claim (null ids for none): its row comes first, and "Claimed place only" turns on (AP8).</summary>
    public void SetClaim(string nationId, string eraId)
    {
        _claimedNation = nationId;
        _claimedEra = eraId;
        if (claimedOnly != null)
            claimedOnly.SetIsOnWithoutNotify(true);
        Redraw();
    }

    /// <summary>Shows the book of <paramref name="category"/> (a step's jump: its register keeps the claimed place's row first); nothing when no book has it.</summary>
    public void ShowBook(ClueCategory category)
    {
        int index = _books.FindIndex(b => b.category == category);
        if (index >= 0)
            Select(index);
    }

    /// <inheritdoc />
    public override void Select(int index)
    {
        _selected = index >= 0 && index < _pages.Count ? index : -1;
        for (int i = 0; i < _pages.Count; i++)
            if (_pages[i] != null && _pages[i].gameObject.activeSelf != (i == _selected))
                _pages[i].gameObject.SetActive(i == _selected);
        RaiseChipsChanged();
    }

    /// <summary>Every book's register from today's rows, the claim and the toggle.</summary>
    private void Redraw()
    {
        bool only = claimedOnly != null && claimedOnly.isOn;
        for (int i = 0; i < _pages.Count; i++)
        {
            ReferenceBookSO book = _books[i];
            IReadOnlyList<FactRow> rows = _facts != null ? _facts.Rows(book.category) : null;
            _pages[i].ShowLines(ReferenceRows.Arrange(rows, _claimedNation, _claimedEra, only, book.groupByEra, _eraOrder, _presentEra), _eraNames);
        }
    }
}
