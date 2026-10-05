using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>The shelf's groups, in their order on the shelf (a hairline parts two groups; the books sit behind one Books menu). Not serialized.</summary>
public enum ShelfGroup
{
    /// <summary>The traveller's papers and the conversation's transcript.</summary>
    Traveller,

    /// <summary>The agency's Citizen records, today's rules and the calendar.</summary>
    Agency,

    /// <summary>The reference books (the Books menu).</summary>
    Books
}

/// <summary>One document on the shelf: its group, its name, where it opens and whether it can be read yet.</summary>
public readonly struct ShelfItem
{
    /// <summary>A document.</summary>
    public ShelfItem(ShelfGroup group, string label, LinkTarget target, bool available)
    {
        Group = group;
        Label = label;
        Target = target;
        Available = available;
    }

    /// <summary>Its group (the books go in the Books menu).</summary>
    public ShelfGroup Group { get; }

    /// <summary>Its name as the chip shows it.</summary>
    public string Label { get; }

    /// <summary>Where it opens (its source and item).</summary>
    public LinkTarget Target { get; }

    /// <summary>False for a paper not scanned yet: the chip is dimmed and opening it shows why.</summary>
    public bool Available { get; }
}

/// <summary>
/// The document shelf (the PC workbench spec IA4, IA5, polished in wave 5
/// A3; lesson D5: every document is held against every other the same way):
/// one calm row, never wrapping. The traveller's group (a chip per paper the
/// day issues, then the transcript), a hairline, the agency's (Citizen
/// records, today's rules, the calendar), a hairline, and one Books chip that
/// opens the Books menu: a short list under it with a row per reference book
/// (the Seal Register among them). When the row is short of room the papers'
/// chips give way (their long names cut with an ellipsis); every other chip
/// keeps its whole name; nothing wraps. A
/// chip (or a book's row) reads the document's name, the side it is open on
/// ("Left", "Right") and a small dot while something waits in it; a paper not
/// readable yet is dimmed; the Books chip names the book open on a side and
/// carries its side. A click opens the document on the target side (Opened;
/// the app swaps it with the other side when it is open there) and closes the
/// menu; a click elsewhere, Escape or the Books chip again closes it too. The
/// app says what is on the shelf (Show) and marks it (Mark).
/// </summary>
public sealed class ShelfView : MonoBehaviour
{
    /// <summary>The row the chips sit in (a HorizontalLayoutGroup).</summary>
    [SerializeField] private RectTransform row;

    /// <summary>A chip (inactive): its Label, its Unread dot, its Side plate with its Text.</summary>
    [SerializeField] private Button chipTemplate;

    /// <summary>The hairline cloned between two groups (inactive).</summary>
    [SerializeField] private GameObject dividerTemplate;

    /// <summary>The Books chip at the row's end: its Label, its Unread dot, its Side plate with its Text.</summary>
    [SerializeField] private Button booksButton;

    /// <summary>The Books menu (inactive until opened; above the work area).</summary>
    [SerializeField] private RectTransform booksMenu;

    /// <summary>The menu's list, where the books' rows go.</summary>
    [SerializeField] private RectTransform booksList;

    /// <summary>A book's row in the menu (inactive): its Label, its Unread dot, its Side plate with its Text.</summary>
    [SerializeField] private Button bookRowTemplate;

    private readonly List<ShelfItem> _items = new List<ShelfItem>();
    private readonly List<Button> _chips = new List<Button>();
    private readonly List<GameObject> _made = new List<GameObject>();
    private readonly Vector3[] _corners = new Vector3[4];
    private bool _wired;

    /// <summary>Raised when a chip or a book's row is clicked.</summary>
    public event Action<ShelfItem> Opened;

    /// <summary>The documents' chips and the books' rows, in the order of <see cref="Items"/> (a book's row is in the Books menu).</summary>
    public IReadOnlyList<Button> Chips => _chips;

    /// <summary>The documents shown, in order.</summary>
    public IReadOnlyList<ShelfItem> Items => _items;

