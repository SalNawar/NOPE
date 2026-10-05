using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>The documents' groups (the traveller's, the agency's, the books). Not serialized.</summary>
public enum ShelfGroup
{
    /// <summary>The traveller's papers, the conversation's transcript and the papers not handed over (the Papers menu).</summary>
    Traveller,

    /// <summary>The agency's Citizen records, today's rules and the calendar (each its own menu).</summary>
    Agency,

    /// <summary>The reference books (the Books menu).</summary>
    Books
}

/// <summary>What a menu row does when clicked. Not serialized.</summary>
public enum ShelfAction
{
    /// <summary>Opens its document on the target side.</summary>
    Open,

    /// <summary>Flags a paper not handed over as missing (the desk-first redesign, item 7: the wheel then offers "Hand me your ...").</summary>
    FlagMissing,

    /// <summary>Holds today's date on the workbench, to match with an expiry or a ticket's date.</summary>
    HoldToday
}

/// <summary>The menu bar's menus, in their order on the bar. Not serialized.</summary>
public enum AppMenu
{
    /// <summary>The traveller's papers, the transcript, the papers to flag missing.</summary>
    Papers,

    /// <summary>The citizen records.</summary>
    Records,

    /// <summary>Today's rules.</summary>
    Rules,

    /// <summary>The reference books.</summary>
    Books,

    /// <summary>The calendar and today's date.</summary>
    Calendar
}

/// <summary>One row of a menu: a document (its group, name, where it opens, whether it can be read yet), a paper to flag missing, or today's date.</summary>
public readonly struct ShelfItem
{
    /// <summary>A document.</summary>
    public ShelfItem(ShelfGroup group, string label, LinkTarget target, bool available)
        : this(group, label, target, available, ShelfAction.Open, null, MissingPaperState.None)
    {
    }

    private ShelfItem(ShelfGroup group, string label, LinkTarget target, bool available, ShelfAction action, string missingId, MissingPaperState missing)
    {
        Group = group;
        Label = label;
        Target = target;
        Available = available;
        Action = action;
        MissingId = missingId;
        Missing = missing;
    }

    /// <summary>A paper of the day's papers menu not handed over (FormRequest <paramref name="requestId"/>, named <paramref name="label"/>), standing at <paramref name="state"/>: a click flags it missing.</summary>
    public static ShelfItem MissingPaper(string requestId, string label, MissingPaperState state) =>
        new ShelfItem(ShelfGroup.Traveller, label, LinkTarget.None, false, ShelfAction.FlagMissing, requestId, state);

    /// <summary>Today's date (<paramref name="label"/>): a click holds it on the workbench.</summary>
    public static ShelfItem Today(string label) =>
        new ShelfItem(ShelfGroup.Agency, label, LinkTarget.ToTab(AppTab.Calendar), true, ShelfAction.HoldToday, null, MissingPaperState.None);

    /// <summary>Its group.</summary>
    public ShelfGroup Group { get; }

    /// <summary>Its name as the row shows it.</summary>
    public string Label { get; }

    /// <summary>Where it opens (its source and item); None for a paper not handed over.</summary>
    public LinkTarget Target { get; }

    /// <summary>False for a paper not scanned yet (dimmed; opening it shows why) and a paper not handed over.</summary>
    public bool Available { get; }

    /// <summary>What a click does.</summary>
    public ShelfAction Action { get; }

    /// <summary>The request id of a paper not handed over (FormRequest.Id), else null.</summary>
    public string MissingId { get; }

    /// <summary>Where a paper not handed over stands (flagged, not carried).</summary>
    public MissingPaperState Missing { get; }

    /// <summary>The menu the row is in: the traveller's in Papers, the books in Books, the agency's by their source (Records, Rules, Calendar; today's date in Calendar).</summary>
    public AppMenu Menu
    {
        get
        {
            if (Group == ShelfGroup.Traveller)
                return AppMenu.Papers;
            if (Group == ShelfGroup.Books)
                return AppMenu.Books;
            if (Action == ShelfAction.HoldToday || Target.Tab == AppTab.Calendar)
                return AppMenu.Calendar;
            return Target.Tab == AppTab.Records ? AppMenu.Records : AppMenu.Rules;
        }
    }

    /// <summary>True when the row is the same as <paramref name="other"/> (name, place, readability, action and flag).</summary>
    public bool Same(ShelfItem other) =>
        Label == other.Label && Target.Equals(other.Target) && Available == other.Available && Group == other.Group && Action == other.Action &&
        MissingId == other.MissingId && Missing == other.Missing;
}

