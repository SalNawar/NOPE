using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// The Investigation app's navigator (the PC UX redesign IA1-IA3, IA11,
/// C2, C3): one entry per case source in the app's saved order, each its
/// full name (Papers, Citizen records, Reference books, Transcript,
/// Deviation report, Today's rules) with its badge dot. The source the
/// active pane shows is selected (its accent plate); the source the other
/// pane shows, while the two are side by side, wears its "beside" outline.
/// Under the selected source list its items (the active view's chips: each
/// paper, each book), the shown one on its paper plate, one not readable yet
/// dimmed (a click still shows why). A click on a source shows it in the
/// active pane, Ctrl+click in the other one; a click on an item shows that
/// item; an entry dragged up or down past a neighbour's middle moves there
/// (TabOrder.DragTarget) and a right-click opens its menu (Move up, Move
/// down, Reset order). The app (InvestigationApp) decides what shows; the
/// navigator only draws it and reports the player's clicks. Its entries and
/// items are laid out in one vertical list (the sidebar's), items cloned
/// from an inactive template right after their source.
/// </summary>
public sealed class AppNav : MonoBehaviour
{
    /// <summary>The sources' entries, indexed by the tab's value (AppTab).</summary>
    [SerializeField] private Button[] entries = new Button[0];

    /// <summary>Each entry's selected look (its accent plate and label), indexed by the tab's value.</summary>
    [SerializeField] private GameObject[] selected = new GameObject[0];

    /// <summary>Each entry's "beside" outline (the other pane shows it), indexed by the tab's value.</summary>
    [SerializeField] private GameObject[] beside = new GameObject[0];

    /// <summary>Each entry's badge dot, indexed by the tab's value.</summary>
    [SerializeField] private GameObject[] badges = new GameObject[0];

    /// <summary>An item row (inactive): a button whose child "Label" reads the item and whose "Chosen" plate marks the shown one.</summary>
    [SerializeField] private Button itemTemplate;

    /// <summary>An unavailable item's tint (dimmed; it still shows why when clicked).</summary>
    [SerializeField] private Color unavailableTint = new Color(1f, 1f, 1f, 0.55f);

    private readonly List<Button> _items = new List<Button>();
    private readonly List<float> _middles = new List<float>();
    private bool _wired;

    /// <summary>Raised when a source is clicked: the tab, and true to show it in the other pane (Ctrl held).</summary>
    public event Action<AppTab, bool> SourceClicked;

    /// <summary>Raised when an item under the selected source is clicked: its index.</summary>
    public event Action<int> ItemClicked;

    /// <summary>Raised when a source is dragged past a neighbour's middle: the tab and the position it belongs at.</summary>
    public event Action<AppTab, int> SourceDragged;

    /// <summary>Raised when a source is right-clicked: the tab and the click (its menu opens there).</summary>
    public event Action<AppTab, PointerEventData> SourceMenuRequested;

    /// <summary>The source's entry, or null.</summary>
    public Button Entry(AppTab tab) => At(entries, tab);

    /// <summary>The items drawn under the selected source, top to bottom.</summary>
    public IReadOnlyList<Button> Items => _items;

    /// <summary>
    /// Draws the navigator: <paramref name="active"/> selected,
    /// <paramref name="other"/> (null: one pane) outlined, and under the
    /// selected source the items of <paramref name="view"/> (none when null:
    /// the no-case state), the shown one chosen.
    /// </summary>
    public void Show(AppTab active, AppTab? other, IAppView view)
    {
        Wire();
        foreach (AppTab tab in TabOrder.Default)
        {
            Toggle(At(selected, tab), tab == active);
            Toggle(At(beside, tab), other.HasValue && other.Value == tab && tab != active);
        }
        DrawItems(active, view);
    }

    /// <summary>Lays the entries out in <paramref name="order"/> (their items follow the selected one).</summary>
    public void ApplyOrder(TabOrder order)
    {
        Wire();
        ClearItems();
        int at = FirstEntryIndex();
        for (int position = 0; position < order.Tabs.Count; position++)
        {
            Button entry = At(entries, order.Tabs[position]);
            if (entry != null)
                entry.transform.SetSiblingIndex(at + position);
        }
    }

    /// <summary>Shows or hides the source's badge.</summary>
    public void SetBadge(AppTab tab, bool on) => Toggle(At(badges, tab), on);

