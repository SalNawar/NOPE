using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The dock of the Lens screen (the PC Lens spec, section 1): the documents of
/// the case and the day in one row under the two sides. Left group: the
/// traveller's papers, the transcript and the papers not handed over (a dashed
/// item: a click flags it missing). A thin rule. Right group: the Registry, the
/// rules, the calendar and each book. An item holds a small paper icon (a band
/// by group), its name (it shrinks, then ends with an ellipsis; nothing wraps),
/// a small "L" or "R" tag when its document is on a side, and the unread dot;
/// an item that is not available is dimmed and cannot be placed. A click raises
/// Opened; a drag (LensDockItem) shows a ghost and raises Dragging and Dropped.
/// The app says what is in the dock (Show) and marks it (Mark). Items are made
/// from one template, as MenuBarView made its rows.
/// </summary>
public sealed class LensDock : MonoBehaviour
{
    /// <summary>The row the items go in (a HorizontalLayoutGroup).</summary>
    [SerializeField] private RectTransform row;

    /// <summary>An item (inactive): Icon (BandTraveller, BandAgency, BandBooks), Unread, Label, Side (Text), Missing.</summary>
    [SerializeField] private Button itemTemplate;

    /// <summary>The thin vertical rule between the groups (inactive).</summary>
    [SerializeField] private RectTransform ruleTemplate;

    /// <summary>The drag ghost (inactive, over everything, no raycasts): Icon (bands) and Label.</summary>
    [SerializeField] private RectTransform ghost;

    private readonly List<ShelfItem> _items = new List<ShelfItem>();
    private readonly List<Button> _buttons = new List<Button>();
    private readonly List<int> _buttonItems = new List<int>();
    private readonly List<GameObject> _made = new List<GameObject>();
    private Func<ShelfItem, string> _sideOf;
    private Func<ShelfItem, bool> _unread;
    private bool _wired;

    /// <summary>Raised when a document's item is clicked.</summary>
    public event Action<ShelfItem> Opened;

    /// <summary>Raised when a paper not handed over is clicked (its request id).</summary>
    public event Action<string> Flagged;

    /// <summary>Raised while an item is dragged: the screen point and the event camera.</summary>
    public event Action<Vector2, Camera> Dragging;

    /// <summary>Raised when a dragged item is dropped: the item, the screen point and the event camera.</summary>
    public event Action<ShelfItem, Vector2, Camera> Dropped;

    /// <summary>The items, in order.</summary>
    public IReadOnlyList<ShelfItem> Items => _items;

    /// <summary>What the keys walk in the dock, in reading order.</summary>
    public IEnumerable<Button> Buttons => _buttons;

    private void Awake() => Wire();

    private void Wire()
    {
        if (_wired)
            return;
        _wired = true;
        foreach (Component template in new Component[] { itemTemplate, ruleTemplate, ghost })
            if (template != null)
                template.gameObject.SetActive(false);
    }

    /// <summary>Puts <paramref name="items"/> in the dock: the traveller's first, a rule, then the rest (the date to hold is not shown).</summary>
    public void Show(IReadOnlyList<ShelfItem> items)
    {
        Wire();
        foreach (GameObject made in _made)
            if (made != null)
            {
                made.SetActive(false);
                Destroy(made);
            }
        _made.Clear();
        _buttons.Clear();
        _buttonItems.Clear();
        _items.Clear();
        if (items != null)
            _items.AddRange(items);
        if (itemTemplate == null || row == null)
            return;
        bool rule = false;
        for (int pass = 0; pass < 2; pass++)
            for (int i = 0; i < _items.Count; i++)
            {
                ShelfItem item = _items[i];
                if (item.Action == ShelfAction.HoldToday || (item.Group == ShelfGroup.Traveller) != (pass == 0))
                    continue;
                if (pass == 1 && !rule && _buttons.Count > 0 && ruleTemplate != null)
                {
                    rule = true;
                    RectTransform bar = Instantiate(ruleTemplate, row);
                    bar.gameObject.SetActive(true);
                    _made.Add(bar.gameObject);
                }
                Make(i, item);
            }
        Mark(_sideOf, _unread);
    }

