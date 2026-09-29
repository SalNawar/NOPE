using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Investigation app's Reference tab (the PC redesign AP5, FO9, §2.6): a
/// chip per reference book of the library, the chosen book's cover when its
/// art exists (SlotArt.CoverFor), and the book's Register (Form_Register,
/// TC-911 to TC-916 by the book's place in the library, on a FormPage cloned
/// per book at the pane's width): the edition line, then a table of PLACE,
/// ERA, VALUE and NOTE over today's places (ReferenceRows): the claimed
/// place's row first, noted "Claimed"; a row whose value history changed
/// noted "Revised" (FactTable.IsChanged; the value and the evidence stay
/// canonical); the Costume Guide (a book with groupByEra) under era heading
/// rows, the claimed era first, the present last. "Claimed place only" (on
/// at each case's start, AP8) shows the claimed row alone. Each row is a
/// pick, the same as ever (EvidencePicks.ForBookRow, PickKeys.BookRow), lit
/// while its key is picked (CM3). The books are built on the first case; the
/// register works between travellers too (a day source), and a register is
/// drawn when its book is chosen (the others wait). A link, Back or a dock
/// side reveals a book row (Reveal: its book chosen, "Claimed place only"
/// lifted when it hides the row, the row outlined and scrolled to the
/// middle). Each row is marked with its key for the keys, the copy and the
/// pins (AppRow). Each pane has one; DayReference fills them all. Its item is
/// the chosen book ("bookof:Currency", IAppItems), which a jump to it names as
/// the target's key.
/// </summary>
public sealed class ReferenceView : AppView, IAppItems
{
    /// <summary>A book's register page (inactive), cloned per book.</summary>
    [SerializeField] private FormPage pageTemplate;

    /// <summary>The Register's page kind (Form_Register, TC-911; each book counts on from it).</summary>
    [SerializeField] private FormSpecSO registerForm;

    /// <summary>"Claimed place only": on, the register shows the claimed row alone.</summary>
    [SerializeField] private Toggle claimedOnly;

    /// <summary>The chosen book's cover (inactive until the book's cover art is found).</summary>
    [SerializeField] private Image cover;

    private readonly List<ReferenceBookSO> _books = new List<ReferenceBookSO>();
    private readonly List<FormPage> _pages = new List<FormPage>();
    private readonly List<IReadOnlyList<ReferenceLine>> _lines = new List<IReadOnlyList<ReferenceLine>>();
    private readonly List<bool> _dirty = new List<bool>();
    private readonly List<string> _foundKeys = new List<string>();
    private readonly List<AppChip> _chips = new List<AppChip>();
    private readonly List<string> _eraOrder = new List<string>();
    private readonly Dictionary<string, string> _eraNames = new Dictionary<string, string>();
    private readonly List<(FormSlot slot, Button button)> _armed = new List<(FormSlot, Button)>();
    private FactTable _facts;
    private CompareController _compare;
    private AgencyContent _agency;
    private int _day = 1;
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

    /// <inheritdoc />
    public string ItemKey => _selected >= 0 ? EntryKeys.Book(_books[_selected].category) : null;

    /// <inheritdoc />
    public string ItemTitle => _selected >= 0 ? _books[_selected].displayName : null;

    /// <summary>The books, in their chips' order (a book's place is its item in the search index).</summary>
    public IReadOnlyList<ReferenceBookSO> Books => _books;

    /// <summary>Today's facts and the compare the rows pick into; the registers are drawn again.</summary>
    public void SetFacts(FactTable facts, CompareController compare)
    {
        _facts = facts;
        _compare = compare;
        Redraw();
    }