    /// <summary>True while the Books menu is open (Escape closes it first).</summary>
    public bool BooksOpen => booksMenu != null && booksMenu.gameObject.activeSelf;

    /// <summary>What the keys walk on the shelf, in reading order: the row's chips, the Books chip, then the open menu's rows.</summary>
    public IEnumerable<Button> Buttons
    {
        get
        {
            for (int i = 0; i < _chips.Count && i < _items.Count; i++)
                if (_items[i].Group != ShelfGroup.Books)
                    yield return _chips[i];
            if (booksButton != null)
                yield return booksButton;
            for (int i = 0; i < _chips.Count && i < _items.Count; i++)
                if (_items[i].Group == ShelfGroup.Books)
                    yield return _chips[i];
        }
    }

    private void Awake() => Wire();

    /// <summary>The templates hidden and the Books chip wired (once).</summary>
    private void Wire()
    {
        if (_wired)
            return;
        _wired = true;
        foreach (Component template in new Component[] { chipTemplate, bookRowTemplate })
            if (template != null)
                template.gameObject.SetActive(false);
        if (dividerTemplate != null)
            dividerTemplate.SetActive(false);
        if (booksButton != null)
            booksButton.onClick.AddListener(() => ShowBooks(!BooksOpen));
        if (booksMenu != null)
            booksMenu.gameObject.SetActive(false);
    }

    /// <summary>Puts <paramref name="items"/> on the shelf (in the order given; a hairline where the group changes; the books in the Books menu).</summary>
    public void Show(IReadOnlyList<ShelfItem> items)
    {
        Wire();
        foreach (GameObject made in _made)
            if (made != null)
            {
                made.SetActive(false); // out of this frame's layout (Destroy waits for the frame's end)
                Destroy(made);
            }
        _made.Clear();
        _chips.Clear();
        _items.Clear();
        if (items != null)
            _items.AddRange(items);

        bool anyBook = false;
        ShelfGroup? group = null;
        for (int i = 0; i < _items.Count; i++)
        {
            ShelfItem item = _items[i];
            bool book = item.Group == ShelfGroup.Books;
            anyBook |= book;
            if (!book && group.HasValue && group.Value != item.Group && dividerTemplate != null)
                Made(Instantiate(dividerTemplate, row)).name = "Divider_" + item.Group;
            if (!book)
                group = item.Group;
            Button chip = Instantiate(book ? bookRowTemplate : chipTemplate, book ? booksList : row);
            chip.gameObject.name = (book ? "Book_" : "Chip_") + i;
            Made(chip.gameObject);
            chip.transform.Find("Label").GetComponent<TMP_Text>().text = item.Label;
            if (chip.TryGetComponent(out CanvasGroup dim))
                dim.alpha = item.Available ? 1f : 0.55f;
            int index = i;
            chip.onClick.AddListener(() =>
            {
                ShowBooks(false);
                Opened?.Invoke(_items[index]);
            });
            _chips.Add(chip);
        }
        if (booksButton != null)
        {
            if (anyBook && group.HasValue && dividerTemplate != null)
                Made(Instantiate(dividerTemplate, row)).name = "Divider_Books";
            booksButton.transform.SetAsLastSibling();
            booksButton.gameObject.SetActive(anyBook);
        }
        if (!anyBook)
            ShowBooks(false);
        Hold();
    }