    private void Make(int index, ShelfItem item)
    {
        Button button = Instantiate(itemTemplate, row);
        button.gameObject.name = "Item_" + index;
        button.gameObject.SetActive(true);
        _made.Add(button.gameObject);
        button.transform.Find("Label").GetComponent<TMP_Text>().text = item.Label;
        Band(button.transform.Find("Icon"), item.Group);
        bool missing = item.Action == ShelfAction.FlagMissing;
        Transform frame = button.transform.Find("Missing");
        if (frame != null)
            frame.gameObject.SetActive(missing);
        if (button.TryGetComponent(out CanvasGroup dim))
            dim.alpha = item.Available || (missing && item.Missing == MissingPaperState.None) ? 1f : 0.55f;
        button.onClick.AddListener(() => Choose(index));
        if (item.Action == ShelfAction.Open && item.Available)
            button.gameObject.AddComponent<LensDockItem>().Setup(this, item);
        _buttons.Add(button);
        _buttonItems.Add(index);
    }

    /// <summary>Marks the items: the side a document is open on (<paramref name="sideOf"/>: "L", "R" or null) and the dot (<paramref name="unread"/>). Kept for the items drawn later.</summary>
    public void Mark(Func<ShelfItem, string> sideOf, Func<ShelfItem, bool> unread)
    {
        _sideOf = sideOf;
        _unread = unread;
        for (int i = 0; i < _buttons.Count && i < _buttonItems.Count; i++)
        {
            ShelfItem item = _items[_buttonItems[i]];
            string side = sideOf != null ? sideOf(item) : null;
            Transform plate = _buttons[i].transform.Find("Side");
            if (plate != null)
            {
                if (plate.gameObject.activeSelf != (side != null))
                    plate.gameObject.SetActive(side != null);
                if (side != null)
                    plate.Find("Text").GetComponent<TMP_Text>().text = side;
            }
            Transform dot = _buttons[i].transform.Find("Unread");
            bool on = unread != null && unread(item);
            if (dot != null && dot.gameObject.activeSelf != on)
                dot.gameObject.SetActive(on);
        }
    }

    private void OnEnable() => Mark(_sideOf, _unread);

    /// <summary>Shows the band of <paramref name="group"/> on an icon (the other two hide).</summary>
    private static void Band(Transform icon, ShelfGroup group)
    {
        if (icon == null)
            return;
        foreach (ShelfGroup g in new[] { ShelfGroup.Traveller, ShelfGroup.Agency, ShelfGroup.Books })
        {
            Transform band = icon.Find("Band" + g);
            if (band != null)
                band.gameObject.SetActive(g == group);
        }
    }

    private void Choose(int index)
    {
        ShelfItem item = _items[index];
        if (item.Action == ShelfAction.FlagMissing)
        {
            if (item.Missing == MissingPaperState.None)
                Flagged?.Invoke(item.MissingId);
        }
        else if (item.Available)
            Opened?.Invoke(item);
    }

    /// <summary>Shows the ghost of <paramref name="item"/> (a small plate with its icon and name, a little turned).</summary>
    public void ShowGhost(ShelfItem item)
    {
        if (ghost == null)
            return;
        ghost.Find("Label").GetComponent<TMP_Text>().text = item.Label;
        Band(ghost.Find("Icon"), item.Group);
        ghost.localRotation = Quaternion.Euler(0f, 0f, -3f);
        ghost.SetAsLastSibling();
        ghost.gameObject.SetActive(true);
    }

    /// <summary>The ghost follows the pointer.</summary>
    public void MoveGhost(Vector2 screen, Camera camera)
    {
        if (ghost != null && ghost.gameObject.activeSelf &&
            RectTransformUtility.ScreenPointToWorldPointInRectangle((RectTransform)ghost.parent, screen, camera, out Vector3 world))
            ghost.position = world;
    }

    /// <summary>Hides the ghost.</summary>
    public void HideGhost()
    {
        if (ghost != null)
            ghost.gameObject.SetActive(false);
    }

    internal void ItemDragging(Vector2 screen, Camera camera)
    {
        MoveGhost(screen, camera);
        Dragging?.Invoke(screen, camera);
    }

    internal void ItemDropped(ShelfItem item, Vector2 screen, Camera camera)
    {
        HideGhost();
        Dropped?.Invoke(item, screen, camera);
    }
}