/// <summary>
/// The Investigation app's menu bar (the desk-first redesign, Saleh
/// 2026-10-05, item 8: "the real estate of viewing documents is too small on
/// the PC; we need menus at the top: you press one and it has a drop-down,
/// to reduce clutter"; it replaces the header and the shelf, so the two
/// document panes get the window's height). One slim row of menu titles,
/// Papers, Records, Rules, Books and Calendar, each shown only while it has
/// something the day has introduced; a title carries the side its open
/// document is on ("L", "R") and a small dot while something waits in it.
/// A click on a title drops its menu under it (one menu at a time; the title
/// again, a click elsewhere, Escape or a choice closes it). Papers lists the
/// traveller's papers (dimmed until readable) and the transcript, then, under
/// "Not handed over", every paper of the day's papers menu the traveller has
/// not handed over, each to flag missing (MissingPapers: a click flags it;
/// the row then says it is flagged, or not carried); Calendar opens the
/// calendar and holds today's date. A document's row opens it on the target
/// side (Opened; the app swaps it with the other side when it is open
/// there). The app says what is in the menus (Show) and marks them (Mark).
/// </summary>
public sealed class MenuBarView : MonoBehaviour
{
    /// <summary>The row the menu titles sit in (a HorizontalLayoutGroup).</summary>
    [SerializeField] private RectTransform row;

    /// <summary>A menu's title (inactive): its Label, its Chevron, its Unread dot, its Side plate with its Text, its Current plate (shown while its menu is open).</summary>
    [SerializeField] private Button titleTemplate;

    /// <summary>The drop-down (inactive until a menu opens; above the work area), where the rows go.</summary>
    [SerializeField] private RectTransform dropdown;

    /// <summary>A row of the drop-down (inactive): its Unread dot, its Label, its Note (muted, at its right), its Side plate with its Text.</summary>
    [SerializeField] private Button rowTemplate;

    /// <summary>A muted caption in the drop-down (inactive): "Not handed over".</summary>
    [SerializeField] private TMP_Text captionTemplate;

    private readonly List<ShelfItem> _items = new List<ShelfItem>();
    private readonly Dictionary<AppMenu, Button> _titles = new Dictionary<AppMenu, Button>();
    private readonly List<Button> _rows = new List<Button>();
    private readonly List<int> _rowItems = new List<int>();
    private readonly List<GameObject> _made = new List<GameObject>();
    private readonly List<GameObject> _rowsMade = new List<GameObject>();
    private readonly Vector3[] _corners = new Vector3[4];
    private Func<ShelfItem, string> _sideOf;
    private Func<ShelfItem, bool> _unread;
    private AppMenu? _open;
    private bool _wired;

    /// <summary>Raised when a document's row is clicked.</summary>
    public event Action<ShelfItem> Opened;

    /// <summary>Raised when a paper not handed over is flagged missing (its request id).</summary>
    public event Action<string> Flagged;

    /// <summary>Raised when today's date is clicked (the app holds it on the workbench).</summary>
    public event Action TodayHeld;

    /// <summary>The rows in the menus, in order.</summary>
    public IReadOnlyList<ShelfItem> Items => _items;

    /// <summary>True while a menu is open (Escape closes it first).</summary>
    public bool MenuOpen => _open.HasValue && dropdown != null && dropdown.gameObject.activeSelf;

    /// <summary>The open menu, or null.</summary>
    public AppMenu? OpenMenu => MenuOpen ? _open : null;

    /// <summary>What the keys walk on the bar, in reading order: the titles, then the open menu's rows.</summary>
    public IEnumerable<Button> Buttons
    {
        get
        {
            foreach (AppMenu menu in Menus)
                if (_titles.TryGetValue(menu, out Button title))
                    yield return title;
            if (MenuOpen)
                foreach (Button r in _rows)
                    yield return r;
        }
    }

    /// <summary>The menus in their order on the bar.</summary>
    private static readonly AppMenu[] Menus = { AppMenu.Papers, AppMenu.Records, AppMenu.Rules, AppMenu.Books, AppMenu.Calendar };

    private void Awake() => Wire();

    /// <summary>The templates hidden (once).</summary>
    private void Wire()
    {
        if (_wired)
            return;
        _wired = true;
        foreach (Component template in new Component[] { titleTemplate, rowTemplate, captionTemplate })
            if (template != null)
                template.gameObject.SetActive(false);
        if (dropdown != null)
            dropdown.gameObject.SetActive(false);
    }