    /// <summary>
    /// Marks each chip and book row: the side it is open on
    /// (<paramref name="sideOf"/>: "Left", "Right" or null) and its dot
    /// (<paramref name="unread"/>); the Books chip names the book open on a
    /// side (<paramref name="booksLabel"/> formats it) and carries that side,
    /// and dots while a book's row is dotted.
    /// </summary>
    public void Mark(Func<ShelfItem, string> sideOf, Func<ShelfItem, bool> unread, Func<string, string> booksLabel)
    {
        string bookSide = null, bookName = null;
        bool bookDot = false;
        for (int i = 0; i < _chips.Count && i < _items.Count; i++)
        {
            string side = sideOf != null ? sideOf(_items[i]) : null;
            bool on = unread != null && unread(_items[i]);
            Tag(_chips[i].transform, side, on);
            if (_items[i].Group != ShelfGroup.Books)
                continue;
            bookDot |= on;
            if (side != null && bookSide == null)
            {
                bookSide = side;
                bookName = _items[i].Label;
            }
        }
        if (booksButton != null)
        {
            Tag(booksButton.transform, bookSide, bookDot);
            TMP_Text label = booksButton.transform.Find("Label").GetComponent<TMP_Text>();
            string text = booksLabel != null ? booksLabel(bookName) : bookName;
            if (label.text != text)
                label.text = text;
        }
        Hold();
    }

    /// <summary>Every chip but a paper's keeps its whole width (its least width is its preferred one); a paper's chip may give way to the template's least width. The row is laid out again.</summary>
    private void Hold()
    {
        float paperMin = chipTemplate != null && chipTemplate.TryGetComponent(out LayoutElement template) ? template.minWidth : 0f;
        for (int i = 0; i < _chips.Count && i < _items.Count; i++)
            if (_items[i].Group != ShelfGroup.Books)
                HoldWidth(_chips[i], _items[i].Target.Tab == AppTab.Documents ? paperMin : -1f);
        if (booksButton != null && booksButton.gameObject.activeSelf)
            HoldWidth(booksButton, -1f);
        if (row != null)
            LayoutRebuilder.MarkLayoutForRebuild(row);
    }

    /// <summary>A chip's least width: <paramref name="min"/>, or (negative) its own preferred width now.</summary>
    private static void HoldWidth(Button chip, float min)
    {
        if (!chip.TryGetComponent(out LayoutElement size) || !chip.TryGetComponent(out HorizontalLayoutGroup content))
            return;
        if (min < 0f)
        {
            if (!chip.gameObject.activeInHierarchy)
                return; // measured again when the shelf shows (the app marks it then)
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)chip.transform);
            min = content.preferredWidth;
        }
        if (!Mathf.Approximately(size.minWidth, min))
            size.minWidth = min;
    }

    /// <summary>Opens the Books menu under the Books chip (its right edge on the chip's), or closes it.</summary>
    public void ShowBooks(bool open)
    {
        Wire();
        if (booksMenu == null)
            return;
        open &= booksButton != null && booksButton.gameObject.activeInHierarchy;
        if (open)
        {
            booksButton.GetComponent<RectTransform>().GetWorldCorners(_corners);
            booksMenu.position = _corners[3];
            booksMenu.anchoredPosition += new Vector2(0f, -6f);
            booksMenu.SetAsLastSibling();
        }
        if (booksMenu.gameObject.activeSelf != open)
            booksMenu.gameObject.SetActive(open);
    }

    /// <summary>True when <paramref name="pressed"/> is the Books chip or inside the menu (a press elsewhere closes it).</summary>
    public bool IsPart(GameObject pressed)
    {
        for (Transform t = pressed != null ? pressed.transform : null; t != null; t = t.parent)
            if ((booksMenu != null && t == booksMenu) || (booksButton != null && t == booksButton.transform))
                return true;
        return false;
    }

    /// <summary>A chip's side plate and dot.</summary>
    private static void Tag(Transform chip, string side, bool dot)
    {
        Transform plate = chip.Find("Side");
        if (plate != null)
        {
            if (plate.gameObject.activeSelf != (side != null))
                plate.gameObject.SetActive(side != null);
            if (side != null)
                plate.Find("Text").GetComponent<TMP_Text>().text = side;
        }
        Transform mark = chip.Find("Unread");
        if (mark != null && mark.gameObject.activeSelf != dot)
            mark.gameObject.SetActive(dot);
    }

    /// <summary>Keeps <paramref name="made"/> to destroy at the next Show, shown.</summary>
    private GameObject Made(GameObject made)
    {
        made.SetActive(true);
        _made.Add(made);
        return made;
    }
}