    /// <summary>An entry is dragged to <paramref name="eventData"/>'s pointer (AppTabHandle): past a neighbour's middle it asks to move there.</summary>
    public void DragEntry(AppTab tab, PointerEventData eventData)
    {
        Button dragged = At(entries, tab);
        var list = dragged != null ? dragged.transform.parent as RectTransform : null;
        if (list == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(list, eventData.position, eventData.pressEventCamera, out Vector2 pointer))
            return;

        // Top to bottom, as rising values (TabOrder.DragTarget reads its middles in order).
        _middles.Clear();
        int from = -1;
        foreach (AppTab t in OrderShown())
        {
            var rect = (RectTransform)At(entries, t).transform;
            if (t == tab)
                from = _middles.Count;
            _middles.Add(-(rect.localPosition.y + (0.5f - rect.pivot.y) * rect.rect.height));
        }
        int to = TabOrder.DragTarget(_middles, from, -pointer.y);
        if (from >= 0 && to != from)
            SourceDragged?.Invoke(tab, to);
    }

    /// <summary>A source is right-clicked (AppTabHandle): its menu.</summary>
    public void RequestMenu(AppTab tab, PointerEventData eventData) => SourceMenuRequested?.Invoke(tab, eventData);

    /// <summary>The entries' clicks and the template hidden (once).</summary>
    private void Wire()
    {
        if (_wired)
            return;
        _wired = true;
        if (itemTemplate != null)
            itemTemplate.gameObject.SetActive(false);
        foreach (AppTab tab in TabOrder.Default)
        {
            Button entry = At(entries, tab);
            if (entry == null)
                continue;
            AppTab t = tab;
            entry.onClick.AddListener(() => SourceClicked?.Invoke(t, CtrlHeld()));
        }
    }

    /// <summary>The items of <paramref name="view"/> right after <paramref name="active"/>'s entry: the chosen one on its plate, an unavailable one dimmed.</summary>
    private void DrawItems(AppTab active, IAppView view)
    {
        ClearItems();

        Button entry = At(entries, active);
        if (itemTemplate == null || entry == null || view == null)
            return;
        IReadOnlyList<AppChip> chips = view.Chips;
        int at = entry.transform.GetSiblingIndex() + 1;
        for (int i = 0; i < chips.Count; i++)
        {
            Button item = Instantiate(itemTemplate, itemTemplate.transform.parent);
            item.name = "Item_" + i;
            item.gameObject.SetActive(true);
            item.transform.SetSiblingIndex(at + i);
            Transform label = item.transform.Find("Label");
            if (label != null && label.TryGetComponent(out TMP_Text text))
                text.text = chips[i].Label;
            Transform chosen = item.transform.Find("Chosen");
            if (chosen != null)
                chosen.gameObject.SetActive(i == view.Selected);
            if (label != null && label.TryGetComponent(out TMP_Text bold))
                bold.fontStyle = i == view.Selected ? FontStyles.Bold : FontStyles.Normal;
            ColorBlock colours = item.colors;
            colours.normalColor = chips[i].Available ? Color.white : unavailableTint;
            colours.selectedColor = colours.normalColor;
            item.colors = colours;
            int index = i;
            item.onClick.AddListener(() => ItemClicked?.Invoke(index));
            _items.Add(item);
        }
    }

    /// <summary>The items go (taken out of the list at once, so the entries' places are right in the same frame; destroyed at its end).</summary>
    private void ClearItems()
    {
        foreach (Button item in _items)
            if (item != null)
            {
                item.gameObject.SetActive(false);
                item.transform.SetParent(null, false);
                Destroy(item.gameObject);
            }
        _items.Clear();
    }

    /// <summary>The sources in their shown order (top to bottom).</summary>
    private IEnumerable<AppTab> OrderShown()
    {
        var shown = new SortedList<int, AppTab>();
        foreach (AppTab tab in TabOrder.Default)
        {
            Button entry = At(entries, tab);
            if (entry != null)
                shown.Add(entry.transform.GetSiblingIndex(), tab);
        }
        return shown.Values;
    }

    /// <summary>The sibling index of the first entry in the list (the entries come after the case summary).</summary>
    private int FirstEntryIndex()
    {
        int first = int.MaxValue;
        foreach (AppTab tab in TabOrder.Default)
        {
            Button entry = At(entries, tab);
            if (entry != null)
                first = Mathf.Min(first, entry.transform.GetSiblingIndex());
        }
        return first == int.MaxValue ? 0 : first;
    }

    private static void Toggle(GameObject go, bool on)
    {
        if (go != null && go.activeSelf != on)
            go.SetActive(on);
    }

    /// <summary>True while Ctrl is held (a Ctrl+click shows the source in the other pane).</summary>
    private static bool CtrlHeld()
    {
        Keyboard keys = Keyboard.current;
        return keys != null && keys.ctrlKey.isPressed;
    }

    /// <summary>The tab's entry in an array indexed by the tab's value, or null.</summary>
    private static T At<T>(T[] byTab, AppTab tab) where T : class
    {
        int i = (int)tab;
        return byTab != null && i >= 0 && i < byTab.Length ? byTab[i] : null;
    }
}