    /// <summary>Puts <paramref name="items"/> in the menus (in the order given): a title per menu that has a row; an open menu is drawn again (closed when it has none left).</summary>
    public void Show(IReadOnlyList<ShelfItem> items)
    {
        Wire();
        foreach (GameObject made in _made)
            Discard(made);
        _made.Clear();
        _titles.Clear();
        _items.Clear();
        if (items != null)
            _items.AddRange(items);

        foreach (AppMenu menu in Menus)
        {
            if (!_items.Exists(x => x.Menu == menu) || titleTemplate == null)
                continue;
            Button title = Instantiate(titleTemplate, row);
            title.gameObject.name = "Menu_" + menu;
            Made(title.gameObject);
            title.transform.Find("Label").GetComponent<TMP_Text>().text = UiText.Get("menu." + menu.ToString().ToLowerInvariant());
            AppMenu which = menu;
            title.onClick.AddListener(() => Toggle(which));
            _titles[menu] = title;
        }
        if (_open.HasValue && !_titles.ContainsKey(_open.Value))
            Close();
        else if (MenuOpen)
            DrawRows(_open.Value);
        Mark(_sideOf, _unread);
        if (row != null)
            LayoutRebuilder.MarkLayoutForRebuild(row);
    }

    /// <summary>
    /// Marks the titles and the open menu's rows: the side a row's document is
    /// open on (<paramref name="sideOf"/>: "L", "R" or null; a title carries
    /// its rows' sides) and its dot (<paramref name="unread"/>; a title dots
    /// while a row of its menu does). Kept for the rows drawn later.
    /// </summary>
    public void Mark(Func<ShelfItem, string> sideOf, Func<ShelfItem, bool> unread)
    {
        _sideOf = sideOf;
        _unread = unread;
        foreach (KeyValuePair<AppMenu, Button> title in _titles)
        {
            string sides = null;
            bool dot = false;
            foreach (ShelfItem item in _items)
            {
                if (item.Menu != title.Key)
                    continue;
                dot |= unread != null && unread(item);
                string side = sideOf != null ? sideOf(item) : null;
                if (side != null && (sides == null || !sides.Contains(side)))
                    sides = sides == null ? side : sides + " " + side;
            }
            Tag(title.Value.transform, sides, dot);
            Transform current = title.Value.transform.Find("Current");
            if (current != null && current.gameObject.activeSelf != (OpenMenu == title.Key))
                current.gameObject.SetActive(OpenMenu == title.Key);
        }
        for (int i = 0; i < _rows.Count && i < _rowItems.Count; i++)
        {
            ShelfItem item = _items[_rowItems[i]];
            Tag(_rows[i].transform, item.Action == ShelfAction.Open && sideOf != null ? sideOf(item) : null, item.Action == ShelfAction.Open && unread != null && unread(item));
        }
    }

    /// <summary>Opens <paramref name="menu"/>'s drop-down under its title, or closes it when it is the open one.</summary>
    public void Toggle(AppMenu menu)
    {
        if (OpenMenu == menu)
            Close();
        else
            OpenDropdown(menu);
    }

    /// <summary>Opens <paramref name="menu"/>'s drop-down under its title (its left edge on the title's); nothing when the menu has no title today.</summary>
    public void OpenDropdown(AppMenu menu)
    {
        Wire();
        if (dropdown == null || !_titles.TryGetValue(menu, out Button title) || !title.gameObject.activeInHierarchy)
            return;
        _open = menu;
        DrawRows(menu);
        title.GetComponent<RectTransform>().GetWorldCorners(_corners);
        dropdown.position = _corners[0];
        dropdown.anchoredPosition += new Vector2(0f, -4f);
        dropdown.SetAsLastSibling();
        if (!dropdown.gameObject.activeSelf)
            dropdown.gameObject.SetActive(true);
        Mark(_sideOf, _unread);
    }

    /// <summary>Closes the open drop-down.</summary>
    public void Close()
    {
        _open = null;
        if (dropdown != null && dropdown.gameObject.activeSelf)
            dropdown.gameObject.SetActive(false);
        Mark(_sideOf, _unread);
    }

    /// <summary>True when <paramref name="pressed"/> is a title or inside the drop-down (a press elsewhere closes it).</summary>
    public bool IsPart(GameObject pressed)
    {
        for (Transform t = pressed != null ? pressed.transform : null; t != null; t = t.parent)
        {
            if (dropdown != null && t == dropdown)
                return true;
            foreach (Button title in _titles.Values)
                if (title != null && t == title.transform)
                    return true;
        }
        return false;
    }