    /// <summary>The agency block the registers print and today's day number (the edition line); the registers are drawn again.</summary>
    public void SetAgency(AgencyContent agency, int day)
    {
        _agency = agency;
        _day = day;
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
            FormPage page = Instantiate(pageTemplate, pageTemplate.transform.parent);
            page.gameObject.name = "Book_" + book.category;
            page.gameObject.SetActive(false);
            int index = _books.Count;
            if (page.Form != null)
            {
                page.Form.SlotClicked += slot => Pick(index, slot);
                page.Redrawn += () => MarkRows(index);
            }
            _books.Add(book);
            _pages.Add(page);
            _lines.Add(System.Array.Empty<ReferenceLine>());
            _dirty.Add(true);
            _foundKeys.Add(null);
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

    /// <inheritdoc />
    public override void Select(int index)
    {
        _selected = index >= 0 && index < _pages.Count ? index : -1;
        for (int i = 0; i < _pages.Count; i++)
            if (_pages[i] != null && _pages[i].gameObject.activeSelf != (i == _selected))
                _pages[i].gameObject.SetActive(i == _selected);
        if (_selected >= 0 && _dirty[_selected])
            DrawBook(_selected);
        if (cover != null)
        {
            Sprite art = _selected >= 0 ? SlotArt.CoverFor(_books[_selected]) : null;
            cover.sprite = art;
            cover.gameObject.SetActive(art != null);
        }
        RaiseChipsChanged();
    }

    /// <summary>
    /// Chooses the book of the target's row (else the book its item key names,
    /// else its item), lifts "Claimed place only" when it hides the row, and
    /// outlines the row, scrolled to the middle; no row: the mark clears.
    /// False when the library has no such book or the register no such row.
    /// </summary>
    public override bool Reveal(LinkTarget target)
    {
        int book = target.Item;
        bool row = PickKeys.TryBookRow(target.Key, out ClueCategory category, out _, out _);
        if (row || EntryKeys.TryBook(target.Key, out category))
        {
            book = _books.FindIndex(b => b.category == category);
            if (book < 0)
                return false;
        }
        if (book >= 0)
            Select(book);
        if (_selected < 0 || _selected >= _pages.Count)
            return book < 0;

        if (row && RegisterPage.LineOf(_lines[_selected], target.Key) < 0 && claimedOnly != null && claimedOnly.isOn)
            claimedOnly.isOn = false;
        int line = row ? RegisterPage.LineOf(_lines[_selected], target.Key) : -1;
        _foundKeys[_selected] = line >= 0 ? target.Key : null;
        _pages[_selected].Reveal(SlotOfLine(_selected, line));
        return line >= 0 || !row;
    }

    /// <summary>Every register is stale (the facts, the claim, the toggle or the day changed): the chosen one is drawn now, the others when chosen.</summary>
    private void Redraw()
    {
        for (int i = 0; i < _dirty.Count; i++)
            _dirty[i] = true;
        if (_selected >= 0)
            DrawBook(_selected);
    }

    /// <summary>Book <paramref name="index"/>'s register from today's rows, the claim and the toggle: its number counts on from the form's, its title is the book's.</summary>
    private void DrawBook(int index)
    {
        _dirty[index] = false;
        FormPage page = _pages[index];
        if (page == null || page.Form == null || registerForm == null)
            return;
        ReferenceBookSO book = _books[index];
        bool only = claimedOnly != null && claimedOnly.isOn;
        IReadOnlyList<FactRow> rows = _facts != null ? _facts.Rows(book.category) : null;
        _lines[index] = ReferenceRows.Arrange(rows, _claimedNation, _claimedEra, only, book.groupByEra, _eraOrder, _presentEra);

        FormData data = registerForm.Page(_agency);
        data.FormNumber = RegisterPage.FormNumber(registerForm.form.formNumber, index);
        data.Title = book.displayName;
        data.Text = new Dictionary<string, string> { { RegisterPage.EditionSlot, UiText.Format("book.edition", _day) } };
        data.Rows = new Dictionary<string, IReadOnlyList<string[]>>
        {
            { RegisterPage.RowsSlot, RegisterPage.Rows(_lines[index], EraName, f => _facts != null && _facts.IsChanged(f.NationId, f.EraId, f.Category),
                                                       UiText.Get("book.claimedNote"), UiText.Get("book.revisedNote")) }
        };
        page.Form.Bind(_compare, slot => TryFact(index, slot, out FactRow fact) ? PickKeys.BookRow(fact.Category, fact.NationId, fact.EraId) : null);
        page.Show(registerForm.form, data, slot => TryFact(index, slot, out _) && _compare != null);
        MarkRows(index);
    }

    /// <summary>An era's name by its id (null when the library does not know it).</summary>
    private string EraName(string eraId) => eraId != null && _eraNames.TryGetValue(eraId, out string name) ? name : null;

    /// <summary>The fact row a slot of book <paramref name="index"/>'s register shows; false for a heading or any other slot.</summary>
    private bool TryFact(int index, FormSlot slot, out FactRow fact)
    {
        fact = default;
        IReadOnlyList<ReferenceLine> lines = _lines[index];
        if (slot.Source != RegisterPage.RowsSlot || slot.Row < 0 || slot.Row >= lines.Count || lines[slot.Row].IsHeading)
            return false;
        fact = lines[slot.Row].Row;
        return true;
    }

    /// <summary>The slot of line <paramref name="line"/> of book <paramref name="index"/>'s register, or -1.</summary>
    private int SlotOfLine(int index, int line)
    {
        PlacedForm placed = line >= 0 ? _pages[index].Placed : null;
        if (placed != null)
            for (int s = 0; s < placed.Slots.Count; s++)
                if (placed.Slots[s].Source == RegisterPage.RowsSlot && placed.Slots[s].Row == line)
                    return s;
        return -1;
    }

    /// <summary>Marks each row of book <paramref name="index"/>'s register with its key and title for the keys, the copy and the pins, and outlines the row a link went to; again after every redraw.</summary>
    private void MarkRows(int index)
    {
        FormPage page = _pages[index];
        if (page == null || page.Form == null)
            return;
        page.Form.ArmedSlots(_armed);
        int found = -1;
        foreach ((FormSlot slot, Button button) in _armed)
        {
            if (!TryFact(index, slot, out FactRow fact))
                continue;
            ComparePick pick = EvidencePicks.ForBookRow(_books[index], fact);
            AppRow.Mark(button.gameObject, AppTab.Reference, pick.Key, pick.Label, fact.OriginLabel, fact.Value, button);
            if (pick.Key == _foundKeys[index])
                found = slot.Index;
        }
        page.Form.MarkFound(found);
    }

    /// <summary>A row of book <paramref name="index"/>'s register was clicked: the entry goes into the compare as a truth source.</summary>
    private void Pick(int index, FormSlot slot)
    {
        if (_compare != null && TryFact(index, slot, out FactRow fact))
            _compare.Select(EvidencePicks.ForBookRow(_books[index], fact), null);
    }
}