    /// <summary>The drop-down's rows for <paramref name="menu"/>: its documents, then (Papers) the caption and the papers not handed over.</summary>
    private void DrawRows(AppMenu menu)
    {
        foreach (GameObject made in _rowsMade)
            Discard(made);
        _rowsMade.Clear();
        _rows.Clear();
        _rowItems.Clear();
        if (dropdown == null || rowTemplate == null)
            return;
        bool caption = false;
        for (int pass = 0; pass < 2; pass++)
            for (int i = 0; i < _items.Count; i++)
            {
                ShelfItem item = _items[i];
                bool missing = item.Action == ShelfAction.FlagMissing;
                if (item.Menu != menu || missing != (pass == 1))
                    continue;
                if (missing && !caption && captionTemplate != null)
                {
                    caption = true;
                    TMP_Text title = Instantiate(captionTemplate, dropdown);
                    title.text = UiText.Get("menu.notHandedOver");
                    _rowsMade.Add(title.gameObject);
                    title.gameObject.SetActive(true);
                }
                _rowsMade.Add(Row(i, item).gameObject);
            }
        LayoutRebuilder.MarkLayoutForRebuild(dropdown);
    }

    /// <summary>One row: its name, dimmed while not readable, its note (a paper not handed over: "Flag missing", "Flagged: ask for it", "Not carried"), and its click.</summary>
    private Button Row(int index, ShelfItem item)
    {
        Button button = Instantiate(rowTemplate, dropdown);
        button.gameObject.name = "Row_" + index;
        button.gameObject.SetActive(true);
        button.transform.Find("Label").GetComponent<TMP_Text>().text = item.Label;
        Transform note = button.transform.Find("Note");
        string noteText = NoteFor(item);
        if (note != null)
        {
            note.gameObject.SetActive(noteText != null);
            if (noteText != null)
                note.GetComponent<TMP_Text>().text = noteText;
        }
        if (button.TryGetComponent(out CanvasGroup dim))
            dim.alpha = item.Available || item.Action != ShelfAction.Open ? 1f : 0.55f;
        button.interactable = item.Action != ShelfAction.FlagMissing || item.Missing == MissingPaperState.None;
        button.onClick.AddListener(() => Choose(index));
        _rows.Add(button);
        _rowItems.Add(index);
        return button;
    }

    /// <summary>A row's note: a paper not handed over says what a click does or what it came to; today's date says it can be compared; none else.</summary>
    private static string NoteFor(ShelfItem item)
    {
        if (item.Action == ShelfAction.HoldToday)
            return UiText.Get("menu.today.note");
        if (item.Action != ShelfAction.FlagMissing)
            return null;
        switch (item.Missing)
        {
            case MissingPaperState.Flagged: return UiText.Get("menu.missing.flagged");
            case MissingPaperState.NotCarried: return UiText.Get("menu.missing.notCarried");
            default: return UiText.Get("menu.missing.flag");
        }
    }

    /// <summary>A row was clicked: the menu closes and the row acts (opens, flags, holds today).</summary>
    private void Choose(int index)
    {
        if (index < 0 || index >= _items.Count)
            return;
        ShelfItem item = _items[index];
        Close();
        switch (item.Action)
        {
            case ShelfAction.FlagMissing:
                if (item.Missing == MissingPaperState.None)
                    Flagged?.Invoke(item.MissingId);
                break;
            case ShelfAction.HoldToday:
                TodayHeld?.Invoke();
                break;
            default:
                Opened?.Invoke(item);
                break;
        }
    }

    /// <summary>A title's or a row's side plate and dot.</summary>
    private static void Tag(Transform target, string side, bool dot)
    {
        Transform plate = target.Find("Side");
        if (plate != null)
        {
            if (plate.gameObject.activeSelf != (side != null))
                plate.gameObject.SetActive(side != null);
            if (side != null)
                plate.Find("Text").GetComponent<TMP_Text>().text = side;
        }
        Transform mark = target.Find("Unread");
        if (mark != null && mark.gameObject.activeSelf != dot)
            mark.gameObject.SetActive(dot);
    }

    /// <summary>Keeps <paramref name="made"/> to destroy at the next Show, shown.</summary>
    private void Made(GameObject made)
    {
        made.SetActive(true);
        _made.Add(made);
    }

    /// <summary>Takes an object out of this frame's layout and destroys it (Destroy waits for the frame's end).</summary>
    private static void Discard(GameObject made)
    {
        if (made == null)
            return;
        made.SetActive(false);
        Destroy(made);
    }
}
